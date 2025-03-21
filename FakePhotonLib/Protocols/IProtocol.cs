using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated;

namespace FakePhotonLib.Protocols;

public abstract class IProtocol
{
    public readonly ByteArraySlicePool ByteArraySlicePool = new();

    public abstract string ProtocolType { get; }

    public abstract byte[] VersionBytes { get; }

    public abstract void Serialize(BinaryWriter dout, object serObject, bool setType);

    public abstract void SerializeShort(BinaryWriter dout, short serObject, bool setType);

    public abstract void SerializeString(BinaryWriter dout, string serObject, bool setType);

    public abstract void SerializeEventData(BinaryWriter stream, EventData serObject, bool setType);

    public abstract void SerializeOperationRequest(
      BinaryWriter stream,
      byte operationCode,
      Dictionary<byte, object?>? parameters,
      bool setType);

    public abstract void SerializeOperationResponse(
      BinaryWriter stream,
      OperationResponse serObject,
      bool setType);

    public abstract object? Deserialize(
      BinaryReader din,
      byte type,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract short DeserializeShort(BinaryReader din);

    public abstract byte DeserializeByte(BinaryReader din);

    public abstract EventData DeserializeEventData(
      BinaryReader din,
      EventData? target = null,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract OperationRequest DeserializeOperationRequest(
      BinaryReader din,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract OperationResponse DeserializeOperationResponse(
      BinaryReader stream,
      DeserializationFlags flags = DeserializationFlags.None);

    public abstract DisconnectMessage DeserializeDisconnectMessage(BinaryReader stream);

    public byte[] Serialize(object obj)
    {
        using MemoryStream ms = new(64);
        using BinaryWriter dout = new(ms);
        Serialize(dout, obj, true);
        return ms.ToArray();
    }

    public object? Deserialize(BinaryReader stream) => Deserialize(stream, stream.ReadByte());

    public object? Deserialize(byte[] serializedData)
    {
        MemoryStream din = new(serializedData);
        BinaryReader reader = new BinaryReader(din);
        return Deserialize(reader, reader.ReadByte());
    }

    public object? DeserializeMessage(BinaryReader stream)
    {
        return Deserialize(stream, stream.ReadByte());
    }

    internal void SerializeMessage(BinaryWriter ms, object msg) => Serialize(ms, msg, true);

    public enum DeserializationFlags
    {
        None,
        AllowPooledByteArray,
        WrapIncomingStructs,
    }
}