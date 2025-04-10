using FakePhotonLib.Servers;
using NetCoreServer;

namespace FakePhotonLib.Managers;

public static class ServerManager
{
    static List<UdpServer> UDPServers = [];

    public static void Start()
    {
        UDPServers.Add(new NameServer(System.Net.IPAddress.Loopback, 5058));
        UDPServers.Add(new MasterGameServer(System.Net.IPAddress.Loopback, 5055));
        UDPServers.Add(new GameServer("Game",System.Net.IPAddress.Loopback, 5000));
        UDPServers.Add(new GameServer("Voice", System.Net.IPAddress.Loopback, 5001));
        // Add Voice Server!

        foreach (var server in UDPServers)
        {
            server.Start();
        }
    }

    public static void Stop()
    {
        foreach (var server in UDPServers)
        {
            server.Stop();
        }
        UDPServers.Clear();
    }
}
