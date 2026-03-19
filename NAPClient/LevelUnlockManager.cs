using System.Collections.Generic;

namespace NAPClient
{
    public class LevelUnlockManager
    {
        Dictionary<int, bool> LevelsUnlocked;
        Dictionary<int, bool> EpisodesUnlocked;

        public LevelUnlockManager()
        {
            LevelsUnlocked = new Dictionary<int, bool>();
            EpisodesUnlocked = new Dictionary<int, bool>();
        }

        public void AddLevelToUnlocks(int levelId)
        {
            LevelsUnlocked[levelId] = true;
        }

        public void RemoveLevelFromUnlocks(int levelId)
        {
            LevelsUnlocked[levelId] = false;
        }

        public void AddEpisodeToUnlocks(int episodeId)
        {
            EpisodesUnlocked[episodeId] = true;
        }

        public void RemoveEpisodeFromUnlocks(int episodeId)
        {
            EpisodesUnlocked[episodeId] = false;
        }

        public bool ShouldLevelUnlock(LevelProfileMemoryBridge level)
        {
            if (!LevelsUnlocked.ContainsKey(level.GetLevelId()) || LevelsUnlocked[level.GetLevelId()] == false)
            {
                level.LockLevel();
                level.RevokeAllGold();
                return false;
            }
            return true;
        }

        public bool ShouldEpisodeUnlock(EpisodeProfileMemoryBridge episode)
        {
            if (!EpisodesUnlocked.ContainsKey(episode.GetEpisodeId()) || EpisodesUnlocked[episode.GetEpisodeId()] == false)
            {
                episode.LockEpisode();
                return false;
            }
            return true;
        }
    }
}
