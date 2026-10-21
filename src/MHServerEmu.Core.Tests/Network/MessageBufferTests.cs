using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Core.Tests.Network
{
    public class MessageBufferTests
    {
        static MessageBufferTests()
        {
            ProtocolDispatchTable.Instance.Initialize();
        }

        [Fact]
        public void Deserialize_FragmentedMessage_ReadsCompletePayload()
        {
            LoginDataPB login = LoginDataPB.CreateBuilder()
                .SetEmailAddress("test@example.invalid")
                .SetPassword("test-password")
                .SetVersion("1.53.0.203")
                .Build();

            using ShortReadStream stream = new(Encode(login));
            MessageBuffer buffer = new(stream);

            Assert.Equal(login, buffer.Deserialize<FrontendProtocolMessage>());
        }

        [Fact]
        public void Constructor_TruncatedMessage_RejectsIncompleteFrame()
        {
            LoginDataPB login = LoginDataPB.CreateBuilder()
                .SetEmailAddress("test@example.invalid")
                .SetPassword("test-password")
                .Build();

            using MemoryStream stream = new(Encode(login)[..^1]);
            MessageBuffer buffer = new(stream);

            Assert.Equal(MessageBuffer.InvalidMessageId, buffer.MessageId);
        }

        [Fact]
        public void Constructor_OversizedMessage_RejectsFrameBeforeReadingPayload()
        {
            using MemoryStream stream = new();
            CodedOutputStream output = CodedOutputStream.CreateInstance(stream);
            output.WriteRawVarint32((uint)FrontendProtocolMessage.LoginDataPB);
            output.WriteRawVarint32(MessageBuffer.MaxSize + 1);
            output.Flush();
            stream.Position = 0;

            MessageBuffer buffer = new(stream);

            Assert.Equal(MessageBuffer.InvalidMessageId, buffer.MessageId);
        }

        private static byte[] Encode(IMessage message)
        {
            using MemoryStream stream = new();
            CodedOutputStream output = CodedOutputStream.CreateInstance(stream);
            new MessagePackageOut(message).WriteTo(output);
            output.Flush();
            return stream.ToArray();
        }

        private sealed class ShortReadStream(byte[] data) : MemoryStream(data)
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, 1));
            }

            public override int Read(Span<byte> buffer)
            {
                return base.Read(buffer[..Math.Min(buffer.Length, 1)]);
            }
        }
    }
}
