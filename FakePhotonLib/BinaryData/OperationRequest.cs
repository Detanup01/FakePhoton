namespace FakePhotonLib.BinaryData;

public class OperationRequest
{
    public byte OperationCode;
    public Dictionary<byte, object?> Parameters = [];

    public override string ToString()
    {

        return $"(OperationRequest) OperationCode: {OperationCode} ({(OperationCodeEnum)OperationCode}) Count: {Parameters.Count} {string.Join(", ", Parameters.Keys)}";
    }
}
