namespace FakePhotonLib.PhotonRelated;

public class StreamBuffer
{
    public StreamBuffer(int size = 0)
    {
        this.buf = new byte[size];
    }

    public StreamBuffer(byte[] buf)
    {
        this.buf = buf;
        this.len = buf.Length;
    }

    public byte[] ToArray()
    {
        byte[] array = new byte[this.len];
        Buffer.BlockCopy(this.buf, 0, array, 0, this.len);
        return array;
    }

    public byte[] ToArrayFromPos()
    {
        int num = this.len - this.pos;
        bool flag = num <= 0;
        byte[] array;
        if (flag)
        {
            array = [];
        }
        else
        {
            byte[] array2 = new byte[num];
            Buffer.BlockCopy(this.buf, this.pos, array2, 0, num);
            array = array2;
        }
        return array;
    }

    public void Compact()
    {
        long num = (long)(this.Length - this.Position);
        bool flag = num > 0L;
        if (flag)
        {
            Buffer.BlockCopy(this.buf, this.Position, this.buf, 0, (int)num);
        }
        this.Position = 0;
        this.SetLength(num);
    }

    public byte[] GetBuffer()
    {
        return this.buf;
    }

    public byte[] GetBufferAndAdvance(int length, out int offset)
    {
        offset = this.Position;
        this.Position += length;
        return this.buf;
    }

    public bool CanRead
    {
        get
        {
            return true;
        }
    }

    public bool CanSeek
    {
        get
        {
            return true;
        }
    }

    public bool CanWrite
    {
        get
        {
            return true;
        }
    }

    public int Length
    {
        get
        {
            return this.len;
        }
    }

    public int Position
    {
        get
        {
            return this.pos;
        }
        set
        {
            this.pos = value;
            bool flag = this.len < this.pos;
            if (flag)
            {
                this.len = this.pos;
                this.CheckSize(this.len);
            }
        }
    }

    public int Available
    {
        get
        {
            int num = this.len - this.pos;
            return (num < 0) ? 0 : num;
        }
    }

    public void Flush()
    {
        buf = [];
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        int num;
        switch (origin)
        {
            case SeekOrigin.Begin:
                num = (int)offset;
                break;
            case SeekOrigin.Current:
                num = this.pos + (int)offset;
                break;
            case SeekOrigin.End:
                num = this.len + (int)offset;
                break;
            default:
                throw new ArgumentException("Invalid seek origin");
        }
        bool flag = num < 0;
        if (flag)
        {
            throw new ArgumentException("Seek before begin");
        }
        bool flag2 = num > this.len;
        if (flag2)
        {
            throw new ArgumentException("Seek after end");
        }
        this.pos = num;
        return (long)this.pos;
    }

    public void SetLength(long value)
    {
        this.len = (int)value;
        this.CheckSize(this.len);
        bool flag = this.pos > this.len;
        if (flag)
        {
            this.pos = this.len;
        }
    }

    public void SetCapacityMinimum(int neededSize)
    {
        this.CheckSize(neededSize);
    }

    public int Read(byte[] buffer, int dstOffset, int count)
    {
        int num = this.len - this.pos;
        bool flag = num <= 0;
        int num2;
        if (flag)
        {
            num2 = 0;
        }
        else
        {
            bool flag2 = count > num;
            if (flag2)
            {
                count = num;
            }
            Buffer.BlockCopy(this.buf, this.pos, buffer, dstOffset, count);
            this.pos += count;
            num2 = count;
        }
        return num2;
    }

    public void Write(byte[] buffer, int srcOffset, int count)
    {
        int num = this.pos + count;
        this.CheckSize(num);
        bool flag = num > this.len;
        if (flag)
        {
            this.len = num;
        }
        Buffer.BlockCopy(buffer, srcOffset, this.buf, this.pos, count);
        this.pos = num;
    }

    public byte ReadByte()
    {
        bool flag = this.pos >= this.len;
        if (flag)
        {
            throw new EndOfStreamException("SteamBuffer.ReadByte() failed. pos:" + this.pos.ToString() + " len:" + this.len.ToString());
        }
        byte[] array = this.buf;
        int num = this.pos;
        this.pos = num + 1;
        return array[num];
    }

    public void WriteByte(byte value)
    {
        bool flag = this.pos >= this.len;
        if (flag)
        {
            this.len = this.pos + 1;
            this.CheckSize(this.len);
        }
        byte[] array = this.buf;
        int num = this.pos;
        this.pos = num + 1;
        array[num] = value;
    }

    public void WriteBytes(byte v0, byte v1)
    {
        int num = this.pos + 2;
        bool flag = this.len < num;
        if (flag)
        {
            this.len = num;
            this.CheckSize(this.len);
        }
        byte[] array = this.buf;
        int num2 = this.pos;
        this.pos = num2 + 1;
        array[num2] = v0;
        byte[] array2 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array2[num2] = v1;
    }

    public void WriteBytes(byte v0, byte v1, byte v2)
    {
        int num = this.pos + 3;
        bool flag = this.len < num;
        if (flag)
        {
            this.len = num;
            this.CheckSize(this.len);
        }
        byte[] array = this.buf;
        int num2 = this.pos;
        this.pos = num2 + 1;
        array[num2] = v0;
        byte[] array2 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array2[num2] = v1;
        byte[] array3 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array3[num2] = v2;
    }

    public void WriteBytes(byte v0, byte v1, byte v2, byte v3)
    {
        int num = this.pos + 4;
        bool flag = this.len < num;
        if (flag)
        {
            this.len = num;
            this.CheckSize(this.len);
        }
        byte[] array = this.buf;
        int num2 = this.pos;
        this.pos = num2 + 1;
        array[num2] = v0;
        byte[] array2 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array2[num2] = v1;
        byte[] array3 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array3[num2] = v2;
        byte[] array4 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array4[num2] = v3;
    }

    public void WriteBytes(byte v0, byte v1, byte v2, byte v3, byte v4, byte v5, byte v6, byte v7)
    {
        int num = this.pos + 8;
        bool flag = this.len < num;
        if (flag)
        {
            this.len = num;
            this.CheckSize(this.len);
        }
        byte[] array = this.buf;
        int num2 = this.pos;
        this.pos = num2 + 1;
        array[num2] = v0;
        byte[] array2 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array2[num2] = v1;
        byte[] array3 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array3[num2] = v2;
        byte[] array4 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array4[num2] = v3;
        byte[] array5 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array5[num2] = v4;
        byte[] array6 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array6[num2] = v5;
        byte[] array7 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array7[num2] = v6;
        byte[] array8 = this.buf;
        num2 = this.pos;
        this.pos = num2 + 1;
        array8[num2] = v7;
    }

    private bool CheckSize(int size)
    {
        bool flag = size <= this.buf.Length;
        bool flag2;
        if (flag)
        {
            flag2 = false;
        }
        else
        {
            int num = this.buf.Length;
            bool flag3 = num == 0;
            if (flag3)
            {
                num = 1;
            }
            while (size > num)
            {
                num *= 2;
            }
            byte[] array = new byte[num];
            Buffer.BlockCopy(this.buf, 0, array, 0, this.buf.Length);
            this.buf = array;
            flag2 = true;
        }
        return flag2;
    }

    private const int DefaultInitialSize = 0;
    private int pos;
    private int len;
    private byte[] buf;
}
