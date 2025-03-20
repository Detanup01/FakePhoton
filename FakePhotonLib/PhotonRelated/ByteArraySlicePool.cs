namespace FakePhotonLib.PhotonRelated;

public class ByteArraySlicePool
{
    private int minStackIndex = 7;
    internal readonly Stack<ByteArraySlice>[] poolTiers = new Stack<ByteArraySlice>[32];
    private int allocationCounter;

    public int MinStackIndex
    {
        get => this.minStackIndex;
        set => this.minStackIndex = value > 0 ? (value < 31 ? value : 31) : 1;
    }

    public int AllocationCounter => this.allocationCounter;

    public ByteArraySlicePool()
    {
        lock (this.poolTiers)
            this.poolTiers[0] = new Stack<ByteArraySlice>();
    }

    public ByteArraySlice Acquire(byte[] buffer, int offset = 0, int count = 0)
    {
        ByteArraySlice byteArraySlice;
        lock (this.poolTiers)
        {
            lock (this.poolTiers[0])
                byteArraySlice = this.PopOrCreate(this.poolTiers[0], 0);
        }
        byteArraySlice.Buffer = buffer;
        byteArraySlice.Offset = offset;
        byteArraySlice.Count = count;
        return byteArraySlice;
    }

    public ByteArraySlice Acquire(int minByteCount)
    {
        if (minByteCount < 0)
            throw new Exception(typeof(ByteArraySlice).Name + " requires a positive minByteCount.");
        int minStackIndex = this.minStackIndex;
        if (minByteCount > 0)
        {
            int num = minByteCount - 1;
            while (minStackIndex < 32 && num >> minStackIndex != 0)
                ++minStackIndex;
        }
        lock (this.poolTiers)
        {
            Stack<ByteArraySlice> stack = this.poolTiers[minStackIndex];
            if (stack == null)
            {
                stack = new Stack<ByteArraySlice>();
                this.poolTiers[minStackIndex] = stack;
            }
            lock (stack)
                return this.PopOrCreate(stack, minStackIndex);
        }
    }

    private ByteArraySlice PopOrCreate(Stack<ByteArraySlice> stack, int stackIndex)
    {
        lock (stack)
        {
            if (stack.Count > 0)
                return stack.Pop();
        }
        ByteArraySlice byteArraySlice = new ByteArraySlice(this, stackIndex);
        ++this.allocationCounter;
        return byteArraySlice;
    }

    internal bool Release(ByteArraySlice slice, int stackIndex)
    {
        if (slice == null || stackIndex < 0)
            return false;
        if (stackIndex == 0)
            slice.Buffer = null;
        lock (this.poolTiers)
        {
            lock (this.poolTiers[stackIndex])
                this.poolTiers[stackIndex].Push(slice);
        }
        return true;
    }

    public void ClearPools(int lower = 0, int upper = 2147483647)
    {
        int minStackIndex = this.minStackIndex;
        for (int index = 0; index < 32; ++index)
        {
            int num = 1 << index;
            if (num >= lower && num <= upper)
            {
                lock (this.poolTiers)
                {
                    if (this.poolTiers[index] != null)
                    {
                        lock (this.poolTiers[index])
                            this.poolTiers[index].Clear();
                    }
                }
            }
        }
    }
}