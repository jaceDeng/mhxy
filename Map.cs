using Godot;
using System;
using System.Collections.Generic;

public class Map : Godot.Object
{
	//private GDXY2 =
	//preload("res://bin/gdxy2.gdns");
	private const float BLOCK_WIDTH = 320.0f; //地图块宽度
	private const float BLOCK_HEIGHT = 240.0f; //地图块高度

	private string path;         //文件路径
	private string flag;
	private uint width;
	private uint height;
	private int rowNum;
	private uint colNum;
	private long blockNum;

	private uint cellRowNum;
	private uint cellColNum;
	private AStar2D astar = new AStar2D();

	private uint[] maskOffset;   //遮罩索引
	private List<MaskInfo> masks = new List<MaskInfo>();
	private Dictionary<uint, bool> no_repeat = new Dictionary<uint, bool>();

	private List<uint> blockOffset;  //地图块偏移信息
	private List<MapBlockInfo> blocks = new List<MapBlockInfo>();

	private byte[] jpeg_head;
	public uint Width { get { return width; } }

	public uint Height { get { return height; } }

	public int Rows { get { return rowNum; } }
	public int Cols { get { return (int)colNum; } }

	/// <summary>
	/// 遮罩
	/// </summary>
	public List<MaskInfo> Masks { get { return masks; } }

	//  private Image image;        //图片

	public Map()
	{

	}
	public Map(string path)
	{
		if (path == null) return;
		this.path = path;

		var file = new File();
		file.Open(path, File.ModeFlags.Read);

		flag = file.GetBuffer(4).GetStringFromUTF8();
		width = file.Get32();
		height = file.Get32();
		rowNum = Mathf.CeilToInt(height / BLOCK_HEIGHT);
		colNum = (uint)Mathf.CeilToInt(width / BLOCK_WIDTH);
		blockNum = rowNum * colNum;

		cellRowNum = height / 20;
		cellRowNum = cellRowNum % 12 == 0 ? cellRowNum : cellRowNum + 12 - cellRowNum % 12;
		cellColNum = width / 20;
		cellColNum = cellColNum % 16 == 0 ? cellColNum : cellColNum + 16 - cellColNum % 16;

		blockOffset = new List<uint>((int)blockNum);
		blocks = new List<MapBlockInfo>((int)blockNum);
		//  blockOffset.Resize(blockNum);
		for (int i = 0; i < blockNum; i++)
		{
			blockOffset.Add(file.Get32());
			blocks.Add(new MapBlockInfo());
			blocks[i].ownMasks = new List<bool>();
		}
		file.Get32();  // 跳过无用的4字节 旧地图为MapSize  新地图为MASK Flag

		if (flag == "0.1M")
		{
			uint maskNum = file.Get32();
			maskOffset = new uint[maskNum];
			// maskOffset.Resize(maskNum);
			for (int i = 0; i < maskOffset.Length; i++)
			{
				maskOffset[i] = file.Get32();
			}
			for (int i = 0; i < maskOffset.Length; i++)
			{
				var offset = (uint)maskOffset[i];
				file.Seek(offset);
				var maskInfo = new MaskInfo();
				maskInfo.id = i;
				maskInfo.offset = offset + 20;
				maskInfo.x = file.Get32();
				maskInfo.y = file.Get32();
				maskInfo.width = file.Get32();
				maskInfo.height = file.Get32();
				maskInfo.size = file.Get32();
				int maskRowStart = (int)Mathf.Max(maskInfo.y / BLOCK_HEIGHT, 0);
				var maskRowEnd = Mathf.Min((maskInfo.y + maskInfo.height) / BLOCK_HEIGHT, rowNum - 1);
				int maskColStart = (int)Mathf.Max(maskInfo.x / BLOCK_WIDTH, 0);
				var maskColEnd = Mathf.Min((maskInfo.x + maskInfo.width) / BLOCK_WIDTH, colNum - 1);
				for (int x = maskRowStart; x <= maskRowEnd; x++)
				{
					for (int y = maskColStart; y <= maskColEnd; y++)
					{
						var index = x * colNum + y;
						if (0 <= index && index < blockNum)
						{
							blocks[(int)index].ownMasks.Add(i != 0);
						}
					}
				}
				masks.Add(maskInfo);
			}
		}
		else if (flag == "XPAM")
		{
			var flag = file.GetBuffer(4).GetStringFromUTF8();
			var size = file.Get32();
			if (flag == "HGPJ")
			{
				jpeg_head = new byte[size];
				jpeg_head = file.GetBuffer(size);
			}
		}

		Travel(file);
		file.Close();
	}

	private void Travel(File file)
	{
		for (uint i = 0; i < blockNum; i++)
		{
			var offset = blockOffset[(int)i];
			file.Seek(offset);
			uint eatNum = file.Get32();
			offset += 4;
			if (flag == "0.1M")
			{
				for (int ignore = 0; ignore < eatNum; ignore++)
				{
					file.Get32();
					offset += 4;
				}
			}
			bool loop = true;
			while (loop)
			{
				file.Seek(offset);
				var flag = file.GetBuffer(4).GetStringFromUTF8();
				var size = file.Get32();
				offset += 8;

				if (flag == "GEPJ" || flag == "2GPJ")
				{
					blocks[(int)i].jpegOffset = (long)file.GetPosition();
					blocks[(int)i].jpegSize = size;
					offset += size;
				}
				else if (flag == "KSAM" || flag == "2SAM")
				{
					ReadOldMask(file, offset, i, size);
					offset += size;
				}
				else if (flag == "LLEC")
				{
					ReadCell(file, offset, i, size);
					offset += size;
				}
				else if (flag == "GIRB")
				{
					offset += size;
				}
				else if (flag == "BLOK")
				{
					offset += size;
				}
				else
				{
					loop = false;
				}
			}
		}
	}

	public void ReadOldMask(File file, uint offset, uint blockIndex, uint size)
	{
		file.Seek(offset);
		var maskInfo = new MaskInfo() { id = 0, offset = offset + 16 };
		maskInfo.id = 0;
		maskInfo.offset = offset + 16;

		var row = blockIndex / colNum;
		var col = blockIndex % colNum;

		maskInfo.x = file.Get32();
		maskInfo.y = (col * 320) + maskInfo.x;
		maskInfo.y = file.Get32();
		maskInfo.y = (row * 240) + maskInfo.y;
		maskInfo.width = file.Get32();
		maskInfo.height = file.Get32();
		maskInfo.size = size - 16;

		var key = maskInfo.x * 1000 + maskInfo.y;
		if (!no_repeat.ContainsKey(key))
		{
			var id = no_repeat.Count;

			no_repeat[key] = id != 0;
			blocks[(int)blockIndex].ownMasks.Add(id != 0);
			masks[id] = maskInfo;
		}
		else
		{
			var id = no_repeat[key];
			blocks[(int)blockIndex].ownMasks.Add(id);
		}
	}


	public void ReadCell(File file, uint offset, uint blockIndex, uint size)
	{
		int row = (int)blockIndex / (int)colNum;
		int col = (int)blockIndex % (int)colNum;
		file.Seek(offset);
		byte[] cell = file.GetBuffer(size);
		int i = 0;
		int j = 0;
		foreach (byte c in cell)
		{
			int ii = row * 12 + i;
			int jj = col * 16 + j;
			astar.AddPoint(ii * 1000 + jj, new Vector2(ii, jj), c);
			j++;
			if (j >= 16)
			{
				j = 0;
				i++;
			}
		}
	}


	public Image GetMap(int i)
	{
		if (i >= blockOffset.Count)
		{
			return null;
		}

		var file = new File();
		Image img = null;
		file.Open(path, File.ModeFlags.Read);

		var offset = blocks[i].jpegOffset;
		var size = blocks[i].jpegSize;

		file.Seek(offset);
		var jpeg = file.GetBuffer(size);
		if (flag == "XPAM")
		{
			List<byte> temp = new List<byte>();
			temp.AddRange(jpeg_head);
			temp.Add(255); // FF
			temp.Add(217); // D9
			temp.AddRange(jpeg);
			var pba = Gdxy2.ReadMapx(temp.ToArray());
			img = new Image();
			img.CreateFromData(320, 240, false, Image.Format.Rgb8, pba);
		}
		else if (flag == "0.1M")
		{
			var pba = Gdxy2.RepairJpeg(jpeg);
			img = new Image();
			var err = img.LoadJpgFromBuffer(pba);
		}

		file.Close();
		return img;


	}
	public Image GetMask(int i)
	{
		if (i >= masks.Count) return null;

		var offset = masks[i].offset;
		var size = masks[i].size;

		using (var file = new File())
		{
			file.Open(path, File.ModeFlags.Read);
			file.Seek(offset);

			byte[] maskBuffer = file.GetBuffer(size);
			byte[] pba = Gdxy2.ReadMask(maskBuffer, masks[i].width, masks[i].height);

			var img = new Image();
			img.CreateFromData((int)masks[i].width, (int)masks[i].height, false, Image.Format.Rgba8, pba);

			file.Close();

			return img;
		}
	}


}
