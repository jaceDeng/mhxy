using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential,Pack =1, Size = 16)] // 指定结构体大小为16
public struct FrameHeader
{
	//[MarshalAs(UnmanagedType.I4)]
	public int key_x;          // 图片的锚点X
//	[MarshalAs(UnmanagedType.I4)]
	public int key_y;          // 图片的锚点Y
//	[MarshalAs(UnmanagedType.U4)]
	public uint width;         // 图片的宽度，单位像素
	//[MarshalAs(UnmanagedType.U4)]
	public uint height;        // 图片的高度，单位像素
}
public class Frame
{
	public int x { get; set; }
	public int y { get; set; }
	public int width { get; set; }
	public int height { get; set; }
	public List<Image> img { get; set; }

	public Frame(int x, int y, int width, int height)
	{
		this.x = x;
		this.y = y;
		this.width = width;
		this.height = height;
		this.img = new List<Image>();
	}
	public Frame(uint x, uint y, uint width, uint height)
	{
		this.x = (int)x;
		this.y = (int)y;
		this.width = (int)width;
		this.height = (int)height;
		this.img = new List<Image>();
	}
	public void Add(Image data)
	{
		img.Add(data);
	}
}
