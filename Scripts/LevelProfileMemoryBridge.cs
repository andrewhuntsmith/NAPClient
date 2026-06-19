using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace NAPClient
{
    public class LevelProfileMemoryBridge
    {
        public Action<LevelProfileMemoryBridge> ValueUpdated;
        public int BaseLevelPointer;
        public Action<int, int> OnChallengeCompleted;

        public ByteArrayAddressValue TotalLevelProfile = new ByteArrayAddressValue();

        IntAddressValue IdAddressValue = new IntAddressValue();
        IntAddressValue AttemptCountAddressValue = new IntAddressValue();
        IntAddressValue LevelSuccessAddressValue = new IntAddressValue();
        IntAddressValue EpisodeSuccessAddressValue = new IntAddressValue();
        ByteArrayAddressValue LevelLockedAddressValue = new ByteArrayAddressValue();
        ByteArrayAddressValue AllGoldAddressValue = new ByteArrayAddressValue();
        IntAddressValue ChallengeAddressValue = new IntAddressValue();
        bool RefreshLevelFlag;
        List<int> Challenges;
        List<int> CompletedChallenges;

        public LevelProfileMemoryBridge(int basePointer)
        {
            BaseLevelPointer = basePointer;

            TotalLevelProfile = new ByteArrayAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer },
                ArraySize = 48
            };
            IdAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer } };
            AttemptCountAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer + 4 } };
            LevelSuccessAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer + 12 } };
            EpisodeSuccessAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer + 16 } };
            LevelLockedAddressValue = new ByteArrayAddressValue() { Offsets = new List<int> { BaseLevelPointer + 20 }, ArraySize = 1 };
            AllGoldAddressValue = new ByteArrayAddressValue() { Offsets = new List<int> { BaseLevelPointer + 28 }, ArraySize = 1 };
            ChallengeAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer + 30 } };

            AttemptCountAddressValue.SetValue(0);
            LevelSuccessAddressValue.SetValue(0);
            EpisodeSuccessAddressValue.SetValue(0);

            AttemptCountAddressValue.ValueUpdated += InternalValueUpdated;
            LevelLockedAddressValue.ValueUpdated += InternalValueUpdated;
            AllGoldAddressValue.ValueUpdated += InternalValueUpdated;
            LevelSuccessAddressValue.ValueUpdated += InternalValueUpdated;
            EpisodeSuccessAddressValue.ValueUpdated += InternalValueUpdated;
            ChallengeAddressValue.ValueChanged += GetChallengeCompleted;

            Challenges = new List<int>();
            CompletedChallenges = new List<int>();
        }

        public void UpdateValue()
        {
            TotalLevelProfile.UpdateValue();
            IdAddressValue.UpdateValue();
            AttemptCountAddressValue.UpdateValue();
            LevelSuccessAddressValue.UpdateValue();
            EpisodeSuccessAddressValue.UpdateValue();
            LevelLockedAddressValue.UpdateValue();
            AllGoldAddressValue.UpdateValue();
            ChallengeAddressValue.UpdateValue();

            if (RefreshLevelFlag)
            {
                ValueUpdated?.Invoke(this);
                RefreshLevelFlag = false;
            }
        }

        void InternalValueUpdated(int _, int __)
        {
            InternalValueUpdated();
        }

        void InternalValueUpdated(byte[] _, byte[] __)
        {
            InternalValueUpdated();
        }

        void InternalValueUpdated()
        {
            RefreshLevelFlag = true;
        }

        public int GetLevelId()
        {
            return IdAddressValue.Value;
        }

        public int GetLevelSuccesses()
        {
            return LevelSuccessAddressValue.Value + EpisodeSuccessAddressValue.Value;
        }

        public LevelCompleteState GetLevelCompleteState()
        {
            return LevelLockedAddressValue.Value[0] == 0 ? LevelCompleteState.LOCKED :
                GetLevelSuccesses() == 0 ? LevelCompleteState.AVAILABLE :
                AllGoldAddressValue.Value[0] == 0 ? LevelCompleteState.COMPLETED :
                LevelCompleteState.ALLGOLD;
        }

        public void LockLevel()
        {
            LevelLockedAddressValue.SetValue(new byte[] { 0 });
        }

        public void UnlockLevel()
        {
            LevelLockedAddressValue.SetValue(new byte[] { 1 });
        }

        public void SetLevelBeaten()
        {
            LevelLockedAddressValue.SetValue(new byte[] { 2 });
        }

        public void RevokeAllGold()
        {
            AllGoldAddressValue.SetValue(new byte[] { 0 });
        }

        public void SetAllGold()
        {
            AllGoldAddressValue.SetValue(new byte[] { 1 });
        }

        public void SetChallengeData(List<int> challenges)
        {
            Challenges.Clear();
            Challenges.AddRange(challenges);
        }

        public void GetChallengeCompleted()
        {
            var challengeAsInt = ChallengeAddressValue.Value;
            foreach (var challenge in Challenges)
            {
                if ((challengeAsInt & challenge) == challenge && !CompletedChallenges.Contains(challenge))
                {
                    CompletedChallenges.Add(challenge);
                    OnChallengeCompleted(GetLevelId(), Challenges.IndexOf(challenge));
                }
            }
            ChallengeAddressValue.SetValue(0);
        }
    }

    public enum LevelCompleteState
    {
        LOCKED,
        AVAILABLE,
        COMPLETED,
        ALLGOLD
    }
}
