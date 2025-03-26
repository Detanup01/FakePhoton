using System.Buffers.Binary;

namespace FakePhotonLib.BinaryData;

public static class WriterExtension
{
    public static void WriteInt32Big(this BinaryWriter br, int value)
    {
        br.Write(BinaryPrimitives.ReverseEndianness(value));
    }
    public static void WriteInt16Big(this BinaryWriter br, short value)
    {
        br.Write(BinaryPrimitives.ReverseEndianness(value));
    }
    public static void WriteUInt16Big(this BinaryWriter br, ushort value)
    {
        br.Write(BinaryPrimitives.ReverseEndianness(value));
    }
    public static void WriteUInt32Big(this BinaryWriter br, uint value)
    {
        br.Write(BinaryPrimitives.ReverseEndianness(value));
    }

    public static void WriteSingleBig(this BinaryWriter br, float value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteSingleBigEndian(bytes, value);
        br.Write(bytes);
    }

    public static void WriteDoubleBig(this BinaryWriter br, double value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
        br.Write(bytes);
    }
}
