namespace NAPClient
{
    public class GoalManager
    {
        GoalType CurrentGoal;
        bool GoalMet;
        MemorySource MS;

        public GoalManager(MemorySource ms)
        {
            MS = ms;
            GoalMet = false;
        }

        public void SetGoal(GoalType goal)
        {
            CurrentGoal = goal;
        }

        public bool CheckMetGoal()
        {
            if (GoalMet)
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
            return false;
        }

        bool CheckSingleBingo()
        {
            return false;
        }

        bool CheckTripleBingo()
        {
            return false;
        }
    }

    public enum GoalType
    {
        BeatOneEpisode,
        BeatAllEpisodes,
        SingleBingo,
        TripleBingo
    }
}
