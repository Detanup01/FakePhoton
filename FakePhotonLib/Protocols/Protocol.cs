using FakePhotonLib.PhotonRelated;

namespace FakePhotonLib.Protocols;

public class Protocol
{
    internal static readonly Dictionary<Type, CustomType> TypeDict = new Dictionary<Type, CustomType>();
    internal static readonly Dictionary<byte, CustomType> CodeDict = new Dictionary<byte, CustomType>();
    public static IProtocol ProtocolDefault = new Protocol18();
    private static readonly float[] memFloatBlock = new float[1];
    private static readonly byte[] memDeserialize = new byte[4];

    public static bool TryRegisterType(
      Type type,
      byte typeCode,
      SerializeMethod serializeFunction,
      DeserializeMethod deserializeFunction)
    {
        if (Protocol.CodeDict.ContainsKey(typeCode) || Protocol.TypeDict.ContainsKey(type))
            return false;
        CustomType customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
        Protocol.CodeDict.Add(typeCode, customType);
        Protocol.TypeDict.Add(type, customType);
        return true;
    }

    public static bool TryRegisterType(
      Type type,
      byte typeCode,
      SerializeStreamMethod serializeFunction,
      DeserializeStreamMethod deserializeFunction)
    {
        if (Protocol.CodeDict.ContainsKey(typeCode) || Protocol.TypeDict.ContainsKey(type))
            return false;
        CustomType customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
        Protocol.CodeDict.Add(typeCode, customType);
        Protocol.TypeDict.Add(type, customType);
        return true;
    }

    [Obsolete]
    public static byte[] Serialize(object obj)
    {
        if (Protocol.ProtocolDefault == null)
            Protocol.ProtocolDefault = (IProtocol)new Protocol16();
        lock (Protocol.ProtocolDefault)
            return Protocol.ProtocolDefault.Serialize(obj);
    }

    [Obsolete]
    public static object Deserialize(byte[] serializedData)
    {
        if (Protocol.ProtocolDefault == null)
            Protocol.ProtocolDefault = (IProtocol)new Protocol16();
        lock (Protocol.ProtocolDefault)
            return Protocol.ProtocolDefault.Deserialize(serializedData);
    }

    public static void Serialize(short value, byte[] target, ref int targetOffset)
    {
        target[targetOffset++] = (byte)((uint)value >> 8);
        target[targetOffset++] = (byte)value;
    }

    public static void Serialize(int value, byte[] target, ref int targetOffset)
    {
        target[targetOffset++] = (byte)(value >> 24);
        target[targetOffset++] = (byte)(value >> 16);
        target[targetOffset++] = (byte)(value >> 8);
        target[targetOffset++] = (byte)value;
    }

    public static void Serialize(float value, byte[] target, ref int targetOffset)
    {
        lock (Protocol.memFloatBlock)
        {
            Protocol.memFloatBlock[0] = value;
            Buffer.BlockCopy((Array)Protocol.memFloatBlock, 0, (Array)target, targetOffset, 4);
        }
        if (BitConverter.IsLittleEndian)
        {
            byte num1 = target[targetOffset];
            byte num2 = target[targetOffset + 1];
            target[targetOffset] = target[targetOffset + 3];
            target[targetOffset + 1] = target[targetOffset + 2];
            target[targetOffset + 2] = num2;
            target[targetOffset + 3] = num1;
        }
        targetOffset += 4;
    }

    public static void Deserialize(out int value, byte[] source, ref int offset)
    {
        value = (int)source[offset++] << 24 | (int)source[offset++] << 16 | (int)source[offset++] << 8 | (int)source[offset++];
    }

    public static void Deserialize(out short value, byte[] source, ref int offset)
    {
        value = (short)((int)source[offset++] << 8 | (int)source[offset++]);
    }

    public static void Deserialize(out float value, byte[] source, ref int offset)
    {
        if (BitConverter.IsLittleEndian)
        {
            lock (Protocol.memDeserialize)
            {
                byte[] memDeserialize = Protocol.memDeserialize;
                memDeserialize[3] = source[offset++];
                memDeserialize[2] = source[offset++];
                memDeserialize[1] = source[offset++];
                memDeserialize[0] = source[offset++];
                value = BitConverter.ToSingle(memDeserialize, 0);
            }
        }
        else
        {
            value = BitConverter.ToSingle(source, offset);
            offset += 4;
        }
    }
}
