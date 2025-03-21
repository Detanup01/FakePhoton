using FakePhotonLib.BinaryData;
using FakePhotonLib.Protocols;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakePhotonLib.Testings;

public class BinaryRWTest
{

    public static void brw_test()
    {
        using MemoryStream ms = new();
        using BinaryWriter binaryWriter = new(ms);
        binaryWriter.WriteInt16Big(short.MinValue);
        binaryWriter.WriteInt16Big(short.MaxValue);
        binaryWriter.WriteUInt16Big(ushort.MinValue);
        binaryWriter.WriteUInt16Big(ushort.MaxValue);
        binaryWriter.WriteInt32Big(int.MinValue);
        binaryWriter.WriteInt32Big(int.MaxValue);
        binaryWriter.WriteUInt32Big(uint.MinValue);
        binaryWriter.WriteUInt32Big(uint.MaxValue);
        binaryWriter.WriteSingleBig(6.60f);
        binaryWriter.WriteDoubleBig(16.90);


        byte[] data = ms.ToArray();
        ms.Dispose();

        using BinaryReader binaryReader = new(new MemoryStream(data));

        short val = binaryReader.ReadInt16Big();
        if (val != short.MinValue)
            throw new InvalidDataException();
        val = binaryReader.ReadInt16Big();
        if (val != short.MaxValue)
            throw new InvalidDataException();

        ushort val2 = binaryReader.ReadUInt16Big();
        if (val2 != ushort.MinValue)
            throw new InvalidDataException();
        val2 = binaryReader.ReadUInt16Big();
        if (val2 != ushort.MaxValue)
            throw new InvalidDataException();

        int val3 = binaryReader.ReadInt32Big();
        if (val3 != int.MinValue)
            throw new InvalidDataException();
        val3 = binaryReader.ReadInt32Big();
        if (val3 != int.MaxValue)
            throw new InvalidDataException();

        uint val4 = binaryReader.ReadUInt32Big();
        if (val4 != uint.MinValue)
            throw new InvalidDataException();
        val4 = binaryReader.ReadUInt32Big();
        if (val4 != uint.MaxValue)
            throw new InvalidDataException();
        Console.WriteLine(binaryReader.ReadSingleBig());
        Console.WriteLine(binaryReader.ReadDoubleBig());
        binaryReader.Dispose();
    }


    public static void protocol18_test()
    {
        
        Dictionary<byte, object> test = new();
        test.Add(9, new byte[] { 0xFF, 0xAA, 0xBB });
        foreach (var item in test)
        {
            Console.WriteLine(item.Key.GetType());
            Console.WriteLine(item.Value.GetType());
        }
        var ser_ = Protocol.ProtocolDefault.Serialize(test);
        Console.WriteLine(Convert.ToHexString(ser_));
        var des = Protocol.ProtocolDefault.Deserialize(ser_);

        Console.WriteLine(des);
        Console.WriteLine(des.GetType());

        Dictionary<byte, object> test_des = (Dictionary<byte, object>)des;

        foreach (var item in test_des)
        {
            Console.WriteLine(item.Key);
            Console.WriteLine(item.Value);
            Console.WriteLine(item.Key.GetType());
            Console.WriteLine(item.Value.GetType());

            if (item.Value.GetType() == typeof(byte[]))
            {
                byte[] bytes = (byte[])item.Value;
                Console.WriteLine(Convert.ToHexString(bytes));
            }
        }
    }

    public static void compress_test()
    {
        using MemoryStream ms = new();
        using BinaryWriter binaryWriter = new(ms);
        WriteCompressedUInt64(binaryWriter, ulong.MaxValue);
        Console.WriteLine(ulong.MaxValue);
        WriteCompressedUInt64(binaryWriter, ulong.MaxValue);
        byte[] data = ms.ToArray();
        ms.Dispose();

        using BinaryReader binaryReader = new(new MemoryStream(data));
        Console.WriteLine(ReadCompressedUInt64(binaryReader));
    }

    private static ulong ReadCompressedUInt64(BinaryReader reader)
    {
        ulong result = 0;
        int shift = 0;

        while (shift != 70)
        {
            byte b = reader.ReadByte();
            Log.Information("{val}", b);
            result |= (ulong)(b & 0x7F) << shift;
            shift += 7;
            Log.Information("shift: {val}", shift);
            if ((b & 0x80) == 0)
                break;
        }

        return result;
    }

    private static void WriteCompressedUInt64(BinaryWriter writer, ulong value)
    {
        while (value > 0x7F)
        {
            writer.Write((byte)((value & 0x7F) | 0x80));
            Log.Information("b4 : {val}", value);
            value >>= 7;
            Log.Information("{val}", value);
        }
        writer.Write((byte)value);
    }
}
