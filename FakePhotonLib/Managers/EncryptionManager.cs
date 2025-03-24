using FakePhotonLib.Encryptions;
using Serilog;
using System.Text;

namespace FakePhotonLib.Managers;

public static class EncryptionManager
{
    public static Dictionary<int, DiffieHellmanCryptoProvider> EncryptionByChallenge = [];

    public static byte[] ExchangeKeys(int challenge, byte[] key)
    {
        Log.Information("Client Key: " + Convert.ToHexString(key));
        EncryptionByChallenge.Remove(challenge);
        DiffieHellmanCryptoProvider ServerEncryption = new();
        ServerEncryption.DeriveSharedKeyAsServer(key);
        EncryptionByChallenge.Add(challenge, ServerEncryption);
        var server_key = ServerEncryption.PublicKeyAsServer;
        Log.Information("Server: " + ServerEncryption.ToString());
        return server_key;
    }



    public static void EncryptionTest()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAAAAAASSDFDSFGDG TETS TET TEST TEST ESTST EST SE  ET ET");

        var ClientEncryption = new DiffieHellmanCryptoProvider();
        var ClientKey = ClientEncryption.PublicKeyAsClient;
        Console.WriteLine("Client: " + ClientEncryption.ToString());
        var ServerEncryption = new DiffieHellmanCryptoProvider();
        var ServerKey = ServerEncryption.PublicKeyAsServer;
        Console.WriteLine("Server: " + ServerEncryption.ToString());
        Console.WriteLine("ClientKey: " + BitConverter.ToString(ClientKey));
        Console.WriteLine("ServerKey: " + BitConverter.ToString(ServerKey));
        ServerEncryption.DeriveSharedKeyAsServer(ClientKey);
        Console.WriteLine("Server: " + ServerEncryption.ToString());
        ClientEncryption.DeriveSharedKeyAsClient(ServerKey);
        Console.WriteLine("Client: " + ClientEncryption.ToString());

        var enc = ClientEncryption.Encrypt(bytes);
        var dec = ServerEncryption.Decrypt(enc);
        var ori =  BitConverter.ToString(bytes);
        var dec_ori = BitConverter.ToString(dec);
        Console.WriteLine("IsWorking: " + (ori == dec_ori));

    }
}
