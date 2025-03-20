namespace FakePhotonLib.BinaryData;

public class DisconnectMessage
{
    public short Code;
    public string? DebugMessage;
    public Dictionary<byte, object> Parameters = [];
}