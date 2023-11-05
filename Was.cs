using System;
using System.IO;
using System.Drawing;
using Godot;

public class Was : Godot.Object
{
	private bool Load = false;
	private Stream WASF;//文件流
	private int HeadLen;//文件头长度
	public int Direction;//方向数
	public int Frame;//单方向帧数
	private int[] ColorList;//颜色表
	private int[] DevList;//帧偏移表
	public Size[] SizeList;//图像大小表
	public Point[] OffsetList;//中心偏移表

	private int ShortRGB(short R565)
	{
		int rcol;
		int b0 = R565 & 255;
		int b1 = (R565 >> 8) & 255;
		int r = b1 & 248;
		int g = ((b1 << 5) | ((b0 & 224) >> 3)) & 255;
		int b = b0 << 3;
		r = (r | ((r & 63) >> 3)) & 255;
		g = (g | ((g & 15) >> 2)) & 255;
		b = (b | ((b & 63) >> 3)) & 255;
		rcol = (r << 16) | (g << 8) | b;
		return rcol;

	}
	public Was(string fpath)
	{
		WASF = new FileStream(fpath, FileMode.Open);
		byte[] mbuf = new byte[8];
		WASF.Read(mbuf, 0, 8);
		if (BitConverter.ToInt16(mbuf, 0) == 0x5053)
		{
			/*加载头*/
			HeadLen = BitConverter.ToInt16(mbuf, 2);
			Direction = BitConverter.ToInt16(mbuf, 4);
			Frame = BitConverter.ToInt16(mbuf, 6);
			WASF.Position += HeadLen - 4;
			/*定义数组*/
			ColorList = new int[256];
			DevList = new int[Direction * Frame + 1];
			SizeList = new Size[Direction * Frame];
			OffsetList = new Point[Direction * Frame];
			/*读取数组信息*/
			/*颜色表*/
			mbuf = new byte[512];
			WASF.Read(mbuf, 0, 512);
			for (int ii = 0; ii < 256; ii++)
			{
				ColorList[ii] = ShortRGB(BitConverter.ToInt16(mbuf, ii * 2));
			}
			/*帧偏移*/
			mbuf = new byte[Direction * Frame * 4];
			WASF.Read(mbuf, 0, Direction * Frame * 4);
			for (int ii = 0; ii < Direction * Frame; ii++)
			{
				DevList[ii] = BitConverter.ToInt32(mbuf, ii * 4);
			}
			DevList[Direction * Frame] = (int)WASF.Length;
			/*图像大小信息*/
			for (int ii = 0; ii < Direction * Frame; ii++)
			{
				if (DevList[ii] != 0)
				{
					WASF.Position = 4 + HeadLen + DevList[ii];
					mbuf = new byte[20];
					WASF.Read(mbuf, 0, 16);
					OffsetList[ii] = new Point(BitConverter.ToInt32(mbuf, 0), BitConverter.ToInt32(mbuf, 4));
					SizeList[ii] = new Size(BitConverter.ToInt32(mbuf, 8), BitConverter.ToInt32(mbuf, 12));
				}
			}
			Load = true;
		}
	}

	public Godot.Image Data(int ind)
	{
		int alpha, colind, dx, ww, hh, bt, len;
		int[] hexlist;
		Stream mem;
		Godot.Color col;
		Godot.Image mbitmap;
		if (DevList[ind] == 0)
		{
			return null;
		}
		ww = SizeList[ind].Width;
		hh = SizeList[ind].Height;
		if ((ww <= 0) || (hh <= 0))
		{
			return null;
		}

		byte[] mbuf = new byte[hh * 4];
		hexlist = new int[hh + 1];
		WASF.Position = 4 + HeadLen + DevList[ind] + 16;
		WASF.Read(mbuf, 0, hh * 4);
		for (int ii = 0; ii < hh; ii++)
		{
			hexlist[ii] = BitConverter.ToInt32(mbuf, ii * 4);
		}
		hexlist[hh] = DevList[ind + 1];
		if (hexlist[hh] == 0)
		{
			hexlist[hh] = (int)WASF.Length - 4 - HeadLen;
		}

		mbitmap = new Godot.Image();
		mbitmap.Create(ww, hh, false, Godot.Image.Format.Rgba8);
		mbitmap.Lock();
		for (int ii = 0; ii < hh; ii++)
		{
			dx = 0;
			len = hexlist[ii + 1] - hexlist[ii];
			mbuf = new byte[len];
			WASF.Position = 4 + HeadLen + DevList[ind] + hexlist[ii];
			WASF.Read(mbuf, 0, len);
			mem = new MemoryStream(mbuf);
			bt = mem.ReadByte();
			while (bt > 0)
			{
				switch ((bt >> 6) & 3)
				{
					case 0://0-63
						{
							if (bt < 32)
							{
								alpha = mem.ReadByte() * 8;
								colind = mem.ReadByte();
								if ((alpha >= 0) && (colind >= 0) && (colind < 256))
								{
									//Color.FromArgb(ColorList[colind] | (255 << 24));
									col = new Godot.Color(ConvertArgbToRgba(ColorList[colind] | (alpha << 24)));
									for (int jj = 0; jj < (bt & 31); jj++)
									{
										if (dx >= ww)
										{
											break;
										}
										mbitmap.SetPixel(dx, ii, col);
										dx++;
									}
								}
							}
							else
							{
								alpha = (bt & 31) * 8;
								colind = mem.ReadByte();
								if ((alpha >= 0) && (colind >= 0) && (colind < 256))
								{
									if (dx >= ww)
									{
										break;
									}
									col = new Godot.Color(ConvertArgbToRgba(ColorList[colind] | (alpha << 24)));
									mbitmap.SetPixel(dx, ii, col);
									dx++;

								}
							}
						}
						break;
					case 1://64-127
						{
							for (int jj = 0; jj < (bt & 63); jj++)
							{
								if (dx >= ww)
								{
									break;
								}
								colind = mem.ReadByte();
								if ((colind >= 0) && (colind < 256))
								{
									col = new Godot.Color(ConvertArgbToRgba(ColorList[colind] | (255 << 24)));
									mbitmap.SetPixel(dx, ii, col);
									dx++;
								}

							}
						}
						break;
					case 2://128-191
						{
							colind = mem.ReadByte();
							if ((colind >= 0) && (colind < 256))
							{
								col = new Godot.Color(ColorList[colind] | (255 << 24));
								for (int jj = 0; jj < (bt & 63); jj++)
								{
									if (dx >= ww)
									{
										break;
									}
									mbitmap.SetPixel(dx, ii, col);
									dx++;
								}
							}
						}
						break;
					case 3://192-255
						{
							dx += bt & 63;
						}
						break;
				}
				bt = mem.ReadByte();
			}
			mem.Close();
		}
		mbitmap.Unlock();
		return mbitmap;
	}
	public int ConvertArgbToRgba(int argbColor)
	{
		byte a = (byte)(argbColor >> 24);
		byte r = (byte)(argbColor >> 16);
		byte g = (byte)(argbColor >> 8);
		byte b = (byte)(argbColor);

		int rgbaColor = (int)((r << 24) + (g << 16) + (b << 8) + a);
		return rgbaColor;
	}

	//public Bitmap PutPNG()
	//{
	//    Bitmap rbitmap;
	//    Bitmap mbitmap;
	//    Graphics rgdi;
	//    Rectangle mrect;
	//    Rectangle drect;
	//    int mx = 0, my = 0, mr = 0, md = 0, mw, mh;
	//    for (int ii = 0; ii < SizeList.Length; ii++)
	//    {
	//        if (mx < OffsetList[ii].X)
	//        {
	//            mx = OffsetList[ii].X;
	//        }
	//        if (my < OffsetList[ii].Y)
	//        {
	//            my = OffsetList[ii].Y;
	//        }
	//        if (mr < SizeList[ii].Width - OffsetList[ii].X)
	//        {
	//            mr = SizeList[ii].Width - OffsetList[ii].X;
	//        }
	//        if (md < SizeList[ii].Height - OffsetList[ii].Y)
	//        {
	//            md = SizeList[ii].Height - OffsetList[ii].Y;
	//        }
	//    }
	//    mw = mx + mr;
	//    mh = my + md;
	//    if ((mw <= 0) || (mh <= 0))
	//    {
	//        return null;
	//    }
	//    rbitmap = new Bitmap(mw * Frame, mh * Direction);
	//    rgdi = Graphics.FromImage(rbitmap);
	//    for (int ii = 0; ii < Direction; ii++)
	//    {
	//        for (int jj = 0; jj < Frame; jj++)
	//        {
	//            mbitmap = Data(ii * Frame + jj);
	//            if (mbitmap == null)
	//            {
	//                /*return null;
	//                rgdi.Dispose();*/
	//            }
	//            else
	//            {
	//                mrect = new Rectangle(0, 0, mbitmap.Width, mbitmap.Height);
	//                drect = new Rectangle(jj * mw + mx - OffsetList[ii * Frame + jj].X, ii * mh + my - OffsetList[ii * Frame + jj].Y, mbitmap.Width, mbitmap.Height);
	//                rgdi.DrawImage(mbitmap, drect, mrect, GraphicsUnit.Pixel);
	//                mbitmap.Dispose();
	//            }
	//        }
	//    }
	//    rgdi.Dispose();
	//    return rbitmap;
	//}
	public void Close()
	{
		ColorList = null;
		DevList = null;
		if (Load == true)
		{
			WASF.Dispose();
		}
	}
}
