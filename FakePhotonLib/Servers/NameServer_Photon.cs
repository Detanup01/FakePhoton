using System.Collections.Generic;
using System.Net;
using FakePhotonLib.BinaryData;
using FakePhotonLib.Managers;
using FakePhotonLib.PhotonRelated;
using NetCoreServer;
using Serilog;

namespace FakePhotonLib.Servers;

public class NameServer_Photon(string uniqueName, IPAddress address, int port) : UdpServer(address, port)
{
    public string UniqueName = uniqueName;

    protected override void OnStarted()
    {
        base.OnStarted();
        ReceiveAsync();
    }
    readonly Queue<(EndPoint, Header)> EnqueueHeaders = new();

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        var buf = buffer.Skip((int)offset).Take((int)size).ToArray();
        Log.Information("Received on {UniqueName} from {EndPoint}\n{Bytes}", UniqueName, Endpoint, Convert.ToHexString(buf));
        using BinaryReader binaryReader = new(new MemoryStream(buf));
        Header header = new();
        header.Read(binaryReader);
        Console.WriteLine(header.ToString());
        var bytes = binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
        binaryReader.Dispose();
        StreamBuffer streamBuffer = new(bytes);
        for (int i = 0; i < header.CommandCount; i++)
        {
            CommandPacket packet = new();
            packet.Read(binaryReader);
            Console.WriteLine(packet.ToString());

            if (packet.Payload != null)
            {
                packet.messageAndCallback = new(header.Challenge);
                try
                {
                    using BinaryReader payload_reader = new(new MemoryStream(packet.Payload));
                    packet.messageAndCallback.Read(payload_reader);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        streamBuffer.Flush();
        EnqueueHeaders.Enqueue((endpoint, header));
        MessageWork();
    }

    public void MessageWork()
    {
        /*
        EndPoint? toSendDest = null;
        Header header = new()
        { 
            CrcOrEncrypted = 0,
            PeerId = 0,
            Commands = []
        };
        byte commandCount = 0;
        StreamBuffer streamBuffer = new(); 
        //List<object> responses = [];
        Queue<(EndPoint, Header)> NotOurs = new();
        foreach (var headers in EnqueueHeaders)
        {
            toSendDest ??= headers.Item1;
            if (toSendDest != headers.Item1)
            {
                NotOurs.Enqueue(headers);
                continue;
            }
            if (header.Challenge == 0)
                header.Challenge = headers.Item2.Challenge;
            if (header.Challenge != headers.Item2.Challenge)
            {
                NotOurs.Enqueue(headers);
                continue;
            }
            foreach (var command in headers.Item2.Commands)
            {
                commandCount++;
                if (command.IsFlaggedReliable)
                {
                    byte[] bytes = streamBuffer.GetBufferAndAdvance(20, out var offset);
                    NCommand.CreateAck(bytes, offset, command, headers.Item2.ServerTime);
                    CommandPool.Release(command);
                }
                if (command.messageAndCallback != null)
                    MessageManager.Parse(command.messageAndCallback);
                // TODO: logic for send
            }
            

        }
        */
    }
}
