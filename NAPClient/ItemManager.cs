namespace NAPClient
{
    public class ItemManager
    {
        public static MemorySource MS;
        public bool Initializing;

        public ItemManager(MemorySource ms) 
        { 
            MS = ms;
            Initializing = true;
        }

        public void HandleCondition(ItemData item)
        {
            if (item.Type == ItemType.LevelUnlock)
                UnlockLevelFromRandomizer(item.Value);
            if (item.Type == ItemType.ChangeColorPalette)
                PaletteSwap(item.Value);
        }

        void UnlockLevelFromRandomizer(int levelId)
        {
            foreach (var levelProfile in MS.LevelProfile)
            {
                if (levelProfile.GetLevelId() == levelId)
                {
                    if (levelProfile.GetLevelCompleteState() == LevelCompleteState.LOCKED)
                        levelProfile.UnlockLevel();
                    break;
                }
            }
        }

        void ProgressiveEpisodeUnlock()
        {

        }

        void PaletteSwap(int paletteId)
        {
            if (Initializing)
                return;

            MS.PaletteIndex.UpdateValue();
            MS.PaletteIndex.SetValue(paletteId);
        }
    }
}
