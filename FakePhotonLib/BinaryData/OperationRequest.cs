namespace FakePhotonLib.BinaryData;

public class OperationRequest
{
    public byte OperationCode;
    public Dictionary<byte, object?> Parameters = [];
}
