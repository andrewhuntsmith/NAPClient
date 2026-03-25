using System;

namespace NAPClient
{
    public class ItemManager
    {
        public static MemorySource MS;
        public LevelUnlockManager LevelUnlockManager;
        public bool Initializing;

        double MaxTime = double.MaxValue;

        public ItemManager(MemorySource ms) 
        { 
            MS = ms;
            LevelUnlockManager = new LevelUnlockManager();
            Initializing = true;

            MS.GoldCollectedInCurrentLevel.ValueChanged += AdjustToMaxTime;
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
                case ItemType.IncreaseMaxTime:
                    IncreaseMaxTime(item.Value);
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

        public void SetMaxTime(double time)
        {
            MaxTime = time;
        }

        void IncreaseMaxTime(int time)
        {
            MaxTime += time;
        }

        void AdjustToMaxTime()
        {
            MS.CurrentTimeRemaining.UpdateValue();
            if (MS.CurrentTimeRemaining.Value > MaxTime)
                MS.CurrentTimeRemaining.SetValue(MaxTime);
        }

        public static ItemData ConvertStringToItem(string itemName)
        {
            var newItem = new ItemData();

            if (itemName.Contains("prog"))
            {
                newItem.Type = ItemType.ProgressiveEpisodeUnlock;

                var idArray = itemName.Split('_')[0];
                var rowNumber = "abcde".IndexOf(idArray[0]);
                int.TryParse(idArray.Substring(1), out var colNumber);
                newItem.Value = (rowNumber * 5) + colNumber;
            }
            else if (itemName.Contains("Level Unlock"))
            {
                throw new NotImplementedException();
            }
            else if (itemName.Contains("Episode Unlock"))
            {
                throw new NotImplementedException();
            }
            else if (itemName.Contains("gold"))
            {
                newItem.Type = ItemType.IncreaseGoldValue;

                int.TryParse(itemName.Split('_')[2], out var valueNumber);
                newItem.Value = valueNumber;
            }
            else if (itemName.Contains("start"))
            {
                newItem.Type = ItemType.IncreaseStartTime;

                int.TryParse(itemName.Split('_')[2], out var valueNumber);
                newItem.Value = valueNumber;
            }
            else if (itemName.Contains("max"))
            {
                newItem.Type = ItemType.IncreaseMaxTime;

                int.TryParse(itemName.Split('_')[2], out var valueNumber);
                newItem.Value = valueNumber;
            }

            return newItem;
        }
    }
}
