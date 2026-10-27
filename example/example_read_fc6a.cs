using System;
using System.Threading;
using System.Threading.Tasks;
using NNIdec;
using NNIdec.Core;

namespace NNIdec.Example
{
    /// <summary>
    /// Contoh program pembacaan berbagai tipe alamat register dan relay pada PLC IDEC MicroSmart FC6A (loop 5x setiap 1 detik).
    /// Menyediakan metode Synchronous (tanpa Async) dan metode Asynchronous.
    /// </summary>
    public class ExampleRead
    {
        public static void Main(string[] args)
        {
            // Menjalankan metode pembacaan synchronous tanpa Async
            Console.WriteLine("Menjalankan metode Synchronous (tanpa Async)...\n");
            RunSynchronous();

            // Catatan: Jika ingin menjalankan metode Asynchronous, gunakan perintah berikut:
            // RunAsynchronous().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Metode pembacaan seluruh alamat data PLC IDEC FC6A secara Synchronous (tanpa async / await).
        /// Menggunakan Connect(), ReadInt16(), ReadBool(), dan Thread.Sleep().
        /// </summary>
        public static void RunSynchronous()
        {
            // Konfigurasi IP PLC IDEC FC6A dan port Maintenance (2101)
            string ipAddress = "192.168.2.36";
            int port = 2101;
            // Station ID default PLC IDEC di WindLDR adalah 0 (atau 255 untuk broadcast 1:1)
            byte stationId = 0;

            // Batas perulangan disetel tepat 5 kali perulangan
            int maxIterations = 5;

            Console.WriteLine($"Mencoba menghubungkan ke PLC IDEC FC6A secara Synchronous ({ipAddress}:{port})...");

            // Inisialisasi klien komunikasi khusus IDEC MicroSmart FC6A (tanpa blok using)
            IdecMicroSmartFC6A plc = new IdecMicroSmartFC6A(ipAddress, port, stationId);

            // Membuka koneksi ke PLC secara synchronous
            IdecResult connectResult = plc.Connect();

            // Evaluasi status koneksi secara eksplisit
            if (!connectResult.IsSuccess)
            {
                Console.WriteLine($"Koneksi gagal: {connectResult.Message} (Error Code: {connectResult.ErrorCode})");
                return;
            }

            Console.WriteLine("Koneksi berhasil terhubung ke PLC IDEC FC6A (Synchronous).");
            Console.WriteLine($"Memulai loop pembacaan seluruh alamat data setiap 1 detik ({maxIterations}x perulangan)...\n");

            for (int count = 1; count <= maxIterations; count++)
            {
                Console.WriteLine($"=== Pembacaan Synchronous Ke-{count} dari {maxIterations} ({DateTime.Now:HH:mm:ss}) ===");

                // 1. Membaca Data Register D0 (Word / 16-bit)
                IdecResult<short> resD0 = plc.ReadInt16("D0");
                if (resD0.IsSuccess)
                {
                    Console.WriteLine($"[D0]   Nilai : {resD0.Content}");
                }
                else
                {
                    Console.WriteLine($"[D0]   Gagal : {resD0.Message} (Error: {resD0.ErrorCode})");
                }

                // 2. Membaca Input Relay I0.0 (Bit)
                IdecResult<bool> resI0 = plc.ReadBool("I0.0");
                if (resI0.IsSuccess)
                {
                    Console.WriteLine($"[I0.0] Nilai : {resI0.Content}");
                }
                else
                {
                    Console.WriteLine($"[I0.0] Gagal : {resI0.Message} (Error: {resI0.ErrorCode})");
                }

                // 3. Membaca Output Relay Q0.0 (Bit)
                IdecResult<bool> resQ0 = plc.ReadBool("Q0.0");
                if (resQ0.IsSuccess)
                {
                    Console.WriteLine($"[Q0.0] Nilai : {resQ0.Content}");
                }
                else
                {
                    Console.WriteLine($"[Q0.0] Gagal : {resQ0.Message} (Error: {resQ0.ErrorCode})");
                }

                // 4. Membaca Internal Relay M0 (Bit)
                IdecResult<bool> resM0 = plc.ReadBool("M0");
                if (resM0.IsSuccess)
                {
                    Console.WriteLine($"[M0]   Nilai : {resM0.Content}");
                }
                else
                {
                    Console.WriteLine($"[M0]   Gagal : {resM0.Message} (Error: {resM0.ErrorCode})");
                }

                // 5. Membaca Shift Register R0 (Bit)
                IdecResult<bool> resR0 = plc.ReadBool("R0");
                if (resR0.IsSuccess)
                {
                    Console.WriteLine($"[R0]   Nilai : {resR0.Content}");
                }
                else
                {
                    Console.WriteLine($"[R0]   Gagal : {resR0.Message} (Error: {resR0.ErrorCode})");
                }

                // 6. Membaca Kontak Timer T0 (Bit)
                IdecResult<bool> resT0 = plc.ReadBool("T0");
                if (resT0.IsSuccess)
                {
                    Console.WriteLine($"[T0]   Nilai : {resT0.Content}");
                }
                else
                {
                    Console.WriteLine($"[T0]   Gagal : {resT0.Message} (Error: {resT0.ErrorCode})");
                }

                // 7. Membaca Timer Current TC0 (Word / 16-bit)
                IdecResult<short> resTC0 = plc.ReadInt16("TC0");
                if (resTC0.IsSuccess)
                {
                    Console.WriteLine($"[TC0]  Nilai : {resTC0.Content}");
                }
                else
                {
                    Console.WriteLine($"[TC0]  Gagal : {resTC0.Message} (Error: {resTC0.ErrorCode})");
                }

                // 8. Membaca Timer Preset TP0 (Word / 16-bit)
                IdecResult<short> resTP0 = plc.ReadInt16("TP0");
                if (resTP0.IsSuccess)
                {
                    Console.WriteLine($"[TP0]  Nilai : {resTP0.Content}");
                }
                else
                {
                    Console.WriteLine($"[TP0]  Gagal : {resTP0.Message} (Error: {resTP0.ErrorCode})");
                }

                // 9. Membaca Kontak Counter C0 (Bit)
                IdecResult<bool> resC0 = plc.ReadBool("C0");
                if (resC0.IsSuccess)
                {
                    Console.WriteLine($"[C0]   Nilai : {resC0.Content}");
                }
                else
                {
                    Console.WriteLine($"[C0]   Gagal : {resC0.Message} (Error: {resC0.ErrorCode})");
                }

                // 10. Membaca Counter Current CC0 (Word / 16-bit)
                IdecResult<short> resCC0 = plc.ReadInt16("CC0");
                if (resCC0.IsSuccess)
                {
                    Console.WriteLine($"[CC0]  Nilai : {resCC0.Content}");
                }
                else
                {
                    Console.WriteLine($"[CC0]  Gagal : {resCC0.Message} (Error: {resCC0.ErrorCode})");
                }

                // 11. Membaca Counter Preset CP0 (Word / 16-bit)
                IdecResult<short> resCP0 = plc.ReadInt16("CP0");
                if (resCP0.IsSuccess)
                {
                    Console.WriteLine($"[CP0]  Nilai : {resCP0.Content}");
                }
                else
                {
                    Console.WriteLine($"[CP0]  Gagal : {resCP0.Message} (Error: {resCP0.ErrorCode})");
                }

                // 12. Membaca Bit di dalam Register D0.0 (Bit)
                IdecResult<bool> resD0Bit = plc.ReadBool("D0.0");
                if (resD0Bit.IsSuccess)
                {
                    Console.WriteLine($"[D0.0] Nilai : {resD0Bit.Content}");
                }
                else
                {
                    Console.WriteLine($"[D0.0] Gagal : {resD0Bit.Message} (Error: {resD0Bit.ErrorCode})");
                }

                Console.WriteLine();

                // Berikan jeda 1 detik jika belum mencapai iterasi terakhir
                if (count < maxIterations)
                {
                    Thread.Sleep(1000);
                }
            }

            // Menutup koneksi ke PLC setelah selesai 5x loop
            plc.Disconnect();
            Console.WriteLine("Loop Synchronous 5x selesai. Koneksi ke PLC telah ditutup.\n");
        }

        /// <summary>
        /// Metode pembacaan seluruh alamat data PLC IDEC FC6A secara Asynchronous (dengan async / await).
        /// Menggunakan ConnectAsync(), ReadInt16Async(), ReadBoolAsync(), dan Task.Delay().
        /// </summary>
        public static async Task RunAsynchronous()
        {
            string ipAddress = "192.168.2.36";
            int port = 2101;
            byte stationId = 0;
            int maxIterations = 5;

            Console.WriteLine($"Mencoba menghubungkan ke PLC IDEC FC6A secara Asynchronous ({ipAddress}:{port})...");

            IdecMicroSmartFC6A plc = new IdecMicroSmartFC6A(ipAddress, port, stationId);

            IdecResult connectResult = await plc.ConnectAsync();
            if (!connectResult.IsSuccess)
            {
                Console.WriteLine($"Koneksi gagal: {connectResult.Message} (Error Code: {connectResult.ErrorCode})");
                return;
            }

            Console.WriteLine("Koneksi berhasil terhubung ke PLC IDEC FC6A (Asynchronous).");
            Console.WriteLine($"Memulai loop pembacaan seluruh alamat data setiap 1 detik ({maxIterations}x perulangan)...\n");

            for (int count = 1; count <= maxIterations; count++)
            {
                Console.WriteLine($"=== Pembacaan Asynchronous Ke-{count} dari {maxIterations} ({DateTime.Now:HH:mm:ss}) ===");

                // 1. Membaca Data Register D0 (Word / 16-bit)
                IdecResult<short> resD0 = await plc.ReadInt16Async("D0");
                if (resD0.IsSuccess)
                {
                    Console.WriteLine($"[D0]   Nilai : {resD0.Content}");
                }
                else
                {
                    Console.WriteLine($"[D0]   Gagal : {resD0.Message} (Error: {resD0.ErrorCode})");
                }

                // 2. Membaca Input Relay I0.0 (Bit)
                IdecResult<bool> resI0 = await plc.ReadBoolAsync("I0.0");
                if (resI0.IsSuccess)
                {
                    Console.WriteLine($"[I0.0] Nilai : {resI0.Content}");
                }
                else
                {
                    Console.WriteLine($"[I0.0] Gagal : {resI0.Message} (Error: {resI0.ErrorCode})");
                }

                // 3. Membaca Output Relay Q0.0 (Bit)
                IdecResult<bool> resQ0 = await plc.ReadBoolAsync("Q0.0");
                if (resQ0.IsSuccess)
                {
                    Console.WriteLine($"[Q0.0] Nilai : {resQ0.Content}");
                }
                else
                {
                    Console.WriteLine($"[Q0.0] Gagal : {resQ0.Message} (Error: {resQ0.ErrorCode})");
                }

                // 4. Membaca Internal Relay M0 (Bit)
                IdecResult<bool> resM0 = await plc.ReadBoolAsync("M0");
                if (resM0.IsSuccess)
                {
                    Console.WriteLine($"[M0]   Nilai : {resM0.Content}");
                }
                else
                {
                    Console.WriteLine($"[M0]   Gagal : {resM0.Message} (Error: {resM0.ErrorCode})");
                }

                // 5. Membaca Shift Register R0 (Bit)
                IdecResult<bool> resR0 = await plc.ReadBoolAsync("R0");
                if (resR0.IsSuccess)
                {
                    Console.WriteLine($"[R0]   Nilai : {resR0.Content}");
                }
                else
                {
                    Console.WriteLine($"[R0]   Gagal : {resR0.Message} (Error: {resR0.ErrorCode})");
                }

                // 6. Membaca Kontak Timer T0 (Bit)
                IdecResult<bool> resT0 = await plc.ReadBoolAsync("T0");
                if (resT0.IsSuccess)
                {
                    Console.WriteLine($"[T0]   Nilai : {resT0.Content}");
                }
                else
                {
                    Console.WriteLine($"[T0]   Gagal : {resT0.Message} (Error: {resT0.ErrorCode})");
                }

                // 7. Membaca Timer Current TC0 (Word / 16-bit)
                IdecResult<short> resTC0 = await plc.ReadInt16Async("TC0");
                if (resTC0.IsSuccess)
                {
                    Console.WriteLine($"[TC0]  Nilai : {resTC0.Content}");
                }
                else
                {
                    Console.WriteLine($"[TC0]  Gagal : {resTC0.Message} (Error: {resTC0.ErrorCode})");
                }

                // 8. Membaca Timer Preset TP0 (Word / 16-bit)
                IdecResult<short> resTP0 = await plc.ReadInt16Async("TP0");
                if (resTP0.IsSuccess)
                {
                    Console.WriteLine($"[TP0]  Nilai : {resTP0.Content}");
                }
                else
                {
                    Console.WriteLine($"[TP0]  Gagal : {resTP0.Message} (Error: {resTP0.ErrorCode})");
                }

                // 9. Membaca Kontak Counter C0 (Bit)
                IdecResult<bool> resC0 = await plc.ReadBoolAsync("C0");
                if (resC0.IsSuccess)
                {
                    Console.WriteLine($"[C0]   Nilai : {resC0.Content}");
                }
                else
                {
                    Console.WriteLine($"[C0]   Gagal : {resC0.Message} (Error: {resC0.ErrorCode})");
                }

                // 10. Membaca Counter Current CC0 (Word / 16-bit)
                IdecResult<short> resCC0 = await plc.ReadInt16Async("CC0");
                if (resCC0.IsSuccess)
                {
                    Console.WriteLine($"[CC0]  Nilai : {resCC0.Content}");
                }
                else
                {
                    Console.WriteLine($"[CC0]  Gagal : {resCC0.Message} (Error: {resCC0.ErrorCode})");
                }

                // 11. Membaca Counter Preset CP0 (Word / 16-bit)
                IdecResult<short> resCP0 = await plc.ReadInt16Async("CP0");
                if (resCP0.IsSuccess)
                {
                    Console.WriteLine($"[CP0]  Nilai : {resCP0.Content}");
                }
                else
                {
                    Console.WriteLine($"[CP0]  Gagal : {resCP0.Message} (Error: {resCP0.ErrorCode})");
                }

                // 12. Membaca Bit di dalam Register D0.0 (Bit)
                IdecResult<bool> resD0Bit = await plc.ReadBoolAsync("D0.0");
                if (resD0Bit.IsSuccess)
                {
                    Console.WriteLine($"[D0.0] Nilai : {resD0Bit.Content}");
                }
                else
                {
                    Console.WriteLine($"[D0.0] Gagal : {resD0Bit.Message} (Error: {resD0Bit.ErrorCode})");
                }

                Console.WriteLine();

                // Berikan jeda 1 detik jika belum mencapai iterasi terakhir
                if (count < maxIterations)
                {
                    await Task.Delay(1000);
                }
            }

            // Menutup koneksi ke PLC setelah selesai 5x loop
            plc.Disconnect();
            Console.WriteLine("Loop Asynchronous 5x selesai. Koneksi ke PLC telah ditutup.\n");
        }
    }
}
