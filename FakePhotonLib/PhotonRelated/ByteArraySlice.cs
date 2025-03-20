namespace FakePhotonLib.PhotonRelated;

public class ByteArraySlice : IDisposable
{
    public byte[]? Buffer;
    public int Offset;
    public int Count;
    private readonly ByteArraySlicePool? returnPool;
    private readonly int stackIndex;

    internal ByteArraySlice(ByteArraySlicePool returnPool, int stackIndex)
    {
        this.Buffer = stackIndex == 0 ? null : new byte[1 << stackIndex];
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

    public ByteArraySlice()
    {
        this.returnPool = null;
        this.stackIndex = -1;
    }

    public void Dispose() => this.Release();

    public bool Release()
    {
        if (this.stackIndex < 0)
            return false;
        this.Count = 0;
        this.Offset = 0;
        if (returnPool == null)
            return true;
        return returnPool!.Release(this, this.stackIndex);
    }

    public void Reset()
    {
        this.Count = 0;
        this.Offset = 0;
    }
}
