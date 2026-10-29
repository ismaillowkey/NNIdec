using NNIdec.Core;

namespace NNIdec
{
    /// <summary>
    /// Klien komunikasi untuk PLC IDEC Micro Legacy (Micro 1, Micro 3, FA Series).
    /// Mendukung kapasitas register D hingga D999.
    /// </summary>
    public class IdecMicro : NNIdec
    {
        public override IdecPlcModel Model => IdecPlcModel.Micro;

        /// <summary>
        /// Inisialisasi klien Micro dengan alamat IP dan port komunikasi.
        /// </summary>
        /// <param name="ipAddress">Alamat IP PLC IDEC Micro atau Serial-to-Ethernet Converter (misal "192.168.0.3").</param>
        /// <param name="port">Port komunikasi (Default 2101 untuk Maintenance ASCII atau 502 untuk gateway converter).</param>
        /// <param name="stationId">Station ID / Unit ID (Default 1).</param>
        /// <param name="autoReconnect">Sambung ulang otomatis saat koneksi jaringan terputus (Default true).</param>
        public IdecMicro(string ipAddress, int port = 2101, byte stationId = 1, bool autoReconnect = true)
            : base(ipAddress, port, stationId, autoReconnect)
        {
        }
    }
}