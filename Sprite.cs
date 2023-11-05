using Godot;
using System;

public class Sprite : Godot.Sprite
{
	private float timer = 0f;
	private int frameIndex = 0;
	private ImageTexture[] frames = null;

	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	WDF wdf;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		wdf = new WDF(@"res\shape.wdf");
		//EBE564E7
		//Was was = wdf.GetWas("0xEBE564E7");
		Was was = new Was(@"res\EBE564E7.was");

		//	System.Diagnostics.Debug.WriteLine(was.GetFrame(0, 0).img.Count);
		//was.Data(0).img[0].SavePng("E:\\Game\\1.png");
		frames = new ImageTexture[was.Frame];
		for (int i = 0; i < was.Frame; i++)
		{
			frames[i] = new ImageTexture();
			frames[i].CreateFromImage(was.Data(i+was.Frame*2));
		}

		this.Texture = frames[0];
		this.Offset = new Vector2(500, 260);
	}

	//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta)
	{

		// 每隔 0.1s 更新一次纹理
		timer += delta;
		if (timer > 0.1f)
		{
			// 更新纹理
			frameIndex++;
			if (frameIndex >= frames.Length)
			{
				frameIndex = 0;
			} 
			this.Texture = frames[frameIndex];
			timer -= 0.1f;

		}

	}

	public override void _ExitTree()
	{
		foreach (var texture in frames)
		{
			texture.Dispose();
		}
		frames = null;
	}

}
