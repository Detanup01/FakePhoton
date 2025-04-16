using FakePhotonLib.Managers;
using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace FakePhotonLib.Servers;

public class GameServer(string serverType, IPAddress address, int port) : UdpServer(address, port)
{
    public string ServerType { get; } = serverType;
    protected override void OnStarted()
    {
        base.OnStarted();
        ReceiveAsync();
    }

    protected override void OnStopped()
    {
        base.OnStopped();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        var buf = buffer.Skip((int)offset).Take((int)size).ToArray();
        Log.Information("Received on {UniqueName} {Id} from {EndPoint}", (nameof(GameServer) + "_" + ServerType), Id, endpoint); //, Convert.ToHexString(buf));
        if (buf.Length == 0)
        {
            PacketManager.DisconnectClient(new(this, endpoint));
            ReceiveAsync();
            return;
        }
        PacketManager.IncommingProcess(new(this, endpoint), buf);
        ReceiveAsync();
    }

    protected override void OnError(SocketError error)
    {
        Log.Error("ERROR! {sockError}", error);
        base.OnError(error);
    }
}
