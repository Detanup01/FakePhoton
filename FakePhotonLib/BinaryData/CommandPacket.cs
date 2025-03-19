using System.Buffers.Binary;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FakePhotonLib.BinaryData;

public enum CommandType : byte
{
    None = 0,
    Ack = 1,
    Connect = 2,
    VerifyConnect = 3,
    Disconnect = 4,
    Ping = 5,
    SendReliable = 6,
    SendUnreliable = 7,
    SendFragment = 8,
    SendUnsequenced = 11,
    ServerTime = 12,
    SendUnreliableProcessed = 13,
    SendReliableUnsequenced = 14,
    SendFragmentUnsequenced = 15,
    AckUnsequenced = 16,
}

public class CommandPacket : IBinaryData
{
    public CommandType CommandType;
    public byte ChannelId;
    public byte CommandFlag;
    public byte Reserved = 4;
    public int Size;
    public int ReliableSequenceNumber;
    public int UnreliableSequenceNumber;
    public int UnsequencedGroupNumber;
    public int StartSequenceNumber;
    public int FragmentCount;
    public int FragmentNumber;
    public int TotalLength;
    public int FragmentOffset;
    public int AckReceivedReliableSequenceNumber;
    public int AckReceivedSentTime;
    public byte[]? PayLoad;

    //
    public short PeerId;

    public Type Type => typeof(CommandPacket);

    public void Read(BinaryReader reader)
    {
        CommandType = (CommandType)reader.ReadByte();
        ChannelId = reader.ReadByte();
        CommandFlag = reader.ReadByte();
        Reserved = reader.ReadByte();
        Size = reader.ReadInt32Big();
        ReliableSequenceNumber = reader.ReadInt32Big();
        int ShouldSetPayload = 0;
        switch (CommandType)
        {
            case CommandType.Ack:
            case CommandType.AckUnsequenced:
                AckReceivedReliableSequenceNumber = reader.ReadInt32Big();
                AckReceivedSentTime = reader.ReadInt32Big();
                break;
            case CommandType.VerifyConnect:
                PeerId = reader.ReadInt16Big();
                break;
            case CommandType.SendReliable:
            case CommandType.SendReliableUnsequenced:
                ShouldSetPayload = Size - 12;
                break;
            case CommandType.SendUnreliable:
                UnreliableSequenceNumber = reader.ReadInt32Big();
                ShouldSetPayload = Size - 17;
                break;
            case CommandType.SendFragment:
            case CommandType.SendFragmentUnsequenced:
                StartSequenceNumber = reader.ReadInt32Big();
                FragmentCount = reader.ReadInt32Big();
                FragmentNumber = reader.ReadInt32Big();
                TotalLength = reader.ReadInt32Big();
                FragmentOffset = reader.ReadInt32Big();
                ShouldSetPayload = Size - 32;
                break;
            case CommandType.SendUnsequenced:
                UnsequencedGroupNumber = reader.ReadInt32Big();
                ShouldSetPayload = Size - 16;
                break;
            default:
                ShouldSetPayload = Size - 12;
                break;
        }
        if (ShouldSetPayload != 0)
        {
            Console.WriteLine("ShouldSetPayload: " + ShouldSetPayload);
            PayLoad = reader.ReadBytes(ShouldSetPayload);
            Console.WriteLine("PayLoad: " + PayLoad.Length);
        }
    }

    public void Reset()
    {
        CommandType = CommandType.None;
        ChannelId = 0;
        CommandFlag = 0;
        Reserved = 4;
        Size = 0;
        ReliableSequenceNumber = 0;
        UnreliableSequenceNumber = 0;
        UnsequencedGroupNumber = 0;
        StartSequenceNumber = 0;
        FragmentCount = 0;
        FragmentNumber = 0;
        TotalLength = 0;
        FragmentOffset = 0;
        AckReceivedReliableSequenceNumber = 0;
        AckReceivedSentTime = 0;
        PayLoad = null;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)CommandType);
        writer.Write(ChannelId);
        writer.Write(CommandFlag);
        writer.Write(Reserved);
        writer.Write(BinaryPrimitives.ReverseEndianness(Size));
        writer.Write(BinaryPrimitives.ReverseEndianness(ReliableSequenceNumber));
        switch (CommandType)
        {
            case CommandType.SendUnreliable:
                writer.Write(BinaryPrimitives.ReverseEndianness(UnreliableSequenceNumber));
                break;
            case CommandType.SendUnsequenced:
                writer.Write(BinaryPrimitives.ReverseEndianness(UnsequencedGroupNumber));
                break;
            case CommandType.SendFragment:
            case CommandType.SendFragmentUnsequenced:
                writer.Write(BinaryPrimitives.ReverseEndianness(StartSequenceNumber));
                writer.Write(BinaryPrimitives.ReverseEndianness(FragmentCount));
                writer.Write(BinaryPrimitives.ReverseEndianness(FragmentNumber));
                writer.Write(BinaryPrimitives.ReverseEndianness(TotalLength));
                writer.Write(BinaryPrimitives.ReverseEndianness(FragmentOffset));
                break;
            default:
                break;
        }

        if (PayLoad != null)
            writer.Write(PayLoad);
    }

    public override string ToString()
    {
        string payload_size = PayLoad == null ? string.Empty : PayLoad.Count().ToString();
        return $"Type: {CommandType} Id: {ChannelId} Flag: {CommandFlag} Reserved: {Reserved} Size: {Size} RSN: {ReliableSequenceNumber} {payload_size}";
    }

    public object ParseToObject()
    {
        return this;
    }

    public object ReadToObject(BinaryReader reader)
    {
        this.Reset();
        this.Read(reader);
        return this;
    }
}
