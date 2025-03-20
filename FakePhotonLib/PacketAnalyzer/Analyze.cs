using FakePhotonLib.BinaryData;
using FakePhotonLib.Managers;
using FakePhotonLib.PhotonRelated;

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
    static NCommandPool CommandPool = new();
    public static void SinglePacket(string packetAsHex)
    {
        MemoryStream ms = new MemoryStream(Convert.FromHexString(packetAsHex));
        BinaryReader binaryReader = new BinaryReader(ms);
        Header header = new();
        header.Read(binaryReader);
        Console.WriteLine(header.ToString());
        var bytes = binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
        StreamBuffer streamBuffer = new StreamBuffer(bytes);
        int offset = 0;
        for (int i = 0; i < header.CommandCount; i++)
        {
            Console.WriteLine($"\n-- {i} --");
            NCommand command = CommandPool.Acquire(streamBuffer.GetBuffer(), ref offset);
            Console.WriteLine(command.ToString());
            if (command.Payload != null)
            {
                MessageAndCallback messageAndCallback = new(header.Challenge);
                try
                {
                    messageAndCallback.Read(command.Payload);
                    MessageManager.Parse(messageAndCallback);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        Console.WriteLine($"Offset: {offset} Len : {streamBuffer.Length}");
    }

    static void ReadPacket()
    {

    }

}
