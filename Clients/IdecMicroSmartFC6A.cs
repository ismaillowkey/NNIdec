using NNIdec.Core;

namespace NNIdec
{
    /// <summary>
    /// Communication client for IDEC MicroSmart FC6A Series PLCs (All-in-One and Plus).
    /// Supports data register capacity up to D55999 and built-in Ethernet ports (Port 502 Modbus TCP / Port 2101 Maintenance).
    /// </summary>
    public class IdecMicroSmartFC6A : NNIdec
    {
        public override IdecPlcModel Model => IdecPlcModel.FC6A;

        /// <summary>
        /// Initializes a new instance of the FC6A client with specified IP address and communication port.
        /// </summary>
        /// <param name="ipAddress">IP address of the IDEC FC6A PLC (e.g. "192.168.0.1").</param>
        /// <param name="port">Communication port (Default 2101 for Maintenance ASCII or 502 for Modbus TCP).</param>
        /// <param name="stationId">Station ID / Unit ID (Default 0 for Maintenance direct 1:1, or 1 for Modbus TCP).</param>
        public IdecMicroSmartFC6A(string ipAddress, int port = 2101, byte stationId = 0)
            : base(ipAddress, port, stationId)
        {
        }
    }
}