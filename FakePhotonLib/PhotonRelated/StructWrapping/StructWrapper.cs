using System.Numerics;

namespace FakePhotonLib.PhotonRelated.StructWrapping;

public abstract class StructWrapper : IDisposable
{
    public readonly WrappedType wrappedType;
    public readonly System.Type ttype;

    public StructWrapper(System.Type ttype, WrappedType wrappedType)
    {
        this.ttype = ttype;
        this.wrappedType = wrappedType;
    }

    public abstract object Box();

    public abstract void DisconnectFromPool();

    public abstract void Dispose();

    public abstract string ToString(bool writeType);

    public static implicit operator StructWrapper(bool value) => (StructWrapper)value.Wrap();

    public static implicit operator StructWrapper(byte value) => (StructWrapper)value.Wrap();

    public static implicit operator StructWrapper(float value)
    {
        return (StructWrapper)value.Wrap<float>();
    }

    public static implicit operator StructWrapper(double value)
    {
        return (StructWrapper)value.Wrap<double>();
    }

    public static implicit operator StructWrapper(short value)
    {
        return (StructWrapper)value.Wrap<short>();
    }

    public static implicit operator StructWrapper(int value) => (StructWrapper)value.Wrap<int>();

    public static implicit operator StructWrapper(long value) => (StructWrapper)value.Wrap<long>();

    public static implicit operator bool(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<bool>).Unwrap();
    }

    public static implicit operator byte(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<byte>).Unwrap();
    }

    public static implicit operator float(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<float>).Unwrap();
    }

    public static implicit operator double(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<double>).Unwrap();
    }

    public static implicit operator short(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<short>).Unwrap();
    }

    public static implicit operator int(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<int>).Unwrap();
    }

    public static implicit operator long(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<long>).Unwrap();
    }

    public static implicit operator StructWrapper(Vector2 value)
    {
        return (StructWrapper)value.Wrap<Vector2>();
    }

    public static implicit operator StructWrapper(Vector3 value)
    {
        return (StructWrapper)value.Wrap<Vector3>();
    }

    public static implicit operator StructWrapper(Quaternion value)
    {
        return (StructWrapper)value.Wrap<Quaternion>();
    }

    public static implicit operator Vector2(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<Vector2>).Unwrap();
    }

    public static implicit operator Vector3(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<Vector3>).Unwrap();
    }

    public static implicit operator Quaternion(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<Quaternion>).Unwrap();
    }
}
