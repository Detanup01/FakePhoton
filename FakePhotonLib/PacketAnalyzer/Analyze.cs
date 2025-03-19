using FakePhotonLib.BinaryData;
using FakePhotonLib.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakePhotonLib.PacketAnalyzer;

public static class Analyze
{

    public static void Init()
    {
        if (Directory.Exists("Anal"))
        {
            foreach (var file in Directory.GetFiles("Anal").OrderBy(x => x))
            {
                //Console.WriteLine(file);
                SinglePacket(File.ReadAllText(file));
            }
        }

        if (File.Exists("dump.txt"))
        {
            int i = 0;
            foreach (string line in File.ReadAllLines("dump.txt"))
            {
                Console.WriteLine("Packet Number: " + i);
                SinglePacket(line.ReplaceLineEndings());
                i++;
            }
        }
    }

    public static void SinglePacket(string packetAsHex)
    {
        MemoryStream ms = new MemoryStream(Convert.FromHexString(packetAsHex));
        BinaryReader binaryReader = new BinaryReader(ms);
        Header header = new();
        header.Read(binaryReader);
        Console.WriteLine(header.ToString());
        for (int i = 0; i < header.CommandCount; i++)
        {
            Console.WriteLine($"\n-- {i} --");
            CommandPacket command = new();
            command.Read(binaryReader);
            Console.WriteLine(command.ToString());
            if (command.PayLoad != null)
            {
                MessageAndCallback messageAndCallback = new(header.Challenge);
                try
                {
                    using MemoryStream ms2 = new MemoryStream(command.PayLoad);
                    var payloadReader = new BinaryReader(ms2);
                    messageAndCallback.Reset();
                    messageAndCallback.Read(payloadReader);
                    Console.WriteLine(messageAndCallback.ToString());
                    MessageManager.Parse(messageAndCallback);
                    payloadReader.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        var bytes = binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
        if (bytes != null && bytes.Length != 0)
        {
            Console.WriteLine("Remaining:");
            Console.WriteLine(BitConverter.ToString(bytes).Replace("-", string.Empty));
        }
    }

    static void ReadPacket()
    {

    }

}
