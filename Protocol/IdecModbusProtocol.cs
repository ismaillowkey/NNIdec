using System;
using NNIdec.Core;

namespace NNIdec.Protocol
{
    /// <summary>
    /// Pembentuk paket Modbus TCP untuk komunikasi Ethernet IDEC (Port 502).
    /// </summary>
    public static class IdecModbusProtocol
    {
        private static int _transactionIdCounter = 0;

        /// <summary>
        /// Mendapatkan Transaction ID berikutnya secara thread-safe.
        /// </summary>
        public static ushort NextTransactionId()
        {
            int next = System.Threading.Interlocked.Increment(ref _transactionIdCounter);
            return (ushort)(next & 0xFFFF);
        }

        /// <summary>
        /// Mengubah alamat IDEC menjadi Modbus Start Address dan Function Code.
        /// </summary>
        public static void MapAddressToModbus(IdecAddress address, out byte functionCode, out ushort modbusStartAddr)
        {
            if (address.DeviceType == IdecDeviceType.Input)
            {
                // Discrete Input (FC 02)
                functionCode = 0x02;
                modbusStartAddr = (ushort)address.AddressNumber;
            }
            else if (address.DeviceType == IdecDeviceType.Output || address.DeviceType == IdecDeviceType.InternalRelay || address.DeviceType == IdecDeviceType.ShiftRegister)
            {
                // Coil (FC 01)
                functionCode = 0x01;
                modbusStartAddr = (ushort)address.AddressNumber;
            }
            else if (address.DeviceType == IdecDeviceType.TimerContact || address.DeviceType == IdecDeviceType.CounterContact)
            {
                // Contact (FC 01)
                functionCode = 0x01;
                modbusStartAddr = (ushort)address.AddressNumber;
            }
            else
            {
                // Holding Register (FC 03) untuk D, TP, TC, CP, CC
                functionCode = 0x03;
                modbusStartAddr = (ushort)address.AddressNumber;
            }
        }

        /// <summary>
        /// Membuat frame Modbus TCP Read Holding Registers (FC 03) atau Read Coils/Inputs (FC 01/02).
        /// </summary>
        public static byte[] BuildModbusReadFrame(ushort transactionId, byte unitId, byte functionCode, ushort startAddress, ushort count)
        {
            byte[] frame = new byte[12];
            // MBAP Header
            frame[0] = (byte)(transactionId >> 8);
            frame[1] = (byte)(transactionId & 0xFF);
            frame[2] = 0x00; // Protocol ID Hi
            frame[3] = 0x00; // Protocol ID Lo
            frame[4] = 0x00; // Length Hi
            frame[5] = 0x06; // Length Lo (6 bytes: UnitId + FC + StartAddr + Count)
            frame[6] = unitId;

            // PDU
            frame[7] = functionCode;
            frame[8] = (byte)(startAddress >> 8);
            frame[9] = (byte)(startAddress & 0xFF);
            frame[10] = (byte)(count >> 8);
            frame[11] = (byte)(count & 0xFF);

            return frame;
        }

        /// <summary>
        /// Membuat frame Modbus TCP Write Single Register (FC 06) atau Multiple Registers (FC 16).
        /// </summary>
        public static byte[] BuildModbusWriteRegistersFrame(ushort transactionId, byte unitId, ushort startAddress, ushort[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("Nilai yang akan ditulis tidak boleh kosong.", nameof(values));
            }

            if (values.Length == 1)
            {
                // FC 06: Write Single Register
                byte[] frame = new byte[12];
                frame[0] = (byte)(transactionId >> 8);
                frame[1] = (byte)(transactionId & 0xFF);
                frame[2] = 0x00;
                frame[3] = 0x00;
                frame[4] = 0x00;
                frame[5] = 0x06;
                frame[6] = unitId;

                frame[7] = 0x06; // FC 06
                frame[8] = (byte)(startAddress >> 8);
                frame[9] = (byte)(startAddress & 0xFF);
                frame[10] = (byte)(values[0] >> 8);
                frame[11] = (byte)(values[0] & 0xFF);
                return frame;
            }
            else
            {
                // FC 16 (0x10): Write Multiple Registers
                ushort count = (ushort)values.Length;
                byte byteCount = (byte)(count * 2);
                ushort pduLength = (ushort)(7 + byteCount);

                byte[] frame = new byte[6 + pduLength];
                frame[0] = (byte)(transactionId >> 8);
                frame[1] = (byte)(transactionId & 0xFF);
                frame[2] = 0x00;
                frame[3] = 0x00;
                frame[4] = (byte)(pduLength >> 8);
                frame[5] = (byte)(pduLength & 0xFF);
                frame[6] = unitId;

                frame[7] = 0x10; // FC 16
                frame[8] = (byte)(startAddress >> 8);
                frame[9] = (byte)(startAddress & 0xFF);
                frame[10] = (byte)(count >> 8);
                frame[11] = (byte)(count & 0xFF);
                frame[12] = byteCount;

                for (int i = 0; i < count; i++)
                {
                    int offset = 13 + (i * 2);
                    frame[offset] = (byte)(values[i] >> 8);
                    frame[offset + 1] = (byte)(values[i] & 0xFF);
                }
                return frame;
            }
        }

        /// <summary>
        /// Membuat frame Modbus TCP Write Single Coil (FC 05).
        /// </summary>
        public static byte[] BuildModbusWriteSingleCoilFrame(ushort transactionId, byte unitId, ushort coilAddress, bool value)
        {
            byte[] frame = new byte[12];
            frame[0] = (byte)(transactionId >> 8);
            frame[1] = (byte)(transactionId & 0xFF);
            frame[2] = 0x00;
            frame[3] = 0x00;
            frame[4] = 0x00;
            frame[5] = 0x06;
            frame[6] = unitId;

            frame[7] = 0x05; // FC 05
            frame[8] = (byte)(coilAddress >> 8);
            frame[9] = (byte)(coilAddress & 0xFF);
            if (value)
            {
                frame[10] = 0xFF; // ON: 0xFF00
                frame[11] = 0x00;
            }
            else
            {
                frame[10] = 0x00; // OFF: 0x0000
                frame[11] = 0x00;
            }
            return frame;
        }
    }
}