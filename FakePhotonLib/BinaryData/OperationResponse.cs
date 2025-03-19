using FakePhotonLib.Protocols;

namespace FakePhotonLib.BinaryData;

public class OperationResponse : IBinaryData
{
    public byte Code;
    public short ReturnCode;
    public string? DebugMessage;
    public Dictionary<byte, object> Parameter = [];

    public Type Type => typeof(OperationResponse);

    public void Read(BinaryReader reader)
    {
        Code = reader.ReadByte();
        ReturnCode = reader.ReadInt16Big();
        DebugMessage = reader.ReadObject(reader.ReadByte()) as string;
        Parameter = reader.ReadParameterDictionary();
    }

    public object ParseToObject()
    {
        return this;
    }

    public object ReadToObject(BinaryReader reader)
    {
        this.Reset();
        this.Read(reader);
        return this;
    }

    public void Reset()
    {
        
    }

    public void Write(BinaryWriter writer)
    {
        
    }

    public override string ToString()
    {
        return $"Code: {Code} ReturnCode: {ReturnCode} DebugMessage: {DebugMessage} DataCount : {Parameter.Count}";
    }
}
