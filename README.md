# NNIdec

[![NuGet](https://img.shields.io/nuget/v/NNIdec.svg)](https://www.nuget.org/packages/NNIdec/)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Standard-2.0%20%7C%20.NET%206%20%7C%20.NET%208-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**NNIdec** is a high-performance, robust, and lightweight C# library for communicating with **IDEC PLCs** over Ethernet (TCP) and Serial.

Tested and verified directly with physical **IDEC MicroSmart FC6A** hardware, it supports out-of-the-box **Maintenance Protocol (Port 2101)** as well as **Modbus TCP (Port 502)** using a clean, modern **Result Pattern** (`IdecResult<T>`).

---

## 📑 Table of Contents
- [🌟 Key Features](#-key-features)
- [📦 Installation](#-installation)
- [🚀 Quick Start](#-quick-start)
  - [1. Synchronous (Without Async - Simple & Fast)](#1-synchronous-without-async---simple--fast)
  - [2. Asynchronous (With Async / Await)](#2-asynchronous-with-async--await)
  - [3. Connecting via Modbus TCP (Port 502)](#3-connecting-via-modbus-tcp-port-502)
- [📑 Memory Device Addressing Reference](#-memory-device-addressing-reference)
- [🔄 Supported Data Types & Endianness](#-supported-data-types--endianness)
- [🏃 Running the Example Project](#-running-the-example-project)
- [📦 Compatibility](#-compatibility)
- [📄 License](#-license)

---

## 🌟 Key Features

- **Dedicated Classes for Each PLC Family**:
  - `IdecMicroSmartFC6A`: Designed for FC6A All-in-One & FC6A Plus (supports registers up to `D55999`).
  - `IdecMicroSmart`: Designed for FC4A and FC5A Pentra (registers up to `D8199`).
  - `IdecMicro`: Designed for legacy Micro 1, Micro 3, and OpenNet Controller (registers up to `D999`).
- **Polymorphic Interface (`IIdecClient`)**:
  - All classes implement `IIdecClient`, allowing interchangeable polymorphic usage across all PLC models:
    ```csharp
    IIdecClient client = new IdecMicroSmartFC6A("192.168.2.36", port: 2101, stationId: 0);
    ```
- **Clean Result Pattern (No "V2" Suffixes)**:
  - All read and write methods return `IdecResult<T>` and `IdecResult` with explicit properties:
    - `IsSuccess`: boolean indicating whether the operation succeeded.
    - `Content` (or `Data`): typed payload returned from the PLC.
    - `ErrorCode`: error code (0 on success).
    - `Message`: human-readable description or error message.
- **Dual Communication Modes**:
  - **Maintenance ASCII Protocol (Port 2101 / Serial)**: Native IDEC Maintenance Protocol (WindLDR port). Enabled by default out of the box on FC6A without any extra configuration.
  - **Modbus TCP (Port 502)**: Standard Ethernet industrial communication built into FC6A and FC5A.
- **Full Type Support**:
  - `bool` (`ReadBool`, `WriteBool`): Supports individual bits in registers (`D0.5`), relay coils (`M0`, `R0`), contact timers/counters (`T0`, `C0`), and physical I/O with dot-notation (`I0.0`, `Q0.0`).
  - `short` / `ushort` (`ReadInt16`, `ReadUInt16`, `WriteInt16`, `WriteUInt16`).
  - `int` / `uint` (`ReadInt32`, `ReadUInt32`, `WriteInt32`, `WriteUInt32`).
  - `long` / `ulong` (`ReadInt64`, `ReadUInt64`, `WriteInt64`, `WriteUInt64`).
  - `float` (`ReadFloat`, `WriteFloat`).
  - `double` (`ReadDouble`, `WriteDouble`).
  - `string` (`ReadString`, `WriteString`): Safe null-terminator handling with automatic trailing buffer truncation.
  - Raw words & bytes (`ReadWords`, `WriteWords`, `ReadBytes`, `WriteBytes`).
- **Full Async / Await Support**:
  - Every operation has a native asynchronous counterpart (`ReadInt16Async`, `ReadBoolAsync`, `WriteStringAsync`, etc.).
- **Configurable Endianness**:
  - Supports `CDAB` (default for Modbus/IDEC), `ABCD` (Big-Endian), `BADC`, and `DCBA`.

---

## 📦 Installation

Install via NuGet Package Manager Console:
```powershell
Install-Package NNIdec
```

Or via .NET CLI:
```bash
dotnet add package NNIdec
```

---

## 🚀 Quick Start

### 1. Synchronous (Without Async - Simple & Fast)

Use `var` for clean, concise, and readable code:

```csharp
using System;
using NNIdec;

// 1. Initialize FC6A client (Port 2101 Maintenance, Station 0)
var plc = new IdecMicroSmartFC6A("192.168.2.36", port: 2101, stationId: 0);

// 2. Connect to PLC synchronously
var connectResult = plc.Connect();
if (!connectResult.IsSuccess)
{
    Console.WriteLine($"Connection failed: {connectResult.Message}");
    return;
}

Console.WriteLine("Successfully connected to IDEC FC6A PLC!");

// 3. Read Data Registers and Relays (using var)
var readD0 = plc.ReadInt16("D0");
var readInput = plc.ReadBool("I0.0");
var readOutput = plc.ReadBool("Q0.0");
var readM0 = plc.ReadBool("M0");
var readR0 = plc.ReadBool("R0");
var timerContact = plc.ReadBool("T0");
var timerCurrent = plc.ReadInt16("TC0");
var counterContact = plc.ReadBool("C0");
var counterCurrent = plc.ReadInt16("CC0");
var bitInD0 = plc.ReadBool("D0.5");

// Display read values
if (readD0.IsSuccess)
{
    Console.WriteLine($"D0: {readD0.Content}");
}

if (readInput.IsSuccess && readOutput.IsSuccess)
{
    Console.WriteLine($"I0.0: {readInput.Content}, Q0.0: {readOutput.Content}");
}

// 4. Write data to PLC synchronously
var writeD0 = plc.WriteInt16("D0", 1234);
var writeFloat = plc.WriteFloat("D20", 3.1415f);
var writeM0 = plc.WriteBool("M0", true);
var writeQ0 = plc.WriteBool("Q0.0", true);
var writeStr = plc.WriteString("D30", "IDEC-FC6A");

// 5. Disconnect when finished
plc.Disconnect();
Console.WriteLine("Connection closed.");
```

---

### 2. Asynchronous (With Async / Await)

For non-blocking applications (WPF, WinForms, Web API, or async Console apps):

```csharp
using System;
using System.Threading.Tasks;
using NNIdec;

var plc = new IdecMicroSmartFC6A("192.168.2.36", port: 2101, stationId: 0);

// Connect to PLC asynchronously
var connectResult = await plc.ConnectAsync();
if (!connectResult.IsSuccess)
{
    Console.WriteLine($"Connection failed: {connectResult.Message}");
    return;
}

// Read data asynchronously (using var)
var readD0 = await plc.ReadInt16Async("D0");
var readInput = await plc.ReadBoolAsync("I0.0");
var readOutput = await plc.ReadBoolAsync("Q0.0");
var readM0 = await plc.ReadBoolAsync("M0");
var readR0 = await plc.ReadBoolAsync("R0");
var timerContact = await plc.ReadBoolAsync("T0");
var timerCurrent = await plc.ReadInt16Async("TC0");
var counterContact = await plc.ReadBoolAsync("C0");
var counterCurrent = await plc.ReadInt16Async("CC0");
var bitInD0 = await plc.ReadBoolAsync("D0.5");

if (readD0.IsSuccess)
{
    Console.WriteLine($"D0: {readD0.Content}");
}

// Write data asynchronously
var writeD0 = await plc.WriteInt16Async("D0", 1234);
var writeFloat = await plc.WriteFloatAsync("D20", 3.1415f);
var writeM0 = await plc.WriteBoolAsync("M0", true);
var writeQ0 = await plc.WriteBoolAsync("Q0.0", true);
var writeStr = await plc.WriteStringAsync("D30", "IDEC-FC6A");

plc.Disconnect();
```

---

### 3. Connecting via Modbus TCP (Port 502)

If the Modbus TCP Server is enabled on your IDEC PLC in WindLDR (*Function Area Settings -> Communication Ports*):

```csharp
// Modbus TCP uses Port 502 (Default Station / Unit ID: 1)
var plc = new IdecMicroSmartFC6A("192.168.2.36", port: 502, stationId: 1);
var connectResult = plc.Connect();

// Read Holding Register D0
var val = plc.ReadInt16("D0");
```

---

## 📑 Memory Device Addressing Reference

| Symbol | Device Name | Data Type | Addressing Format / Radix | Supported Models |
| :--- | :--- | :--- | :--- | :--- |
| **I / X** | External Input Relay | Bit | Byte.Bit (`I0.0`-`I7.7`) or Octal (`I0`-`I7`, `I10`...) | FC6A, MicroSmart, Micro |
| **Q / Y** | External Output Relay | Bit | Byte.Bit (`Q0.0`-`Q7.7`) or Octal (`Q0`-`Q7`, `Q10`...) | FC6A, MicroSmart, Micro |
| **M** | Internal Relay | Bit | Decimal (`M0` - `M8000` on FC6A) | FC6A, MicroSmart, Micro |
| **R** | Shift Register Relay | Bit | Decimal (`R0` - `R255`) | FC6A, MicroSmart, Micro |
| **T** | Timer Contact | Bit | Decimal (`T0` - `T1023`) | FC6A, MicroSmart, Micro |
| **TC** | Timer Current Value | Word (16-bit) | Decimal (`TC0` - `TC1023`) | FC6A, MicroSmart, Micro |
| **TP** | Timer Preset Value | Word (16-bit) | Decimal (`TP0` - `TP1023`) | FC6A, MicroSmart, Micro |
| **C** | Counter Contact | Bit | Decimal (`C0` - `C511`) | FC6A, MicroSmart, Micro |
| **CC** | Counter Current Value | Word (16-bit) | Decimal (`CC0` - `CC511`) | FC6A, MicroSmart, Micro |
| **CP** | Counter Preset Value | Word (16-bit) | Decimal (`CP0` - `CP511`) | FC6A, MicroSmart, Micro |
| **D** | Data Register | Word (16-bit) | Decimal (`D0` - `D55999` on FC6A) | FC6A, MicroSmart, Micro |
| **D_Bit** | Bit in Data Register | Bit | Format: `D{number}.{bit}` (`D0.0`-`D0.15`) | FC6A, MicroSmart, Micro |

---

## 🔄 Supported Data Types & Endianness

NNIdec supports configurable byte ordering (endianness) for 32-bit and 64-bit data types:
```csharp
plc.ByteOrder = EndianFormat.CDAB; // Default IDEC & Modbus standard
// Other supported formats: EndianFormat.ABCD, EndianFormat.BADC, EndianFormat.DCBA
```

---

## 🏃 Running the Example Project

An example console application targeting **.NET Framework 4.7** is provided in the `example/` directory:

```bash
dotnet run --project example
```

This program connects to an IDEC FC6A PLC at `192.168.2.36:2101` and executes a 5-iteration loop reading all supported device addresses (`D0`, `I0.0`, `Q0.0`, `M0`, `R0`, `T0`, `TC0`, `TP0`, `C0`, `CC0`, `CP0`, `D0.0`) at 1-second intervals.

---

## 📦 Compatibility

- **.NET Standard 2.0**: Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5, .NET 6, .NET 7, .NET 8, .NET 9, and .NET 10.
- **.NET 6.0 & .NET 8.0**: Multi-targeted for optimal asynchronous socket performance.

---

## 📄 License

MIT License. Developed by Ismail Lowkey.