using System;
using System.Collections.Generic;

namespace NAPClient
{
    public class EpisodeProfileMemoryBridge
    {
        // episode profile data is almost exactly the same as level data in most of the important ways
        // the only major difference is that G++ is calculated by G++ all its levels,
        //      rather than as a flag on the episode data
        public Action<EpisodeProfileMemoryBridge> ValueUpdated;
        public int BaseEpisodePointer;

        public ByteArrayAddressValue TotalEpisodeProfile = new ByteArrayAddressValue();

        IntAddressValue IdAddressValue = new IntAddressValue();
        IntAddressValue AttemptCountAddressValue = new IntAddressValue();
        IntAddressValue LevelSuccessAddressValue = new IntAddressValue();
        IntAddressValue EpisodeSuccessAddressValue = new IntAddressValue();
        ByteArrayAddressValue EpisodeLockedAddressValue = new ByteArrayAddressValue();
        bool RefreshEpisodeFlag;

        public EpisodeProfileMemoryBridge(int basePointer)
        {
            BaseEpisodePointer = basePointer;

            TotalEpisodeProfile = new ByteArrayAddressValue()
            {
                Offsets = new List<int> { BaseEpisodePointer },
                ArraySize = 48
            };
            IdAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseEpisodePointer } };
            AttemptCountAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseEpisodePointer + 4 } };
            LevelSuccessAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseEpisodePointer + 12 } };
            EpisodeSuccessAddressValue = new IntAddressValue() { Offsets = new List<int> { BaseEpisodePointer + 16 } };
            EpisodeLockedAddressValue = new ByteArrayAddressValue() { Offsets = new List<int> { BaseEpisodePointer + 20 }, ArraySize = 1 };

            AttemptCountAddressValue.ValueUpdated += InternalValueUpdated;
            EpisodeLockedAddressValue.ValueUpdated += InternalValueUpdated;
            LevelSuccessAddressValue.ValueUpdated += InternalValueUpdated;
            EpisodeSuccessAddressValue.ValueUpdated += InternalValueUpdated;
        }

        public void UpdateValue()
        {
            TotalEpisodeProfile.UpdateValue();
            IdAddressValue.UpdateValue();
            AttemptCountAddressValue.UpdateValue();
            LevelSuccessAddressValue.UpdateValue();
            EpisodeSuccessAddressValue.UpdateValue();
            EpisodeLockedAddressValue.UpdateValue();

            if (RefreshEpisodeFlag)
            {
                ValueUpdated?.Invoke(this);
                RefreshEpisodeFlag = false;
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
            RefreshEpisodeFlag = true;
        }

        public int GetEpisodeId()
        {
            return IdAddressValue.Value;
        }

        public EpisodeCompleteState GetEpisodeCompleteState()
        {
            return EpisodeLockedAddressValue.Value[0] == 0 ? EpisodeCompleteState.LOCKED :
                EpisodeLockedAddressValue.Value[0] == 1 ? EpisodeCompleteState.AVAILABLE :
                EpisodeCompleteState.COMPLETED;
        }

        public void LockEpisode()
        {
            EpisodeLockedAddressValue.SetValue(new byte[] { 0 });
        }

        public void UnlockEpisode()
        {
            EpisodeLockedAddressValue.SetValue(new byte[] { 1 });
        }

        public void SetEpisodeBeaten()
        {
            EpisodeLockedAddressValue.SetValue(new byte[] { 2 });
        }
    }

    public enum EpisodeCompleteState
    {
        LOCKED,
        AVAILABLE,
        COMPLETED
    }
}
