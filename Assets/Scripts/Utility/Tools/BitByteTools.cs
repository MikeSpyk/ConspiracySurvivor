using System;

public static class BitByteTools
{
    public static byte getByte(bool bit0, bool bit1, bool bit2, bool bit3, bool bit4, bool bit5, bool bit6, bool bit7)
    {
        byte result = 0;

        if (bit0)
            result += 1;

        if (bit1)
            result += 2;

        if (bit2)
            result += 4;

        if (bit3)
            result += 8;

        if (bit4)
            result += 16;

        if (bit5)
            result += 32;

        if (bit6)
            result += 64;

        if (bit7)
            result += 128;

        return result;
    }

    public static byte[] prependBytes(byte[] byteArray, byte[] prepend)
    {
        byte[] returnValue = new byte[prepend.Length + byteArray.Length];

        Array.Copy(prepend, returnValue, prepend.Length);
        Array.Copy(byteArray, 0, returnValue, prepend.Length, byteArray.Length);

        return returnValue;
    }
}
