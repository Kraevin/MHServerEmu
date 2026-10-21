using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Tests.Network
{
    public class ArchiveMessageBuilderRotationTests
    {
        [Theory]
        [InlineData(0f, 0f, 1.25f, true)]
        [InlineData(0f, 0f, -1.25f, true)]
        [InlineData(1.25f, 0f, 0f, false)]
        [InlineData(0f, 1.25f, 0f, true)]
        [InlineData(0.5f, 0.75f, 1.25f, true)]
        [InlineData(0f, 0f, 0f, false)]
        public void BuildLocomotionStateUpdateMessage_PreservesOrientation(float yaw, float pitch, float roll, bool fullOrientation)
        {
            Orientation orientation = new(yaw, pitch, roll);
            WorldEntity entity = CreateEntity(orientation);

            var message = ArchiveMessageBuilder.BuildLocomotionStateUpdateMessage(entity,
                ref LocomotionState.Null, ref LocomotionState.Null, false);

            AssertOrientation(message.ArchiveData, orientation, fullOrientation);
        }

        [Theory]
        [InlineData(0f, 0f, 1.25f, true)]
        [InlineData(0f, 0f, -1.25f, true)]
        [InlineData(1.25f, 0f, 0f, false)]
        [InlineData(0f, 1.25f, 0f, true)]
        [InlineData(0.5f, 0.75f, 1.25f, true)]
        [InlineData(0f, 0f, 0f, false)]
        public void BuildEntityEnterGameWorldMessage_PreservesOrientation(float yaw, float pitch, float roll, bool fullOrientation)
        {
            Orientation orientation = new(yaw, pitch, roll);
            WorldEntity entity = CreateEntity(orientation);

            var message = ArchiveMessageBuilder.BuildEntityEnterGameWorldMessage(entity);

            AssertOrientation(message.ArchiveData, orientation, fullOrientation);
        }

        private static WorldEntity CreateEntity(Orientation orientation)
        {
            // The builders only need a valid region reference and transform, not a
            // running game or prototype catalogs. Avoid initializing world simulation.
            WorldEntity entity = new(null);
            entity.RegionLocation.SetRegion(new Region(null));
            Assert.True(entity.RegionLocation.SetOrientation(orientation));
            return entity;
        }

        private static void AssertOrientation(ByteString data, Orientation expected, bool fullOrientation)
        {
            using Archive archive = new(ArchiveSerializeType.Replication, data);

            ulong entityId = 0;
            uint flags = 0;
            Vector3 position = Vector3.Zero;
            Orientation actual = Orientation.Zero;

            Assert.True(Serializer.Transfer(archive, ref entityId));
            Assert.True(Serializer.Transfer(archive, ref flags));
            Assert.Equal(fullOrientation, (flags & (uint)LocomotionMessageFlags.HasFullOrientation) != 0);
            Assert.True(Serializer.TransferVectorFixed(archive, ref position, 3));
            Assert.True(Serializer.TransferOrientationFixed(archive, ref actual, !fullOrientation, 6));

            Assert.False(archive.HasError);
            Assert.Equal(expected, actual);
        }
    }
}
