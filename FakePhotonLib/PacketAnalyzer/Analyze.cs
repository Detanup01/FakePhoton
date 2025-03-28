using FakePhotonLib.BinaryData;
using FakePhotonLib.Protocols;
using Serilog;
using System.Collections;

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

        if (File.Exists("packets.txt"))
        {
            int i = 0;
            foreach (string line in File.ReadAllLines("packets.txt"))
            {
                Console.WriteLine("Packet Number: " + i);
                ReadMessageAndCallback(line.ReplaceLineEndings());
                i++;
            }
        }
    }

    public static void SinglePacket(string packetAsHex)
    {
        // TODO: Create fake peers

        using MemoryStream ms = new MemoryStream(Convert.FromHexString(packetAsHex));
        using BinaryReader binaryReader = new BinaryReader(ms);
        Header header = new();
        header.Read(binaryReader);
        Console.WriteLine(header.ToString());
        for (int i = 0; i < header.CommandCount; i++)
        {
            Console.WriteLine($"\n-- {i} --");
            CommandPacket packet = new();
            packet.Read(binaryReader);
            Console.WriteLine(packet.ToString());
            
            if (packet.Payload != null)
            {
                MessageAndCallback messageAndCallback = new();
                try
                {
                    using BinaryReader payload_reader = new BinaryReader(new MemoryStream(packet.Payload));
                    messageAndCallback.Read(payload_reader);
                    Console.WriteLine(messageAndCallback.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }


    static void ReadMessageAndCallback(string bytes)
    {
        using BinaryReader payload_reader = new BinaryReader(new MemoryStream(Convert.FromHexString(bytes)));
        try
        {
            var req = Protocol.ProtocolDefault.DeserializeOperationRequest(payload_reader);
            foreach (var item in req.Parameters)
            {
                Log.Information("Req! Key: {Key} Value: {Value}", item.Key, item.Value);
                if (item.Value != null && item.Value.GetType() == typeof(Hashtable))
                {
                    Hashtable ht = (Hashtable)item.Value;
                    
                    foreach (var item1 in ht.Keys)
                    {
                        Log.Information("HT! Key: {Key} Value: {Value}", item1, ht[item1]);
                        if (ht[item1] != null &&  ht[item1]!.GetType() == typeof(string[]))
                        {
                            Log.Information("HT! stringArray: {array}", string.Join(", ", (string[])ht[item1]!));
                        }
                    }
                }
            }
        }
        catch (Exception)
        {

            
        }
        payload_reader.BaseStream.Position = 0;
        try
        {
            var rsp = Protocol.ProtocolDefault.DeserializeOperationResponse(payload_reader);
            foreach (var item in rsp.Parameters)
            {
                Log.Information("Rsp! Key: {Key} Value: {Value}", item.Key, item.Value);
                if (item.Value != null && item.Value.GetType() == typeof(Hashtable))
                {
                    Hashtable ht = (Hashtable)item.Value;
                    foreach (var item1 in ht.Keys)
                    {
                        Log.Information("HT! Key: {Key} Value: {Value}", item1, ht[item1]);
                    }
                }
            }
        }
        catch (Exception)
        {

            
        }
        payload_reader.BaseStream.Position = 0;
        try
        {
            var ev = Protocol.ProtocolDefault.DeserializeEventData(payload_reader);
            foreach (var item in ev.Parameters)
            {
                Log.Information("Event! Key: {Key} Value: {Value}", item.Key, item.Value);
                if (item.Value != null && item.Value.GetType() == typeof(Hashtable))
                {
                    Hashtable ht = (Hashtable)item.Value;
                    foreach (var item1 in ht.Keys)
                    {
                        Log.Information("HT! Key: {Key} Value: {Value}", item1, ht[item1]);
                    }
                }
            }
        }
        catch (Exception)
        {

            
        }
        payload_reader.BaseStream.Position = 0;
    }

}
