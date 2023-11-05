using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

internal class Gdxy2
{
    internal static Godot.Color[] FormatPal(byte[] bytes)
    {

        Godot.Color[] pba = new Godot.Color[256 * 4];

        Godot.Color rgba;
        for (int k = 0; k < 256; k++)
        {
            rgba = RGB565to888(bytes[k]); // 16to32调色板转换
            pba[k] = rgba;
            //pba[k * 4 + 0] = (byte)rgba.r8;
            //pba[k * 4 + 1] = (byte)rgba.g8;
            //pba[k * 4 + 2] = (byte)rgba.b8;
            //pba[k * 4 + 3] = (byte)rgba.a8;
            ////pba.Set(k * 4 + 1, rgba.G);
            ////pba.Set(k * 4 + 2, rgba.B);
            ////pba.Set(k * 4 + 3, rgba.A);
        }
        return pba;
    }

    private static Godot.Color RGB565to888(ushort color565)
    {
        byte r = (byte)((color565 & 0xF800) >> 11);
        byte g = (byte)((color565 & 0x07E0) >> 5);
        byte b = (byte)(color565 & 0x001F);
        r = (byte)(r << 3 | r >> 2);
        g = (byte)(g << 2 | g >> 4);
        b = (byte)(b << 3 | b >> 2);
        return Godot.Color.Color8(r, g, b, 0xFF);
    }

    internal static byte[] ReadMapx(byte[] bytes)
    {
        byte[] pba = new byte[230400];
        //bool result = m_ujpeg.decode(in, bytes.size(), true);
        //if (!result)
        //    return pba;
        //if (!m_ujpeg.isValid())
        //    return pba;


        //m_ujpeg.getImage(pba.write().ptr());

        return pba;
    }

    internal static byte[] ReadMask(byte[] maskBuffer, uint width, uint height)
    {
        throw new NotImplementedException();
    }

    public static byte[] ReadWas(byte[] buff, Color[] pal)
    {

        byte[] data = buff;
        //    RGBA[] pal = (RGBA[])palette.Read()[0];

        uint offset = 0;

        FrameHeader frameHeader = new FrameHeader();
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            frameHeader = Marshal.PtrToStructure<FrameHeader>(ptr);
        }
        finally
        {
            handle.Free();
        }
        offset += 16;// sizeof(FrameHeader);

        uint[] frameLineOffset = new uint[(int)frameHeader.height];

        Buffer.BlockCopy(data, (int)offset, frameLineOffset, 0, (int)(frameHeader.height * 4));
        offset += frameHeader.height * 4;

        uint[] rgba = new uint[frameHeader.height * frameHeader.width];
        uint pos = 0;

        for (uint h = 0; h < frameHeader.height; h++)
        {
            uint linePixels = 0;
            bool lineNotOver = true;
            unsafe
            {
                fixed (byte* pD = data)
                {
                    byte* pData = pD + frameLineOffset[(int)h];
                    while (*pData != 0 && lineNotOver)
                    {

                        byte level = 0;  // Alpha
                        byte repeat = 0; // 重复次数
                        uint color = 0;      // 重复颜色
                        byte style = (byte)((*pData & 0xc0) >> 6);   // 取字节的前两个比特
                        switch (style)
                        {
                            case 0:   // {00******}
                                if ((*pData & 0x20) != 0)
                                {  // {001*****} 表示带有Alpha通道的单个像素
                                   // {001 +5bit Alpha}+{1Byte Index}, 表示带有Alpha通道的单个像素。
                                   // {001 +0~31层Alpha通道}+{1~255个调色板引索}
                                    level = (byte)(*pData & 0x1f);  // 0x1f=(11111) 获得Alpha通道的值
                                    if (*(pData - 1) == 0xc0)
                                    {  //特殊处理
                                        if (linePixels <= frameHeader.width)
                                        {
                                            rgba[pos] = rgba[pos - 1];
                                            linePixels++;
                                            pos++;
                                            pData += 2;
                                            break;
                                        }
                                        else
                                        {
                                            lineNotOver = false;
                                        }
                                    }
                                    pData++;  // 下一个字节
                                    if (linePixels <= frameHeader.width)
                                    {
                                        rgba[pos] = set_alpha(pal[*pData], (level << 3) | 7 - 1);
                                        linePixels++;
                                        pos++;
                                        pData++;
                                    }
                                    else
                                    {
                                        lineNotOver = false;
                                    }
                                }
                                else
                                {   // {000*****} 表示重复n次带有Alpha通道的像素
                                    // {000 +5bit Times}+{1Byte Alpha}+{1Byte Index}, 表示重复n次带有Alpha通道的像素。
                                    // {000 +重复1~31次}+{0~255层Alpha通道}+{1~255个调色板引索}
                                    // 注: 这里的{00000000} 保留给像素行结束使用，所以只可以重复1~31次。
                                    repeat = (byte)(*pData & 0x1f); // 获得重复的次数
                                    pData++;
                                    level = *pData; // 获得Alpha通道值
                                    pData++;
                                    color = set_alpha(pal[*pData], (level << 3) | 7 - 1);
                                    for (int i = 1; i <= repeat; i++)
                                    {
                                        if (linePixels <= frameHeader.width)
                                        {
                                            rgba[pos] = color;
                                            pos++;
                                            linePixels++;
                                        }
                                        else
                                        {
                                            lineNotOver = false;
                                        }
                                    }
                                    pData++;
                                }
                                break;
                            case 1: // {01******} 表示不带Alpha通道不重复的n个像素组成的数据段
                                    // {01  +6bit Times}+{nByte Datas},表示不带Alpha通道不重复的n个像素组成的数据段。
                                    // {01  +1~63个长度}+{n个字节的数据},{01000000}保留。
                                repeat = (byte)(*pData & 0x3f); // 获得数据组中的长度
                                pData++;
                                fixed (Color* pPal = pal)
                                {
                                    for (int i = 1; i <= repeat; i++)
                                    {
                                        if (linePixels <= frameHeader.width)
                                        {
                                            rgba[pos] = *(uint*)&pPal[*pData];
                                            pos++;
                                            linePixels++;
                                            pData++;
                                        }
                                        else
                                        {
                                            lineNotOver = false;
                                        }
                                    }
                                }
                                break;
                            case 2: // {10******} 表示重复n次像素
                                    // {10  +6bit Times}+{1Byte Index}, 表示重复n次像素。
                                    // {10  +重复1~63次}+{0~255个调色板引索},{10000000}保留。
                                repeat = (byte)(*pData & 0x3f); // 获得重复的次数
                                pData++;
                                fixed (Color* pPal = pal)
                                {
                                    color = *(uint*)&pPal[*pData];
                                }
                                for (int i = 1; i <= repeat; i++)
                                {
                                    if (linePixels <= frameHeader.width)
                                    {
                                        rgba[pos] = color;
                                        pos++;
                                        linePixels++;
                                    }
                                    else
                                    {
                                        lineNotOver = false;
                                    }
                                }
                                pData++;
                                break;
                            case 3: // {11******} 表示跳过n个像素，跳过的像素用透明色代替
                                    // {11  +6bit Times}, 表示跳过n个像素，跳过的像素用透明色代替。
                                    // {11  +跳过1~63个像素},{11000000}保留。
                                repeat = (byte)(*pData & 0x3f); // 获得重复次数
                                if (repeat == 0)
                                {
                                    if (linePixels <= frameHeader.width)
                                    { //特殊处理
                                        pos--;
                                        linePixels--;
                                    }
                                    else
                                    {
                                        lineNotOver = false;
                                    }
                                }
                                else
                                {
                                    for (int i = 1; i <= repeat; i++)
                                    {
                                        if (linePixels <= frameHeader.width)
                                        {
                                            pos++;
                                            linePixels++;
                                        }
                                        else
                                        {
                                            lineNotOver = false;
                                        }
                                    }
                                }
                                pData++;
                                break;
                            default: // 一般不存在这种情况
                                Console.WriteLine("WAS ERROR");
                                break;
                        }
                    }
                    if (*pData == 0 || !lineNotOver)
                    {
                        uint repeat = frameHeader.width - linePixels;
                        if (h > 0 && linePixels == 0)
                        {
                            //法术处理
                            byte* last = pData + frameLineOffset[(int)(h - 1)];
                            if (*last != 0)
                            {
                                Buffer.BlockCopy(rgba, (int)pos, rgba, (int)(pos - frameHeader.width), (int)(frameHeader.width * 4));
                                pos += frameHeader.width;
                            }
                        }
                        else if (repeat > 0)
                        {
                            pos += repeat;
                        }
                    }
                }
            }
        }

        frameLineOffset = null;

        byte[] pba = new byte[(int)(frameHeader.height * frameHeader.width * 4)];


        //   m_ujpeg.GetImage(pba.Write()[0]);

        Buffer.BlockCopy(rgba, 0, pba, 0, (int)(frameHeader.height * frameHeader.width * 4));
        return pba;


    }

    private static uint set_alpha(Color color, int Alpha)
    {
        ;
        color.a8  = Alpha;
        return (uint)color.ToRgba32();// * (uint32_t*)&color;
    }

    public static byte[] RepairJpeg(byte[] jpeg)
    {
        return jpeg;
        //byte[] Buffer = jpeg;
        //int outSize;
        //int inSize = jpeg.Length;
        //byte[] outBuffer;
        //// JPEG数据处理原理
        //// 1、复制D8到D9的数据到缓冲区中
        //// 2、删除第3、4个字节 FFA0
        //// 3、修改FFDA的长度00 09 为 00 0C
        //// 4、在FFDA数据的最后添加00 3F 00
        //// 5、替换FFDA到FF D9之间的FF数据为FF 00


        //uint TempNum = 0; // 临时变量，表示已读取的长度
        //ushort TempTimes = 0; // 临时变量，表示循环的次数
        //uint Temp = 0;
        //bool break_while = false;

        //// fixed 语句固定 Buffer 和 outBuffer 数组的内存地址，使得后面的指针操作不会出错
        //unsafe
        //{
        //    fixed (byte* pBuffer = Buffer, pOutBuffer = outBuffer)
        //    {
        //        byte* pIn = pBuffer;
        //        byte* pOut = pOutBuffer;

        //        // 当已读取数据的长度小于总长度时继续
        //        while (!break_while && TempNum < inSize && pIn[0] == 0xFF)
        //        {
        //            pOut[0] = 0xFF;
        //            pOut += 1;
        //            TempNum += 1;
        //            switch (pIn[1])
        //            {
        //                case 0xD8:
        //                    pOut[0] = 0xD8;
        //                    pIn += 1;
        //                    TempNum += 1;
        //                    pOut += 1;
        //                    break;
        //                case 0xA0:
        //                    pIn += 1;
        //                    pOut -= 1;
        //                    TempNum += 1;
        //                    break;
        //                case 0xC0:
        //                    pOut[0] = 0xC0;
        //                    pIn += 1;
        //                    TempNum += 1;

        //                    BufferToUInt16(pIn, ref TempTimes); // 读取长度
        //                    byte_swap(ref TempTimes); // 将长度转换为Intel顺序

        //                    for (int i = 0; i < TempTimes; i++)
        //                    {
        //                        pOut[1] = pIn[1];
        //                        pIn += 1;
        //                        TempNum += 1;
        //                        pOut += 1;
        //                    }

        //                    break;
        //                case 0xC4:
        //                    pOut[0] = 0xC4;
        //                    pIn += 1;
        //                    TempNum += 1;
        //                    BufferToUInt16(pIn, ref TempTimes); // 读取长度
        //                    byte_swap(ref TempTimes); // 将长度转换为Intel顺序

        //                    for (int i = 0; i < TempTimes; i++)
        //                    {
        //                        pOut[1] = pIn[1];
        //                        pIn += 1;
        //                        TempNum += 1;
        //                        pOut += 1;
        //                    }
        //                    break;
        //                case 0xDB:
        //                    pOut[0] = 0xDB;
        //                    pIn += 1;
        //                    TempNum += 1;

        //                    BufferToUInt16(pIn, ref TempTimes); // 读取长度
        //                    byte_swap(ref TempTimes); // 将长度转换为Intel顺序

        //                    for (int i = 0; i < TempTimes; i++)
        //                    {
        //                        pOut[1] = pIn[1];
        //                        pIn += 1;
        //                        TempNum += 1;
        //                        pOut += 1;
        //                    }
        //                    break;
        //                case 0xDA:
        //                    pOut[0] = 0xDA;
        //                    pOut[1] = 0x00;
        //                    pOut[2] = 0x0C;
        //                    pIn += 1;
        //                    TempNum += 1;

        //                    BufferToUInt16(pIn, ref TempTimes); // 读取长度
        //                    byte_swap(ref TempTimes); // 将长度转换为Intel顺序
        //                    pIn += 1;
        //                    TempNum += 1;
        //                    pIn += 1;

        //                    for (int i = 2; i < TempTimes; i++)
        //                    {
        //                        pOut[1] = pIn[1];
        //                        pIn += 1;
        //                        TempNum += 1;
        //                        pOut += 1;
        //                    }
        //                    pOut[0] = 0x00;
        //                    pOut[1] = 0x3F;
        //                    pOut[2] = 0x00;
        //                    Temp += 1; // 这里应该是+3的，因为前面的0xFFA0没有-2，所以这里只+1。

        //                    // 循环处理0xFFDA到0xFFD9之间所有的0xFF替换为0xFF00
        //                    for (; TempNum < inSize - 2;)
        //                    {
        //                        if (pIn[0] == 0xFF)
        //                        {
        //                            pOut[0] = 0xFF;
        //                            pOut[1] = 0x00;
        //                            pIn += 1;
        //                            TempNum += 1;
        //                            Temp += 1;
        //                        }
        //                        else
        //                        {
        //                            pOut[0] = pIn[0];
        //                            pIn += 1;
        //                            TempNum += 1;
        //                        }
        //                        pOut += 1;
        //                    }
        //                    // 直接在这里写上了0xFFD9结束Jpeg图片.
        //                    Temp -= 1; // 这里多了一个字节，所以减去。
        //                    pOut -= 1;
        //                    pOut -= 1;
        //                    pOut[0] = 0xD9;
        //                    break;
        //                case 0xD9:
        //                    // 算法问题，这里不会被执行，但结果一样。
        //                    pOut[0] = 0xD9;
        //                    TempNum += 1;
        //                    pOut += 1;
        //                    break;
        //                case 0xE0:
        //                    break_while = true; // 如果碰到E0,则说明
        //                    break;
        //                default:
        //                    // 其它情况直接复制
        //                    pOut[0] = pIn[0];
        //                    pIn += 1;
        //                    TempNum += 1;
        //                    pOut += 1;
        //                    break;
        //            }
        //        }

        //        outSize = TempNum;

        //        // fixed 语句结束后，数组又可以像普通数组一样使用
        //    }
        //}

    }
    private static void byte_swap(ref ushort value)
    {
        ushort tempvalue = (ushort)(value >> 8);
        value = (ushort)((value << 8) | tempvalue);
    }

    private static void BufferToUInt16(byte[] buffer, ref ushort target)
    {
        if (BitConverter.IsLittleEndian)
        {
            target = BitConverter.ToUInt16(new byte[] { buffer[1], buffer[0] }, 0);
        }
        else
        {
            target = BitConverter.ToUInt16(buffer, 0);
        }
    }
    public static uint string_id(string s)
    {
        string str = s;
        uint i;
        uint v;
        uint[] m = new uint[70];
        byte[] bytes = Encoding.ASCII.GetBytes(str);
        Buffer.BlockCopy(bytes, 0, m, 0, Math.Min(bytes.Length, m.Length) * sizeof(uint));
        for (i = 0; i < 256 / 4 && m[i] != 0; i++) { }
        m[i++] = 0x9BE74448;
        m[i++] = 0x66F42C48;
        v = 0xF4FA8928;
        uint esi = 0x37A8470E;
        uint edi = 0x7758B42B;
        uint eax = 0;
        uint ebx = 0;
        uint ecx = 0;
        uint edx = 0;
        ulong temp = 0;
        while (true)
        {
            ebx = 0x267B0B11;
            uint cf = (v & 0x80000000) > 0 ? 1U : 0U;
            v = ((v << 1) & 0xffffffff) + cf;
            ebx = ebx ^ v;
            eax = m[ecx];
            edx = ebx;
            esi = esi ^ eax;
            edi = edi ^ eax;
            temp = (ulong)edx + (ulong)edi;
            cf = (temp & 0x100000000) > 0 ? 1U : 0U;
            edx = (uint)(temp & 0xFFFFFFFF);
            edx = edx | 0x2040801U;
            edx = edx & 0xBFEF7FDFU;
            eax = esi;
            temp = (ulong)eax * (ulong)edx;
            eax = (uint)(temp & 0xffffffff);
            edx = (uint)((temp >> 32) & 0xffffffff);
            cf = edx > 0 ? 1U : 0U;
            temp = (ulong)eax + (ulong)edx + (ulong)cf;
            eax = (uint)(temp & 0xffffffff);
            cf = (temp & 0x100000000) > 0 ? 1U : 0U;
            edx = ebx;
            temp = (ulong)eax + (ulong)cf;
            eax = (uint)(temp & 0xffffffff);
            cf = (temp & 0x100000000) > 0 ? 1U : 0U;
            temp = (ulong)edx + (ulong)esi;
            cf = (temp & 0x100000000) > 0 ? 1U : 0U;
            edx = (uint)(temp & 0xFFFFFFFF);
            edx = edx | 0x804021U;
            edx = edx & 0x7DFEFBFFU;
            esi = eax;
            eax = edi;
            temp = (ulong)eax * (ulong)edx;
            eax = (uint)(temp & 0xffffffff);
            edx = (uint)((temp >> 32) & 0xffffffff);
            cf = edx > 0 ? 1U : 0U;
            temp = (ulong)edx << 1;
            edx = (uint)(temp & 0xFFFFFFFF);
            temp = (ulong)eax + (ulong)edx + (ulong)cf;
            eax = (uint)(temp & 0xffffffff);
            cf = (temp & 0x100000000) > 0 ? 1U : 0U;
            if (cf == 0) goto _skip;
            temp = (ulong)eax + 2UL;
            cf = (temp & 0x100000000) > 0 ? 1U : 0U;
            eax = (uint)(temp & 0xFFFFFFFF);
        _skip:
            ecx += 1U;
            edi = eax;
            if (ecx - i == 0) break;
        }
        esi = esi ^ edi;
        v = esi;
        return v;
    }
}
