using FakePhotonLib.Managers;
using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace FakePhotonLib.Servers;

public class MasterGameServer(IPAddress address, int port) : UdpServer(address, port)
{
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
        Log.Information("Received on {UniqueName} from {EndPoint}\n{Bytes}", nameof(MasterGameServer), endpoint, Convert.ToHexString(buf));
        if (buf.Length == 0)
        {
            PacketManager.DisconnectClient(new(this, endpoint));
            ReceiveAsync();
            return;
        }
        if (buf.Length >= 12 && Convert.ToHexString(buf[..12]) == "7D7D7D7D7D7D7D7D7D7D7D7D")
        {
            Send(endpoint, buf);
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
