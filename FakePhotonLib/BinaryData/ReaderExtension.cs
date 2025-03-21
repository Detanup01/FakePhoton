using System.Buffers.Binary;

namespace FakePhotonLib.BinaryData;

public static class ReaderExtension
{
    public static int ReadInt32Big(this BinaryReader br)
    {
        return BinaryPrimitives.ReverseEndianness(br.ReadInt32());
    }
    public static uint ReadUInt32Big(this BinaryReader br)
    {
        return BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
    }

    public static short ReadInt16Big(this BinaryReader br)
    {
        return BinaryPrimitives.ReverseEndianness(br.ReadInt16());
    }

    public static ushort ReadUInt16Big(this BinaryReader br)
    {
        return BinaryPrimitives.ReverseEndianness(br.ReadUInt16());
    }

    public static float ReadSingleBig(this BinaryReader br)
    {
        return BinaryPrimitives.ReadSingleBigEndian(br.ReadBytes(4));
    }

    public static double ReadDoubleBig(this BinaryReader br)
    {
        return BinaryPrimitives.ReadDoubleBigEndian(br.ReadBytes(8));
    }
}
