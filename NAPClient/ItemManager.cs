namespace NAPClient
{
    public class ItemManager
    {
        public static MemorySource MS;
        public LevelUnlockManager LevelUnlockManager;
        public bool Initializing;

        public ItemManager(MemorySource ms) 
        { 
            MS = ms;
            LevelUnlockManager = new LevelUnlockManager();
            Initializing = true;
        }

        public void HandleCondition(ItemData item)
        {
            switch (item.Type)
            {
                case ItemType.LevelUnlock:
                    UnlockLevelFromRandomizer(item.Value);
                    return;
                case ItemType.EpisodeUnlock:
                    UnlockEpisodeFromRandomizer(item.Value);
                    return;
                case ItemType.ProgressiveEpisodeUnlock:
                    ProgressiveEpisodeUnlock(item.Value);
                    return;
                case ItemType.ChangeColorPalette:
                    PaletteSwap(item.Value);
                    return;
                case ItemType.IncreaseStartTime:
                    IncreaseStartTime(item.Value);
                    return;
                case ItemType.IncreaseGoldValue:
                    IncreaseGoldTime(item.Value);
                    return;
            }
        }

        void UnlockLevelFromRandomizer(int levelId)
        {
            var levelProfile = MS.LevelProfile[levelId];

            if (levelProfile.GetLevelCompleteState() == LevelCompleteState.LOCKED)
            {
                LevelUnlockManager.AddLevelToUnlocks(levelId);
                levelProfile.UnlockLevel();
            }
        }

        void UnlockEpisodeFromRandomizer(int episodeId)
        {
            var episodeProfile = MS.EpisodeProfile[episodeId];

            if (episodeProfile.GetEpisodeCompleteState() == EpisodeCompleteState.LOCKED)
            {
                LevelUnlockManager.AddEpisodeToUnlocks(episodeId);
                episodeProfile.UnlockEpisode();
            }
        }

        void ProgressiveEpisodeUnlock(int episodeId)
        {
            for (int i = 0; i < 5; i++)
            {
                var state = MS.LevelProfile[episodeId * 5 + i].GetLevelCompleteState();
                if (state == LevelCompleteState.LOCKED)
                {
                    UnlockLevelFromRandomizer(episodeId * 5 + i);
                    return;
                }
            }

            UnlockEpisodeFromRandomizer(episodeId);
        }

        void PaletteSwap(int paletteId)
        {
            if (Initializing)
                return;

            MS.PaletteIndex.UpdateValue();
            MS.PaletteIndex.SetValue(paletteId);
        }

        void IncreaseStartTime(int time)
        {
            MS.LevelStartTime.SetValue(MS.LevelStartTime.Value + time);
        }

        void IncreaseGoldTime(int time)
        {
            var adjustTime = time / 10.0f;
            MS.TimeGrantedByGold.SetValue(MS.TimeGrantedByGold.Value + adjustTime);
        }
    }
}
