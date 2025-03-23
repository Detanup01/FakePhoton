namespace FakePhotonLib.BinaryData;

public class Header
{
    public bool IsServer => PeerId == 0;
    public short PeerId = -1;
    public byte CrcOrEncrypted;
    public byte CommandCount;
    public int ServerTime;
    public int Challenge;

    public List<CommandPacket> Commands = [];


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
        writer.WriteInt16Big(PeerId);
        writer.Write(CrcOrEncrypted);
        writer.Write(CommandCount);
        writer.WriteInt32Big(ServerTime);
        writer.WriteInt32Big(Challenge);
    }
    
    public override string ToString()
    {
        return $"Is Sent by Server: {IsServer} PeerId: {PeerId:x2} CrcOrEncrypted: {CrcOrEncrypted} CommandCount: {CommandCount} ServerTime: {ServerTime:x2} Challenge: {Challenge:x2}";
    }
}
