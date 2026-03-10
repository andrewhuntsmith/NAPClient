using System;
using System.Collections.Generic;

namespace NAPClient
{
    public class LevelProfileMemoryBridge
    {
        public Action ValueUpdated;
        public int BaseLevelPointer;

        public ByteArrayAddressValue TotalLevelProfile = new ByteArrayAddressValue();

        IntAddressValue IdAddressValue = new IntAddressValue();
        IntAddressValue AttemptCountAddressValue = new IntAddressValue();
        IntAddressValue LevelSuccessAddressValue = new IntAddressValue();
        IntAddressValue EpisodeSuccessAddressValue = new IntAddressValue();
        IntAddressValue LevelLockedAddressValue = new IntAddressValue();
        IntAddressValue AllGoldAddressValue = new IntAddressValue();
        bool RefreshLevelFlag;

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
            LevelLockedAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer + 20 } };
            AllGoldAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseLevelPointer + 28 } };

            LevelLockedAddressValue.ValueUpdated += InternalValueUpdated;
            AllGoldAddressValue.ValueUpdated += InternalValueUpdated;
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

            if (RefreshLevelFlag)
            {
                // do refresh logic
                RefreshLevelFlag = false;
            }
        }

        void InternalValueUpdated(int _, int __)
        {
            RefreshLevelFlag = true;
        }

        public int GetLevelId()
        {
            return IdAddressValue.Value;
        }

        public LevelCompleteState GetLevelCompleteState()
        {
            return LevelLockedAddressValue.Value == 0 ? LevelCompleteState.LOCKED :
                LevelLockedAddressValue.Value == 1 ? LevelCompleteState.AVAILABLE :
                AllGoldAddressValue.Value == 0 ? LevelCompleteState.COMPLETED :
                LevelCompleteState.ALLGOLD;
        }

        public void LockLevel()
        {
            LevelLockedAddressValue.SetValue(0);
        }

        public void UnlockLevel()
        {
            LevelLockedAddressValue.SetValue(1);
        }

        public void SetLevelBeaten()
        {
            LevelLockedAddressValue.SetValue(2);
        }

        public void RevokeAllGold()
        {
            AllGoldAddressValue.SetValue(0);
        }

        public void SetAllGold()
        {
            AllGoldAddressValue.SetValue(1);
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
