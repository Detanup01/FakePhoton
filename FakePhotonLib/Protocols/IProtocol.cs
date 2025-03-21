using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated;

namespace FakePhotonLib.Protocols;

public abstract class IProtocol
{
    public readonly ByteArraySlicePool ByteArraySlicePool = new();

    public abstract string ProtocolType { get; }

    public abstract byte[] VersionBytes { get; }

    public abstract void Serialize(StreamBuffer dout, object serObject, bool setType);

    public abstract void SerializeShort(StreamBuffer dout, short serObject, bool setType);

    public abstract void SerializeString(StreamBuffer dout, string serObject, bool setType);

    public abstract void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType);

    public abstract void SerializeOperationRequest(
      StreamBuffer stream,
      byte operationCode,
      Dictionary<byte, object?>? parameters,
      bool setType);

    public abstract void SerializeOperationResponse(
      StreamBuffer stream,
      OperationResponse serObject,
      bool setType);

    public abstract object? Deserialize(
      StreamBuffer din,
      byte type,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract short DeserializeShort(StreamBuffer din);

    public abstract byte DeserializeByte(StreamBuffer din);

    public abstract EventData DeserializeEventData(
      StreamBuffer din,
      EventData? target = null,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract OperationRequest DeserializeOperationRequest(
      StreamBuffer din,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract OperationResponse DeserializeOperationResponse(
      StreamBuffer stream,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream);

    public byte[] Serialize(object obj)
    {
        StreamBuffer dout = new(64);
        Serialize(dout, obj, true);
        return dout.ToArray();
    }

    public object? Deserialize(StreamBuffer stream) => Deserialize(stream, stream.ReadByte());

    public object? Deserialize(byte[] serializedData)
    {
        StreamBuffer din = new(serializedData);
        return Deserialize(din, din.ReadByte());
    }

    public object? DeserializeMessage(StreamBuffer stream)
    {
        return Deserialize(stream, stream.ReadByte());
    }

    internal void SerializeMessage(StreamBuffer ms, object msg) => Serialize(ms, msg, true);

    public enum DeserializationFlags
    {
        None,
        AllowPooledByteArray,
        WrapIncomingStructs,
    }
}