using System;
using System.Globalization;

namespace NNIdec.Core
{
    /// <summary>
    /// Representasi alamat memori PLC IDEC yang telah di-parse dan divalidasi.
    /// </summary>
    public class IdecAddress
    {
        /// <summary>
        /// String alamat asli yang dimasukkan oleh pengguna (misal "D100", "I0", "D100.5").
        /// </summary>
        public string RawAddress { get; set; } = string.Empty;

        /// <summary>
        /// Area atau tipe memori IDEC (D, M, I, Q, T, C, dll).
        /// </summary>
        public IdecDeviceType DeviceType { get; set; }

        /// <summary>
        /// Nomor indeks alamat dalam bentuk angka desimal.
        /// </summary>
        public int AddressNumber { get; set; }

        /// <summary>
        /// Indeks bit khusus untuk D_Bit (misal D100.5 memiliki BitIndex = 5).
        /// Bernilai -1 jika bukan pengalamatan bit di dalam register.
        /// </summary>
        public int BitIndex { get; set; } = -1;

        /// <summary>
        /// Menunjukkan apakah alamat ini bertipe bit (I, Q, M, R, T, C, D_Bit).
        /// </summary>
        public bool IsBit { get; set; }

        /// <summary>
        /// Kode karakter protokol IDEC Maintenance (misal 'D', 'M', 'X', 'Y', dll).
        /// </summary>
        public string ProtocolCode { get; set; } = string.Empty;

        /// <summary>
        /// Parsing string alamat PLC IDEC sesuai dengan model PLC yang aktif.
        /// </summary>
        /// <param name="addressString">Teks alamat PLC (misal "D100", "M20", "I10").</param>
        /// <param name="model">Model PLC IDEC yang digunakan.</param>
        /// <returns>Objek IdecAddress jika valid, atau exception jika format salah.</returns>
        public static IdecAddress Parse(string addressString, IdecPlcModel model = IdecPlcModel.FC6A)
        {
            if (string.IsNullOrWhiteSpace(addressString))
            {
                throw new ArgumentException("Alamat PLC IDEC tidak boleh kosong atau null.", nameof(addressString));
            }

            string raw = addressString.Trim().ToUpperInvariant();
            IdecAddress address = new IdecAddress();
            address.RawAddress = raw;

            // 1. Cek apakah alamat register dengan bit khusus, contoh: D100.5
            if (raw.StartsWith("D") && raw.Contains("."))
            {
                string[] parts = raw.Split('.');
                if (parts.Length != 2)
                {
                    throw new FormatException("Format bit register tidak valid: " + raw + ". Format harus berupa D{nomor}.{bit}, contoh D100.5");
                }

                // Ambil nomor register
                string regNumberStr = parts[0].Substring(1);
                int regNumber;
                if (!int.TryParse(regNumberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out regNumber))
                {
                    throw new FormatException("Nomor register tidak valid pada alamat: " + raw);
                }

                // Ambil nomor bit (0 - 15)
                int bitIndex;
                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out bitIndex))
                {
                    throw new FormatException("Nomor bit tidak valid pada alamat: " + raw);
                }

                if (bitIndex < 0 || bitIndex > 15)
                {
                    throw new ArgumentOutOfRangeException(nameof(addressString), "Indeks bit register harus di antara 0 sampai 15. Ditemukan: " + bitIndex);
                }

                address.DeviceType = IdecDeviceType.DataRegisterBit;
                address.AddressNumber = regNumber;
                address.BitIndex = bitIndex;
                address.IsBit = true;
                address.ProtocolCode = "D";

                ValidateAddressRange(address, model);
                return address;
            }

            // 2. Cek Timer Preset (TP) dan Timer Current (TC)
            if (raw.StartsWith("TP"))
            {
                address.DeviceType = IdecDeviceType.TimerPreset;
                address.AddressNumber = ParseInt(raw.Substring(2), raw);
                address.IsBit = false;
                address.ProtocolCode = "T";
                ValidateAddressRange(address, model);
                return address;
            }

            if (raw.StartsWith("TC"))
            {
                address.DeviceType = IdecDeviceType.TimerCurrent;
                address.AddressNumber = ParseInt(raw.Substring(2), raw);
                address.IsBit = false;
                address.ProtocolCode = "T";
                ValidateAddressRange(address, model);
                return address;
            }

            // 3. Cek Counter Preset (CP) dan Counter Current (CC)
            if (raw.StartsWith("CP"))
            {
                address.DeviceType = IdecDeviceType.CounterPreset;
                address.AddressNumber = ParseInt(raw.Substring(2), raw);
                address.IsBit = false;
                address.ProtocolCode = "C";
                ValidateAddressRange(address, model);
                return address;
            }

            if (raw.StartsWith("CC"))
            {
                address.DeviceType = IdecDeviceType.CounterCurrent;
                address.AddressNumber = ParseInt(raw.Substring(2), raw);
                address.IsBit = false;
                address.ProtocolCode = "C";
                ValidateAddressRange(address, model);
                return address;
            }

            // 4. Cek Input Relay (I atau X) - Format Oktal atau Dot-Notation (I0.0)
            if (raw.StartsWith("I") || raw.StartsWith("X"))
            {
                address.DeviceType = IdecDeviceType.Input;
                address.IsBit = true;
                // Protokol internal IDEC Maintenance menggunakan kode 'X' untuk input relay
                address.ProtocolCode = "X";

                string numberPart = raw.Substring(1);
                if (numberPart.Contains("."))
                {
                    // Format byte.bit seperti I0.0 atau X0.0
                    string[] parts = numberPart.Split('.');
                    if (parts.Length != 2)
                    {
                        throw new FormatException("Format input relay tidak valid: " + raw + ". Format harus berupa I{byte}.{bit}, contoh: I0.0");
                    }

                    int byteIndex = ParseInt(parts[0], raw);
                    int bitIndex = ParseInt(parts[1], raw);
                    if (bitIndex < 0 || bitIndex > 7)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addressString), "Indeks bit pada input relay harus di antara 0 sampai 7. Ditemukan: " + bitIndex);
                    }

                    // Rumus konversi oktal ke desimal linier (1 byte = 8 bit)
                    address.AddressNumber = (byteIndex * 8) + bitIndex;
                }
                else
                {
                    // Format oktal biasa (I0, I7, I10, dst)
                    address.AddressNumber = ParseOctal(numberPart, raw);
                }

                ValidateAddressRange(address, model);
                return address;
            }

            // 5. Cek Output Relay (Q atau Y) - Format Oktal atau Dot-Notation (Q0.0)
            if (raw.StartsWith("Q") || raw.StartsWith("Y"))
            {
                address.DeviceType = IdecDeviceType.Output;
                address.IsBit = true;
                // Protokol internal IDEC Maintenance menggunakan kode 'Y' untuk output relay
                address.ProtocolCode = "Y";

                string numberPart = raw.Substring(1);
                if (numberPart.Contains("."))
                {
                    // Format byte.bit seperti Q0.0 atau Y0.0
                    string[] parts = numberPart.Split('.');
                    if (parts.Length != 2)
                    {
                        throw new FormatException("Format output relay tidak valid: " + raw + ". Format harus berupa Q{byte}.{bit}, contoh: Q0.0");
                    }

                    int byteIndex = ParseInt(parts[0], raw);
                    int bitIndex = ParseInt(parts[1], raw);
                    if (bitIndex < 0 || bitIndex > 7)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addressString), "Indeks bit pada output relay harus di antara 0 sampai 7. Ditemukan: " + bitIndex);
                    }

                    // Rumus konversi oktal ke desimal linier (1 byte = 8 bit)
                    address.AddressNumber = (byteIndex * 8) + bitIndex;
                }
                else
                {
                    // Format oktal biasa (Q0, Q7, Q10, dst)
                    address.AddressNumber = ParseOctal(numberPart, raw);
                }

                ValidateAddressRange(address, model);
                return address;
            }

            // 6. Cek Internal Relay (M) - Format Desimal
            if (raw.StartsWith("M"))
            {
                address.DeviceType = IdecDeviceType.InternalRelay;
                address.AddressNumber = ParseInt(raw.Substring(1), raw);
                address.IsBit = true;
                address.ProtocolCode = "M";
                ValidateAddressRange(address, model);
                return address;
            }

            // 7. Cek Shift Register (R) - Format Desimal
            if (raw.StartsWith("R"))
            {
                address.DeviceType = IdecDeviceType.ShiftRegister;
                address.AddressNumber = ParseInt(raw.Substring(1), raw);
                address.IsBit = true;
                address.ProtocolCode = "R";
                ValidateAddressRange(address, model);
                return address;
            }

            // 8. Cek Kontak Timer (T) - Format Desimal (Bit)
            if (raw.StartsWith("T"))
            {
                address.DeviceType = IdecDeviceType.TimerContact;
                address.AddressNumber = ParseInt(raw.Substring(1), raw);
                address.IsBit = true;
                address.ProtocolCode = "T";
                ValidateAddressRange(address, model);
                return address;
            }

            // 9. Cek Kontak Counter (C) - Format Desimal (Bit)
            if (raw.StartsWith("C"))
            {
                address.DeviceType = IdecDeviceType.CounterContact;
                address.AddressNumber = ParseInt(raw.Substring(1), raw);
                address.IsBit = true;
                address.ProtocolCode = "C";
                ValidateAddressRange(address, model);
                return address;
            }

            // 10. Cek Data Register (D) - Format Desimal (Word)
            if (raw.StartsWith("D"))
            {
                address.DeviceType = IdecDeviceType.DataRegister;
                address.AddressNumber = ParseInt(raw.Substring(1), raw);
                address.IsBit = false;
                address.ProtocolCode = "D";
                ValidateAddressRange(address, model);
                return address;
            }

            throw new FormatException("Format alamat IDEC tidak dikenali: " + raw + ". Simbol yang didukung: I, Q, M, R, T, TP, TC, C, CP, CC, D.");
        }

        /// <summary>
        /// Validasi batas memori PLC IDEC berdasarkan spesifikasi masing-masing model.
        /// </summary>
        private static void ValidateAddressRange(IdecAddress addr, IdecPlcModel model)
        {
            if (model == IdecPlcModel.FC6A)
            {
                // Batas memori untuk FC6A
                if (addr.DeviceType == IdecDeviceType.DataRegister || addr.DeviceType == IdecDeviceType.DataRegisterBit)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 55999)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat register D pada FC6A harus berada pada rentang D0 - D55999. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.InternalRelay)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 8000)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat relay M pada FC6A harus berada pada rentang M0 - M8000. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.TimerContact || addr.DeviceType == IdecDeviceType.TimerCurrent || addr.DeviceType == IdecDeviceType.TimerPreset)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 1023)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat Timer pada FC6A harus berada pada rentang 0 - 1023. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.CounterContact || addr.DeviceType == IdecDeviceType.CounterCurrent || addr.DeviceType == IdecDeviceType.CounterPreset)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 511)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat Counter pada FC6A harus berada pada rentang 0 - 511. Ditemukan: " + addr.AddressNumber);
                    }
                }
            }
            else if (model == IdecPlcModel.MicroSmart)
            {
                // Batas memori untuk MicroSmart FC4A / FC5A
                if (addr.DeviceType == IdecDeviceType.DataRegister || addr.DeviceType == IdecDeviceType.DataRegisterBit)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 8199)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat register D pada MicroSmart harus berada pada rentang D0 - D8199. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.InternalRelay)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 2047)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat relay M pada MicroSmart harus berada pada rentang M0 - M2047. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.TimerContact || addr.DeviceType == IdecDeviceType.TimerCurrent || addr.DeviceType == IdecDeviceType.TimerPreset)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 255)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat Timer pada MicroSmart harus berada pada rentang 0 - 255. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.CounterContact || addr.DeviceType == IdecDeviceType.CounterCurrent || addr.DeviceType == IdecDeviceType.CounterPreset)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 255)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat Counter pada MicroSmart harus berada pada rentang 0 - 255. Ditemukan: " + addr.AddressNumber);
                    }
                }
            }
            else if (model == IdecPlcModel.Micro)
            {
                // Batas memori untuk legacy Micro
                if (addr.DeviceType == IdecDeviceType.DataRegister || addr.DeviceType == IdecDeviceType.DataRegisterBit)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 999)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat register D pada Micro harus berada pada rentang D0 - D999. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.InternalRelay)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 999)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat relay M pada Micro harus berada pada rentang M0 - M999. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.TimerContact || addr.DeviceType == IdecDeviceType.TimerCurrent || addr.DeviceType == IdecDeviceType.TimerPreset)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 99)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat Timer pada Micro harus berada pada rentang 0 - 99. Ditemukan: " + addr.AddressNumber);
                    }
                }
                else if (addr.DeviceType == IdecDeviceType.CounterContact || addr.DeviceType == IdecDeviceType.CounterCurrent || addr.DeviceType == IdecDeviceType.CounterPreset)
                {
                    if (addr.AddressNumber < 0 || addr.AddressNumber > 99)
                    {
                        throw new ArgumentOutOfRangeException(nameof(addr.AddressNumber), "Alamat Counter pada Micro harus berada pada rentang 0 - 99. Ditemukan: " + addr.AddressNumber);
                    }
                }
            }
        }

        private static int ParseInt(string str, string raw)
        {
            int val;
            if (!int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            {
                throw new FormatException("Nomor alamat desimal tidak valid pada: " + raw);
            }
            return val;
        }

        private static int ParseOctal(string octalStr, string raw)
        {
            try
            {
                // Parsing nilai bilangan oktal ke desimal
                return Convert.ToInt32(octalStr, 8);
            }
            catch (Exception ex)
            {
                throw new FormatException("Nomor alamat oktal tidak valid pada: " + raw + ". Input/Output IDEC menggunakan bilangan oktal (digit 0 sampai 7).", ex);
            }
        }

        public override string ToString()
        {
            return RawAddress;
        }
    }
}