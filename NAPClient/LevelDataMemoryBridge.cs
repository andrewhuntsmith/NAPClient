using System;
using System.Collections.Generic;

namespace NAPClient
{
    public class LevelDataMemoryBridge
    {
        public Action<LevelDataMemoryBridge> ValueUpdated;
        public int BaseLevelPointer;

        public ByteArrayAddressValue TotalLevelData = new ByteArrayAddressValue();

        ByteArrayAddressValue PreNameBytes = new ByteArrayAddressValue();
        IntAddressValue LevelId = new IntAddressValue();
        StringAddressValue LevelName = new StringAddressValue();
        StringAddressValue AuthorName = new StringAddressValue();
        ByteArrayAddressValue TileSet = new ByteArrayAddressValue();
        ByteArrayAddressValue ItemSet = new ByteArrayAddressValue();
        bool RefreshLevelFlag;

        public LevelDataMemoryBridge(int basePointer)
        {
            BaseLevelPointer = basePointer;

            TotalLevelData = new ByteArrayAddressValue() 
            { 
                Offsets = new List<int> { BaseLevelPointer },
                ArraySize = 1228 
            };
            PreNameBytes = new ByteArrayAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer },
                ArraySize = 30
            };
            LevelId = new IntAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer }
            };
            LevelName = new StringAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer + 30 },
                StringLength = 129
            };
            AuthorName = new StringAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer + 30 + 129 },
                StringLength = 17
            };
            TileSet = new ByteArrayAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer + 30 + 129 + 17 },
                ArraySize = 966 // Levels are 42 across, 23 tall, and each block is represented by one byte
            };
            ItemSet = new ByteArrayAddressValue()
            {
                Offsets = new List<int> { BaseLevelPointer + 30 + 129 + 17 + 966 },
                ArraySize = 86 // I think this is indexing into item data somewhere else, these bytes definitely don't represent item data directly
            };

            LevelName.ValueUpdated += InternalValueUpdated;
        }

        public void UpdateValue()
        {
            TotalLevelData.UpdateValue();
            PreNameBytes.UpdateValue();
            LevelId.UpdateValue();
            LevelName.UpdateValue();
            AuthorName.UpdateValue();
            TileSet.UpdateValue();
            ItemSet.UpdateValue();
            if (RefreshLevelFlag)
            {
                ValueUpdated?.Invoke(this);
                RefreshLevelFlag = false;
            }
        }

        void InternalValueUpdated(string _, string __)
        {
            RefreshLevelFlag = true;
        }

        public string GetLevelName()
        {
            return LevelName.Value;
        }

        public int GetLevelId()
        {
            return LevelId.Value;
        }

        public void SetLevelId(int id)
        {
            LevelId.SetValue(id);
        }
    }
}
