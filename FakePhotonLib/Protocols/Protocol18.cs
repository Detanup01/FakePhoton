using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated;
using FakePhotonLib.PhotonRelated.StructWrapping;
using Serilog;
using System.Collections;
using System.Text;

namespace FakePhotonLib.Protocols;

public class Protocol18 : IProtocol
{
    public static readonly StructWrapperPools wrapperPools = new StructWrapperPools();
    private readonly byte[] versionBytes = [1, 8];
    private static readonly byte[] boolMasks = [1, 2, 4, 8, 16, 32, 64, 128];
    private readonly double[] memDoubleBlock = new double[1];
    private readonly float[] memFloatBlock = new float[1];
    private readonly byte[] memCustomTypeBodyLengthSerialized = new byte[5];
    private readonly byte[] memCompressedUInt32 = new byte[5];
    private byte[] memCompressedUInt64 = new byte[10];
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

    public override object Deserialize(StreamBuffer din, byte type, DeserializationFlags flags = DeserializationFlags.None)
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

    private GpType GetCodeOfType(Type type)
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
            if (elementType == null)
                throw new InvalidDataException($"Arrays of type {type} are not supported");

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

    private GpType GetCodeOfTypeCode(TypeCode typeCode) =>
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
        int num1 = gpType >= 128 ? gpType - 128 : gpType;
        int num2 = num1 >= 64 ? num1 - 64 : num1;
        bool wrapIncomingStructs = (flags & DeserializationFlags.WrapIncomingStructs) == DeserializationFlags.WrapIncomingStructs;

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
            20 => (object)ReadDictionary(stream, flags, parameters),
            21 => (object)ReadHashtable(stream, flags, parameters),
            23 => (object)ReadObjectArray(stream, flags, parameters),
            24 => (object)DeserializeOperationRequest(stream, IProtocol.DeserializationFlags.None),
            25 => (object)DeserializeOperationResponse(stream, flags),
            26 => (object)DeserializeEventData(stream, null, IProtocol.DeserializationFlags.None),
            27 => false,
            28 => true,
            29 => (short)0,
            30 => 0,
            31 => 0L,
            32 => 0.0f,
            33 => 0.0,
            34 => (byte)0,
            64 => (object)ReadArrayInArray(stream, flags, parameters),
            66 => (object)ReadBooleanArray(stream),
            67 => (object)ReadByteArray(stream),
            68 => (object)ReadInt16Array(stream),
            69 => (object)ReadSingleArray(stream),
            70 => (object)ReadDoubleArray(stream),
            71 => (object)ReadStringArray(stream),
            73 => (object)ReadCompressedInt32Array(stream),
            74 => (object)ReadCompressedInt64Array(stream),
            83 => ReadCustomTypeArray(stream),
            84 => (object)ReadDictionaryArray(stream, flags, parameters),
            85 => (object)ReadHashtableArray(stream, flags, parameters),
            _ => throw new InvalidDataException($"GpTypeCode not found: {gpType}(0x{gpType:X}). Is not a CustomType either. Pos: {stream.Position} Available: {stream.Available}")
        };
    }

    internal bool ReadBoolean(StreamBuffer stream) => stream.ReadByte() > 0;

    internal byte ReadByte(StreamBuffer stream) => stream.ReadByte();

    internal short ReadInt16(StreamBuffer stream)
    {
        int offset;
        byte[] buffer = stream.GetBufferAndAdvance(2, out offset);
        return (short)(buffer[offset] | buffer[offset + 1] << 8);
    }

    internal ushort ReadUShort(StreamBuffer stream)
    {
        int offset;
        byte[] buffer = stream.GetBufferAndAdvance(2, out offset);
        return (ushort)(buffer[offset] | buffer[offset + 1] << 8);
    }

    internal int ReadInt32(StreamBuffer stream)
    {
        int offset;
        byte[] buffer = stream.GetBufferAndAdvance(4, out offset);
        return buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3];
    }

    internal long ReadInt64(StreamBuffer stream)
    {
        int offset;
        byte[] buffer = stream.GetBufferAndAdvance(8, out offset);
        return (long)buffer[offset] << 56 | (long)buffer[offset + 1] << 48 | (long)buffer[offset + 2] << 40 | (long)buffer[offset + 3] << 32 |
               (long)buffer[offset + 4] << 24 | (long)buffer[offset + 5] << 16 | (long)buffer[offset + 6] << 8 | buffer[offset + 7];
    }

    internal float ReadSingle(StreamBuffer stream)
    {
        int offset;
        return BitConverter.ToSingle(stream.GetBufferAndAdvance(4, out offset), offset);
    }

    internal double ReadDouble(StreamBuffer stream)
    {
        int offset;
        return BitConverter.ToDouble(stream.GetBufferAndAdvance(8, out offset), offset);
    }

    internal ByteArraySlice ReadNonAllocByteArray(StreamBuffer stream)
    {
        uint length = ReadCompressedUInt32(stream);
        ByteArraySlice byteArraySlice = ByteArraySlicePool.Acquire((int)length);
        stream.Read(byteArraySlice.Buffer!, 0, (int)length);
        byteArraySlice.Count = (int)length;
        return byteArraySlice;
    }

    internal byte[] ReadByteArray(StreamBuffer stream)
    {
        uint length = ReadCompressedUInt32(stream);
        byte[] buffer = new byte[length];
        stream.Read(buffer, 0, (int)length);
        return buffer;
    }

    public object ReadCustomType(StreamBuffer stream, byte gpType = 0)
    {
        byte typeCode = gpType != 0 ? (byte)(gpType - 128) : stream.ReadByte();
        int length = (int)ReadCompressedUInt32(stream);
        if (length < 0)
            throw new InvalidDataException($"ReadCustomType read negative size value: {length} before position: {stream.Position}");

        if (length > stream.Available || length > short.MaxValue || !Protocol.CodeDict.TryGetValue(typeCode, out CustomType? customType))
        {
            UnknownType unknownType = new UnknownType { TypeCode = typeCode, Size = length };
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

    private Dictionary<byte, object?> ReadParameters(
      StreamBuffer stream,
      Dictionary<byte, object?>? target = null,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        short capacity = (short)ReadByte(stream);
        Dictionary<byte, object?> parameters = target ?? new Dictionary<byte, object?>(capacity);
        bool allowPooledByteArray = (flags & IProtocol.DeserializationFlags.AllowPooledByteArray) == IProtocol.DeserializationFlags.AllowPooledByteArray;

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

    public Hashtable ReadHashtable(
      StreamBuffer stream,
      DeserializationFlags flags,
      Dictionary<byte, object?>? parameters)
    {
        int count = (int)ReadCompressedUInt32(stream);
        Hashtable hashtable = new Hashtable(count);

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

    public int[] ReadIntArray(StreamBuffer stream)
    {
        int length = ReadInt32(stream);
        int[] numArray = new int[length];
        for (uint index = 0; (long)index < (long)length; ++index)
            numArray[(int)index] = ReadInt32(stream);
        return numArray;
    }

    public override OperationRequest DeserializeOperationRequest(
      StreamBuffer din,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        OperationRequest operationRequest = new OperationRequest()
        {
            OperationCode = ReadByte(din)
        };
        operationRequest.Parameters = ReadParameters(din, operationRequest.Parameters, flags);
        return operationRequest;
    }

    public override OperationResponse DeserializeOperationResponse(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        OperationResponse operationResponse = new OperationResponse()
        {
            OperationCode = ReadByte(stream),
            ReturnCode = ReadInt16(stream)
        };
        operationResponse.DebugMessage = Read(stream, ReadByte(stream), flags, operationResponse.Parameters) as string;
        operationResponse.Parameters = ReadParameters(stream, operationResponse.Parameters, flags);
        return operationResponse;
    }

    public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
    {
        return new DisconnectMessage()
        {
            Code = ReadInt16(stream),
            DebugMessage = Read(stream, ReadByte(stream)) as string,
            Parameters = ReadParameters(stream)
        };
    }

    internal string ReadString(StreamBuffer stream)
    {
        int num = (int)ReadCompressedUInt32(stream);
        if (num == 0)
            return string.Empty;
        int offset = 0;
        return Encoding.UTF8.GetString(stream.GetBufferAndAdvance(num, out offset), offset, num);
    }

    private object ReadCustomTypeArray(StreamBuffer stream)
    {
        uint length1 = ReadCompressedUInt32(stream);
        byte key = stream.ReadByte();
        CustomType customType;
        if (!Protocol.CodeDict.TryGetValue(key, out customType))
        {
            int position = stream.Position;
            for (uint index = 0; index < length1; ++index)
            {
                int num1 = (int)ReadCompressedUInt32(stream);
                int available = stream.Available;
                int num2 = num1 > available ? available : num1;
                stream.Position += num2;
            }
            return (object)new UnknownType[1]
            {
          new UnknownType()
          {
            TypeCode = key,
            Size = stream.Position - position
          }
            };
        }
        Array instance = Array.CreateInstance(customType.Type, (int)length1);
        for (uint index = 0; index < length1; ++index)
        {
            int length2 = (int)ReadCompressedUInt32(stream);
            if (length2 < 0)
                throw new InvalidDataException("ReadCustomTypeArray read negative size value: " + length2.ToString() + " before position: " + stream.Position.ToString());
            if (length2 > stream.Available || length2 > (int)short.MaxValue)
            {
                stream.Position = stream.Length;
                throw new InvalidDataException("ReadCustomTypeArray read size value: " + length2.ToString() + " larger than short.MaxValue or available data: " + stream.Available.ToString());
            }
            object obj;
            if (customType.DeserializeStreamFunction == null)
            {
                byte[] numArray = new byte[length2];
                stream.Read(numArray, 0, length2);
                obj = customType.DeserializeFunction(numArray);
            }
            else
            {
                int position = stream.Position;
                obj = customType.DeserializeStreamFunction(stream, (short)length2);
                if (stream.Position - position != length2)
                    stream.Position = position + length2;
            }
            if (obj != null && customType.Type.IsAssignableFrom(obj.GetType()))
                instance.SetValue(obj, (int)index);
        }
        return (object)instance;
    }

    private Type ReadDictionaryType(
      StreamBuffer stream,
      out Protocol18.GpType keyReadType,
      out Protocol18.GpType valueReadType)
    {
        keyReadType = (Protocol18.GpType)stream.ReadByte();
        Protocol18.GpType gpType = (Protocol18.GpType)stream.ReadByte();
        valueReadType = gpType;
        Type type1 = keyReadType != Protocol18.GpType.Unknown ? Protocol18.GetAllowedDictionaryKeyTypes(keyReadType) : typeof(object);
        Type type2;
        switch (gpType)
        {
            case Protocol18.GpType.Unknown:
                type2 = typeof(object);
                break;
            case Protocol18.GpType.Dictionary:
                type2 = ReadDictionaryType(stream);
                break;
            case Protocol18.GpType.ObjectArray:
                type2 = typeof(object[]);
                break;
            case Protocol18.GpType.Array:
                type2 = GetDictArrayType(stream);
                valueReadType = Protocol18.GpType.Unknown;
                break;
            case Protocol18.GpType.HashtableArray:
                type2 = typeof(Hashtable[]);
                break;
            default:
                type2 = Protocol18.GetClrArrayType(gpType);
                break;
        }
        return typeof(Dictionary<,>).MakeGenericType(type1, type2);
    }

    private Type ReadDictionaryType(StreamBuffer stream)
    {
        Protocol18.GpType gpType1 = (Protocol18.GpType)stream.ReadByte();
        Protocol18.GpType gpType2 = (Protocol18.GpType)stream.ReadByte();
        Type type1 = gpType1 != Protocol18.GpType.Unknown ? Protocol18.GetAllowedDictionaryKeyTypes(gpType1) : typeof(object);
        Type type2;
        switch (gpType2)
        {
            case Protocol18.GpType.Unknown:
                type2 = typeof(object);
                break;
            case Protocol18.GpType.Dictionary:
                type2 = ReadDictionaryType(stream);
                break;
            case Protocol18.GpType.Array:
                type2 = GetDictArrayType(stream);
                break;
            default:
                type2 = Protocol18.GetClrArrayType(gpType2);
                break;
        }
        return typeof(Dictionary<,>).MakeGenericType(type1, type2);
    }

    private Type GetDictArrayType(StreamBuffer stream)
    {
        Protocol18.GpType gpType = (Protocol18.GpType)stream.ReadByte();
        int num = 0;
        for (; gpType == Protocol18.GpType.Array; gpType = (Protocol18.GpType)stream.ReadByte())
            ++num;
        Type dictArrayType = Protocol18.GetClrArrayType(gpType).MakeArrayType();
        for (uint index = 0; (long)index < (long)num; ++index)
            dictArrayType = dictArrayType.MakeArrayType();
        return dictArrayType;
    }

    private IDictionary ReadDictionary(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        Protocol18.GpType keyReadType;
        Protocol18.GpType valueReadType;
        Type type = ReadDictionaryType(stream, out keyReadType, out valueReadType);
        if (type == (Type)null || !(Activator.CreateInstance(type) is IDictionary instance))
            return (IDictionary)null;
        ReadDictionaryElements(stream, keyReadType, valueReadType, instance, flags, parameters);
        return instance;
    }

    private bool ReadDictionaryElements(
      StreamBuffer stream,
      Protocol18.GpType keyReadType,
      Protocol18.GpType valueReadType,
      IDictionary dictionary,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint num = ReadCompressedUInt32(stream);
        for (uint index = 0; index < num; ++index)
        {
            object key = keyReadType == Protocol18.GpType.Unknown ? Read(stream, flags, parameters) : Read(stream, (byte)keyReadType);
            object obj = valueReadType == Protocol18.GpType.Unknown ? Read(stream, flags, parameters) : Read(stream, (byte)valueReadType);
            if (key != null)
                dictionary.Add(key, obj);
        }
        return true;
    }

    private object[] ReadObjectArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        object[] objArray = new object[(int)length];
        for (uint index = 0; index < length; ++index)
        {
            object obj = Read(stream, flags, parameters);
            objArray[(int)index] = obj;
        }
        return objArray;
    }

    private StructWrapper[] ReadWrapperArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        StructWrapper[] structWrapperArray = new StructWrapper[(int)length];
        for (uint index = 0; index < length; ++index)
        {
            object obj = Read(stream, flags, parameters);
            structWrapperArray[(int)index] = obj as StructWrapper;
            if (obj == null)
                Log.Debug("Error: ReadWrapperArray hit null");
            if (structWrapperArray[(int)index] == null)
                Log.Debug("Error: ReadWrapperArray null wrapper");
        }
        return structWrapperArray;
    }

    private bool[] ReadBooleanArray(StreamBuffer stream)
    {
        uint length = ReadCompressedUInt32(stream);
        bool[] flagArray1 = new bool[(int)length];
        int num1 = (int)length / 8;
        int num2 = 0;
        for (; num1 > 0; --num1)
        {
            byte num3 = stream.ReadByte();
            bool[] flagArray2 = flagArray1;
            int index1 = num2;
            int num4 = index1 + 1;
            int num5 = ((int)num3 & 1) == 1 ? 1 : 0;
            flagArray2[index1] = num5 != 0;
            bool[] flagArray3 = flagArray1;
            int index2 = num4;
            int num6 = index2 + 1;
            int num7 = ((int)num3 & 2) == 2 ? 1 : 0;
            flagArray3[index2] = num7 != 0;
            bool[] flagArray4 = flagArray1;
            int index3 = num6;
            int num8 = index3 + 1;
            int num9 = ((int)num3 & 4) == 4 ? 1 : 0;
            flagArray4[index3] = num9 != 0;
            bool[] flagArray5 = flagArray1;
            int index4 = num8;
            int num10 = index4 + 1;
            int num11 = ((int)num3 & 8) == 8 ? 1 : 0;
            flagArray5[index4] = num11 != 0;
            bool[] flagArray6 = flagArray1;
            int index5 = num10;
            int num12 = index5 + 1;
            int num13 = ((int)num3 & 16) == 16 ? 1 : 0;
            flagArray6[index5] = num13 != 0;
            bool[] flagArray7 = flagArray1;
            int index6 = num12;
            int num14 = index6 + 1;
            int num15 = ((int)num3 & 32) == 32 ? 1 : 0;
            flagArray7[index6] = num15 != 0;
            bool[] flagArray8 = flagArray1;
            int index7 = num14;
            int num16 = index7 + 1;
            int num17 = ((int)num3 & 64) == 64 ? 1 : 0;
            flagArray8[index7] = num17 != 0;
            bool[] flagArray9 = flagArray1;
            int index8 = num16;
            num2 = index8 + 1;
            int num18 = ((int)num3 & 128) == 128 ? 1 : 0;
            flagArray9[index8] = num18 != 0;
        }
        if ((long)num2 < (long)length)
        {
            byte num19 = stream.ReadByte();
            int index = 0;
            while ((long)num2 < (long)length)
            {
                flagArray1[num2++] = ((int)num19 & (int)Protocol18.boolMasks[index]) == (int)Protocol18.boolMasks[index];
                ++index;
            }
        }
        return flagArray1;
    }

    internal short[] ReadInt16Array(StreamBuffer stream)
    {
        short[] numArray = new short[(int)ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)numArray.Length; ++index)
            numArray[(int)index] = ReadInt16(stream);
        return numArray;
    }

    private float[] ReadSingleArray(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        int num = length * 4;
        float[] dst = new float[length];
        int offset;
        Buffer.BlockCopy((Array)stream.GetBufferAndAdvance(num, out offset), offset, (Array)dst, 0, num);
        return dst;
    }

    private double[] ReadDoubleArray(StreamBuffer stream)
    {
        int length = (int)ReadCompressedUInt32(stream);
        int num = length * 8;
        double[] dst = new double[length];
        int offset;
        Buffer.BlockCopy((Array)stream.GetBufferAndAdvance(num, out offset), offset, (Array)dst, 0, num);
        return dst;
    }

    internal string[] ReadStringArray(StreamBuffer stream)
    {
        string[] strArray = new string[(int)ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)strArray.Length; ++index)
            strArray[(int)index] = ReadString(stream);
        return strArray;
    }

    private Hashtable[] ReadHashtableArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        Hashtable[] hashtableArray = new Hashtable[(int)length];
        for (uint index = 0; index < length; ++index)
            hashtableArray[(int)index] = ReadHashtable(stream, flags, parameters);
        return hashtableArray;
    }

    private IDictionary[] ReadDictionaryArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        Protocol18.GpType keyReadType;
        Protocol18.GpType valueReadType;
        Type type = ReadDictionaryType(stream, out keyReadType, out valueReadType);
        uint length = ReadCompressedUInt32(stream);
        IDictionary[] instance = (IDictionary[])Array.CreateInstance(type, (int)length);
        for (uint index = 0; index < length; ++index)
        {
            instance[(int)index] = (IDictionary)Activator.CreateInstance(type);
            ReadDictionaryElements(stream, keyReadType, valueReadType, instance[(int)index], flags, parameters);
        }
        return instance;
    }

    private Array ReadArrayInArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = ReadCompressedUInt32(stream);
        Array array1 = (Array)null;
        Type elementType = (Type)null;
        for (uint index = 0; index < length; ++index)
        {
            if (Read(stream, flags, parameters) is Array array2)
            {
                if (array1 == null)
                {
                    elementType = array2.GetType();
                    array1 = Array.CreateInstance(elementType, (int)length);
                }
                if (elementType.IsAssignableFrom(array2.GetType()))
                    array1.SetValue((object)array2, (int)index);
            }
        }
        return array1;
    }

    internal int ReadInt1(StreamBuffer stream, bool signNegative)
    {
        return signNegative ? (int)-stream.ReadByte() : (int)stream.ReadByte();
    }

    internal int ReadInt2(StreamBuffer stream, bool signNegative)
    {
        return signNegative ? (int)-ReadUShort(stream) : (int)ReadUShort(stream);
    }

    internal int ReadCompressedInt32(StreamBuffer stream)
    {
        return DecodeZigZag32(ReadCompressedUInt32(stream));
    }

    private uint ReadCompressedUInt32(StreamBuffer stream)
    {
        uint num1 = 0;
        int num2 = 0;
        byte[] buffer = stream.GetBuffer();
        int position = stream.Position;
        while (num2 != 35)
        {
            if (position >= stream.Length)
            {
                stream.Position = stream.Length;
                string[] strArray = new string[8];
                strArray[0] = "Failed to read full uint. offset: ";
                strArray[1] = position.ToString();
                strArray[2] = " stream.Length: ";
                int num3 = stream.Length;
                strArray[3] = num3.ToString();
                strArray[4] = " data.Length: ";
                num3 = buffer.Length;
                strArray[5] = num3.ToString();
                strArray[6] = " stream.Available: ";
                num3 = stream.Available;
                strArray[7] = num3.ToString();
                throw new EndOfStreamException(string.Concat(strArray));
            }
            byte num4 = buffer[position];
            ++position;
            num1 |= (uint)(((int)num4 & (int)sbyte.MaxValue) << num2);
            num2 += 7;
            if (((int)num4 & 128) == 0)
                break;
        }
        stream.Position = position;
        return num1;
    }

    internal long ReadCompressedInt64(StreamBuffer stream)
    {
        return DecodeZigZag64(ReadCompressedUInt64(stream));
    }

    private ulong ReadCompressedUInt64(StreamBuffer stream)
    {
        ulong num1 = 0;
        int num2 = 0;
        byte[] buffer = stream.GetBuffer();
        int position = stream.Position;
        while (num2 != 70)
        {
            if (position >= buffer.Length)
                throw new EndOfStreamException("Failed to read full ulong.");
            byte num3 = buffer[position];
            ++position;
            num1 |= (ulong)((int)num3 & (int)sbyte.MaxValue) << num2;
            num2 += 7;
            if (((int)num3 & 128) == 0)
                break;
        }
        stream.Position = position;
        return num1;
    }

    internal int[] ReadCompressedInt32Array(StreamBuffer stream)
    {
        int[] numArray = new int[(int)ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)numArray.Length; ++index)
            numArray[(int)index] = ReadCompressedInt32(stream);
        return numArray;
    }

    internal long[] ReadCompressedInt64Array(StreamBuffer stream)
    {
        long[] numArray = new long[(int)ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)numArray.Length; ++index)
            numArray[(int)index] = ReadCompressedInt64(stream);
        return numArray;
    }

    private int DecodeZigZag32(uint value) => (int)((long)(value >> 1) ^ (long)-(value & 1U));

    private long DecodeZigZag64(ulong value) => (long)(value >> 1) ^ -((long)value & 1L);

    internal void Write(StreamBuffer stream, object value, bool writeType)
    {
        if (value == null)
            Write(stream, value, Protocol18.GpType.Null, writeType);
        else
            Write(stream, value, GetCodeOfType(value.GetType()), writeType);
    }

    private void Write(
      StreamBuffer stream,
      object value,
      Protocol18.GpType gpType,
      bool writeType)
    {
        switch (gpType)
        {
            case Protocol18.GpType.Unknown:
                switch (value)
                {
                    case ByteArraySlice _:
                        ByteArraySlice buffer = (ByteArraySlice)value;
                        WriteByteArraySlice(stream, buffer, writeType);
                        return;
                    case ArraySegment<byte> seg:
                        WriteArraySegmentByte(stream, seg, writeType);
                        return;
                    case StructWrapper structWrapper:
                        switch (structWrapper.wrappedType)
                        {
                            case WrappedType.Bool:
                                WriteBoolean(stream, value.Get<bool>(), writeType);
                                return;
                            case WrappedType.Byte:
                                WriteByte(stream, value.Get<byte>(), writeType);
                                return;
                            case WrappedType.Int16:
                                WriteInt16(stream, value.Get<short>(), writeType);
                                return;
                            case WrappedType.Int32:
                                WriteCompressedInt32(stream, value.Get<int>(), writeType);
                                return;
                            case WrappedType.Int64:
                                WriteCompressedInt64(stream, value.Get<long>(), writeType);
                                return;
                            case WrappedType.Single:
                                WriteSingle(stream, value.Get<float>(), writeType);
                                return;
                            case WrappedType.Double:
                                WriteDouble(stream, value.Get<double>(), writeType);
                                return;
                            default:
                                WriteCustomType(stream, value, writeType);
                                return;
                        }
                    default:
                        goto label_18;
                }
            case Protocol18.GpType.Boolean:
                WriteBoolean(stream, (bool)value, writeType);
                break;
            case Protocol18.GpType.Byte:
                WriteByte(stream, (byte)value, writeType);
                break;
            case Protocol18.GpType.Short:
                WriteInt16(stream, (short)value, writeType);
                break;
            case Protocol18.GpType.Float:
                WriteSingle(stream, (float)value, writeType);
                break;
            case Protocol18.GpType.Double:
                WriteDouble(stream, (double)value, writeType);
                break;
            case Protocol18.GpType.String:
                WriteString(stream, (string)value, writeType);
                break;
            case Protocol18.GpType.Null:
                if (!writeType)
                    break;
                stream.WriteByte((byte)8);
                break;
            case Protocol18.GpType.CompressedInt:
                WriteCompressedInt32(stream, (int)value, writeType);
                break;
            case Protocol18.GpType.CompressedLong:
                WriteCompressedInt64(stream, (long)value, writeType);
                break;
            case Protocol18.GpType.Custom:
            label_18:
                WriteCustomType(stream, value, writeType);
                break;
            case Protocol18.GpType.Dictionary:
                WriteDictionary(stream, (object)(IDictionary)value, writeType);
                break;
            case Protocol18.GpType.Hashtable:
                WriteHashtable(stream, (object)(Hashtable)value, writeType);
                break;
            case Protocol18.GpType.ObjectArray:
                WriteObjectArray(stream, (IList)value, writeType);
                break;
            case Protocol18.GpType.OperationRequest:
                SerializeOperationRequest(stream, (OperationRequest)value, writeType);
                break;
            case Protocol18.GpType.OperationResponse:
                SerializeOperationResponse(stream, (OperationResponse)value, writeType);
                break;
            case Protocol18.GpType.EventData:
                SerializeEventData(stream, (EventData)value, writeType);
                break;
            case Protocol18.GpType.Array:
                WriteArrayInArray(stream, value, writeType);
                break;
            case Protocol18.GpType.BooleanArray:
                WriteBoolArray(stream, (bool[])value, writeType);
                break;
            case Protocol18.GpType.ByteArray:
                WriteByteArray(stream, (byte[])value, writeType);
                break;
            case Protocol18.GpType.ShortArray:
                WriteInt16Array(stream, (short[])value, writeType);
                break;
            case Protocol18.GpType.FloatArray:
                WriteSingleArray(stream, (float[])value, writeType);
                break;
            case Protocol18.GpType.DoubleArray:
                WriteDoubleArray(stream, (double[])value, writeType);
                break;
            case Protocol18.GpType.StringArray:
                WriteStringArray(stream, value, writeType);
                break;
            case Protocol18.GpType.CompressedIntArray:
                WriteInt32ArrayCompressed(stream, (int[])value, writeType);
                break;
            case Protocol18.GpType.CompressedLongArray:
                WriteInt64ArrayCompressed(stream, (long[])value, writeType);
                break;
            case Protocol18.GpType.CustomTypeArray:
                WriteCustomTypeArray(stream, value, writeType);
                break;
            case Protocol18.GpType.DictionaryArray:
                WriteDictionaryArray(stream, (IDictionary[])value, writeType);
                break;
            case Protocol18.GpType.HashtableArray:
                WriteHashtableArray(stream, value, writeType);
                break;
        }
    }

    public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)26);
        stream.WriteByte(serObject.Code);
        WriteParameterTable(stream, serObject.Parameters);
    }

    private void WriteParameterTable(StreamBuffer stream, Dictionary<byte, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            WriteByte(stream, (byte)0, false);
        }
        else
        {
            WriteByte(stream, (byte)parameters.Count, false);
            foreach (KeyValuePair<byte, object> parameter in parameters)
            {
                stream.WriteByte(parameter.Key);
                Write(stream, parameter.Value, true);
            }
        }
    }

    private void SerializeOperationRequest(
      StreamBuffer stream,
      OperationRequest operation,
      bool setType)
    {
        SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
    }

    public override void SerializeOperationRequest(
      StreamBuffer stream,
      byte operationCode,
      Dictionary<byte, object> parameters,
      bool setType)
    {
        if (setType)
            stream.WriteByte((byte)24);
        stream.WriteByte(operationCode);
        WriteParameterTable(stream, parameters);
    }

    public override void SerializeOperationResponse(
      StreamBuffer stream,
      OperationResponse serObject,
      bool setType)
    {
        if (setType)
            stream.WriteByte((byte)25);
        stream.WriteByte(serObject.OperationCode);
        WriteInt16(stream, serObject.ReturnCode, false);
        if (string.IsNullOrEmpty(serObject.DebugMessage))
        {
            stream.WriteByte((byte)8);
        }
        else
        {
            stream.WriteByte((byte)7);
            WriteString(stream, serObject.DebugMessage, false);
        }
        WriteParameterTable(stream, serObject.Parameters);
    }

    internal void WriteByte(StreamBuffer stream, byte value, bool writeType)
    {
        if (writeType)
        {
            if (value == (byte)0)
            {
                stream.WriteByte((byte)34);
                return;
            }
            stream.WriteByte((byte)3);
        }
        stream.WriteByte(value);
    }

    internal void WriteBoolean(StreamBuffer stream, bool value, bool writeType)
    {
        if (writeType)
        {
            if (value)
                stream.WriteByte((byte)28);
            else
                stream.WriteByte((byte)27);
        }
        else
            stream.WriteByte(value ? (byte)1 : (byte)0);
    }

    internal void WriteUShort(StreamBuffer stream, ushort value)
    {
        stream.WriteBytes((byte)value, (byte)((uint)value >> 8));
    }

    internal void WriteInt16(StreamBuffer stream, short value, bool writeType)
    {
        if (writeType)
        {
            if (value == (short)0)
            {
                stream.WriteByte((byte)29);
                return;
            }
            stream.WriteByte((byte)4);
        }
        stream.WriteBytes((byte)value, (byte)((uint)value >> 8));
    }

    internal void WriteDouble(StreamBuffer stream, double value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)6);
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(8, out offset);
        lock (memDoubleBlock)
        {
            memDoubleBlock[0] = value;
            Buffer.BlockCopy((Array)memDoubleBlock, 0, (Array)bufferAndAdvance, offset, 8);
        }
    }

    internal void WriteSingle(StreamBuffer stream, float value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)5);
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(4, out offset);
        lock (memFloatBlock)
        {
            memFloatBlock[0] = value;
            Buffer.BlockCopy((Array)memFloatBlock, 0, (Array)bufferAndAdvance, offset, 4);
        }
    }

    internal void WriteString(StreamBuffer stream, string value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)7);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > (int)short.MaxValue)
            throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + byteCount.ToString());
        WriteIntLength(stream, byteCount);
        int offset = 0;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(byteCount, out offset);
        Encoding.UTF8.GetBytes(value, 0, value.Length, bufferAndAdvance, offset);
    }

    private void WriteHashtable(StreamBuffer stream, object value, bool writeType)
    {
        Hashtable hashtable = (Hashtable)value;
        if (writeType)
            stream.WriteByte((byte)21);
        WriteIntLength(stream, hashtable.Count);
        foreach (object key in hashtable.Keys)
        {
            Write(stream, key, true);
            Write(stream, hashtable[key], true);
        }
    }

    internal void WriteByteArray(StreamBuffer stream, byte[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)67);
        WriteIntLength(stream, value.Length);
        stream.Write(value, 0, value.Length);
    }

    private void WriteArraySegmentByte(StreamBuffer stream, ArraySegment<byte> seg, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)67);
        int count = seg.Count;
        WriteIntLength(stream, count);
        if (count <= 0)
            return;
        stream.Write(seg.Array, seg.Offset, count);
    }

    private void WriteByteArraySlice(StreamBuffer stream, ByteArraySlice buffer, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)67);
        int count = buffer.Count;
        WriteIntLength(stream, count);
        stream.Write(buffer.Buffer, buffer.Offset, count);
        buffer.Release();
    }

    internal void WriteInt32ArrayCompressed(StreamBuffer stream, int[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)73);
        WriteIntLength(stream, value.Length);
        for (int index = 0; index < value.Length; ++index)
            WriteCompressedInt32(stream, value[index], false);
    }

    private void WriteInt64ArrayCompressed(StreamBuffer stream, long[] values, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)74);
        WriteIntLength(stream, values.Length);
        for (int index = 0; index < values.Length; ++index)
            WriteCompressedInt64(stream, values[index], false);
    }

    internal void WriteBoolArray(StreamBuffer stream, bool[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)66);
        WriteIntLength(stream, value.Length);
        int num1 = value.Length >> 3;
        byte[] buffer = new byte[num1 + 1];
        int count = 0;
        int index1 = 0;
        while (num1 > 0)
        {
            byte num2 = 0;
            bool[] flagArray1 = value;
            int index2 = index1;
            int num3 = index2 + 1;
            if (flagArray1[index2])
                num2 |= (byte)1;
            bool[] flagArray2 = value;
            int index3 = num3;
            int num4 = index3 + 1;
            if (flagArray2[index3])
                num2 |= (byte)2;
            bool[] flagArray3 = value;
            int index4 = num4;
            int num5 = index4 + 1;
            if (flagArray3[index4])
                num2 |= (byte)4;
            bool[] flagArray4 = value;
            int index5 = num5;
            int num6 = index5 + 1;
            if (flagArray4[index5])
                num2 |= (byte)8;
            bool[] flagArray5 = value;
            int index6 = num6;
            int num7 = index6 + 1;
            if (flagArray5[index6])
                num2 |= (byte)16;
            bool[] flagArray6 = value;
            int index7 = num7;
            int num8 = index7 + 1;
            if (flagArray6[index7])
                num2 |= (byte)32;
            bool[] flagArray7 = value;
            int index8 = num8;
            int num9 = index8 + 1;
            if (flagArray7[index8])
                num2 |= (byte)64;
            bool[] flagArray8 = value;
            int index9 = num9;
            index1 = index9 + 1;
            if (flagArray8[index9])
                num2 |= (byte)128;
            buffer[count] = num2;
            --num1;
            ++count;
        }
        if (index1 < value.Length)
        {
            byte num10 = 0;
            int num11 = 0;
            for (; index1 < value.Length; ++index1)
            {
                if (value[index1])
                    num10 |= (byte)(1 << num11);
                ++num11;
            }
            buffer[count] = num10;
            ++count;
        }
        stream.Write(buffer, 0, count);
    }

    internal void WriteInt16Array(StreamBuffer stream, short[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)68);
        WriteIntLength(stream, value.Length);
        for (int index = 0; index < value.Length; ++index)
            WriteInt16(stream, value[index], false);
    }

    internal void WriteSingleArray(StreamBuffer stream, float[] values, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)69);
        WriteIntLength(stream, values.Length);
        int num = values.Length * 4;
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(num, out offset);
        Buffer.BlockCopy((Array)values, 0, (Array)bufferAndAdvance, offset, num);
    }

    internal void WriteDoubleArray(StreamBuffer stream, double[] values, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)70);
        WriteIntLength(stream, values.Length);
        int num = values.Length * 8;
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(num, out offset);
        Buffer.BlockCopy((Array)values, 0, (Array)bufferAndAdvance, offset, num);
    }

    internal void WriteStringArray(StreamBuffer stream, object value0, bool writeType)
    {
        string[] strArray = (string[])value0;
        if (writeType)
            stream.WriteByte((byte)71);
        WriteIntLength(stream, strArray.Length);
        for (int index = 0; index < strArray.Length; ++index)
        {
            if (strArray[index] == null)
                throw new InvalidDataException("Unexpected - cannot serialize string array with null element " + index.ToString());
            WriteString(stream, strArray[index], false);
        }
    }

    private void WriteObjectArray(StreamBuffer stream, object array, bool writeType)
    {
        WriteObjectArray(stream, (IList)array, writeType);
    }

    private void WriteObjectArray(StreamBuffer stream, IList array, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)23);
        WriteIntLength(stream, array.Count);
        for (int index = 0; index < array.Count; ++index)
        {
            object obj = array[index];
            Write(stream, obj, true);
        }
    }

    private void WriteArrayInArray(StreamBuffer stream, object value, bool writeType)
    {
        object[] objArray = (object[])value;
        stream.WriteByte((byte)64);
        WriteIntLength(stream, objArray.Length);
        foreach (object obj in objArray)
            Write(stream, obj, true);
    }

    private void WriteCustomTypeBody(CustomType customType, StreamBuffer stream, object value)
    {
        if (customType.SerializeFunction != null)
        {
            byte[] buffer = customType.SerializeFunction(value);
            WriteIntLength(stream, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
        else if (customType.SerializeStreamFunction != null)
        {
            int position = stream.Position;
            ++stream.Position;
            uint num = (uint)customType.SerializeStreamFunction(stream, value);
            int count1 = stream.Position - position - 1;
            if ((long)count1 != (long)num)
                Log.Debug("Serialization for Custom Type '" + value.GetType()?.ToString() + "' returns size " + num.ToString() + " bytes but wrote " + count1.ToString() + " bytes. Sending the latter as size.");
            int count2 = WriteCompressedUInt32(memCustomTypeBodyLengthSerialized, (uint)count1);
            if (count2 == 1)
            {
                stream.GetBuffer()[position] = memCustomTypeBodyLengthSerialized[0];
            }
            else
            {
                for (int index = 0; index < count2 - 1; ++index)
                    stream.WriteByte((byte)0);
                Buffer.BlockCopy((Array)stream.GetBuffer(), position + 1, (Array)stream.GetBuffer(), position + count2, count1);
                Buffer.BlockCopy((Array)memCustomTypeBodyLengthSerialized, 0, (Array)stream.GetBuffer(), position, count2);
                stream.Position = position + count2 + count1;
            }
        }
    }

    private void WriteCustomType(StreamBuffer stream, object value, bool writeType)
    {
        Type key = !(value is StructWrapper structWrapper) ? value.GetType() : structWrapper.ttype;
        CustomType? customType;
        if (!Protocol.TypeDict.TryGetValue(key, out customType))
            throw new Exception("Write failed. Custom type not found: " + key?.ToString());
        if (writeType)
        {
            if (customType.Code < (byte)100)
            {
                stream.WriteByte((byte)(128U + (uint)customType.Code));
            }
            else
            {
                stream.WriteByte((byte)19);
                stream.WriteByte(customType.Code);
            }
        }
        else
            stream.WriteByte(customType.Code);
        WriteCustomTypeBody(customType, stream, value);
    }

    private void WriteCustomTypeArray(StreamBuffer stream, object value, bool writeType)
    {
        IList list = (IList)value;
        Type? elementType = value.GetType().GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        CustomType? customType;
        if (!Protocol.TypeDict.TryGetValue(elementType, out customType))
            throw new Exception("Write failed. Custom type of element not found: " + elementType?.ToString());
        if (writeType)
            stream.WriteByte((byte)83);
        WriteIntLength(stream, list.Count);
        stream.WriteByte(customType.Code);
        foreach (object obj in (IEnumerable)list)
            WriteCustomTypeBody(customType, stream, obj);
    }

    private bool WriteArrayHeader(StreamBuffer stream, Type type)
    {
        Type? elementType;
        for (elementType = type.GetElementType(); elementType.IsArray; elementType = elementType.GetElementType())
            stream.WriteByte((byte)64);
        Protocol18.GpType codeOfType = GetCodeOfType(elementType);
        if (codeOfType == Protocol18.GpType.Unknown)
            return false;
        stream.WriteByte((byte)(codeOfType | Protocol18.GpType.CustomTypeSlim));
        return true;
    }

    private void WriteDictionaryElements(
      StreamBuffer stream,
      IDictionary dictionary,
      Protocol18.GpType keyWriteType,
      Protocol18.GpType valueWriteType)
    {
        WriteIntLength(stream, dictionary.Count);
        foreach (DictionaryEntry dictionaryEntry in dictionary)
        {
            Write(stream, dictionaryEntry.Key, keyWriteType == Protocol18.GpType.Unknown);
            Write(stream, dictionaryEntry.Value!, valueWriteType == Protocol18.GpType.Unknown);
        }
    }

    private void WriteDictionary(StreamBuffer stream, object dict, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)20);
        Protocol18.GpType keyWriteType;
        Protocol18.GpType valueWriteType;
        WriteDictionaryHeader(stream, dict.GetType(), out keyWriteType, out valueWriteType);
        IDictionary dictionary = (IDictionary)dict;
        WriteDictionaryElements(stream, dictionary, keyWriteType, valueWriteType);
    }

    private void WriteDictionaryHeader(
      StreamBuffer stream,
      Type type,
      out Protocol18.GpType keyWriteType,
      out Protocol18.GpType valueWriteType)
    {
        Type[] genericArguments = type.GetGenericArguments();
        if (genericArguments[0] == typeof(object))
        {
            stream.WriteByte((byte)0);
            keyWriteType = Protocol18.GpType.Unknown;
        }
        else
        {
            keyWriteType = genericArguments[0].IsPrimitive || !(genericArguments[0] != typeof(string)) ? GetCodeOfType(genericArguments[0]) : throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            if (keyWriteType == Protocol18.GpType.Unknown)
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            stream.WriteByte((byte)keyWriteType);
        }
        if (genericArguments[1] == typeof(object))
        {
            stream.WriteByte((byte)0);
            valueWriteType = Protocol18.GpType.Unknown;
        }
        else if (genericArguments[1].IsArray)
        {
            if (!WriteArrayType(stream, genericArguments[1], out valueWriteType))
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
        }
        else
        {
            valueWriteType = GetCodeOfType(genericArguments[1]);
            if (valueWriteType == Protocol18.GpType.Unknown)
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            if (valueWriteType == Protocol18.GpType.Array)
            {
                if (!WriteArrayHeader(stream, genericArguments[1]))
                    throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            }
            else if (valueWriteType == Protocol18.GpType.Dictionary)
            {
                stream.WriteByte((byte)valueWriteType);
                WriteDictionaryHeader(stream, genericArguments[1], out Protocol18.GpType _, out Protocol18.GpType _);
            }
            else
                stream.WriteByte((byte)valueWriteType);
        }
    }

    private bool WriteArrayType(StreamBuffer stream, Type type, out Protocol18.GpType writeType)
    {
        Type elementType = type.GetElementType();
        if (elementType == null)
            throw new InvalidDataException("Unexpected - cannot serialize array with type: " + type?.ToString());
        if (elementType.IsArray)
        {
            for (; elementType != null && elementType.IsArray; elementType = elementType.GetElementType())
                stream.WriteByte((byte)64);
            byte num = (byte)(GetCodeOfType(elementType) | Protocol18.GpType.Array);
            stream.WriteByte(num);
            writeType = Protocol18.GpType.Array;
            return true;
        }
        if (elementType.IsPrimitive)
        {
            byte num = (byte)(GetCodeOfType(elementType) | Protocol18.GpType.Array);
            if (num == (byte)226)
                num = (byte)67;
            stream.WriteByte(num);
            if (Enum.IsDefined(typeof(Protocol18.GpType), (object)num))
            {
                writeType = (Protocol18.GpType)num;
                return true;
            }
            writeType = Protocol18.GpType.Unknown;
            return false;
        }
        if (elementType == typeof(string))
        {
            stream.WriteByte((byte)71);
            writeType = Protocol18.GpType.StringArray;
            return true;
        }
        if (elementType == typeof(object))
        {
            stream.WriteByte((byte)23);
            writeType = Protocol18.GpType.ObjectArray;
            return true;
        }
        if (elementType == typeof(Hashtable))
        {
            stream.WriteByte((byte)85);
            writeType = Protocol18.GpType.HashtableArray;
            return true;
        }
        writeType = Protocol18.GpType.Unknown;
        return false;
    }

    private void WriteHashtableArray(StreamBuffer stream, object value, bool writeType)
    {
        Hashtable[] hashtableArray = (Hashtable[])value;
        if (writeType)
            stream.WriteByte((byte)85);
        WriteIntLength(stream, hashtableArray.Length);
        foreach (Hashtable hashtable in hashtableArray)
            WriteHashtable(stream, (object)hashtable, false);
    }

    private void WriteDictionaryArray(StreamBuffer stream, IDictionary[] dictArray, bool writeType)
    {
        stream.WriteByte((byte)84);
        Protocol18.GpType keyWriteType;
        Protocol18.GpType valueWriteType;
        WriteDictionaryHeader(stream, dictArray.GetType().GetElementType()!, out keyWriteType, out valueWriteType);
        WriteIntLength(stream, dictArray.Length);
        foreach (IDictionary dict in dictArray)
            WriteDictionaryElements(stream, dict, keyWriteType, valueWriteType);
    }

    private void WriteIntLength(StreamBuffer stream, int value)
    {
        WriteCompressedUInt32(stream, (uint)value);
    }

    private void WriteVarInt32(StreamBuffer stream, int value, bool writeType)
    {
        WriteCompressedInt32(stream, value, writeType);
    }

    private void WriteCompressedInt32(StreamBuffer stream, int value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0)
            {
                stream.WriteByte((byte)30);
                return;
            }
            if (value > 0)
            {
                if (value <= (int)byte.MaxValue)
                {
                    stream.WriteByte((byte)11);
                    stream.WriteByte((byte)value);
                    return;
                }
                if (value <= (int)ushort.MaxValue)
                {
                    stream.WriteByte((byte)13);
                    WriteUShort(stream, (ushort)value);
                    return;
                }
            }
            else if (value >= -65535)
            {
                if (value >= -255)
                {
                    stream.WriteByte((byte)12);
                    stream.WriteByte((byte)-value);
                    return;
                }
                if (value >= -65535)
                {
                    stream.WriteByte((byte)14);
                    WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType)
            stream.WriteByte((byte)9);
        uint num = EncodeZigZag32(value);
        WriteCompressedUInt32(stream, num);
    }

    private void WriteCompressedInt64(StreamBuffer stream, long value, bool writeType)
    {
        if (writeType)
        {
            if (value == 0L)
            {
                stream.WriteByte((byte)31);
                return;
            }
            if (value > 0L)
            {
                if (value <= (long)byte.MaxValue)
                {
                    stream.WriteByte((byte)15);
                    stream.WriteByte((byte)value);
                    return;
                }
                if (value <= (long)ushort.MaxValue)
                {
                    stream.WriteByte((byte)17);
                    WriteUShort(stream, (ushort)value);
                    return;
                }
            }
            else if (value >= -65535L)
            {
                if (value >= -255L)
                {
                    stream.WriteByte((byte)16);
                    stream.WriteByte((byte)-value);
                    return;
                }
                if (value >= -65535L)
                {
                    stream.WriteByte((byte)18);
                    WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType)
            stream.WriteByte((byte)10);
        ulong num = EncodeZigZag64(value);
        WriteCompressedUInt64(stream, num);
    }

    private void WriteCompressedUInt32(StreamBuffer stream, uint value)
    {
        lock (memCompressedUInt32)
            stream.Write(memCompressedUInt32, 0, WriteCompressedUInt32(memCompressedUInt32, value));
    }

    private int WriteCompressedUInt32(byte[] buffer, uint value)
    {
        int index = 0;
        buffer[index] = (byte)(value & (uint)sbyte.MaxValue);
        for (value >>= 7; value > 0U; value >>= 7)
        {
            buffer[index] |= (byte)128;
            buffer[++index] = (byte)(value & (uint)sbyte.MaxValue);
        }
        return index + 1;
    }

    private void WriteCompressedUInt64(StreamBuffer stream, ulong value)
    {
        int index = 0;
        lock (memCompressedUInt64)
        {
            memCompressedUInt64[index] = (byte)(value & (ulong)sbyte.MaxValue);
            for (value >>= 7; value > 0UL; value >>= 7)
            {
                memCompressedUInt64[index] |= (byte)128;
                memCompressedUInt64[++index] = (byte)(value & (ulong)sbyte.MaxValue);
            }
            int count = index + 1;
            stream.Write(memCompressedUInt64, 0, count);
        }
    }

    private uint EncodeZigZag32(int value) => (uint)(value << 1 ^ value >> 31);

    private ulong EncodeZigZag64(long value) => (ulong)(value << 1 ^ value >> 63);

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
        CompressedLong = 10, // 0x0A
        Int1 = 11, // 0x0B
        Int1_ = 12, // 0x0C
        Int2 = 13, // 0x0D
        Int2_ = 14, // 0x0E
        L1 = 15, // 0x0F
        L1_ = 16, // 0x10
        L2 = 17, // 0x11
        L2_ = 18, // 0x12
        Custom = 19, // 0x13
        Dictionary = 20, // 0x14
        Hashtable = 21, // 0x15
        ObjectArray = 23, // 0x17
        OperationRequest = 24, // 0x18
        OperationResponse = 25, // 0x19
        EventData = 26, // 0x1A
        BooleanFalse = 27, // 0x1B
        BooleanTrue = 28, // 0x1C
        ShortZero = 29, // 0x1D
        IntZero = 30, // 0x1E
        LongZero = 31, // 0x1F
        FloatZero = 32, // 0x20
        DoubleZero = 33, // 0x21
        ByteZero = 34, // 0x22
        Array = 64, // 0x40
        BooleanArray = 66, // 0x42
        ByteArray = 67, // 0x43
        ShortArray = 68, // 0x44
        FloatArray = 69, // 0x45
        DoubleArray = 70, // 0x46
        StringArray = 71, // 0x47
        CompressedIntArray = 73, // 0x49
        CompressedLongArray = 74, // 0x4A
        CustomTypeArray = 83, // 0x53
        DictionaryArray = 84, // 0x54
        HashtableArray = 85, // 0x55
        CustomTypeSlim = 128, // 0x80
    }
}
