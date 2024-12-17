namespace StreamingService.Common.Models
{
    public class NetworkPacket
    {
        public PacketType Type { get; set; }
        public byte[] Data { get; set; }
        public int Length => Data?.Length ?? 0;

        public NetworkPacket(PacketType type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)Type);
                writer.Write(Length);
                if (Data != null)
                {
                    writer.Write(Data);
                }
                return ms.ToArray();
            }
        }

        public static NetworkPacket Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                var type = (PacketType)reader.ReadByte();
                var length = reader.ReadInt32();
                var payload = reader.ReadBytes(length);
                return new NetworkPacket(type, payload);
            }
        }
    }

    public enum PacketType : byte
    {
        Video = 0,
        Audio = 1,
        Control = 2
    }
}