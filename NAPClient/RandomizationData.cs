using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAPClient
{
    public class RandomizationData
    {
        public class CompletionCondition
        {
            public int Id;
            public LevelCompleteState State;

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

        public List<int> LevelOrder = new List<int>();
        public List<int> InitialLevels = new List<int>();
        public Dictionary<CompletionCondition, ItemData> UnlockConditions = new Dictionary<CompletionCondition, ItemData>();
    }
}
