using System.Security.Cryptography;

namespace FakePhotonLib.PhotonRelated;

public class BigIntegerPhoton
{
    private uint length = 1;
    private uint[] data;
    private const uint DEFAULT_LEN = 20;
    private const string WouldReturnNegVal = "Operation would return a negative value";
    private static RandomNumberGenerator rng = RandomNumberGenerator.Create();

    public BigIntegerPhoton()
    {
        this.data = new uint[DEFAULT_LEN];
        this.length = DEFAULT_LEN;
    }

    public BigIntegerPhoton(BigIntegerPhoton.Sign sign, uint len)
    {
        this.data = new uint[(int)len];
        this.length = len;
    }

    public BigIntegerPhoton(BigIntegerPhoton bi)
    {
        this.data = (uint[])bi.data.Clone();
        this.length = bi.length;
    }

    public BigIntegerPhoton(BigIntegerPhoton bi, uint len)
    {
        this.data = new uint[(int)len];
        for (uint index = 0; index < bi.length; ++index)
            this.data[(int)index] = bi.data[(int)index];
        this.length = bi.length;
    }

    public BigIntegerPhoton(byte[] inData)
    {
        this.length = (uint)(inData.Length >>> 2);
        int num = inData.Length & 3;
        if (num != 0)
            ++this.length;
        this.data = new uint[(int)this.length];
        int index1 = inData.Length - 1;
        int index2 = 0;
        while (index1 >= 3)
        {
            this.data[index2] = (uint)((int)inData[index1 - 3] << 24 | (int)inData[index1 - 2] << 16 | (int)inData[index1 - 1] << 8) | (uint)inData[index1];
            index1 -= 4;
            ++index2;
        }
        switch (num)
        {
            case 1:
                this.data[(int)this.length - 1] = (uint)inData[0];
                break;
            case 2:
                this.data[(int)this.length - 1] = (uint)inData[0] << 8 | (uint)inData[1];
                break;
            case 3:
                this.data[(int)this.length - 1] = (uint)((int)inData[0] << 16 | (int)inData[1] << 8) | (uint)inData[2];
                break;
        }
        this.Normalize();
    }

    public BigIntegerPhoton(uint[] inData)
    {
        this.length = (uint)inData.Length;
        this.data = new uint[(int)this.length];
        int index1 = (int)this.length - 1;
        int index2 = 0;
        while (index1 >= 0)
        {
            this.data[index2] = inData[index1];
            --index1;
            ++index2;
        }
        this.Normalize();
    }

    public BigIntegerPhoton(uint ui)
    {
        this.data = new uint[1] { ui };
    }

    public BigIntegerPhoton(ulong ul)
    {
        this.data = new uint[2]
        {
        (uint) ul,
        (uint) (ul >> 32)
        };
        this.length = 2U;
        this.Normalize();
    }

    public static implicit operator BigIntegerPhoton(uint value) => new BigIntegerPhoton(value);

    public static implicit operator BigIntegerPhoton(int value)
    {
        return value >= 0 ? new BigIntegerPhoton((uint)value) : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public static implicit operator BigIntegerPhoton(ulong value) => new BigIntegerPhoton(value);

    public static BigIntegerPhoton Parse(string number)
    {
        if (number == null)
            throw new ArgumentNullException(nameof(number));
        int index1 = 0;
        int length = number.Length;
        bool flag = false;
        BigIntegerPhoton bigInteger = new BigIntegerPhoton(0U);
        if (number[index1] == '+')
            ++index1;
        else if (number[index1] == '-')
            throw new FormatException("Operation would return a negative value");
        for (; index1 < length; ++index1)
        {
            char c = number[index1];
            switch (c)
            {
                case char.MinValue:
                    index1 = length;
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    bigInteger = bigInteger * 10 + (BigIntegerPhoton)((int)c - 48);
                    flag = true;
                    break;
                default:
                    if (!char.IsWhiteSpace(c))
                        throw new FormatException();
                    for (int index2 = index1 + 1; index2 < length; ++index2)
                    {
                        if (!char.IsWhiteSpace(number[index2]))
                            throw new FormatException();
                    }
                    goto label_18;
            }
        }
    label_18:
        if (!flag)
            throw new FormatException();
        return bigInteger;
    }

    public static BigIntegerPhoton operator +(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        if (bi1 == 0U)
            return new BigIntegerPhoton(bi2);
        return bi2 == 0U ? new BigIntegerPhoton(bi1) : BigIntegerPhoton.Kernel.AddSameSign(bi1, bi2);
    }

    public static BigIntegerPhoton operator -(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        if (bi2 == 0U)
            return new BigIntegerPhoton(bi1);
        if (bi1 == 0U)
            throw new ArithmeticException("Operation would return a negative value");
        switch (BigIntegerPhoton.Kernel.Compare(bi1, bi2))
        {
            case BigIntegerPhoton.Sign.Negative:
                throw new ArithmeticException("Operation would return a negative value");
            case BigIntegerPhoton.Sign.Zero:
                return (BigIntegerPhoton)0;
            case BigIntegerPhoton.Sign.Positive:
                return BigIntegerPhoton.Kernel.Subtract(bi1, bi2);
            default:
                throw new Exception();
        }
    }

    public static int operator %(BigIntegerPhoton bi, int i)
    {
        return i > 0 ? (int)BigIntegerPhoton.Kernel.DwordMod(bi, (uint)i) : -(int)BigIntegerPhoton.Kernel.DwordMod(bi, (uint)-i);
    }

    public static uint operator %(BigIntegerPhoton bi, uint ui) => BigIntegerPhoton.Kernel.DwordMod(bi, ui);

    public static BigIntegerPhoton operator %(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        return BigIntegerPhoton.Kernel.multiByteDivide(bi1, bi2)[1];
    }

    public static BigIntegerPhoton operator /(BigIntegerPhoton bi, int i)
    {
        return i > 0 ? BigIntegerPhoton.Kernel.DwordDiv(bi, (uint)i) : throw new ArithmeticException("Operation would return a negative value");
    }

    public static BigIntegerPhoton operator /(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        return BigIntegerPhoton.Kernel.multiByteDivide(bi1, bi2)[0];
    }

    public static BigIntegerPhoton operator *(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        if (bi1 == 0U || bi2 == 0U)
            return (BigIntegerPhoton)0;
        if ((long)bi1.data.Length < (long)bi1.length)
            throw new IndexOutOfRangeException("bi1 out of range");
        if ((long)bi2.data.Length < (long)bi2.length)
            throw new IndexOutOfRangeException("bi2 out of range");
        BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, bi1.length + bi2.length);
        BigIntegerPhoton.Kernel.Multiply(bi1.data, 0U, bi1.length, bi2.data, 0U, bi2.length, bigInteger.data, 0U);
        bigInteger.Normalize();
        return bigInteger;
    }

    public static BigIntegerPhoton operator *(BigIntegerPhoton bi, int i)
    {
        if (i < 0)
            throw new ArithmeticException("Operation would return a negative value");
        if (i == 0)
            return (BigIntegerPhoton)0;
        return i == 1 ? new BigIntegerPhoton(bi) : BigIntegerPhoton.Kernel.MultiplyByDword(bi, (uint)i);
    }

    public static BigIntegerPhoton operator <<(BigIntegerPhoton bi1, int shiftVal)
    {
        return BigIntegerPhoton.Kernel.LeftShift(bi1, shiftVal);
    }

    public static BigIntegerPhoton operator >>(BigIntegerPhoton bi1, int shiftVal)
    {
        return BigIntegerPhoton.Kernel.RightShift(bi1, shiftVal);
    }

    public static BigIntegerPhoton Add(BigIntegerPhoton bi1, BigIntegerPhoton bi2) => bi1 + bi2;

    public static BigIntegerPhoton Subtract(BigIntegerPhoton bi1, BigIntegerPhoton bi2) => bi1 - bi2;

    public static int Modulus(BigIntegerPhoton bi, int i) => bi % i;

    public static uint Modulus(BigIntegerPhoton bi, uint ui) => bi % ui;

    public static BigIntegerPhoton Modulus(BigIntegerPhoton bi1, BigIntegerPhoton bi2) => bi1 % bi2;

    public static BigIntegerPhoton Divid(BigIntegerPhoton bi, int i) => bi / i;

    public static BigIntegerPhoton Divid(BigIntegerPhoton bi1, BigIntegerPhoton bi2) => bi1 / bi2;

    public static BigIntegerPhoton Multiply(BigIntegerPhoton bi1, BigIntegerPhoton bi2) => bi1 * bi2;

    public static BigIntegerPhoton Multiply(BigIntegerPhoton bi, int i) => bi * i;

    private static RandomNumberGenerator Rng
    {
        get
        {
            if (BigIntegerPhoton.rng == null)
                BigIntegerPhoton.rng = RandomNumberGenerator.Create();
            return BigIntegerPhoton.rng;
        }
    }

    public static BigIntegerPhoton GenerateRandom(int bits, RandomNumberGenerator rng)
    {
        int num1 = bits >> 5;
        int num2 = bits & 31;
        if (num2 != 0)
            ++num1;
        BigIntegerPhoton random = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, (uint)(num1 + 1));
        byte[] numArray = new byte[num1 << 2];
        rng.GetBytes(numArray);
        Buffer.BlockCopy((Array)numArray, 0, (Array)random.data, 0, num1 << 2);
        if (num2 != 0)
        {
            uint num3 = (uint)(1 << num2 - 1);
            random.data[num1 - 1] |= num3;
            uint num4 = uint.MaxValue >> 32 - num2;
            random.data[num1 - 1] &= num4;
        }
        else
            random.data[num1 - 1] |= 2147483648U;
        random.Normalize();
        return random;
    }

    public static BigIntegerPhoton GenerateRandom(int bits)
    {
        return BigIntegerPhoton.GenerateRandom(bits, BigIntegerPhoton.Rng);
    }

    public void Randomize(RandomNumberGenerator rng)
    {
        if (this == 0U)
            return;
        int num1 = this.BitCount();
        int num2 = num1 >> 5;
        int num3 = num1 & 31;
        if (num3 != 0)
            ++num2;
        byte[] numArray = new byte[num2 << 2];
        rng.GetBytes(numArray);
        Buffer.BlockCopy((Array)numArray, 0, (Array)this.data, 0, num2 << 2);
        if (num3 != 0)
        {
            uint num4 = (uint)(1 << num3 - 1);
            this.data[num2 - 1] |= num4;
            uint num5 = uint.MaxValue >> 32 - num3;
            this.data[num2 - 1] &= num5;
        }
        else
            this.data[num2 - 1] |= 2147483648U;
        this.Normalize();
    }

    public void Randomize() => this.Randomize(BigIntegerPhoton.Rng);

    public int BitCount()
    {
        this.Normalize();
        uint num1 = this.data[(int)this.length - 1];
        uint num2 = 2147483648;
        uint num3;
        for (num3 = 32U; num3 > 0U && ((int)num1 & (int)num2) == 0; num2 >>= 1)
            --num3;
        return (int)(num3 + (uint)((int)this.length - 1 << 5));
    }

    public bool TestBit(uint bitNum)
    {
        return (this.data[(int)(bitNum >> 5)] & 1U << (int)(byte)(bitNum & 31U)) > 0U;
    }

    public bool TestBit(int bitNum)
    {
        if (bitNum < 0)
            throw new IndexOutOfRangeException("bitNum out of range");
        uint index = (uint)(bitNum >>> 5);
        uint num = 1U << (int)(byte)(bitNum & 31);
        return ((int)this.data[(int)index] | (int)num) == (int)this.data[(int)index];
    }

    public void SetBit(uint bitNum) => this.SetBit(bitNum, true);

    public void ClearBit(uint bitNum) => this.SetBit(bitNum, false);

    public void SetBit(uint bitNum, bool value)
    {
        uint index = bitNum >> 5;
        if (index >= this.length)
            return;
        uint num = 1U << (int)bitNum;
        if (value)
            this.data[(int)index] |= num;
        else
            this.data[(int)index] &= ~num;
    }

    public int LowestSetBit()
    {
        if (this == 0U)
            return -1;
        int bitNum = 0;
        while (!this.TestBit(bitNum))
            ++bitNum;
        return bitNum;
    }

    public byte[] GetBytes()
    {
        if (this == 0U)
            return new byte[1];
        int num1 = this.BitCount();
        int length = num1 >> 3;
        if ((num1 & 7) != 0)
            ++length;
        byte[] bytes = new byte[length];
        int num2 = length & 3;
        if (num2 == 0)
            num2 = 4;
        int num3 = 0;
        for (int index1 = (int)this.length - 1; index1 >= 0; --index1)
        {
            uint num4 = this.data[index1];
            for (int index2 = num2 - 1; index2 >= 0; --index2)
            {
                bytes[num3 + index2] = (byte)(num4 & (uint)byte.MaxValue);
                num4 >>= 8;
            }
            num3 += num2;
            num2 = 4;
        }
        return bytes;
    }

    public static bool operator ==(BigIntegerPhoton bi1, uint ui)
    {
        if (bi1.length != 1U)
            bi1.Normalize();
        return bi1.length == 1U && (int)bi1.data[0] == (int)ui;
    }

    public static bool operator !=(BigIntegerPhoton bi1, uint ui)
    {
        if (bi1.length != 1U)
            bi1.Normalize();
        return bi1.length != 1U || (int)bi1.data[0] != (int)ui;
    }

    public static bool operator ==(BigIntegerPhoton? bi1, BigIntegerPhoton? bi2)
    {
        if (bi1 == bi2)
            return true;
        return !(null == bi1) && !(null == bi2) && BigIntegerPhoton.Kernel.Compare(bi1, bi2) == BigIntegerPhoton.Sign.Zero;
    }

    public static bool operator !=(BigIntegerPhoton? bi1, BigIntegerPhoton? bi2)
    {
        if (bi1 == bi2)
            return false;
        return null == bi1 || null == bi2 || BigIntegerPhoton.Kernel.Compare(bi1, bi2) != 0;
    }

    public static bool operator >(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        return BigIntegerPhoton.Kernel.Compare(bi1, bi2) > BigIntegerPhoton.Sign.Zero;
    }

    public static bool operator <(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        return BigIntegerPhoton.Kernel.Compare(bi1, bi2) < BigIntegerPhoton.Sign.Zero;
    }

    public static bool operator >=(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        return BigIntegerPhoton.Kernel.Compare(bi1, bi2) >= BigIntegerPhoton.Sign.Zero;
    }

    public static bool operator <=(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
    {
        return BigIntegerPhoton.Kernel.Compare(bi1, bi2) <= BigIntegerPhoton.Sign.Zero;
    }

    public BigIntegerPhoton.Sign Compare(BigIntegerPhoton bi) => BigIntegerPhoton.Kernel.Compare(this, bi);

    public string ToString(uint radix)
    {
        return this.ToString(radix, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    }

    public string ToString(uint radix, string characterSet)
    {
        if ((long)characterSet.Length < (long)radix)
            throw new ArgumentException("charSet length less than radix", nameof(characterSet));
        if (radix == 1U)
            throw new ArgumentException("There is no such thing as radix one notation", nameof(radix));
        if (this == 0U)
            return "0";
        if (this == 1U)
            return "1";
        string str = "";
        BigIntegerPhoton n = new BigIntegerPhoton(this);
        while (n != 0U)
        {
            uint index = BigIntegerPhoton.Kernel.SingleByteDivideInPlace(n, radix);
            str = characterSet[(int)index].ToString() + str;
        }
        return str;
    }

    private void Normalize()
    {
        while (this.length > 0U && this.data[(int)this.length - 1] == 0U)
            --this.length;
        if (this.length != 0U)
            return;
        ++this.length;
    }

    public void Clear()
    {
        for (int index = 0; (long)index < (long)this.length; ++index)
            this.data[index] = 0U;
    }

    public override int GetHashCode()
    {
        uint hashCode = 0;
        for (uint index = 0; index < this.length; ++index)
            hashCode ^= this.data[(int)index];
        return (int)hashCode;
    }

    public override string ToString() => this.ToString(10U);

    public override bool Equals(object? o)
    {
        if (o == null)
            return false;
        if (!(o is int num))
            return BigIntegerPhoton.Kernel.Compare(this, (BigIntegerPhoton)o) == BigIntegerPhoton.Sign.Zero;
        return num >= 0 && this == (uint)o;
    }

    public BigIntegerPhoton GCD(BigIntegerPhoton bi) => BigIntegerPhoton.Kernel.gcd(this, bi);

    public BigIntegerPhoton ModInverse(BigIntegerPhoton modulus) => BigIntegerPhoton.Kernel.modInverse(this, modulus);

    public BigIntegerPhoton ModPow(BigIntegerPhoton exp, BigIntegerPhoton n)
    {
        return new BigIntegerPhoton.ModulusRing(n).Pow(this, exp);
    }

    public void Incr2()
    {
        int num = 0;
        this.data[0] += 2U;
        if (this.data[0] >= 2U)
            return;
        int index;
        ++this.data[index = num + 1];
        while (this.data[index++] == 0U)
            ++this.data[index];
        if ((int)this.length != index)
            return;
        ++this.length;
    }

    public enum Sign
    {
        Negative = -1, // 0xFFFFFFFF
        Zero = 0,
        Positive = 1,
    }

    public sealed class ModulusRing
    {
        private BigIntegerPhoton mod;
        private BigIntegerPhoton constant;

        public ModulusRing(BigIntegerPhoton modulus)
        {
            this.mod = modulus;
            uint index = this.mod.length << 1;
            this.constant = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, index + 1U);
            this.constant.data[(int)index] = 1U;
            this.constant /= this.mod;
        }

        public void BarrettReduction(BigIntegerPhoton x)
        {
            BigIntegerPhoton mod = this.mod;
            uint length = mod.length;
            uint index = length + 1U;
            uint xOffset = length - 1U;
            if (x.length < length)
                return;
            if ((long)x.data.Length < (long)x.length)
                throw new IndexOutOfRangeException("x out of range");
            BigIntegerPhoton bigInteger1 = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, x.length - xOffset + this.constant.length);
            BigIntegerPhoton.Kernel.Multiply(x.data, xOffset, x.length - xOffset, this.constant.data, 0U, this.constant.length, bigInteger1.data, 0U);
            uint num = x.length > index ? index : x.length;
            x.length = num;
            x.Normalize();
            BigIntegerPhoton small = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, index);
            BigIntegerPhoton.Kernel.MultiplyMod2p32pmod(bigInteger1.data, (int)index, (int)bigInteger1.length - (int)index, mod.data, 0, (int)mod.length, small.data, 0, (int)index);
            small.Normalize();
            if (small <= x)
            {
                BigIntegerPhoton.Kernel.MinusEq(x, small);
            }
            else
            {
                BigIntegerPhoton bigInteger2 = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, index + 1U);
                bigInteger2.data[(int)index] = 1U;
                BigIntegerPhoton.Kernel.MinusEq(bigInteger2, small);
                BigIntegerPhoton.Kernel.PlusEq(x, bigInteger2);
            }
            while (x >= mod)
                BigIntegerPhoton.Kernel.MinusEq(x, mod);
        }

        public BigIntegerPhoton Multiply(BigIntegerPhoton a, BigIntegerPhoton b)
        {
            if (a == 0U || b == 0U)
                return (BigIntegerPhoton)0;
            if (a.length >= this.mod.length << 1)
                a %= this.mod;
            if (b.length >= this.mod.length << 1)
                b %= this.mod;
            if (a.length >= this.mod.length)
                this.BarrettReduction(a);
            if (b.length >= this.mod.length)
                this.BarrettReduction(b);
            BigIntegerPhoton x = new BigIntegerPhoton(a * b);
            this.BarrettReduction(x);
            return x;
        }

        public BigIntegerPhoton Difference(BigIntegerPhoton a, BigIntegerPhoton b)
        {
            BigIntegerPhoton.Sign sign = BigIntegerPhoton.Kernel.Compare(a, b);
            BigIntegerPhoton x;
            switch (sign)
            {
                case BigIntegerPhoton.Sign.Negative:
                    x = b - a;
                    break;
                case BigIntegerPhoton.Sign.Zero:
                    return (BigIntegerPhoton)0;
                case BigIntegerPhoton.Sign.Positive:
                    x = a - b;
                    break;
                default:
                    throw new Exception();
            }
            if (x >= this.mod)
            {
                if (x.length >= this.mod.length << 1)
                    x %= this.mod;
                else
                    this.BarrettReduction(x);
            }
            if (sign == BigIntegerPhoton.Sign.Negative)
                x = this.mod - x;
            return x;
        }

        public BigIntegerPhoton Pow(BigIntegerPhoton b, BigIntegerPhoton exp)
        {
            return ((int)this.mod.data[0] & 1) == 1 ? this.OddPow(b, exp) : this.EvenPow(b, exp);
        }

        public BigIntegerPhoton EvenPow(BigIntegerPhoton b, BigIntegerPhoton exp)
        {
            BigIntegerPhoton x = new BigIntegerPhoton((BigIntegerPhoton)1, this.mod.length << 1);
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(b % this.mod, this.mod.length << 1);
            uint num = (uint)exp.BitCount();
            uint[] wkSpace = new uint[(int)this.mod.length << 1];
            for (uint bitNum = 0; bitNum < num; ++bitNum)
            {
                if (exp.TestBit(bitNum))
                {
                    Array.Clear((Array)wkSpace, 0, wkSpace.Length);
                    BigIntegerPhoton.Kernel.Multiply(x.data, 0U, x.length, bigInteger.data, 0U, bigInteger.length, wkSpace, 0U);
                    x.length += bigInteger.length;
                    uint[] numArray = wkSpace;
                    wkSpace = x.data;
                    x.data = numArray;
                    this.BarrettReduction(x);
                }
                BigIntegerPhoton.Kernel.SquarePositive(bigInteger, ref wkSpace);
                this.BarrettReduction(bigInteger);
                if (bigInteger == 1U)
                    return x;
            }
            return x;
        }

        private BigIntegerPhoton OddPow(BigIntegerPhoton b, BigIntegerPhoton exp)
        {
            BigIntegerPhoton n = new BigIntegerPhoton(BigIntegerPhoton.Montgomery.ToMont((BigIntegerPhoton)1, this.mod), this.mod.length << 1);
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Montgomery.ToMont(b, this.mod), this.mod.length << 1);
            uint mPrime = BigIntegerPhoton.Montgomery.Inverse(this.mod.data[0]);
            uint num = (uint)exp.BitCount();
            uint[] wkSpace = new uint[(int)this.mod.length << 1];
            for (uint bitNum = 0; bitNum < num; ++bitNum)
            {
                if (exp.TestBit(bitNum))
                {
                    Array.Clear((Array)wkSpace, 0, wkSpace.Length);
                    BigIntegerPhoton.Kernel.Multiply(n.data, 0U, n.length, bigInteger.data, 0U, bigInteger.length, wkSpace, 0U);
                    n.length += bigInteger.length;
                    uint[] numArray = wkSpace;
                    wkSpace = n.data;
                    n.data = numArray;
                    BigIntegerPhoton.Montgomery.Reduce(n, this.mod, mPrime);
                }
                BigIntegerPhoton.Kernel.SquarePositive(bigInteger, ref wkSpace);
                BigIntegerPhoton.Montgomery.Reduce(bigInteger, this.mod, mPrime);
            }
            BigIntegerPhoton.Montgomery.Reduce(n, this.mod, mPrime);
            return n;
        }

        public BigIntegerPhoton Pow(uint b, BigIntegerPhoton exp)
        {
            return ((int)this.mod.data[0] & 1) == 1 ? this.OddPow(b, exp) : this.EvenPow(b, exp);
        }

        private unsafe BigIntegerPhoton OddPow(uint b, BigIntegerPhoton exp)
        {
            exp.Normalize();
            uint[] wkSpace = new uint[(int)this.mod.length << 2];
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Montgomery.ToMont((BigIntegerPhoton)b, this.mod), this.mod.length << 2);
            uint mPrime = BigIntegerPhoton.Montgomery.Inverse(this.mod.data[0]);
            uint bitNum = (uint)(exp.BitCount() - 2);
            do
            {
                BigIntegerPhoton.Kernel.SquarePositive(bigInteger, ref wkSpace);
                bigInteger = BigIntegerPhoton.Montgomery.Reduce(bigInteger, this.mod, mPrime);
                if (exp.TestBit(bitNum))
                {
                    fixed (uint* numPtr = bigInteger.data)
                    {
                        uint num1 = 0;
                        ulong num2 = 0;
                        do
                        {
                            ulong num3 = num2 + (ulong)*(uint*)((IntPtr)numPtr + (IntPtr)((long)num1 * 4L)) * (ulong)b;
                            *(int*)((IntPtr)numPtr + (IntPtr)((long)num1 * 4L)) = (int)(uint)num3;
                            num2 = num3 >> 32;
                        }
                        while (++num1 < bigInteger.length);
                        if (bigInteger.length < this.mod.length)
                        {
                            if (num2 != 0UL)
                            {
                                *(int*)((IntPtr)numPtr + (IntPtr)((long)num1 * 4L)) = (int)(uint)num2;
                                ++bigInteger.length;
                                while (bigInteger >= this.mod)
                                    BigIntegerPhoton.Kernel.MinusEq(bigInteger, this.mod);
                            }
                        }
                        else if (num2 != 0UL)
                        {
                            uint num4 = (uint)num2;
                            uint num5 = this.mod.data[(int)this.mod.length - 1] >= uint.MaxValue ? (uint)(((ulong)num4 << 32 | (ulong)*(uint*)((IntPtr)numPtr + (IntPtr)((long)(num1 - 1U) * 4L))) / (ulong)this.mod.data[(int)this.mod.length - 1]) : (uint)(((ulong)num4 << 32 | (ulong)*(uint*)((IntPtr)numPtr + (IntPtr)((long)(num1 - 1U) * 4L))) / (ulong)(this.mod.data[(int)this.mod.length - 1] + 1U));
                            uint index1 = 0;
                            ulong num6 = 0;
                            do
                            {
                                ulong num7 = num6 + (ulong)this.mod.data[(int)index1] * (ulong)num5;
                                uint num8 = *(uint*)((IntPtr)numPtr + (IntPtr)((long)index1 * 4L));
                                IntPtr num9 = (IntPtr)numPtr + (IntPtr)((long)index1 * 4L);
                                *(int*)num9 = (int)*(uint*)num9 - (int)(uint)num7;
                                num6 = num7 >> 32;
                                if (*(uint*)((IntPtr)numPtr + (IntPtr)((long)index1 * 4L)) > num8)
                                    ++num6;
                                ++index1;
                            }
                            while (index1 < bigInteger.length);
                            uint num10 = num4 - (uint)num6;
                            if (num10 != 0U)
                            {
                                uint num11 = 0;
                                uint index2 = 0;
                                uint[] data = this.mod.data;
                                do
                                {
                                    uint num12;
                                    int num13 = (num12 = data[(int)index2] + num11) < num11 ? 1 : 0;
                                    IntPtr num14 = (IntPtr)numPtr + (IntPtr)((long)index2 * 4L);
                                    int num15;
                                    uint num16 = (uint)(num15 = (int)*(uint*)num14 - (int)num12);
                                    *(int*)num14 = num15;
                                    int num17 = num16 > ~num12 ? 1 : 0;
                                    num11 = (num13 | num17) == 0 ? 0U : 1U;
                                    ++index2;
                                }
                                while (index2 < bigInteger.length);
                                uint num18 = num10 - num11;
                            }
                            while (bigInteger >= this.mod)
                                BigIntegerPhoton.Kernel.MinusEq(bigInteger, this.mod);
                        }
                        else
                        {
                            while (bigInteger >= this.mod)
                                BigIntegerPhoton.Kernel.MinusEq(bigInteger, this.mod);
                        }
                    }
                }
            }
            while (bitNum-- > 0U);
            return BigIntegerPhoton.Montgomery.Reduce(bigInteger, this.mod, mPrime);
        }

        private unsafe BigIntegerPhoton EvenPow(uint b, BigIntegerPhoton exp)
        {
            exp.Normalize();
            uint[] wkSpace = new uint[(int)this.mod.length << 2];
            BigIntegerPhoton bigInteger = new BigIntegerPhoton((BigIntegerPhoton)b, this.mod.length << 2);
            uint bitNum = (uint)(exp.BitCount() - 2);
            do
            {
                BigIntegerPhoton.Kernel.SquarePositive(bigInteger, ref wkSpace);
                if (bigInteger.length >= this.mod.length)
                    this.BarrettReduction(bigInteger);
                if (exp.TestBit(bitNum))
                {
                    fixed (uint* numPtr = bigInteger.data)
                    {
                        uint num1 = 0;
                        ulong num2 = 0;
                        do
                        {
                            ulong num3 = num2 + (ulong)*(uint*)((IntPtr)numPtr + (IntPtr)((long)num1 * 4L)) * (ulong)b;
                            *(int*)((IntPtr)numPtr + (IntPtr)((long)num1 * 4L)) = (int)(uint)num3;
                            num2 = num3 >> 32;
                        }
                        while (++num1 < bigInteger.length);
                        if (bigInteger.length < this.mod.length)
                        {
                            if (num2 != 0UL)
                            {
                                *(int*)((IntPtr)numPtr + (IntPtr)((long)num1 * 4L)) = (int)(uint)num2;
                                ++bigInteger.length;
                                while (bigInteger >= this.mod)
                                    BigIntegerPhoton.Kernel.MinusEq(bigInteger, this.mod);
                            }
                        }
                        else if (num2 != 0UL)
                        {
                            uint num4 = (uint)num2;
                            uint num5 = (uint)(((ulong)num4 << 32 | (ulong)*(uint*)((IntPtr)numPtr + (IntPtr)((long)(num1 - 1U) * 4L))) / (ulong)(this.mod.data[(int)this.mod.length - 1] + 1U));
                            uint index1 = 0;
                            ulong num6 = 0;
                            do
                            {
                                ulong num7 = num6 + (ulong)this.mod.data[(int)index1] * (ulong)num5;
                                uint num8 = *(uint*)((IntPtr)numPtr + (IntPtr)((long)index1 * 4L));
                                IntPtr num9 = (IntPtr)numPtr + (IntPtr)((long)index1 * 4L);
                                *(int*)num9 = (int)*(uint*)num9 - (int)(uint)num7;
                                num6 = num7 >> 32;
                                if (*(uint*)((IntPtr)numPtr + (IntPtr)((long)index1 * 4L)) > num8)
                                    ++num6;
                                ++index1;
                            }
                            while (index1 < bigInteger.length);
                            uint num10 = num4 - (uint)num6;
                            if (num10 != 0U)
                            {
                                uint num11 = 0;
                                uint index2 = 0;
                                uint[] data = this.mod.data;
                                do
                                {
                                    uint num12;
                                    int num13 = (num12 = data[(int)index2] + num11) < num11 ? 1 : 0;
                                    IntPtr num14 = (IntPtr)numPtr + (IntPtr)((long)index2 * 4L);
                                    int num15;
                                    uint num16 = (uint)(num15 = (int)*(uint*)num14 - (int)num12);
                                    *(int*)num14 = num15;
                                    int num17 = num16 > ~num12 ? 1 : 0;
                                    num11 = (num13 | num17) == 0 ? 0U : 1U;
                                    ++index2;
                                }
                                while (index2 < bigInteger.length);
                                uint num18 = num10 - num11;
                            }
                            while (bigInteger >= this.mod)
                                BigIntegerPhoton.Kernel.MinusEq(bigInteger, this.mod);
                        }
                        else
                        {
                            while (bigInteger >= this.mod)
                                BigIntegerPhoton.Kernel.MinusEq(bigInteger, this.mod);
                        }
                    }
                }
            }
            while (bitNum-- > 0U);
            return bigInteger;
        }
    }

    internal sealed class Montgomery
    {
        private Montgomery()
        {
        }

        public static uint Inverse(uint n)
        {
            uint num1 = n;
            uint num2;
            while ((num2 = n * num1) != 1U)
                num1 *= 2U - num2;
            return (uint)-num1;
        }

        public static BigIntegerPhoton ToMont(BigIntegerPhoton n, BigIntegerPhoton m)
        {
            n.Normalize();
            m.Normalize();
            n <<= (int)m.length * 32;
            n %= m;
            return n;
        }

        public static unsafe BigIntegerPhoton Reduce(BigIntegerPhoton n, BigIntegerPhoton m, uint mPrime)
        {
            BigIntegerPhoton big = n;
            fixed (uint* numPtr1 = big.data)
            fixed (uint* numPtr2 = m.data)
            {
                for (uint index = 0; index < m.length; ++index)
                {
                    uint num1 = numPtr1[0] * mPrime;
                    uint* numPtr3 = numPtr2;
                    uint* numPtr4 = numPtr1;
                    uint* numPtr5 = numPtr1;
                    long num2 = (long)num1;
                    uint* numPtr6 = numPtr3;
                    uint* numPtr7 = (uint*)((IntPtr)numPtr6 + 4);
                    long num3 = (long)*numPtr6;
                    long num4 = num2 * num3;
                    uint* numPtr8 = numPtr4;
                    uint* numPtr9 = (uint*)((IntPtr)numPtr8 + 4);
                    long num5 = (long)*numPtr8;
                    ulong num6 = (ulong)(num4 + num5 >>> 32);
                    uint num7;
                    for (num7 = 1U; num7 < m.length; ++num7)
                    {
                        ulong num8 = num6 + ((ulong)num1 * (ulong)*numPtr7++ + (ulong)*numPtr9++);
                        *numPtr5++ = (uint)num8;
                        num6 = num8 >> 32;
                    }
                    for (; num7 < big.length; ++num7)
                    {
                        ulong num9 = num6 + (ulong)*numPtr9++;
                        *numPtr5++ = (uint)num9;
                        num6 = num9 >> 32;
                        if (num6 == 0UL)
                        {
                            ++num7;
                            break;
                        }
                    }
                    for (; num7 < big.length; ++num7)
                        *numPtr5++ = *numPtr9++;
                    uint* numPtr10 = numPtr5;
                    uint* numPtr11 = (uint*)((IntPtr)numPtr10 + 4);
                    int num10 = (int)(uint)num6;
                    *numPtr10 = (uint)num10;
                }
                while (big.length > 1U && *(uint*)((IntPtr)numPtr1 + (IntPtr)((long)(big.length - 1U) * 4L)) == 0U)
                    --big.length;
            }
            if (big >= m)
                BigIntegerPhoton.Kernel.MinusEq(big, m);
            return big;
        }
    }

    private sealed class Kernel
    {
        public static BigIntegerPhoton AddSameSign(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
        {
            uint index = 0;
            uint[] data1;
            uint length1;
            uint[] data2;
            uint length2;
            if (bi1.length < bi2.length)
            {
                data1 = bi2.data;
                length1 = bi2.length;
                data2 = bi1.data;
                length2 = bi1.length;
            }
            else
            {
                data1 = bi1.data;
                length1 = bi1.length;
                data2 = bi2.data;
                length2 = bi2.length;
            }
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, length1 + 1U);
            uint[] data3 = bigInteger.data;
            ulong num1 = 0;
            do
            {
                ulong num2 = (ulong)data1[(int)index] + (ulong)data2[(int)index] + num1;
                data3[(int)index] = (uint)num2;
                num1 = num2 >> 32;
            }
            while (++index < length2);
            bool flag = num1 > 0UL;
            if (flag)
            {
                if (index < length1)
                {
                    do
                    {
                        flag = (data3[(int)index] = data1[(int)index] + 1U) == 0U;
                    }
                    while (++index < length1 & flag);
                }
                if (flag)
                {
                    data3[(int)index] = 1U;
                    uint num3;
                    bigInteger.length = num3 = index + 1U;
                    return bigInteger;
                }
            }
            if (index < length1)
            {
                do
                {
                    data3[(int)index] = data1[(int)index];
                }
                while (++index < length1);
            }
            bigInteger.Normalize();
            return bigInteger;
        }

        public static BigIntegerPhoton Subtract(BigIntegerPhoton big, BigIntegerPhoton small)
        {
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, big.length);
            uint[] data1 = bigInteger.data;
            uint[] data2 = big.data;
            uint[] data3 = small.data;
            uint index = 0;
            uint num1 = 0;
            do
            {
                uint num2;
                num1 = !((num2 = data3[(int)index] + num1) < num1 | (data1[(int)index] = data2[(int)index] - num2) > ~num2) ? 0U : 1U;
            }
            while (++index < small.length);
            if ((int)index != (int)big.length)
            {
                if (num1 == 1U)
                {
                    do
                    {
                        data1[(int)index] = data2[(int)index] - 1U;
                    }
                    while (data2[(int)index++] == 0U && index < big.length);
                    if ((int)index == (int)big.length)
                        goto label_7;
                }
                do
                {
                    data1[(int)index] = data2[(int)index];
                }
                while (++index < big.length);
            }
        label_7:
            bigInteger.Normalize();
            return bigInteger;
        }

        public static void MinusEq(BigIntegerPhoton big, BigIntegerPhoton small)
        {
            uint[] data1 = big.data;
            uint[] data2 = small.data;
            uint index = 0;
            uint num1 = 0;
            do
            {
                uint num2;
                num1 = !((num2 = data2[(int)index] + num1) < num1 | (data1[(int)index] -= num2) > ~num2) ? 0U : 1U;
            }
            while (++index < small.length);
            if ((int)index != (int)big.length && num1 == 1U)
            {
                do
                {
                    --data1[(int)index];
                }
                while (data1[(int)index++] == 0U && index < big.length);
            }
            while (big.length > 0U && big.data[(int)big.length - 1] == 0U)
                --big.length;
            if (big.length != 0U)
                return;
            ++big.length;
        }

        public static void PlusEq(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
        {
            uint index = 0;
            bool flag1 = false;
            uint[] data1;
            uint length1;
            uint[] data2;
            uint length2;
            if (bi1.length < bi2.length)
            {
                flag1 = true;
                data1 = bi2.data;
                length1 = bi2.length;
                data2 = bi1.data;
                length2 = bi1.length;
            }
            else
            {
                data1 = bi1.data;
                length1 = bi1.length;
                data2 = bi2.data;
                length2 = bi2.length;
            }
            uint[] data3 = bi1.data;
            ulong num1 = 0;
            do
            {
                ulong num2 = num1 + ((ulong)data1[(int)index] + (ulong)data2[(int)index]);
                data3[(int)index] = (uint)num2;
                num1 = num2 >> 32;
            }
            while (++index < length2);
            bool flag2 = num1 > 0UL;
            if (flag2)
            {
                if (index < length1)
                {
                    do
                    {
                        flag2 = (data3[(int)index] = data1[(int)index] + 1U) == 0U;
                    }
                    while (++index < length1 & flag2);
                }
                if (flag2)
                {
                    data3[(int)index] = 1U;
                    uint num3;
                    bi1.length = num3 = index + 1U;
                    return;
                }
            }
            if (flag1 && index < length1 - 1U)
            {
                do
                {
                    data3[(int)index] = data1[(int)index];
                }
                while (++index < length1);
            }
            bi1.length = length1 + 1U;
            bi1.Normalize();
        }

        public static BigIntegerPhoton.Sign Compare(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
        {
            uint length1 = bi1.length;
            uint length2 = bi2.length;
            while (length1 > 0U && bi1.data[(int)length1 - 1] == 0U)
                --length1;
            while (length2 > 0U && bi2.data[(int)length2 - 1] == 0U)
                --length2;
            if (length1 == 0U && length2 == 0U)
                return BigIntegerPhoton.Sign.Zero;
            if (length1 < length2)
                return BigIntegerPhoton.Sign.Negative;
            if (length1 > length2)
                return BigIntegerPhoton.Sign.Positive;
            uint index = length1 - 1U;
            while (index != 0U && (int)bi1.data[(int)index] == (int)bi2.data[(int)index])
                --index;
            if (bi1.data[(int)index] < bi2.data[(int)index])
                return BigIntegerPhoton.Sign.Negative;
            return bi1.data[(int)index] > bi2.data[(int)index] ? BigIntegerPhoton.Sign.Positive : BigIntegerPhoton.Sign.Zero;
        }

        public static uint SingleByteDivideInPlace(BigIntegerPhoton n, uint d)
        {
            ulong num1 = 0;
            uint length = n.length;
            while (length-- > 0U)
            {
                ulong num2 = num1 << 32 | (ulong)n.data[(int)length];
                n.data[(int)length] = (uint)(num2 / (ulong)d);
                num1 = num2 % (ulong)d;
            }
            n.Normalize();
            return (uint)num1;
        }

        public static uint DwordMod(BigIntegerPhoton n, uint d)
        {
            ulong num = 0;
            uint length = n.length;
            while (length-- > 0U)
                num = (num << 32 | (ulong)n.data[(int)length]) % (ulong)d;
            return (uint)num;
        }

        public static BigIntegerPhoton DwordDiv(BigIntegerPhoton n, uint d)
        {
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, n.length);
            ulong num1 = 0;
            uint length = n.length;
            while (length-- > 0U)
            {
                ulong num2 = num1 << 32 | (ulong)n.data[(int)length];
                bigInteger.data[(int)length] = (uint)(num2 / (ulong)d);
                num1 = num2 % (ulong)d;
            }
            bigInteger.Normalize();
            return bigInteger;
        }

        public static BigIntegerPhoton[] DwordDivMod(BigIntegerPhoton n, uint d)
        {
            BigIntegerPhoton bigInteger1 = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, n.length);
            ulong num1 = 0;
            uint length = n.length;
            while (length-- > 0U)
            {
                ulong num2 = num1 << 32 | (ulong)n.data[(int)length];
                bigInteger1.data[(int)length] = (uint)(num2 / (ulong)d);
                num1 = num2 % (ulong)d;
            }
            bigInteger1.Normalize();
            BigIntegerPhoton bigInteger2 = (BigIntegerPhoton)(uint)num1;
            return new BigIntegerPhoton[2] { bigInteger1, bigInteger2 };
        }

        public static BigIntegerPhoton[] multiByteDivide(BigIntegerPhoton bi1, BigIntegerPhoton bi2)
        {
            if (BigIntegerPhoton.Kernel.Compare(bi1, bi2) == BigIntegerPhoton.Sign.Negative)
                return new BigIntegerPhoton[2]
                {
            (BigIntegerPhoton) 0,
            new BigIntegerPhoton(bi1)
                };
            bi1.Normalize();
            bi2.Normalize();
            if (bi2.length == 1U)
                return BigIntegerPhoton.Kernel.DwordDivMod(bi1, bi2.data[0]);
            uint num1 = bi1.length + 1U;
            int num2 = (int)bi2.length + 1;
            uint num3 = 2147483648;
            uint num4 = bi2.data[(int)bi2.length - 1];
            int num5 = 0;
            int num6 = (int)bi1.length - (int)bi2.length;
            for (; num3 != 0U && ((int)num4 & (int)num3) == 0; num3 >>= 1)
                ++num5;
            BigIntegerPhoton bigInteger1 = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, (uint)((int)bi1.length - (int)bi2.length + 1));
            BigIntegerPhoton bigInteger2 = bi1 << num5;
            uint[] data = bigInteger2.data;
            bi2 <<= num5;
            int num7 = (int)num1 - (int)bi2.length;
            int index1 = (int)num1 - 1;
            uint num8 = bi2.data[(int)bi2.length - 1];
            ulong num9 = (ulong)bi2.data[(int)bi2.length - 2];
            for (; num7 > 0; --num7)
            {
                long num10 = ((long)data[index1] << 32) + (long)data[index1 - 1];
                ulong num11 = (ulong)num10 / (ulong)num8;
                ulong num12 = (ulong)num10 % (ulong)num8;
                while (num11 == 4294967296UL || num11 * num9 > (num12 << 32) + (ulong)data[index1 - 2])
                {
                    --num11;
                    num12 += (ulong)num8;
                    if (num12 >= 4294967296UL)
                        break;
                }
                uint index2 = 0;
                int index3 = index1 - num2 + 1;
                ulong num13 = 0;
                uint num14 = (uint)num11;
                do
                {
                    ulong num15 = num13 + (ulong)bi2.data[(int)index2] * (ulong)num14;
                    uint num16 = data[index3];
                    data[index3] -= (uint)num15;
                    num13 = num15 >> 32;
                    if (data[index3] > num16)
                        ++num13;
                    ++index2;
                    ++index3;
                }
                while ((long)index2 < (long)num2);
                int index4 = index1 - num2 + 1;
                uint index5 = 0;
                if (num13 != 0UL)
                {
                    --num14;
                    ulong num17 = 0;
                    do
                    {
                        ulong num18 = (ulong)data[index4] + (ulong)bi2.data[(int)index5] + num17;
                        data[index4] = (uint)num18;
                        num17 = num18 >> 32;
                        ++index5;
                        ++index4;
                    }
                    while ((long)index5 < (long)num2);
                }
                bigInteger1.data[num6--] = num14;
                --index1;
            }
            bigInteger1.Normalize();
            bigInteger2.Normalize();
            BigIntegerPhoton[] bigIntegerArray1 = new BigIntegerPhoton[2]
            {
          bigInteger1,
          bigInteger2
            };
            if (num5 != 0)
            {
                BigIntegerPhoton[] bigIntegerArray2 = bigIntegerArray1;
                bigIntegerArray2[1] = bigIntegerArray2[1] >> num5;
            }
            return bigIntegerArray1;
        }

        public static BigIntegerPhoton LeftShift(BigIntegerPhoton bi, int n)
        {
            if (n == 0)
                return new BigIntegerPhoton(bi, bi.length + 1U);
            int num1 = n >> 5;
            n &= 31;
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, (uint)((int)bi.length + 1 + num1));
            uint index = 0;
            uint length = bi.length;
            if (n != 0)
            {
                uint num2 = 0;
                for (; index < length; ++index)
                {
                    uint num3 = bi.data[(int)index];
                    bigInteger.data[(long)index + (long)num1] = num3 << n | num2;
                    num2 = num3 >> 32 - n;
                }
                bigInteger.data[(long)index + (long)num1] = num2;
            }
            else
            {
                for (; index < length; ++index)
                    bigInteger.data[(long)index + (long)num1] = bi.data[(int)index];
            }
            bigInteger.Normalize();
            return bigInteger;
        }

        public static BigIntegerPhoton RightShift(BigIntegerPhoton bi, int n)
        {
            if (n == 0)
                return new BigIntegerPhoton(bi);
            int num1 = n >> 5;
            int num2 = n & 31;
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, (uint)((int)bi.length - num1 + 1));
            uint index = (uint)(bigInteger.data.Length - 1);
            if (num2 != 0)
            {
                uint num3 = 0;
                while (index-- > 0U)
                {
                    uint num4 = bi.data[(long)index + (long)num1];
                    bigInteger.data[(int)index] = num4 >> n | num3;
                    num3 = num4 << 32 - n;
                }
            }
            else
            {
                while (index-- > 0U)
                    bigInteger.data[(int)index] = bi.data[(long)index + (long)num1];
            }
            bigInteger.Normalize();
            return bigInteger;
        }

        public static BigIntegerPhoton MultiplyByDword(BigIntegerPhoton n, uint f)
        {
            BigIntegerPhoton bigInteger = new BigIntegerPhoton(BigIntegerPhoton.Sign.Positive, n.length + 1U);
            uint index = 0;
            ulong num1 = 0;
            do
            {
                ulong num2 = num1 + (ulong)n.data[(int)index] * (ulong)f;
                bigInteger.data[(int)index] = (uint)num2;
                num1 = num2 >> 32;
            }
            while (++index < n.length);
            bigInteger.data[(int)index] = (uint)num1;
            bigInteger.Normalize();
            return bigInteger;
        }

        public static unsafe void Multiply(
          uint[] x,
          uint xOffset,
          uint xLen,
          uint[] y,
          uint yOffset,
          uint yLen,
          uint[] d,
          uint dOffset)
        {
            fixed (uint* numPtr1 = x)
            fixed (uint* numPtr2 = y)
            fixed (uint* numPtr3 = d)
            {
                uint* numPtr4 = (uint*)((IntPtr)numPtr1 + (IntPtr)((long)xOffset * 4L));
                uint* numPtr5 = (uint*)((IntPtr)numPtr4 + (IntPtr)((long)xLen * 4L));
                uint* numPtr6 = (uint*)((IntPtr)numPtr2 + (IntPtr)((long)yOffset * 4L));
                uint* numPtr7 = (uint*)((IntPtr)numPtr6 + (IntPtr)((long)yLen * 4L));
                uint* numPtr8 = (uint*)((IntPtr)numPtr3 + (IntPtr)((long)dOffset * 4L));
                while (numPtr4 < numPtr5)
                {
                    if (*numPtr4 != 0U)
                    {
                        ulong num1 = 0;
                        uint* numPtr9 = numPtr8;
                        uint* numPtr10 = numPtr6;
                        while (numPtr10 < numPtr7)
                        {
                            ulong num2 = num1 + ((ulong)*numPtr4 * (ulong)*numPtr10 + (ulong)*numPtr9);
                            *numPtr9 = (uint)num2;
                            num1 = num2 >> 32;
                            ++numPtr10;
                            ++numPtr9;
                        }
                        if (num1 != 0UL)
                            *numPtr9 = (uint)num1;
                    }
                    ++numPtr4;
                    ++numPtr8;
                }
            }
        }

        public static unsafe void MultiplyMod2p32pmod(
          uint[] x,
          int xOffset,
          int xLen,
          uint[] y,
          int yOffest,
          int yLen,
          uint[] d,
          int dOffset,
          int mod)
        {
            fixed (uint* numPtr1 = x)
            fixed (uint* numPtr2 = y)
            fixed (uint* numPtr3 = d)
            {
                uint* numPtr4 = numPtr1 + xOffset;
                uint* numPtr5 = numPtr4 + xLen;
                uint* numPtr6 = numPtr2 + yOffest;
                uint* numPtr7 = numPtr6 + yLen;
                uint* numPtr8 = numPtr3 + dOffset;
                uint* numPtr9 = numPtr8 + mod;
                while (numPtr4 < numPtr5)
                {
                    if (*numPtr4 != 0U)
                    {
                        ulong num1 = 0;
                        uint* numPtr10 = numPtr8;
                        for (uint* numPtr11 = numPtr6; numPtr11 < numPtr7 && numPtr10 < numPtr9; ++numPtr10)
                        {
                            ulong num2 = num1 + ((ulong)*numPtr4 * (ulong)*numPtr11 + (ulong)*numPtr10);
                            *numPtr10 = (uint)num2;
                            num1 = num2 >> 32;
                            ++numPtr11;
                        }
                        if (num1 != 0UL && numPtr10 < numPtr9)
                            *numPtr10 = (uint)num1;
                    }
                    ++numPtr4;
                    ++numPtr8;
                }
            }
        }

        public static unsafe void SquarePositive(BigIntegerPhoton bi, ref uint[] wkSpace)
        {
            uint[] numArray1 = wkSpace;
            wkSpace = bi.data;
            uint[] data = bi.data;
            uint length = bi.length;
            bi.data = numArray1;
            uint[] numArray2 = data;
            uint* numPtr1 = (uint*)null;
            fixed (uint* numPtr2 = numArray1)
            {
                if (data != null && numArray2.Length != 0)
                {
                    fixed (uint* numPtr1_ = &numArray2[0])
                    {
                        numPtr1 = numPtr1_;
                    }
                }
                uint* numPtr3 = numPtr2 + numArray1.Length;
                for (uint* numPtr4 = numPtr2; numPtr4 < numPtr3; ++numPtr4)
                    *numPtr4 = 0U;
                uint* numPtr5 = numPtr1;
                uint* numPtr6 = numPtr2;
                uint num1 = 0;
                while (num1 < length)
                {
                    if (*numPtr5 != 0U)
                    {
                        ulong num2 = 0;
                        uint num3 = *numPtr5;
                        uint* numPtr7 = numPtr5 + 1;
                        uint* numPtr8 = (uint*)((IntPtr)numPtr6 + (IntPtr)((long)(2U * num1) * 4L) + 4);
                        uint num4 = num1 + 1U;
                        while (num4 < length)
                        {
                            ulong num5 = num2 + ((ulong)num3 * (ulong)*numPtr7 + (ulong)*numPtr8);
                            *numPtr8 = (uint)num5;
                            num2 = num5 >> 32;
                            ++num4;
                            ++numPtr8;
                            ++numPtr7;
                        }
                        if (num2 != 0UL)
                            *numPtr8 = (uint)num2;
                    }
                    ++num1;
                    ++numPtr5;
                }
                uint* numPtr9 = numPtr2;
                uint num6 = 0;
                for (; numPtr9 < numPtr3; ++numPtr9)
                {
                    uint num7 = *numPtr9;
                    *numPtr9 = num7 << 1 | num6;
                    num6 = num7 >> 31;
                }
                if (num6 != 0U)
                    *numPtr9 = num6;
                uint* numPtr10 = numPtr1;
                uint* numPtr11 = numPtr2;
                uint* numPtr12 = (uint*)((IntPtr)numPtr10 + (IntPtr)((long)length * 4L));
                while (numPtr10 < numPtr12)
                {
                    ulong num8 = (ulong)*numPtr10 * (ulong)*numPtr10 + (ulong)*numPtr11;
                    *numPtr11 = (uint)num8;
                    ulong num9 = num8 >> 32;
                    IntPtr num10;
                    uint* numPtr13 = (uint*)(num10 = (IntPtr)(numPtr11 + 1));
                    *(int*)num10 = (int)*(uint*)num10 + (int)(uint)num9;
                    if (*numPtr13 < (uint)num9)
                    {
                        IntPtr num11;
                        uint* numPtr14 = (uint*)(num11 = (IntPtr)(numPtr13 + 1));
                        *(int*)num11 = (int)*(uint*)num11 + 1;
                        while (*numPtr14++ == 0U)
                        {
                            uint* numPtr15 = numPtr14;
                            int num12 = (int)*numPtr15 + 1;
                            *numPtr15 = (uint)num12;
                        }
                    }
                    ++numPtr10;
                    numPtr11 = numPtr13 + 1;
                }
                bi.length <<= 1;
                while (*(uint*)((IntPtr)numPtr2 + (IntPtr)((long)(bi.length - 1U) * 4L)) == 0U && bi.length > 1U)
                    --bi.length;
                numArray2 = [];
            }
        }

        public static BigIntegerPhoton gcd(BigIntegerPhoton a, BigIntegerPhoton b)
        {
            BigIntegerPhoton bigInteger1 = a;
            BigIntegerPhoton bigInteger2 = b;
            BigIntegerPhoton bigInteger3 = bigInteger2;
            while (bigInteger1.length > 1U)
            {
                bigInteger3 = bigInteger1;
                bigInteger1 = bigInteger2 % bigInteger1;
                bigInteger2 = bigInteger3;
            }
            if (bigInteger1 == 0U)
                return bigInteger3;
            uint num1 = bigInteger1.data[0];
            uint num2 = bigInteger2 % num1;
            int num3 = 0;
            while ((((int)num2 | (int)num1) & 1) == 0)
            {
                num2 >>= 1;
                num1 >>= 1;
                ++num3;
            }
            while (num2 != 0U)
            {
                while (((int)num2 & 1) == 0)
                    num2 >>= 1;
                while (((int)num1 & 1) == 0)
                    num1 >>= 1;
                if (num2 >= num1)
                    num2 = num2 - num1 >> 1;
                else
                    num1 = num1 - num2 >> 1;
            }
            return (BigIntegerPhoton)(num1 << num3);
        }

        public static uint modInverse(BigIntegerPhoton bi, uint modulus)
        {
            uint num1 = modulus;
            uint num2 = bi % modulus;
            uint num3 = 0;
            uint num4 = 1;
            for (; num2 != 0U; num2 %= num1)
            {
                if (num2 == 1U)
                    return num4;
                num3 += num1 / num2 * num4;
                num1 %= num2;
                if (num1 != 0U)
                {
                    if (num1 == 1U)
                        return modulus - num3;
                    num4 += num2 / num1 * num3;
                }
                else
                    break;
            }
            return 0;
        }

        public static BigIntegerPhoton modInverse(BigIntegerPhoton bi, BigIntegerPhoton modulus)
        {
            if (modulus.length == 1U)
                return (BigIntegerPhoton)BigIntegerPhoton.Kernel.modInverse(bi, modulus.data[0]);
            BigIntegerPhoton[] bigIntegerArray1 = new BigIntegerPhoton[2]
            {
          (BigIntegerPhoton) 0,
          (BigIntegerPhoton) 1
            };
            BigIntegerPhoton[] bigIntegerArray2 = new BigIntegerPhoton[2];
            BigIntegerPhoton[] bigIntegerArray3 = new BigIntegerPhoton[2]
            {
          (BigIntegerPhoton) 0,
          (BigIntegerPhoton) 0
            };
            int num = 0;
            BigIntegerPhoton bi1 = modulus;
            BigIntegerPhoton bi2 = bi;
            BigIntegerPhoton.ModulusRing modulusRing = new BigIntegerPhoton.ModulusRing(modulus);
            while (bi2 != 0U)
            {
                if (num > 1)
                {
                    BigIntegerPhoton bigInteger = modulusRing.Difference(bigIntegerArray1[0], bigIntegerArray1[1] * bigIntegerArray2[0]);
                    bigIntegerArray1[0] = bigIntegerArray1[1];
                    bigIntegerArray1[1] = bigInteger;
                }
                BigIntegerPhoton[] bigIntegerArray4 = BigIntegerPhoton.Kernel.multiByteDivide(bi1, bi2);
                bigIntegerArray2[0] = bigIntegerArray2[1];
                bigIntegerArray2[1] = bigIntegerArray4[0];
                bigIntegerArray3[0] = bigIntegerArray3[1];
                bigIntegerArray3[1] = bigIntegerArray4[1];
                bi1 = bi2;
                bi2 = bigIntegerArray4[1];
                ++num;
            }
            if (bigIntegerArray3[0] != 1U)
                throw new ArithmeticException("No inverse!");
            return modulusRing.Difference(bigIntegerArray1[0], bigIntegerArray1[1] * bigIntegerArray2[0]);
        }
    }
}
