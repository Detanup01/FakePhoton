using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using NetCoreServer;
using Serilog;
using System.Net;
using System.Security.Cryptography;

namespace FakePhotonLib.Managers;

public class PacketManager
{
    public static List<ClientPeer> Peers = [];

    public static Dictionary<int, List<int>> DoNotProcessReliablePacketIds = [];

    public static void IncommingProcess(EndPoint endpoint, UdpServer server, byte[] data)
    {
        using BinaryReader binaryReader = new(new MemoryStream(data));
        Header header = new();
        header.Read(binaryReader);
        ClientPeer? peer = null;
        if (Peers.Any(x=>x.server == server && x.endPoint == endpoint && x.challenge == header.Challenge))
        {
            var peerIndex = Peers.FindIndex(x => x.server == server && x.endPoint == endpoint && x.challenge == header.Challenge);
            peer = peerIndex == -1 ? null : Peers[peerIndex];
        }
        if (peer == null)
        {
            peer = new()
            { 
                server = server,
                endPoint = endpoint,
                challenge = header.Challenge,
            };
            Peers.Add(peer);
        }


        Log.Information("{UniqueName} Received: {Header}", server.GetType().Name, header.ToString());
        for (int i = 0; i < header.CommandCount; i++)
        {
            CommandPacket packet = new();
            packet.Read(binaryReader);
            Log.Information("{UniqueName} Received: {Packet}", server.GetType().Name, packet.ToString());

            if (packet.Payload != null)
            {
                packet.messageAndCallback = new();
                packet.messageAndCallback.peer = peer;
                try
                {
                    using BinaryReader payload_reader = new(new MemoryStream(packet.Payload));
                    packet.messageAndCallback.Read(payload_reader);
                    Log.Information("{UniqueName} Received: {MC}", server.GetType().Name, packet.messageAndCallback.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            header.Commands.Add(packet);
        }
        ProcessAndSend(peer, header);
    }

    public static void ProcessAndSend(ClientPeer peer, Header header_from)
    {
        Header header = new()
        {
            CrcOrEncrypted = 0,
            PeerId = 0,
            Commands = [],
            ServerTime = Environment.TickCount,
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
                    ChannelID = command.ChannelID,
                });
            }
            if (command.messageAndCallback != null)
            {
                if (DoNotProcessReliablePacketIds.TryGetValue(header.Challenge, out var rsn) && rsn.Contains(command.ReliableSequenceNumber))
                    continue;
                Log.Information("Replying with messageAndCallback!");
                var new_callback = MessageManager.Parse(peer, command.messageAndCallback);
                header.Commands.Add(new CommandPacket()
                {
                    commandType = CommandType.SendReliable,
                    ReliableSequenceNumber = command.ReliableSequenceNumber,
                    Size = 12,
                    ChannelID = command.ChannelID,
                    CommandFlags = 1,
                    messageAndCallback = new_callback,
                    ReservedByte = 0,
                });
                if (!DoNotProcessReliablePacketIds.ContainsKey(header.Challenge))
                {
                    DoNotProcessReliablePacketIds.Add(header.Challenge, []);
                }
                DoNotProcessReliablePacketIds[header.Challenge].Add(command.ReliableSequenceNumber);
            }
            if (command.commandType == CommandType.Connect)
            {
                Log.Information("Replying with VerifyConnect!");
                var peerID = (short)RandomNumberGenerator.GetInt32(short.MaxValue);
                peer.peerId = peerID;
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
        Send(peer, header);
    }

    public static void Send(ClientPeer peer, Header header)
    {
        if (peer.server == null)
        {
            Log.Error("Peer does not have associated server!");
            return;
        }
        if (peer.endPoint == null)
        {
            Log.Error("Peer does not have associated endpoint!");
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
                Log.Information("Command payload is now: {payload}", Convert.ToHexString(commandPacket.Payload));
                ms.SetLength(0);
            }
            commandPacket.Write(out_writer);
            out_writer.Flush();
        }

        var packet = out_BigStream.ToArray();

        Log.Information("Sending out packet {packet} {packetLen} to {address}", Convert.ToHexString(packet), packet.Length, peer.endPoint);

        var sentBytes = peer.server.Send(peer.endPoint, packet);
        Log.Information("Sent bytes: {sent}", sentBytes);
    }
}
