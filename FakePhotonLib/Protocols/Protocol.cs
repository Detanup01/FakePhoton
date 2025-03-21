using FakePhotonLib.PhotonRelated;

namespace FakePhotonLib.Protocols;

public class Protocol
{
    internal static readonly Dictionary<Type, CustomType> TypeDict = new();
    internal static readonly Dictionary<byte, CustomType> CodeDict = new();
    public static IProtocol ProtocolDefault = new Protocol18();
    private static readonly float[] memFloatBlock = new float[1];
    private static readonly byte[] memDeserialize = new byte[4];

    public static bool TryRegisterType(Type type, byte typeCode, SerializeMethod serializeFunction, DeserializeMethod deserializeFunction)
    {
        if (CodeDict.ContainsKey(typeCode) || TypeDict.ContainsKey(type))
            return false;
        var customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
        CodeDict[typeCode] = customType;
        TypeDict[type] = customType;
        return true;
    }

    public static bool TryRegisterType(Type type, byte typeCode, SerializeStreamMethod serializeFunction, DeserializeStreamMethod deserializeFunction)
    {
        if (CodeDict.ContainsKey(typeCode) || TypeDict.ContainsKey(type))
            return false;
        var customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
        CodeDict[typeCode] = customType;
        TypeDict[type] = customType;
        return true;
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
        lock (memFloatBlock)
        {
            memFloatBlock[0] = value;
            Buffer.BlockCopy(memFloatBlock, 0, target, targetOffset, 4);
        }
        if (BitConverter.IsLittleEndian)
        {
            (target[targetOffset], target[targetOffset + 3]) = (target[targetOffset + 3], target[targetOffset]);
            (target[targetOffset + 1], target[targetOffset + 2]) = (target[targetOffset + 2], target[targetOffset + 1]);
        }
        targetOffset += 4;
    }


    public static void Deserialize(out int value, byte[] source, ref int offset)
    {
        value = (source[offset++] << 24) | (source[offset++] << 16) | (source[offset++] << 8) | source[offset++];
    }

    public static void Deserialize(out short value, byte[] source, ref int offset)
    {
        value = (short)((source[offset++] << 8) | source[offset++]);
    }

    public static void Deserialize(out float value, byte[] source, ref int offset)
    {
        if (BitConverter.IsLittleEndian)
        {
            lock (memDeserialize)
            {
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
