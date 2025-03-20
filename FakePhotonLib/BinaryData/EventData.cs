namespace FakePhotonLib.BinaryData;

public class EventData
{
    public byte Code;
    public Dictionary<byte, object> Parameters = [];
    public byte SenderKey = 254;
    public int sender = -1;
    public byte CustomDataKey = 245;
    public object? customData;
    public int Sender
    {
        get
        {
            bool flag = this.sender == -1;
            if (flag)
            {
                object? num;
                this.sender = (this.Parameters.TryGetValue(this.SenderKey, out num) ? (int)num : (-1));
            }
            return this.sender;
        }
        internal set
        {
            this.sender = value;
        }
    }

    public object? CustomData
    {
        get
        {
            bool flag = this.customData == null;
            if (flag)
            {
                this.Parameters.TryGetValue(this.CustomDataKey, out this.customData);
            }
            return this.customData;
        }
        internal set
        {
            this.customData = value;
        }
    }

    internal void Reset()
    {
        this.Code = 0;
        this.Parameters.Clear();
        this.sender = -1;
        this.customData = null;
    }
}
