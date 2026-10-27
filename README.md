# NNIdec

[![NuGet](https://img.shields.io/nuget/v/NNIdec.svg)](https://www.nuget.org/packages/NNIdec/)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**NNIdec** is a high-performance, robust, and lightweight C# library for communicating with **IDEC PLCs** over Ethernet (TCP) and Serial.

It supports modern **IDEC MicroSmart FC6A Series**, **MicroSmart FC4A / FC5A Pentra**, and legacy **IDEC Micro** (Micro 1 / Micro 3 / FA series) PLCs using a clean, modern **Result Pattern** without awkward method suffixes.

---

## 馃専 Key Features

- **Dedicated Classes for Each PLC Family**:
  - `IdecMicroSmartFC6A`: Designed for FC6A All-in-One & FC6A Plus (supports registers up to `D55999`).
  - `IdecMicroSmart`: Designed for FC4A and FC5A Pentra (registers up to `D8199`).
  - `IdecMicro`: Designed for legacy Micro 1, Micro 3, and OpenNet Controller (registers up to `D999`).
- **Polymorphic Base Class & Interface**:
  - All classes derive from `NNIdec` and implement `IIdecClient`, allowing interchangeable polymorphic usage:
    ```csharp
    NNIdec client = new IdecMicroSmartFC6A("192.168.0.10");
    ```
- **Clean Result Pattern (No "V2" Suffixes)**:
  - All read/write methods return `IdecResult<T>` and `IdecResult` with properties:
    - `IsSuccess`: boolean indicating whether the operation succeeded.
    - `Content` (or `Data`): typed payload returned from the PLC.
    - `ErrorCode`: error code (0 on success).
    - `Message`: human-readable description or error message.
- **Dual Communication Modes**:
  - **Modbus TCP (Port 502)**: Standard Ethernet industrial communication built into FC6A and FC5A.
  - **Maintenance ASCII Protocol (Port 2101 / Serial)**: Native IDEC Computer Link frame protocol.
- **Full Type Support**:
  - `bool` (`ReadBool`, `WriteBool`) - Supports individual bits in registers (`D100.5`) and relay coils (`M10`, `I0`, `Q0`).
  - `short` / `ushort` (`ReadInt16`, `ReadUInt16`, `WriteInt16`, `WriteUInt16`).
  - `int` / `uint` (`ReadInt32`, `ReadUInt32`, `WriteInt32`, `WriteUInt32`).
  - `long` / `ulong` (`ReadInt64`, `ReadUInt64`, `WriteInt64`, `WriteUInt64`).
  - `float` (`ReadFloat`, `WriteFloat`).
  - `double` (`ReadDouble`, `WriteDouble`).
  - `string` (`ReadString`, `WriteString`) - Safe null-terminator handling with automatic trailing buffer truncation.
  - Raw words & bytes (`ReadWords`, `WriteWords`, `ReadBytes`, `WriteBytes`).
- **Full Async / Await Support**:
  - Every operation has a native asynchronous counterpart (`ReadInt32Async`, `WriteStringAsync`, etc.).
- **Configurable Endianness**:
  - Supports `CDAB` (default for Modbus/IDEC), `ABCD` (Big-Endian), `BADC`, and `DCBA`.

---

## 馃殌 Quick Start

### 1. Connecting to IDEC MicroSmart FC6A (Modern Series)

```csharp
using System;
using System.Threading.Tasks;
using NNIdec;
using NNIdec.Core;

// 1. Initialize client using polymorphic base type
NNIdec plc = new IdecMicroSmartFC6A("192.168.0.10", port: 502, stationId: 1);

// Optional: Configure endianness (default is CDAB)
plc.ByteOrder = EndianFormat.CDAB;

// 2. Connect
IdecResult connectResult = await plc.ConnectAsync();
if (!connectResult.IsSuccess)
{
    Console.WriteLine($"Connection failed: {connectResult.Message}");
    return;
}

Console.WriteLine("Connected to IDEC FC6A PLC!");

// 3. Read Data Register (D100)
IdecResult<int> readD100 = await plc.ReadInt32Async("D100");
if (readD100.IsSuccess)
{
    Console.WriteLine($"D100 Value: {readD100.Content}");
}

// 4. Read Floating Point Value (D200)
IdecResult<float> readFloat = await plc.ReadFloatAsync("D200");
if (readFloat.IsSuccess)
{
    Console.WriteLine($"D200 Float: {readFloat.Content}");
}

// 5. Read Bit inside a Register (D100.5)
IdecResult<bool> readBit = await plc.ReadBoolAsync("D100.5");
if (readBit.IsSuccess)
{
    Console.WriteLine($"D100.5 Bit: {readBit.Content}");
}

// 6. Write Values to PLC
await plc.WriteInt32Async("D100", 98765);
await plc.WriteFloatAsync("D200", 123.45f);
await plc.WriteBoolAsync("D100.5", true);
await plc.WriteStringAsync("D300", "IDEC_FC6A_OK");

// 7. Disconnect when finished
plc.Disconnect();
```

---

### 2. Connecting to IDEC MicroSmart (FC4A / FC5A)

```csharp
NNIdec microsmart = new IdecMicroSmart("192.168.0.20", port: 502);
microsmart.Connect();

// Read Internal Relay (M10)
IdecResult<bool> m10 = microsmart.ReadBool("M10");

// Read Timer Current Value (TC1)
IdecResult<ushort> timerVal = microsmart.ReadUInt16("TC1");

// Read Data Register (D50)
IdecResult<short> regVal = microsmart.ReadInt16("D50");
```

---

### 3. Connecting to IDEC Micro (Legacy Series)

```csharp
NNIdec micro = new IdecMicro("192.168.0.30", port: 2101);
micro.Connect();

// Read Input Relay (Octal I0)
IdecResult<bool> input0 = micro.ReadBool("I0");

// Read Output Relay (Octal Q10)
IdecResult<bool> output10 = micro.ReadBool("Q10");
```

---

## 馃搵 Memory Device Addressing Reference

| Symbol | Device Name | Data Type | Addressing Radix | Supported Models |
| :--- | :--- | :--- | :--- | :--- |
| **I** | External Input Relay | Bit | Octal (`I0`-`I7`, `I10`-`I17`, ...) | FC6A, MicroSmart, Micro |
| **Q** | External Output Relay | Bit | Octal (`Q0`-`Q7`, `Q10`-`Q17`, ...) | FC6A, MicroSmart, Micro |
| **M** | Internal Relay | Bit | Decimal (`M0` - `M8000`) | FC6A, MicroSmart, Micro |
| **R** | Shift Register Relay | Bit | Decimal (`R0` - `R255`) | FC6A, MicroSmart, Micro |
| **T** | Timer Contact | Bit | Decimal (`T0` - `T1023`) | FC6A, MicroSmart, Micro |
| **TP** | Timer Preset Value | Word (16-bit) | Decimal | FC6A, MicroSmart, Micro |
| **TC** | Timer Current Value | Word (16-bit) | Decimal | FC6A, MicroSmart, Micro |
| **C** | Counter Contact | Bit | Decimal (`C0` - `C511`) | FC6A, MicroSmart, Micro |
| **CP** | Counter Preset Value | Word (16-bit) | Decimal | FC6A, MicroSmart, Micro |
| **CC** | Counter Current Value | Word (16-bit) | Decimal | FC6A, MicroSmart, Micro |
| **D** | Data Register | Word (16-bit) | Decimal (`D0` - `D55999` on FC6A) | FC6A, MicroSmart, Micro |
| **D_Bit** | Bit in Data Register | Bit | Format: `D{number}.{bit}` (`D100.0`-`D100.15`) | FC6A, MicroSmart, Micro |

---

## 馃摝 Compatibility

- **.NET Standard 2.0**: Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5, .NET 6, .NET 7, .NET 8, and .NET 9.
- **.NET 6.0 & .NET 8.0**: Multi-targeted for optimal async socket performance.

---

## 馃搫 License

MIT License. Developed by Ismail Lowkey.