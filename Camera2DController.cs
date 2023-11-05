using Godot;
using System;

public class Camera2DController : Godot.Camera2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	bool pull = false;
	Vector2 mousePos;
	Vector2 currentPos;
	private Vector2 _tilemapSize;
	private TileMap _tileMap;
	private Vector2 _cameraSize;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	 
	 _tileMap= GetParent().GetParent().GetNode<TileMap> ("TileMap") ;
		//var s = TileSet;
		//System.Diagnostics.Debug.WriteLine("123");
		
		//var usedRectSize = _tileMap.GetUsedRect().Size;
		//var tileSize = _tileMap.CellSize;
		//_tilemapSize = new Vector2(usedRectSize.x * tileSize.x, usedRectSize.y * tileSize.y);

		//GD.Print("TileMap size in pixels: " + _tilemapSize);
		//_cameraSize = GetViewportRect().Size / Zoom;
	}

	public override void _PhysicsProcess(float delta)
	{
		if (Input.IsActionPressed("ui_down"))
		{
			KeyPress(0, 10);
		}
		else if (Input.IsActionPressed("ui_up"))
		{
			KeyPress(0, -10);
		}
		else if (Input.IsActionPressed("ui_right"))
		{
			KeyPress(10, 0);
		}
		else if (Input.IsActionPressed("ui_left"))
		{
			KeyPress(-10, 0);
		}
		return;
	}

	private void KeyPress(float dx, float dy)
	{
		var pos = this.Offset;
		var x = pos.x + dx;// - _cameraSize.x / 2;
		var y = pos.y + dy;// - _cameraSize.y / 2;
		System.Diagnostics.Debug.WriteLine($"{x} {y}");
		if (x < 0) x = 0;
		if (y < 0) y = 0;
		if (x > _tileMap.MapWidth-850) x =  _tileMap.MapWidth-850;
		if (y > _tileMap.MapHeight-620) y =  _tileMap.MapHeight-620;
		//	System.Diagnostics.Debug.WriteLine($"{_tilemapSize.x} {_tilemapSize.y}");
		this.Offset = new Vector2(x, y);
	}

}
