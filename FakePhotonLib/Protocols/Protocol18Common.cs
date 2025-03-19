using FakePhotonLib.BinaryData;
using System.Collections;

namespace FakePhotonLib.Protocols;

public class Protocol18Common
{
    public static Type GetAllowedDictionaryKeyTypes(GpType gpType)
    {
        switch (gpType)
        {
            case GpType.Byte:
            case GpType.ByteZero:
                return typeof(byte);
            case GpType.Short:
            case GpType.ShortZero:
                return typeof(short);
            case GpType.Float:
            case GpType.FloatZero:
                return typeof(float);
            case GpType.Double:
            case GpType.DoubleZero:
                return typeof(double);
            case GpType.String:
                return typeof(string);
            case GpType.CompressedInt:
            case GpType.Int1:
            case GpType.Int1_:
            case GpType.Int2:
            case GpType.Int2_:
            case GpType.IntZero:
                return typeof(int);
            case GpType.CompressedLong:
            case GpType.L1:
            case GpType.L1_:
            case GpType.L2:
            case GpType.L2_:
            case GpType.LongZero:
                return typeof(long);
        }
        throw new Exception(string.Format("{0} is not a valid Type as Dictionary key.", gpType));
    }

    public static Type? GetClrArrayType(GpType gpType)
    {
        switch (gpType)
        {
            case GpType.Boolean:
            case GpType.BooleanFalse:
            case GpType.BooleanTrue:
                return typeof(bool);
            case GpType.Byte:
            case GpType.ByteZero:
                return typeof(byte);
            case GpType.Short:
            case GpType.ShortZero:
                return typeof(short);
            case GpType.Float:
            case GpType.FloatZero:
                return typeof(float);
            case GpType.Double:
            case GpType.DoubleZero:
                return typeof(double);
            case GpType.String:
                return typeof(string);
            case GpType.Null:
            case GpType.Custom:
            case GpType.Dictionary:
            case (GpType)22:
            case GpType.ObjectArray:
                break;
            case GpType.CompressedInt:
            case GpType.Int1:
            case GpType.Int1_:
            case GpType.Int2:
            case GpType.Int2_:
            case GpType.IntZero:
                return typeof(int);
            case GpType.CompressedLong:
            case GpType.L1:
            case GpType.L1_:
            case GpType.L2:
            case GpType.L2_:
            case GpType.LongZero:
                return typeof(long);
            case GpType.Hashtable:
                return typeof(Hashtable);
            case GpType.OperationRequest:
                return typeof(OperationRequest);
            case GpType.OperationResponse:
                return typeof(OperationResponse);
            case GpType.EventData:
                return typeof(EventData);
            default:
                switch (gpType)
                {
                    case GpType.BooleanArray:
                        return typeof(bool[]);
                    case GpType.ByteArray:
                        return typeof(byte[]);
                    case GpType.ShortArray:
                        return typeof(short[]);
                    case GpType.FloatArray:
                        return typeof(float[]);
                    case GpType.DoubleArray:
                        return typeof(double[]);
                    case GpType.StringArray:
                        return typeof(string[]);
                    case (GpType)72:
                        break;
                    case GpType.CompressedIntArray:
                        return typeof(int[]);
                    case GpType.CompressedLongArray:
                        return typeof(long[]);
                    default:
                        if (gpType == GpType.HashtableArray)
                        {
                            return typeof(Hashtable[]);
                        }
                        break;
                }
                break;
        }
        return null;
    }

    public static GpType GetCodeOfType(Type type)
    {
        if (type == null)
        {
            return GpType.Null;
        }
        if (type.IsPrimitive || type.IsEnum)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            return GetCodeOfTypeCode(typeCode);
        }
        if (type == typeof(string))
            return GpType.String;
        if (type == typeof(Hashtable))
            return GpType.Hashtable;
        if (type == typeof(List<object>))
            return GpType.ObjectArray;
        if (type.IsGenericType && typeof(Dictionary<,>) == type.GetGenericTypeDefinition())
            return GpType.Dictionary;
        if (type == typeof(OperationResponse))
            return GpType.OperationResponse;
        if (type == typeof(EventData))
            return GpType.EventData;
        if (type == typeof(OperationRequest))
            return GpType.OperationRequest;

        if (type.IsArray)
        {
            Type? elementType = type.GetElementType();
            if (elementType == null)
            {
                throw new InvalidDataException(string.Format("Arrays of type {0} are not supported", type));
            }
            if (elementType.IsPrimitive)
            {
                switch (Type.GetTypeCode(elementType))
                {
                    case TypeCode.Boolean:
                        return GpType.BooleanArray;
                    case TypeCode.Byte:
                        return GpType.ByteArray;
                    case TypeCode.Int16:
                        return GpType.ShortArray;
                    case TypeCode.Int32:
                        return GpType.CompressedIntArray;
                    case TypeCode.Int64:
                        return GpType.CompressedLongArray;
                    case TypeCode.Single:
                        return GpType.FloatArray;
                    case TypeCode.Double:
                        return GpType.DoubleArray;
                }
            }
            if (type == typeof(string))
                return GpType.StringArray;
            if (type == typeof(object))
                return GpType.ObjectArray;
            if (type == typeof(Hashtable))
                return GpType.HashtableArray;
            if (elementType.IsGenericType && typeof(Dictionary<,>) == elementType.GetGenericTypeDefinition())
            {
                return GpType.DictionaryArray;
            }
            else
            {
                return GpType.CustomTypeArray;
            }
        }
        return GpType.Unknown;
    }

    public static GpType GetCodeOfTypeCode(TypeCode type)
    {
        switch (type)
        {
            case TypeCode.Boolean:
                return GpType.Boolean;
            case TypeCode.Char:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                break;
            case TypeCode.Byte:
                return GpType.Byte;
            case TypeCode.Int16:
                return GpType.Short;
            case TypeCode.Int32:
                return GpType.CompressedInt;
            case TypeCode.Int64:
                return GpType.CompressedLong;
            case TypeCode.Single:
                return GpType.Float;
            case TypeCode.Double:
                return GpType.Double;
            default:
                if (type == TypeCode.String)
                {
                    return GpType.String;
                }
                break;
        }
        return GpType.Unknown;
    }

    public static Type GetDictArrayType(BinaryReader stream)
    {
        GpType gpType = (GpType)stream.ReadByte();
        int num = 0;
        while (gpType == GpType.Array)
        {
            num++;
            gpType = (GpType)stream.ReadByte();
        }
        Type? clrArrayType = GetClrArrayType(gpType);
        if (clrArrayType == null)
            throw new Exception("GetClrArrayType is null!");
        Type type = clrArrayType.MakeArrayType();
        uint num2 = 0U;
        while ((ulong)num2 < (ulong)((long)num))
        {
            type = type.MakeArrayType();
            num2 += 1U;
        }
        return type;
    }

    public static readonly byte[] boolMasks = [1, 2, 4, 8, 16, 32, 64, 128];
}

public enum GpType : byte
{
    // Token: 0x04000240 RID: 576
    Unknown,
    // Token: 0x04000241 RID: 577
    Boolean = 2,
    // Token: 0x04000242 RID: 578
    Byte,
    // Token: 0x04000243 RID: 579
    Short,
    // Token: 0x04000244 RID: 580
    Float,
    // Token: 0x04000245 RID: 581
    Double,
    // Token: 0x04000246 RID: 582
    String,
    // Token: 0x04000247 RID: 583
    Null,
    // Token: 0x04000248 RID: 584
    CompressedInt,
    // Token: 0x04000249 RID: 585
    CompressedLong,
    // Token: 0x0400024A RID: 586
    Int1,
    // Token: 0x0400024B RID: 587
    Int1_,
    // Token: 0x0400024C RID: 588
    Int2,
    // Token: 0x0400024D RID: 589
    Int2_,
    // Token: 0x0400024E RID: 590
    L1,
    // Token: 0x0400024F RID: 591
    L1_,
    // Token: 0x04000250 RID: 592
    L2,
    // Token: 0x04000251 RID: 593
    L2_,
    // Token: 0x04000252 RID: 594
    Custom,
    // Token: 0x04000253 RID: 595
    CustomTypeSlim = 128,
    // Token: 0x04000254 RID: 596
    Dictionary = 20,
    // Token: 0x04000255 RID: 597
    Hashtable,
    // Token: 0x04000256 RID: 598
    ObjectArray = 23,
    // Token: 0x04000257 RID: 599
    OperationRequest,
    // Token: 0x04000258 RID: 600
    OperationResponse,
    // Token: 0x04000259 RID: 601
    EventData,
    // Token: 0x0400025A RID: 602
    BooleanFalse,
    // Token: 0x0400025B RID: 603
    BooleanTrue,
    // Token: 0x0400025C RID: 604
    ShortZero,
    // Token: 0x0400025D RID: 605
    IntZero,
    // Token: 0x0400025E RID: 606
    LongZero,
    // Token: 0x0400025F RID: 607
    FloatZero,
    // Token: 0x04000260 RID: 608
    DoubleZero,
    // Token: 0x04000261 RID: 609
    ByteZero,
    // Token: 0x04000262 RID: 610
    Array = 64,
    // Token: 0x04000263 RID: 611
    BooleanArray = 66,
    // Token: 0x04000264 RID: 612
    ByteArray,
    // Token: 0x04000265 RID: 613
    ShortArray,
    // Token: 0x04000266 RID: 614
    DoubleArray = 70,
    // Token: 0x04000267 RID: 615
    FloatArray = 69,
    // Token: 0x04000268 RID: 616
    StringArray = 71,
    // Token: 0x04000269 RID: 617
    HashtableArray = 85,
    // Token: 0x0400026A RID: 618
    DictionaryArray = 84,
    // Token: 0x0400026B RID: 619
    CustomTypeArray = 83,
    // Token: 0x0400026C RID: 620
    CompressedIntArray = 73,
    // Token: 0x0400026D RID: 621
    CompressedLongArray
}
