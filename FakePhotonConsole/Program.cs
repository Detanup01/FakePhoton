using FakePhotonLib.Managers;
using FakePhotonLib.PacketAnalyzer;
using FakePhotonLib.Testings;

namespace FakePhotonConsole;

internal class Program
{
    static void Main(string[] args)
    {
        
        LogManager.CreateNew();
        BinaryRWTest.protocol18_test();
        //Analyze.Init();
        //EncryptionManager.EncryptionTest();
        Console.ReadLine();
        LogManager.Close();

        
        /*
        LogManager.CreateNew();
        ServerManager.Start();

        Console.ReadLine();

        ServerManager.Stop();
        LogManager.Close();
        */
    }
}