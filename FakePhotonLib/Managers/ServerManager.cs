using FakePhotonLib.Servers;

namespace FakePhotonLib.Managers;

public static class ServerManager
{
    static List<PhotonUDPServer> UDPServers = [];

    public static void Start()
    {
        UDPServers.Add(new("NameServer", System.Net.IPAddress.Any, 5058));
        UDPServers.Add(new("EU_MasterServer", System.Net.IPAddress.Any, 5055));

        foreach (PhotonUDPServer server in UDPServers)
        {
            server.Start();
        }
    }

    public static void Stop()
    {
        foreach (PhotonUDPServer server in UDPServers)
        {
            server.Stop();
        }
        UDPServers.Clear();
    }
}
