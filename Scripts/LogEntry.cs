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
				Text = "Start time increased by " + itemData.Value.ToString();
				break;
			case ItemType.IncreaseGoldValue:
				stylebox.BgColor = GoldValueColor;
				Text = "Gold value increased by " + (itemData.Value / 10f).ToString();
				break;
			case ItemType.IncreaseMaxTime:
				stylebox.BgColor = MaxTimeColor;
				Text = "Max time increased by " + itemData.Value.ToString();
				break;
			case ItemType.LevelUnlock:
                stylebox.BgColor = LevelUnlockColor;
                Text = "Unlocked level " + GenerateLevelName(itemData.Value);
                break;
            case ItemType.EpisodeUnlock:
                stylebox.BgColor = LevelUnlockColor;
                Text = "Unlocked episode " + GenerateEpisodeName(itemData.Value);
                break;
            case ItemType.ProgressiveEpisodeUnlock:
				stylebox.BgColor = LevelUnlockColor;
				Text = "Unlocked next level in " + GenerateEpisodeName(itemData.Value);
				break;
			case ItemType.ChangeColorPalette:
				stylebox.BgColor = PaletteColor;
				Text = "Palette changed to ID " + itemData.Value.ToString();
				break;
			default:
				stylebox.BgColor = UnhandledColor;
				Text = "Received unknown item!";
				break;
		}

		AddThemeStyleboxOverride("normal", stylebox);
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
