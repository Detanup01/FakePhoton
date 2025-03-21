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
    private static readonly byte[] boolMasks = [1, 2, 4, 8, 16, 32, 64, 128];
    private static readonly double[] memDoubleBlock = new double[1];
    private static readonly float[] memFloatBlock = new float[1];
    private static readonly byte[] memCustomTypeBodyLengthSerialized = new byte[5];
    private static readonly byte[] memCompressedUInt32 = new byte[5];
    private static readonly byte[] memCompressedUInt64 = new byte[10];
    public override string ProtocolType => "GpBinaryV18";
    public override byte[] VersionBytes => versionBytes;

    public override void Serialize(StreamBuffer dout, object serObject, bool setType)
    {
        Write(dout, serObject, setType);
    }

    public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
    {
        WriteInt16(dout, serObject, setType);
    }

    public override void SerializeString(StreamBuffer dout, string serObject, bool setType)
    {
        WriteString(dout, serObject, setType);
    }

    public override object? Deserialize(StreamBuffer din, byte type, DeserializationFlags flags = DeserializationFlags.None)
    {
        return Read(din, type);
    }

    public override short DeserializeShort(StreamBuffer din) => ReadInt16(din);

    public override byte DeserializeByte(StreamBuffer din) => ReadByte(din);

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

    private object? Read(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters) =>
        Read(stream, ReadByte(stream), flags, parameters);

    private object? Read(StreamBuffer stream, byte gpType, DeserializationFlags flags = DeserializationFlags.None, Dictionary<byte, object?>? parameters = null)
    {
        if (gpType >= 128 && gpType <= 228)
            return ReadCustomType(stream, gpType);

        return gpType switch
        {
            2 => ReadBoolean(stream),
            3 => ReadByte(stream),
            4 => ReadInt16(stream),
            5 => ReadSingle(stream),
            6 => ReadDouble(stream),
            7 => (object)ReadString(stream),
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
            24 => DeserializeOperationRequest(stream, IProtocol.DeserializationFlags.None),
            25 => DeserializeOperationResponse(stream, flags),
            26 => DeserializeEventData(stream, null, IProtocol.DeserializationFlags.None),
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
            _ => throw new InvalidDataException($"GpTypeCode not found: {gpType}(0x{gpType:X}). Is not a CustomType either. Pos: {stream.Position} Available: {stream.Available}")
        };
    }

    internal static bool ReadBoolean(StreamBuffer stream) => stream.ReadByte() > 0;

    internal static byte ReadByte(StreamBuffer stream) => stream.ReadByte();

    internal static short ReadInt16(StreamBuffer stream)
    {
        byte[] buffer = stream.GetBufferAndAdvance(2, out int offset);
        return (short)(buffer[offset] | buffer[offset + 1] << 8);
    }

    internal static ushort ReadUShort(StreamBuffer stream)
    {
        byte[] buffer = stream.GetBufferAndAdvance(2, out int offset);
        return (ushort)(buffer[offset] | buffer[offset + 1] << 8);
    }

    internal static int ReadInt32(StreamBuffer stream)
    {
        byte[] buffer = stream.GetBufferAndAdvance(4, out int offset);
        return buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3];
    }

    internal static long ReadInt64(StreamBuffer stream)
    {
        byte[] buffer = stream.GetBufferAndAdvance(8, out int offset);
        return (long)buffer[offset] << 56 | (long)buffer[offset + 1] << 48 | (long)buffer[offset + 2] << 40 | (long)buffer[offset + 3] << 32 |
               (long)buffer[offset + 4] << 24 | (long)buffer[offset + 5] << 16 | (long)buffer[offset + 6] << 8 | buffer[offset + 7];
    }

    internal static float ReadSingle(StreamBuffer stream)
    {
        return BitConverter.ToSingle(stream.GetBufferAndAdvance(4, out int offset), offset);
    }

    internal static double ReadDouble(StreamBuffer stream)
    {
        return BitConverter.ToDouble(stream.GetBufferAndAdvance(8, out int offset), offset);
    }

    internal ByteArraySlice ReadNonAllocByteArray(StreamBuffer stream)
    {
        uint length = ReadCompressedUInt32(stream);
        ByteArraySlice byteArraySlice = ByteArraySlicePool.Acquire((int)length);
        stream.Read(byteArraySlice.Buffer!, 0, (int)length);
        byteArraySlice.Count = (int)length;
        return byteArraySlice;
    }

    internal static byte[] ReadByteArray(StreamBuffer stream)
    {
        uint length = ReadCompressedUInt32(stream);
        byte[] buffer = new byte[length];
        stream.Read(buffer, 0, (int)length);
        return buffer;
    }

    public static object ReadCustomType(StreamBuffer stream, byte gpType = 0)
    {
        byte typeCode = gpType != 0 ? (byte)(gpType - 128) : stream.ReadByte();
        int length = (int)ReadCompressedUInt32(stream);
        if (length < 0)
            throw new InvalidDataException($"ReadCustomType read negative size value: {length} before position: {stream.Position}");

        if (length > stream.Available || length > short.MaxValue || !Protocol.CodeDict.TryGetValue(typeCode, out CustomType? customType))
        {
            UnknownType unknownType = new() { TypeCode = typeCode, Size = length };
            int count = length > stream.Available ? stream.Available : length;
            if (count > 0)
            {
                byte[] buffer = new byte[count];
                stream.Read(buffer, 0, count);
                unknownType.Data = buffer;
            }
            return unknownType;
        }

        if (customType.DeserializeFunction != null)
        {
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return customType.DeserializeFunction(buffer);
        }

        int startPosition = stream.Position;
        object result = customType.DeserializeStreamFunction!(stream, (short)length);
        if (stream.Position - startPosition != length)
            stream.Position = startPosition + length;

        return result;
    }


    public override EventData DeserializeEventData(StreamBuffer din, EventData? target = null, DeserializationFlags flags = DeserializationFlags.None)
    {
        EventData eventData = target ?? new EventData();
        eventData.Code = ReadByte(din);
        short parameterCount = (short)ReadByte(din);
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

    private Dictionary<byte, object?> ReadParameters(StreamBuffer stream, Dictionary<byte, object?>? target = null, DeserializationFlags flags = DeserializationFlags.None)
    {
        short capacity = (short)ReadByte(stream);
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

    public Hashtable ReadHashtable(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
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

    public static int[] ReadIntArray(StreamBuffer stream)
    {
        int length = ReadInt32(stream);
        int[] array = new int[length];

        for (uint i = 0; i < length; i++)
            array[i] = ReadInt32(stream);

        return array;
    }

    public override OperationRequest DeserializeOperationRequest(StreamBuffer din, DeserializationFlags flags = DeserializationFlags.None)
    {
        return new()
        {
            OperationCode = ReadByte(din),
            Parameters = ReadParameters(din, null, flags)
        };
    }

    public override OperationResponse DeserializeOperationResponse(StreamBuffer stream, DeserializationFlags flags = DeserializationFlags.None)
    {
        return new()
        {
            OperationCode = ReadByte(stream),
            ReturnCode = ReadInt16(stream),
            DebugMessage = Read(stream, ReadByte(stream), flags, null) as string,
            Parameters = ReadParameters(stream, null, flags)
        };
    }

    public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
    {
        return new()
        {
            Code = ReadInt16(stream),
            DebugMessage = Read(stream, ReadByte(stream)) as string,
            Parameters = ReadParameters(stream)
        };
    }

    internal static string ReadString(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        if (length == 0)
            return string.Empty;
        return Encoding.UTF8.GetString(stream.GetBufferAndAdvance(length, out int offset), offset, length);
    }

    private static object ReadCustomTypeArray(StreamBuffer stream)
    {
        uint length = ReadCompressedUInt32(stream);
        byte typeCode = stream.ReadByte();
        if (!Protocol.CodeDict.TryGetValue(typeCode, out CustomType? customType))
        {
            int startPosition = stream.Position;
            for (uint i = 0; i < length; i++)
            {
                int size = (int)ReadCompressedUInt32(stream);
                int available = stream.Available;
                int readSize = Math.Min(size, available);
                stream.Position += readSize;
            }
            return new[] { new UnknownType { TypeCode = typeCode, Size = stream.Position - startPosition } };
        }

        Array array = Array.CreateInstance(customType.Type, (int)length);
        for (uint i = 0; i < length; i++)
        {
            int size = (int)ReadCompressedUInt32(stream);
            if (size < 0)
                throw new InvalidDataException($"ReadCustomTypeArray read negative size value: {size} before position: {stream.Position}");
            if (size > stream.Available || size > short.MaxValue)
            {
                stream.Position = stream.Length;
                throw new InvalidDataException($"ReadCustomTypeArray read size value: {size} larger than short.MaxValue or available data: {stream.Available}");
            }

            object value;
            if (customType.DeserializeFunction != null)
            {
                byte[] buffer = new byte[size];
                stream.Read(buffer, 0, size);
                value = customType.DeserializeFunction(buffer);
            }
            else
            {
                int startPosition = stream.Position;
                value = customType.DeserializeStreamFunction!(stream, (short)size);
                if (stream.Position - startPosition != size)
                    stream.Position = startPosition + size;
            }

            if (value != null && customType.Type.IsAssignableFrom(value.GetType()))
                array.SetValue(value, (int)i);
        }

        return array;
    }


    private static Type ReadDictionaryType(StreamBuffer stream, out GpType keyReadType, out GpType valueReadType)
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

    private static Type ReadDictionaryType(StreamBuffer stream)
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

    private static Type GetDictArrayType(StreamBuffer stream)
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

    private IDictionary? ReadDictionary(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        Type dictionaryType = ReadDictionaryType(stream, out GpType keyReadType, out GpType valueReadType);
        if (dictionaryType == null || Activator.CreateInstance(dictionaryType) is not IDictionary dictionary)
            return null;

        ReadDictionaryElements(stream, keyReadType, valueReadType, dictionary, flags, parameters);
        return dictionary;
    }

    private bool ReadDictionaryElements(StreamBuffer stream, GpType keyReadType, GpType valueReadType, IDictionary? dictionary, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
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

    private object[] ReadObjectArray(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        object[] array = new object[length];
        for (uint i = 0; i < length; i++)
            array[i] = Read(stream, flags, parameters)!;

        return array;
    }

    private StructWrapper[] ReadWrapperArray(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
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

    private static bool[] ReadBooleanArray(StreamBuffer stream)
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

    internal static short[] ReadInt16Array(StreamBuffer stream)
    {
        short[] array = new short[ReadCompressedUInt32(stream)];
        for (int i = 0; i < array.Length; i++)
            array[i] = ReadInt16(stream);

        return array;
    }

    private static float[] ReadSingleArray(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        float[] array = new float[length];
        Buffer.BlockCopy(stream.GetBufferAndAdvance(length * 4, out int offset), offset, array, 0, length * 4);
        return array;
    }

    private static double[] ReadDoubleArray(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        double[] array = new double[length];
        Buffer.BlockCopy(stream.GetBufferAndAdvance(length * 8, out int offset), offset, array, 0, length * 8);
        return array;
    }

    internal static string[] ReadStringArray(StreamBuffer stream)
    {
        string[] array = new string[ReadCompressedUInt32(stream)];
        for (int i = 0; i < array.Length; i++)
            array[i] = ReadString(stream);

        return array;
    }

    private Hashtable[] ReadHashtableArray(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
    {
        Hashtable[] array = new Hashtable[ReadCompressedUInt32(stream)];
        for (int i = 0; i < array.Length; i++)
            array[i] = ReadHashtable(stream, flags, parameters);

        return array;
    }

    private IDictionary?[] ReadDictionaryArray(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
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

    private Array? ReadArrayInArray(StreamBuffer stream, DeserializationFlags flags, Dictionary<byte, object?>? parameters)
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

    internal static int ReadInt1(StreamBuffer stream, bool signNegative) => signNegative ? -stream.ReadByte() : stream.ReadByte();

    internal static int ReadInt2(StreamBuffer stream, bool signNegative) => signNegative ? -ReadUShort(stream) : ReadUShort(stream);

    internal static int ReadCompressedInt32(StreamBuffer stream) => DecodeZigZag32(ReadCompressedUInt32(stream));

    private static uint ReadCompressedUInt32(StreamBuffer stream)
    {
        uint value = 0;
        int shift = 0;
        byte[] buffer = stream.GetBuffer();
        int position = stream.Position;

        while (shift != 35)
        {
            if (position >= stream.Length)
            {
                stream.Position = stream.Length;
                throw new EndOfStreamException($"Failed to read full uint. offset: {position} stream.Length: {stream.Length} data.Length: {buffer.Length} stream.Available: {stream.Available}");
            }

            byte b = buffer[position++];
            value |= (uint)(b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                break;
        }

        stream.Position = position;
        return value;
    }

    internal static long ReadCompressedInt64(StreamBuffer stream) => DecodeZigZag64(ReadCompressedUInt64(stream));

    private static ulong ReadCompressedUInt64(StreamBuffer stream)
    {
        ulong value = 0;
        int shift = 0;
        byte[] buffer = stream.GetBuffer();
        int position = stream.Position;

        while (shift != 70)
        {
            if (position >= buffer.Length)
                throw new EndOfStreamException("Failed to read full ulong.");

            byte b = buffer[position++];
            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                break;
        }

        stream.Position = position;
        return value;
    }

    internal static int[] ReadCompressedInt32Array(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        int[] array = new int[length];
        for (int i = 0; i < length; i++)
            array[i] = ReadCompressedInt32(stream);
        return array;
    }

    internal static long[] ReadCompressedInt64Array(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        long[] array = new long[length];
        for (int i = 0; i < length; i++)
            array[i] = ReadCompressedInt64(stream);
        return array;
    }

    private static int DecodeZigZag32(uint value) => (int)((value >> 1) ^ -(value & 1U));

    private static long DecodeZigZag64(ulong value) => (long)(value >> 1) ^ -((long)value & 1L);

    internal void Write(StreamBuffer stream, object? value, bool writeType)
    {
        GpType gpType = value == null ? GpType.Null : GetCodeOfType(value.GetType());
        Write(stream, value, gpType, writeType);
    }

    private void Write(StreamBuffer stream, object? value, GpType gpType, bool writeType)
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
                    stream.WriteByte(8);
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

    public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
    {
        if (setType) stream.WriteByte(26);
        stream.WriteByte(serObject.Code);
        WriteParameterTable(stream, serObject.Parameters);
    }

    private void WriteParameterTable(StreamBuffer stream, Dictionary<byte, object?>? parameters)
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
                stream.WriteByte(parameter.Key);
                Write(stream, parameter.Value, true);
            }
        }
    }

    private void SerializeOperationRequest(StreamBuffer stream, OperationRequest operation, bool setType)
    {
        SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
    }

    public override void SerializeOperationRequest(StreamBuffer stream, byte operationCode, Dictionary<byte, object?>? parameters, bool setType)
    {
        if (setType) stream.WriteByte(24);
        stream.WriteByte(operationCode);
        WriteParameterTable(stream, parameters);
    }

    public override void SerializeOperationResponse(StreamBuffer stream, OperationResponse serObject, bool setType)
    {
        if (setType) stream.WriteByte(25);
        stream.WriteByte(serObject.OperationCode);
        WriteInt16(stream, serObject.ReturnCode, false);
        if (string.IsNullOrEmpty(serObject.DebugMessage))
        {
            stream.WriteByte(8);
        }
        else
        {
            stream.WriteByte(7);
            WriteString(stream, serObject.DebugMessage, false);
        }
        WriteParameterTable(stream, serObject.Parameters);
    }

    internal static void WriteByte(StreamBuffer stream, byte value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.WriteByte(34);
                return;
            }
            stream.WriteByte(3);
        }
        stream.WriteByte(value);
    }

    internal static void WriteBoolean(StreamBuffer stream, bool value, bool writeType)
    {
        if (writeType)
        {
            stream.WriteByte(value ? (byte)28 : (byte)27);
        }
        else
        {
            stream.WriteByte(value ? (byte)1 : (byte)0);
        }
    }

    internal static void WriteUShort(StreamBuffer stream, ushort value)
    {
        stream.WriteBytes((byte)value, (byte)(value >> 8));
    }

    internal static void WriteInt16(StreamBuffer stream, short value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.WriteByte(29);
                return;
            }
            stream.WriteByte(4);
        }
        stream.WriteBytes((byte)value, (byte)(value >> 8));
    }

    internal static void WriteDouble(StreamBuffer stream, double value, bool writeType)
    {
        if (writeType) stream.WriteByte(6);
        byte[] buffer = stream.GetBufferAndAdvance(8, out int offset);
        lock (memDoubleBlock)
        {
            memDoubleBlock[0] = value;
            Buffer.BlockCopy(memDoubleBlock, 0, buffer, offset, 8);
        }
    }

    internal static void WriteSingle(StreamBuffer stream, float value, bool writeType)
    {
        if (writeType) stream.WriteByte(5);
        byte[] buffer = stream.GetBufferAndAdvance(4, out int offset);
        lock (memFloatBlock)
        {
            memFloatBlock[0] = value;
            Buffer.BlockCopy(memFloatBlock, 0, buffer, offset, 4);
        }
    }

    internal static void WriteString(StreamBuffer stream, string value, bool writeType)
    {
        if (writeType) stream.WriteByte(7);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > short.MaxValue)
            throw new NotSupportedException($"Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: {byteCount}");
        WriteIntLength(stream, byteCount);
        byte[] buffer = stream.GetBufferAndAdvance(byteCount, out int offset);
        Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
    }

    private void WriteHashtable(StreamBuffer stream, object value, bool writeType)
    {
        Hashtable hashtable = (Hashtable)value;
        if (writeType) stream.WriteByte(21);
        WriteIntLength(stream, hashtable.Count);
        foreach (DictionaryEntry entry in hashtable)
        {
            Write(stream, entry.Key, true);
            Write(stream, entry.Value, true);
        }
    }

    internal static void WriteByteArray(StreamBuffer stream, byte[] value, bool writeType)
    {
        if (writeType) stream.WriteByte(67);
        WriteIntLength(stream, value.Length);
        stream.Write(value, 0, value.Length);
    }

    private static void WriteArraySegmentByte(StreamBuffer stream, ArraySegment<byte> seg, bool writeType)
    {
        if (writeType) stream.WriteByte(67);
        WriteIntLength(stream, seg.Count);
        if (seg.Count > 0)
            stream.Write(seg.Array!, seg.Offset, seg.Count);
    }

    private static void WriteByteArraySlice(StreamBuffer stream, ByteArraySlice slice, bool writeType)
    {
        if (writeType) stream.WriteByte(67);
        WriteIntLength(stream, slice.Count);
        stream.Write(slice.Buffer!, slice.Offset, slice.Count);
        slice.Release();
    }


    internal static void WriteInt32ArrayCompressed(StreamBuffer stream, int[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(73);
        WriteIntLength(stream, array.Length);
        foreach (int value in array)
            WriteCompressedInt32(stream, value, false);
    }

    private static void WriteInt64ArrayCompressed(StreamBuffer stream, long[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(74);
        WriteIntLength(stream, array.Length);
        foreach (long value in array)
            WriteCompressedInt64(stream, value, false);
    }

    internal static void WriteBoolArray(StreamBuffer stream, bool[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(66);
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

    internal static void WriteInt16Array(StreamBuffer stream, short[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(68);
        WriteIntLength(stream, array.Length);
        foreach (short value in array)
            WriteInt16(stream, value, false);
    }

    internal static void WriteSingleArray(StreamBuffer stream, float[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(69);
        WriteIntLength(stream, array.Length);
        int byteCount = array.Length * 4;
        byte[] buffer = stream.GetBufferAndAdvance(byteCount, out int offset);
        Buffer.BlockCopy(array, 0, buffer, offset, byteCount);
    }

    internal static void WriteDoubleArray(StreamBuffer stream, double[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(70);
        WriteIntLength(stream, array.Length);
        int byteCount = array.Length * 8;
        byte[] buffer = stream.GetBufferAndAdvance(byteCount, out int offset);
        Buffer.BlockCopy(array, 0, buffer, offset, byteCount);
    }

    internal static void WriteStringArray(StreamBuffer stream, object value, bool writeType)
    {
        string[] array = (string[])value;
        if (writeType) stream.WriteByte(71);
        WriteIntLength(stream, array.Length);
        foreach (string s in array)
        {
            if (s == null)
                throw new InvalidDataException("Unexpected - cannot serialize string array with null element");
            WriteString(stream, s, false);
        }
    }

    private void WriteObjectArray(StreamBuffer stream, object array, bool writeType)
    {
        WriteObjectArray(stream, (IList)array, writeType);
    }

    private void WriteObjectArray(StreamBuffer stream, IList array, bool writeType)
    {
        if (writeType) stream.WriteByte(23);
        WriteIntLength(stream, array.Count);
        foreach (object value in array)
            Write(stream, value, true);
    }

    private void WriteArrayInArray(StreamBuffer stream, object value, bool writeType)
    {
        object[] array = (object[])value;
        if (writeType) stream.WriteByte(64);
        WriteIntLength(stream, array.Length);
        foreach (object item in array)
            Write(stream, item, true);
    }

    private static void WriteCustomTypeBody(CustomType customType, StreamBuffer stream, object value)
    {
        if (customType.SerializeFunction != null)
        {
            byte[] buffer = customType.SerializeFunction(value);
            WriteIntLength(stream, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
        else if (customType.SerializeStreamFunction != null)
        {
            int startPosition = stream.Position;
            stream.Position++; // Placeholder for length
            uint length = (uint)customType.SerializeStreamFunction(stream, value);
            int writtenBytes = stream.Position - startPosition - 1;
            if (length != writtenBytes)
                Log.Debug($"Serialization for Custom Type '{value.GetType()}' returns size {length} bytes but wrote {writtenBytes} bytes. Sending the latter.");

            int lengthPrefixSize = WriteCompressedUInt32(memCustomTypeBodyLengthSerialized, (uint)writtenBytes);
            if (lengthPrefixSize == 1)
            {
                stream.GetBuffer()[startPosition] = memCustomTypeBodyLengthSerialized[0];
            }
            else
            {
                for (int i = 0; i < lengthPrefixSize - 1; i++)
                    stream.WriteByte(0);
                Buffer.BlockCopy(stream.GetBuffer(), startPosition + 1, stream.GetBuffer(), startPosition + lengthPrefixSize, writtenBytes);
                Buffer.BlockCopy(memCustomTypeBodyLengthSerialized, 0, stream.GetBuffer(), startPosition, lengthPrefixSize);
                stream.Position = startPosition + lengthPrefixSize + writtenBytes;
            }
        }
    }

    private static void WriteCustomType(StreamBuffer stream, object value, bool writeType)
    {
        Type key = value is StructWrapper structWrapper ? structWrapper.ttype : value.GetType();
        if (!Protocol.TypeDict.TryGetValue(key, out CustomType? customType))
            throw new Exception($"Write failed. Custom type not found: {key}");

        if (writeType)
        {
            if (customType.Code < 100)
            {
                stream.WriteByte((byte)(128 + customType.Code));
            }
            else
            {
                stream.WriteByte(19);
                stream.WriteByte(customType.Code);
            }
        }
        else
        {
            stream.WriteByte(customType.Code);
        }

        WriteCustomTypeBody(customType, stream, value);
    }

    private static void WriteCustomTypeArray(StreamBuffer stream, object value, bool writeType)
    {
        IList list = (IList)value;
        Type? elementType = value.GetType().GetElementType();
        if (elementType == null || !Protocol.TypeDict.TryGetValue(elementType, out CustomType? customType))
            throw new Exception($"Write failed. Custom type of element not found: {elementType}");

        if (writeType) stream.WriteByte(83);
        WriteIntLength(stream, list.Count);
        stream.WriteByte(customType.Code);
        foreach (object item in list)
            WriteCustomTypeBody(customType, stream, item);
    }

    private static bool WriteArrayHeader(StreamBuffer stream, Type type)
    {
        Type? elementType = type.GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        while (elementType.IsArray)
        {
            stream.WriteByte(64);
            elementType = elementType.GetElementType();
            ArgumentNullException.ThrowIfNull(elementType);
        }

        GpType codeOfType = GetCodeOfType(elementType);
        if (codeOfType == GpType.Unknown)
            return false;

        stream.WriteByte((byte)(codeOfType | GpType.CustomTypeSlim));
        return true;
    }

    private void WriteDictionaryElements(StreamBuffer stream, IDictionary dictionary, GpType keyWriteType, GpType valueWriteType)
    {
        WriteIntLength(stream, dictionary.Count);
        foreach (DictionaryEntry entry in dictionary)
        {
            Write(stream, entry.Key, keyWriteType == GpType.Unknown);
            Write(stream, entry.Value, valueWriteType == GpType.Unknown);
        }
    }

    private void WriteDictionary(StreamBuffer stream, object dict, bool setType)
    {
        if (setType) stream.WriteByte(20);
        WriteDictionaryHeader(stream, dict.GetType(), out GpType keyWriteType, out GpType valueWriteType);
        WriteDictionaryElements(stream, (IDictionary)dict, keyWriteType, valueWriteType);
    }


    private static void WriteDictionaryHeader(
      StreamBuffer stream,
      Type type,
      out GpType keyWriteType,
      out GpType valueWriteType)
    {
        Type[] genericArguments = type.GetGenericArguments();
        if (genericArguments[0] == typeof(object))
        {
            stream.WriteByte((byte)0);
            keyWriteType = GpType.Unknown;
        }
        else
        {
            keyWriteType = genericArguments[0].IsPrimitive || !(genericArguments[0] != typeof(string)) ? GetCodeOfType(genericArguments[0]) : throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            if (keyWriteType == GpType.Unknown)
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            stream.WriteByte((byte)keyWriteType);
        }
        if (genericArguments[1] == typeof(object))
        {
            stream.WriteByte((byte)0);
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
                stream.WriteByte((byte)valueWriteType);
                WriteDictionaryHeader(stream, genericArguments[1], out GpType _, out GpType _);
            }
            else
                stream.WriteByte((byte)valueWriteType);
        }
    }

    private static bool WriteArrayType(StreamBuffer stream, Type type, out GpType writeType)
    {
        Type? elementType = type.GetElementType(); 
        ArgumentNullException.ThrowIfNull(elementType);

        if (elementType.IsArray)
        {
            while (elementType.IsArray)
            {
                stream.WriteByte(64);
                elementType = elementType.GetElementType();
                ArgumentNullException.ThrowIfNull(elementType);
            }
            stream.WriteByte((byte)(GetCodeOfType(elementType) | GpType.Array));
            writeType = GpType.Array;
            return true;
        }

        GpType gpType = GetCodeOfType(elementType) | GpType.Array;
        if (gpType == GpType.ByteArray)
            gpType = GpType.ByteArray;
        stream.WriteByte((byte)gpType);
        writeType = Enum.IsDefined(gpType) ? gpType : GpType.Unknown;
        return writeType != GpType.Unknown;
    }

    private void WriteHashtableArray(StreamBuffer stream, object value, bool writeType)
    {
        Hashtable[] array = (Hashtable[])value;
        if (writeType) stream.WriteByte(85);
        WriteIntLength(stream, array.Length);
        foreach (Hashtable hashtable in array)
            WriteHashtable(stream, hashtable, false);
    }


    private void WriteDictionaryArray(StreamBuffer stream, IDictionary[] array, bool writeType)
    {
        if (writeType) stream.WriteByte(84);
        WriteDictionaryHeader(stream, array.GetType().GetElementType()!, out GpType keyWriteType, out GpType valueWriteType);
        WriteIntLength(stream, array.Length);
        foreach (IDictionary dict in array)
            WriteDictionaryElements(stream, dict, keyWriteType, valueWriteType);
    }

    private static void WriteIntLength(StreamBuffer stream, int value) => WriteCompressedUInt32(stream, (uint)value);

    private static void WriteCompressedInt32(StreamBuffer stream, int value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.WriteByte(30);
                return;
            }
            if (value > 0)
            {
                if (value <= byte.MaxValue)
                {
                    stream.WriteByte(11);
                    stream.WriteByte((byte)value);
                    return;
                }
                if (value <= ushort.MaxValue)
                {
                    stream.WriteByte(13);
                    WriteUShort(stream, (ushort)value);
                    return;
                }
            }
            else if (value >= -65535)
            {
                if (value >= -255)
                {
                    stream.WriteByte(12);
                    stream.WriteByte((byte)-value);
                    return;
                }
                if (value >= -65535)
                {
                    stream.WriteByte(14);
                    WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType) stream.WriteByte(9);
        WriteCompressedUInt32(stream, EncodeZigZag32(value));
    }

    private static void WriteCompressedInt64(StreamBuffer stream, long value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.WriteByte(31);
                return;
            }
            if (value > 0)
            {
                if (value <= byte.MaxValue)
                {
                    stream.WriteByte(15);
                    stream.WriteByte((byte)value);
                    return;
                }
                if (value <= ushort.MaxValue)
                {
                    stream.WriteByte(17);
                    WriteUShort(stream, (ushort)value);
                    return;
                }
            }
            else if (value >= -65535)
            {
                if (value >= -255)
                {
                    stream.WriteByte(16);
                    stream.WriteByte((byte)-value);
                    return;
                }
                if (value >= -65535)
                {
                    stream.WriteByte(18);
                    WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType) stream.WriteByte(10);
        WriteCompressedUInt64(stream, EncodeZigZag64(value));
    }

    private static void WriteCompressedUInt32(StreamBuffer stream, uint value)
    {
        lock (memCompressedUInt32)
            stream.Write(memCompressedUInt32, 0, WriteCompressedUInt32(memCompressedUInt32, value));
    }

    private static int WriteCompressedUInt32(byte[] buffer, uint value)
    {
        int index = 0;
        buffer[index] = (byte)(value & 0x7F);
        for (value >>= 7; value > 0U; value >>= 7)
        {
            buffer[index] |= 0x80;
            buffer[++index] = (byte)(value & 0x7F);
        }
        return index + 1;
    }

    private static void WriteCompressedUInt64(StreamBuffer stream, ulong value)
    {
        int index = 0;
        lock (memCompressedUInt64)
        {
            memCompressedUInt64[index] = (byte)(value & 0x7F);
            for (value >>= 7; value > 0UL; value >>= 7)
            {
                memCompressedUInt64[index] |= 0x80;
                memCompressedUInt64[++index] = (byte)(value & 0x7F);
            }
            int count = index + 1;
            stream.Write(memCompressedUInt64, 0, count);
        }
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
