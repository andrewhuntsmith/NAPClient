using System.Threading;

namespace NAPClient
{
    public class GoalManager
    {
        GoalType CurrentGoal;
        bool GoalMet;
        MemorySource MS;
        public bool Initializing;

        public GoalManager(MemorySource ms)
        {
            MS = ms;
            GoalMet = false;
            Initializing = true;
        }

        public void Reset()
        {
            GoalMet = false;
            Initializing = true;
        }

        public void SetGoal(GoalType goal)
        {
            CurrentGoal = goal;
        }

        public bool CheckMetGoal()
        {
            if (GoalMet || Initializing)
                return false;

            switch (CurrentGoal)
            {
                case GoalType.BeatOneEpisode:
                    GoalMet = CheckBeatEpisode();
                    break;
                case GoalType.BeatAllEpisodes:
                    GoalMet = CheckBeatAllEpisodes();
                    break;
                case GoalType.SingleBingo:
                    GoalMet = CheckSingleBingo();
                    break;
                case GoalType.TripleBingo:
                    GoalMet = CheckTripleBingo();
                    break;
                default:
                    return false;
            }

            return GoalMet;
        }

        bool CheckBeatEpisode()
        {
            foreach (var episode in MS.EpisodeProfile)
                if (episode.GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED)
                    return true;
            return false;
        }

        bool CheckBeatAllEpisodes()
        {
            foreach (var episode in MS.EpisodeProfile)
                if (episode.GetEpisodeCompleteState() != EpisodeCompleteState.COMPLETED)
                    return false;
            return true;
        }

        bool CheckSingleBingo()
        {
            return CheckBingoNumber(1);
        }

        bool CheckTripleBingo()
        {
            return CheckBingoNumber(3);
        }

        bool CheckBingoNumber(int bingoLimit)
        {
            var count = 0;
            
            bool AddBingo()
            {
                count += 1;
                return count >= bingoLimit;
            }

            //check horizontal bingos
            for (var startingEpisodeIndex = 0; startingEpisodeIndex < 5; startingEpisodeIndex += 1)
            {
                var bingo = true;

                for (var indexAdjustment = 0; indexAdjustment < 25; indexAdjustment += 5)
                {
                    if (MS.EpisodeProfile[startingEpisodeIndex + indexAdjustment].GetEpisodeCompleteState() != EpisodeCompleteState.COMPLETED)
                    {
                        bingo = false;
                        break;
                    }
                }

                if (bingo)
                    if (AddBingo())
                        return true;
            }

            //check vertical bingos
            for (var startingEpisodeIndex = 0; startingEpisodeIndex < 24; startingEpisodeIndex += 5)
            {
                var bingo = true;

                for (var indexAdjustment = 0; indexAdjustment < 5; indexAdjustment += 1)
                {
                    if (MS.EpisodeProfile[startingEpisodeIndex + indexAdjustment].GetEpisodeCompleteState() != EpisodeCompleteState.COMPLETED)
                    {
                        bingo = false;
                        break;
                    }
                }

                if (bingo)
                    if (AddBingo())
                        return true;
            }

            //check diagonal bingos manually
            if (MS.EpisodeProfile[0].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[6].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[12].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[18].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[24].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED)
                if (AddBingo())
                    return true;

            if (MS.EpisodeProfile[4].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[8].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[12].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[16].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED
                && MS.EpisodeProfile[20].GetEpisodeCompleteState() == EpisodeCompleteState.COMPLETED)
                if (AddBingo())
                    return true;

            return false;
        }
    }

    public enum GoalType
    {
        SingleBingo,
        TripleBingo,
        BeatOneEpisode,
        BeatAllEpisodes
    }
}
