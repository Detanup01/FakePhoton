using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using NetCoreServer;
using Serilog;
using System.Security.Cryptography;

namespace FakePhotonLib.Managers;

public class PacketManager
{
    public static List<ClientPeer> Peers = [];

    public static Dictionary<int, List<int>> DoNotProcessReliablePacketIds = [];

    public static void DisconnectClient(ClientPeer.ClientConnection clientConnection)
    {
        Log.Information("!!!! DisconnectClient");
        var peerIndex = Peers.FindIndex(x => x.Connections.Contains(clientConnection));
        if (peerIndex == -1)
            return;
        var peer = Peers[peerIndex];
        peer.Connections.Remove(clientConnection);
        peer.LastConnectionIndex = -1;
    }

    public static void IncommingProcess(ClientPeer.ClientConnection clientConnection, byte[] data)
    {
        using BinaryReader binaryReader = new(new MemoryStream(data));
        Header header = new();
        header.Read(binaryReader);
        ClientPeer? peer = null;
        if (Peers.Any(x=> x.Challenge == header.Challenge))
        {
            var peerIndex = Peers.FindIndex(x => x.Challenge == header.Challenge);
            peer = peerIndex == -1 ? null : Peers[peerIndex];
        }
        if (peer == null)
        {
            peer = new()
            { 
                Challenge = header.Challenge,
                LastUnreliableSequence =
                {
                    { clientConnection.Server.Id, 0 }
                },
                LastReliableSequence =
                { 
                    { clientConnection.Server.Id, 0 }
                },
                Connections =
                {
                    clientConnection
                }
            };
            Peers.Add(peer);
        }

        if (!peer.LastUnreliableSequence.ContainsKey(clientConnection.Server.Id))
            peer.LastUnreliableSequence.Add(clientConnection.Server.Id, 0);
        if (!peer.LastReliableSequence.ContainsKey(clientConnection.Server.Id))
            peer.LastReliableSequence.Add(clientConnection.Server.Id, 0);

        if (!peer.Connections.Exists(x => x.Server.Id == clientConnection.Server.Id))
            peer.Connections.Add(clientConnection);

        peer.LastConnectionIndex = peer.Connections.FindIndex(x=>x.Server.Id == clientConnection.Server.Id);

        Log.Information("{UniqueName} Received: {Header}", clientConnection.Server.Id, header.ToString());
        for (int i = 0; i < header.CommandCount; i++)
        {
            CommandPacket packet = new();
            packet.Read(binaryReader);
            Log.Information("{UniqueName} Received: {Packet}", clientConnection.Server.Id, packet.ToString());

            if (packet.Payload != null)
            {
                packet.messageAndCallback = new();
                packet.messageAndCallback.peer = peer;
                try
                {
                    using BinaryReader payload_reader = new(new MemoryStream(packet.Payload));
                    packet.messageAndCallback.Read(payload_reader);
                    //Log.Information("{UniqueName} Received: {MC}", clientConnection.Server.Id, packet.messageAndCallback.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            header.Commands.Add(packet);
        }
        ProcessAndSend(peer, clientConnection, header);
    }

    public static void ProcessAndSend(ClientPeer peer, ClientPeer.ClientConnection clientToSendTo, Header header_from)
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
            //Log.Information("Working on: {CommandType}", command.commandType);
            if (command.IsFlaggedReliable && command.commandType != CommandType.Ack)
            {
                //Log.Information("Replying with Ack!");
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
                //Log.Information("Replying with messageAndCallback!");
                peer.LastReliableSequence[clientToSendTo.Server.Id] = command.ReliableSequenceNumber;
                var new_callback = MessageManager.Parse(peer, command.messageAndCallback, out (MessageAndCallback, CommandType)? optional);
                if (new_callback.MessageType == RtsMessageType.Unknown)
                    continue;
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
                // This is always the event!
                if (optional.HasValue)
                {
                    header.Commands.Add(new CommandPacket()
                    {
                        commandType = optional.Value.Item2,
                        ReliableSequenceNumber = command.ReliableSequenceNumber + 1,
                        UnreliableSequenceNumber = optional.Value.Item2 == CommandType.SendUnreliable ? peer.LastUnreliableSequence[clientToSendTo.Server.Id!]++ : 0,
                        Size = optional.Value.Item2 == CommandType.SendUnreliable ? 16 : 12,
                        ChannelID = command.ChannelID,
                        CommandFlags = (byte)(optional.Value.Item2 == CommandType.SendUnreliable ? 0 : 1),
                        messageAndCallback = optional.Value.Item1,
                        ReservedByte = 0,
                    });
                }
                if (!DoNotProcessReliablePacketIds.ContainsKey(header.Challenge))
                {
                    DoNotProcessReliablePacketIds.Add(header.Challenge, []);
                }
                DoNotProcessReliablePacketIds[header.Challenge].Add(command.ReliableSequenceNumber);
            }
            if (command.commandType == CommandType.Connect)
            {
                //Log.Information("Replying with VerifyConnect!");
                var peerID = (short)RandomNumberGenerator.GetInt32(short.MaxValue);
                peer.PeerId = peerID;
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
            if (command.commandType == CommandType.Disconnect)
            {
                DoNotProcessReliablePacketIds[header.Challenge].Clear();
            }
        }

        if (header.Commands.Count == 0)
        {
            Log.Information("No commands to send!");
            return;
        }
        Send(peer, clientToSendTo, header);
    }

    public static void Send(ClientPeer peer, ClientPeer.ClientConnection? clientToSendTo, Header header)
    {
        if (clientToSendTo == null)
        {
            Log.Error("Peer does not have associated server!");
            return;
        }
        if (clientToSendTo.Server == null)
        {
            Log.Error("Peer does not have associated server!");
            return;
        }
        if (clientToSendTo.EndPoint == null)
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
                //Log.Information("Command payload is now: {payload}", Convert.ToHexString(commandPacket.Payload));
                ms.SetLength(0);
            }
            commandPacket.Write(out_writer);
            out_writer.Flush();
        }

        var packet = out_BigStream.ToArray();

        //Log.Information("Sending out packet {packet} {packetLen} to {address}", Convert.ToHexString(packet), packet.Length, clientToSendTo.EndPoint);

        var sentBytes = clientToSendTo.Server.Send(clientToSendTo.EndPoint, packet);
        //Log.Information("Sent bytes: {sent}", sentBytes);
    }
}
