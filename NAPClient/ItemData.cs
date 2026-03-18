namespace NAPClient
{
    public class ItemData
    {
        public int Value;
        public ItemType Type;
    }

    public enum ItemType
    {
        LevelUnlock,
        EpisodeUnlock,
        ProgressiveEpisodeUnlock,
        IncreaseStartTime,
        IncreaseGoldValue,
        ChangeColorPalette,
    }
}
