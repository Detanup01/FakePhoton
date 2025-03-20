using FakePhotonLib.BinaryData;
using FakePhotonLib.PhotonRelated;
using FakePhotonLib.PhotonRelated.StructWrapping;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakePhotonLib.Protocols;

public class Protocol18 : IProtocol
{
    public static readonly StructWrapperPools wrapperPools = new StructWrapperPools();
    private readonly byte[] versionBytes = new byte[2]
    {
      (byte) 1,
      (byte) 8
    };
    private static readonly byte[] boolMasks = new byte[8]
    {
      (byte) 1,
      (byte) 2,
      (byte) 4,
      (byte) 8,
      (byte) 16,
      (byte) 32,
      (byte) 64,
      (byte) 128
    };
    private readonly double[] memDoubleBlock = new double[1];
    private readonly float[] memFloatBlock = new float[1];
    private readonly byte[] memCustomTypeBodyLengthSerialized = new byte[5];
    private readonly byte[] memCompressedUInt32 = new byte[5];
    private byte[] memCompressedUInt64 = new byte[10];

    public override string ProtocolType => "GpBinaryV18";

    public override byte[] VersionBytes => this.versionBytes;

    public override void Serialize(StreamBuffer dout, object serObject, bool setType)
    {
        this.Write(dout, serObject, setType);
    }

    public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
    {
        this.WriteInt16(dout, serObject, setType);
    }

    public override void SerializeString(StreamBuffer dout, string serObject, bool setType)
    {
        this.WriteString(dout, serObject, setType);
    }

    public override object Deserialize(
      StreamBuffer din,
      byte type,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        return this.Read(din, type);
    }

    public override short DeserializeShort(StreamBuffer din) => this.ReadInt16(din);

    public override byte DeserializeByte(StreamBuffer din) => this.ReadByte(din);

    private static Type GetAllowedDictionaryKeyTypes(Protocol18.GpType gpType)
    {
        switch (gpType)
        {
            case Protocol18.GpType.Byte:
            case Protocol18.GpType.ByteZero:
                return typeof(byte);
            case Protocol18.GpType.Short:
            case Protocol18.GpType.ShortZero:
                return typeof(short);
            case Protocol18.GpType.Float:
            case Protocol18.GpType.FloatZero:
                return typeof(float);
            case Protocol18.GpType.Double:
            case Protocol18.GpType.DoubleZero:
                return typeof(double);
            case Protocol18.GpType.String:
                return typeof(string);
            case Protocol18.GpType.CompressedInt:
            case Protocol18.GpType.Int1:
            case Protocol18.GpType.Int1_:
            case Protocol18.GpType.Int2:
            case Protocol18.GpType.Int2_:
            case Protocol18.GpType.IntZero:
                return typeof(int);
            case Protocol18.GpType.CompressedLong:
            case Protocol18.GpType.L1:
            case Protocol18.GpType.L1_:
            case Protocol18.GpType.L2:
            case Protocol18.GpType.L2_:
            case Protocol18.GpType.LongZero:
                return typeof(long);
            default:
                throw new Exception(string.Format("{0} is not a valid Type as Dictionary key.", (object)gpType));
        }
    }

    private static Type GetClrArrayType(Protocol18.GpType gpType)
    {
        switch (gpType)
        {
            case Protocol18.GpType.Boolean:
            case Protocol18.GpType.BooleanFalse:
            case Protocol18.GpType.BooleanTrue:
                return typeof(bool);
            case Protocol18.GpType.Byte:
            case Protocol18.GpType.ByteZero:
                return typeof(byte);
            case Protocol18.GpType.Short:
            case Protocol18.GpType.ShortZero:
                return typeof(short);
            case Protocol18.GpType.Float:
            case Protocol18.GpType.FloatZero:
                return typeof(float);
            case Protocol18.GpType.Double:
            case Protocol18.GpType.DoubleZero:
                return typeof(double);
            case Protocol18.GpType.String:
                return typeof(string);
            case Protocol18.GpType.CompressedInt:
            case Protocol18.GpType.Int1:
            case Protocol18.GpType.Int1_:
            case Protocol18.GpType.Int2:
            case Protocol18.GpType.Int2_:
            case Protocol18.GpType.IntZero:
                return typeof(int);
            case Protocol18.GpType.CompressedLong:
            case Protocol18.GpType.L1:
            case Protocol18.GpType.L1_:
            case Protocol18.GpType.L2:
            case Protocol18.GpType.L2_:
            case Protocol18.GpType.LongZero:
                return typeof(long);
            case Protocol18.GpType.Hashtable:
                return typeof(Hashtable);
            case Protocol18.GpType.OperationRequest:
                return typeof(OperationRequest);
            case Protocol18.GpType.OperationResponse:
                return typeof(OperationResponse);
            case Protocol18.GpType.EventData:
                return typeof(EventData);
            case Protocol18.GpType.BooleanArray:
                return typeof(bool[]);
            case Protocol18.GpType.ByteArray:
                return typeof(byte[]);
            case Protocol18.GpType.ShortArray:
                return typeof(short[]);
            case Protocol18.GpType.FloatArray:
                return typeof(float[]);
            case Protocol18.GpType.DoubleArray:
                return typeof(double[]);
            case Protocol18.GpType.StringArray:
                return typeof(string[]);
            case Protocol18.GpType.CompressedIntArray:
                return typeof(int[]);
            case Protocol18.GpType.CompressedLongArray:
                return typeof(long[]);
            case Protocol18.GpType.HashtableArray:
                return typeof(Hashtable[]);
            default:
                return (Type)null;
        }
    }

    private Protocol18.GpType GetCodeOfType(Type type)
    {
        if (type == (Type)null)
            return Protocol18.GpType.Null;
        if (type == typeof(StructWrapper<>))
            return Protocol18.GpType.Unknown;
        if (type.IsPrimitive || type.IsEnum)
            return this.GetCodeOfTypeCode(Type.GetTypeCode(type));
        if (type == typeof(string))
            return Protocol18.GpType.String;
        if (type.IsArray)
        {
            Type elementType = type.GetElementType();
            if (elementType == (Type)null)
                throw new InvalidDataException(string.Format("Arrays of type {0} are not supported", (object)type));
            if (elementType.IsPrimitive)
            {
                switch (Type.GetTypeCode(elementType))
                {
                    case TypeCode.Boolean:
                        return Protocol18.GpType.BooleanArray;
                    case TypeCode.Byte:
                        return Protocol18.GpType.ByteArray;
                    case TypeCode.Int16:
                        return Protocol18.GpType.ShortArray;
                    case TypeCode.Int32:
                        return Protocol18.GpType.CompressedIntArray;
                    case TypeCode.Int64:
                        return Protocol18.GpType.CompressedLongArray;
                    case TypeCode.Single:
                        return Protocol18.GpType.FloatArray;
                    case TypeCode.Double:
                        return Protocol18.GpType.DoubleArray;
                }
            }
            if (elementType.IsArray)
                return Protocol18.GpType.Array;
            if (elementType == typeof(string))
                return Protocol18.GpType.StringArray;
            if (elementType == typeof(object) || elementType == typeof(StructWrapper))
                return Protocol18.GpType.ObjectArray;
            if (elementType == typeof(Hashtable))
                return Protocol18.GpType.HashtableArray;
            return elementType.IsGenericType && typeof(Dictionary<,>) == elementType.GetGenericTypeDefinition() ? Protocol18.GpType.DictionaryArray : Protocol18.GpType.CustomTypeArray;
        }
        if (type == typeof(Hashtable))
            return Protocol18.GpType.Hashtable;
        if (type == typeof(List<object>))
            return Protocol18.GpType.ObjectArray;
        if (type.IsGenericType && typeof(Dictionary<,>) == type.GetGenericTypeDefinition())
            return Protocol18.GpType.Dictionary;
        if (type == typeof(EventData))
            return Protocol18.GpType.EventData;
        if (type == typeof(OperationRequest))
            return Protocol18.GpType.OperationRequest;
        return type == typeof(OperationResponse) ? Protocol18.GpType.OperationResponse : Protocol18.GpType.Unknown;
    }

    private Protocol18.GpType GetCodeOfTypeCode(TypeCode type)
    {
        switch (type)
        {
            case TypeCode.Boolean:
                return Protocol18.GpType.Boolean;
            case TypeCode.Byte:
                return Protocol18.GpType.Byte;
            case TypeCode.Int16:
                return Protocol18.GpType.Short;
            case TypeCode.Int32:
                return Protocol18.GpType.CompressedInt;
            case TypeCode.Int64:
                return Protocol18.GpType.CompressedLong;
            case TypeCode.Single:
                return Protocol18.GpType.Float;
            case TypeCode.Double:
                return Protocol18.GpType.Double;
            case TypeCode.String:
                return Protocol18.GpType.String;
            default:
                return Protocol18.GpType.Unknown;
        }
    }

    private object Read(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        return this.Read(stream, this.ReadByte(stream), flags, parameters);
    }

    private object Read(
      StreamBuffer stream,
      byte gpType,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None,
      Dictionary<byte, object> parameters = null)
    {
        int num1 = gpType >= (byte)128 ? (int)gpType - 128 : (int)gpType;
        int num2 = num1 >= 64 ? num1 - 64 : num1;
        bool flag1 = (flags & IProtocol.DeserializationFlags.WrapIncomingStructs) == IProtocol.DeserializationFlags.WrapIncomingStructs;
        if (gpType >= (byte)128 && gpType <= (byte)228)
            return this.ReadCustomType(stream, gpType);
        switch (gpType)
        {
            case 2:
                bool flag2 = this.ReadBoolean(stream);
                return flag1 ? (object)wrapperPools.Acquire(flag2) : (object)flag2;
            case 3:
                byte num3 = this.ReadByte(stream);
                return flag1 ? (object)wrapperPools.Acquire(num3) : (object)num3;
            case 4:
                short num4 = this.ReadInt16(stream);
                return flag1 ? (object)wrapperPools.Acquire<short>(num4) : (object)num4;
            case 5:
                float num5 = this.ReadSingle(stream);
                return flag1 ? (object)wrapperPools.Acquire<float>(num5) : (object)num5;
            case 6:
                double num6 = this.ReadDouble(stream);
                return flag1 ? (object)wrapperPools.Acquire<double>(num6) : (object)num6;
            case 7:
                return (object)this.ReadString(stream);
            case 8:
                return (object)null;
            case 9:
                int num7 = this.ReadCompressedInt32(stream);
                return flag1 ? (object)wrapperPools.Acquire<int>(num7) : (object)num7;
            case 10:
                long num8 = this.ReadCompressedInt64(stream);
                return flag1 ? (object)wrapperPools.Acquire<long>(num8) : (object)num8;
            case 11:
                int num9 = this.ReadInt1(stream, false);
                return flag1 ? (object)wrapperPools.Acquire<int>(num9) : (object)num9;
            case 12:
                int num10 = this.ReadInt1(stream, true);
                return flag1 ? (object)wrapperPools.Acquire<int>(num10) : (object)num10;
            case 13:
                int num11 = this.ReadInt2(stream, false);
                return flag1 ? (object)wrapperPools.Acquire<int>(num11) : (object)num11;
            case 14:
                int num12 = this.ReadInt2(stream, true);
                return flag1 ? (object)wrapperPools.Acquire<int>(num12) : (object)num12;
            case 15:
                long num13 = (long)this.ReadInt1(stream, false);
                return flag1 ? (object)wrapperPools.Acquire<long>(num13) : (object)num13;
            case 16:
                long num14 = (long)this.ReadInt1(stream, true);
                return flag1 ? (object)wrapperPools.Acquire<long>(num14) : (object)num14;
            case 17:
                long num15 = (long)this.ReadInt2(stream, false);
                return flag1 ? (object)wrapperPools.Acquire<long>(num15) : (object)num15;
            case 18:
                long num16 = (long)this.ReadInt2(stream, true);
                return flag1 ? (object)wrapperPools.Acquire<long>(num16) : (object)num16;
            case 19:
                return this.ReadCustomType(stream);
            case 20:
                return (object)this.ReadDictionary(stream, flags, parameters);
            case 21:
                return (object)this.ReadHashtable(stream, flags, parameters);
            case 23:
                return (object)this.ReadObjectArray(stream, flags, parameters);
            case 24:
                return (object)this.DeserializeOperationRequest(stream, IProtocol.DeserializationFlags.None);
            case 25:
                return (object)this.DeserializeOperationResponse(stream, flags);
            case 26:
                return (object)this.DeserializeEventData(stream, null, IProtocol.DeserializationFlags.None);
            case 27:
                bool flag3 = false;
                return flag1 ? (object)wrapperPools.Acquire(flag3) : (object)flag3;
            case 28:
                bool flag4 = true;
                return flag1 ? (object)wrapperPools.Acquire(flag4) : (object)flag4;
            case 29:
                short num17 = 0;
                return flag1 ? (object)wrapperPools.Acquire<short>(num17) : (object)num17;
            case 30:
                int num18 = 0;
                return flag1 ? (object)wrapperPools.Acquire<int>(num18) : (object)num18;
            case 31:
                long num19 = 0;
                return flag1 ? (object)wrapperPools.Acquire<long>(num19) : (object)num19;
            case 32:
                float num20 = 0.0f;
                return flag1 ? (object)wrapperPools.Acquire<float>(num20) : (object)num20;
            case 33:
                double num21 = 0.0;
                return flag1 ? (object)wrapperPools.Acquire<double>(num21) : (object)num21;
            case 34:
                byte num22 = 0;
                return flag1 ? (object)wrapperPools.Acquire(num22) : (object)num22;
            case 64:
                return (object)this.ReadArrayInArray(stream, flags, parameters);
            case 66:
                return (object)this.ReadBooleanArray(stream);
            case 67:
                return (object)this.ReadByteArray(stream);
            case 68:
                return (object)this.ReadInt16Array(stream);
            case 69:
                return (object)this.ReadSingleArray(stream);
            case 70:
                return (object)this.ReadDoubleArray(stream);
            case 71:
                return (object)this.ReadStringArray(stream);
            case 73:
                return (object)this.ReadCompressedInt32Array(stream);
            case 74:
                return (object)this.ReadCompressedInt64Array(stream);
            case 83:
                return this.ReadCustomTypeArray(stream);
            case 84:
                return (object)this.ReadDictionaryArray(stream, flags, parameters);
            case 85:
                return (object)this.ReadHashtableArray(stream, flags, parameters);
            default:
                throw new InvalidDataException(string.Format("GpTypeCode not found: {0}(0x{0:X}). Is not a CustomType either. Pos: {1} Available: {2}", (object)gpType, (object)stream.Position, (object)stream.Available));
        }
    }

    internal bool ReadBoolean(StreamBuffer stream) => stream.ReadByte() > (byte)0;

    internal byte ReadByte(StreamBuffer stream) => stream.ReadByte();

    internal short ReadInt16(StreamBuffer stream)
    {
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(2, out offset);
        byte[] numArray = bufferAndAdvance;
        int index1 = offset;
        int index2 = index1 + 1;
        return (short)((int)numArray[index1] | (int)bufferAndAdvance[index2] << 8);
    }

    internal ushort ReadUShort(StreamBuffer stream)
    {
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(2, out offset);
        byte[] numArray = bufferAndAdvance;
        int index1 = offset;
        int index2 = index1 + 1;
        return (ushort)((uint)numArray[index1] | (uint)bufferAndAdvance[index2] << 8);
    }

    internal int ReadInt32(StreamBuffer stream)
    {
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(4, out offset);
        byte[] numArray1 = bufferAndAdvance;
        int index1 = offset;
        int num1 = index1 + 1;
        int num2 = (int)numArray1[index1] << 24;
        byte[] numArray2 = bufferAndAdvance;
        int index2 = num1;
        int num3 = index2 + 1;
        int num4 = (int)numArray2[index2] << 16;
        int num5 = num2 | num4;
        byte[] numArray3 = bufferAndAdvance;
        int index3 = num3;
        int index4 = index3 + 1;
        int num6 = (int)numArray3[index3] << 8;
        return num5 | num6 | (int)bufferAndAdvance[index4];
    }

    internal long ReadInt64(StreamBuffer stream)
    {
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(4, out offset);
        byte[] numArray1 = bufferAndAdvance;
        int index1 = offset;
        int num1 = index1 + 1;
        long num2 = (long)numArray1[index1] << 56;
        byte[] numArray2 = bufferAndAdvance;
        int index2 = num1;
        int num3 = index2 + 1;
        long num4 = (long)numArray2[index2] << 48;
        long num5 = num2 | num4;
        byte[] numArray3 = bufferAndAdvance;
        int index3 = num3;
        int num6 = index3 + 1;
        long num7 = (long)numArray3[index3] << 40;
        long num8 = num5 | num7;
        byte[] numArray4 = bufferAndAdvance;
        int index4 = num6;
        int num9 = index4 + 1;
        long num10 = (long)numArray4[index4] << 32;
        long num11 = num8 | num10;
        byte[] numArray5 = bufferAndAdvance;
        int index5 = num9;
        int num12 = index5 + 1;
        long num13 = (long)numArray5[index5] << 24;
        long num14 = num11 | num13;
        byte[] numArray6 = bufferAndAdvance;
        int index6 = num12;
        int num15 = index6 + 1;
        long num16 = (long)numArray6[index6] << 16;
        long num17 = num14 | num16;
        byte[] numArray7 = bufferAndAdvance;
        int index7 = num15;
        int index8 = index7 + 1;
        long num18 = (long)numArray7[index7] << 8;
        return num17 | num18 | (long)bufferAndAdvance[index8];
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
        uint num = this.ReadCompressedUInt32(stream);
        ByteArraySlice byteArraySlice = this.ByteArraySlicePool.Acquire((int)num);
        stream.Read(byteArraySlice.Buffer, 0, (int)num);
        byteArraySlice.Count = (int)num;
        return byteArraySlice;
    }

    internal byte[] ReadByteArray(StreamBuffer stream)
    {
        uint count = this.ReadCompressedUInt32(stream);
        byte[] buffer = new byte[(int)count];
        stream.Read(buffer, 0, (int)count);
        return buffer;
    }

    public object ReadCustomType(StreamBuffer stream, byte gpType = 0)
    {
        byte key = gpType != (byte)0 ? (byte)((uint)gpType - 128U) : stream.ReadByte();
        int length = (int)this.ReadCompressedUInt32(stream);
        if (length < 0)
            throw new InvalidDataException("ReadCustomType read negative size value: " + length.ToString() + " before position: " + stream.Position.ToString());
        bool flag = length <= stream.Available;
        CustomType? customType;
        if (!flag || length > (int)short.MaxValue || !Protocol.CodeDict.TryGetValue(key, out customType))
        {
            UnknownType unknownType = new UnknownType()
            {
                TypeCode = key,
                Size = length
            };
            int count = flag ? length : stream.Available;
            if (count > 0)
            {
                byte[] buffer = new byte[count];
                stream.Read(buffer, 0, count);
                unknownType.Data = buffer;
            }
            return (object)unknownType;
        }
        if (customType.DeserializeStreamFunction == null)
        {
            byte[] numArray = new byte[length];
            stream.Read(numArray, 0, length);
            return customType.DeserializeFunction(numArray);
        }
        int position = stream.Position;
        object obj = customType.DeserializeStreamFunction(stream, (short)length);
        if (stream.Position - position != length)
            stream.Position = position + length;
        return obj;
    }

    public override EventData DeserializeEventData(
      StreamBuffer din,
      EventData? target = null,
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
        eventData.Code = this.ReadByte(din);
        short num = (short)this.ReadByte(din);
        bool flag = (flags & IProtocol.DeserializationFlags.AllowPooledByteArray) == IProtocol.DeserializationFlags.AllowPooledByteArray;
        for (uint index = 0; (long)index < (long)num; ++index)
        {
            byte code = din.ReadByte();
            byte gpType = din.ReadByte();
            object obj;
            if (!flag)
                obj = this.Read(din, gpType, flags, eventData.Parameters);
            else if (gpType == (byte)67)
                obj = (object)this.ReadNonAllocByteArray(din);
            else if ((int)code == (int)eventData.SenderKey)
            {
                switch ((Protocol18.GpType)gpType)
                {
                    case Protocol18.GpType.CompressedInt:
                        eventData.Sender = this.ReadCompressedInt32(din);
                        continue;
                    case Protocol18.GpType.Int1:
                        eventData.Sender = this.ReadInt1(din, false);
                        continue;
                    case Protocol18.GpType.Int1_:
                        eventData.Sender = this.ReadInt1(din, true);
                        continue;
                    case Protocol18.GpType.Int2:
                        eventData.Sender = this.ReadInt2(din, false);
                        continue;
                    case Protocol18.GpType.Int2_:
                        eventData.Sender = this.ReadInt2(din, true);
                        continue;
                    case Protocol18.GpType.IntZero:
                        eventData.Sender = 0;
                        continue;
                    default:
                        continue;
                }
            }
            else
                obj = this.Read(din, gpType, flags, eventData.Parameters);
            eventData.Parameters.Add(code, obj);
        }
        return eventData;
    }

    [Obsolete("Use Dictionary<byte, object> instead.")]
    private Dictionary<byte, object> ReadParameterTable(
      StreamBuffer stream,
      Dictionary<byte, object> target = null,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        short capacity = (short)this.ReadByte(stream);
        Dictionary<byte, object> dictionary = target != null ? target : new Dictionary<byte, object>((int)capacity);
        for (uint index = 0; (long)index < (long)capacity; ++index)
        {
            byte key = stream.ReadByte();
            byte gpType = stream.ReadByte();
            object obj = gpType != (byte)67 || (flags & IProtocol.DeserializationFlags.AllowPooledByteArray) != IProtocol.DeserializationFlags.AllowPooledByteArray ? this.Read(stream, gpType, flags) : (object)this.ReadNonAllocByteArray(stream);
            dictionary[key] = obj;
        }
        return dictionary;
    }

    private Dictionary<byte, object> ReadParameters(
      StreamBuffer stream,
      Dictionary<byte, object> target = null,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        short capacity = (short)this.ReadByte(stream);
        Dictionary<byte, object> parameters = target != null ? target : new((int)capacity);
        bool flag = (flags & IProtocol.DeserializationFlags.AllowPooledByteArray) == IProtocol.DeserializationFlags.AllowPooledByteArray;
        for (uint index = 0; (long)index < (long)capacity; ++index)
        {
            byte code = stream.ReadByte();
            byte gpType = stream.ReadByte();
            object obj = !flag || gpType != (byte)67 ? this.Read(stream, gpType, flags, parameters) : (object)this.ReadNonAllocByteArray(stream);
            parameters.Add(code, obj);
        }
        return parameters;
    }

    public Hashtable ReadHashtable(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        int x = (int)this.ReadCompressedUInt32(stream);
        Hashtable hashtable = new Hashtable(x);
        for (uint index = 0; (long)index < (long)x; ++index)
        {
            object key = this.Read(stream, flags, parameters);
            object obj = this.Read(stream, flags, parameters);
            if (key != null)
            {
                if (!(key is StructWrapper<byte> structWrapper))
                    hashtable[key] = obj;
                else
                    hashtable[structWrapper.Unwrap<byte>()] = obj;
            }
        }
        return hashtable;
    }

    public int[] ReadIntArray(StreamBuffer stream)
    {
        int length = this.ReadInt32(stream);
        int[] numArray = new int[length];
        for (uint index = 0; (long)index < (long)length; ++index)
            numArray[(int)index] = this.ReadInt32(stream);
        return numArray;
    }

    public override OperationRequest DeserializeOperationRequest(
      StreamBuffer din,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        OperationRequest operationRequest = new OperationRequest()
        {
            OperationCode = this.ReadByte(din)
        };
        operationRequest.Parameters = this.ReadParameters(din, operationRequest.Parameters, flags);
        return operationRequest;
    }

    public override OperationResponse DeserializeOperationResponse(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags = IProtocol.DeserializationFlags.None)
    {
        OperationResponse operationResponse = new OperationResponse()
        {
            OperationCode = this.ReadByte(stream),
            ReturnCode = this.ReadInt16(stream)
        };
        operationResponse.DebugMessage = this.Read(stream, this.ReadByte(stream), flags, operationResponse.Parameters) as string;
        operationResponse.Parameters = this.ReadParameters(stream, operationResponse.Parameters, flags);
        return operationResponse;
    }

    public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
    {
        return new DisconnectMessage()
        {
            Code = this.ReadInt16(stream),
            DebugMessage = this.Read(stream, this.ReadByte(stream)) as string,
            Parameters = this.ReadParameterTable(stream)
        };
    }

    internal string ReadString(StreamBuffer stream)
    {
        int num = (int)this.ReadCompressedUInt32(stream);
        if (num == 0)
            return string.Empty;
        int offset = 0;
        return Encoding.UTF8.GetString(stream.GetBufferAndAdvance(num, out offset), offset, num);
    }

    private object ReadCustomTypeArray(StreamBuffer stream)
    {
        uint length1 = this.ReadCompressedUInt32(stream);
        byte key = stream.ReadByte();
        CustomType customType;
        if (!Protocol.CodeDict.TryGetValue(key, out customType))
        {
            int position = stream.Position;
            for (uint index = 0; index < length1; ++index)
            {
                int num1 = (int)this.ReadCompressedUInt32(stream);
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
            int length2 = (int)this.ReadCompressedUInt32(stream);
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
                type2 = this.ReadDictionaryType(stream);
                break;
            case Protocol18.GpType.ObjectArray:
                type2 = typeof(object[]);
                break;
            case Protocol18.GpType.Array:
                type2 = this.GetDictArrayType(stream);
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
                type2 = this.ReadDictionaryType(stream);
                break;
            case Protocol18.GpType.Array:
                type2 = this.GetDictArrayType(stream);
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
        Type type = this.ReadDictionaryType(stream, out keyReadType, out valueReadType);
        if (type == (Type)null || !(Activator.CreateInstance(type) is IDictionary instance))
            return (IDictionary)null;
        this.ReadDictionaryElements(stream, keyReadType, valueReadType, instance, flags, parameters);
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
        uint num = this.ReadCompressedUInt32(stream);
        for (uint index = 0; index < num; ++index)
        {
            object key = keyReadType == Protocol18.GpType.Unknown ? this.Read(stream, flags, parameters) : this.Read(stream, (byte)keyReadType);
            object obj = valueReadType == Protocol18.GpType.Unknown ? this.Read(stream, flags, parameters) : this.Read(stream, (byte)valueReadType);
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
        uint length = this.ReadCompressedUInt32(stream);
        object[] objArray = new object[(int)length];
        for (uint index = 0; index < length; ++index)
        {
            object obj = this.Read(stream, flags, parameters);
            objArray[(int)index] = obj;
        }
        return objArray;
    }

    private StructWrapper[] ReadWrapperArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = this.ReadCompressedUInt32(stream);
        StructWrapper[] structWrapperArray = new StructWrapper[(int)length];
        for (uint index = 0; index < length; ++index)
        {
            object obj = this.Read(stream, flags, parameters);
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
        uint length = this.ReadCompressedUInt32(stream);
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
        short[] numArray = new short[(int)this.ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)numArray.Length; ++index)
            numArray[(int)index] = this.ReadInt16(stream);
        return numArray;
    }

    private float[] ReadSingleArray(StreamBuffer stream)
    {
        int length = (int)this.ReadCompressedUInt32(stream);
        int num = length * 4;
        float[] dst = new float[length];
        int offset;
        Buffer.BlockCopy((Array)stream.GetBufferAndAdvance(num, out offset), offset, (Array)dst, 0, num);
        return dst;
    }

    private double[] ReadDoubleArray(StreamBuffer stream)
    {
        int length = (int)this.ReadCompressedUInt32(stream);
        int num = length * 8;
        double[] dst = new double[length];
        int offset;
        Buffer.BlockCopy((Array)stream.GetBufferAndAdvance(num, out offset), offset, (Array)dst, 0, num);
        return dst;
    }

    internal string[] ReadStringArray(StreamBuffer stream)
    {
        string[] strArray = new string[(int)this.ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)strArray.Length; ++index)
            strArray[(int)index] = this.ReadString(stream);
        return strArray;
    }

    private Hashtable[] ReadHashtableArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = this.ReadCompressedUInt32(stream);
        Hashtable[] hashtableArray = new Hashtable[(int)length];
        for (uint index = 0; index < length; ++index)
            hashtableArray[(int)index] = this.ReadHashtable(stream, flags, parameters);
        return hashtableArray;
    }

    private IDictionary[] ReadDictionaryArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        Protocol18.GpType keyReadType;
        Protocol18.GpType valueReadType;
        Type type = this.ReadDictionaryType(stream, out keyReadType, out valueReadType);
        uint length = this.ReadCompressedUInt32(stream);
        IDictionary[] instance = (IDictionary[])Array.CreateInstance(type, (int)length);
        for (uint index = 0; index < length; ++index)
        {
            instance[(int)index] = (IDictionary)Activator.CreateInstance(type);
            this.ReadDictionaryElements(stream, keyReadType, valueReadType, instance[(int)index], flags, parameters);
        }
        return instance;
    }

    private Array ReadArrayInArray(
      StreamBuffer stream,
      IProtocol.DeserializationFlags flags,
      Dictionary<byte, object> parameters)
    {
        uint length = this.ReadCompressedUInt32(stream);
        Array array1 = (Array)null;
        Type elementType = (Type)null;
        for (uint index = 0; index < length; ++index)
        {
            if (this.Read(stream, flags, parameters) is Array array2)
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
        return signNegative ? (int)-this.ReadUShort(stream) : (int)this.ReadUShort(stream);
    }

    internal int ReadCompressedInt32(StreamBuffer stream)
    {
        return this.DecodeZigZag32(this.ReadCompressedUInt32(stream));
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
        return this.DecodeZigZag64(this.ReadCompressedUInt64(stream));
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
        int[] numArray = new int[(int)this.ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)numArray.Length; ++index)
            numArray[(int)index] = this.ReadCompressedInt32(stream);
        return numArray;
    }

    internal long[] ReadCompressedInt64Array(StreamBuffer stream)
    {
        long[] numArray = new long[(int)this.ReadCompressedUInt32(stream)];
        for (uint index = 0; (long)index < (long)numArray.Length; ++index)
            numArray[(int)index] = this.ReadCompressedInt64(stream);
        return numArray;
    }

    private int DecodeZigZag32(uint value) => (int)((long)(value >> 1) ^ (long)-(value & 1U));

    private long DecodeZigZag64(ulong value) => (long)(value >> 1) ^ -((long)value & 1L);

    internal void Write(StreamBuffer stream, object value, bool writeType)
    {
        if (value == null)
            this.Write(stream, value, Protocol18.GpType.Null, writeType);
        else
            this.Write(stream, value, this.GetCodeOfType(value.GetType()), writeType);
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
                        this.WriteByteArraySlice(stream, buffer, writeType);
                        return;
                    case ArraySegment<byte> seg:
                        this.WriteArraySegmentByte(stream, seg, writeType);
                        return;
                    case StructWrapper structWrapper:
                        switch (structWrapper.wrappedType)
                        {
                            case WrappedType.Bool:
                                this.WriteBoolean(stream, value.Get<bool>(), writeType);
                                return;
                            case WrappedType.Byte:
                                this.WriteByte(stream, value.Get<byte>(), writeType);
                                return;
                            case WrappedType.Int16:
                                this.WriteInt16(stream, value.Get<short>(), writeType);
                                return;
                            case WrappedType.Int32:
                                this.WriteCompressedInt32(stream, value.Get<int>(), writeType);
                                return;
                            case WrappedType.Int64:
                                this.WriteCompressedInt64(stream, value.Get<long>(), writeType);
                                return;
                            case WrappedType.Single:
                                this.WriteSingle(stream, value.Get<float>(), writeType);
                                return;
                            case WrappedType.Double:
                                this.WriteDouble(stream, value.Get<double>(), writeType);
                                return;
                            default:
                                this.WriteCustomType(stream, value, writeType);
                                return;
                        }
                    default:
                        goto label_18;
                }
            case Protocol18.GpType.Boolean:
                this.WriteBoolean(stream, (bool)value, writeType);
                break;
            case Protocol18.GpType.Byte:
                this.WriteByte(stream, (byte)value, writeType);
                break;
            case Protocol18.GpType.Short:
                this.WriteInt16(stream, (short)value, writeType);
                break;
            case Protocol18.GpType.Float:
                this.WriteSingle(stream, (float)value, writeType);
                break;
            case Protocol18.GpType.Double:
                this.WriteDouble(stream, (double)value, writeType);
                break;
            case Protocol18.GpType.String:
                this.WriteString(stream, (string)value, writeType);
                break;
            case Protocol18.GpType.Null:
                if (!writeType)
                    break;
                stream.WriteByte((byte)8);
                break;
            case Protocol18.GpType.CompressedInt:
                this.WriteCompressedInt32(stream, (int)value, writeType);
                break;
            case Protocol18.GpType.CompressedLong:
                this.WriteCompressedInt64(stream, (long)value, writeType);
                break;
            case Protocol18.GpType.Custom:
            label_18:
                this.WriteCustomType(stream, value, writeType);
                break;
            case Protocol18.GpType.Dictionary:
                this.WriteDictionary(stream, (object)(IDictionary)value, writeType);
                break;
            case Protocol18.GpType.Hashtable:
                this.WriteHashtable(stream, (object)(Hashtable)value, writeType);
                break;
            case Protocol18.GpType.ObjectArray:
                this.WriteObjectArray(stream, (IList)value, writeType);
                break;
            case Protocol18.GpType.OperationRequest:
                this.SerializeOperationRequest(stream, (OperationRequest)value, writeType);
                break;
            case Protocol18.GpType.OperationResponse:
                this.SerializeOperationResponse(stream, (OperationResponse)value, writeType);
                break;
            case Protocol18.GpType.EventData:
                this.SerializeEventData(stream, (EventData)value, writeType);
                break;
            case Protocol18.GpType.Array:
                this.WriteArrayInArray(stream, value, writeType);
                break;
            case Protocol18.GpType.BooleanArray:
                this.WriteBoolArray(stream, (bool[])value, writeType);
                break;
            case Protocol18.GpType.ByteArray:
                this.WriteByteArray(stream, (byte[])value, writeType);
                break;
            case Protocol18.GpType.ShortArray:
                this.WriteInt16Array(stream, (short[])value, writeType);
                break;
            case Protocol18.GpType.FloatArray:
                this.WriteSingleArray(stream, (float[])value, writeType);
                break;
            case Protocol18.GpType.DoubleArray:
                this.WriteDoubleArray(stream, (double[])value, writeType);
                break;
            case Protocol18.GpType.StringArray:
                this.WriteStringArray(stream, value, writeType);
                break;
            case Protocol18.GpType.CompressedIntArray:
                this.WriteInt32ArrayCompressed(stream, (int[])value, writeType);
                break;
            case Protocol18.GpType.CompressedLongArray:
                this.WriteInt64ArrayCompressed(stream, (long[])value, writeType);
                break;
            case Protocol18.GpType.CustomTypeArray:
                this.WriteCustomTypeArray(stream, value, writeType);
                break;
            case Protocol18.GpType.DictionaryArray:
                this.WriteDictionaryArray(stream, (IDictionary[])value, writeType);
                break;
            case Protocol18.GpType.HashtableArray:
                this.WriteHashtableArray(stream, value, writeType);
                break;
        }
    }

    public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)26);
        stream.WriteByte(serObject.Code);
        this.WriteParameterTable(stream, serObject.Parameters);
    }

    private void WriteParameterTable(StreamBuffer stream, Dictionary<byte, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            this.WriteByte(stream, (byte)0, false);
        }
        else
        {
            this.WriteByte(stream, (byte)parameters.Count, false);
            foreach (KeyValuePair<byte, object> parameter in parameters)
            {
                stream.WriteByte(parameter.Key);
                this.Write(stream, parameter.Value, true);
            }
        }
    }

    private void SerializeOperationRequest(
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
            stream.WriteByte((byte)24);
        stream.WriteByte(operationCode);
        this.WriteParameterTable(stream, parameters);
    }

    public override void SerializeOperationResponse(
      StreamBuffer stream,
      OperationResponse serObject,
      bool setType)
    {
        if (setType)
            stream.WriteByte((byte)25);
        stream.WriteByte(serObject.OperationCode);
        this.WriteInt16(stream, serObject.ReturnCode, false);
        if (string.IsNullOrEmpty(serObject.DebugMessage))
        {
            stream.WriteByte((byte)8);
        }
        else
        {
            stream.WriteByte((byte)7);
            this.WriteString(stream, serObject.DebugMessage, false);
        }
        this.WriteParameterTable(stream, serObject.Parameters);
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
        lock (this.memDoubleBlock)
        {
            this.memDoubleBlock[0] = value;
            Buffer.BlockCopy((Array)this.memDoubleBlock, 0, (Array)bufferAndAdvance, offset, 8);
        }
    }

    internal void WriteSingle(StreamBuffer stream, float value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)5);
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(4, out offset);
        lock (this.memFloatBlock)
        {
            this.memFloatBlock[0] = value;
            Buffer.BlockCopy((Array)this.memFloatBlock, 0, (Array)bufferAndAdvance, offset, 4);
        }
    }

    internal void WriteString(StreamBuffer stream, string value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)7);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > (int)short.MaxValue)
            throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + byteCount.ToString());
        this.WriteIntLength(stream, byteCount);
        int offset = 0;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(byteCount, out offset);
        Encoding.UTF8.GetBytes(value, 0, value.Length, bufferAndAdvance, offset);
    }

    private void WriteHashtable(StreamBuffer stream, object value, bool writeType)
    {
        Hashtable hashtable = (Hashtable)value;
        if (writeType)
            stream.WriteByte((byte)21);
        this.WriteIntLength(stream, hashtable.Count);
        foreach (object key in hashtable.Keys)
        {
            this.Write(stream, key, true);
            this.Write(stream, hashtable[key], true);
        }
    }

    internal void WriteByteArray(StreamBuffer stream, byte[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)67);
        this.WriteIntLength(stream, value.Length);
        stream.Write(value, 0, value.Length);
    }

    private void WriteArraySegmentByte(StreamBuffer stream, ArraySegment<byte> seg, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)67);
        int count = seg.Count;
        this.WriteIntLength(stream, count);
        if (count <= 0)
            return;
        stream.Write(seg.Array, seg.Offset, count);
    }

    private void WriteByteArraySlice(StreamBuffer stream, ByteArraySlice buffer, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)67);
        int count = buffer.Count;
        this.WriteIntLength(stream, count);
        stream.Write(buffer.Buffer, buffer.Offset, count);
        buffer.Release();
    }

    internal void WriteInt32ArrayCompressed(StreamBuffer stream, int[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)73);
        this.WriteIntLength(stream, value.Length);
        for (int index = 0; index < value.Length; ++index)
            this.WriteCompressedInt32(stream, value[index], false);
    }

    private void WriteInt64ArrayCompressed(StreamBuffer stream, long[] values, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)74);
        this.WriteIntLength(stream, values.Length);
        for (int index = 0; index < values.Length; ++index)
            this.WriteCompressedInt64(stream, values[index], false);
    }

    internal void WriteBoolArray(StreamBuffer stream, bool[] value, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)66);
        this.WriteIntLength(stream, value.Length);
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
        this.WriteIntLength(stream, value.Length);
        for (int index = 0; index < value.Length; ++index)
            this.WriteInt16(stream, value[index], false);
    }

    internal void WriteSingleArray(StreamBuffer stream, float[] values, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)69);
        this.WriteIntLength(stream, values.Length);
        int num = values.Length * 4;
        int offset;
        byte[] bufferAndAdvance = stream.GetBufferAndAdvance(num, out offset);
        Buffer.BlockCopy((Array)values, 0, (Array)bufferAndAdvance, offset, num);
    }

    internal void WriteDoubleArray(StreamBuffer stream, double[] values, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)70);
        this.WriteIntLength(stream, values.Length);
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
        this.WriteIntLength(stream, strArray.Length);
        for (int index = 0; index < strArray.Length; ++index)
        {
            if (strArray[index] == null)
                throw new InvalidDataException("Unexpected - cannot serialize string array with null element " + index.ToString());
            this.WriteString(stream, strArray[index], false);
        }
    }

    private void WriteObjectArray(StreamBuffer stream, object array, bool writeType)
    {
        this.WriteObjectArray(stream, (IList)array, writeType);
    }

    private void WriteObjectArray(StreamBuffer stream, IList array, bool writeType)
    {
        if (writeType)
            stream.WriteByte((byte)23);
        this.WriteIntLength(stream, array.Count);
        for (int index = 0; index < array.Count; ++index)
        {
            object obj = array[index];
            this.Write(stream, obj, true);
        }
    }

    private void WriteArrayInArray(StreamBuffer stream, object value, bool writeType)
    {
        object[] objArray = (object[])value;
        stream.WriteByte((byte)64);
        this.WriteIntLength(stream, objArray.Length);
        foreach (object obj in objArray)
            this.Write(stream, obj, true);
    }

    private void WriteCustomTypeBody(CustomType customType, StreamBuffer stream, object value)
    {
        if (customType.SerializeFunction != null)
        {
            byte[] buffer = customType.SerializeFunction(value);
            this.WriteIntLength(stream, buffer.Length);
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
            int count2 = this.WriteCompressedUInt32(this.memCustomTypeBodyLengthSerialized, (uint)count1);
            if (count2 == 1)
            {
                stream.GetBuffer()[position] = this.memCustomTypeBodyLengthSerialized[0];
            }
            else
            {
                for (int index = 0; index < count2 - 1; ++index)
                    stream.WriteByte((byte)0);
                Buffer.BlockCopy((Array)stream.GetBuffer(), position + 1, (Array)stream.GetBuffer(), position + count2, count1);
                Buffer.BlockCopy((Array)this.memCustomTypeBodyLengthSerialized, 0, (Array)stream.GetBuffer(), position, count2);
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
        this.WriteCustomTypeBody(customType, stream, value);
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
        this.WriteIntLength(stream, list.Count);
        stream.WriteByte(customType.Code);
        foreach (object obj in (IEnumerable)list)
            this.WriteCustomTypeBody(customType, stream, obj);
    }

    private bool WriteArrayHeader(StreamBuffer stream, Type type)
    {
        Type? elementType;
        for (elementType = type.GetElementType(); elementType.IsArray; elementType = elementType.GetElementType())
            stream.WriteByte((byte)64);
        Protocol18.GpType codeOfType = this.GetCodeOfType(elementType);
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
        this.WriteIntLength(stream, dictionary.Count);
        foreach (DictionaryEntry dictionaryEntry in dictionary)
        {
            this.Write(stream, dictionaryEntry.Key, keyWriteType == Protocol18.GpType.Unknown);
            this.Write(stream, dictionaryEntry.Value!, valueWriteType == Protocol18.GpType.Unknown);
        }
    }

    private void WriteDictionary(StreamBuffer stream, object dict, bool setType)
    {
        if (setType)
            stream.WriteByte((byte)20);
        Protocol18.GpType keyWriteType;
        Protocol18.GpType valueWriteType;
        this.WriteDictionaryHeader(stream, dict.GetType(), out keyWriteType, out valueWriteType);
        IDictionary dictionary = (IDictionary)dict;
        this.WriteDictionaryElements(stream, dictionary, keyWriteType, valueWriteType);
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
            keyWriteType = genericArguments[0].IsPrimitive || !(genericArguments[0] != typeof(string)) ? this.GetCodeOfType(genericArguments[0]) : throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]?.ToString());
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
            if (!this.WriteArrayType(stream, genericArguments[1], out valueWriteType))
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
        }
        else
        {
            valueWriteType = this.GetCodeOfType(genericArguments[1]);
            if (valueWriteType == Protocol18.GpType.Unknown)
                throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            if (valueWriteType == Protocol18.GpType.Array)
            {
                if (!this.WriteArrayHeader(stream, genericArguments[1]))
                    throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[1]?.ToString());
            }
            else if (valueWriteType == Protocol18.GpType.Dictionary)
            {
                stream.WriteByte((byte)valueWriteType);
                this.WriteDictionaryHeader(stream, genericArguments[1], out Protocol18.GpType _, out Protocol18.GpType _);
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
            byte num = (byte)(this.GetCodeOfType(elementType) | Protocol18.GpType.Array);
            stream.WriteByte(num);
            writeType = Protocol18.GpType.Array;
            return true;
        }
        if (elementType.IsPrimitive)
        {
            byte num = (byte)(this.GetCodeOfType(elementType) | Protocol18.GpType.Array);
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
        this.WriteIntLength(stream, hashtableArray.Length);
        foreach (Hashtable hashtable in hashtableArray)
            this.WriteHashtable(stream, (object)hashtable, false);
    }

    private void WriteDictionaryArray(StreamBuffer stream, IDictionary[] dictArray, bool writeType)
    {
        stream.WriteByte((byte)84);
        Protocol18.GpType keyWriteType;
        Protocol18.GpType valueWriteType;
        this.WriteDictionaryHeader(stream, dictArray.GetType().GetElementType()!, out keyWriteType, out valueWriteType);
        this.WriteIntLength(stream, dictArray.Length);
        foreach (IDictionary dict in dictArray)
            this.WriteDictionaryElements(stream, dict, keyWriteType, valueWriteType);
    }

    private void WriteIntLength(StreamBuffer stream, int value)
    {
        this.WriteCompressedUInt32(stream, (uint)value);
    }

    private void WriteVarInt32(StreamBuffer stream, int value, bool writeType)
    {
        this.WriteCompressedInt32(stream, value, writeType);
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
                    this.WriteUShort(stream, (ushort)value);
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
                    this.WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType)
            stream.WriteByte((byte)9);
        uint num = this.EncodeZigZag32(value);
        this.WriteCompressedUInt32(stream, num);
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
                    this.WriteUShort(stream, (ushort)value);
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
                    this.WriteUShort(stream, (ushort)-value);
                    return;
                }
            }
        }
        if (writeType)
            stream.WriteByte((byte)10);
        ulong num = this.EncodeZigZag64(value);
        this.WriteCompressedUInt64(stream, num);
    }

    private void WriteCompressedUInt32(StreamBuffer stream, uint value)
    {
        lock (this.memCompressedUInt32)
            stream.Write(this.memCompressedUInt32, 0, this.WriteCompressedUInt32(this.memCompressedUInt32, value));
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
        lock (this.memCompressedUInt64)
        {
            this.memCompressedUInt64[index] = (byte)(value & (ulong)sbyte.MaxValue);
            for (value >>= 7; value > 0UL; value >>= 7)
            {
                this.memCompressedUInt64[index] |= (byte)128;
                this.memCompressedUInt64[++index] = (byte)(value & (ulong)sbyte.MaxValue);
            }
            int count = index + 1;
            stream.Write(this.memCompressedUInt64, 0, count);
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
