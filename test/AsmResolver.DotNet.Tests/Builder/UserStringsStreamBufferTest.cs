using System.Text;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.DotNet.Metadata.UserStrings;
using Xunit;

namespace AsmResolver.DotNet.Tests.Builder
{
    public class UserStringsStreamBufferTest
    {
        [Fact]
        public void AddDistinct()
        {
            var buffer = new UserStringsStreamBuffer();

            const string string1 = "String 1";
            uint index1 = buffer.GetStringIndex(string1);

            const string string2 = "String 2";
            uint index2 = buffer.GetStringIndex(string2);

            Assert.NotEqual(index1, index2);
            
            var usStream = (UserStringsStream) buffer.CreateStream();
            Assert.Equal(string1, usStream.GetStringByIndex(index1));
            Assert.Equal(string2, usStream.GetStringByIndex(index2));
        }

        [Fact]
        public void AddDuplicate()
        {
            var buffer = new UserStringsStreamBuffer();

            const string string1 = "String 1";
            uint index1 = buffer.GetStringIndex(string1);

            const string string2 = "String 1";
            uint index2 = buffer.GetStringIndex(string2);

            Assert.Equal(index1, index2);
            
            var usStream = (UserStringsStream) buffer.CreateStream();
            Assert.Equal(string1, usStream.GetStringByIndex(index1));
        }

        [Fact]
        public void AddRaw()
        {
            var buffer = new UserStringsStreamBuffer();

            const string string1 = "String 1";
            var rawData = Encoding.UTF8.GetBytes(string1);

            uint index1 = buffer.AppendRawData(rawData);
            uint index2 = buffer.GetStringIndex(string1);

            Assert.NotEqual(index1, index2);

            var usStream = (UserStringsStream) buffer.CreateStream();
            Assert.Equal(string1, usStream.GetStringByIndex(index2));
        }

        [Theory]
        [InlineData('\x00', 0)]
        [InlineData('\x01', 1)]
        [InlineData('\x08', 1)]
        [InlineData('\x09', 0)]
        [InlineData('\x0E', 1)]
        [InlineData('\x1F', 1)]
        [InlineData('\x26', 0)]
        [InlineData('\x27', 1)]
        [InlineData('\x2D', 1)]
        [InlineData('A', 0)]
        [InlineData('\x7F', 1)]
        [InlineData('\u3910', 1)]
        public void SpecialCharactersTerminatorByte(char specialChar, byte terminatorByte)
        {
            string value = "My String" + specialChar;
            
            var buffer = new UserStringsStreamBuffer();
            uint index = buffer.GetStringIndex(value);
            var usStream = (UserStringsStream) buffer.CreateStream();

            Assert.Equal(value, usStream.GetStringByIndex(index));
            
            var reader = usStream.CreateReader();
            reader.FileOffset = (uint) (index + Encoding.Unicode.GetByteCount(value) + 1);
            byte b = reader.ReadByte();
            
            Assert.Equal(terminatorByte, b);
        }
    }
}