using System;
using Xunit;
using NNIdec.Core;

namespace NNIdec.Tests
{
    public class IdecAddressTests
    {
        [Fact]
        public void Parse_DataRegister_ReturnsCorrectAddress()
        {
            IdecAddress addr = IdecAddress.Parse("D100", IdecPlcModel.FC6A);
            Assert.Equal(IdecDeviceType.DataRegister, addr.DeviceType);
            Assert.Equal(100, addr.AddressNumber);
            Assert.False(addr.IsBit);
        }

        [Fact]
        public void Parse_DataRegisterBit_ReturnsCorrectBitIndex()
        {
            IdecAddress addr = IdecAddress.Parse("D100.5", IdecPlcModel.FC6A);
            Assert.Equal(IdecDeviceType.DataRegisterBit, addr.DeviceType);
            Assert.Equal(100, addr.AddressNumber);
            Assert.Equal(5, addr.BitIndex);
            Assert.True(addr.IsBit);
        }

        [Fact]
        public void Parse_InputOctal_ConvertsToCorrectDecimal()
        {
            // Input I10 dalam oktal bernilai desimal 8
            IdecAddress addr = IdecAddress.Parse("I10", IdecPlcModel.FC6A);
            Assert.Equal(IdecDeviceType.Input, addr.DeviceType);
            Assert.Equal(8, addr.AddressNumber);
            Assert.True(addr.IsBit);
        }

        [Fact]
        public void Parse_OutputOctal_ConvertsToCorrectDecimal()
        {
            // Output Q17 dalam oktal bernilai desimal 15
            IdecAddress addr = IdecAddress.Parse("Q17", IdecPlcModel.FC6A);
            Assert.Equal(IdecDeviceType.Output, addr.DeviceType);
            Assert.Equal(15, addr.AddressNumber);
            Assert.True(addr.IsBit);
        }

        [Fact]
        public void Parse_InternalRelay_ReturnsCorrectAddress()
        {
            IdecAddress addr = IdecAddress.Parse("M200", IdecPlcModel.FC6A);
            Assert.Equal(IdecDeviceType.InternalRelay, addr.DeviceType);
            Assert.Equal(200, addr.AddressNumber);
            Assert.True(addr.IsBit);
        }

        [Fact]
        public void Parse_TimerAndCounter_ReturnsCorrectTypes()
        {
            IdecAddress t = IdecAddress.Parse("T5", IdecPlcModel.FC6A);
            IdecAddress tp = IdecAddress.Parse("TP5", IdecPlcModel.FC6A);
            IdecAddress tc = IdecAddress.Parse("TC5", IdecPlcModel.FC6A);

            Assert.Equal(IdecDeviceType.TimerContact, t.DeviceType);
            Assert.True(t.IsBit);

            Assert.Equal(IdecDeviceType.TimerPreset, tp.DeviceType);
            Assert.False(tp.IsBit);

            Assert.Equal(IdecDeviceType.TimerCurrent, tc.DeviceType);
            Assert.False(tc.IsBit);
        }

        [Fact]
        public void RangeValidation_FC6A_AllowsUpTo55999()
        {
            IdecAddress addr = IdecAddress.Parse("D55999", IdecPlcModel.FC6A);
            Assert.Equal(55999, addr.AddressNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                IdecAddress.Parse("D56000", IdecPlcModel.FC6A);
            });
        }

        [Fact]
        public void RangeValidation_MicroSmart_AllowsUpTo8199()
        {
            IdecAddress addr = IdecAddress.Parse("D8199", IdecPlcModel.MicroSmart);
            Assert.Equal(8199, addr.AddressNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                IdecAddress.Parse("D8200", IdecPlcModel.MicroSmart);
            });
        }

        [Fact]
        public void RangeValidation_Micro_AllowsUpTo999()
        {
            IdecAddress addr = IdecAddress.Parse("D999", IdecPlcModel.Micro);
            Assert.Equal(999, addr.AddressNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                IdecAddress.Parse("D1000", IdecPlcModel.Micro);
            });
        }
    }
}