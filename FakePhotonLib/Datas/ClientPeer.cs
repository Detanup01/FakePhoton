using NetCoreServer;
using System.Net;

namespace FakePhotonLib.Datas;

public class ClientPeer
{
    public UdpServer? server;
    public EndPoint? endPoint;
    public short peerId;
    public int challenge;
    public string? UserId;
}
