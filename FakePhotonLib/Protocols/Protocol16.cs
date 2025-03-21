using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated.StructWrapping;
using FakePhotonLib.PhotonRelated;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace FakePhotonLib.Protocols;

public class Protocol16 : IProtocol
{
    private static readonly byte[] versionBytes = { 1, 6 };
    private static readonly byte[] memShort = new byte[2];
    private static readonly long[] memLongBlock = new long[1];
    private static readonly byte[] memLongBlockBytes = new byte[8];
    private static readonly float[] memFloatBlock = new float[1];
    private static readonly byte[] memFloatBlockBytes = new byte[4];
    private static readonly double[] memDoubleBlock = new double[1];
    private static readonly byte[] memDoubleBlockBytes = new byte[8];
    private static readonly byte[] memInteger = new byte[4];
    private static readonly byte[] memLong = new byte[8];
    private static readonly byte[] memFloat = new byte[4];
    private static readonly byte[] memDouble = new byte[8];

    public override string ProtocolType => "GpBinaryV16";
    public override byte[] VersionBytes => versionBytes;

    private bool SerializeCustom(StreamBuffer dout, object serObject)
    {
        Type key = serObject is StructWrapper structWrapper ? structWrapper.ttype : serObject.GetType();
        if (!Protocol.TypeDict.TryGetValue(key, out CustomType? customType))
            return false;

        dout.WriteByte(99);
        dout.WriteByte(customType.Code);

        if (customType.SerializeFunction != null)
        {
            byte[] buffer = customType.SerializeFunction(serObject);
            SerializeLengthAsShort(dout, buffer.Length, $"Custom Type {customType.Code}");
            dout.Write(buffer, 0, buffer.Length);
            return true;
        }

        int position1 = dout.Position;
        dout.Position += 2;
        short length = customType.SerializeStreamFunction!(dout, serObject);
        long position2 = dout.Position;
        dout.Position = position1;
        SerializeLengthAsShort(dout, length, $"Custom Type {customType.Code}");
        dout.Position += length;

        if (dout.Position != position2)
            throw new Exception($"Serialization failed. Stream position corrupted. Should be {position2} is now: {dout.Position} serializedLength: {length}");

        return true;
    }

    private object DeserializeCustom(StreamBuffer din, byte customTypeCode, DeserializationFlags flags = DeserializationFlags.None)
    {
        short length = DeserializeShort(din);
        if (length < 0)
            throw new InvalidDataException($"DeserializeCustom read negative length value: {length} before position: {din.Position}");

        if (length <= din.Available && Protocol.CodeDict.TryGetValue(customTypeCode, out CustomType? customType))
        {
            if (customType.DeserializeStreamFunction == null)
            {
                byte[] buffer2 = new byte[length];
                din.Read(buffer2, 0, length);
                return customType.DeserializeFunction!(buffer2);
            }

            int position = din.Position;
            object obj = customType.DeserializeStreamFunction(din, length);
            if (din.Position - position != length)
                din.Position = position + length;
            return obj;
        }

        int count = Math.Min(length, din.Available);
        byte[] buffer = new byte[count];
        din.Read(buffer, 0, count);
        return buffer;
    }

    private Type GetTypeOfCode(byte typeCode)
    {
        return typeCode switch
        {
            0 or 42 => typeof(object),
            68 => typeof(IDictionary),
            97 => typeof(string[]),
            98 => typeof(byte),
            99 => typeof(CustomType),
            100 => typeof(double),
            101 => typeof(EventData),
            102 => typeof(float),
            104 => typeof(Hashtable),
            105 => typeof(int),
            107 => typeof(short),
            108 => typeof(long),
            110 => typeof(int[]),
            111 => typeof(bool),
            112 => typeof(OperationResponse),
            113 => typeof(OperationRequest),
            115 => typeof(string),
            120 => typeof(byte[]),
            121 => typeof(Array),
            122 => typeof(object[]),
            _ => throw new Exception($"deserialize(): {typeCode}")
        };
    }

    private GpType GetCodeOfType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => GpType.Boolean,
            TypeCode.Byte => GpType.Byte,
            TypeCode.Int16 => GpType.Short,
            TypeCode.Int32 => GpType.Integer,
            TypeCode.Int64 => GpType.Long,
            TypeCode.Single => GpType.Float,
            TypeCode.Double => GpType.Double,
            TypeCode.String => GpType.String,
            _ when type.IsArray => type == typeof(byte[]) ? GpType.ByteArray : GpType.Array,
            _ when type == typeof(Hashtable) => GpType.Hashtable,
            _ when type == typeof(List<object>) => GpType.ObjectArray,
            _ when type.IsGenericType && typeof(Dictionary<,>) == type.GetGenericTypeDefinition() => GpType.Dictionary,
            _ when type == typeof(EventData) => GpType.EventData,
            _ when type == typeof(OperationRequest) => GpType.OperationRequest,
            _ when type == typeof(OperationResponse) => GpType.OperationResponse,
            _ => GpType.Unknown,
        };
    }

    private Array CreateArrayByType(byte arrayType, short length)
    {
        return Array.CreateInstance(GetTypeOfCode(arrayType), length);
    }

    public void SerializeOperationRequest(StreamBuffer stream, OperationRequest operation, bool setType)
    {
        SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
    }

    public override void SerializeOperationRequest(StreamBuffer stream, byte operationCode, Dictionary<byte, object?>? parameters, bool setType)
    {
        if (setType) stream.WriteByte(113);
        stream.WriteByte(operationCode);
        SerializeParameterTable(stream, parameters);
    }

    public override OperationRequest DeserializeOperationRequest(StreamBuffer din, IProtocol.DeserializationFlags flags)
    {
        OperationRequest operationRequest = new OperationRequest()
        {
            OperationCode = DeserializeByte(din)
        };
        operationRequest.Parameters = DeserializeParameterTable(din, operationRequest.Parameters, flags);
        return operationRequest;
    }

    public override void SerializeOperationResponse(StreamBuffer stream, OperationResponse serObject, bool setType)
    {
        if (setType) stream.WriteByte(112);
        stream.WriteByte(serObject.OperationCode);
        SerializeShort(stream, serObject.ReturnCode, false);
        if (string.IsNullOrEmpty(serObject.DebugMessage))
            stream.WriteByte(42);
        else
            SerializeString(stream, serObject.DebugMessage, false);
        SerializeParameterTable(stream, serObject.Parameters);
    }

    public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
    {
        return new()
        {
            Code = DeserializeShort(stream),
            DebugMessage = Deserialize(stream, DeserializeByte(stream), DeserializationFlags.None) as string,
            Parameters = DeserializeParameterTable(stream)
        };
    }

    public override OperationResponse DeserializeOperationResponse(StreamBuffer stream, DeserializationFlags flags = DeserializationFlags.None)
    {
        return new()
        {
            OperationCode = DeserializeByte(stream),
            ReturnCode = DeserializeShort(stream),
            DebugMessage = Deserialize(stream, DeserializeByte(stream), DeserializationFlags.None) as string,
            Parameters = DeserializeParameterTable(stream)
        };
    }

    public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
    {
        if (setType) stream.WriteByte(101);
        stream.WriteByte(serObject.Code);
        SerializeParameterTable(stream, serObject.Parameters);
    }

    public override EventData DeserializeEventData(StreamBuffer din, EventData? target = null, DeserializationFlags flags = DeserializationFlags.None)
    {
        EventData eventData = target ?? new EventData();
        eventData.Code = DeserializeByte(din);
        DeserializeParameterTable(din, eventData.Parameters);
        return eventData;
    }

    private void SerializeParameterTable(StreamBuffer stream, Dictionary<byte, object?>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            SerializeShort(stream, 0, false);
        }
        else
        {
            SerializeLengthAsShort(stream, parameters.Count, "ParameterTable");
            foreach (var parameter in parameters)
            {
                stream.WriteByte(parameter.Key);
                Serialize(stream, parameter.Value, true);
            }
        }
    }

    private Dictionary<byte, object?> DeserializeParameterTable(StreamBuffer stream, Dictionary<byte, object?>? target = null, DeserializationFlags flag = DeserializationFlags.None)
    {
        short capacity = DeserializeShort(stream);
        Dictionary<byte, object?> dictionary = target ?? new Dictionary<byte, object?>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            byte key = stream.ReadByte();
            object? obj = Deserialize(stream, stream.ReadByte(), flag);
            dictionary[key] = obj;
        }
        return dictionary;
    }

    public override void Serialize(StreamBuffer dout, object? serObject, bool setType)
    {
        if (serObject == null)
        {
            if (!setType) return;
            dout.WriteByte(42);
        }
        else
        {
            Type type = serObject is StructWrapper structWrapper ? structWrapper.ttype : serObject.GetType();
            switch (GetCodeOfType(type))
            {
                case GpType.Dictionary:
                    SerializeDictionary(dout, (IDictionary)serObject, setType);
                    break;
                case GpType.Byte:
                    SerializeByte(dout, serObject.Get<byte>(), setType);
                    break;
                case GpType.Double:
                    SerializeDouble(dout, serObject.Get<double>(), setType);
                    break;
                case GpType.EventData:
                    SerializeEventData(dout, (EventData)serObject, setType);
                    break;
                case GpType.Float:
                    SerializeFloat(dout, serObject.Get<float>(), setType);
                    break;
                case GpType.Hashtable:
                    SerializeHashTable(dout, (Hashtable)serObject, setType);
                    break;
                case GpType.Integer:
                    SerializeInteger(dout, serObject.Get<int>(), setType);
                    break;
                case GpType.Short:
                    SerializeShort(dout, serObject.Get<short>(), setType);
                    break;
                case GpType.Long:
                    SerializeLong(dout, serObject.Get<long>(), setType);
                    break;
                case GpType.Boolean:
                    SerializeBoolean(dout, serObject.Get<bool>(), setType);
                    break;
                case GpType.OperationResponse:
                    SerializeOperationResponse(dout, (OperationResponse)serObject, setType);
                    break;
                case GpType.OperationRequest:
                    SerializeOperationRequest(dout, (OperationRequest)serObject, setType);
                    break;
                case GpType.String:
                    SerializeString(dout, (string)serObject, setType);
                    break;
                case GpType.ByteArray:
                    SerializeByteArray(dout, (byte[])serObject, setType);
                    break;
                case GpType.Array:
                    if (serObject is int[] intArray)
                    {
                        SerializeIntArrayOptimized(dout, intArray, setType);
                    }
                    else if (type.GetElementType() == typeof(object))
                    {
                        SerializeObjectArray(dout, (IList)(object[])serObject, setType);
                    }
                    else
                    {
                        SerializeArray(dout, (Array)serObject, setType);
                    }
                    break;
                case GpType.ObjectArray:
                    SerializeObjectArray(dout, (IList)serObject, setType);
                    break;
                default:
                    if (serObject is ArraySegment<byte> arraySegment)
                    {
                        SerializeByteArraySegment(dout, arraySegment.Array!, arraySegment.Offset, arraySegment.Count, setType);
                    }
                    else if (!SerializeCustom(dout, serObject))
                    {
                        throw new Exception($"cannot serialize(): {type}");
                    }
                    break;
            }
        }
    }

    private void SerializeByte(StreamBuffer dout, byte serObject, bool setType)
    {
        if (setType) dout.WriteByte(98);
        dout.WriteByte(serObject);
    }

    private void SerializeBoolean(StreamBuffer dout, bool serObject, bool setType)
    {
        if (setType) dout.WriteByte(111);
        dout.WriteByte(serObject ? (byte)1 : (byte)0);
    }

    public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
    {
        if (setType) dout.WriteByte(107);
        lock (memShort)
        {
            memShort[0] = (byte)(serObject >> 8);
            memShort[1] = (byte)serObject;
            dout.Write(memShort, 0, 2);
        }
    }

    public void SerializeLengthAsShort(StreamBuffer dout, int serObject, string type)
    {
        if (serObject > short.MaxValue || serObject < 0)
            throw new NotSupportedException($"Exceeding 32767 (short.MaxValue) entries are not supported. Failed writing {type}. Length: {serObject}");
        lock (memShort)
        {
            memShort[0] = (byte)(serObject >> 8);
            memShort[1] = (byte)serObject;
            dout.Write(memShort, 0, 2);
        }
    }


    private void SerializeInteger(StreamBuffer dout, int serObject, bool setType)
    {
        if (setType) dout.WriteByte(105);
        lock (memInteger)
        {
            memInteger[0] = (byte)(serObject >> 24);
            memInteger[1] = (byte)(serObject >> 16);
            memInteger[2] = (byte)(serObject >> 8);
            memInteger[3] = (byte)serObject;
            dout.Write(memInteger, 0, 4);
        }
    }

    private void SerializeLong(StreamBuffer dout, long serObject, bool setType)
    {
        if (setType) dout.WriteByte(108);
        lock (memLongBlock)
        {
            memLongBlock[0] = serObject;
            Buffer.BlockCopy(memLongBlock, 0, memLongBlockBytes, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(memLongBlockBytes);
            }
            dout.Write(memLongBlockBytes, 0, 8);
        }
    }

    private void SerializeFloat(StreamBuffer dout, float serObject, bool setType)
    {
        if (setType) dout.WriteByte(102);
        lock (memFloatBlockBytes)
        {
            memFloatBlock[0] = serObject;
            Buffer.BlockCopy(memFloatBlock, 0, memFloatBlockBytes, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(memFloatBlockBytes);
            }
            dout.Write(memFloatBlockBytes, 0, 4);
        }
    }

    private void SerializeDouble(StreamBuffer dout, double serObject, bool setType)
    {
        if (setType) dout.WriteByte(100);
        lock (memDoubleBlockBytes)
        {
            memDoubleBlock[0] = serObject;
            Buffer.BlockCopy(memDoubleBlock, 0, memDoubleBlockBytes, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(memDoubleBlockBytes);
            }
            dout.Write(memDoubleBlockBytes, 0, 8);
        }
    }

    public override void SerializeString(StreamBuffer stream, string value, bool setType)
    {
        if (setType) stream.WriteByte(115);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > short.MaxValue)
            throw new NotSupportedException($"Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: {byteCount}");
        SerializeLengthAsShort(stream, byteCount, "String");
        int offset = 0;
        byte[] buffer = stream.GetBufferAndAdvance(byteCount, out offset);
        Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
    }

    private void SerializeArray(StreamBuffer dout, Array serObject, bool setType)
    {
        if (setType) dout.WriteByte(121);
        SerializeLengthAsShort(dout, serObject.Length, "Array");
        Type? elementType = serObject.GetType().GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        GpType codeOfType = GetCodeOfType(elementType);

        if (codeOfType != 0)
        {
            dout.WriteByte((byte)codeOfType);
            if (codeOfType == GpType.Dictionary)
            {
                SerializeDictionaryHeader(dout, serObject, out bool setKeyType, out bool setValueType);
                foreach (var item in serObject)
                {
                    SerializeDictionaryElements(dout, (IDictionary)item, setKeyType, setValueType);
                }
            }
            else
            {
                foreach (var item in serObject)
                {
                    Serialize(dout, item, false);
                }
            }
        }
        else
        {
            if (!Protocol.TypeDict.TryGetValue(elementType, out CustomType? customType))
                throw new NotSupportedException($"cannot serialize array of type {elementType}");

            dout.WriteByte(99);
            dout.WriteByte(customType.Code);
            foreach (var item in serObject)
            {
                if (customType.SerializeStreamFunction == null)
                {
                    byte[] buffer = customType.SerializeFunction!(item);
                    SerializeLengthAsShort(dout, buffer.Length, $"Custom Type {customType.Code}");
                    dout.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    int position1 = dout.Position;
                    dout.Position += 2;
                    short length = customType.SerializeStreamFunction(dout, item);
                    long position2 = dout.Position;
                    dout.Position = position1;
                    SerializeLengthAsShort(dout, length, $"Custom Type {customType.Code}");
                    dout.Position += length;

                    if (dout.Position != position2)
                        throw new Exception($"Serialization failed. Stream position corrupted. Should be {position2} is now: {dout.Position} serializedLength: {length}");
                }
            }
        }
    }

    private void SerializeByteArray(StreamBuffer dout, byte[] serObject, bool setType)
    {
        if (setType) dout.WriteByte(120);
        SerializeInteger(dout, serObject.Length, false);
        dout.Write(serObject, 0, serObject.Length);
    }

    private void SerializeByteArraySegment(StreamBuffer dout, byte[] serObject, int offset, int count, bool setType)
    {
        if (setType) dout.WriteByte(120);
        SerializeInteger(dout, count, false);
        dout.Write(serObject, offset, count);
    }

    private void SerializeIntArrayOptimized(StreamBuffer dout, int[] serObject, bool setType)
    {
        if (setType) dout.WriteByte(121);
        SerializeLengthAsShort(dout, serObject.Length, "int[]");
        dout.WriteByte(105);
        byte[] buffer = new byte[serObject.Length * 4];
        for (int i = 0, j = 0; i < serObject.Length; i++, j += 4)
        {
            buffer[j] = (byte)(serObject[i] >> 24);
            buffer[j + 1] = (byte)(serObject[i] >> 16);
            buffer[j + 2] = (byte)(serObject[i] >> 8);
            buffer[j + 3] = (byte)serObject[i];
        }
        dout.Write(buffer, 0, buffer.Length);
    }

    private void SerializeStringArray(StreamBuffer dout, string[] serObject, bool setType)
    {
        if (setType) dout.WriteByte(97);
        SerializeLengthAsShort(dout, serObject.Length, "string[]");
        foreach (var str in serObject)
        {
            SerializeString(dout, str, false);
        }
    }

    private void SerializeObjectArray(StreamBuffer dout, IList objects, bool setType)
    {
        if (setType) dout.WriteByte(122);
        SerializeLengthAsShort(dout, objects.Count, "object[]");
        foreach (var obj in objects)
        {
            Serialize(dout, obj, true);
        }
    }

    private void SerializeHashTable(StreamBuffer dout, Hashtable serObject, bool setType)
    {
        if (setType) dout.WriteByte(104);
        SerializeLengthAsShort(dout, serObject.Count, "Hashtable");
        foreach (DictionaryEntry entry in serObject)
        {
            Serialize(dout, entry.Key, true);
            Serialize(dout, entry.Value, true);
        }
    }

    private void SerializeDictionary(StreamBuffer dout, IDictionary serObject, bool setType)
    {
        if (setType) dout.WriteByte(68);
        SerializeDictionaryHeader(dout, serObject, out bool setKeyType, out bool setValueType);
        SerializeDictionaryElements(dout, serObject, setKeyType, setValueType);
    }

    private void SerializeDictionaryHeader(StreamBuffer writer, object dict, out bool setKeyType, out bool setValueType)
    {
        Type[] genericArguments = dict.GetType().GetGenericArguments();
        setKeyType = genericArguments[0] == typeof(object);
        setValueType = genericArguments[1] == typeof(object);

        writer.WriteByte(setKeyType ? (byte)0 : (byte)GetCodeOfType(genericArguments[0]));
        writer.WriteByte(setValueType ? (byte)0 : (byte)GetCodeOfType(genericArguments[1]));

        if (!setValueType && GetCodeOfType(genericArguments[1]) == GpType.Dictionary)
        {
            SerializeDictionaryHeader(writer, genericArguments[1]);
        }
    }

    private void SerializeDictionaryHeader(StreamBuffer writer, Type dictType)
    {
        SerializeDictionaryHeader(writer, (object)dictType, out bool _, out bool _);
    }

    private void SerializeDictionaryElements(StreamBuffer writer, IDictionary dictionary, bool setKeyType, bool setValueType)
    {
        SerializeLengthAsShort(writer, dictionary.Count, "Dictionary elements");
        foreach (DictionaryEntry entry in dictionary)
        {
            if (!setKeyType && entry.Key == null) throw new Exception("Can't serialize null in Dictionary with specific key-type.");
            if (!setValueType && entry.Value == null) throw new Exception("Can't serialize null in Dictionary with specific value-type.");
            Serialize(writer, entry.Key, setKeyType);
            Serialize(writer, entry.Value, setValueType);
        }
    }

    public override object? Deserialize(StreamBuffer din, byte type, DeserializationFlags flags = DeserializationFlags.None)
    {
        return type switch
        {
            0 or 42 => null,
            68 => DeserializeDictionary(din),
            97 => DeserializeStringArray(din),
            98 => DeserializeByte(din),
            99 => DeserializeCustom(din, din.ReadByte()),
            100 => DeserializeDouble(din),
            101 => DeserializeEventData(din, null, DeserializationFlags.None),
            102 => DeserializeFloat(din),
            104 => DeserializeHashTable(din),
            105 => DeserializeInteger(din),
            107 => DeserializeShort(din),
            108 => DeserializeLong(din),
            110 => DeserializeIntArray(din),
            111 => DeserializeBoolean(din),
            112 => DeserializeOperationResponse(din, flags),
            113 => DeserializeOperationRequest(din, flags),
            115 => DeserializeString(din),
            120 => DeserializeByteArray(din),
            121 => DeserializeArray(din),
            122 => DeserializeObjectArray(din),
            _ => throw new Exception($"Deserialize(): {type} pos: {din.Position} bytes: {din.Length}. {BitConverter.ToString(din.GetBuffer())}")
        };
    }

    public override byte DeserializeByte(StreamBuffer din) => din.ReadByte();

    private bool DeserializeBoolean(StreamBuffer din) => din.ReadByte() > 0;

    public override short DeserializeShort(StreamBuffer din)
    {
        lock (memShort)
        {
            din.Read(memShort, 0, 2);
            return (short)((memShort[0] << 8) | memShort[1]);
        }
    }

    private int DeserializeInteger(StreamBuffer din)
    {
        lock (memInteger)
        {
            din.Read(memInteger, 0, 4);
            return (memInteger[0] << 24) | (memInteger[1] << 16) | (memInteger[2] << 8) | memInteger[3];
        }
    }

    private long DeserializeLong(StreamBuffer din)
    {
        lock (memLong)
        {
            din.Read(memLong, 0, 8);
            if (BitConverter.IsLittleEndian) Array.Reverse(memLong);
            return BitConverter.ToInt64(memLong, 0);
        }
    }

    private float DeserializeFloat(StreamBuffer din)
    {
        lock (memFloat)
        {
            din.Read(memFloat, 0, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(memFloat);
            return BitConverter.ToSingle(memFloat, 0);
        }
    }

    private double DeserializeDouble(StreamBuffer din)
    {
        lock (memDouble)
        {
            din.Read(memDouble, 0, 8);
            if (BitConverter.IsLittleEndian) Array.Reverse(memDouble);
            return BitConverter.ToDouble(memDouble, 0);
        }
    }

    private string DeserializeString(StreamBuffer din)
    {
        short length = DeserializeShort(din);
        if (length == 0) return string.Empty;
        if (length < 0) throw new NotSupportedException($"Received string type with unsupported length: {length}");
        int offset = 0;
        return Encoding.UTF8.GetString(din.GetBufferAndAdvance(length, out offset), offset, length);
    }

    private Array DeserializeArray(StreamBuffer din)
    {
        short length = DeserializeShort(din);
        byte typeCode = din.ReadByte();

        return typeCode switch
        {
            68 => DeserializeDictionaryArray(din, length, out Array arrayResult) ? arrayResult : throw new Exception("Failed to deserialize dictionary array"),
            98 => DeserializeByteArray(din, length),
            99 => DeserializeCustomArray(din, length, din.ReadByte()),
            105 => DeserializeIntArray(din, length),
            120 => DeserializeByteArrayOfArrays(din, length),
            121 => DeserializeNestedArray(din, length),
            _ => DeserializeSimpleArray(din, typeCode, length)
        };
    }

    private Array DeserializeCustomArray(StreamBuffer din, short length, byte customTypeCode)
    {
        if (!Protocol.CodeDict.TryGetValue(customTypeCode, out CustomType? customType))
            throw new Exception($"Cannot find deserializer for custom type: {customTypeCode}");

        Array array = Array.CreateInstance(customType.Type, length);
        for (int i = 0; i < length; i++)
        {
            short len = DeserializeShort(din);
            if (len < 0) throw new InvalidDataException($"DeserializeArray read negative objLength value: {len} before position: {din.Position}");
            if (customType.DeserializeStreamFunction == null)
            {
                byte[] buffer = new byte[len];
                din.Read(buffer, 0, len);
                array.SetValue(customType.DeserializeFunction!(buffer), i);
            }
            else
            {
                int position = din.Position;
                object obj = customType.DeserializeStreamFunction(din, len);
                if (din.Position - position != len)
                    din.Position = position + len;
                array.SetValue(obj, i);
            }
        }
        return array;
    }

    private Array DeserializeByteArrayOfArrays(StreamBuffer din, short length)
    {
        Array array = Array.CreateInstance(typeof(byte[]), length);
        for (int i = 0; i < length; i++)
        {
            array.SetValue(DeserializeByteArray(din), i);
        }
        return array;
    }

    private Array DeserializeNestedArray(StreamBuffer din, short length)
    {
        Array array = Array.CreateInstance(typeof(Array), length);
        for (int i = 0; i < length; i++)
        {
            array.SetValue(DeserializeArray(din), i);
        }
        return array;
    }

    private Array DeserializeSimpleArray(StreamBuffer din, byte typeCode, short length)
    {
        Array array = CreateArrayByType(typeCode, length);
        for (int i = 0; i < length; i++)
        {
            array.SetValue(Deserialize(din, typeCode, IProtocol.DeserializationFlags.None), i);
        }
        return array;
    }

    private byte[] DeserializeByteArray(StreamBuffer din, int size = -1)
    {
        if (size == -1) size = DeserializeInteger(din);
        byte[] buffer = new byte[size];
        din.Read(buffer, 0, size);
        return buffer;
    }

    private int[] DeserializeIntArray(StreamBuffer din, int size = -1)
    {
        if (size == -1) size = DeserializeInteger(din);
        int[] array = new int[size];
        for (int i = 0; i < size; i++)
        {
            array[i] = DeserializeInteger(din);
        }
        return array;
    }

    private string[] DeserializeStringArray(StreamBuffer din)
    {
        int length = DeserializeShort(din);
        string[] array = new string[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = DeserializeString(din);
        }
        return array;
    }

    private object?[] DeserializeObjectArray(StreamBuffer din)
    {
        short length = DeserializeShort(din);
        object?[] array = new object[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = Deserialize(din, din.ReadByte(), DeserializationFlags.None);
        }
        return array;
    }

    private Hashtable DeserializeHashTable(StreamBuffer din)
    {
        int count = DeserializeShort(din);
        Hashtable hashtable = new Hashtable(count);
        for (int i = 0; i < count; i++)
        {
            object? key = Deserialize(din, din.ReadByte(), DeserializationFlags.None);
            object? value = Deserialize(din, din.ReadByte(), DeserializationFlags.None);
            if (key == null)
                continue;
            if (key != null)
            {
                hashtable[key] = value;
            }
        }
        return hashtable;
    }

    private IDictionary? DeserializeDictionary(StreamBuffer din)
    {
        byte keyTypeCode = din.ReadByte();
        byte valueTypeCode = din.ReadByte();
        if (keyTypeCode == 68 || keyTypeCode == 121)
            throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary keys.");
        if (valueTypeCode == 68 || valueTypeCode == 121)
            throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary values.");

        int count = DeserializeShort(din);
        bool dynamicKeyType = keyTypeCode == 0 || keyTypeCode == 42;
        bool dynamicValueType = valueTypeCode == 0 || valueTypeCode == 42;

        IDictionary? dictionary = (IDictionary?)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(GetTypeOfCode(keyTypeCode), GetTypeOfCode(valueTypeCode)));
        for (int i = 0; i < count; i++)
        {
            object? key = Deserialize(din, dynamicKeyType ? din.ReadByte() : keyTypeCode, DeserializationFlags.None);
            object? value = Deserialize(din, dynamicValueType ? din.ReadByte() : valueTypeCode, DeserializationFlags.None);
            if (key == null)
                continue;
            if (key != null)
            {
                dictionary?.Add(key, value);
            }
        }
        return dictionary;
    }

    private bool DeserializeDictionaryArray(StreamBuffer din, short size, out Array arrayResult)
    {
        byte keyTypeCode, valueTypeCode;
        Type dictType = DeserializeDictionaryType(din, out keyTypeCode, out valueTypeCode);
        arrayResult = Array.CreateInstance(dictType, size);

        for (int i = 0; i < size; i++)
        {
            if (!(Activator.CreateInstance(dictType) is IDictionary dictionary))
                return false;

            short count = DeserializeShort(din);
            for (int j = 0; j < count; j++)
            {
                object? key = keyTypeCode > 0 ? Deserialize(din, keyTypeCode, DeserializationFlags.None) : Deserialize(din, din.ReadByte(), DeserializationFlags.None);
                object? value = valueTypeCode > 0 ? Deserialize(din, valueTypeCode, DeserializationFlags.None) : Deserialize(din, din.ReadByte(), DeserializationFlags.None);
                if (key != null)
                {
                    dictionary.Add(key, value);
                }
            }
            arrayResult.SetValue(dictionary, i);
        }
        return true;
    }

    private Type DeserializeDictionaryType(StreamBuffer reader, out byte keyTypeCode, out byte valueTypeCode)
    {
        keyTypeCode = reader.ReadByte();
        valueTypeCode = reader.ReadByte();
        Type keyType = keyTypeCode switch
        {
            0 => typeof(object),
            68 or 121 => throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary keys."),
            _ => GetTypeOfCode(keyTypeCode)
        };
        Type valueType = valueTypeCode switch
        {
            0 => typeof(object),
            68 or 121 => throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary values."),
            _ => GetTypeOfCode(valueTypeCode)
        };
        return typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
    }

    public enum GpType : byte
    {
        Unknown = 0,
        Null = 42, // 0x2A
        Dictionary = 68, // 0x44
        StringArray = 97, // 0x61
        Byte = 98, // 0x62
        Custom = 99, // 0x63
        Double = 100, // 0x64
        EventData = 101, // 0x65
        Float = 102, // 0x66
        Hashtable = 104, // 0x68
        Integer = 105, // 0x69
        Short = 107, // 0x6B
        Long = 108, // 0x6C
        IntegerArray = 110, // 0x6E
        Boolean = 111, // 0x6F
        OperationResponse = 112, // 0x70
        OperationRequest = 113, // 0x71
        String = 115, // 0x73
        ByteArray = 120, // 0x78
        Array = 121, // 0x79
        ObjectArray = 122, // 0x7A
    }
}