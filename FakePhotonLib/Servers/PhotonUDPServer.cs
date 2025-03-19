using System.Net;
using NetCoreServer;
using Serilog;

namespace FakePhotonLib.Servers;

public class PhotonUDPServer : UdpServer
{
    public string UniqueName;
    public PhotonUDPServer(string uniqueName, IPAddress address, int port) : base(address, port)
    {
        UniqueName = uniqueName;
    }

    protected override void OnStarted()
    {
        base.OnStarted();
        ReceiveAsync();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        var buf = buffer.Skip((int)offset).Take((int)size).ToArray();
        Log.Information("Received on {UniqueName} from {EndPoint}\n{Bytes}", UniqueName, Endpoint, BitConverter.ToString(buf).Replace("-",string.Empty));
    }
}
