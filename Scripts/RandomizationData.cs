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
        public List<List<int>> Challenges = new List<List<int>>();
        public float StartingLevelTime;
        public float StartingGoldValue;
        public double InitialMaxTime;
        public GoalType Goal;
        public Dictionary<CompletionCondition, ItemData> UnlockConditions = new Dictionary<CompletionCondition, ItemData>();
    
        public static CompletionCondition ConvertApStringToCondition(string input)
        {
            int id = -1;
            ProgressState state = ProgressState.LevelComplete;
            var splitInput = input.Split(' ');
            var splitId = splitInput[0].Split('-');
            if (splitInput[0].Length == 4)
            {
                var letter = splitId[0];
                var letterValue = "ABCDE".IndexOf(letter);
                int.TryParse(splitId[1], out var colValue);
                id = colValue * 5 + letterValue;
                state = ProgressState.EpisodeComplete;
            }
            else
            {
                var letter = splitId[0];
                var letterValue = "ABCDE".IndexOf(letter);
                int.TryParse(splitId[1], out var colValue);
                int.TryParse(splitId[2], out var levelValue);
                id = colValue * 25 + letterValue * 5 + levelValue;

                if (splitInput.Length == 2)
                    state = ProgressState.LevelComplete;
                else if (splitInput[2] == "1")
                    state = ProgressState.LevelChallenge1;
                else if (splitInput[2] == "2")
                    state = ProgressState.LevelChallenge2;
                else if (splitInput[2] == "3")
                    state = ProgressState.LevelChallenge3;
            }

            return new CompletionCondition
            {
                Id = id,
                State = state
            };
        }
    }

    public enum ProgressState
    {
        LevelComplete,
        LevelAllGold,
        EpisodeComplete,
        EpisodeAllGold,
        LevelChallenge1,
        LevelChallenge2,
        LevelChallenge3
    }
}
