using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using FakePhotonLib.BinaryData;
using FakePhotonLib.Managers;
using FakePhotonLib.PacketAnalyzer;
using FakePhotonLib.PhotonRelated;
using NetCoreServer;
using Serilog;

namespace FakePhotonLib.Servers;

public class NameServer_Photon(string uniqueName, IPAddress address, int port) : UdpServer(address, port)
{
    public Stopwatch Stopwatch = new Stopwatch();

    public string UniqueName = uniqueName;

    protected override void OnStarted()
    {
        Stopwatch.Start();
        base.OnStarted();
        ReceiveAsync();
    }
    protected override void OnStopped()
    {
        base.OnStopped();
        Stopwatch.Stop();
    }

    readonly List<(EndPoint, Header)> PacketsThatExists = new();

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        var buf = buffer.Skip((int)offset).Take((int)size).ToArray();
        Log.Information("Received on {UniqueName} from {EndPoint}\n{Bytes}", UniqueName, endpoint, Convert.ToHexString(buf));
        using BinaryReader binaryReader = new(new MemoryStream(buf));
        Header header = new();
        header.Read(binaryReader);
        //Console.WriteLine(header.ToString());
        //var bytes = binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
        for (int i = 0; i < header.CommandCount; i++)
        {
            CommandPacket packet = new();
            packet.Read(binaryReader);
            //Console.WriteLine(packet.ToString());

            if (packet.Payload != null)
            {
                packet.messageAndCallback = new(header.Challenge);
                try
                {
                    using BinaryReader payload_reader = new(new MemoryStream(packet.Payload));
                    packet.messageAndCallback.Read(payload_reader);
                    Console.WriteLine(packet.messageAndCallback.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            header.Commands.Add(packet);
        }
        //streamBuffer.Flush();
        MessageWork(endpoint, header);
        ReceiveAsync();
    }

    public void MessageWork(EndPoint endpoint, Header header_from)
    {
        Header header = new()
        { 
            CrcOrEncrypted = 0,
            PeerId = 0,
            Commands = [],
            ServerTime = (int)Stopwatch.ElapsedMilliseconds,
            Challenge = header_from.Challenge,
        };

        foreach (var command in header_from.Commands)
        {
            Log.Information("Working on: {CommandType}", command.commandType);
            if (command.IsFlaggedReliable && command.commandType != CommandType.Ack)
            {
                Log.Information("Replying with Ack!");
                header.Commands.Add(new CommandPacket()
                {
                    commandType = command.IsFlaggedUnsequenced ? CommandType.AckUnsequenced : CommandType.Ack,
                    AckReceivedReliableSequenceNumber = command.ReliableSequenceNumber,
                    AckReceivedSentTime = header_from.ServerTime,
                    Size = 20,
                    ReliableSequenceNumber = 0,
                    ReservedByte = 0,
                    ChannelID = 255
                });
            }
            if (command.messageAndCallback != null)
            {
                Log.Information("Replying with messageAndCallback!");
                var new_callback = MessageManager.Parse(command.messageAndCallback);
                header.Commands.Add(new CommandPacket()
                {
                    commandType = CommandType.SendReliable,
                    ReliableSequenceNumber = command.ReliableSequenceNumber,
                    AckReceivedSentTime = header_from.ServerTime,
                    Size = 12,
                    ChannelID = command.ChannelID,
                    CommandFlags = 1,
                    messageAndCallback = new_callback,
                    ReservedByte = 0,
                });
            }
            if (command.commandType == CommandType.Connect)
            {
                Log.Information("Replying with VerifyConnect!");
                var peerID = (short)RandomNumberGenerator.GetInt32(short.MaxValue);
                MessageManager.ChallengeToPeerId.Add(header.Challenge, peerID);
                header.Commands.Add(new CommandPacket()
                {
                    peerID = peerID,
                    commandType = CommandType.VerifyConnect,
                    ReliableSequenceNumber = command.ReliableSequenceNumber,
                    Size = 44,
                    ChannelID = command.ChannelID,
                    ReservedByte = 0,
                    CommandFlags = 1,
                });
            }
        }

        if (header.Commands.Count == 0)
        {
            Log.Information("No commands to send!");
            return;
        }

        using MemoryStream out_BigStream = new();
        using BinaryWriter out_writer = new(out_BigStream);
        header.CommandCount = (byte)header.Commands.Count;
        header.Write(out_writer);
        out_writer.Flush();

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);
       
        foreach (var commandPacket in header.Commands)
        {
            if (commandPacket.messageAndCallback != null)
            {
                commandPacket.messageAndCallback.Write(writer);
                writer.Flush();
                commandPacket.Size += (int)writer.BaseStream.Length;
                commandPacket.Payload = ms.ToArray();
                //Log.Information("Command payload is now: {payload}", Convert.ToHexString(commandPacket.Payload));
                ms.SetLength(0);
            }
            commandPacket.Write(out_writer);
            out_writer.Flush();
        }

        var packet = out_BigStream.ToArray();

        Log.Information("Sending out packet {packet} {packetLen} to {address}", Convert.ToHexString(packet), packet.Length, endpoint);

        //Analyze.SinglePacket(Convert.ToHexString(packet));

        var sentBytes = Send(endpoint, packet);
        Log.Information("Sent bytes: {sent}", sentBytes);
    }

    protected override void OnError(SocketError error)
    {
        Log.Error("ERROR! {sockError}" , error);
        base.OnError(error);
    }
}
