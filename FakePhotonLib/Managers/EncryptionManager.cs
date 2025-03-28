using FakePhotonLib.Datas;
using FakePhotonLib.Encryptions;
using Serilog;

namespace FakePhotonLib.Managers;

public static class EncryptionManager
{
    public static Dictionary<ClientPeer, DiffieHellmanCryptoProvider> EncryptionByChallenge = [];

    public static byte[] ExchangeKeys(ClientPeer peer, byte[] key)
    {
        Log.Information("Client Key: " + Convert.ToHexString(key));
        EncryptionByChallenge.Remove(peer);
        DiffieHellmanCryptoProvider ServerEncryption = new();
        ServerEncryption.DeriveSharedKeyAsServer(key);
        EncryptionByChallenge.Add(peer, ServerEncryption);
        var server_key = ServerEncryption.PublicKeyAsServer;
        Log.Information("Server: " + ServerEncryption.ToString());
        return server_key;
    }
}
