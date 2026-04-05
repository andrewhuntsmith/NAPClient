using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Windows;

namespace NAPClient
{
    public class RandomizationDataConverter : JsonConverter<RandomizationData>
    {
        class JSONUnlockCondition
        {
            public JSONCompletionCondition CompletionCondition { get; set; }
            public JSONItemData ItemData { get; set; }
        }

        class JSONCompletionCondition
        {
            public int Id { get; set; }
            public string State { get; set; }
        }

        class JSONItemData
        {
            public int Value { get; set; }
            public string Type { get; set; }
        }

        public override RandomizationData ReadJson(JsonReader reader, Type objectType, RandomizationData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            RandomizationData returnObject = new RandomizationData();
            returnObject.LevelOrder = jo["RandomizationData"]["LevelOrder"].ToObject<int[]>().ToList();
            returnObject.InitialLevels = jo["RandomizationData"]["InitialLevels"].ToObject<int[]>().ToList();
            returnObject.StartingLevelTime = jo["RandomizationData"]["StartingLevelTime"].ToObject<float>();
            returnObject.StartingGoldValue = jo["RandomizationData"]["StartingGoldValue"].ToObject<float>();
            returnObject.InitialMaxTime = jo["RandomizationData"]["InitialMaxTime"].ToObject<double>();
            
            var goalParse = Enum.TryParse(jo["RandomizationData"]["Goal"].ToString(), true, out GoalType goal);
            if (!goalParse)
            {
                string caption = "Error reading json!";
                string errorMessage = "Cannot parse goal condition: " + jo["RandomizationData"]["Goal"].ToString();
                MessageBox.Show(errorMessage, caption);
                return null;
            }
            returnObject.Goal = goal;

            var unlockConditions = jo["RandomizationData"]["UnlockConditions"].ToObject<JSONUnlockCondition[]>();
            foreach (var condition in unlockConditions)
            {
                var stateParse = Enum.TryParse(condition.CompletionCondition.State, true, out ProgressState state);
                if (!stateParse)
                {
                    string caption = "Error reading json!";
                    string errorMessage = "Cannot parse completion condition state: " + condition.CompletionCondition.State;
                    MessageBox.Show(errorMessage, caption);
                    return null;
                }

                var typeParse = Enum.TryParse(condition.ItemData.Type, true, out ItemType type);
                if (!typeParse)
                {
                    string caption = "Error reading json!";
                    string errorMessage = "Cannot parse item data type: " + condition.ItemData.Type;
                    MessageBox.Show(errorMessage, caption);
                    return null;
                }

                var compCondition = new RandomizationData.CompletionCondition()
                {
                    Id = condition.CompletionCondition.Id,
                    State = state
                };
                var itemData = new ItemData()
                {
                    Value = condition.ItemData.Value,
                    Type = type
                };
                returnObject.UnlockConditions[compCondition] = itemData;
            }

            return returnObject;
        }

        public override void WriteJson(JsonWriter writer, RandomizationData value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
