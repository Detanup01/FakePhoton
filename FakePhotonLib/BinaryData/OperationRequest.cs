
using FakePhotonLib.Protocols;

namespace FakePhotonLib.BinaryData;

public class OperationRequest : IBinaryData
{
    public byte OperationCode;
    public Dictionary<byte, object> Parameters = [];

    public Type Type => typeof(OperationRequest);

    public void Read(BinaryReader reader)
    {
        OperationCode = reader.ReadByte();
        Parameters = reader.ReadParameterDictionary();
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
        return $"OperationCode: {OperationCode} ParametersCount : {Parameters.Count}";
    }
}
