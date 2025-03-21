using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated.StructWrapping;
using FakePhotonLib.PhotonRelated;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakePhotonLib.Protocols;

public class Protocol16 : IProtocol
{
    private readonly byte[] versionBytes =
    [
      (byte) 1,
      (byte) 6
    ];
    private readonly byte[] memShort = new byte[2];
    private readonly long[] memLongBlock = new long[1];
    private readonly byte[] memLongBlockBytes = new byte[8];
    private static readonly float[] memFloatBlock = new float[1];
    private static readonly byte[] memFloatBlockBytes = new byte[4];
    private readonly double[] memDoubleBlock = new double[1];
    private readonly byte[] memDoubleBlockBytes = new byte[8];
    private readonly byte[] memInteger = new byte[4];
    private readonly byte[] memLong = new byte[8];
    private readonly byte[] memFloat = new byte[4];
    private readonly byte[] memDouble = new byte[8];

    public override string ProtocolType => "GpBinaryV16";

    public override byte[] VersionBytes => this.versionBytes;

    private bool SerializeCustom(StreamBuffer dout, object serObject)
    {
        Type key = serObject is StructWrapper structWrapper ? structWrapper.ttype : serObject.GetType();
        CustomType customType;
        if (!Protocol.TypeDict.TryGetValue(key, out customType))
            return false;
        if (customType.SerializeStreamFunction == null)
        {
            byte[] buffer = customType.SerializeFunction(serObject);
            dout.WriteByte((byte)99);
            dout.WriteByte(customType.Code);
            this.SerializeLengthAsShort(dout, buffer.Length, "Custom Type " + customType.Code.ToString());
            dout.Write(buffer, 0, buffer.Length);
            return true;
        }
        dout.WriteByte((byte)99);
        dout.WriteByte(customType.Code);
        int position1 = dout.Position;
        dout.Position += 2;
        short serObject1 = customType.SerializeStreamFunction(dout, serObject);
        long position2 = (long)dout.Position;
        dout.Position = position1;
        this.SerializeLengthAsShort(dout, (int)serObject1, "Custom Type " + customType.Code.ToString());
        dout.Position += (int)serObject1;
        if ((long)dout.Position != position2)
            throw new Exception("Serialization failed. Stream position corrupted. Should be " + position2.ToString() + " is now: " + dout.Position.ToString() + " serializedLength: " + serObject1.ToString());
        return true;
    }

    private object DeserializeCustom(
      StreamBuffer din,
      byte customTypeCode,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        short length = this.DeserializeShort(din);
        if (length < (short)0)
            throw new InvalidDataException("DeserializeCustom read negative length value: " + length.ToString() + " before position: " + din.Position.ToString());
        CustomType customType;
        if ((int)length <= din.Available && Protocol.CodeDict.TryGetValue(customTypeCode, out customType))
        {
            if (customType.DeserializeStreamFunction == null)
            {
                byte[] numArray = new byte[(int)length];
                din.Read(numArray, 0, (int)length);
                return customType.DeserializeFunction(numArray);
            }
            int position = din.Position;
            object obj = customType.DeserializeStreamFunction(din, length);
            if (din.Position - position != (int)length)
                din.Position = position + (int)length;
            return obj;
        }
        int count = (int)length <= din.Available ? (int)length : (int)(short)din.Available;
        byte[] buffer = new byte[count];
        din.Read(buffer, 0, count);
        return (object)buffer;
    }

    private Type GetTypeOfCode(byte typeCode)
    {
        switch (typeCode)
        {
            case 0:
            case 42:
                return typeof(object);
            case 68:
                return typeof(IDictionary);
            case 97:
                return typeof(string[]);
            case 98:
                return typeof(byte);
            case 99:
                return typeof(CustomType);
            case 100:
                return typeof(double);
            case 101:
                return typeof(EventData);
            case 102:
                return typeof(float);
            case 104:
                return typeof(Hashtable);
            case 105:
                return typeof(int);
            case 107:
                return typeof(short);
            case 108:
                return typeof(long);
            case 110:
                return typeof(int[]);
            case 111:
                return typeof(bool);
            case 112:
                return typeof(OperationResponse);
            case 113:
                return typeof(OperationRequest);
            case 115:
                return typeof(string);
            case 120:
                return typeof(byte[]);
            case 121:
                return typeof(Array);
            case 122:
                return typeof(object[]);
            default:
                Debug.WriteLine("missing type: " + typeCode.ToString());
                throw new Exception("deserialize(): " + typeCode.ToString());
        }
    }

    private Protocol16.GpType GetCodeOfType(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return Protocol16.GpType.Boolean;
            case TypeCode.Byte:
                return Protocol16.GpType.Byte;
            case TypeCode.Int16:
                return Protocol16.GpType.Short;
            case TypeCode.Int32:
                return Protocol16.GpType.Integer;
            case TypeCode.Int64:
                return Protocol16.GpType.Long;
            case TypeCode.Single:
                return Protocol16.GpType.Float;
            case TypeCode.Double:
                return Protocol16.GpType.Double;
            case TypeCode.String:
                return Protocol16.GpType.String;
            default:
                if (type.IsArray)
                    return type == typeof(byte[]) ? Protocol16.GpType.ByteArray : Protocol16.GpType.Array;
                if (type == typeof(Hashtable))
                    return Protocol16.GpType.Hashtable;
                if (type == typeof(List<object>))
                    return Protocol16.GpType.ObjectArray;
                if (type.IsGenericType && typeof(Dictionary<,>) == type.GetGenericTypeDefinition())
                    return Protocol16.GpType.Dictionary;
                if (type == typeof(EventData))
                    return Protocol16.GpType.EventData;
                if (type == typeof(OperationRequest))
                    return Protocol16.GpType.OperationRequest;
                return type == typeof(OperationResponse) ? Protocol16.GpType.OperationResponse : Protocol16.GpType.Unknown;
        }
    }

    private Array CreateArrayByType(byte arrayType, short length)
    {
        return Array.CreateInstance(this.GetTypeOfCode(arrayType), (int)length);
    }

    public void SerializeOperationRequest(
      StreamBuffer stream,
      OperationRequest operation,
      bool setType)
    {
        this.SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
    }

    public override void SerializeOperationRequest(
      StreamBuffer stream,
      byte operationCode,
      Dictionary<byte, object> parameters,
      bool setType)
    {
        if (setType)
            stream.WriteByte((byte)113);
        stream.WriteByte(operationCode);
        this.SerializeParameterTable(stream, parameters);
    }

    public override OperationRequest DeserializeOperationRequest(
      StreamBuffer din,
      IProtocol.DeserializationFlags flags)
    {
        OperationRequest operationRequest = new OperationRequest()
        {
            OperationCode = this.DeserializeByte(din)
        };
        operationRequest.Parameters = this.DeserializeParameterTable(din, operationRequest.Parameters, flags);
        return operationRequest;
    }

    public override void SerializeOperationResponse(
      StreamBuffer stream,
      OperationResponse serObject,
      bool setType)
    {
        if (setType)
            stream.WriteByte((byte)112);
        stream.WriteByte(serObject.OperationCode);
        this.SerializeShort(stream, serObject.ReturnCode, false);
        if (string.IsNullOrEmpty(serObject.DebugMessage))
            stream.WriteByte((byte)42);
        else
            this.SerializeString(stream, serObject.DebugMessage, false);
        this.SerializeParameterTable(stream, serObject.Parameters);
    }

    public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
    {
        return new DisconnectMessage()
        {
            Code = this.DeserializeShort(stream),
            DebugMessage = this.Deserialize(stream, this.DeserializeByte(stream), IProtocol.DeserializationFlags.None) as string,
            Parameters = this.DeserializeParameterTable(stream)
        };
    }

    public override OperationResponse DeserializeOperationResponse(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        return new OperationResponse()
        {
            OperationCode = this.DeserializeByte(stream),
            ReturnCode = this.DeserializeShort(stream),
            DebugMessage = this.Deserialize(stream, this.DeserializeByte(stream), IProtocol.DeserializationFlags.None) as string,
            Parameters = this.DeserializeParameterTable(stream)
        };
    }

    public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)101);
        stream.WriteByte(serObject.Code);
        this.SerializeParameterTable(stream, serObject.Parameters);
    }

    public override EventData DeserializeEventData(
      StreamBuffer din,
      EventData target = null,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        EventData eventData;
        if (target != null)
        {
            target.Reset();
            eventData = target;
        }
        else
            eventData = new EventData();
        eventData.Code = this.DeserializeByte(din);
        this.DeserializeParameterTable(din, eventData.Parameters);
        return eventData;
    }

    private void SerializeParameterTable(StreamBuffer stream, Dictionary<byte, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            this.SerializeShort(stream, (short)0, false);
        }
        else
        {
            this.SerializeLengthAsShort(stream, parameters.Count, "ParameterTable");
            foreach (KeyValuePair<byte, object> parameter in parameters)
            {
                stream.WriteByte(parameter.Key);
                this.Serialize(stream, parameter.Value, true);
            }
        }
    }

    private Dictionary<byte, object> DeserializeParameterTable(
      StreamBuffer stream,
      Dictionary<byte, object> target = null,
      IProtocol.DeserializationFlags flag = DeserializationFlags.None)
    {
        short capacity = this.DeserializeShort(stream);
        Dictionary<byte, object> dictionary = target != null ? target : new Dictionary<byte, object>((int)capacity);
        for (int index = 0; index < (int)capacity; ++index)
        {
            byte key = stream.ReadByte();
            object obj = this.Deserialize(stream, stream.ReadByte(), flag);
            dictionary[key] = obj;
        }
        return dictionary;
    }

    public override void Serialize(StreamBuffer dout, object serObject, bool setType)
    {
        if (serObject == null)
        {
            if (!setType)
                return;
            dout.WriteByte((byte)42);
        }
        else
        {
            Type type = serObject is StructWrapper structWrapper ? structWrapper.ttype : serObject.GetType();
            switch (this.GetCodeOfType(type))
            {
                case Protocol16.GpType.Dictionary:
                    this.SerializeDictionary(dout, (IDictionary)serObject, setType);
                    break;
                case Protocol16.GpType.Byte:
                    this.SerializeByte(dout, serObject.Get<byte>(), setType);
                    break;
                case Protocol16.GpType.Double:
                    this.SerializeDouble(dout, serObject.Get<double>(), setType);
                    break;
                case Protocol16.GpType.EventData:
                    this.SerializeEventData(dout, (EventData)serObject, setType);
                    break;
                case Protocol16.GpType.Float:
                    this.SerializeFloat(dout, serObject.Get<float>(), setType);
                    break;
                case Protocol16.GpType.Hashtable:
                    this.SerializeHashTable(dout, (Hashtable)serObject, setType);
                    break;
                case Protocol16.GpType.Integer:
                    this.SerializeInteger(dout, serObject.Get<int>(), setType);
                    break;
                case Protocol16.GpType.Short:
                    this.SerializeShort(dout, serObject.Get<short>(), setType);
                    break;
                case Protocol16.GpType.Long:
                    this.SerializeLong(dout, serObject.Get<long>(), setType);
                    break;
                case Protocol16.GpType.Boolean:
                    this.SerializeBoolean(dout, serObject.Get<bool>(), setType);
                    break;
                case Protocol16.GpType.OperationResponse:
                    this.SerializeOperationResponse(dout, (OperationResponse)serObject, setType);
                    break;
                case Protocol16.GpType.OperationRequest:
                    this.SerializeOperationRequest(dout, (OperationRequest)serObject, setType);
                    break;
                case Protocol16.GpType.String:
                    this.SerializeString(dout, (string)serObject, setType);
                    break;
                case Protocol16.GpType.ByteArray:
                    this.SerializeByteArray(dout, (byte[])serObject, setType);
                    break;
                case Protocol16.GpType.Array:
                    if (serObject is int[])
                    {
                        this.SerializeIntArrayOptimized(dout, (int[])serObject, setType);
                        break;
                    }
                    if (type.GetElementType() == typeof(object))
                    {
                        this.SerializeObjectArray(dout, (IList)(serObject as object[]), setType);
                        break;
                    }
                    this.SerializeArray(dout, (Array)serObject, setType);
                    break;
                case Protocol16.GpType.ObjectArray:
                    this.SerializeObjectArray(dout, (IList)serObject, setType);
                    break;
                default:
                    if (serObject is ArraySegment<byte> arraySegment)
                    {
                        this.SerializeByteArraySegment(dout, arraySegment.Array, arraySegment.Offset, arraySegment.Count, setType);
                        break;
                    }
                    if (this.SerializeCustom(dout, serObject))
                        break;
                    if (serObject is StructWrapper)
                        throw new Exception("cannot serialize(): StructWrapper<" + (serObject as StructWrapper).ttype.Name + ">");
                    throw new Exception("cannot serialize(): " + type?.ToString());
            }
        }
    }

    private void SerializeByte(StreamBuffer dout, byte serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)98);
        dout.WriteByte(serObject);
    }

    private void SerializeBoolean(StreamBuffer dout, bool serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)111);
        dout.WriteByte(serObject ? (byte)1 : (byte)0);
    }

    public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)107);
        lock (this.memShort)
        {
            byte[] memShort = this.memShort;
            memShort[0] = (byte)((uint)serObject >> 8);
            memShort[1] = (byte)serObject;
            dout.Write(memShort, 0, 2);
        }
    }

    public void SerializeLengthAsShort(StreamBuffer dout, int serObject, string type)
    {
        if (serObject > (int)short.MaxValue || serObject < 0)
            throw new NotSupportedException(string.Format("Exceeding 32767 (short.MaxValue) entries are not supported. Failed writing {0}. Length: {1}", (object)type, (object)serObject));
        lock (this.memShort)
        {
            byte[] memShort = this.memShort;
            memShort[0] = (byte)(serObject >> 8);
            memShort[1] = (byte)serObject;
            dout.Write(memShort, 0, 2);
        }
    }

    private void SerializeInteger(StreamBuffer dout, int serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)105);
        lock (this.memInteger)
        {
            byte[] memInteger = this.memInteger;
            memInteger[0] = (byte)(serObject >> 24);
            memInteger[1] = (byte)(serObject >> 16);
            memInteger[2] = (byte)(serObject >> 8);
            memInteger[3] = (byte)serObject;
            dout.Write(memInteger, 0, 4);
        }
    }

    private void SerializeLong(StreamBuffer dout, long serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)108);
        lock (this.memLongBlock)
        {
            this.memLongBlock[0] = serObject;
            Buffer.BlockCopy((Array)this.memLongBlock, 0, (Array)this.memLongBlockBytes, 0, 8);
            byte[] memLongBlockBytes = this.memLongBlockBytes;
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = memLongBlockBytes[0];
                byte num2 = memLongBlockBytes[1];
                byte num3 = memLongBlockBytes[2];
                byte num4 = memLongBlockBytes[3];
                memLongBlockBytes[0] = memLongBlockBytes[7];
                memLongBlockBytes[1] = memLongBlockBytes[6];
                memLongBlockBytes[2] = memLongBlockBytes[5];
                memLongBlockBytes[3] = memLongBlockBytes[4];
                memLongBlockBytes[4] = num4;
                memLongBlockBytes[5] = num3;
                memLongBlockBytes[6] = num2;
                memLongBlockBytes[7] = num1;
            }
            dout.Write(memLongBlockBytes, 0, 8);
        }
    }

    private void SerializeFloat(StreamBuffer dout, float serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)102);
        lock (Protocol16.memFloatBlockBytes)
        {
            Protocol16.memFloatBlock[0] = serObject;
            Buffer.BlockCopy((Array)Protocol16.memFloatBlock, 0, (Array)Protocol16.memFloatBlockBytes, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                byte memFloatBlockByte1 = Protocol16.memFloatBlockBytes[0];
                byte memFloatBlockByte2 = Protocol16.memFloatBlockBytes[1];
                Protocol16.memFloatBlockBytes[0] = Protocol16.memFloatBlockBytes[3];
                Protocol16.memFloatBlockBytes[1] = Protocol16.memFloatBlockBytes[2];
                Protocol16.memFloatBlockBytes[2] = memFloatBlockByte2;
                Protocol16.memFloatBlockBytes[3] = memFloatBlockByte1;
            }
            dout.Write(Protocol16.memFloatBlockBytes, 0, 4);
        }
    }

    private void SerializeDouble(StreamBuffer dout, double serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)100);
        lock (this.memDoubleBlockBytes)
        {
            this.memDoubleBlock[0] = serObject;
            Buffer.BlockCopy((Array)this.memDoubleBlock, 0, (Array)this.memDoubleBlockBytes, 0, 8);
            byte[] doubleBlockBytes = this.memDoubleBlockBytes;
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = doubleBlockBytes[0];
                byte num2 = doubleBlockBytes[1];
                byte num3 = doubleBlockBytes[2];
                byte num4 = doubleBlockBytes[3];
                doubleBlockBytes[0] = doubleBlockBytes[7];
                doubleBlockBytes[1] = doubleBlockBytes[6];
                doubleBlockBytes[2] = doubleBlockBytes[5];
                doubleBlockBytes[3] = doubleBlockBytes[4];
                doubleBlockBytes[4] = num4;
                doubleBlockBytes[5] = num3;
                doubleBlockBytes[6] = num2;
                doubleBlockBytes[7] = num1;
            }
            dout.Write(doubleBlockBytes, 0, 8);
        }
    }

    public override void SerializeString(StreamBuffer stream, string value, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)115);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > (int)short.MaxValue)
            throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + byteCount.ToString());
        this.SerializeLengthAsShort(stream, byteCount, "String");
        int offset = 0;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(byteCount, out offset);
        Encoding.UTF8.GetBytes(value, 0, value.Length, bufferAndAdvance, offset);
    }

    private void SerializeArray(StreamBuffer dout, Array serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)121);
        this.SerializeLengthAsShort(dout, serObject.Length, "Array");
        Type elementType = serObject.GetType().GetElementType();
        Protocol16.GpType codeOfType = this.GetCodeOfType(elementType);
        if (codeOfType != 0)
        {
            dout.WriteByte((byte)codeOfType);
            if (codeOfType == Protocol16.GpType.Dictionary)
            {
                bool setKeyType;
                bool setValueType;
                this.SerializeDictionaryHeader(dout, (object)serObject, out setKeyType, out setValueType);
                for (int index = 0; index < serObject.Length; ++index)
                {
                    object dict = serObject.GetValue(index);
                    this.SerializeDictionaryElements(dout, dict, setKeyType, setValueType);
                }
            }
            else
            {
                for (int index = 0; index < serObject.Length; ++index)
                {
                    object serObject1 = serObject.GetValue(index);
                    this.Serialize(dout, serObject1, false);
                }
            }
        }
        else
        {
            CustomType customType;
            if (!Protocol.TypeDict.TryGetValue(elementType, out customType))
                throw new NotSupportedException("cannot serialize array of type " + elementType?.ToString());
            dout.WriteByte((byte)99);
            dout.WriteByte(customType.Code);
            for (int index = 0; index < serObject.Length; ++index)
            {
                object customObject = serObject.GetValue(index);
                byte code;
                if (customType.SerializeStreamFunction == null)
                {
                    byte[] buffer = customType.SerializeFunction(customObject);
                    StreamBuffer dout1 = dout;
                    int length = buffer.Length;
                    code = customType.Code;
                    string type = "Custom Type " + code.ToString();
                    this.SerializeLengthAsShort(dout1, length, type);
                    dout.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    int position1 = dout.Position;
                    dout.Position += 2;
                    short num = customType.SerializeStreamFunction(dout, customObject);
                    long position2 = (long)dout.Position;
                    dout.Position = position1;
                    StreamBuffer dout2 = dout;
                    int serObject2 = (int)num;
                    code = customType.Code;
                    string type = "Custom Type " + code.ToString();
                    this.SerializeLengthAsShort(dout2, serObject2, type);
                    dout.Position += (int)num;
                    if ((long)dout.Position != position2)
                        throw new Exception("Serialization failed. Stream position corrupted. Should be " + position2.ToString() + " is now: " + dout.Position.ToString() + " serializedLength: " + num.ToString());
                }
            }
        }
    }

    private void SerializeByteArray(StreamBuffer dout, byte[] serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)120);
        this.SerializeInteger(dout, serObject.Length, false);
        dout.Write(serObject, 0, serObject.Length);
    }

    private void SerializeByteArraySegment(
      StreamBuffer dout,
      byte[] serObject,
      int offset,
      int count,
      bool setType)
    {
        if (setType)
            dout.WriteByte((byte)120);
        this.SerializeInteger(dout, count, false);
        dout.Write(serObject, offset, count);
    }

    private void SerializeIntArrayOptimized(StreamBuffer inWriter, int[] serObject, bool setType)
    {
        if (setType)
            inWriter.WriteByte((byte)121);
        this.SerializeLengthAsShort(inWriter, serObject.Length, "int[]");
        inWriter.WriteByte((byte)105);
        byte[] buffer = new byte[serObject.Length * 4];
        int num1 = 0;
        for (int index1 = 0; index1 < serObject.Length; ++index1)
        {
            byte[] numArray1 = buffer;
            int index2 = num1;
            int num2 = index2 + 1;
            int num3 = (int)(byte)(serObject[index1] >> 24);
            numArray1[index2] = (byte)num3;
            byte[] numArray2 = buffer;
            int index3 = num2;
            int num4 = index3 + 1;
            int num5 = (int)(byte)(serObject[index1] >> 16);
            numArray2[index3] = (byte)num5;
            byte[] numArray3 = buffer;
            int index4 = num4;
            int num6 = index4 + 1;
            int num7 = (int)(byte)(serObject[index1] >> 8);
            numArray3[index4] = (byte)num7;
            byte[] numArray4 = buffer;
            int index5 = num6;
            num1 = index5 + 1;
            int num8 = (int)(byte)serObject[index1];
            numArray4[index5] = (byte)num8;
        }
        inWriter.Write(buffer, 0, buffer.Length);
    }

    private void SerializeStringArray(StreamBuffer dout, string[] serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)97);
        this.SerializeLengthAsShort(dout, serObject.Length, "string[]");
        for (int index = 0; index < serObject.Length; ++index)
            this.SerializeString(dout, serObject[index], false);
    }

    private void SerializeObjectArray(StreamBuffer dout, IList objects, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)122);
        this.SerializeLengthAsShort(dout, objects.Count, "object[]");
        for (int index = 0; index < objects.Count; ++index)
        {
            object serObject = objects[index];
            this.Serialize(dout, serObject, true);
        }
    }

    private void SerializeHashTable(StreamBuffer dout, Hashtable serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)104);
        this.SerializeLengthAsShort(dout, serObject.Count, "Hashtable");
        foreach (object key in serObject.Keys)
        {
            this.Serialize(dout, key, true);
            this.Serialize(dout, serObject[key], true);
        }
    }

    private void SerializeDictionary(StreamBuffer dout, IDictionary serObject, bool setType)
    {
        if (setType)
            dout.WriteByte((byte)68);
        bool setKeyType;
        bool setValueType;
        this.SerializeDictionaryHeader(dout, (object)serObject, out setKeyType, out setValueType);
        this.SerializeDictionaryElements(dout, (object)serObject, setKeyType, setValueType);
    }

    private void SerializeDictionaryHeader(StreamBuffer writer, Type dictType)
    {
        this.SerializeDictionaryHeader(writer, (object)dictType, out bool _, out bool _);
    }

    private void SerializeDictionaryHeader(
      StreamBuffer writer,
      object dict,
      out bool setKeyType,
      out bool setValueType)
    {
        Type[] genericArguments = dict.GetType().GetGenericArguments();
        setKeyType = genericArguments[0] == typeof(object);
        setValueType = genericArguments[1] == typeof(object);
        if (setKeyType)
        {
            writer.WriteByte((byte)0);
        }
        else
        {
            Protocol16.GpType codeOfType = this.GetCodeOfType(genericArguments[0]);
            if (codeOfType == Protocol16.GpType.Unknown || codeOfType == Protocol16.GpType.Dictionary)
                throw new Exception("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
            writer.WriteByte((byte)codeOfType);
        }
        if (setValueType)
        {
            writer.WriteByte((byte)0);
        }
        else
        {
            Protocol16.GpType codeOfType = this.GetCodeOfType(genericArguments[1]);
            if (codeOfType == Protocol16.GpType.Unknown)
                throw new Exception("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            writer.WriteByte((byte)codeOfType);
            if (codeOfType == Protocol16.GpType.Dictionary)
                this.SerializeDictionaryHeader(writer, genericArguments[1]);
        }
    }

    private void SerializeDictionaryElements(
      StreamBuffer writer,
      object dict,
      bool setKeyType,
      bool setValueType)
    {
        IDictionary dictionary = (IDictionary)dict;
        this.SerializeLengthAsShort(writer, dictionary.Count, "Dictionary elements");
        foreach (DictionaryEntry dictionaryEntry in dictionary)
        {
            if (!setValueType && dictionaryEntry.Value == null)
                throw new Exception("Can't serialize null in Dictionary with specific value-type.");
            if (!setKeyType && dictionaryEntry.Key == null)
                throw new Exception("Can't serialize null in Dictionary with specific key-type.");
            this.Serialize(writer, dictionaryEntry.Key, setKeyType);
            this.Serialize(writer, dictionaryEntry.Value, setValueType);
        }
    }

    public override object Deserialize(
      StreamBuffer din,
      byte type,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        switch (type)
        {
            case 0:
            case 42:
                return (object)null;
            case 68:
                return (object)this.DeserializeDictionary(din);
            case 97:
                return (object)this.DeserializeStringArray(din);
            case 98:
                return (object)this.DeserializeByte(din);
            case 99:
                byte customTypeCode = din.ReadByte();
                return this.DeserializeCustom(din, customTypeCode);
            case 100:
                return (object)this.DeserializeDouble(din);
            case 101:
                return (object)this.DeserializeEventData(din, (EventData)null, IProtocol.DeserializationFlags.None);
            case 102:
                return (object)this.DeserializeFloat(din);
            case 104:
                return (object)this.DeserializeHashTable(din);
            case 105:
                return (object)this.DeserializeInteger(din);
            case 107:
                return (object)this.DeserializeShort(din);
            case 108:
                return (object)this.DeserializeLong(din);
            case 110:
                return (object)this.DeserializeIntArray(din);
            case 111:
                return (object)this.DeserializeBoolean(din);
            case 112:
                return (object)this.DeserializeOperationResponse(din, flags);
            case 113:
                return (object)this.DeserializeOperationRequest(din, flags);
            case 115:
                return (object)this.DeserializeString(din);
            case 120:
                return (object)this.DeserializeByteArray(din);
            case 121:
                return (object)this.DeserializeArray(din);
            case 122:
                return (object)this.DeserializeObjectArray(din);
            default:
                throw new Exception("Deserialize(): " + type.ToString() + " pos: " + din.Position.ToString() + " bytes: " + din.Length.ToString() + ". " + BitConverter.ToString(din.GetBuffer()));
        }
    }

    public override byte DeserializeByte(StreamBuffer din) => din.ReadByte();

    private bool DeserializeBoolean(StreamBuffer din) => din.ReadByte() > (byte)0;

    public override short DeserializeShort(StreamBuffer din)
    {
        lock (this.memShort)
        {
            byte[] memShort = this.memShort;
            din.Read(memShort, 0, 2);
            return (short)((int)memShort[0] << 8 | (int)memShort[1]);
        }
    }

    private int DeserializeInteger(StreamBuffer din)
    {
        lock (this.memInteger)
        {
            byte[] memInteger = this.memInteger;
            din.Read(memInteger, 0, 4);
            return (int)memInteger[0] << 24 | (int)memInteger[1] << 16 | (int)memInteger[2] << 8 | (int)memInteger[3];
        }
    }

    private long DeserializeLong(StreamBuffer din)
    {
        lock (this.memLong)
        {
            byte[] memLong = this.memLong;
            din.Read(memLong, 0, 8);
            return BitConverter.IsLittleEndian ? (long)memLong[0] << 56 | (long)memLong[1] << 48 | (long)memLong[2] << 40 | (long)memLong[3] << 32 | (long)memLong[4] << 24 | (long)memLong[5] << 16 | (long)memLong[6] << 8 | (long)memLong[7] : BitConverter.ToInt64(memLong, 0);
        }
    }

    private float DeserializeFloat(StreamBuffer din)
    {
        lock (this.memFloat)
        {
            byte[] memFloat = this.memFloat;
            din.Read(memFloat, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = memFloat[0];
                byte num2 = memFloat[1];
                memFloat[0] = memFloat[3];
                memFloat[1] = memFloat[2];
                memFloat[2] = num2;
                memFloat[3] = num1;
            }
            return BitConverter.ToSingle(memFloat, 0);
        }
    }

    private double DeserializeDouble(StreamBuffer din)
    {
        lock (this.memDouble)
        {
            byte[] memDouble = this.memDouble;
            din.Read(memDouble, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = memDouble[0];
                byte num2 = memDouble[1];
                byte num3 = memDouble[2];
                byte num4 = memDouble[3];
                memDouble[0] = memDouble[7];
                memDouble[1] = memDouble[6];
                memDouble[2] = memDouble[5];
                memDouble[3] = memDouble[4];
                memDouble[4] = num4;
                memDouble[5] = num3;
                memDouble[6] = num2;
                memDouble[7] = num1;
            }
            return BitConverter.ToDouble(memDouble, 0);
        }
    }

    private string DeserializeString(StreamBuffer din)
    {
        short num = this.DeserializeShort(din);
        if (num == (short)0)
            return string.Empty;
        if (num < (short)0)
            throw new NotSupportedException("Received string type with unsupported length: " + num.ToString());
        int offset = 0;
        return Encoding.UTF8.GetString(din.GetBufferAndAdvance((int)num, out offset), offset, (int)num);
    }

    private Array DeserializeArray(StreamBuffer din)
    {
        short num1 = this.DeserializeShort(din);
        byte num2 = din.ReadByte();
        Array array1;
        switch (num2)
        {
            case 68:
                Array arrayResult = (Array)null;
                this.DeserializeDictionaryArray(din, num1, out arrayResult);
                return arrayResult;
            case 98:
                array1 = (Array)this.DeserializeByteArray(din, (int)num1);
                break;
            case 99:
                byte key = din.ReadByte();
                CustomType customType;
                if (!Protocol.CodeDict.TryGetValue(key, out customType))
                    throw new Exception("Cannot find deserializer for custom type: " + key.ToString());
                array1 = Array.CreateInstance(customType.Type, (int)num1);
                for (int index = 0; index < (int)num1; ++index)
                {
                    short length = this.DeserializeShort(din);
                    if (length < (short)0)
                        throw new InvalidDataException("DeserializeArray read negative objLength value: " + length.ToString() + " before position: " + din.Position.ToString());
                    if (customType.DeserializeStreamFunction == null)
                    {
                        byte[] numArray = new byte[(int)length];
                        din.Read(numArray, 0, (int)length);
                        array1.SetValue(customType.DeserializeFunction(numArray), index);
                    }
                    else
                    {
                        int position = din.Position;
                        object obj = customType.DeserializeStreamFunction(din, length);
                        if (din.Position - position != (int)length)
                            din.Position = position + (int)length;
                        array1.SetValue(obj, index);
                    }
                }
                break;
            case 105:
                array1 = (Array)this.DeserializeIntArray(din, (int)num1);
                break;
            case 120:
                array1 = Array.CreateInstance(typeof(byte[]), (int)num1);
                for (short index = 0; (int)index < (int)num1; ++index)
                {
                    Array array2 = (Array)this.DeserializeByteArray(din);
                    array1.SetValue((object)array2, (int)index);
                }
                break;
            case 121:
                Array array3 = this.DeserializeArray(din);
                array1 = Array.CreateInstance(array3.GetType(), (int)num1);
                array1.SetValue((object)array3, 0);
                for (short index = 1; (int)index < (int)num1; ++index)
                {
                    Array array4 = this.DeserializeArray(din);
                    array1.SetValue((object)array4, (int)index);
                }
                break;
            default:
                array1 = this.CreateArrayByType(num2, num1);
                for (short index = 0; (int)index < (int)num1; ++index)
                    array1.SetValue(this.Deserialize(din, num2, IProtocol.DeserializationFlags.None), (int)index);
                break;
        }
        return array1;
    }

    private byte[] DeserializeByteArray(StreamBuffer din, int size = -1)
    {
        if (size == -1)
            size = this.DeserializeInteger(din);
        byte[] buffer = new byte[size];
        din.Read(buffer, 0, size);
        return buffer;
    }

    private int[] DeserializeIntArray(StreamBuffer din, int size = -1)
    {
        if (size == -1)
            size = this.DeserializeInteger(din);
        int[] numArray = new int[size];
        for (int index = 0; index < size; ++index)
            numArray[index] = this.DeserializeInteger(din);
        return numArray;
    }

    private string[] DeserializeStringArray(StreamBuffer din)
    {
        int length = (int)this.DeserializeShort(din);
        string[] strArray = new string[length];
        for (int index = 0; index < length; ++index)
            strArray[index] = this.DeserializeString(din);
        return strArray;
    }

    private object[] DeserializeObjectArray(StreamBuffer din)
    {
        short length = this.DeserializeShort(din);
        object[] objArray = new object[(int)length];
        for (int index = 0; index < (int)length; ++index)
        {
            byte type = din.ReadByte();
            objArray[index] = this.Deserialize(din, type, IProtocol.DeserializationFlags.None);
        }
        return objArray;
    }

    private Hashtable DeserializeHashTable(StreamBuffer din)
    {
        int x = (int)this.DeserializeShort(din);
        Hashtable hashtable = new Hashtable(x);
        for (int index = 0; index < x; ++index)
        {
            object key = this.Deserialize(din, din.ReadByte(), IProtocol.DeserializationFlags.None);
            object obj = this.Deserialize(din, din.ReadByte(), IProtocol.DeserializationFlags.None);
            if (key != null)
                hashtable[key] = obj;
        }
        return hashtable;
    }

    private IDictionary DeserializeDictionary(StreamBuffer din)
    {
        byte typeCode1 = din.ReadByte();
        byte typeCode2 = din.ReadByte();
        if (typeCode1 == (byte)68 || typeCode1 == (byte)121)
            throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary keys.");
        if (typeCode2 == (byte)68 || typeCode2 == (byte)121)
            throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary values.");
        int num = (int)this.DeserializeShort(din);
        bool flag1 = typeCode1 == (byte)0 || typeCode1 == (byte)42;
        bool flag2 = typeCode2 == (byte)0 || typeCode2 == (byte)42;
        IDictionary instance = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(this.GetTypeOfCode(typeCode1), this.GetTypeOfCode(typeCode2))) as IDictionary;
        for (int index = 0; index < num; ++index)
        {
            object key = this.Deserialize(din, flag1 ? din.ReadByte() : typeCode1, IProtocol.DeserializationFlags.None);
            object obj = this.Deserialize(din, flag2 ? din.ReadByte() : typeCode2, IProtocol.DeserializationFlags.None);
            if (key != null)
                instance.Add(key, obj);
        }
        return instance;
    }

    private bool DeserializeDictionaryArray(StreamBuffer din, short size, out Array arrayResult)
    {
        byte keyTypeCode;
        byte valTypeCode;
        Type type1 = this.DeserializeDictionaryType(din, out keyTypeCode, out valTypeCode);
        arrayResult = Array.CreateInstance(type1, (int)size);
        for (short index1 = 0; (int)index1 < (int)size; ++index1)
        {
            if (!(Activator.CreateInstance(type1) is IDictionary instance))
                return false;
            short num = this.DeserializeShort(din);
            for (int index2 = 0; index2 < (int)num; ++index2)
            {
                object key;
                if (keyTypeCode > (byte)0)
                {
                    key = this.Deserialize(din, keyTypeCode, IProtocol.DeserializationFlags.None);
                }
                else
                {
                    byte type2 = din.ReadByte();
                    key = this.Deserialize(din, type2, IProtocol.DeserializationFlags.None);
                }
                object obj;
                if (valTypeCode > (byte)0)
                {
                    obj = this.Deserialize(din, valTypeCode, IProtocol.DeserializationFlags.None);
                }
                else
                {
                    byte type3 = din.ReadByte();
                    obj = this.Deserialize(din, type3, IProtocol.DeserializationFlags.None);
                }
                if (key != null)
                    instance.Add(key, obj);
            }
            arrayResult.SetValue((object)instance, (int)index1);
        }
        return true;
    }

    private Type DeserializeDictionaryType(
      StreamBuffer reader,
      out byte keyTypeCode,
      out byte valTypeCode)
    {
        keyTypeCode = reader.ReadByte();
        valTypeCode = reader.ReadByte();
        Protocol16.GpType gpType1 = (Protocol16.GpType)keyTypeCode;
        Protocol16.GpType gpType2 = (Protocol16.GpType)valTypeCode;
        Type type1;
        int num1;
        switch (gpType1)
        {
            case Protocol16.GpType.Unknown:
                type1 = typeof(object);
                goto label_7;
            case Protocol16.GpType.Dictionary:
                num1 = 1;
                break;
            default:
                num1 = gpType1 == Protocol16.GpType.Array ? 1 : 0;
                break;
        }
        if (num1 != 0)
            throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary keys.");
        type1 = this.GetTypeOfCode(keyTypeCode);
    label_7:
        Type type2;
        int num2;
        switch (gpType2)
        {
            case Protocol16.GpType.Unknown:
                type2 = typeof(object);
                goto label_14;
            case Protocol16.GpType.Dictionary:
                num2 = 1;
                break;
            default:
                num2 = gpType2 == Protocol16.GpType.Array ? 1 : 0;
                break;
        }
        if (num2 != 0)
            throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary values.");
        type2 = this.GetTypeOfCode(valTypeCode);
    label_14:
        return typeof(Dictionary<,>).MakeGenericType(type1, type2);
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