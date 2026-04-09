using Godot;
using Godot.Collections;
using NAPClient;
using System;

public partial class LogEntry : Label
{
	[Export] Color StartTimeColor;
	[Export] Color GoldValueColor;
    [Export] Color MaxTimeColor;
	[Export] Color LevelUnlockColor;
	[Export] Color PaletteColor;
	[Export] Color UnhandledColor;

	public void SetData(ItemData itemData)
	{
		var stylebox = new StyleBoxFlat();

		switch (itemData.Type)
		{
			case ItemType.IncreaseStartTime:
				stylebox.BgColor = StartTimeColor;
				break;
			case ItemType.IncreaseGoldValue:
				stylebox.BgColor = GoldValueColor;
				break;
			case ItemType.IncreaseMaxTime:
				stylebox.BgColor = MaxTimeColor;
				break;
			case ItemType.LevelUnlock:
			case ItemType.EpisodeUnlock:
			case ItemType.ProgressiveEpisodeUnlock:
				stylebox.BgColor = LevelUnlockColor;
				break;
			case ItemType.ChangeColorPalette:
				stylebox.BgColor = PaletteColor;
				break;
			default:
				stylebox.BgColor = UnhandledColor;
				break;
		}

		AddThemeStyleboxOverride("normal", stylebox);
		Text = "Received " + itemData.Type.ToString() + " " + itemData.Value.ToString();
    }

    public static string GenerateEpisodeName(int index)
    {
        var letters = "ABCDE";
        var letter = letters[index % 5];
        var number = index / 5;
        return "SI-" + letter + "-" + number.ToString();
    }

    public static string GenerateLevelName(int index)
    {
        var episodeId = index / 5;
        var levelId = index % 5;
        return GenerateEpisodeName(episodeId) + "-" + levelId.ToString();
    }

}
