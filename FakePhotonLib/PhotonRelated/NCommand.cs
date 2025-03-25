using FakePhotonLib.Protocols;
using Serilog;

namespace FakePhotonLib.PhotonRelated;

public class NCommand : IComparable<NCommand>
{
    internal const int HEADER_UDP_PACK_LENGTH = 12;
    internal byte commandFlags;
    internal CommandType commandType;
    internal byte commandChannelID;
    internal int reliableSequenceNumber;
    internal int unreliableSequenceNumber;
    internal int unsequencedGroupNumber;
    internal byte reservedByte = 4;
    internal int startSequenceNumber;
    internal int fragmentCount;
    internal int fragmentNumber;
    internal int totalLength;
    internal int fragmentOffset;
    internal int fragmentsRemaining;
    internal int commandSentTime;
    internal byte commandSentCount;
    internal int roundTripTimeout;
    internal int timeoutTime;
    internal int ackReceivedReliableSequenceNumber;
    internal int ackReceivedSentTime;
    internal int Size;
    internal StreamBuffer? Payload;
    internal NCommandPool? returnPool;
    internal int peerID;

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

    /*
    public enum CommandSize
    {
        Minimum = 12,
        Ack = 20,
        Connect = 44,
        VerifyConnect = 44,
        Disconnect = 12,
        Ping = 12,
        Reliable = 12,
        Unreliable = 16,
        Unsequenced = 16,
        Fragment = 32,
        Max = 36
    }
    */

    protected internal int SizeOfPayload => Payload?.Length ?? 0;

    protected internal bool IsFlaggedUnsequenced => (commandFlags & (int)CommandFlag.UNRELIABLE_UNSEQUENCED) > 0;

    protected internal bool IsFlaggedReliable => (commandFlags & (int)CommandFlag.RELIABLE) > 0;

    internal static void CreateAck(byte[] buffer, int offset, NCommand commandToAck, int sentTime)
    {
        buffer[offset++] = commandToAck.IsFlaggedUnsequenced ? (byte)CommandType.AckUnsequenced : (byte)CommandType.Ack;
        buffer[offset++] = commandToAck.commandChannelID;
        buffer[offset++] = 0;
        buffer[offset++] = 4;
        Protocol.Serialize(20, buffer, ref offset);
        Protocol.Serialize(0, buffer, ref offset);
        Protocol.Serialize(commandToAck.reliableSequenceNumber, buffer, ref offset);
        Protocol.Serialize(sentTime, buffer, ref offset);
    }

    internal NCommand(CommandType commandType, StreamBuffer payload, byte channel)
    {
        Initialize(commandType, payload, channel);
    }

    internal void Initialize(CommandType commandType, StreamBuffer payload, byte channel)
    {
        this.commandType = commandType;
        commandFlags = 1;
        commandChannelID = channel;
        Payload = payload;
        Size = commandType switch
        {
            CommandType.Connect => InitializeConnect(),
            CommandType.Disconnect => 12,
            CommandType.SendReliable => 12 + payload.Length,
            CommandType.SendUnreliable => InitializeUnreliable(payload),
            CommandType.SendFragment => 32 + payload.Length,
            CommandType.SendUnsequenced => InitializeUnsequenced(payload),
            CommandType.SendReliableUnsequenced => 12 + payload.Length,
            CommandType.SendFragmentUnsequenced => 32 + payload.Length,
            _ => 12
        };
    }

    private int InitializeConnect()
    {
        byte[] numArray = new byte[32];
        numArray[0] = 0;
        numArray[1] = 0;
        int targetOffset = 2;
        Protocol.Serialize((short)1200, numArray, ref targetOffset);
        numArray[4] = 0;
        numArray[5] = 0;
        numArray[6] = 128;
        numArray[7] = 0;
        numArray[11] = 2; // Channel Count
        numArray[15] = 0;
        numArray[19] = 0;
        numArray[22] = 19;
        numArray[23] = 136;
        numArray[27] = 2;
        numArray[31] = 2;
        Payload = new StreamBuffer(numArray);
        return 44;
    }

    private int InitializeUnreliable(StreamBuffer payload)
    {
        commandFlags = 0;
        return 16 + payload.Length;
    }

    private int InitializeUnsequenced(StreamBuffer payload)
    {
        commandFlags = 2;
        return 16 + payload.Length;
    }

    internal NCommand(byte[] inBuff, ref int readingOffset)
    {
        this.Initialize(inBuff, ref readingOffset);
    }

    internal void Initialize(byte[] inBuff, ref int readingOffset)
    {
        commandType = (CommandType)inBuff[readingOffset++];
        commandChannelID = inBuff[readingOffset++];
        commandFlags = inBuff[readingOffset++];
        reservedByte = inBuff[readingOffset++];
        Protocol.Deserialize(out Size, inBuff, ref readingOffset);
        Protocol.Deserialize(out reliableSequenceNumber, inBuff, ref readingOffset);

        int count = commandType switch
        {
            CommandType.Ack or CommandType.AckUnsequenced => InitializeAck(inBuff, ref readingOffset),
            CommandType.VerifyConnect => InitializeVerifyConnect(inBuff, ref readingOffset),
            CommandType.SendReliable or CommandType.SendReliableUnsequenced => Size - 12,
            CommandType.SendUnreliable => InitializeUnreliable(inBuff, ref readingOffset),
            CommandType.SendFragment or CommandType.SendFragmentUnsequenced => InitializeFragment(inBuff, ref readingOffset),
            CommandType.SendUnsequenced => InitializeSendUnsequenced(inBuff, ref readingOffset),
            _ => readingOffset += Size - 12
        };

        if (count == 0) 
            return;
        Log.Information("{reading}, {count}", readingOffset, count);
        if (readingOffset == count)
            return;
        Payload = new StreamBuffer(count);
        Payload.Write(inBuff, readingOffset, count);
        Payload.Position = 0;
        readingOffset += count;
    }

    private int InitializeAck(byte[] inBuff, ref int readingOffset)
    {
        Protocol.Deserialize(out ackReceivedReliableSequenceNumber, inBuff, ref readingOffset);
        Protocol.Deserialize(out ackReceivedSentTime, inBuff, ref readingOffset);
        return 0;
    }

    private int InitializeVerifyConnect(byte[] inBuff, ref int readingOffset)
    {
        Protocol.Deserialize(out short num, inBuff, ref readingOffset);
        readingOffset += 30;
        peerID = num;
        return 0;
    }

    private int InitializeUnreliable(byte[] inBuff, ref int readingOffset)
    {
        Protocol.Deserialize(out unreliableSequenceNumber, inBuff, ref readingOffset);
        return Size - 16;
    }

    private int InitializeFragment(byte[] inBuff, ref int readingOffset)
    {
        Protocol.Deserialize(out startSequenceNumber, inBuff, ref readingOffset);
        Protocol.Deserialize(out fragmentCount, inBuff, ref readingOffset);
        Protocol.Deserialize(out fragmentNumber, inBuff, ref readingOffset);
        Protocol.Deserialize(out totalLength, inBuff, ref readingOffset);
        Protocol.Deserialize(out fragmentOffset, inBuff, ref readingOffset);
        fragmentsRemaining = fragmentCount;
        return Size - 32;
    }

    private int InitializeSendUnsequenced(byte[] inBuff, ref int readingOffset)
    {
        Protocol.Deserialize(out unsequencedGroupNumber, inBuff, ref readingOffset);
        return Size - 16;
    }

    public void Reset()
    {
        this.commandFlags = 0;
        this.commandType = 0;
        this.commandChannelID = 0;
        this.reliableSequenceNumber = 0;
        this.unreliableSequenceNumber = 0;
        this.unsequencedGroupNumber = 0;
        this.reservedByte = 4;
        this.startSequenceNumber = 0;
        this.fragmentCount = 0;
        this.fragmentNumber = 0;
        this.totalLength = 0;
        this.fragmentOffset = 0;
        this.fragmentsRemaining = 0;
        this.commandSentTime = 0;
        this.commandSentCount = 0;
        this.roundTripTimeout = 0;
        this.timeoutTime = 0;
        this.ackReceivedReliableSequenceNumber = 0;
        this.ackReceivedSentTime = 0;
        this.Size = 0;
    }

    internal void SerializeHeader(byte[] buffer, ref int bufferIndex)
    {
        buffer[bufferIndex++] = (byte)commandType;
        buffer[bufferIndex++] = commandChannelID;
        buffer[bufferIndex++] = commandFlags;
        buffer[bufferIndex++] = reservedByte;
        Protocol.Serialize(Size, buffer, ref bufferIndex);
        Protocol.Serialize(reliableSequenceNumber, buffer, ref bufferIndex);
        if (commandType == CommandType.SendUnreliable)
            Protocol.Serialize(unreliableSequenceNumber, buffer, ref bufferIndex);
        if (commandType == CommandType.SendUnsequenced)
        {
            Protocol.Serialize(unsequencedGroupNumber, buffer, ref bufferIndex);
        }
        else if (commandType is CommandType.SendFragment or CommandType.SendFragmentUnsequenced)
        {
            Protocol.Serialize(startSequenceNumber, buffer, ref bufferIndex);
            Protocol.Serialize(fragmentCount, buffer, ref bufferIndex);
            Protocol.Serialize(fragmentNumber, buffer, ref bufferIndex);
            Protocol.Serialize(totalLength, buffer, ref bufferIndex);
            Protocol.Serialize(fragmentOffset, buffer, ref bufferIndex);
        }
    }

    internal byte[] Serialize() => Payload == null ? [] : Payload.GetBuffer();

    public void FreePayload()
    {
        Payload?.Flush();
        Payload = null;
    }

    public void Release() => returnPool?.Release(this);

    public int CompareTo(NCommand? other)
    {
        if (other == null) return 1;
        int num = this.reliableSequenceNumber - other.reliableSequenceNumber;
        return this.IsFlaggedReliable || num != 0 ? num : this.unreliableSequenceNumber - other.unreliableSequenceNumber;
    }

    public override string ToString() =>
            commandType is CommandType.Ack or CommandType.AckUnsequenced
                ? $"CMD({commandChannelID} ack for ch#/sq#/time: {reliableSequenceNumber}/{ackReceivedSentTime})"
                : $"CMD({commandType} ch#/sq#/time: {commandChannelID}/{reliableSequenceNumber}/{commandSentTime})";
}