using FakePhotonLib.Servers;

namespace FakePhotonLib.Managers;

public static class ServerManager
{
    static List<NameServer_Photon> UDPServers = [];

    public static void Start()
    {
        UDPServers.Add(new("NameServer", System.Net.IPAddress.Any, 5058));
        UDPServers.Add(new("EU_MasterServer", System.Net.IPAddress.Any, 5055));

        foreach (NameServer_Photon server in UDPServers)
        {
            server.Start();
        }
    }

    public static void Stop()
    {
        foreach (NameServer_Photon server in UDPServers)
        {
            server.Stop();
        }
        UDPServers.Clear();
    }
}
