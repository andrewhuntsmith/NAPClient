using Godot;

public partial class ColorKey : Node
{
	[Export] ColorRect UnavailableRect;
	[Export] ColorRect AvailableRect;
	[Export] ColorRect BeatenRect;
	[Export] ColorRect AllGoldRect;
 
	public void SetColors(Color unavailableColor, Color availableColor, Color beatenColor, Color allGoldColor)
	{
		UnavailableRect.Color = unavailableColor;
        AvailableRect.Color = availableColor;
        BeatenRect.Color = beatenColor;
        AllGoldRect.Color = allGoldColor;
    }
}
