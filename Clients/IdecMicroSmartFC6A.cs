using NNIdec.Core;

namespace NNIdec
{
    /// <summary>
    /// Klien komunikasi untuk PLC IDEC MicroSmart FC6A Series (All-in-One dan Plus).
    /// Mendukung kapasitas register D hingga D55999 dan port Ethernet bawaan (Port 502 Modbus TCP / Port 2101 Maintenance).
    /// </summary>
    public class IdecMicroSmartFC6A : NNIdec
    {
        public override IdecPlcModel Model => IdecPlcModel.FC6A;

        /// <summary>
        /// Inisialisasi klien FC6A dengan alamat IP dan port komunikasi.
        /// </summary>
        /// <param name="ipAddress">Alamat IP PLC IDEC FC6A (misal "192.168.0.1").</param>
        /// <param name="port">Port komunikasi (Default 502 untuk Modbus TCP atau 2101 untuk Maintenance ASCII).</param>
        /// <param name="stationId">Station ID / Unit ID (Default 1).</param>
        public IdecMicroSmartFC6A(string ipAddress, int port = 502, byte stationId = 1)
            : base(ipAddress, port, stationId)
        {
        }
    }
}