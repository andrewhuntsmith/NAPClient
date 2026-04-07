using System.Collections.Generic;

namespace NAPClient
{
    public class RandomizationData
    {
        public class CompletionCondition
        {
            public int Id;
            public ProgressState State;

            public override bool Equals(object o)
            {
                var otherCond = (CompletionCondition)o;
                if (otherCond == null) return false;
                return otherCond.Id == Id && otherCond.State == State;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() ^ State.GetHashCode();
            }
        }


        // IMPORTANT
        // The numbers used in LevelOrder refer to the original level IDs
        // The numbers used in InitialLevels and CompletionConditions refer to the new level IDs
        // If the first number in LevelOrder is 10, the completion conditions attached to that level will still be 0
        public List<int> LevelOrder = new List<int>();
        public List<int> InitialLevels = new List<int>();
        public float StartingLevelTime;
        public float StartingGoldValue;
        public double InitialMaxTime;
        public GoalType Goal;
        public Dictionary<CompletionCondition, ItemData> UnlockConditions = new Dictionary<CompletionCondition, ItemData>();
    }

    public enum ProgressState
    {
        LevelComplete,
        LevelAllGold,
        EpisodeComplete,
        EpisodeAllGold
    }
}
