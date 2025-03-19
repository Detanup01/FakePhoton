

namespace FakePhotonLib.BinaryData;

public class EventData : IBinaryData
{
    public byte Code;
    public Dictionary<byte, object> Parameters = [];
    public byte SenderKey = 254;
    private int sender = -1;
    public byte CustomDataKey = 245;
    private object customData;

    public Type Type => typeof(EventData);

    public void Reset()
    {

    }

    public void Read(BinaryReader reader)
    {

    }

    public void Write(BinaryWriter writer)
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
}
