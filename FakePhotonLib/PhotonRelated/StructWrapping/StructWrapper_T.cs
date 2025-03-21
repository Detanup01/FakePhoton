namespace FakePhotonLib.PhotonRelated.StructWrapping;

public class StructWrapper<T> : StructWrapper
{
    internal Pooling pooling;
    internal T? value;
    internal static StructWrapperPool<T> staticPool = new(true);

    public StructWrapperPool<T>? ReturnPool { get; internal set; }

    public StructWrapper(Pooling releasing)
      : base(typeof(T), StructWrapperPool.GetWrappedType(typeof(T)))
    {
        pooling = releasing;
    }

    public StructWrapper(Pooling releasing, Type tType, WrappedType wType)
      : base(tType, wType)
    {
        pooling = releasing;
    }

    public StructWrapper<T> Poke(byte _)
    {
        if (pooling == Pooling.Readonly)
            throw new InvalidOperationException("Trying to Poke the value of a readonly StructWrapper<byte>. Value cannot be modified.");
        return this;
    }

    public StructWrapper<T> Poke(bool _)
    {
        if (pooling == Pooling.Readonly)
            throw new InvalidOperationException("Trying to Poke the value of a readonly StructWrapper<bool>. Value cannot be modified.");
        return this;
    }

    public StructWrapper<T> Poke(T value)
    {
        this.value = value;
        return this;
    }

    public T? Unwrap()
    {
        T? obj = value;
        if (pooling != Pooling.Readonly)
            ReturnPool?.Release(this);
        return obj;
    }

    public T? Peek() => value;

    public override object Box()
    {
        T? obj = value;
        ReturnPool?.Release(this);
        return (object)obj!;
    }

    public override void Dispose()
    {
        if ((pooling & Pooling.CheckedOut) != Pooling.CheckedOut || ReturnPool == null)
            return;
        GC.SuppressFinalize(this);
        ReturnPool.Release(this);
    }

    public override void DisconnectFromPool()
    {
        if (pooling == Pooling.Readonly)
            return;
        pooling = Pooling.Disconnected;
        ReturnPool = null;
    }

    public override string? ToString() => Unwrap()?.ToString();

    public override string ToString(bool writeTypeInfo)
    {
        return writeTypeInfo ? string.Format("(StructWrapper<{0}>){1}", wrappedType, Unwrap()?.ToString()!) : Unwrap()?.ToString()!;
    }

    public static implicit operator StructWrapper<T>(T value)
    {
        return StructWrapper<T>.staticPool.Acquire(value);
    }
}