using System;
using System.Globalization;
using System.Text;
using NNIdec.Core;

namespace NNIdec.Protocol
{
    /// <summary>
    /// Pembentuk dan pemroses frame protokol IDEC Computer Link / Maintenance ASCII (Port 2101).
    /// Format frame Request: &lt;ENQ&gt; + Device/Station(2) + Mode/Cmd(2) + DeviceType(1) + Address(4) + ByteCount(2) + [Data] + BCC(2) + &lt;CR&gt;
    /// Format frame Reply  : &lt;ACK&gt; + Device/Station(2) + Status(1) + [DataHex] + BCC(2) + &lt;CR&gt;
    /// </summary>
    public static class IdecMaintenanceProtocol
    {
        public const byte ENQ = 0x05; // Enquiry
        public const byte STX = 0x02; // Start of Text
        public const byte ETX = 0x03; // End of Text
        public const byte ACK = 0x06; // Acknowledge
        public const byte NAK = 0x15; // Negative Acknowledge
        public const byte CR  = 0x0D; // Carriage Return

        /// <summary>
        /// Menghitung BCC (Block Check Character) untuk paket IDEC ASCII.
        /// Sesuai spesifikasi IDEC Maintenance Protocol, BCC dihitung dari operasi XOR (Exclusive OR)
        /// terhadap seluruh byte payload (termasuk karakter pembuka ENQ) lalu diformat menjadi 2 karakter Hexadecimal.
        /// </summary>
        public static string CalculateBcc(byte[] buffer, int offset, int length)
        {
            int bcc = 0;
            for (int i = 0; i < length; i++)
            {
                bcc = bcc ^ buffer[offset + i];
            }
            int bccByte = bcc & 0xFF;
            return bccByte.ToString("X2");
        }

        /// <summary>
        /// Membuat paket pesan baca register/word untuk protokol Maintenance IDEC.
        /// </summary>
        public static byte[] BuildReadWordFrame(byte station, IdecAddress address, ushort wordCount)
        {
            // Tentukan representasi alamat station
            string devStr;
            if (station == 0)
            {
                // Gunakan "FF" sebagai default 1:1 direct connection jika station adalah 0
                devStr = "FF";
            }
            else
            {
                devStr = station.ToString("X2");
            }

            // Jumlah byte yang dibaca (1 word = 2 byte)
            int byteCount = wordCount * 2;

            StringBuilder sb = new StringBuilder();
            sb.Append((char)ENQ);
            sb.Append(devStr);
            sb.Append("0R"); // Perintah Baca Word
            sb.Append(address.ProtocolCode);
            sb.Append(address.AddressNumber.ToString("D4"));
            sb.Append(byteCount.ToString("X2"));

            byte[] msgBytes = Encoding.ASCII.GetBytes(sb.ToString());
            string bcc = CalculateBcc(msgBytes, 0, msgBytes.Length);
            byte[] bccBytes = Encoding.ASCII.GetBytes(bcc);

            // Total frame: msgBytes.Length + 2 (BCC) + 1 (CR)
            byte[] frame = new byte[msgBytes.Length + 2 + 1];
            Array.Copy(msgBytes, 0, frame, 0, msgBytes.Length);
            Array.Copy(bccBytes, 0, frame, msgBytes.Length, 2);
            frame[frame.Length - 1] = CR;

            return frame;
        }

        /// <summary>
        /// Membuat paket pesan baca bit/relay untuk protokol Maintenance IDEC.
        /// </summary>
        public static byte[] BuildReadBitFrame(byte station, IdecAddress address, ushort bitCount)
        {
            string devStr;
            if (station == 0)
            {
                devStr = "FF";
            }
            else
            {
                devStr = station.ToString("X2");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append((char)ENQ);
            sb.Append(devStr);
            sb.Append("0R"); // Perintah Baca Bit
            sb.Append(address.ProtocolCode);
            sb.Append(address.AddressNumber.ToString("D4"));
            sb.Append(bitCount.ToString("X2"));

            byte[] msgBytes = Encoding.ASCII.GetBytes(sb.ToString());
            string bcc = CalculateBcc(msgBytes, 0, msgBytes.Length);
            byte[] bccBytes = Encoding.ASCII.GetBytes(bcc);

            byte[] frame = new byte[msgBytes.Length + 2 + 1];
            Array.Copy(msgBytes, 0, frame, 0, msgBytes.Length);
            Array.Copy(bccBytes, 0, frame, msgBytes.Length, 2);
            frame[frame.Length - 1] = CR;

            return frame;
        }

        /// <summary>
        /// Membuat paket pesan tulis satu atau beberapa register/word untuk protokol Maintenance IDEC.
        /// </summary>
        public static byte[] BuildWriteWordFrame(byte station, IdecAddress address, ushort[] values)
        {
            string devStr;
            if (station == 0)
            {
                devStr = "FF";
            }
            else
            {
                devStr = station.ToString("X2");
            }

            int byteCount = values.Length * 2;

            StringBuilder sb = new StringBuilder();
            sb.Append((char)ENQ);
            sb.Append(devStr);
            sb.Append("0W"); // Perintah Tulis Word
            sb.Append(address.ProtocolCode);
            sb.Append(address.AddressNumber.ToString("D4"));
            sb.Append(byteCount.ToString("X2"));

            // Data word ditulis dalam format 4-digit Hex per word
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString("X4"));
            }

            byte[] msgBytes = Encoding.ASCII.GetBytes(sb.ToString());
            string bcc = CalculateBcc(msgBytes, 0, msgBytes.Length);
            byte[] bccBytes = Encoding.ASCII.GetBytes(bcc);

            byte[] frame = new byte[msgBytes.Length + 2 + 1];
            Array.Copy(msgBytes, 0, frame, 0, msgBytes.Length);
            Array.Copy(bccBytes, 0, frame, msgBytes.Length, 2);
            frame[frame.Length - 1] = CR;

            return frame;
        }

        /// <summary>
        /// Membuat paket pesan tulis bit/relay untuk protokol Maintenance IDEC.
        /// </summary>
        public static byte[] BuildWriteBitFrame(byte station, IdecAddress address, bool value)
        {
            string devStr;
            if (station == 0)
            {
                devStr = "FF";
            }
            else
            {
                devStr = station.ToString("X2");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append((char)ENQ);
            sb.Append(devStr);
            sb.Append("0W"); // Perintah Tulis Bit
            sb.Append(address.ProtocolCode);
            sb.Append(address.AddressNumber.ToString("D4"));
            sb.Append("01"); // 1 bit

            if (value)
            {
                sb.Append("1");
            }
            else
            {
                sb.Append("0");
            }

            byte[] msgBytes = Encoding.ASCII.GetBytes(sb.ToString());
            string bcc = CalculateBcc(msgBytes, 0, msgBytes.Length);
            byte[] bccBytes = Encoding.ASCII.GetBytes(bcc);

            byte[] frame = new byte[msgBytes.Length + 2 + 1];
            Array.Copy(msgBytes, 0, frame, 0, msgBytes.Length);
            Array.Copy(bccBytes, 0, frame, msgBytes.Length, 2);
            frame[frame.Length - 1] = CR;

            return frame;
        }

        /// <summary>
        /// Validasi dan ekstraksi respon data word dari frame balasan PLC.
        /// Format respon normal: &lt;ACK&gt;(0x06) + Station(2) + Status(1) + DataHex(4 * count) + BCC(2) + &lt;CR&gt;(0x0D)
        /// </summary>
        public static IdecResult<ushort[]> ParseReadWordResponse(byte[] response, int length, ushort expectedCount)
        {
            if (response == null || length < 4)
            {
                return IdecResult<ushort[]>.Fail("Respon dari PLC IDEC kosong atau terlalu pendek.", -1);
            }

            // Periksa jika ada balasan NAK (0x15)
            if (response[0] == NAK)
            {
                string errCode = "Unknown";
                if (length >= 3)
                {
                    errCode = Encoding.ASCII.GetString(response, 1, 2);
                }
                return IdecResult<ushort[]>.Fail("PLC IDEC merespon dengan NAK. Kode error: " + errCode, -2);
            }

            // Tentukan offset awal payload data
            int dataStart;
            if (response[0] == ACK)
            {
                // Format FC6A Maintenance: <ACK>(1) + Station(2) + Status(1) + DataHex + BCC(2) + <CR>(1)
                dataStart = 4;
            }
            else if (response[0] == STX)
            {
                // Format Computer Link: <STX>(1) + Station(2) + Status(2) + DataHex + <ETX>(1) + BCC(2)
                dataStart = 5;
            }
            else
            {
                dataStart = 4;
            }

            // Tiap word direpresentasikan oleh 4 karakter ASCII Hexadecimal
            int neededChars = expectedCount * 4;
            if (length - dataStart < neededChars)
            {
                return IdecResult<ushort[]>.Fail("Panjang data balasan tidak mencukupi untuk " + expectedCount + " word.", -3);
            }

            ushort[] words = new ushort[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                int charIdx = dataStart + (i * 4);
                string hexWord = Encoding.ASCII.GetString(response, charIdx, 4);
                ushort val;
                if (!ushort.TryParse(hexWord, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                {
                    return IdecResult<ushort[]>.Fail("Gagal mengurai nilai hex word pada indeks " + i + ": " + hexWord, -4);
                }
                words[i] = val;
            }

            return IdecResult<ushort[]>.Ok(words);
        }
    }
}