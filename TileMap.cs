using Godot;
using System;

public class TileMap : Godot.TileMap
{
	public int MapWidth{get;set;}
	public int MapHeight{get;set;}
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{


		//System.Diagnostics.Debug.WriteLine(this.TileSet.GetTilesIds().Count);
		this.TileSet = new TileSet();

		var map = new Map(@"res\1070.map");
		 


		var Width = (int)map.Width;
		var Height = (int)map.Height;
		MapWidth= Width;
		MapHeight=Height;
		System.Diagnostics.Debug.WriteLine(map.Masks.Count);
		int index = 1;
		for (int i = 0; i < map.Rows; i++)
		{
			for (int j = 0; j < map.Cols; j++)
			{
				var image = map.GetMap(i * map.Cols + j);
				//image.SavePng("E:\\梦幻西游制作资料\\地图资源\\" + index + ".png");
				var texture = new ImageTexture();
				texture.CreateFromImage(image);
				this.TileSet.CreateTile(index);
				this.TileSet.TileSetTexture(index++, texture);
				this.SetCell(j, i, index - 1);
			}
		}

	}

	//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	//  public override void _Process(float delta)
	//  {
	//      
	//  }
}
