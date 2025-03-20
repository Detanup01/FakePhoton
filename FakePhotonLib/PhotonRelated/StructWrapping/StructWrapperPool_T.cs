namespace FakePhotonLib.PhotonRelated.StructWrapping;

public class StructWrapperPool<T> : StructWrapperPool
{
    public const int GROWBY = 4;
    public readonly Type tType = typeof(T);
    public readonly WrappedType wType = StructWrapperPool.GetWrappedType(typeof(T));
    public Stack<StructWrapper<T>> pool;
    public readonly bool isStaticPool;

    public StructWrapperPool(bool isStaticPool)
    {
        this.pool = new Stack<StructWrapper<T>>();
        this.isStaticPool = isStaticPool;
    }

    public StructWrapper<T> Acquire()
    {
        StructWrapper<T> structWrapper;
        if (this.pool.Count == 0)
        {
            int num = 1;
            while (true)
            {
                structWrapper = new StructWrapper<T>(this.isStaticPool ? Pooling.Connected | Pooling.ReleaseOnUnwrap : Pooling.Connected, this.tType, this.wType);
                structWrapper.ReturnPool = this;
                if (num != 4)
                {
                    this.pool.Push(structWrapper);
                    ++num;
                }
                else
                    break;
            }
        }
        else
            structWrapper = this.pool.Pop();
        structWrapper.pooling |= Pooling.CheckedOut;
        return structWrapper;
    }

    public StructWrapper<T> Acquire(T value)
    {
        StructWrapper<T> structWrapper = this.Acquire();
        structWrapper.value = value;
        return structWrapper;
    }

    public int Count => this.pool.Count;

    internal void Release(StructWrapper<T> obj)
    {
        obj.pooling &= ~Pooling.CheckedOut;
        this.pool.Push(obj);
    }
}
