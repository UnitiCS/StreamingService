// StreamingService.Common/Network/StreamPacket.cs
using System.Net.Sockets;

public class StreamPacket
{
    public const int HeaderSize = 9; // 1 байт тип + 4 байта размер + 4 байта sequence
    public byte PacketType { get; set; }
    public int DataSize { get; set; }
    public int SequenceNumber { get; set; }
    public byte[] Data { get; set; }

    public byte[] ToBytes()
    {
        var result = new byte[HeaderSize + DataSize];
        result[0] = PacketType;
        BitConverter.GetBytes(DataSize).CopyTo(result, 1);
        BitConverter.GetBytes(SequenceNumber).CopyTo(result, 5);
        Data.CopyTo(result, HeaderSize);
        return result;
    }

    public static async Task<StreamPacket> ReadFromStreamAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[HeaderSize];
        var bytesRead = 0;
        while (bytesRead < HeaderSize)
        {
            var read = await stream.ReadAsync(headerBuffer.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
            if (read == 0) throw new EndOfStreamException();
            bytesRead += read;
        }

        var packet = new StreamPacket
        {
            PacketType = headerBuffer[0],
            DataSize = BitConverter.ToInt32(headerBuffer, 1),
            SequenceNumber = BitConverter.ToInt32(headerBuffer, 5)
        };

        if (packet.DataSize <= 0 || packet.DataSize > 1024 * 1024 * 10)
            throw new InvalidDataException($"Invalid packet size: {packet.DataSize}");

        packet.Data = new byte[packet.DataSize];
        bytesRead = 0;
        while (bytesRead < packet.DataSize)
        {
            var read = await stream.ReadAsync(packet.Data.AsMemory(bytesRead, packet.DataSize - bytesRead), cancellationToken);
            if (read == 0) throw new EndOfStreamException();
            bytesRead += read;
        }

        return packet;
    }
}