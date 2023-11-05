using Godot;
using Godot.Collections;
using System;
using System.IO;

public class WDF : Godot.Object
{
	private string path;
	private string flag;
	private Dictionary<uint, Dictionary<string, uint>> file_dict;
	private uint n;
	public WDF()
	{

	}
	public WDF(string path)
	{
		if (path == null) return;
		this.path = path;
		file_dict = new Dictionary<uint, Dictionary<string, uint>>();
		var file = new Godot.File();
		var err = file.Open(path, Godot.File.ModeFlags.Read);
		if (err != Error.Ok)
		{
			throw new System.IO.FileNotFoundException("File Not Existed");
		}

		var buffer = file.GetBuffer(4);
		this.flag = buffer.GetStringFromUTF8();
		if (this.flag != "PFDW")
		{
			throw new InvalidDataException("Not Valid WDF File");
		}

		this.n = file.Get32();
		var offset = file.Get32();

		file.Seek(offset);
		for (int i = 0; i < this.n; i++)
		{
			var _hash = file.Get32();
			var _offset = file.Get32();
			var _size = file.Get32();
			var _spaces = file.Get32();
			var wdf_file = new Dictionary<string, uint>()
		{
			{ "hash", _hash },
			{ "offset", _offset },
			{ "size", _size },
			{ "spaces", _spaces },
		};
			this.file_dict[_hash] = wdf_file;
		}

		file.Close();
	}


	public Was GetWas(string s)
	{
		int _hash;
		if (s.StartsWith("0x"))
		{
			_hash = Convert.ToInt32(s.Substring(2), 16);
		}
		else
		{
			_hash = (int)Gdxy2.string_id(s);
		}

		Dictionary<string, uint> file_info = this.file_dict[(uint)_hash];
		var file = new Godot.File();
		file.Open(path, Godot.File.ModeFlags.Read);
		file.Seek(file_info["offset"]);

		string flag = file.GetBuffer(2).GetStringFromUTF8();
		Was item = null;
		if (flag == "SP")
		{
			//item = new Was(this.path, (int)file_info["offset"], (int)file_info["size"]);
		}
	 
		file.Close();
		return item;
	}



}
