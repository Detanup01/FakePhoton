using FakePhotonLib.PhotonRelated;
using System.Buffers.Binary;

namespace FakePhotonLib.BinaryData;

public class Header
{
    public bool IsServer => PeerId == 0;
    public short PeerId = -1;
    public byte CrcOrEncrypted;
    public byte CommandCount;
    public int ServerTime;
    public int Challenge;

    public List<NCommand> Commands = [];


    public void Read(BinaryReader reader)
    {
        PeerId = reader.ReadInt16Big();
        CrcOrEncrypted = reader.ReadByte();
        CommandCount = reader.ReadByte();
        ServerTime = reader.ReadInt32Big();
        Challenge = reader.ReadInt32Big();
    }

    public void Reset()
    {
        PeerId = -1;
        CrcOrEncrypted = 0;
        CommandCount = 0;
        ServerTime = 0;
        Challenge = 0;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(BinaryPrimitives.ReverseEndianness(PeerId));
        writer.Write(CrcOrEncrypted);
        writer.Write(CommandCount);
        writer.Write(BinaryPrimitives.ReverseEndianness(ServerTime));
        writer.Write(BinaryPrimitives.ReverseEndianness(Challenge));
    }
    
    public override string ToString()
    {
        return $"Is Sent by Server: {IsServer} PeerId: {PeerId} CrcOrEncrypted: {CrcOrEncrypted} CommandCount: {CommandCount} ServerTime: {ServerTime} Challenge: {Challenge:x2}";
    }
}
