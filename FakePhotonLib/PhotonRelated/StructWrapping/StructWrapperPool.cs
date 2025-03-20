namespace FakePhotonLib.PhotonRelated.StructWrapping;

public class StructWrapperPool
{
    public static WrappedType GetWrappedType(Type type)
    {
        if (type == typeof(bool))
            return WrappedType.Bool;
        if (type == typeof(byte))
            return WrappedType.Byte;
        if (type == typeof(short))
            return WrappedType.Int16;
        if (type == typeof(int))
            return WrappedType.Int32;
        if (type == typeof(long))
            return WrappedType.Int64;
        if (type == typeof(float))
            return WrappedType.Single;
        return type == typeof(double) ? WrappedType.Double : WrappedType.Unknown;
    }
}
