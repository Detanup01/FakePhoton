namespace FakePhotonLib.PhotonRelated.StructWrapping;

public class StructWrapper<T> : StructWrapper
{
    internal Pooling pooling;
    internal T value;
    internal static StructWrapperPool<T> staticPool = new StructWrapperPool<T>(true);

    public StructWrapperPool<T> ReturnPool { get; internal set; }

    public StructWrapper(Pooling releasing)
      : base(typeof(T), StructWrapperPool.GetWrappedType(typeof(T)))
    {
        this.pooling = releasing;
    }

    public StructWrapper(Pooling releasing, Type tType, WrappedType wType)
      : base(tType, wType)
    {
        this.pooling = releasing;
    }

    public StructWrapper<T> Poke(byte value)
    {
        if (this.pooling == Pooling.Readonly)
            throw new InvalidOperationException("Trying to Poke the value of a readonly StructWrapper<byte>. Value cannot be modified.");
        return this;
    }

    public StructWrapper<T> Poke(bool value)
    {
        if (this.pooling == Pooling.Readonly)
            throw new InvalidOperationException("Trying to Poke the value of a readonly StructWrapper<bool>. Value cannot be modified.");
        return this;
    }

    public StructWrapper<T> Poke(T value)
    {
        this.value = value;
        return this;
    }

    public T Unwrap()
    {
        T obj = this.value;
        if (this.pooling != Pooling.Readonly)
            this.ReturnPool.Release(this);
        return obj;
    }

    public T Peek() => this.value;

    public override object Box()
    {
        T obj = this.value;
        if (this.ReturnPool != null)
            this.ReturnPool.Release(this);
        return (object)obj;
    }

    public override void Dispose()
    {
        if ((this.pooling & Pooling.CheckedOut) != Pooling.CheckedOut || this.ReturnPool == null)
            return;
        this.ReturnPool.Release(this);
    }

    public override void DisconnectFromPool()
    {
        if (this.pooling == Pooling.Readonly)
            return;
        this.pooling = Pooling.Disconnected;
        this.ReturnPool = (StructWrapperPool<T>)null;
    }

    public override string ToString() => this.Unwrap().ToString();

    public override string ToString(bool writeTypeInfo)
    {
        return writeTypeInfo ? string.Format("(StructWrapper<{0}>){1}", (object)this.wrappedType, (object)this.Unwrap().ToString()) : this.Unwrap().ToString();
    }

    public static implicit operator StructWrapper<T>(T value)
    {
        return StructWrapper<T>.staticPool.Acquire(value);
    }
}