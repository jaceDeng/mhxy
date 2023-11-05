using System.Collections.Generic;

public class MapBlockInfo
{
	public long jpegOffset { get; set; }
	public long jpegSize { get; set; }
	public List<bool> ownMasks { get; set; }=new List<bool>();    
}
