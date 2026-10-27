using Xunit;
using NNIdec;
using NNIdec.Core;
using NNIdec.Interfaces;

namespace NNIdec.Tests
{
    public class IdecClientModelTests
    {
        [Fact]
        public void PolymorphicClientInstantiation_WorksAsRequested()
        {
            // Memastikan sintaks polimorfik yang diminta Paduka Raja.ST berfungsi 100%
            NNIdec micro = new IdecMicro("192.168.0.10");
            NNIdec microsmart = new IdecMicroSmart("192.168.0.11");
            NNIdec fc6a = new IdecMicroSmartFC6A("192.168.0.12");

            Assert.Equal(IdecPlcModel.Micro, micro.Model);
            Assert.Equal(2101, micro.Port);

            Assert.Equal(IdecPlcModel.MicroSmart, microsmart.Model);
            Assert.Equal(502, microsmart.Port);

            Assert.Equal(IdecPlcModel.FC6A, fc6a.Model);
            Assert.Equal(502, fc6a.Port);
        }

        [Fact]
        public void InterfaceAssignment_Works()
        {
            IIdecClient client = new IdecMicroSmartFC6A("192.168.0.50");
            Assert.Equal(IdecPlcModel.FC6A, client.Model);
            Assert.False(client.IsConnected);
        }
    }
}