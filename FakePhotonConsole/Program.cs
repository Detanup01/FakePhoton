using FakePhotonLib.Managers;
using FakePhotonLib.PacketAnalyzer;

namespace FakePhotonConsole;

internal class Program
{
    static void Main(string[] args)
    {
        LogManager.CreateNew();
        if (args.Contains("anal"))
            Analyze.Init();
        else
            ServerManager.Start();

        Console.ReadLine();

        ServerManager.Stop();
        LogManager.Close();
        
    }
}