namespace FakePhotonLib.PhotonRelated;

public class ByteArraySlice : IDisposable
{
    public byte[]? Buffer;
    public int Offset;
    public int Count;
    private readonly ByteArraySlicePool? returnPool;
    private readonly int stackIndex;

    internal ByteArraySlice(ByteArraySlicePool? returnPool, int stackIndex)
    {
        Buffer = stackIndex == 0 ? null : new byte[1 << stackIndex];
        this.returnPool = returnPool;
        this.stackIndex = stackIndex;
    }

    public ByteArraySlice(byte[] buffer, int offset = 0, int count = 0)
    {
        this.Buffer = buffer;
        this.Count = count;
        this.Offset = offset;
        this.returnPool = null;
        this.stackIndex = -1;
    }

    public ByteArraySlice() : this(null, -1)
    {
    }

    public void Dispose() => Release();

    public bool Release()
    {
        if (stackIndex < 0) return false;
        Count = 0;
        Offset = 0;
        return returnPool?.Release(this, stackIndex) ?? true;
    }

    public void Reset()
    {
        Count = 0;
        Offset = 0;
    }
}
