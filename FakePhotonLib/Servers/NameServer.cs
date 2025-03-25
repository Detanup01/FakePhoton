using System.Net;
using System.Net.Sockets;
using FakePhotonLib.Managers;
using NetCoreServer;
using Serilog;

namespace FakePhotonLib.Servers;

public class NameServer(IPAddress address, int port) : UdpServer(address, port)
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
        Log.Information("Received on {UniqueName} from {EndPoint}\n{Bytes}", nameof(NameServer), endpoint, Convert.ToHexString(buf));

        if (buf.Length == 0)
            return;
        PacketManager.IncommingProcess(endpoint, this, buf);
        ReceiveAsync();
    }


    protected override void OnError(SocketError error)
    {
        Log.Error("ERROR! {sockError}" , error);
        base.OnError(error);
    }
}
