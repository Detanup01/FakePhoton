using System.Numerics;

namespace FakePhotonLib.PhotonRelated.StructWrapping;

public abstract class StructWrapper(Type ttype, WrappedType wrappedType) : IDisposable
{
    public readonly WrappedType wrappedType = wrappedType;
    public readonly Type ttype = ttype;

    public abstract object Box();

    public abstract void DisconnectFromPool();

    public abstract void Dispose();

    public abstract string ToString(bool writeType);

    public static implicit operator StructWrapper(bool value) => value.Wrap();

    public static implicit operator StructWrapper(byte value) => value.Wrap();

    public static implicit operator StructWrapper(float value) => value.Wrap();

    public static implicit operator StructWrapper(double value) => value.Wrap();

    public static implicit operator StructWrapper(short value) => value.Wrap();

    public static implicit operator StructWrapper(int value) => value.Wrap();

    public static implicit operator StructWrapper(long value) => value.Wrap();

    public static implicit operator bool(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<bool>)!.Unwrap();
    }

    public static implicit operator byte(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<byte>)!.Unwrap();
    }

    public static implicit operator float(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<float>)!.Unwrap();
    }

    public static implicit operator double(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<double>)!.Unwrap();
    }

    public static implicit operator short(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<short>)!.Unwrap();
    }

    public static implicit operator int(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<int>)!.Unwrap();
    }

    public static implicit operator long(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<long>)!.Unwrap();
    }

    public static implicit operator StructWrapper(Vector2 value) => value.Wrap();

    public static implicit operator StructWrapper(Vector3 value) => value.Wrap();

    public static implicit operator StructWrapper(Quaternion value) => value.Wrap();

    public static implicit operator Vector2(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<Vector2>)!.Unwrap();
    }

    public static implicit operator Vector3(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<Vector3>)!.Unwrap();
    }

    public static implicit operator Quaternion(StructWrapper wrapper)
    {
        return (wrapper as StructWrapper<Quaternion>)!.Unwrap();
    }
}
