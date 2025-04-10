using NetCoreServer;
using System.Net;

namespace FakePhotonLib.Datas;

public class ClientPeer
{
    public UdpServer? Server;
    public EndPoint? EndPoint;
    public short PeerId;
    public int Challenge;
    public string? UserId;
    public Dictionary<UdpServer, int> LastUnreliableSequence = [];
}
