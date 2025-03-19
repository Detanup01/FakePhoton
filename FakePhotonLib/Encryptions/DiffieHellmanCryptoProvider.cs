using Serilog;
using System;
using System.Numerics;
using System.Security.Cryptography;

namespace FakePhotonLib.Encryptions;

public class DiffieHellmanCryptoProvider : IDisposable
{
    public static readonly byte[] OakleyPrime768 =
        [
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 32, 54,
            58, 166, 233, 66, 76, 244, 198, 126, 94, 98,
            118, 181, 133, 228, 69, 194, 81, 109, 109, 53,
            225, 79, 55, 20, 95, 242, 109, 10, 43, 48,
            27, 67, 58, 205, 179, 25, 149, 239, 221, 4,
            52, 142, 121, 8, 74, 81, 34, 155, 19, 59,
            166, 190, 11, 2, 116, 204, 103, 138, 8, 78,
            2, 41, 209, 28, 220, 128, 139, 98, 198, 196,
            52, 194, 104, 33, 162, 218, 15, 201, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0
        ];
    private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
    private static readonly BigInteger primeRoot = new(22);
    private readonly BigInteger prime;
    private readonly BigInteger secret;
    private readonly BigInteger publicKey;
    private byte[]? sharedKey;

    private Aes? crypto;

    public DiffieHellmanCryptoProvider()
    {
        this.prime = new BigInteger(OakleyPrime768);
        this.secret = this.GenerateRandomSecret(160);
        this.publicKey = this.CalculatePublicKey();
    }

    
    public void DeriveSharedKeyServer(byte[] otherPartyPublicKey)
    {
        BigInteger bigInteger = new BigInteger(otherPartyPublicKey);
        var shared_prev = this.CalculateSharedKey(bigInteger).ToByteArray();
        this.sharedKey = shared_prev;

        byte[] array = SHA256.Create().ComputeHash(this.sharedKey);
        this.crypto = Aes.Create();
        this.crypto.Key = array;
        this.crypto.IV = new byte[16];
        this.crypto.Padding = PaddingMode.PKCS7;
        this.crypto.Mode = CipherMode.CBC;
    }

    public void DeriveSharedKey(byte[] otherPartyPublicKey)
    {
        BigInteger bigInteger = new BigInteger(otherPartyPublicKey);
        var shared_prev = this.CalculateSharedKey(bigInteger).ToByteArray();
        this.sharedKey = shared_prev;

        byte[] array = SHA256.Create().ComputeHash(this.sharedKey);
        this.crypto = Aes.Create();
        this.crypto.Key = array;
        this.crypto.IV = new byte[16];
        this.crypto.Padding = PaddingMode.PKCS7;
        this.crypto.Mode = CipherMode.CBC;
    }

    public bool IsInitialized
    {
        get
        {
            return this.crypto != null;
        }
    }
    public byte[] PublicKey
    {
        get
        {
            return MsBigIntArrayToPhotonBigIntArray(this.publicKey.ToByteArray());
        }
    }

    public byte[] PublicKeyForServer
    {
        get
        {
            return this.publicKey.ToByteArray();
        }
    }
    private BigInteger CalculatePublicKey()
    {

        return BigInteger.ModPow(primeRoot, this.secret, this.prime);
    }
    private BigInteger CalculateSharedKey(BigInteger otherPartyPublicKey)
    {
        return BigInteger.ModPow(otherPartyPublicKey, this.secret, this.prime);
    }

    private BigInteger GenerateRandomSecret(int secretLength)
    {
        BigInteger result;
        do
        {
            result = GenerateRandom(secretLength);
        }
        while (result >= this.prime - 1 || result < 2);
        return result;
    }

    private static BigInteger GenerateRandom(int bits)
    {
        int bytes = bits >> 3;
        if ((bits & 7) > 0)
        {
            bytes++;
        }
        byte[] randomBytes = new byte[bytes];
        rng.GetBytes(randomBytes);
        if ((randomBytes[bytes - 1] & 128) != 0)
        {
            byte[] temp = new byte[bytes + 1];
            Buffer.BlockCopy(randomBytes, 0, temp, 0, bytes);
            randomBytes = temp;
        }
        return new BigInteger(randomBytes);
    }

    public static byte[] PhotonBigIntArrayToMsBigIntArray(byte[] array)
    {
        Array.Reverse(array);
        bool flag = (array[array.Length - 1] & 128) == 128;
        byte[] array3;
        if (flag)
        {
            byte[] array2 = new byte[array.Length + 1];
            Buffer.BlockCopy(array, 0, array2, 0, array.Length);
            array3 = array2;
        }
        else
        {
            array3 = array;
        }
        return array3;
    }

    public static byte[] MsBigIntArrayToPhotonBigIntArray(byte[] array)
    {
        Array.Reverse(array);
        bool flag = array[0] == 0;
        byte[] array3;
        if (flag)
        {
            byte[] array2 = new byte[array.Length - 1];
            Buffer.BlockCopy(array, 1, array2, 0, array.Length - 1);
            array3 = array2;
        }
        else
        {
            array3 = array;
        }
        return array3;
    }

    public byte[] Encrypt(byte[] data)
    {
        return this.Encrypt(data, 0, data.Length);
    }
    public byte[] Encrypt(byte[] data, int offset, int count)
    {
        if (this.crypto == null)
            throw new InvalidOperationException("Cannot Encrypt");
        using ICryptoTransform cryptoTransform = this.crypto.CreateEncryptor();
        return cryptoTransform.TransformFinalBlock(data, offset, count);
    }

    public byte[] Decrypt(byte[] data)
    {
        return this.Decrypt(data, 0, data.Length);
    }

    public byte[] Decrypt(byte[] data, int offset, int count)
    {
        if (this.crypto == null)
            throw new InvalidOperationException("Cannot Decrypt");
        using ICryptoTransform cryptoTransform = this.crypto.CreateDecryptor();
        return cryptoTransform.TransformFinalBlock(data, offset, count);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override string ToString()
    {
        string s = string.Empty;
        if (crypto != null)
        {
            s += "Key: " + BitConverter.ToString(this.crypto.Key).Replace("-", string.Empty) + "\n";
        }
        s += BitConverter.ToString(publicKey.ToByteArray()).Replace("-", string.Empty) + "\n";
        s += BitConverter.ToString(secret.ToByteArray()).Replace("-", string.Empty) + "\n";
        if (sharedKey != null)
        {
            s += BitConverter.ToString(this.sharedKey).Replace("-", string.Empty) + "\n";
        }
        return s;
    }
}
