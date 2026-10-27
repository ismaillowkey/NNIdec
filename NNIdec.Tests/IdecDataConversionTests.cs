using System;
using Xunit;
using NNIdec.Core;

namespace NNIdec.Tests
{
    public class IdecDataConversionTests
    {
        [Fact]
        public void ResultPattern_SuccessAndFail_PropertiesMatchExpectation()
        {
            // Verifikasi Result pattern tanpa suffix V2: IsSuccess, Content, Data, ErrorCode, Message
            IdecResult<int> successResult = IdecResult<int>.Ok(12345, "Read successfully");
            Assert.True(successResult.IsSuccess);
            Assert.Equal(12345, successResult.Content);
            Assert.Equal(12345, successResult.Data);
            Assert.Equal(0, successResult.ErrorCode);
            Assert.Equal("Read successfully", successResult.Message);

            IdecResult<int> failResult = IdecResult<int>.Fail("Timeout error", -101);
            Assert.False(failResult.IsSuccess);
            Assert.Equal(0, failResult.Content);
            Assert.Equal(-101, failResult.ErrorCode);
            Assert.Equal("Timeout error", failResult.Message);

            IdecResult writeSuccess = IdecResult.Ok();
            Assert.True(writeSuccess.IsSuccess);

            IdecResult writeFail = IdecResult.Fail("PLC exception", 2);
            Assert.False(writeFail.IsSuccess);
            Assert.Equal(2, writeFail.ErrorCode);
        }

        [Fact]
        public void StringProcessing_NullTerminatorTruncation_WorksCorrectly()
        {
            // Mensimulasikan pembacaan string dengan sisa buffer kotor setelah karakter null (\0)
            string dirtyString = "HELLO\0RESIDUE_DATA";
            int nullIdx = dirtyString.IndexOf('\0');
            string cleanString = dirtyString;
            if (nullIdx >= 0)
            {
                cleanString = dirtyString.Substring(0, nullIdx);
            }
            Assert.Equal("HELLO", cleanString);
        }
    }
}