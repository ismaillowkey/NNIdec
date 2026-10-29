using System;
using System.Threading.Tasks;
using NNIdec.Core;

namespace NNIdec.Interfaces
{
    /// <summary>
    /// Antarmuka standar klien komunikasi PLC IDEC.
    /// Menyediakan operasi baca dan tulis data dengan Result Pattern yang bersih tanpa akhiran V2.
    /// </summary>
    public interface IIdecClient : IDisposable
    {
        /// <summary>
        /// Model PLC IDEC yang sedang aktif.
        /// </summary>
        IdecPlcModel Model { get; }

        /// <summary>
        /// Format endianness yang digunakan untuk konversi tipe data 32-bit dan 64-bit.
        /// </summary>
        EndianFormat ByteOrder { get; set; }

        /// <summary>
        /// Status koneksi saat ini ke PLC IDEC.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Mengaktifkan sambung ulang otomatis saat koneksi jaringan ke PLC terputus (Default true).
        /// </summary>
        bool AutoReconnect { get; set; }

        /// <summary>
        /// Membuka koneksi ke PLC secara synchronous.
        /// </summary>
        IdecResult Connect();

        /// <summary>
        /// Membuka koneksi ke PLC secara asynchronous.
        /// </summary>
        Task<IdecResult> ConnectAsync();

        /// <summary>
        /// Menutup koneksi ke PLC.
        /// </summary>
        void Disconnect();

        #region Operasi Baca (Read)

        /// <summary>
        /// Membaca nilai boolean / bit (misal I0, Q0, M10, D100.5).
        /// </summary>
        IdecResult<bool> ReadBool(string address);
        Task<IdecResult<bool>> ReadBoolAsync(string address);

        /// <summary>
        /// Membaca nilai integer 16-bit bertanda (short) dari register (misal D100, TC1).
        /// </summary>
        IdecResult<short> ReadInt16(string address);
        Task<IdecResult<short>> ReadInt16Async(string address);

        /// <summary>
        /// Membaca nilai integer 16-bit tanpa tanda (ushort) dari register.
        /// </summary>
        IdecResult<ushort> ReadUInt16(string address);
        Task<IdecResult<ushort>> ReadUInt16Async(string address);

        /// <summary>
        /// Membaca nilai integer 32-bit bertanda (int) dari 2 register berurutan.
        /// </summary>
        IdecResult<int> ReadInt32(string address);
        Task<IdecResult<int>> ReadInt32Async(string address);

        /// <summary>
        /// Membaca nilai integer 32-bit tanpa tanda (uint) dari 2 register berurutan.
        /// </summary>
        IdecResult<uint> ReadUInt32(string address);
        Task<IdecResult<uint>> ReadUInt32Async(string address);

        /// <summary>
        /// Membaca nilai integer 64-bit bertanda (long) dari 4 register berurutan.
        /// </summary>
        IdecResult<long> ReadInt64(string address);
        Task<IdecResult<long>> ReadInt64Async(string address);

        /// <summary>
        /// Membaca nilai integer 64-bit tanpa tanda (ulong) dari 4 register berurutan.
        /// </summary>
        IdecResult<ulong> ReadUInt64(string address);
        Task<IdecResult<ulong>> ReadUInt64Async(string address);

        /// <summary>
        /// Membaca nilai bilangan desimal pecahan 32-bit (float) dari 2 register berurutan.
        /// </summary>
        IdecResult<float> ReadFloat(string address);
        Task<IdecResult<float>> ReadFloatAsync(string address);

        /// <summary>
        /// Membaca nilai bilangan desimal presisi ganda 64-bit (double) dari 4 register berurutan.
        /// </summary>
        IdecResult<double> ReadDouble(string address);
        Task<IdecResult<double>> ReadDoubleAsync(string address);

        /// <summary>
        /// Membaca teks string ASCII dari serangkaian register.
        /// </summary>
        IdecResult<string> ReadString(string address, ushort length);
        Task<IdecResult<string>> ReadStringAsync(string address, ushort length);

        /// <summary>
        /// Membaca array word (ushort[]) mentah dari register PLC.
        /// </summary>
        IdecResult<ushort[]> ReadWords(string address, ushort count);
        Task<IdecResult<ushort[]>> ReadWordsAsync(string address, ushort count);

        /// <summary>
        /// Membaca array byte mentah dari register PLC.
        /// </summary>
        IdecResult<byte[]> ReadBytes(string address, ushort byteCount);
        Task<IdecResult<byte[]>> ReadBytesAsync(string address, ushort byteCount);

        #endregion

        #region Operasi Tulis (Write)

        /// <summary>
        /// Menulis nilai boolean / bit ke PLC (misal Q0, M10, D100.5).
        /// </summary>
        IdecResult WriteBool(string address, bool value);
        Task<IdecResult> WriteBoolAsync(string address, bool value);

        /// <summary>
        /// Menulis nilai integer 16-bit bertanda (short) ke register.
        /// </summary>
        IdecResult WriteInt16(string address, short value);
        Task<IdecResult> WriteInt16Async(string address, short value);

        /// <summary>
        /// Menulis nilai integer 16-bit tanpa tanda (ushort) ke register.
        /// </summary>
        IdecResult WriteUInt16(string address, ushort value);
        Task<IdecResult> WriteUInt16Async(string address, ushort value);

        /// <summary>
        /// Menulis nilai integer 32-bit bertanda (int) ke 2 register berurutan.
        /// </summary>
        IdecResult WriteInt32(string address, int value);
        Task<IdecResult> WriteInt32Async(string address, int value);

        /// <summary>
        /// Menulis nilai integer 32-bit tanpa tanda (uint) ke 2 register berurutan.
        /// </summary>
        IdecResult WriteUInt32(string address, uint value);
        Task<IdecResult> WriteUInt32Async(string address, uint value);

        /// <summary>
        /// Menulis nilai integer 64-bit bertanda (long) ke 4 register berurutan.
        /// </summary>
        IdecResult WriteInt64(string address, long value);
        Task<IdecResult> WriteInt64Async(string address, long value);

        /// <summary>
        /// Menulis nilai integer 64-bit tanpa tanda (ulong) ke 4 register berurutan.
        /// </summary>
        IdecResult WriteUInt64(string address, ulong value);
        Task<IdecResult> WriteUInt64Async(string address, ulong value);

        /// <summary>
        /// Menulis nilai bilangan desimal pecahan 32-bit (float) ke 2 register berurutan.
        /// </summary>
        IdecResult WriteFloat(string address, float value);
        Task<IdecResult> WriteFloatAsync(string address, float value);

        /// <summary>
        /// Menulis nilai bilangan desimal presisi ganda 64-bit (double) ke 4 register berurutan.
        /// </summary>
        IdecResult WriteDouble(string address, double value);
        Task<IdecResult> WriteDoubleAsync(string address, double value);

        /// <summary>
        /// Menulis teks string ASCII ke dalam register PLC.
        /// </summary>
        IdecResult WriteString(string address, string value);
        Task<IdecResult> WriteStringAsync(string address, string value);

        /// <summary>
        /// Menulis array word (ushort[]) ke serangkaian register.
        /// </summary>
        IdecResult WriteWords(string address, ushort[] values);
        Task<IdecResult> WriteWordsAsync(string address, ushort[] values);

        /// <summary>
        /// Menulis array byte mentah ke dalam register PLC.
        /// </summary>
        IdecResult WriteBytes(string address, byte[] bytes);
        Task<IdecResult> WriteBytesAsync(string address, byte[] bytes);

        #endregion
    }
}