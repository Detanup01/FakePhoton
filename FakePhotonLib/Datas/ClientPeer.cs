using NetCoreServer;
using System.Net;

namespace FakePhotonLib.Datas;

public class ClientPeer
{
    public short PeerId;
    public int Challenge;
    public string? UserId;
    public Dictionary<UdpServer, int> LastUnreliableSequence = [];

    public List<ClientConnection> Connections = [];

    public int LastConnectionIndex = -1;

    public ClientConnection? GetLastConnection()
    {
        if (LastConnectionIndex == -1)
            return null;
        if (Connections.Count < LastConnectionIndex)
            return null;
        return Connections[LastConnectionIndex];
    }

    public class ClientConnection
    {
        public UdpServer Server;
        public EndPoint EndPoint;

        public ClientConnection(UdpServer server, EndPoint endPoint)
        {
            Server = server;
            EndPoint = endPoint;
        }
    }
}
