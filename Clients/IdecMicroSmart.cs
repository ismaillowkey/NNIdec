using NNIdec.Core;

namespace NNIdec
{
    /// <summary>
    /// Klien komunikasi untuk PLC IDEC MicroSmart (FC4A dan FC5A Pentra Series).
    /// Mendukung kapasitas register D hingga D8199.
    /// </summary>
    public class IdecMicroSmart : NNIdec
    {
        public override IdecPlcModel Model => IdecPlcModel.MicroSmart;

        /// <summary>
        /// Inisialisasi klien MicroSmart dengan alamat IP dan port komunikasi.
        /// </summary>
        /// <param name="ipAddress">Alamat IP PLC IDEC MicroSmart (misal "192.168.0.2").</param>
        /// <param name="port">Port komunikasi (Default 502 untuk Modbus TCP atau 2101 untuk Maintenance ASCII).</param>
        /// <param name="stationId">Station ID / Unit ID (Default 1).</param>
        /// <param name="autoReconnect">Sambung ulang otomatis saat koneksi jaringan terputus (Default true).</param>
        public IdecMicroSmart(string ipAddress, int port = 502, byte stationId = 1, bool autoReconnect = true)
            : base(ipAddress, port, stationId, autoReconnect)
        {
        }
    }
}