using FakePhotonLib.Protocols;

namespace FakePhotonLib.BinaryData;

public class OperationResponse 
{
    public byte OperationCode;
    public short ReturnCode;
    public string? DebugMessage;
    public Dictionary<byte, object?> Parameters = [];

    public override string ToString()
    {
        return $"Code: {OperationCode} ReturnCode: {ReturnCode} DebugMessage: {DebugMessage} DataCount : {Parameters.Count}";
    }
}
