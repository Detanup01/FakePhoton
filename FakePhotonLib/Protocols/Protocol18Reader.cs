using FakePhotonLib.BinaryData;
using System.Collections;
using System.IO;
using System.Text;

namespace FakePhotonLib.Protocols;

public static class Protocol18Reader
{
    #region Required stuff
    private static int DecodeZigZag32(uint value)
    {
        return (int)((value >> 1) ^ -(value & 1U));
    }

    private static long DecodeZigZag64(ulong value)
    {
        return ((long)(value >> 1) ^ -(long)(value & 1UL));
    }

    public static int ReadCompressedInt32(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        return DecodeZigZag32(num);
    }

    public static uint ReadCompressedUInt32(this BinaryReader br)
    {
        uint num = 0U;
        int num2 = 0;
        while (num2 != 35)
        {
            byte b = br.ReadByte();
            num |= (uint)(b & 127) << num2;
            num2 += 7;
            if ((b & 128) == 0)
            {
                break;
            }
        }
        return num;
    }

    public static long ReadCompressedInt64(this BinaryReader br)
    {
        ulong num = br.ReadCompressedUInt64();
        return DecodeZigZag64(num);
    }
    public static ulong ReadCompressedUInt64(this BinaryReader br)
    {
        ulong num = 0UL;
        int num2 = 0;
        while (num2 != 70)
        {
            byte b = br.ReadByte();
            num |= (ulong)(long)(b & 127) << num2;
            num2 += 7;
            if ((b & 128) == 0)
            {
                break;
            }
        }
        return num;
    }

    public static int ReadInt1(this BinaryReader br, bool signNegative)
    {
        int num;
        if (signNegative)
        {
            num = -br.ReadByte();
        }
        else
        {
            num = br.ReadByte();
        }
        return num;
    }

    public static int ReadInt2(this BinaryReader br, bool signNegative)
    {
        int num;
        if (signNegative)
        {
            num = -br.ReadUInt16();
        }
        else
        {
            num = br.ReadUInt16();
        }
        return num;
    }

    public static string ReadString(this BinaryReader br)
    {
        int num = (int)br.ReadCompressedUInt32();
        if (num == 0)
        {
            return string.Empty;
        }
        else
        {
            return Encoding.UTF8.GetString(br.ReadBytes(num));
        }
    }
    #endregion
    #region Custom readers
    public static OperationRequest ReadOperationRequest(this BinaryReader br)
    {
        OperationRequest operationRequest = new();
        operationRequest.Read(br);
        return operationRequest;
    }

    public static OperationResponse ReadOperationResponse(this BinaryReader br)
    {
        OperationResponse operationResponse = new();
        operationResponse.Read(br);
        return operationResponse;
    }

    public static EventData ReadEventData(this BinaryReader br)
    {
        EventData eventData = new();
        eventData.Read(br);
        return eventData;
    }

    public static Hashtable ReadHashtable(this BinaryReader br)
    {
        int num = (int)br.ReadCompressedUInt32();
        Hashtable hashtable = new Hashtable(num);
        uint num2 = 0U;
        while (num2 < (ulong)(long)num)
        {
            object? obj = br.ReadObject();
            object? obj2 = br.ReadObject();
            if (obj != null)
            {
                hashtable[obj] = obj2;
            }
            num2 += 1U;
        }
        return hashtable;
    }
    #endregion
    #region Dictionary

    public static Dictionary<byte, object> ReadParameterDictionary(this BinaryReader br)
    {
        short num = (short)br.ReadByte();
        Dictionary<byte, object> dic = new(num);
        uint num2 = 0U;
        while ((ulong)num2 < (ulong)((long)num))
        {
            byte b = br.ReadByte();
            byte b2 = br.ReadByte();
            var obj = br.ReadObject(b2);
            dic.Add(b, obj);
            num2 += 1U;
        }
        return dic;
    }

    public static IDictionary? ReadDictionary(this BinaryReader br)
    {
        Type type = br.ReadDictionaryType(out GpType gpType, out GpType gpType2);
        bool flag = type == null;
        if (type == null)
        {
            return null;
        }
        IDictionary? dictionary2 = Activator.CreateInstance(type) as IDictionary;
        if (dictionary2 == null)
        {
            return null;
        }
        else
        {
            br.ReadDictionaryElements(gpType, gpType2, dictionary2);
            return dictionary2;
        }
    }

    public static bool ReadDictionaryElements(this BinaryReader stream, GpType keyReadType, GpType valueReadType, IDictionary dictionary)
    {
        uint num = stream.ReadCompressedUInt32();
        for (uint num2 = 0U; num2 < num; num2 += 1U)
        {
            object? obj = ((keyReadType == GpType.Unknown) ? ReadObject(stream) : ReadObject(stream, (byte)keyReadType));
            object? obj2 = ((valueReadType == GpType.Unknown) ? ReadObject(stream) : ReadObject(stream, (byte)valueReadType));
            if (obj != null)
            {
                dictionary.Add(obj, obj2);
            }
        }
        return true;
    }

    public static Type ReadDictionaryType(this BinaryReader br,  out GpType keyReadType, out GpType valueReadType)
    {
        keyReadType = (GpType)br.ReadByte();
        GpType gpType = (GpType)br.ReadByte();
        valueReadType = gpType;
        bool flag = keyReadType == GpType.Unknown;
        Type type;
        if (flag)
        {
            type = typeof(object);
        }
        else
        {
            type = Protocol18Common.GetAllowedDictionaryKeyTypes(keyReadType);
        }
        bool flag2 = gpType == GpType.Unknown;
        Type? type2;
        if (gpType == GpType.Unknown)
        {
            type2 = typeof(object);
        }
        if (gpType == GpType.Dictionary)
        {
            type2 = br.ReadDictionaryType();
        }
        if (gpType == GpType.Array)
        {
            type2 = Protocol18Common.GetDictArrayType(br);
            valueReadType = GpType.Unknown;
        }
        if (gpType == GpType.ObjectArray)
        {
            type2 = typeof(object[]);
        }
        if (gpType == GpType.HashtableArray)
        {
            type2 = typeof(Hashtable[]);
        }
        else
        {
            type2 = Protocol18Common.GetClrArrayType(gpType);
        }

        if (type2 == null)
            throw new Exception("Type2 is null!");

        return typeof(Dictionary<,>).MakeGenericType([type, type2]);
    }

    private static Type ReadDictionaryType(this BinaryReader br)
    {
        GpType gpType = (GpType)br.ReadByte();
        GpType gpType2 = (GpType)br.ReadByte();
        bool flag = gpType == GpType.Unknown;
        Type type;
        if (flag)
        {
            type = typeof(object);
        }
        else
        {
            type = Protocol18Common.GetAllowedDictionaryKeyTypes(gpType);
        }
        Type? type2;
        if (gpType2 == GpType.Unknown)
        {
            type2 = typeof(object);
        }
        if (gpType2 == GpType.Dictionary)
        {
            type2 = br.ReadDictionaryType();
        }
        if (gpType2 == GpType.Array)
        {
            type2 = Protocol18Common.GetDictArrayType(br);
        }
        else
        {
            type2 = Protocol18Common.GetClrArrayType(gpType2);
        }

        if (type2 == null)
            throw new Exception("Type2 is null!");

        return typeof(Dictionary<,>).MakeGenericType([type, type2]);
    }
    #endregion
    #region Arrays
    public static object[] ReadObjectArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        object[] array = new object[num];
        for (uint num2 = 0U; num2 < num; num2 += 1U)
        {
            object? obj = br.ReadObject();
            array[(int)num2] = obj;
        }
        return array;
    }
    public static bool[] ReadBooleanArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        bool[] array = new bool[num];
        int i = (int)(num / 8U);
        int num2 = 0;
        while (i > 0)
        {
            byte b = br.ReadByte();
            array[num2++] = (b & 1) == 1;
            array[num2++] = (b & 2) == 2;
            array[num2++] = (b & 4) == 4;
            array[num2++] = (b & 8) == 8;
            array[num2++] = (b & 16) == 16;
            array[num2++] = (b & 32) == 32;
            array[num2++] = (b & 64) == 64;
            array[num2++] = (b & 128) == 128;
            i--;
        }
        bool flag = (long)num2 < (long)((ulong)num);
        if (flag)
        {
            byte b2 = br.ReadByte();
            int num3 = 0;
            while ((long)num2 < (long)((ulong)num))
            {
                array[num2++] = (b2 & Protocol18Common.boolMasks[num3]) == Protocol18Common.boolMasks[num3];
                num3++;
            }
        }
        return array;
    }

    public static short[] ReadInt16Array(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        short[] array = new short[num];
        uint num2 = 0U;
        while ((ulong)num2 < (ulong)((long)array.Length))
        {
            array[(int)num2] = br.ReadInt16Big();
            num2 += 1U;
        }
        return array;
    }

    public static float[] ReadSingleArray(this BinaryReader br)
    {
        int num = (int)br.ReadCompressedUInt32();
        int num2 = num * 4;
        float[] array = new float[num];
        byte[] bufferAndAdvance = br.ReadBytes(num2);
        Buffer.BlockCopy(bufferAndAdvance, 0, array, 0, num2);
        return array;
    }

    public static double[] ReadDoubleArray(this BinaryReader br)
    {
        int num = (int)br.ReadCompressedUInt32();
        int num2 = num * 8;
        double[] array = new double[num];
        byte[] bufferAndAdvance = br.ReadBytes(num2);
        Buffer.BlockCopy(bufferAndAdvance, 0, array, 0, num2);
        return array;
    }

    public static string[] ReadStringArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        string[] array = new string[num];
        uint num2 = 0U;
        while ((ulong)num2 < (ulong)((long)array.Length))
        {
            array[(int)num2] = br.ReadString();
            num2 += 1U;
        }
        return array;
    }

    public static Hashtable[] ReadHashtableArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        Hashtable[] array = new Hashtable[num];
        for (uint num2 = 0U; num2 < num; num2 += 1U)
        {
            array[(int)num2] = br.ReadHashtable();
        }
        return array;
    }

    public static IDictionary[] ReadDictionaryArray(this BinaryReader br)
    {
        GpType gpType;
        GpType gpType2;
        Type type = br.ReadDictionaryType(out gpType, out gpType2);
        uint num = br.ReadCompressedUInt32();
        IDictionary[] array = (IDictionary[])Array.CreateInstance(type, (int)num);
        for (uint num2 = 0U; num2 < num; num2 += 1U)
        {
            array[(int)num2] = (IDictionary)Activator.CreateInstance(type);
            br.ReadDictionaryElements( gpType, gpType2, array[(int)num2]);
        }
        return array;
    }


    public static Array ReadArrayInArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        Array array = null;
        Type type = null;
        for (uint num2 = 0U; num2 < num; num2 += 1U)
        {
            object obj = br.ReadObject();
            Array array2 = obj as Array;
            bool flag = array2 != null;
            if (flag)
            {
                bool flag2 = array == null;
                if (flag2)
                {
                    type = array2.GetType();
                    array = Array.CreateInstance(type, (int)num);
                }
                bool flag3 = type.IsAssignableFrom(array2.GetType());
                if (flag3)
                {
                    array.SetValue(array2, (int)num2);
                }
            }
        }
        return array;
    }

    public static byte[] ReadByteArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        byte[] array = new byte[num];
        br.Read(array, 0, (int)num);
        return array;
    }

    public static int[] ReadCompressedInt32Array(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        int[] array = new int[num];
        uint num2 = 0U;
        while ((ulong)num2 < (ulong)((long)array.Length))
        {
            array[(int)num2] = br.ReadCompressedInt32();
            num2 += 1U;
        }
        return array;
    }

    public static long[] ReadCompressedInt64Array(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        long[] array = new long[num];
        uint num2 = 0U;
        while ((ulong)num2 < (ulong)((long)array.Length))
        {
            array[(int)num2] = br.ReadCompressedInt64();
            num2 += 1U;
        }
        return array;
    }

    public static object ReadCustomTypeArray(this BinaryReader br)
    {
        uint num = br.ReadCompressedUInt32();
        byte b = br.ReadByte();
        if (!BinaryDataHelper.CustomBinaryData.TryGetValue(b, out var data))
        {
            int position = (int)br.BaseStream.Position;
            for (uint num2 = 0U; num2 < num; num2 += 1U)
            {
                int num3 = (int)br.ReadCompressedUInt32();
                int available = br.PeekChar();
                int num4 = (num3 > available) ? available : num3;
                br.ReadBytes(num4);
            }
            return new UnknownType[]
            {
                    new UnknownType
                    {
                        TypeCode = b,
                        Size = (int)(br.BaseStream.Position - position)
                    }
            };
        }
        Array array = Array.CreateInstance(data.Type, (int)num);
        for (uint num5 = 0U; num5 < num; num5 += 1U)
        {
            int num6 = (int)br.ReadCompressedUInt32();
            bool flag2 = num6 < 0;
            if (flag2)
            {
                throw new InvalidDataException("ReadCustomTypeArray read negative size value: " + num6.ToString());
            }
            bool flag3 = num6 > br.PeekChar() || num6 > 32767;
            if (flag3)
            {
                br.BaseStream.Position = br.BaseStream.Length;
                throw new InvalidDataException("ReadCustomTypeArray read size value: " + num6.ToString());
            }
            object obj2 = data.ReadToObject(br);
            bool flag6 = obj2 != null && data.Type.IsAssignableFrom(obj2.GetType());
            if (flag6)
            {
                array.SetValue(obj2, (int)num5);
            }
        }
        return array;
    }

    #endregion
    #region CustomType
    public static object ReadCustomType(this BinaryReader br, byte gpType = 0)
    {
        bool flag = gpType == 0;
        byte b;
        if (gpType == 0)
        {
            b = br.ReadByte();
        }
        else
        {
            b = (byte)(gpType - 128);
        }
        int num = (int)br.ReadCompressedUInt32();
        if (num < 0)
        {
            throw new InvalidDataException("ReadCustomType read negative size value: " + num.ToString());
        }

        if (num > 32767 || !BinaryDataHelper.CustomBinaryData.TryGetValue(b, out var data))
        {
            UnknownType unknownType = new();
            return unknownType.ReadToObject(br);
        }
        return data.ReadToObject(br);
    }



    #endregion

    public static object? ReadObject(this BinaryReader br)
    {
        return br.ReadObject(br.ReadByte());
    }

    public static object? ReadObject(this BinaryReader br, byte gpType)
    {
        int num = (gpType >= 128) ? (gpType - 128) : gpType;
        int num2 = (num >= 64) ? (num - 64) : num;
        bool flag2 = gpType >= 128 && gpType <= 228;
        if (!flag2)
        {
            switch (gpType)
            {
                case 2:
                    {
                        return br.ReadBoolean();
                    }
                case 3:
                    {
                        return br.ReadByte();
                    }
                case 4:
                    {
                        return br.ReadInt16Big();
                    }
                case 5:
                    {
                        return br.ReadSingle();
                    }
                case 6:
                    {
                        return br.ReadDouble();
                    }
                case 7:
                    return br.ReadString();
                case 8:
                    return null;
                case 9:
                    {
                        return br.ReadCompressedInt32();
                    }
                case 10:
                    {
                        return br.ReadCompressedInt64();
                    }
                case 11:
                    {
                        return br.ReadInt1(false);
                    }
                case 12:
                    {
                        return br.ReadInt1(true);
                    }
                case 13:
                    {
                        return br.ReadInt2(false);
                    }
                case 14:
                    {
                        return br.ReadInt2(true);
                    }
                case 15:
                    {
                        return (long)br.ReadInt1(false);
                    }
                case 16:
                    {
                        return (long)br.ReadInt1(true);
                    }
                case 17:
                    {
                        return (long)br.ReadInt2(false);
                    }
                case 18:
                    {
                        return (long)br.ReadInt2(true);
                    }
                case 19:
                    return br.ReadCustomType(0);
                case 20:
                    return br.ReadDictionary();
                case 21:
                    return br.ReadHashtable();
                case 23:
                    return br.ReadObjectArray();
                case 24:
                    return br.ReadOperationRequest();
                case 25:
                    return br.ReadOperationResponse();
                case 26:
                    return br.ReadEventData();
                case 27:
                    {
                        return false;
                    }
                case 28:
                    {
                        return true;
                    }
                case 29:
                    {
                        return (short)0;
                    }
                case 30:
                    {
                        return 0;
                    }
                case 31:
                    {
                        return 0L;
                    }
                case 32:
                    {
                        return 0f;
                    }
                case 33:
                    {
                        return 0.0;
                    }
                case 34:
                    {
                        return (byte)0;
                    }
                case 64:
                    return br.ReadArrayInArray();
                case 66:
                    return br.ReadBooleanArray();
                case 67:
                    return br.ReadByteArray();
                case 68:
                    return br.ReadInt16Array();
                case 69:
                    return br.ReadSingleArray();
                case 70:
                    return br.ReadDoubleArray();
                case 71:
                    return br.ReadStringArray();
                case 73:
                    return br.ReadCompressedInt32Array();
                case 74:
                    return br.ReadCompressedInt64Array();
                case 83:
                    return br.ReadCustomTypeArray();
                case 84:
                    return br.ReadDictionaryArray();
                case 85:
                    return br.ReadHashtableArray();
            }
            throw new InvalidDataException(string.Format("GpTypeCode not found: {0}(0x{0:X}). Is not a CustomType either.", gpType));
        }
        return br.ReadCustomType(gpType);
    }
}
