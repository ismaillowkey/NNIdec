using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NNIdec.Core;
using NNIdec.Interfaces;
using NNIdec.Protocol;

namespace NNIdec
{
    /// <summary>
    /// Kelas dasar abstrak klien PLC IDEC yang mengimplementasikan protokol komunikasi lengkap.
    /// Pengguna dapat menggunakan tipe ini secara polimorfik untuk semua tipe PLC IDEC.
    /// </summary>
    public abstract class NNIdec : IIdecClient
    {
        private readonly object _syncLock = new object();
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;

        /// <summary>
        /// Alamat IP dari PLC IDEC.
        /// </summary>
        public string IpAddress { get; set; } = "192.168.0.1";

        /// <summary>
        /// Nomor Port komunikasi (Default 502 untuk Modbus TCP atau 2101 untuk IDEC Maintenance Protocol).
        /// </summary>
        public int Port { get; set; } = 502;

        /// <summary>
        /// Nomor Station atau Unit ID PLC IDEC (Default 0 atau 1).
        /// </summary>
        public byte StationId { get; set; } = 1;

        /// <summary>
        /// Batas waktu timeout untuk pembentukan koneksi (milidetik).
        /// </summary>
        public int ConnectTimeout { get; set; } = 3000;

        /// <summary>
        /// Batas waktu timeout untuk pengiriman dan penerimaan data (milidetik).
        /// </summary>
        public int ReceiveTimeout { get; set; } = 3000;

        /// <summary>
        /// Format urutan byte (Endianness) untuk tipe data 32-bit dan 64-bit.
        /// Default adalah CDAB (Little-Endian word swap yang umum digunakan pada IDEC &amp; Modbus).
        /// </summary>
        public EndianFormat ByteOrder { get; set; } = EndianFormat.CDAB;

        /// <summary>
        /// Mengaktifkan fitur sambung ulang otomatis (Auto Reconnect).
        /// Jika bernilai true (default), maka ketika koneksi jaringan ke PLC terputus,
        /// library akan otomatis membuka koneksi kembali pada saat operasi baca/tulis berikutnya,
        /// dan mengulang (retry) satu kali request yang gagal akibat koneksi putus.
        /// Jika bernilai false, operasi baca/tulis akan langsung gagal saat koneksi terputus
        /// dan pengguna wajib memanggil Connect() / ConnectAsync() secara manual.
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// Model PLC IDEC yang sedang aktif (FC6A, MicroSmart, atau Micro).
        /// </summary>
        public abstract IdecPlcModel Model { get; }

        /// <summary>
        /// Menunjukkan apakah koneksi TCP sedang aktif.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_tcpClient == null)
                {
                    return false;
                }
                if (_tcpClient.Client == null)
                {
                    return false;
                }
                return _tcpClient.Connected;
            }
        }

        /// <summary>
        /// Konstruktor dasar untuk inisialisasi parameter koneksi.
        /// </summary>
        protected NNIdec(string ipAddress, int port = 502, byte stationId = 1, bool autoReconnect = true)
        {
            IpAddress = ipAddress;
            Port = port;
            StationId = stationId;
            AutoReconnect = autoReconnect;
        }

        #region Manajemen Koneksi

        /// <summary>
        /// Membuka koneksi TCP ke PLC secara synchronous.
        /// </summary>
        public virtual IdecResult Connect()
        {
            lock (_syncLock)
            {
                try
                {
                    Disconnect();

                    TcpClient client = new TcpClient();
                    IAsyncResult ar = client.BeginConnect(IpAddress, Port, null, null);
                    bool success = ar.AsyncWaitHandle.WaitOne(ConnectTimeout);

                    if (!success)
                    {
                        client.Close();
                        return IdecResult.Fail("Koneksi ke PLC IDEC di " + IpAddress + ":" + Port + " melebihi batas waktu (timeout " + ConnectTimeout + " ms).", -101);
                    }

                    client.EndConnect(ar);
                    client.ReceiveTimeout = ReceiveTimeout;
                    client.SendTimeout = ReceiveTimeout;

                    _tcpClient = client;
                    _networkStream = client.GetStream();

                    return IdecResult.Ok("Berhasil terhubung ke PLC IDEC " + Model + " di " + IpAddress + ":" + Port);
                }
                catch (Exception ex)
                {
                    Disconnect();
                    return IdecResult.Fail("Gagal menghubungkan ke PLC IDEC: " + ex.Message, -102);
                }
            }
        }

        /// <summary>
        /// Membuka koneksi TCP ke PLC secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult> ConnectAsync()
        {
            try
            {
                Disconnect();

                TcpClient client = new TcpClient();
                using (CancellationTokenSource cts = new CancellationTokenSource(ConnectTimeout))
                {
#if NET6_0_OR_GREATER
                    await client.ConnectAsync(IpAddress, Port, cts.Token).ConfigureAwait(false);
#else
                    Task connectTask = client.ConnectAsync(IpAddress, Port);
                    Task timeoutTask = Task.Delay(ConnectTimeout, cts.Token);
                    Task completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                    if (completedTask != connectTask)
                    {
                        client.Close();
                        return IdecResult.Fail("Koneksi async ke PLC IDEC di " + IpAddress + ":" + Port + " melebihi batas waktu (timeout).", -101);
                    }
                    await connectTask.ConfigureAwait(false);
#endif
                }

                client.ReceiveTimeout = ReceiveTimeout;
                client.SendTimeout = ReceiveTimeout;

                lock (_syncLock)
                {
                    _tcpClient = client;
                    _networkStream = client.GetStream();
                }

                return IdecResult.Ok("Berhasil terhubung ke PLC IDEC " + Model + " di " + IpAddress + ":" + Port);
            }
            catch (Exception ex)
            {
                Disconnect();
                return IdecResult.Fail("Gagal menghubungkan async ke PLC IDEC: " + ex.Message, -102);
            }
        }

        /// <summary>
        /// Menutup koneksi TCP dan membersihkan stream.
        /// </summary>
        public virtual void Disconnect()
        {
            lock (_syncLock)
            {
                if (_networkStream != null)
                {
                    try { _networkStream.Dispose(); } catch { }
                    _networkStream = null;
                }

                if (_tcpClient != null)
                {
                    try { _tcpClient.Close(); } catch { }
                    _tcpClient = null;
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        #endregion

        #region Komunikasi Jaringan Tingkat Rendah

        /// <summary>
        /// Mengirim data byte request ke PLC dan menerima respon balasannya secara synchronous.
        /// </summary>
        protected virtual IdecResult<byte[]> SendAndReceive(byte[] request)
        {
            lock (_syncLock)
            {
                // Langkah 1: Pastikan koneksi tersedia sebelum mengirim request
                if (!IsConnected)
                {
                    if (!AutoReconnect)
                    {
                        // Auto Reconnect nonaktif: langsung gagal, pengguna harus Connect() manual
                        return IdecResult<byte[]>.Fail("Tidak terhubung ke PLC IDEC di " + IpAddress + ":" + Port + ". AutoReconnect nonaktif, silakan panggil Connect() terlebih dahulu.", -106);
                    }

                    // Auto Reconnect aktif: buka koneksi kembali secara otomatis
                    IdecResult connResult = Connect();
                    if (!connResult.IsSuccess)
                    {
                        return IdecResult<byte[]>.Fail(connResult.Message, connResult.ErrorCode);
                    }
                }

                // Langkah 2: Kirim request untuk pertama kali
                IdecResult<byte[]> firstResult = TransmitOnce(request);
                if (firstResult.IsSuccess)
                {
                    return firstResult;
                }

                // Langkah 3: Jika gagal bukan karena koneksi putus, atau AutoReconnect nonaktif, kembalikan hasil apa adanya
                if (!AutoReconnect)
                {
                    return firstResult;
                }
                if (!IsConnectionLostError(firstResult.ErrorCode))
                {
                    return firstResult;
                }

                // Langkah 4: Koneksi terputus -> sambung ulang lalu ulangi request satu kali
                IdecResult reconnectResult = Connect();
                if (!reconnectResult.IsSuccess)
                {
                    return IdecResult<byte[]>.Fail("Koneksi ke PLC IDEC terputus dan gagal tersambung ulang: " + reconnectResult.Message, reconnectResult.ErrorCode);
                }

                return TransmitOnce(request);
            }
        }

        /// <summary>
        /// Menentukan apakah kode error menandakan koneksi jaringan terputus / timeout,
        /// sehingga layak dilakukan sambung ulang (reconnect) dan pengulangan request.
        /// </summary>
        private static bool IsConnectionLostError(int errorCode)
        {
            // -101: Timeout, -103: Stream belum siap, -104: Koneksi ditutup remote, -105: Error transmisi socket
            if (errorCode == -101)
            {
                return true;
            }
            if (errorCode == -103)
            {
                return true;
            }
            if (errorCode == -104)
            {
                return true;
            }
            if (errorCode == -105)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mengirim satu kali request ke PLC dan membaca respon (tanpa logika reconnect) secara synchronous.
        /// Wajib dipanggil di dalam lock _syncLock.
        /// </summary>
        private IdecResult<byte[]> TransmitOnce(byte[] request)
        {
            try
            {
                if (_networkStream == null)
                {
                    return IdecResult<byte[]>.Fail("NetworkStream belum terinisialisasi.", -103);
                }

                // Bersihkan sisa data lama di buffer masuk jika ada
                while (_networkStream.DataAvailable)
                {
                    _networkStream.ReadByte();
                }

                // Kirim request ke PLC
                _networkStream.Write(request, 0, request.Length);
                _networkStream.Flush();

                // Baca respon dari PLC
                byte[] buffer = new byte[2048];
                int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);

                if (bytesRead <= 0)
                {
                    // Socket ditutup oleh remote -> tandai koneksi sebagai terputus
                    Disconnect();
                    return IdecResult<byte[]>.Fail("Koneksi terputus saat membaca respon dari PLC IDEC.", -104);
                }

                byte[] response = new byte[bytesRead];
                Array.Copy(buffer, 0, response, 0, bytesRead);
                return IdecResult<byte[]>.Ok(response);
            }
            catch (Exception ex)
            {
                Disconnect();
                return IdecResult<byte[]>.Fail("Terjadi kesalahan transmisi data dengan PLC IDEC: " + ex.Message, -105);
            }
        }

        /// <summary>
        /// Mengirim data byte request ke PLC dan menerima respon balasannya secara asynchronous.
        /// </summary>
        protected virtual async Task<IdecResult<byte[]>> SendAndReceiveAsync(byte[] request)
        {
            // Langkah 1: Pastikan koneksi tersedia sebelum mengirim request
            if (!IsConnected)
            {
                if (!AutoReconnect)
                {
                    // Auto Reconnect nonaktif: langsung gagal, pengguna harus ConnectAsync() manual
                    return IdecResult<byte[]>.Fail("Tidak terhubung ke PLC IDEC di " + IpAddress + ":" + Port + ". AutoReconnect nonaktif, silakan panggil ConnectAsync() terlebih dahulu.", -106);
                }

                // Auto Reconnect aktif: buka koneksi kembali secara otomatis
                IdecResult connResult = await ConnectAsync().ConfigureAwait(false);
                if (!connResult.IsSuccess)
                {
                    return IdecResult<byte[]>.Fail(connResult.Message, connResult.ErrorCode);
                }
            }

            // Langkah 2: Kirim request untuk pertama kali
            IdecResult<byte[]> firstResult = await TransmitOnceAsync(request).ConfigureAwait(false);
            if (firstResult.IsSuccess)
            {
                return firstResult;
            }

            // Langkah 3: Jika gagal bukan karena koneksi putus, atau AutoReconnect nonaktif, kembalikan hasil apa adanya
            if (!AutoReconnect)
            {
                return firstResult;
            }
            if (!IsConnectionLostError(firstResult.ErrorCode))
            {
                return firstResult;
            }

            // Langkah 4: Koneksi terputus -> sambung ulang lalu ulangi request satu kali
            IdecResult reconnectResult = await ConnectAsync().ConfigureAwait(false);
            if (!reconnectResult.IsSuccess)
            {
                return IdecResult<byte[]>.Fail("Koneksi ke PLC IDEC terputus dan gagal tersambung ulang: " + reconnectResult.Message, reconnectResult.ErrorCode);
            }

            return await TransmitOnceAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Mengirim satu kali request ke PLC dan membaca respon (tanpa logika reconnect) secara asynchronous.
        /// </summary>
        private async Task<IdecResult<byte[]>> TransmitOnceAsync(byte[] request)
        {
            try
            {
                NetworkStream? stream;
                lock (_syncLock)
                {
                    stream = _networkStream;
                }

                if (stream == null)
                {
                    return IdecResult<byte[]>.Fail("NetworkStream belum terinisialisasi.", -103);
                }

                // Menggunakan CancellationTokenSource dan Task.WhenAny agar batas ReceiveTimeout berfungsi pasti di seluruh runtime (.NET Core dan .NET Framework)
                using (CancellationTokenSource cts = new CancellationTokenSource(ReceiveTimeout))
                {
                    try
                    {
                        // Kirim request ke PLC secara asynchronous
                        await stream.WriteAsync(request, 0, request.Length, cts.Token).ConfigureAwait(false);
                        await stream.FlushAsync(cts.Token).ConfigureAwait(false);

                        // Baca respon dari PLC secara asynchronous dengan batas waktu (timeout)
                        byte[] buffer = new byte[2048];
                        Task<int> readTask = stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        Task timeoutTask = Task.Delay(ReceiveTimeout, cts.Token);
                        Task completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                        if (completedTask != readTask)
                        {
                            Disconnect();
                            return IdecResult<byte[]>.Fail("Waktu tunggu respon (timeout) habis saat berkomunikasi async dengan PLC IDEC di " + IpAddress + ":" + Port + " (" + ReceiveTimeout + " ms).", -101);
                        }

                        int bytesRead = await readTask.ConfigureAwait(false);

                        if (bytesRead <= 0)
                        {
                            Disconnect();
                            return IdecResult<byte[]>.Fail("Koneksi terputus saat membaca respon async dari PLC IDEC.", -104);
                        }

                        byte[] response = new byte[bytesRead];
                        Array.Copy(buffer, 0, response, 0, bytesRead);
                        return IdecResult<byte[]>.Ok(response);
                    }
                    catch (OperationCanceledException)
                    {
                        Disconnect();
                        return IdecResult<byte[]>.Fail("Waktu tunggu respon (timeout) habis saat berkomunikasi async dengan PLC IDEC di " + IpAddress + ":" + Port + " (" + ReceiveTimeout + " ms).", -101);
                    }
                }
            }
            catch (Exception ex)
            {
                Disconnect();
                return IdecResult<byte[]>.Fail("Terjadi kesalahan transmisi data async dengan PLC IDEC: " + ex.Message, -105);
            }
        }

        #endregion

        #region Operasi Inti Baca &amp; Tulis Word / Bit

        /// <summary>
        /// Membaca array word mentah dari register PLC (misal D, TC, TP, CP, CC).
        /// </summary>
        public virtual IdecResult<ushort[]> ReadWords(string address, ushort count)
        {
            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult<ushort[]>.Fail("Alamat PLC tidak valid: " + ex.Message, -201);
            }

            if (count == 0)
            {
                return IdecResult<ushort[]>.Fail("Jumlah pembacaan word harus lebih besar dari 0.", -202);
            }

            // Jika menggunakan port 502 (Modbus TCP)
            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusReadFrame(txId, StationId, fc, startAddr, count);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<ushort[]>.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                // Validasi panjang respon Modbus TCP: MBAP (6 byte) + UnitId (1 byte) + FC (1 byte) + ByteCount (1 byte) + Data
                if (resp.Length < 9)
                {
                    return IdecResult<ushort[]>.Fail("Ukuran paket respon Modbus TCP terlalu pendek.", -203);
                }

                // Cek exception function code
                if ((resp[7] & 0x80) != 0)
                {
                    byte errCode = resp[8];
                    return IdecResult<ushort[]>.Fail("PLC IDEC mengembalikan Modbus Exception Code: " + errCode, -204);
                }

                byte byteCount = resp[8];
                int actualWords = byteCount / 2;
                ushort[] words = new ushort[actualWords];

                for (int i = 0; i < actualWords; i++)
                {
                    int offset = 9 + (i * 2);
                    words[i] = (ushort)((resp[offset] << 8) | resp[offset + 1]);
                }

                return IdecResult<ushort[]>.Ok(words);
            }
            else
            {
                // Menggunakan protokol Maintenance ASCII IDEC (port 2101 / custom)
                byte[] req = IdecMaintenanceProtocol.BuildReadWordFrame(StationId, addr, count);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<ushort[]>.Fail(netRes.Message, netRes.ErrorCode);
                }

                return IdecMaintenanceProtocol.ParseReadWordResponse(netRes.Content, netRes.Content.Length, count);
            }
        }

        /// <summary>
        /// Membaca array word mentah secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult<ushort[]>> ReadWordsAsync(string address, ushort count)
        {
            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult<ushort[]>.Fail("Alamat PLC tidak valid: " + ex.Message, -201);
            }

            if (count == 0)
            {
                return IdecResult<ushort[]>.Fail("Jumlah pembacaan word harus lebih besar dari 0.", -202);
            }

            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusReadFrame(txId, StationId, fc, startAddr, count);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<ushort[]>.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length < 9)
                {
                    return IdecResult<ushort[]>.Fail("Ukuran paket respon Modbus TCP async terlalu pendek.", -203);
                }

                if ((resp[7] & 0x80) != 0)
                {
                    byte errCode = resp[8];
                    return IdecResult<ushort[]>.Fail("PLC IDEC mengembalikan Modbus Exception Code: " + errCode, -204);
                }

                byte byteCount = resp[8];
                int actualWords = byteCount / 2;
                ushort[] words = new ushort[actualWords];

                for (int i = 0; i < actualWords; i++)
                {
                    int offset = 9 + (i * 2);
                    words[i] = (ushort)((resp[offset] << 8) | resp[offset + 1]);
                }

                return IdecResult<ushort[]>.Ok(words);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildReadWordFrame(StationId, addr, count);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<ushort[]>.Fail(netRes.Message, netRes.ErrorCode);
                }

                return IdecMaintenanceProtocol.ParseReadWordResponse(netRes.Content, netRes.Content.Length, count);
            }
        }

        /// <summary>
        /// Menulis array word ke register PLC.
        /// </summary>
        public virtual IdecResult WriteWords(string address, ushort[] values)
        {
            if (values == null || values.Length == 0)
            {
                return IdecResult.Fail("Array data values yang akan ditulis tidak boleh kosong.", -301);
            }

            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult.Fail("Alamat PLC tidak valid: " + ex.Message, -302);
            }

            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusWriteRegistersFrame(txId, StationId, startAddr, values);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length >= 9 && (resp[7] & 0x80) != 0)
                {
                    return IdecResult.Fail("PLC IDEC mengembalikan Modbus Exception saat menulis: " + resp[8], -303);
                }

                return IdecResult.Ok("Berhasil menulis " + values.Length + " word ke alamat " + address);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildWriteWordFrame(StationId, addr, values);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                return IdecResult.Ok("Berhasil menulis " + values.Length + " word ke alamat " + address);
            }
        }

        /// <summary>
        /// Menulis array word ke register PLC secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult> WriteWordsAsync(string address, ushort[] values)
        {
            if (values == null || values.Length == 0)
            {
                return IdecResult.Fail("Array data values yang akan ditulis tidak boleh kosong.", -301);
            }

            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult.Fail("Alamat PLC tidak valid: " + ex.Message, -302);
            }

            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusWriteRegistersFrame(txId, StationId, startAddr, values);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length >= 9 && (resp[7] & 0x80) != 0)
                {
                    return IdecResult.Fail("PLC IDEC mengembalikan Modbus Exception saat menulis async: " + resp[8], -303);
                }

                return IdecResult.Ok("Berhasil menulis async " + values.Length + " word ke alamat " + address);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildWriteWordFrame(StationId, addr, values);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                return IdecResult.Ok("Berhasil menulis async " + values.Length + " word ke alamat " + address);
            }
        }

        #endregion

        #region Operasi Boolean / Bit

        /// <summary>
        /// Membaca nilai boolean / bit dari PLC (I, Q, M, R, T, C, atau D_Bit seperti D100.5).
        /// </summary>
        public virtual IdecResult<bool> ReadBool(string address)
        {
            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult<bool>.Fail("Alamat bit tidak valid: " + ex.Message, -401);
            }

            // Jika alamat adalah bit di dalam register (D100.5)
            if (addr.DeviceType == IdecDeviceType.DataRegisterBit)
            {
                IdecResult<ushort> regRes = ReadUInt16("D" + addr.AddressNumber);
                if (!regRes.IsSuccess)
                {
                    return IdecResult<bool>.Fail(regRes.Message, regRes.ErrorCode);
                }

                int mask = 1 << addr.BitIndex;
                bool isSet = (regRes.Content & mask) != 0;
                return IdecResult<bool>.Ok(isSet);
            }

            // Jika membaca relay / coil / input fisik
            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusReadFrame(txId, StationId, fc, startAddr, 1);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<bool>.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length < 10)
                {
                    return IdecResult<bool>.Fail("Respon baca bit Modbus TCP terlalu pendek.", -402);
                }

                if ((resp[7] & 0x80) != 0)
                {
                    return IdecResult<bool>.Fail("PLC IDEC mengembalikan exception Modbus: " + resp[8], -403);
                }

                bool bitVal = (resp[9] & 0x01) != 0;
                return IdecResult<bool>.Ok(bitVal);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildReadBitFrame(StationId, addr, 1);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<bool>.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length >= 6 && resp[0] == IdecMaintenanceProtocol.ACK)
                {
                    if (resp[3] == '0' && resp.Length >= 7)
                    {
                        string hexStr = Encoding.ASCII.GetString(resp, 4, 2);
                        int val;
                        if (int.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                        {
                            return IdecResult<bool>.Ok(val != 0);
                        }
                    }
                    else if (resp[3] == '2')
                    {
                        return IdecResult<bool>.Fail("PLC IDEC mengembalikan status error saat membaca bit: " + address, -403);
                    }
                }
                else if (resp.Length > 0 && resp[0] == IdecMaintenanceProtocol.NAK)
                {
                    return IdecResult<bool>.Fail("PLC IDEC menolak perintah baca bit (NAK) untuk alamat: " + address, -402);
                }

                string text = Encoding.ASCII.GetString(resp);
                return IdecResult<bool>.Ok(text.Contains("1"));
            }
        }

        /// <summary>
        /// Membaca nilai boolean / bit secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult<bool>> ReadBoolAsync(string address)
        {
            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult<bool>.Fail("Alamat bit tidak valid: " + ex.Message, -401);
            }

            if (addr.DeviceType == IdecDeviceType.DataRegisterBit)
            {
                IdecResult<ushort> regRes = await ReadUInt16Async("D" + addr.AddressNumber).ConfigureAwait(false);
                if (!regRes.IsSuccess)
                {
                    return IdecResult<bool>.Fail(regRes.Message, regRes.ErrorCode);
                }

                int mask = 1 << addr.BitIndex;
                bool isSet = (regRes.Content & mask) != 0;
                return IdecResult<bool>.Ok(isSet);
            }

            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusReadFrame(txId, StationId, fc, startAddr, 1);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<bool>.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length < 10)
                {
                    return IdecResult<bool>.Fail("Respon baca bit Modbus TCP async terlalu pendek.", -402);
                }

                if ((resp[7] & 0x80) != 0)
                {
                    return IdecResult<bool>.Fail("PLC IDEC mengembalikan exception Modbus: " + resp[8], -403);
                }

                bool bitVal = (resp[9] & 0x01) != 0;
                return IdecResult<bool>.Ok(bitVal);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildReadBitFrame(StationId, addr, 1);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult<bool>.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length >= 6 && resp[0] == IdecMaintenanceProtocol.ACK)
                {
                    if (resp[3] == '0' && resp.Length >= 7)
                    {
                        string hexStr = Encoding.ASCII.GetString(resp, 4, 2);
                        int val;
                        if (int.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                        {
                            return IdecResult<bool>.Ok(val != 0);
                        }
                    }
                    else if (resp[3] == '2')
                    {
                        return IdecResult<bool>.Fail("PLC IDEC mengembalikan status error saat membaca bit: " + address, -403);
                    }
                }
                else if (resp.Length > 0 && resp[0] == IdecMaintenanceProtocol.NAK)
                {
                    return IdecResult<bool>.Fail("PLC IDEC menolak perintah baca bit (NAK) untuk alamat: " + address, -402);
                }

                string text = Encoding.ASCII.GetString(resp);
                return IdecResult<bool>.Ok(text.Contains("1"));
            }
        }

        /// <summary>
        /// Menulis nilai boolean / bit ke PLC.
        /// </summary>
        public virtual IdecResult WriteBool(string address, bool value)
        {
            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult.Fail("Alamat bit tidak valid: " + ex.Message, -404);
            }

            // Jika menulis ke bit di dalam register (D100.5)
            if (addr.DeviceType == IdecDeviceType.DataRegisterBit)
            {
                string regAddr = "D" + addr.AddressNumber;
                IdecResult<ushort> readRes = ReadUInt16(regAddr);
                if (!readRes.IsSuccess)
                {
                    return IdecResult.Fail("Gagal membaca register dasar sebelum menulis bit: " + readRes.Message, readRes.ErrorCode);
                }

                ushort current = readRes.Content;
                int mask = 1 << addr.BitIndex;
                ushort modified;

                if (value)
                {
                    // Set bit
                    modified = (ushort)(current | mask);
                }
                else
                {
                    // Clear bit
                    modified = (ushort)(current & ~mask);
                }

                return WriteUInt16(regAddr, modified);
            }

            // Jika menulis ke relay / coil output
            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusWriteSingleCoilFrame(txId, StationId, startAddr, value);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length >= 9 && (resp[7] & 0x80) != 0)
                {
                    return IdecResult.Fail("PLC IDEC mengembalikan exception Modbus saat menulis coil: " + resp[8], -405);
                }

                return IdecResult.Ok("Berhasil menulis bit ke alamat " + address);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildWriteBitFrame(StationId, addr, value);
                IdecResult<byte[]> netRes = SendAndReceive(req);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                return IdecResult.Ok("Berhasil menulis bit ke alamat " + address);
            }
        }

        /// <summary>
        /// Menulis nilai boolean / bit secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult> WriteBoolAsync(string address, bool value)
        {
            IdecAddress addr;
            try
            {
                addr = IdecAddress.Parse(address, Model);
            }
            catch (Exception ex)
            {
                return IdecResult.Fail("Alamat bit tidak valid: " + ex.Message, -404);
            }

            if (addr.DeviceType == IdecDeviceType.DataRegisterBit)
            {
                string regAddr = "D" + addr.AddressNumber;
                IdecResult<ushort> readRes = await ReadUInt16Async(regAddr).ConfigureAwait(false);
                if (!readRes.IsSuccess)
                {
                    return IdecResult.Fail("Gagal membaca register dasar sebelum menulis bit async: " + readRes.Message, readRes.ErrorCode);
                }

                ushort current = readRes.Content;
                int mask = 1 << addr.BitIndex;
                ushort modified;

                if (value)
                {
                    modified = (ushort)(current | mask);
                }
                else
                {
                    modified = (ushort)(current & ~mask);
                }

                return await WriteUInt16Async(regAddr, modified).ConfigureAwait(false);
            }

            if (Port == 502)
            {
                ushort txId = IdecModbusProtocol.NextTransactionId();
                byte fc;
                ushort startAddr;
                IdecModbusProtocol.MapAddressToModbus(addr, out fc, out startAddr);

                byte[] req = IdecModbusProtocol.BuildModbusWriteSingleCoilFrame(txId, StationId, startAddr, value);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                byte[] resp = netRes.Content;
                if (resp.Length >= 9 && (resp[7] & 0x80) != 0)
                {
                    return IdecResult.Fail("PLC IDEC mengembalikan exception Modbus saat menulis coil async: " + resp[8], -405);
                }

                return IdecResult.Ok("Berhasil menulis async bit ke alamat " + address);
            }
            else
            {
                byte[] req = IdecMaintenanceProtocol.BuildWriteBitFrame(StationId, addr, value);
                IdecResult<byte[]> netRes = await SendAndReceiveAsync(req).ConfigureAwait(false);

                if (!netRes.IsSuccess || netRes.Content == null)
                {
                    return IdecResult.Fail(netRes.Message, netRes.ErrorCode);
                }

                return IdecResult.Ok("Berhasil menulis async bit ke alamat " + address);
            }
        }

        #endregion

        #region Operasi Integer 16-bit (Int16 / UInt16)

        public virtual IdecResult<short> ReadInt16(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 1);
            if (!res.IsSuccess || res.Content == null || res.Content.Length == 0)
            {
                return IdecResult<short>.Fail(res.Message, res.ErrorCode);
            }
            return IdecResult<short>.Ok((short)res.Content[0]);
        }

        public virtual async Task<IdecResult<short>> ReadInt16Async(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 1).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length == 0)
            {
                return IdecResult<short>.Fail(res.Message, res.ErrorCode);
            }
            return IdecResult<short>.Ok((short)res.Content[0]);
        }

        public virtual IdecResult<ushort> ReadUInt16(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 1);
            if (!res.IsSuccess || res.Content == null || res.Content.Length == 0)
            {
                return IdecResult<ushort>.Fail(res.Message, res.ErrorCode);
            }
            return IdecResult<ushort>.Ok(res.Content[0]);
        }

        public virtual async Task<IdecResult<ushort>> ReadUInt16Async(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 1).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length == 0)
            {
                return IdecResult<ushort>.Fail(res.Message, res.ErrorCode);
            }
            return IdecResult<ushort>.Ok(res.Content[0]);
        }

        public virtual IdecResult WriteInt16(string address, short value)
        {
            return WriteWords(address, new ushort[] { (ushort)value });
        }

        public virtual async Task<IdecResult> WriteInt16Async(string address, short value)
        {
            return await WriteWordsAsync(address, new ushort[] { (ushort)value }).ConfigureAwait(false);
        }

        public virtual IdecResult WriteUInt16(string address, ushort value)
        {
            return WriteWords(address, new ushort[] { value });
        }

        public virtual async Task<IdecResult> WriteUInt16Async(string address, ushort value)
        {
            return await WriteWordsAsync(address, new ushort[] { value }).ConfigureAwait(false);
        }

        #endregion

        #region Operasi Integer 32-bit (Int32 / UInt32)

        public virtual IdecResult<int> ReadInt32(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 2);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 2)
            {
                return IdecResult<int>.Fail(res.Message, res.ErrorCode);
            }
            int val = ConvertWordsToInt32(res.Content[0], res.Content[1], ByteOrder);
            return IdecResult<int>.Ok(val);
        }

        public virtual async Task<IdecResult<int>> ReadInt32Async(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 2).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 2)
            {
                return IdecResult<int>.Fail(res.Message, res.ErrorCode);
            }
            int val = ConvertWordsToInt32(res.Content[0], res.Content[1], ByteOrder);
            return IdecResult<int>.Ok(val);
        }

        public virtual IdecResult<uint> ReadUInt32(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 2);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 2)
            {
                return IdecResult<uint>.Fail(res.Message, res.ErrorCode);
            }
            uint val = (uint)ConvertWordsToInt32(res.Content[0], res.Content[1], ByteOrder);
            return IdecResult<uint>.Ok(val);
        }

        public virtual async Task<IdecResult<uint>> ReadUInt32Async(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 2).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 2)
            {
                return IdecResult<uint>.Fail(res.Message, res.ErrorCode);
            }
            uint val = (uint)ConvertWordsToInt32(res.Content[0], res.Content[1], ByteOrder);
            return IdecResult<uint>.Ok(val);
        }

        public virtual IdecResult WriteInt32(string address, int value)
        {
            ushort[] words = ConvertInt32ToWords(value, ByteOrder);
            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteInt32Async(string address, int value)
        {
            ushort[] words = ConvertInt32ToWords(value, ByteOrder);
            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        public virtual IdecResult WriteUInt32(string address, uint value)
        {
            ushort[] words = ConvertInt32ToWords((int)value, ByteOrder);
            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteUInt32Async(string address, uint value)
        {
            ushort[] words = ConvertInt32ToWords((int)value, ByteOrder);
            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        #endregion

        #region Operasi Integer 64-bit (Int64 / UInt64)

        public virtual IdecResult<long> ReadInt64(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 4);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 4)
            {
                return IdecResult<long>.Fail(res.Message, res.ErrorCode);
            }
            long val = ConvertWordsToInt64(res.Content, ByteOrder);
            return IdecResult<long>.Ok(val);
        }

        public virtual async Task<IdecResult<long>> ReadInt64Async(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 4).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 4)
            {
                return IdecResult<long>.Fail(res.Message, res.ErrorCode);
            }
            long val = ConvertWordsToInt64(res.Content, ByteOrder);
            return IdecResult<long>.Ok(val);
        }

        public virtual IdecResult<ulong> ReadUInt64(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 4);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 4)
            {
                return IdecResult<ulong>.Fail(res.Message, res.ErrorCode);
            }
            ulong val = (ulong)ConvertWordsToInt64(res.Content, ByteOrder);
            return IdecResult<ulong>.Ok(val);
        }

        public virtual async Task<IdecResult<ulong>> ReadUInt64Async(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 4).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 4)
            {
                return IdecResult<ulong>.Fail(res.Message, res.ErrorCode);
            }
            ulong val = (ulong)ConvertWordsToInt64(res.Content, ByteOrder);
            return IdecResult<ulong>.Ok(val);
        }

        public virtual IdecResult WriteInt64(string address, long value)
        {
            ushort[] words = ConvertInt64ToWords(value, ByteOrder);
            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteInt64Async(string address, long value)
        {
            ushort[] words = ConvertInt64ToWords(value, ByteOrder);
            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        public virtual IdecResult WriteUInt64(string address, ulong value)
        {
            ushort[] words = ConvertInt64ToWords((long)value, ByteOrder);
            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteUInt64Async(string address, ulong value)
        {
            ushort[] words = ConvertInt64ToWords((long)value, ByteOrder);
            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        #endregion

        #region Operasi Pecahan Floating-Point (Float / Double)

        public virtual IdecResult<float> ReadFloat(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 2);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 2)
            {
                return IdecResult<float>.Fail(res.Message, res.ErrorCode);
            }
            byte[] bytes = ReorderBytesFromWords(res.Content[0], res.Content[1], ByteOrder);
            float val = BitConverter.ToSingle(bytes, 0);
            return IdecResult<float>.Ok(val);
        }

        public virtual async Task<IdecResult<float>> ReadFloatAsync(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 2).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 2)
            {
                return IdecResult<float>.Fail(res.Message, res.ErrorCode);
            }
            byte[] bytes = ReorderBytesFromWords(res.Content[0], res.Content[1], ByteOrder);
            float val = BitConverter.ToSingle(bytes, 0);
            return IdecResult<float>.Ok(val);
        }

        public virtual IdecResult WriteFloat(string address, float value)
        {
            byte[] raw = BitConverter.GetBytes(value);
            ushort[] words = ReorderWordsFromBytes(raw, ByteOrder);
            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteFloatAsync(string address, float value)
        {
            byte[] raw = BitConverter.GetBytes(value);
            ushort[] words = ReorderWordsFromBytes(raw, ByteOrder);
            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        public virtual IdecResult<double> ReadDouble(string address)
        {
            IdecResult<ushort[]> res = ReadWords(address, 4);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 4)
            {
                return IdecResult<double>.Fail(res.Message, res.ErrorCode);
            }
            byte[] bytes = ReorderBytesFrom4Words(res.Content, ByteOrder);
            double val = BitConverter.ToDouble(bytes, 0);
            return IdecResult<double>.Ok(val);
        }

        public virtual async Task<IdecResult<double>> ReadDoubleAsync(string address)
        {
            IdecResult<ushort[]> res = await ReadWordsAsync(address, 4).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null || res.Content.Length < 4)
            {
                return IdecResult<double>.Fail(res.Message, res.ErrorCode);
            }
            byte[] bytes = ReorderBytesFrom4Words(res.Content, ByteOrder);
            double val = BitConverter.ToDouble(bytes, 0);
            return IdecResult<double>.Ok(val);
        }

        public virtual IdecResult WriteDouble(string address, double value)
        {
            byte[] raw = BitConverter.GetBytes(value);
            ushort[] words = Reorder4WordsFromBytes(raw, ByteOrder);
            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteDoubleAsync(string address, double value)
        {
            byte[] raw = BitConverter.GetBytes(value);
            ushort[] words = Reorder4WordsFromBytes(raw, ByteOrder);
            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        #endregion

        #region Operasi String & Raw Bytes

        /// <summary>
        /// Membaca string teks ASCII dari register PLC.
        /// </summary>
        public virtual IdecResult<string> ReadString(string address, ushort length)
        {
            if (length == 0)
            {
                return IdecResult<string>.Ok(string.Empty);
            }

            // Tiap word register menampung 2 karakter ASCII
            int wordCount = (length + 1) / 2;
            IdecResult<ushort[]> res = ReadWords(address, (ushort)wordCount);

            if (!res.IsSuccess || res.Content == null)
            {
                return IdecResult<string>.Fail(res.Message, res.ErrorCode);
            }

            byte[] rawBytes = new byte[res.Content.Length * 2];
            for (int i = 0; i < res.Content.Length; i++)
            {
                ushort w = res.Content[i];
                // Urutan standar Modbus/IDEC string: High byte dulu lalu Low byte
                rawBytes[i * 2] = (byte)(w >> 8);
                rawBytes[i * 2 + 1] = (byte)(w & 0xFF);
            }

            string rawText = Encoding.ASCII.GetString(rawBytes, 0, Math.Min(length, rawBytes.Length));

            // Truncate pada karakter null pertama (\0) agar tidak membawa sisa buffer PLC
            int nullIndex = rawText.IndexOf('\0');
            if (nullIndex >= 0)
            {
                rawText = rawText.Substring(0, nullIndex);
            }

            return IdecResult<string>.Ok(rawText.TrimEnd(' '));
        }

        /// <summary>
        /// Membaca string teks ASCII secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult<string>> ReadStringAsync(string address, ushort length)
        {
            if (length == 0)
            {
                return IdecResult<string>.Ok(string.Empty);
            }

            int wordCount = (length + 1) / 2;
            IdecResult<ushort[]> res = await ReadWordsAsync(address, (ushort)wordCount).ConfigureAwait(false);

            if (!res.IsSuccess || res.Content == null)
            {
                return IdecResult<string>.Fail(res.Message, res.ErrorCode);
            }

            byte[] rawBytes = new byte[res.Content.Length * 2];
            for (int i = 0; i < res.Content.Length; i++)
            {
                ushort w = res.Content[i];
                rawBytes[i * 2] = (byte)(w >> 8);
                rawBytes[i * 2 + 1] = (byte)(w & 0xFF);
            }

            string rawText = Encoding.ASCII.GetString(rawBytes, 0, Math.Min(length, rawBytes.Length));

            int nullIndex = rawText.IndexOf('\0');
            if (nullIndex >= 0)
            {
                rawText = rawText.Substring(0, nullIndex);
            }

            return IdecResult<string>.Ok(rawText.TrimEnd(' '));
        }

        /// <summary>
        /// Menulis teks string ASCII ke register PLC.
        /// Jika string bernilai null atau kosong, menulis 1 register bernilai 0x0000 sebagai null-terminator.
        /// </summary>
        public virtual IdecResult WriteString(string address, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return WriteWords(address, new ushort[] { 0x0000 });
            }

            byte[] asciiBytes = Encoding.ASCII.GetBytes(value);
            // Tambahkan 1 byte null-terminator di akhir string jika ganjil atau untuk keamanan
            int paddedLength = asciiBytes.Length;
            if (paddedLength % 2 != 0)
            {
                paddedLength = paddedLength + 1;
            }

            byte[] buffer = new byte[paddedLength];
            Array.Copy(asciiBytes, 0, buffer, 0, asciiBytes.Length);

            ushort[] words = new ushort[paddedLength / 2];
            for (int i = 0; i < words.Length; i++)
            {
                byte hi = buffer[i * 2];
                byte lo = buffer[i * 2 + 1];
                words[i] = (ushort)((hi << 8) | lo);
            }

            return WriteWords(address, words);
        }

        /// <summary>
        /// Menulis teks string ASCII secara asynchronous.
        /// </summary>
        public virtual async Task<IdecResult> WriteStringAsync(string address, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return await WriteWordsAsync(address, new ushort[] { 0x0000 }).ConfigureAwait(false);
            }

            byte[] asciiBytes = Encoding.ASCII.GetBytes(value);
            int paddedLength = asciiBytes.Length;
            if (paddedLength % 2 != 0)
            {
                paddedLength = paddedLength + 1;
            }

            byte[] buffer = new byte[paddedLength];
            Array.Copy(asciiBytes, 0, buffer, 0, asciiBytes.Length);

            ushort[] words = new ushort[paddedLength / 2];
            for (int i = 0; i < words.Length; i++)
            {
                byte hi = buffer[i * 2];
                byte lo = buffer[i * 2 + 1];
                words[i] = (ushort)((hi << 8) | lo);
            }

            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        public virtual IdecResult<byte[]> ReadBytes(string address, ushort byteCount)
        {
            int wordCount = (byteCount + 1) / 2;
            IdecResult<ushort[]> res = ReadWords(address, (ushort)wordCount);
            if (!res.IsSuccess || res.Content == null)
            {
                return IdecResult<byte[]>.Fail(res.Message, res.ErrorCode);
            }

            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                int wordIdx = i / 2;
                int byteIdx = i % 2;
                if (byteIdx == 0)
                {
                    bytes[i] = (byte)(res.Content[wordIdx] >> 8);
                }
                else
                {
                    bytes[i] = (byte)(res.Content[wordIdx] & 0xFF);
                }
            }
            return IdecResult<byte[]>.Ok(bytes);
        }

        public virtual async Task<IdecResult<byte[]>> ReadBytesAsync(string address, ushort byteCount)
        {
            int wordCount = (byteCount + 1) / 2;
            IdecResult<ushort[]> res = await ReadWordsAsync(address, (ushort)wordCount).ConfigureAwait(false);
            if (!res.IsSuccess || res.Content == null)
            {
                return IdecResult<byte[]>.Fail(res.Message, res.ErrorCode);
            }

            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                int wordIdx = i / 2;
                int byteIdx = i % 2;
                if (byteIdx == 0)
                {
                    bytes[i] = (byte)(res.Content[wordIdx] >> 8);
                }
                else
                {
                    bytes[i] = (byte)(res.Content[wordIdx] & 0xFF);
                }
            }
            return IdecResult<byte[]>.Ok(bytes);
        }

        public virtual IdecResult WriteBytes(string address, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return IdecResult.Fail("Array bytes yang akan ditulis tidak boleh kosong.", -501);
            }

            int wordCount = (bytes.Length + 1) / 2;
            ushort[] words = new ushort[wordCount];

            for (int i = 0; i < wordCount; i++)
            {
                byte hi = bytes[i * 2];
                byte lo = 0;
                if ((i * 2 + 1) < bytes.Length)
                {
                    lo = bytes[i * 2 + 1];
                }
                words[i] = (ushort)((hi << 8) | lo);
            }

            return WriteWords(address, words);
        }

        public virtual async Task<IdecResult> WriteBytesAsync(string address, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return IdecResult.Fail("Array bytes yang akan ditulis tidak boleh kosong.", -501);
            }

            int wordCount = (bytes.Length + 1) / 2;
            ushort[] words = new ushort[wordCount];

            for (int i = 0; i < wordCount; i++)
            {
                byte hi = bytes[i * 2];
                byte lo = 0;
                if ((i * 2 + 1) < bytes.Length)
                {
                    lo = bytes[i * 2 + 1];
                }
                words[i] = (ushort)((hi << 8) | lo);
            }

            return await WriteWordsAsync(address, words).ConfigureAwait(false);
        }

        #endregion

        #region Helper Konversi Endianness & Format Data

        private static int ConvertWordsToInt32(ushort word1, ushort word2, EndianFormat format)
        {
            byte[] bytes = ReorderBytesFromWords(word1, word2, format);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static ushort[] ConvertInt32ToWords(int value, EndianFormat format)
        {
            byte[] raw = BitConverter.GetBytes(value);
            return ReorderWordsFromBytes(raw, format);
        }

        private static byte[] ReorderBytesFromWords(ushort word1, ushort word2, EndianFormat format)
        {
            byte bA = (byte)(word1 >> 8);
            byte bB = (byte)(word1 & 0xFF);
            byte bC = (byte)(word2 >> 8);
            byte bD = (byte)(word2 & 0xFF);

            // Susun ulang byte sesuai EndianFormat target untuk sistem x86/ARM (BitConverter Little-Endian)
            // BitConverter.ToInt32 membaca urutan byte [0]=terendah, [3]=tertinggi
            byte[] result = new byte[4];

            if (format == EndianFormat.ABCD)
            {
                // ABCD: A paling signifikan, D paling kecil
                result[0] = bD;
                result[1] = bC;
                result[2] = bB;
                result[3] = bA;
            }
            else if (format == EndianFormat.BADC)
            {
                result[0] = bC;
                result[1] = bD;
                result[2] = bA;
                result[3] = bB;
            }
            else if (format == EndianFormat.CDAB)
            {
                // CDAB: C, D, A, B
                result[0] = bB;
                result[1] = bA;
                result[2] = bD;
                result[3] = bC;
            }
            else
            {
                // DCBA
                result[0] = bA;
                result[1] = bB;
                result[2] = bC;
                result[3] = bD;
            }

            return result;
        }

        private static ushort[] ReorderWordsFromBytes(byte[] raw, EndianFormat format)
        {
            // raw berasal dari BitConverter.GetBytes(int): [0]=terendah, [3]=tertinggi
            byte bD = raw[0];
            byte bC = raw[1];
            byte bB = raw[2];
            byte bA = raw[3];

            ushort w1 = 0;
            ushort w2 = 0;

            if (format == EndianFormat.ABCD)
            {
                w1 = (ushort)((bA << 8) | bB);
                w2 = (ushort)((bC << 8) | bD);
            }
            else if (format == EndianFormat.BADC)
            {
                w1 = (ushort)((bB << 8) | bA);
                w2 = (ushort)((bD << 8) | bC);
            }
            else if (format == EndianFormat.CDAB)
            {
                w1 = (ushort)((bC << 8) | bD);
                w2 = (ushort)((bA << 8) | bB);
            }
            else
            {
                // DCBA
                w1 = (ushort)((bD << 8) | bC);
                w2 = (ushort)((bB << 8) | bA);
            }

            return new ushort[] { w1, w2 };
        }

        private static long ConvertWordsToInt64(ushort[] words, EndianFormat format)
        {
            byte[] bytes = ReorderBytesFrom4Words(words, format);
            return BitConverter.ToInt64(bytes, 0);
        }

        private static ushort[] ConvertInt64ToWords(long value, EndianFormat format)
        {
            byte[] raw = BitConverter.GetBytes(value);
            return Reorder4WordsFromBytes(raw, format);
        }

        private static byte[] ReorderBytesFrom4Words(ushort[] words, EndianFormat format)
        {
            byte[] result = new byte[8];
            byte[] temp = new byte[8];

            for (int i = 0; i < 4; i++)
            {
                temp[i * 2] = (byte)(words[i] >> 8);
                temp[i * 2 + 1] = (byte)(words[i] & 0xFF);
            }

            if (format == EndianFormat.ABCD)
            {
                for (int i = 0; i < 8; i++)
                {
                    result[i] = temp[7 - i];
                }
            }
            else if (format == EndianFormat.CDAB)
            {
                // Tukar pasangan word
                result[0] = temp[7];
                result[1] = temp[6];
                result[2] = temp[5];
                result[3] = temp[4];
                result[4] = temp[3];
                result[5] = temp[2];
                result[6] = temp[1];
                result[7] = temp[0];
            }
            else
            {
                Array.Copy(temp, 0, result, 0, 8);
            }

            return result;
        }

        private static ushort[] Reorder4WordsFromBytes(byte[] raw, EndianFormat format)
        {
            ushort[] words = new ushort[4];
            if (format == EndianFormat.ABCD)
            {
                for (int i = 0; i < 4; i++)
                {
                    byte hi = raw[7 - (i * 2)];
                    byte lo = raw[7 - (i * 2 + 1)];
                    words[i] = (ushort)((hi << 8) | lo);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    byte lo = raw[i * 2];
                    byte hi = raw[i * 2 + 1];
                    words[i] = (ushort)((hi << 8) | lo);
                }
            }
            return words;
        }

        #endregion
    }
}