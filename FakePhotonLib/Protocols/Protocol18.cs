using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated;
using FakePhotonLib.PhotonRelated.StructWrapping;
using Serilog;
using System.Collections;
using System.Text;

namespace FakePhotonLib.Protocols;

public class Protocol18 : IProtocol
{
    public static readonly StructWrapperPools wrapperPools = new();
    private readonly byte[] versionBytes = [1, 8];
    public override string ProtocolType => "GpBinaryV18";
    public override byte[] VersionBytes => versionBytes;

    public override void Serialize(BinaryWriter dout, object serObject, bool setType)
    {
        Write(dout, serObject, setType);
    }

    public override void SerializeShort(BinaryWriter dout, short serObject, bool setType)
    {
        WriteInt16(dout, serObject, setType);
    }

    public override void SerializeString(BinaryWriter dout, string serObject, bool setType)
    {
        WriteString(dout, serObject, setType);
    }

    public override object? Deserialize(BinaryReader din, byte type, DeserializationFlags flags = DeserializationFlags.None)
    {
        return Read(din, type);
    }

    public override short DeserializeShort(BinaryReader din) => din.ReadInt16Big();

    public override byte DeserializeByte(BinaryReader din) => din.ReadByte();

    private static Type GetAllowedDictionaryKeyTypes(GpType gpType) =>
        gpType switch
        {
            GpType.Byte or GpType.ByteZero => typeof(byte),
            GpType.Short or GpType.ShortZero => typeof(short),
            GpType.Float or GpType.FloatZero => typeof(float),
            GpType.Double or GpType.DoubleZero => typeof(double),
            GpType.String => typeof(string),
            GpType.CompressedInt or GpType.Int1 or GpType.Int1_ or GpType.Int2 or GpType.Int2_ or GpType.IntZero => typeof(int),
            GpType.CompressedLong or GpType.L1 or GpType.L1_ or GpType.L2 or GpType.L2_ or GpType.LongZero => typeof(long),
            _ => throw new Exception($"{gpType} is not a valid Type as Dictionary key.")
        };

    private static Type? GetClrArrayType(GpType gpType) =>
            gpType switch
            {
                GpType.Boolean or GpType.BooleanFalse or GpType.BooleanTrue => typeof(bool),
                GpType.Byte or GpType.ByteZero => typeof(byte),
                GpType.Short or GpType.ShortZero => typeof(short),
                GpType.Float or GpType.FloatZero => typeof(float),
                GpType.Double or GpType.DoubleZero => typeof(double),
                GpType.String => typeof(string),
                GpType.CompressedInt or GpType.Int1 or GpType.Int1_ or GpType.Int2 or GpType.Int2_ or GpType.IntZero => typeof(int),
                GpType.CompressedLong or GpType.L1 or GpType.L1_ or GpType.L2 or GpType.L2_ or GpType.LongZero => typeof(long),
                GpType.Hashtable => typeof(Hashtable),
                GpType.OperationRequest => typeof(OperationRequest),
                GpType.OperationResponse => typeof(OperationResponse),
                GpType.EventData => typeof(EventData),
                GpType.BooleanArray => typeof(bool[]),
                GpType.ByteArray => typeof(byte[]),
                GpType.ShortArray => typeof(short[]),
                GpType.FloatArray => typeof(float[]),
                GpType.DoubleArray => typeof(double[]),
                GpType.StringArray => typeof(string[]),
                GpType.CompressedIntArray => typeof(int[]),
                GpType.CompressedLongArray => typeof(long[]),
                GpType.HashtableArray => typeof(Hashtable[]),
                _ => null
            };

    private static GpType GetCodeOfType(Type type)
    {
        if (type == null)
            return GpType.Null;
        if (type == typeof(StructWrapper<>))
            return GpType.Unknown;

        if (type.IsPrimitive || type.IsEnum)
            return GetCodeOfTypeCode(Type.GetTypeCode(type));

        if (type == typeof(string))
            return GpType.String;

        if (type.IsArray)
        {
            Type? elementType = type.GetElementType();
            ArgumentNullException.ThrowIfNull(elementType);

            if (elementType.IsPrimitive)
            {
                return Type.GetTypeCode(elementType) switch
                {
                    TypeCode.Boolean => GpType.BooleanArray,
                    TypeCode.Byte => GpType.ByteArray,
                    TypeCode.Int16 => GpType.ShortArray,
                    TypeCode.Int32 => GpType.CompressedIntArray,
                    TypeCode.Int64 => GpType.CompressedLongArray,
                    TypeCode.Single => GpType.FloatArray,
                    TypeCode.Double => GpType.DoubleArray,
                    _ => throw new InvalidDataException($"Primitive type {elementType} is not supported")
                };
            }

            if (elementType.IsArray)
                return GpType.Array;
            if (elementType == typeof(string))
                return GpType.StringArray;
            if (elementType == typeof(object) || elementType == typeof(StructWrapper))
                return GpType.ObjectArray;
            if (elementType == typeof(Hashtable))
                return GpType.HashtableArray;

            return elementType.IsGenericType && typeof(Dictionary<,>) == elementType.GetGenericTypeDefinition()
                ? GpType.DictionaryArray
                : GpType.CustomTypeArray;
        }

        return type switch
        {
            _ when type == typeof(Hashtable) => GpType.Hashtable,
            _ when type == typeof(List<object>) => GpType.ObjectArray,
            _ when type.IsGenericType && typeof(Dictionary<,>) == type.GetGenericTypeDefinition() => GpType.Dictionary,
            _ when type == typeof(EventData) => GpType.EventData,
            _ when type == typeof(OperationRequest) => GpType.OperationRequest,
            _ when type == typeof(OperationResponse) => GpType.OperationResponse,
            _ => GpType.Unknown
        };
    }

    private static GpType GetCodeOfTypeCode(TypeCode typeCode) =>
        typeCode switch
        {
            TypeCode.Boolean => GpType.Boolean,
            TypeCode.Byte => GpType.Byte,
            TypeCode.Int16 => GpType.Short,
            TypeCode.Int32 => GpType.CompressedInt,
            TypeCode.Int64 => GpType.CompressedLong,
            TypeCode.Single => GpType.Float,
            TypeCode.Double => GpType.Double,
            TypeCode.String => GpType.String,
            _ => GpType.Unknown
        };

    private object? Read(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters) =>
        Read(stream, stream.ReadByte(), flags, parameters);

    private object? Read(BinaryReader stream, byte gpType, DeserializationFlags flags = DeserializationFlags.None, Dictionary<byte, object?>? parameters = null)
    {
        if (gpType >= 128 && gpType <= 228)
            return ReadCustomType(stream, gpType);
        return gpType switch
        {
            2 => stream.ReadBoolean(),
            3 => stream.ReadByte(),
            4 => stream.ReadInt16Big(),
            5 => stream.ReadSingle(),
            6 => stream.ReadDouble(),
            7 => ReadStringBig(stream),
            8 => null,
            9 => ReadCompressedInt32(stream),
            10 => ReadCompressedInt64(stream),
            11 => ReadInt1(stream, false),
            12 => ReadInt1(stream, true),
            13 => ReadInt2(stream, false),
            14 => ReadInt2(stream, true),
            15 => (long)ReadInt1(stream, false),
            16 => (long)ReadInt1(stream, true),
            17 => (long)ReadInt2(stream, false),
            18 => (long)ReadInt2(stream, true),
            19 => ReadCustomType(stream),
            20 => ReadDictionary(stream, flags, parameters),
            21 => ReadHashtable(stream, flags, parameters),
            23 => ReadObjectArray(stream, flags, parameters),
            24 => DeserializeOperationRequest(stream, DeserializationFlags.None),
            25 => DeserializeOperationResponse(stream, flags),
            26 => DeserializeEventData(stream, null, DeserializationFlags.None),
            27 => false,
            28 => true,
            29 => (short)0,
            30 => 0,
            31 => 0L,
            32 => 0.0f,
            33 => 0.0,
            34 => (byte)0,
            64 => ReadArrayInArray(stream, flags, parameters),
            66 => ReadBooleanArray(stream),
            67 => ReadByteArray(stream),
            68 => ReadInt16Array(stream),
            69 => ReadSingleArray(stream),
            70 => ReadDoubleArray(stream),
            71 => ReadStringArray(stream),
            73 => ReadCompressedInt32Array(stream),
            74 => ReadCompressedInt64Array(stream),
            83 => ReadCustomTypeArray(stream),
            84 => ReadDictionaryArray(stream, flags, parameters),
            85 => ReadHashtableArray(stream, flags, parameters),
            _ => throw new InvalidDataException($"GpTypeCode not found: {gpType}(0x{gpType:X}). Is not a CustomType either. Pos: {stream.BaseStream.Position} Available: {stream.BaseStream.Length - stream.BaseStream.Position}")
        };
    }

    internal ByteArraySlice ReadNonAllocByteArray(BinaryReader stream)
    {
        uint length = ReadCompressedUInt32(stream);
        ByteArraySlice byteArraySlice = ByteArraySlicePool.Acquire((int)length);
        stream.Read(byteArraySlice.Buffer!, 0, (int)length);
        byteArraySlice.Count = (int)length;
        return byteArraySlice;
    }

    internal static byte[] ReadByteArray(BinaryReader stream)
    {
        uint length = ReadCompressedUInt32(stream);
        byte[] buffer = new byte[length];
        stream.Read(buffer, 0, (int)length);
        return buffer;
    }

    public static object ReadCustomType(BinaryReader reader, byte gpType = 0)
    {
        byte typeCode = gpType != 0 ? (byte)(gpType - 128) : reader.ReadByte();
        int length = (int)ReadCompressedUInt32(reader);
        if (length < 0)
            throw new InvalidDataException($"ReadCustomType read negative size value: {length} before position: {reader.BaseStream.Position}");
        bool flag = length <= reader.BaseStream.Length - reader.BaseStream.Position;
        if (!flag || length > short.MaxValue || !Protocol.CodeDict.TryGetValue(typeCode, out CustomType? customType))
        {
            UnknownType unknownType = new() { TypeCode = typeCode, Size = length };
            int count = flag ? length : (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            if (count > 0)
            {
                byte[] buffer = reader.ReadBytes(count);
                unknownType.Data = buffer;
            }
            return unknownType;
        }

        if (customType.DeserializeFunction != null)
        {
            byte[] numArray = reader.ReadBytes(length);
            return customType.DeserializeFunction(numArray);
        }

        long position = reader.BaseStream.Position;
        object result = customType.DeserializeStreamFunction!(reader, (short)length);
        if (reader.BaseStream.Position - position != length)
            reader.BaseStream.Position = position + length;

        return result;
    }


    public override EventData DeserializeEventData(BinaryReader din, EventData? target = null, DeserializationFlags flags = DeserializationFlags.None)
    {
        EventData eventData = target ?? new EventData();
        eventData.Code = din.ReadByte();
        short parameterCount = (short)din.ReadByte();
        bool allowPooledByteArray = (flags & DeserializationFlags.AllowPooledByteArray) == DeserializationFlags.AllowPooledByteArray;

        for (uint i = 0; i < parameterCount; i++)
        {
            byte code = din.ReadByte();
            byte gpType = din.ReadByte();
            object? value;
            if (!allowPooledByteArray)
            {
                value = Read(din, gpType, flags, eventData.Parameters);
            }
            else if (gpType == 67)
            {
                value = ReadNonAllocByteArray(din);
            }
            else if (code == eventData.SenderKey)
            {
                value = gpType switch
                {
                    9 => ReadCompressedInt32(din),
                    11 => ReadInt1(din, false),
                    12 => ReadInt1(din, true),
                    13 => ReadInt2(din, false),
                    14 => ReadInt2(din, true),
                    30 => 0,
                    _ => throw new InvalidDataException($"Unsupported GpType: {gpType}")
                };

                eventData.Sender = (int)value;
                continue;
            }
            else
            {
                value = Read(din, gpType, flags, eventData.Parameters);
            }

            eventData.Parameters.Add(code, value);
        }

        return eventData;
    }

    private Dictionary<byte, object?> ReadParameters(BinaryReader stream, Dictionary<byte, object?>? target = null, DeserializationFlags flags = DeserializationFlags.None)
    {
        short capacity = (short)stream.ReadByte();
        Dictionary<byte, object?> parameters = target ?? new Dictionary<byte, object?>(capacity);
        bool allowPooledByteArray = (flags & DeserializationFlags.AllowPooledByteArray) == DeserializationFlags.AllowPooledByteArray;

        for (uint i = 0; i < capacity; i++)
        {
            byte code = stream.ReadByte();
            byte gpType = stream.ReadByte();
            object? value = allowPooledByteArray && gpType == 67
                ? ReadNonAllocByteArray(stream)
                : Read(stream, gpType, flags, parameters);
            parameters.Add(code, value);
        }

        return parameters;
    }

    public Hashtable ReadHashtable(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        int count = (int)ReadCompressedUInt32(stream);
        Hashtable hashtable = new(count);

        for (uint i = 0; i < count; i++)
        {
            object? key = Read(stream, flags, parameters);
            object? value = Read(stream, flags, parameters);
            if (key == null)
                continue;
            if (key is StructWrapper<byte> structWrapper)
                hashtable[structWrapper.Unwrap<byte>()] = value;
            else
                hashtable[key] = value;
        }
        return hashtable;
    }

    public static int[] ReadIntArray(BinaryReader stream)
    {
        int length = stream.ReadInt32Big();
        int[] array = new int[length];

        for (uint i = 0; i < length; i++)
            array[i] = stream.ReadInt32Big();

        return array;
    }

    public override OperationRequest DeserializeOperationRequest(BinaryReader din, DeserializationFlags flags = DeserializationFlags.None)
    {
        return new()
        {
            OperationCode = din.ReadByte(),
            Parameters = ReadParameters(din, null, flags)
        };
    }

    public override OperationResponse DeserializeOperationResponse(BinaryReader stream, DeserializationFlags flags = DeserializationFlags.None)
    {
        return new()
        {
            OperationCode = stream.ReadByte(),
            ReturnCode = stream.ReadInt16Big(),
            DebugMessage = Read(stream, stream.ReadByte(), flags, null) as string,
            Parameters = ReadParameters(stream, null, flags)
        };
    }

    public override DisconnectMessage DeserializeDisconnectMessage(BinaryReader stream)
    {
        return new()
        {
            Code = stream.ReadInt16Big(),
            DebugMessage = Read(stream, stream.ReadByte()) as string,
            Parameters = ReadParameters(stream)
        };
    }

    internal static string ReadStringBig(BinaryReader stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        if (length == 0)
            return string.Empty;
        return Encoding.UTF8.GetString(stream.ReadBytes(length));
    }

    private static object ReadCustomTypeArray(BinaryReader reader)
    {
        uint length = ReadCompressedUInt32(reader);
        byte typeCode = reader.ReadByte();
        if (!Protocol.CodeDict.TryGetValue(typeCode, out CustomType? customType))
        {
            long position = reader.BaseStream.Position;
            for (uint index = 0; index < length; ++index)
            {
                int num1 = (int)ReadCompressedUInt32(reader);
                long available = reader.BaseStream.Length - reader.BaseStream.Position;
                int num2 = num1 > available ? (int)available : num1;
                reader.BaseStream.Position += num2;
            }
            return new[] { new UnknownType { TypeCode = typeCode, Size = (int)(reader.BaseStream.Position - position) } };
        }

        Array array = Array.CreateInstance(customType.Type, (int)length);
        for (uint i = 0; i < length; i++)
        {
            int size = (int)ReadCompressedUInt32(reader);
            if (size < 0)
                throw new InvalidDataException("ReadCustomTypeArray read negative size value: " + size.ToString() + " before position: " + reader.BaseStream.Position.ToString());
            if (size > reader.BaseStream.Length - reader.BaseStream.Position || size > (int)short.MaxValue)
            {
                reader.BaseStream.Position = reader.BaseStream.Length;
                throw new InvalidDataException("ReadCustomTypeArray read size value: " + size.ToString() + " larger than short.MaxValue or available data: " + (reader.BaseStream.Length - reader.BaseStream.Position).ToString());
            }

            object value;
            if (customType.DeserializeFunction != null)
            {
                byte[] numArray = reader.ReadBytes(size);
                value = customType.DeserializeFunction(numArray);
            }
            else
            {
                long position = reader.BaseStream.Position;
                value = customType.DeserializeStreamFunction!(reader, (short)size);
                if (reader.BaseStream.Position - position != size)
                    reader.BaseStream.Position = position + size;
            }

            if (value != null && customType.Type.IsAssignableFrom(value.GetType()))
                array.SetValue(value, (int)i);
        }

        return array;
    }


    private static Type ReadDictionaryType(BinaryReader stream, out GpType keyReadType, out GpType valueReadType)
    {
        keyReadType = (GpType)stream.ReadByte();
        valueReadType = (GpType)stream.ReadByte();
        Type keyType = keyReadType != GpType.Unknown ? GetAllowedDictionaryKeyTypes(keyReadType) : typeof(object);
        Type? valueType = valueReadType switch
        {
            GpType.Unknown => typeof(object),
            GpType.Dictionary => ReadDictionaryType(stream),
            GpType.ObjectArray => typeof(object[]),
            GpType.Array => GetDictArrayType(stream),
            GpType.HashtableArray => typeof(Hashtable[]),
            _ => GetClrArrayType(valueReadType)
        };
        ArgumentNullException.ThrowIfNull(valueType);

        return typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
    }

    private static Type ReadDictionaryType(BinaryReader stream)
    {
        GpType keyGpType = (GpType)stream.ReadByte();
        GpType valueGpType = (GpType)stream.ReadByte();
        Type keyType = keyGpType != GpType.Unknown ? GetAllowedDictionaryKeyTypes(keyGpType) : typeof(object);
        Type? valueType = valueGpType switch
        {
            GpType.Unknown => typeof(object),
            GpType.Dictionary => ReadDictionaryType(stream),
            GpType.Array => GetDictArrayType(stream),
            _ => GetClrArrayType(valueGpType)
        };
        ArgumentNullException.ThrowIfNull(valueType);
        return typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
    }

    private static Type GetDictArrayType(BinaryReader stream)
    {
        GpType gpType = (GpType)stream.ReadByte();
        int arrayDepth = 0;
        while (gpType == GpType.Array)
        {
            gpType = (GpType)stream.ReadByte();
            arrayDepth++;
        }

        Type arrayType = GetClrArrayType(gpType)!.MakeArrayType();
        for (int i = 0; i < arrayDepth; i++)
            arrayType = arrayType.MakeArrayType();

        return arrayType;
    }

    private IDictionary? ReadDictionary(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        Type dictionaryType = ReadDictionaryType(stream, out GpType keyReadType, out GpType valueReadType);
        if (dictionaryType == null || Activator.CreateInstance(dictionaryType) is not IDictionary dictionary)
            return null;

        ReadDictionaryElements(stream, keyReadType, valueReadType, dictionary, flags, parameters);
        return dictionary;
    }

    private bool ReadDictionaryElements(BinaryReader stream, GpType keyReadType, GpType valueReadType, IDictionary? dictionary, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        uint count = ReadCompressedUInt32(stream);
        for (uint i = 0; i < count; i++)
        {
            object? key = keyReadType == GpType.Unknown ? Read(stream, flags, parameters) : Read(stream, (byte)keyReadType);
            object? value = valueReadType == GpType.Unknown ? Read(stream, flags, parameters) : Read(stream, (byte)valueReadType);
            if (key != null)
                dictionary?.Add(key, value);
        }
        return true;
    }

    private object[] ReadObjectArray(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        object[] array = new object[length];
        for (uint i = 0; i < length; i++)
            array[i] = Read(stream, flags, parameters)!;

        return array;
    }

    private StructWrapper[] ReadWrapperArray(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        StructWrapper[] array = new StructWrapper[length];
        for (uint i = 0; i < length; i++)
        {
            object value = Read(stream, flags, parameters)!;
            array[i] = (StructWrapper)value;
            if (value == null)
                Log.Debug("Error: ReadWrapperArray hit null");
            if (array[i] == null)
                Log.Debug("Error: ReadWrapperArray null wrapper");
        }
        return array;
    }

    private static bool[] ReadBooleanArray(BinaryReader stream)
    {
        uint length = ReadCompressedUInt32(stream);
        bool[] array = new bool[length];
        int byteCount = (int)length / 8;
        int index = 0;

        for (; byteCount > 0; byteCount--)
        {
            byte b = stream.ReadByte();
            for (int j = 0; j < 8; j++, index++)
                array[index] = (b & (1 << j)) != 0;
        }

        if (index < length)
        {
            byte b = stream.ReadByte();
            for (int j = 0; index < length; j++, index++)
                array[index] = (b & (1 << j)) != 0;
        }

        return array;
    }

    internal static short[] ReadInt16Array(BinaryReader stream)
    {
        short[] array = new short[ReadCompressedUInt32(stream)];
        for (int i = 0; i < array.Length; i++)
            array[i] = stream.ReadInt16Big();

        return array;
    }

    private static float[] ReadSingleArray(BinaryReader stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        float[] array = new float[length];
        for (int i = 0; i < array.Length; i++)
            array[i] = stream.ReadSingleBig();
        return array;
    }

    private static double[] ReadDoubleArray(BinaryReader stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        double[] array = new double[length];
        for (int i = 0; i < array.Length; i++)
            array[i] = stream.ReadDoubleBig();
        return array;
    }

    internal static string[] ReadStringArray(BinaryReader stream)
    {
        string[] array = new string[ReadCompressedUInt32(stream)];
        for (int i = 0; i < array.Length; i++)
            array[i] = ReadStringBig(stream);

        return array;
    }

    private Hashtable[] ReadHashtableArray(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        Hashtable[] array = new Hashtable[ReadCompressedUInt32(stream)];
        for (int i = 0; i < array.Length; i++)
            array[i] = ReadHashtable(stream, flags, parameters);

        return array;
    }

    private IDictionary?[] ReadDictionaryArray(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        Type dictionaryType = ReadDictionaryType(stream, out GpType keyReadType, out GpType valueReadType);
        IDictionary?[] array = (IDictionary?[])Array.CreateInstance(dictionaryType, ReadCompressedUInt32(stream));
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (IDictionary?)Activator.CreateInstance(dictionaryType);
            ReadDictionaryElements(stream, keyReadType, valueReadType, array[i], flags, parameters);
        }
        return array;
    }

    private Array? ReadArrayInArray(BinaryReader stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        Array? outerArray = null;
        Type? elementType = null;

        for (uint i = 0; i < length; i++)
        {
            if (Read(stream, flags, parameters) is Array innerArray)
            {
                if (outerArray == null)
                {
                    elementType = innerArray.GetType();
                    outerArray = Array.CreateInstance(elementType, (int)length);
                }
                if (elementType != null && elementType.IsAssignableFrom(innerArray.GetType()))
                    outerArray.SetValue(innerArray, (int)i);
            }
        }

        return outerArray;
    }

    internal static int ReadInt1(BinaryReader stream, bool signNegative) => signNegative ? -stream.ReadByte() : stream.ReadByte();

    internal static int ReadInt2(BinaryReader stream, bool signNegative) => signNegative ? -stream.ReadUInt16Big() : stream.ReadUInt16Big();

    internal static int ReadCompressedInt32(BinaryReader stream) => DecodeZigZag32(ReadCompressedUInt32(stream));

    private static uint ReadCompressedUInt32(BinaryReader reader)
    {
        uint result = 0;
        int shift = 0;

        while (shift != 35)
        {
            byte b = reader.ReadByte();
            result |= (uint)(b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                break;
        }

        return result;
    }

    internal static long ReadCompressedInt64(BinaryReader stream) => DecodeZigZag64(ReadCompressedUInt64(stream));

    private static ulong ReadCompressedUInt64(BinaryReader reader)
    {
        ulong result = 0;
        int shift = 0;

        while (shift != 70)
        {
            byte b = reader.ReadByte();
            result |= (ulong)(b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                break;
        }

        return result;
    }

    internal static int[] ReadCompressedInt32Array(BinaryReader stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        int[] array = new int[length];
        for (int i = 0; i < length; i++)
            array[i] = ReadCompressedInt32(stream);
        return array;
    }

    internal static long[] ReadCompressedInt64Array(BinaryReader stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        long[] array = new long[length];
        for (int i = 0; i < length; i++)
            array[i] = ReadCompressedInt64(stream);
        return array;
    }

    private static int DecodeZigZag32(uint value) => (int)((value >> 1) ^ -(value & 1U));

    private static long DecodeZigZag64(ulong value) => (long)(value >> 1) ^ -((long)value & 1L);

    internal void Write(BinaryWriter stream, object? value, bool writeType)
    {
        GpType gpType = value == null ? GpType.Null : GetCodeOfType(value.GetType());
        Write(stream, value, gpType, writeType);
    }

    private void Write(BinaryWriter stream, object? value, GpType gpType, bool writeType)
    {
        switch (gpType)
        {
            case GpType.Boolean:
                WriteBoolean(stream, (bool)value!, writeType);
                break;
            case GpType.Byte:
                WriteByte(stream, (byte)value!, writeType);
                break;
            case GpType.Short:
                WriteInt16(stream, (short)value!, writeType);
                break;
            case GpType.Float:
                WriteSingle(stream, (float)value!, writeType);
                break;
            case GpType.Double:
                WriteDouble(stream, (double)value!, writeType);
                break;
            case GpType.String:
                WriteString(stream, (string)value!, writeType);
                break;
            case GpType.Null:
                if (writeType) 
                    stream.Write(8);
                break;
            case GpType.CompressedInt:
                WriteCompressedInt32(stream, (int)value!, writeType);
                break;
            case GpType.CompressedLong:
                WriteCompressedInt64(stream, (long)value!, writeType);
                break;
            case GpType.Custom:
                WriteCustomType(stream, value!, writeType);
                break;
            case GpType.Dictionary:
                WriteDictionary(stream, (IDictionary)value!, writeType);
                break;
            case GpType.Hashtable:
                WriteHashtable(stream, (Hashtable)value!, writeType);
                break;
            case GpType.ObjectArray:
                WriteObjectArray(stream, (IList)value!, writeType);
                break;
            case GpType.OperationRequest:
                SerializeOperationRequest(stream, (OperationRequest)value!, writeType);
                break;
            case GpType.OperationResponse:
                SerializeOperationResponse(stream, (OperationResponse)value!, writeType);
                break;
            case GpType.EventData:
                SerializeEventData(stream, (EventData)value!, writeType);
                break;
            case GpType.Array:
                WriteArrayInArray(stream, value!, writeType);
                break;
            case GpType.BooleanArray:
                WriteBoolArray(stream, (bool[])value!, writeType);
                break;
            case GpType.ByteArray:
                WriteByteArray(stream, (byte[])value!, writeType);
                break;
            case GpType.ShortArray:
                WriteInt16Array(stream, (short[])value!, writeType);
                break;
            case GpType.FloatArray:
                WriteSingleArray(stream, (float[])value!, writeType);
                break;
            case GpType.DoubleArray:
                WriteDoubleArray(stream, (double[])value!, writeType);
                break;
            case GpType.StringArray:
                WriteStringArray(stream, value!, writeType);
                break;
            case GpType.CompressedIntArray:
                WriteInt32ArrayCompressed(stream, (int[])value!, writeType);
                break;
            case GpType.CompressedLongArray:
                WriteInt64ArrayCompressed(stream, (long[])value!, writeType);
                break;
            case GpType.CustomTypeArray:
                WriteCustomTypeArray(stream, value!, writeType);
                break;
            case GpType.DictionaryArray:
                WriteDictionaryArray(stream, (IDictionary[])value!, writeType);
                break;
            case GpType.HashtableArray:
                WriteHashtableArray(stream, value!, writeType);
                break;
            default:
                throw new InvalidDataException($"Unknown GpType: {gpType}");
        }
    }

    public override void SerializeEventData(BinaryWriter stream, EventData serObject, bool setType)
    {
        if (setType) stream.Write((byte)26);
        stream.Write(serObject.Code);
        WriteParameterTable(stream, serObject.Parameters);
    }

    private void WriteParameterTable(BinaryWriter stream, Dictionary<byte, object?>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            WriteByte(stream, 0, false);
        }
        else
        {
            WriteByte(stream, (byte)parameters.Count, false);
            foreach (var parameter in parameters)
            {
                stream.Write(parameter.Key);
                Write(stream, parameter.Value, true);
            }
        }
    }

    private void SerializeOperationRequest(BinaryWriter stream, OperationRequest operation, bool setType)
    {
        SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
    }

    public override void SerializeOperationRequest(BinaryWriter stream, byte operationCode, Dictionary<byte, object?>? parameters, bool setType)
    {
        if (setType) stream.Write((byte)24);
        stream.Write(operationCode);
        WriteParameterTable(stream, parameters);
    }

    public override void SerializeOperationResponse(BinaryWriter stream, OperationResponse serObject, bool setType)
    {
        if (setType) 
            stream.Write((byte)25);
        stream.Write(serObject.OperationCode);
        WriteInt16(stream, serObject.ReturnCode, false);
        if (string.IsNullOrEmpty(serObject.DebugMessage))
        {
            stream.Write((byte)8);
        }
        else
        {
            stream.Write((byte)7);
            WriteString(stream, serObject.DebugMessage, false);
        }
        WriteParameterTable(stream, serObject.Parameters);
    }

    internal static void WriteByte(BinaryWriter stream, byte value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.Write((byte)34);
                return;
            }
            stream.Write((byte)3);
        }
        stream.Write(value);
    }

    internal static void WriteBoolean(BinaryWriter stream, bool value, bool writeType)
    {
        if (writeType)
        {
            stream.Write(value ? (byte)28 : (byte)27);
        }
        else
        {
            stream.Write(value ? (byte)1 : (byte)0);
        }
    }

    internal static void WriteUShort(BinaryWriter stream, ushort value)
    {
        stream.WriteUInt16Big(value);
    }

    internal static void WriteInt16(BinaryWriter stream, short value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.Write((byte)29);
                return;
            }
            stream.Write((byte)4);
        }
        stream.WriteInt16Big(value);
    }

    internal static void WriteDouble(BinaryWriter stream, double value, bool writeType)
    {
        if (writeType) stream.Write((byte)6);
        stream.WriteDoubleBig(value);
    }

    internal static void WriteSingle(BinaryWriter stream, float value, bool writeType)
    {
        if (writeType) stream.Write((byte)5);
        stream.WriteSingleBig(value);
    }

    internal static void WriteString(BinaryWriter stream, string value, bool writeType)
    {
        if (writeType) stream.Write((byte)7);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > short.MaxValue)
            throw new NotSupportedException($"Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: {byteCount}");
        WriteIntLength(stream, byteCount);
        stream.Write(Encoding.UTF8.GetBytes(value));
    }

    public void WriteHashtable(BinaryWriter stream, object value, bool writeType)
    {
        Hashtable hashtable = (Hashtable)value;
        if (writeType) stream.Write((byte)21);
        WriteIntLength(stream, hashtable.Count);
        foreach (DictionaryEntry entry in hashtable)
        {
            Write(stream, entry.Key, true);
            Write(stream, entry.Value, true);
        }
    }

    internal static void WriteByteArray(BinaryWriter stream, byte[] value, bool writeType)
    {
        if (writeType) stream.Write((byte)67);
        WriteIntLength(stream, value.Length);
        stream.Write(value, 0, value.Length);
    }

    private static void WriteArraySegmentByte(BinaryWriter stream, ArraySegment<byte> seg, bool writeType)
    {
        if (writeType) stream.Write((byte)67);
        WriteIntLength(stream, seg.Count);
        if (seg.Count > 0)
            stream.Write(seg.Array!, seg.Offset, seg.Count);
    }

    private static void WriteByteArraySlice(BinaryWriter stream, ByteArraySlice slice, bool writeType)
    {
        if (writeType) stream.Write((byte)67);
        WriteIntLength(stream, slice.Count);
        stream.Write(slice.Buffer!, slice.Offset, slice.Count);
        slice.Release();
    }


    internal static void WriteInt32ArrayCompressed(BinaryWriter stream, int[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)73);
        WriteIntLength(stream, array.Length);
        foreach (int value in array)
            WriteCompressedInt32(stream, value, false);
    }

    private static void WriteInt64ArrayCompressed(BinaryWriter stream, long[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)74);
        WriteIntLength(stream, array.Length);
        foreach (long value in array)
            WriteCompressedInt64(stream, value, false);
    }

    internal static void WriteBoolArray(BinaryWriter stream, bool[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)66);
        WriteIntLength(stream, array.Length);
        int byteCount = array.Length >> 3;
        byte[] buffer = new byte[byteCount + 1];
        int bufferIndex = 0;
        int arrayIndex = 0;

        while (byteCount > 0)
        {
            byte b = 0;
            for (int i = 0; i < 8; i++)
            {
                if (array[arrayIndex++])
                    b |= (byte)(1 << i);
            }
            buffer[bufferIndex++] = b;
            byteCount--;
        }

        if (arrayIndex < array.Length)
        {
            byte b = 0;
            for (int i = 0; arrayIndex < array.Length; i++, arrayIndex++)
            {
                if (array[arrayIndex])
                    b |= (byte)(1 << i);
            }
            buffer[bufferIndex++] = b;
        }

        stream.Write(buffer, 0, bufferIndex);
    }

    internal static void WriteInt16Array(BinaryWriter stream, short[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)68);
        WriteIntLength(stream, array.Length);
        foreach (short value in array)
            WriteInt16(stream, value, false);
    }

    internal static void WriteSingleArray(BinaryWriter stream, float[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)69);
        WriteIntLength(stream, array.Length);
        foreach (float value in array)
        {
            WriteSingle(stream, value, false);
        }
    }

    internal static void WriteDoubleArray(BinaryWriter stream, double[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)70);
        WriteIntLength(stream, array.Length);
        foreach (float value in array)
        {
            WriteDouble(stream, value, false);
        }
    }

    internal static void WriteStringArray(BinaryWriter stream, object value, bool writeType)
    {
        string[] array = (string[])value;
        if (writeType) stream.Write((byte)71);
        WriteIntLength(stream, array.Length);
        foreach (string s in array)
        {
            if (s == null)
                throw new InvalidDataException("Unexpected - cannot serialize string array with null element");
            WriteString(stream, s, false);
        }
    }

    private void WriteObjectArray(BinaryWriter stream, object array, bool writeType)
    {
        WriteObjectArray(stream, (IList)array, writeType);
    }

    private void WriteObjectArray(BinaryWriter stream, IList array, bool writeType)
    {
        if (writeType) stream.Write((byte)23);
        WriteIntLength(stream, array.Count);
        foreach (object value in array)
            Write(stream, value, true);
    }

    private void WriteArrayInArray(BinaryWriter stream, object value, bool writeType)
    {
        object[] array = (object[])value;
        if (writeType) stream.Write((byte)64);
        WriteIntLength(stream, array.Length);
        foreach (object item in array)
            Write(stream, item, true);
    }

    private static void WriteCustomTypeBody(CustomType customType, BinaryWriter stream, object value)
    {
        
        if (customType.SerializeFunction != null)
        {
            byte[] buffer = customType.SerializeFunction(value);
            WriteIntLength(stream, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
        else if (customType.SerializeStreamFunction != null)
        {
            long startPosition = stream.BaseStream.Position;
            stream.BaseStream.Position += 1; // Reserve space for length
            long lengthPosition = stream.BaseStream.Position;

            customType.SerializeStreamFunction(stream, value);

            long endPosition = stream.BaseStream.Position;
            long writtenBytes = endPosition - lengthPosition;

            stream.BaseStream.Position = startPosition;
            WriteCompressedUInt32(stream, (uint)writtenBytes);
            stream.BaseStream.Position = endPosition;
        }
    }

    private static void WriteCustomType(BinaryWriter stream, object value, bool writeType)
    {
        Type key = value is StructWrapper structWrapper ? structWrapper.ttype : value.GetType();
        if (!Protocol.TypeDict.TryGetValue(key, out CustomType? customType))
            throw new Exception($"Write failed. Custom type not found: {key}");

        if (writeType)
        {
            if (customType.Code < 100)
            {
                stream.Write((byte)(128 + customType.Code));
            }
            else
            {
                stream.Write((byte)19);
                stream.Write(customType.Code);
            }
        }
        else
        {
            stream.Write(customType.Code);
        }

        WriteCustomTypeBody(customType, stream, value);
    }

    private static void WriteCustomTypeArray(BinaryWriter stream, object value, bool writeType)
    {
        IList list = (IList)value;
        Type? elementType = value.GetType().GetElementType();
        if (elementType == null || !Protocol.TypeDict.TryGetValue(elementType, out CustomType? customType))
            throw new Exception($"Write failed. Custom type of element not found: {elementType}");

        if (writeType) stream.Write((byte)83);
        WriteIntLength(stream, list.Count);
        stream.Write(customType.Code);
        foreach (object item in list)
            WriteCustomTypeBody(customType, stream, item);
    }

    private static bool WriteArrayHeader(BinaryWriter stream, Type type)
    {
        Type? elementType = type.GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        while (elementType.IsArray)
        {
            stream.Write((byte)64);
            elementType = elementType.GetElementType();
            ArgumentNullException.ThrowIfNull(elementType);
        }

        GpType codeOfType = GetCodeOfType(elementType);
        if (codeOfType == GpType.Unknown)
            return false;

        stream.Write((byte)(codeOfType | GpType.CustomTypeSlim));
        return true;
    }

    private void WriteDictionaryElements(BinaryWriter stream, IDictionary dictionary, GpType keyWriteType, GpType valueWriteType)
    {
        WriteIntLength(stream, dictionary.Count);
        foreach (DictionaryEntry entry in dictionary)
        {
            Write(stream, entry.Key, keyWriteType == GpType.Unknown);
            Write(stream, entry.Value, valueWriteType == GpType.Unknown);
        }
    }

    private void WriteDictionary(BinaryWriter stream, object dict, bool setType)
    {
        if (setType) stream.Write((byte)20);
        WriteDictionaryHeader(stream, dict.GetType(), out GpType keyWriteType, out GpType valueWriteType);
        WriteDictionaryElements(stream, (IDictionary)dict, keyWriteType, valueWriteType);
    }


    private static void WriteDictionaryHeader(
      BinaryWriter stream,
      Type type,
      out GpType keyWriteType,
      out GpType valueWriteType)
    {
        Type[] genericArguments = type.GetGenericArguments();
        if (genericArguments[0] == typeof(object))
        {
            stream.Write((byte)0);
            keyWriteType = GpType.Unknown;
        }
        else
        {
            keyWriteType = genericArguments[0].IsPrimitive || !(genericArguments[0] != typeof(string)) ? GetCodeOfType(genericArguments[0]) : throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            if (keyWriteType == GpType.Unknown)
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            stream.Write((byte)keyWriteType);
        }
        if (genericArguments[1] == typeof(object))
        {
            stream.Write((byte)0);
            valueWriteType = GpType.Unknown;
        }
        else if (genericArguments[1].IsArray)
        {
            if (!WriteArrayType(stream, genericArguments[1], out valueWriteType))
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
        }
        else
        {
            valueWriteType = GetCodeOfType(genericArguments[1]);
            if (valueWriteType == GpType.Unknown)
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            if (valueWriteType == GpType.Array)
            {
                if (!WriteArrayHeader(stream, genericArguments[1]))
                    throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            }
            else if (valueWriteType == GpType.Dictionary)
            {
                stream.Write((byte)valueWriteType);
                WriteDictionaryHeader(stream, genericArguments[1], out GpType _, out GpType _);
            }
            else
                stream.Write((byte)valueWriteType);
        }
    }

    private static bool WriteArrayType(BinaryWriter stream, Type type, out GpType writeType)
    {
        Type? elementType = type.GetElementType(); 
        ArgumentNullException.ThrowIfNull(elementType);

        if (elementType.IsArray)
        {
            while (elementType.IsArray)
            {
                stream.Write((byte)64);
                elementType = elementType.GetElementType();
                ArgumentNullException.ThrowIfNull(elementType);
            }
            stream.Write((byte)(GetCodeOfType(elementType) | GpType.Array));
            writeType = GpType.Array;
            return true;
        }

        GpType gpType = GetCodeOfType(elementType) | GpType.Array;
        if (gpType == GpType.ByteArray)
            gpType = GpType.ByteArray;
        stream.Write((byte)gpType);
        writeType = Enum.IsDefined(gpType) ? gpType : GpType.Unknown;
        return writeType != GpType.Unknown;
    }

    private void WriteHashtableArray(BinaryWriter stream, object value, bool writeType)
    {
        Hashtable[] array = (Hashtable[])value;
        if (writeType) stream.Write((byte)85);
        WriteIntLength(stream, array.Length);
        foreach (Hashtable hashtable in array)
            WriteHashtable(stream, hashtable, false);
    }


    private void WriteDictionaryArray(BinaryWriter stream, IDictionary[] array, bool writeType)
    {
        if (writeType) stream.Write((byte)84);
        WriteDictionaryHeader(stream, array.GetType().GetElementType()!, out GpType keyWriteType, out GpType valueWriteType);
        WriteIntLength(stream, array.Length);
        foreach (IDictionary dict in array)
            WriteDictionaryElements(stream, dict, keyWriteType, valueWriteType);
    }

    private static void WriteIntLength(BinaryWriter stream, int value) => WriteCompressedUInt32(stream, (uint)value);

    private static void WriteCompressedInt32(BinaryWriter stream, int value, bool writeType)
    {
        
        if (writeType)
        {
            if (value == 0)
            {
                stream.Write((byte)30);
                return;
            }
            if (value > 0)
            {
                if (value <= byte.MaxValue)
                {
                    stream.Write((byte)11);
                    stream.Write((byte)value);
                    return;
                }
                if (value <= ushort.MaxValue)
                {
                    stream.Write((byte)13);
                    WriteUShort(stream, (ushort)value);
                    return;
                }
            }
            else if (value >= -65535)
            {
                if (value >= -255)
                {
                    stream.Write((byte)12);
                    stream.Write((byte)-value);
                    return;
                }
                if (value >= -65535)
                {
                    stream.Write((byte)14);
                    WriteUShort(stream, (ushort)-((ushort)value));
                    return;
                }
            }
        }
        if (writeType) stream.Write((byte)9);
        WriteCompressedUInt32(stream, EncodeZigZag32(value));
    }

    private static void WriteCompressedInt64(BinaryWriter stream, long value, bool writeType)
    {
        
        if (writeType)
        {
            if (value == 0)
            {
                stream.Write((byte)31);
                return;
            }
            if (value > 0)
            {
                if (value <= byte.MaxValue)
                {
                    stream.Write((byte)15);
                    stream.Write((byte)value);
                    return;
                }
                if (value <= ushort.MaxValue)
                {
                    stream.Write((byte)17);
                    WriteUShort(stream, (ushort)value);
                    return;
                }
            }
            else if (value >= -65535)
            {
                if (value >= -255)
                {
                    stream.Write((byte)16);
                    stream.Write((byte)-value);
                    return;
                }
                if (value >= -65535)
                {
                    stream.Write((byte)18);
                    WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType) stream.Write((byte)10);
        WriteCompressedUInt64(stream, EncodeZigZag64(value));
    }

    private static void WriteCompressedUInt32(BinaryWriter stream, uint value)
    {
        while (value > 0x7F)
        {
            stream.Write((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        stream.Write((byte)value);
    }

    private static void WriteCompressedUInt64(BinaryWriter stream, ulong value)
    {
        while (value > 0x7F)
        {
            stream.Write((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        stream.Write((byte)value);
    }

    private static uint EncodeZigZag32(int value) => (uint)(value << 1 ^ value >> 31);

    private static ulong EncodeZigZag64(long value) => (ulong)(value << 1 ^ value >> 63);

    public enum GpType : byte
    {
        Unknown = 0,
        Boolean = 2,
        Byte = 3,
        Short = 4,
        Float = 5,
        Double = 6,
        String = 7,
        Null = 8,
        CompressedInt = 9,
        CompressedLong = 10,
        Int1 = 11,
        Int1_ = 12,
        Int2 = 13,
        Int2_ = 14,
        L1 = 15,
        L1_ = 16,
        L2 = 17,
        L2_ = 18,
        Custom = 19,
        Dictionary = 20,
        Hashtable = 21,
        ObjectArray = 23,
        OperationRequest = 24,
        OperationResponse = 25,
        EventData = 26,
        BooleanFalse = 27,
        BooleanTrue = 28,
        ShortZero = 29,
        IntZero = 30,
        LongZero = 31,
        FloatZero = 32,
        DoubleZero = 33,
        ByteZero = 34,
        Array = 64,
        BooleanArray = 66,
        ByteArray = 67,
        ShortArray = 68,
        FloatArray = 69,
        DoubleArray = 70,
        StringArray = 71,
        CompressedIntArray = 73,
        CompressedLongArray = 74,
        CustomTypeArray = 83,
        DictionaryArray = 84,
        HashtableArray = 85,
        CustomTypeSlim = 128,
    }
}
