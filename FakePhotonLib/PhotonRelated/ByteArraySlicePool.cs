namespace FakePhotonLib.PhotonRelated;

public class ByteArraySlicePool
{
    private int minStackIndex = 7;
    internal readonly Stack<ByteArraySlice>[] poolTiers = new Stack<ByteArraySlice>[32];
    private int allocationCounter;

    public int MinStackIndex
    {
        get => minStackIndex;
        set => minStackIndex = Math.Clamp(value, 1, 31);
    }

    public int AllocationCounter => allocationCounter;

    public ByteArraySlicePool()
    {
        poolTiers[0] = new Stack<ByteArraySlice>();
    }

    public ByteArraySlice Acquire(byte[] buffer, int offset = 0, int count = 0)
    {
        var byteArraySlice = GetOrCreateSlice(0);
        byteArraySlice.Buffer = buffer;
        byteArraySlice.Offset = offset;
        byteArraySlice.Count = count;
        return byteArraySlice;
    }

    public ByteArraySlice Acquire(int minByteCount)
    {
        if (minByteCount < 0)
            throw new ArgumentException("minByteCount must be positive.", nameof(minByteCount));

        int stackIndex = CalculateStackIndex(minByteCount);
        return GetOrCreateSlice(stackIndex);
    }

    private int CalculateStackIndex(int minByteCount)
    {
        int index = minStackIndex;
        if (minByteCount > 0)
        {
            int num = minByteCount - 1;
            while (index < 32 && num >> index != 0)
                index++;
        }
        return index;
    }

    private ByteArraySlice GetOrCreateSlice(int stackIndex)
    {
        lock (poolTiers)
        {
            var stack = poolTiers[stackIndex] ??= new Stack<ByteArraySlice>();
            lock (stack)
            {
                if (stack.Count > 0)
                    return stack.Pop();
            }
            allocationCounter++;
            return new ByteArraySlice(this, stackIndex);
        }
    }

    internal bool Release(ByteArraySlice slice, int stackIndex)
    {
        if (slice == null || stackIndex < 0) return false;
        if (stackIndex == 0) slice.Buffer = null;
        lock (poolTiers[stackIndex])
        {
            poolTiers[stackIndex].Push(slice);
        }
        return true;
    }

    public void ClearPools(int lower = 0, int upper = int.MaxValue)
    {
        for (int i = 0; i < 32; i++)
        {
            int size = 1 << i;
            if (size >= lower && size <= upper)
            {
                lock (poolTiers)
                {
                    poolTiers[i]?.Clear();
                }
            }
        }
    }
}