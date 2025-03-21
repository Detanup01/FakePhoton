using Serilog;

namespace FakePhotonLib.PhotonRelated;

public class StreamBuffer
{
    private int pos;
    private int len;
    private byte[] buf;

    public StreamBuffer(int size = 0) => buf = new byte[size];

    public StreamBuffer(byte[] buf)
    {
        this.buf = buf;
        len = buf.Length;
    }

    public byte[] ToArray()
    {
        var array = new byte[len];
        Buffer.BlockCopy(buf, 0, array, 0, len);
        return array;
    }

    public byte[] ToArrayFromPos()
    {
        int num = len - pos;
        if (num <= 0) return [];
        var array = new byte[num];
        Buffer.BlockCopy(buf, pos, array, 0, num);
        return array;
    }

    public void Compact()
    {
        long num = Length - Position;
        if (num > 0L)
            Buffer.BlockCopy(buf, Position, buf, 0, (int)num);
        Position = 0;
        SetLength(num);
    }

    public byte[] GetBuffer() => buf;

    public byte[] GetBufferAndAdvance(int length, out int offset)
    {
        offset = Position;
        Position += length;
        return buf;
    }

    public static bool CanRead => true;
    public static bool CanSeek => true;
    public static bool CanWrite => true;
    public int Length => len;

    public int Position
    {
        get => pos;
        set
        {
            pos = value;
            if (len < pos)
            {
                len = pos;
                CheckSize(len);
            }
        }
    }

    public int Available => Math.Max(0, len - pos);

    public void Flush() => buf = [];

    public long Seek(long offset, SeekOrigin origin)
    {
        int num = origin switch
        {
            SeekOrigin.Begin => (int)offset,
            SeekOrigin.Current => pos + (int)offset,
            SeekOrigin.End => len + (int)offset,
            _ => throw new ArgumentException("Invalid seek origin")
        };
        if (num < 0) throw new ArgumentException("Seek before begin");
        if (num > len) throw new ArgumentException("Seek after end");
        pos = num;
        return pos;
    }

    public void SetLength(long value)
    {
        len = (int)value;
        CheckSize(len);
        if (pos > len) pos = len;
    }

    public void SetCapacityMinimum(int neededSize) => CheckSize(neededSize);

    public int Read(byte[] buffer, int dstOffset, int count)
    {
        int num = len - pos;
        if (num <= 0) return 0;
        if (count > num) count = num;
        Buffer.BlockCopy(buf, pos, buffer, dstOffset, count);
        pos += count;
        return count;
    }

    public void Write(byte[] buffer, int srcOffset, int count)
    {
        int num = pos + count;
        CheckSize(num);
        if (num > len) len = num;
        Log.Information("{srcOffset}, {pos}, {count}, {bufferLen} {bufLen}", srcOffset, pos, count, buffer.Length, buf.Length);
        Buffer.BlockCopy(buffer, srcOffset, buf, pos, count);
        pos = num;
    }

    public byte ReadByte()
    {
        if (pos >= len)
            throw new EndOfStreamException($"StreamBuffer.ReadByte() failed. pos:{pos} len:{len}");
        return buf[pos++];
    }

    public void WriteByte(byte value)
    {
        if (pos >= len)
        {
            len = pos + 1;
            CheckSize(len);
        }
        buf[pos++] = value;
    }

    public void WriteBytes(params byte[] values)
    {
        int num = pos + values.Length;
        if (len < num)
        {
            len = num;
            CheckSize(len);
        }
        Buffer.BlockCopy(values, 0, buf, pos, values.Length);
        pos += values.Length;
    }

    private bool CheckSize(int size)
    {
        if (size <= buf.Length) return false;
        int newSize = buf.Length == 0 ? 1 : buf.Length;
        while (size > newSize) newSize *= 2;
        Array.Resize(ref buf, newSize);
        return true;
    }
}
