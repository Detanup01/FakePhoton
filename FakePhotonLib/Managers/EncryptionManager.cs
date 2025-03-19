using FakePhotonLib.Encryptions;
using System;
using System.Text;

namespace FakePhotonLib.Managers;

public static class EncryptionManager
{
    public static Dictionary<int, DiffieHellmanCryptoProvider> EncryptionByChallenge = [];


    public static void EncryptionTest()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAAAAAASSDFDSFGDG TETS TET TEST TEST ESTST EST SE  ET ET");

        var serverCrypto = new DiffieHellmanCryptoProvider();
        var key = serverCrypto.PublicKey;
        Console.WriteLine("PublicKey: " + BitConverter.ToString(key).Replace("-", string.Empty));

        var clientCrypto = new DiffieHellmanCryptoProvider();

        clientCrypto.DeriveSharedKey(key);

        var enc = serverCrypto.Encrypt(bytes);
        var dec = serverCrypto.Decrypt(enc);
        Console.WriteLine(BitConverter.ToString(bytes).Replace("-", string.Empty));
        Console.WriteLine(BitConverter.ToString(dec).Replace("-", string.Empty));

        enc = clientCrypto.Encrypt(bytes);
        dec = clientCrypto.Decrypt(enc);

        Console.WriteLine(BitConverter.ToString(bytes).Replace("-", string.Empty));
        Console.WriteLine(BitConverter.ToString(dec).Replace("-", string.Empty));

        enc = serverCrypto.Encrypt(bytes);
        dec = clientCrypto.Decrypt(enc);

        Console.WriteLine(BitConverter.ToString(bytes).Replace("-", string.Empty));
        Console.WriteLine(BitConverter.ToString(dec).Replace("-", string.Empty));


    }
}
