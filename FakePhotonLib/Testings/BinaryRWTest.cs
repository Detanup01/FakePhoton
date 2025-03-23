using FakePhotonLib.BinaryData;
using FakePhotonLib.Encryptions;
using FakePhotonLib.Protocols;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
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
        var ServerEncryption = new DiffieHellmanCryptoProvider();
        var ServerKey = ServerEncryption.PublicKeyAsServer;
        OperationResponse response = new()
        {
            OperationCode = 0,
            ReturnCode = 0,
            Parameters = new()
            {
                { 1, ServerKey },
            },
            DebugMessage = null,
        };
        using MemoryStream ms = new();
        using BinaryWriter binaryWriter = new(ms);
        Protocol.ProtocolDefault.SerializeOperationResponse(binaryWriter, response, false);

        byte[] data = ms.ToArray();
        ms.Dispose();

        using BinaryReader binaryReader = new(new MemoryStream(data));

        var des = Protocol.ProtocolDefault.DeserializeOperationResponse(binaryReader);

        Console.WriteLine(des);
        Console.WriteLine(des.GetType());
    }
}
