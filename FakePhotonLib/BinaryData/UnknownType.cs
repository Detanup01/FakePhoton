namespace FakePhotonLib.BinaryData;

class UnknownType : IBinaryData
{
    public byte TypeCode;
    public int Size;
    public byte[]? Data;

    public Type Type => typeof(UnknownType);

    public void Read(BinaryReader reader)
    {
        
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
}
