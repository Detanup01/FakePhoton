using FakePhotonLib.Managers;
using FakePhotonLib.PacketAnalyzer;

namespace FakePhotonConsole;

internal class Program
{
    static void Main(string[] args)
    {
        
        LogManager.CreateNew();
        //Analyze.Init();
        EncryptionManager.EncryptionTest();
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