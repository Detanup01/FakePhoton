using FakePhotonLib.BinaryData;
using FakePhotonLib.Protocols;

namespace FakePhotonLib.PhotonRelated;

public class NCommand : IComparable<NCommand>
{
    internal const byte FV_UNRELIABLE = 0;
    internal const byte FV_RELIABLE = 1;
    internal const byte FV_UNRELIABLE_UNSEQUENCED = 2;
    internal const byte FV_RELIBALE_UNSEQUENCED = 3;
    internal const int HEADER_UDP_PACK_LENGTH = 12;
    internal const int CmdSizeMinimum = 12;
    internal const int CmdSizeAck = 20;
    internal const int CmdSizeConnect = 44;
    internal const int CmdSizeVerifyConnect = 44;
    internal const int CmdSizeDisconnect = 12;
    internal const int CmdSizePing = 12;
    internal const int CmdSizeReliableHeader = 12;
    internal const int CmdSizeUnreliableHeader = 16;
    internal const int CmdSizeUnsequensedHeader = 16;
    internal const int CmdSizeFragmentHeader = 32;
    internal const int CmdSizeMaxHeader = 36;
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
    internal StreamBuffer Payload;
    internal NCommandPool returnPool;
    internal int peerID;
    internal MessageAndCallback messageAndCallback;

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

    protected internal int SizeOfPayload => this.Payload != null ? this.Payload.Length : 0;

    protected internal bool IsFlaggedUnsequenced => ((int)this.commandFlags & 2) > 0;

    protected internal bool IsFlaggedReliable => ((int)this.commandFlags & 1) > 0;

    internal static void CreateAck(byte[] buffer, int offset, NCommand commandToAck, int sentTime)
    {
        buffer[offset++] = commandToAck.IsFlaggedUnsequenced ? (byte)16 : (byte)1;
        buffer[offset++] = commandToAck.commandChannelID;
        buffer[offset++] = (byte)0;
        buffer[offset++] = (byte)4;
        Protocol.Serialize(20, buffer, ref offset);
        Protocol.Serialize(0, buffer, ref offset);
        Protocol.Serialize(commandToAck.reliableSequenceNumber, buffer, ref offset);
        Protocol.Serialize(sentTime, buffer, ref offset);
    }

    internal NCommand(CommandType commandType, StreamBuffer payload, byte channel)
    {
        this.Initialize(commandType, payload, channel);
    }

    internal void Initialize(CommandType commandType, StreamBuffer payload, byte channel)
    {
        this.commandType = commandType;
        this.commandFlags = (byte)1;
        this.commandChannelID = channel;
        this.Payload = payload;
        this.Size = 12;
        switch (this.commandType)
        {
            case CommandType.Connect:
                this.Size = 44;
                byte[] numArray = new byte[32];
                numArray[0] = (byte)0;
                numArray[1] = (byte)0;
                int targetOffset = 2;
                Protocol.Serialize((short)1200, numArray, ref targetOffset);
                numArray[4] = (byte)0;
                numArray[5] = (byte)0;
                numArray[6] = (byte)128;
                numArray[7] = (byte)0;
                numArray[11] = 2; // Channel Count
                numArray[15] = (byte)0;
                numArray[19] = (byte)0;
                numArray[22] = (byte)19;
                numArray[23] = (byte)136;
                numArray[27] = (byte)2;
                numArray[31] = (byte)2;
                this.Payload = new StreamBuffer(numArray);
                break;
            case CommandType.Disconnect:
                this.Size = 12;
                this.commandFlags = (byte)2;
                this.reservedByte = (byte)4;
                break;
            case CommandType.SendReliable:
                this.Size = 12 + payload.Length;
                break;
            case CommandType.SendUnreliable:
                this.Size = 16 + payload.Length;
                this.commandFlags = (byte)0;
                break;
            case CommandType.SendFragment:
                this.Size = 32 + payload.Length;
                break;
            case CommandType.SendUnsequenced:
                this.Size = 16 + payload.Length;
                this.commandFlags = (byte)2;
                break;
            case CommandType.SendReliableUnsequenced:
                this.Size = 12 + payload.Length;
                this.commandFlags = (byte)3;
                break;
            case CommandType.SendFragmentUnsequenced:
                this.Size = 32 + payload.Length;
                this.commandFlags = (byte)3;
                break;
        }
    }

    internal NCommand(byte[] inBuff, ref int readingOffset)
    {
        this.Initialize(inBuff, ref readingOffset);
    }

    internal void Initialize(byte[] inBuff, ref int readingOffset)
    {
        this.commandType = (CommandType)inBuff[readingOffset++];
        this.commandChannelID = inBuff[readingOffset++];
        this.commandFlags = inBuff[readingOffset++];
        this.reservedByte = inBuff[readingOffset++];
        Protocol.Deserialize(out this.Size, inBuff, ref readingOffset);
        Protocol.Deserialize(out this.reliableSequenceNumber, inBuff, ref readingOffset);
        int count = 0;
        switch (this.commandType)
        {
            case CommandType.Ack:
            case CommandType.AckUnsequenced:
                Protocol.Deserialize(out this.ackReceivedReliableSequenceNumber, inBuff, ref readingOffset);
                Protocol.Deserialize(out this.ackReceivedSentTime, inBuff, ref readingOffset);
                break;
            case CommandType.VerifyConnect:
                short num;
                Protocol.Deserialize(out num, inBuff, ref readingOffset);
                readingOffset += 30;
                peerID = num;
                break;
            case CommandType.SendReliable:
            case CommandType.SendReliableUnsequenced:
                count = this.Size - 12;
                break;
            case CommandType.SendUnreliable:
                Protocol.Deserialize(out this.unreliableSequenceNumber, inBuff, ref readingOffset);
                count = this.Size - 16;
                break;
            case CommandType.SendFragment:
            case CommandType.SendFragmentUnsequenced:
                Protocol.Deserialize(out this.startSequenceNumber, inBuff, ref readingOffset);
                Protocol.Deserialize(out this.fragmentCount, inBuff, ref readingOffset);
                Protocol.Deserialize(out this.fragmentNumber, inBuff, ref readingOffset);
                Protocol.Deserialize(out this.totalLength, inBuff, ref readingOffset);
                Protocol.Deserialize(out this.fragmentOffset, inBuff, ref readingOffset);
                count = this.Size - 32;
                this.fragmentsRemaining = this.fragmentCount;
                break;
            case CommandType.SendUnsequenced:
                Protocol.Deserialize(out this.unsequencedGroupNumber, inBuff, ref readingOffset);
                count = this.Size - 16;
                break;
            default:
                readingOffset += this.Size - 12;
                break;
        }
        if (count == 0)
            return;
        StreamBuffer streamBuffer = new();
        streamBuffer.Write(inBuff, readingOffset, count);
        this.Payload = streamBuffer;
        this.Payload.Position = 0;
        readingOffset += count;
    }

    public void Reset()
    {
        this.commandFlags = (byte)0;
        this.commandType = (byte)0;
        this.commandChannelID = (byte)0;
        this.reliableSequenceNumber = 0;
        this.unreliableSequenceNumber = 0;
        this.unsequencedGroupNumber = 0;
        this.reservedByte = (byte)4;
        this.startSequenceNumber = 0;
        this.fragmentCount = 0;
        this.fragmentNumber = 0;
        this.totalLength = 0;
        this.fragmentOffset = 0;
        this.fragmentsRemaining = 0;
        this.commandSentTime = 0;
        this.commandSentCount = (byte)0;
        this.roundTripTimeout = 0;
        this.timeoutTime = 0;
        this.ackReceivedReliableSequenceNumber = 0;
        this.ackReceivedSentTime = 0;
        this.Size = 0;
    }

    internal void SerializeHeader(byte[] buffer, ref int bufferIndex)
    {
        buffer[bufferIndex++] = (byte)this.commandType;
        buffer[bufferIndex++] = this.commandChannelID;
        buffer[bufferIndex++] = this.commandFlags;
        buffer[bufferIndex++] = this.reservedByte;
        Protocol.Serialize(this.Size, buffer, ref bufferIndex);
        Protocol.Serialize(this.reliableSequenceNumber, buffer, ref bufferIndex);
        if (this.commandType == CommandType.SendUnreliable)
            Protocol.Serialize(this.unreliableSequenceNumber, buffer, ref bufferIndex);
        if (this.commandType ==  CommandType.SendUnsequenced)
        {
            Protocol.Serialize(this.unsequencedGroupNumber, buffer, ref bufferIndex);
        }
        else
        {
            if (this.commandType !=  CommandType.SendFragment && this.commandType !=  CommandType.SendFragmentUnsequenced)
                return;
            Protocol.Serialize(this.startSequenceNumber, buffer, ref bufferIndex);
            Protocol.Serialize(this.fragmentCount, buffer, ref bufferIndex);
            Protocol.Serialize(this.fragmentNumber, buffer, ref bufferIndex);
            Protocol.Serialize(this.totalLength, buffer, ref bufferIndex);
            Protocol.Serialize(this.fragmentOffset, buffer, ref bufferIndex);
        }
    }

    internal byte[] Serialize() => this.Payload.GetBuffer();

    public void FreePayload()
    {
        if (this.Payload != null)
            this.Payload.Flush();
        this.Payload = (StreamBuffer)null;
    }

    public void Release() => this.returnPool.Release(this);

    public int CompareTo(NCommand other)
    {
        if (other == null)
            return 1;
        int num = this.reliableSequenceNumber - other.reliableSequenceNumber;
        return this.IsFlaggedReliable || num != 0 ? num : this.unreliableSequenceNumber - other.unreliableSequenceNumber;
    }

    public override string ToString()
    {
        return this.commandType ==  CommandType.Ack || this.commandType == CommandType.AckUnsequenced ? string.Format("CMD({1} ack for ch#/sq#/time: {0}/{2}/{3})", (object)this.commandChannelID, (object)this.commandType, (object)this.ackReceivedReliableSequenceNumber, (object)this.ackReceivedSentTime) : string.Format("CMD({1} ch#/sq#/usq#: {0}/{2}/{3} r#/st/tt:{5}/{4}/{6})", (object)this.commandChannelID, (object)this.commandType, (object)this.reliableSequenceNumber, (object)this.unreliableSequenceNumber, (object)this.commandSentTime, (object)this.commandSentCount, (object)this.timeoutTime);
    }
}