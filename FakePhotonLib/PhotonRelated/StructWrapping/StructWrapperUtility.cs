using System.Collections;

namespace FakePhotonLib.PhotonRelated.StructWrapping;

public static class StructWrapperUtility
{
    public static Type GetWrappedType(this object obj)
    {
        return !(obj is StructWrapper structWrapper) ? obj.GetType() : structWrapper.ttype;
    }

    public static StructWrapper<T> Wrap<T>(this T value, bool persistant)
    {
        StructWrapper<T> structWrapper = StructWrapper<T>.staticPool.Acquire(value);
        if (persistant)
            structWrapper.DisconnectFromPool();
        return structWrapper;
    }

    public static StructWrapper<T> Wrap<T>(this T value)
    {
        return StructWrapper<T>.staticPool.Acquire(value);
    }

    public static StructWrapper<byte> Wrap(this byte value)
    {
        return StructWrapperPools.mappedByteWrappers[(int)value];
    }

    public static StructWrapper<bool> Wrap(this bool value)
    {
        return StructWrapperPools.mappedBoolWrappers[value ? 1 : 0];
    }

    public static bool IsType<T>(this object obj)
    {
        switch (obj)
        {
            case T _:
                return true;
            case StructWrapper<T> _:
                return true;
            default:
                return false;
        }
    }

    public static T DisconnectPooling<T>(this T table) where T : IEnumerable<object>
    {
        foreach (object obj in table)
        {
            if (obj is StructWrapper structWrapper)
                structWrapper.DisconnectFromPool();
        }
        return table;
    }

    public static List<object> ReleaseAllWrappers(this List<object> collection)
    {
        foreach (object obj in collection)
        {
            if (obj is StructWrapper structWrapper)
                structWrapper.Dispose();
        }
        return collection;
    }

    public static object[] ReleaseAllWrappers(this object[] collection)
    {
        foreach (object obj in collection)
        {
            if (obj is StructWrapper structWrapper)
                structWrapper.Dispose();
        }
        return collection;
    }

    public static Hashtable ReleaseAllWrappers(this Hashtable table)
    {
        foreach (object obj in table.Values)
        {
            if (obj is StructWrapper structWrapper)
                structWrapper.Dispose();
        }
        return table;
    }

    public static void BoxAll(this Hashtable table, bool recursive = false)
    {
        foreach (object obj in table.Values)
        {
            if (recursive && obj is Hashtable table1)
                table1.BoxAll();
            if (obj is StructWrapper structWrapper)
                structWrapper.Box();
        }
    }

    public static T Unwrap<T>(this object obj)
    {
        if (!(obj is StructWrapper<T> structWrapper))
            return (T)obj;
        T obj1 = structWrapper.value;
        if ((structWrapper.pooling & Pooling.ReleaseOnUnwrap) == Pooling.ReleaseOnUnwrap)
            structWrapper.Dispose();
        return structWrapper.value;
    }

    public static T Get<T>(this object obj)
    {
        return !(obj is StructWrapper<T> structWrapper) ? (T)obj : structWrapper.value;
    }

    public static T? Unwrap<T>(this Hashtable table, object key) => table[key]!.Unwrap<T>();

    public static bool TryUnwrapValue<T>(this Hashtable table, byte key, out T value) where T : new()
    {
        object obj;
        if (!table.TryGetValue((object)key, out obj))
        {
            value = new T();
            return false;
        }
        value = obj.Unwrap<T>();
        return true;
    }

    public static bool TryGetValue<T>(this Hashtable table, byte key, out T value) where T : new()
    {
        object obj;
        if (!table.TryGetValue((object)key, out obj))
        {
            value = new T();
            return false;
        }
        value = obj.Get<T>();
        return true;
    }

    public static bool TryGetValue<T>(this Hashtable table, object key, out T value) where T : new()
    {
        object obj;
        if (!table.TryGetValue(key, out obj))
        {
            value = new T();
            return false;
        }
        value = obj.Get<T>();
        return true;
    }

    public static bool TryUnwrapValue<T>(this Hashtable table, object key, out T value) where T : new()
    {
        object obj;
        if (!table.TryGetValue(key, out obj))
        {
            value = new T();
            return false;
        }
        value = obj.Unwrap<T>();
        return true;
    }

    public static T Unwrap<T>(this Hashtable table, byte key) => table[key]!.Unwrap<T>();

    public static T Get<T>(this Hashtable table, byte key) => table[key]!.Get<T>();
}