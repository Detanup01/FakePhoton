using System.Net;

namespace FakePhotonLib.Datas;

public class ClientPeer
{
    public EndPoint? endPoint;
    public short peerId;
    public short challenge;
    public string? UserId;
}
