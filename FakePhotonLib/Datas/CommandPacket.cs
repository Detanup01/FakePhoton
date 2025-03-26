using Serilog;
using System.Security.Cryptography;

namespace FakePhotonLib.BinaryData;

public class CommandPacket
{
    internal byte CommandFlags;
    internal CommandType commandType;
    internal byte ChannelID;
    internal int ReliableSequenceNumber;
    internal int UnreliableSequenceNumber;
    internal int UnsequencedGroupNumber;
    internal byte ReservedByte = 4;
    internal int StartSequenceNumber;
    internal int FragmentCount;
    internal int FragmentNumber;
    internal int TotalLength;
    internal int FragmentOffset;
    internal int AckReceivedReliableSequenceNumber;
    internal int AckReceivedSentTime;
    internal int Size;
    internal byte[]? Payload;
    internal short? peerID;
    internal MessageAndCallback? messageAndCallback;
    public static byte[] ConnectPacket = [0x04, 0xB0, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x13, 0x88, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02];
    protected internal bool IsFlaggedUnsequenced => (CommandFlags & (int)CommandFlag.UNRELIABLE_UNSEQUENCED) > 0;
    protected internal bool IsFlaggedReliable => (CommandFlags & (int)CommandFlag.RELIABLE) > 0;
    public void Read(BinaryReader reader)
    {
        commandType = (CommandType)reader.ReadByte();
        ChannelID = reader.ReadByte();
        CommandFlags = reader.ReadByte();
        ReservedByte = reader.ReadByte();
        Size = reader.ReadInt32Big();
        ReliableSequenceNumber = reader.ReadInt32Big();
        int ShouldSetPayload = 0;
        switch (commandType)
        {
            case CommandType.Ack:
            case CommandType.AckUnsequenced:
                AckReceivedReliableSequenceNumber = reader.ReadInt32Big();
                AckReceivedSentTime = reader.ReadInt32Big();
                break;
            case CommandType.VerifyConnect:
                peerID = reader.ReadInt16Big();
                var connectPacket = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
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
            Payload = reader.ReadBytes(ShouldSetPayload);
            if (commandType == CommandType.Connect)
                Payload = null;
        }
    }

    public void Write(BinaryWriter writer)
    {
        short peerid = (short)RandomNumberGenerator.GetInt32(short.MaxValue);
        writer.Write((byte)commandType);
        writer.Write(ChannelID);
        writer.Write(CommandFlags);
        writer.Write(ReservedByte);
        writer.WriteInt32Big(Size);
        writer.WriteInt32Big(ReliableSequenceNumber);
        switch (commandType)
        {
            case CommandType.SendUnreliable:
                writer.WriteInt32Big(UnreliableSequenceNumber);
                break;
            case CommandType.SendUnsequenced:
                writer.WriteInt32Big(UnsequencedGroupNumber);
                break;
            case CommandType.SendFragment:
            case CommandType.SendFragmentUnsequenced:
                writer.WriteInt32Big(StartSequenceNumber);
                writer.WriteInt32Big(FragmentCount);
                writer.WriteInt32Big(FragmentNumber);
                writer.WriteInt32Big(TotalLength);
                writer.WriteInt32Big(FragmentOffset);
                break;
            case CommandType.Ack:
            case CommandType.AckUnsequenced:
                writer.WriteInt32Big(AckReceivedReliableSequenceNumber);
                writer.WriteInt32Big(AckReceivedSentTime);
                break;
            case CommandType.Connect:
                if (peerID.HasValue)
                {
                    peerid = peerID.Value;
                }
                writer.WriteInt16Big(peerid);
                writer.Write(ConnectPacket);
                break;
            case CommandType.VerifyConnect:
                if (peerID.HasValue)
                {
                    peerid = peerID.Value;
                }
                writer.WriteInt16Big(peerid);
                writer.Write(ConnectPacket);
                break;
            default:
                break;
        }

        if (Payload != null)
            writer.Write(Payload);
    }


    public override string ToString()
    {
        string payload_size = Payload == null ? string.Empty : Payload.Count().ToString();
        string ack_str = string.Empty;
        if (commandType == CommandType.Ack)
        {
            ack_str = $"ARSN: {AckReceivedReliableSequenceNumber} ARST: {AckReceivedSentTime:x2}";
        }
        return $"Type: {commandType} ChannelId: {ChannelID} Flag: {CommandFlags} Reserved: {ReservedByte} Size: {Size} RSN: {ReliableSequenceNumber} {ack_str} {payload_size}";
    }
}


public enum CommandFlag
{
    UNRELIABLE,
    RELIABLE,
    UNRELIABLE_UNSEQUENCED,
    RELIBALE_UNSEQUENCED
}

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