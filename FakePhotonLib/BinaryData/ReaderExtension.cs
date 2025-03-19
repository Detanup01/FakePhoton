using System.Buffers.Binary;

namespace FakePhotonLib.BinaryData;

public static class ReaderExtension
{
    public static int ReadInt32Big(this BinaryReader br)
    {
        return BinaryPrimitives.ReverseEndianness(br.ReadInt32());
    }

    public static short ReadInt16Big(this BinaryReader br)
    {
        return BinaryPrimitives.ReverseEndianness(br.ReadInt16());
    }
}
