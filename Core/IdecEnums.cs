namespace NNIdec.Core
{
    /// <summary>
    /// Format urutan byte (Endianness) untuk konversi data 32-bit dan 64-bit.
    /// </summary>
    public enum EndianFormat
    {
        /// <summary>
        /// Big-Endian (ABCD): Byte paling signifikan berada di depan.
        /// </summary>
        ABCD = 0,

        /// <summary>
        /// Big-Endian byte swap / Little-Endian word swap (BADC).
        /// </summary>
        BADC = 1,

        /// <summary>
        /// Little-Endian word swap (CDAB). Sering digunakan sebagai default pada berbagai perangkat PLC / Modbus.
        /// </summary>
        CDAB = 2,

        /// <summary>
        /// Little-Endian (DCBA): Byte paling tidak signifikan berada di depan.
        /// </summary>
        DCBA = 3
    }

    /// <summary>
    /// Model / Keluarga PLC IDEC yang didukung.
    /// </summary>
    public enum IdecPlcModel
    {
        /// <summary>
        /// Seri IDEC MicroSmart FC6A (FC6A All-in-One dan FC6A Plus).
        /// Default model modern dengan register D hingga 55999.
        /// </summary>
        FC6A = 0,

        /// <summary>
        /// Seri IDEC MicroSmart (FC4A dan FC5A Pentra).
        /// Register D hingga 8199.
        /// </summary>
        MicroSmart = 1,

        /// <summary>
        /// Seri IDEC Micro legacy (Micro 1, Micro 3, FA series).
        /// Register D hingga 999.
        /// </summary>
        Micro = 2
    }

    /// <summary>
    /// Area memori / tipe device pada PLC IDEC.
    /// </summary>
    public enum IdecDeviceType
    {
        /// <summary>
        /// Input relay fisik (I) - Pengalamatan oktal.
        /// </summary>
        Input = 0,

        /// <summary>
        /// Output relay fisik (Q) - Pengalamatan oktal.
        /// </summary>
        Output = 1,

        /// <summary>
        /// Internal relay (M) - Bit memory internal.
        /// </summary>
        InternalRelay = 2,

        /// <summary>
        /// Shift register relay (R).
        /// </summary>
        ShiftRegister = 3,

        /// <summary>
        /// Kontak Timer (T) - bernilai bit (0 atau 1).
        /// </summary>
        TimerContact = 4,

        /// <summary>
        /// Timer Preset value (TP) - bernilai word (16-bit unsigned).
        /// </summary>
        TimerPreset = 5,

        /// <summary>
        /// Timer Current value (TC) - bernilai word (16-bit unsigned).
        /// </summary>
        TimerCurrent = 6,

        /// <summary>
        /// Kontak Counter (C) - bernilai bit (0 atau 1).
        /// </summary>
        CounterContact = 7,

        /// <summary>
        /// Counter Preset value (CP) - bernilai word (16-bit unsigned).
        /// </summary>
        CounterPreset = 8,

        /// <summary>
        /// Counter Current value (CC) - bernilai word (16-bit unsigned).
        /// </summary>
        CounterCurrent = 9,

        /// <summary>
        /// Data Register (D) - bernilai word (16-bit).
        /// </summary>
        DataRegister = 10,

        /// <summary>
        /// Bit individual di dalam Data Register (D_Bit), misalnya D100.5.
        /// </summary>
        DataRegisterBit = 11
    }
}