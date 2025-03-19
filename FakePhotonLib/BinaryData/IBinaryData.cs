namespace FakePhotonLib.BinaryData;

public interface IBinaryData
{
    public Type Type { get; }
    public void Reset();
    public void Read(BinaryReader reader);
    public void Write(BinaryWriter writer);
    public object ParseToObject();
    public object ReadToObject(BinaryReader reader);
}
