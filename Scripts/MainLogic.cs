
using Archipelago.MultiClient.Net;
using NAPClient;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static NAPClient.RandomizationData;

public class MainLogic
{
    public static MainLogic Instance { get; private set; }

    public static MemorySource MS = new MemorySource();
    public ItemManager ItemManager;
    GoalManager GoalManager;
    ArchipelagoManager ApManager;
    MainWindow GodotTreeNode;

    public int CurrentSelectedLevelId = -1;
    public int CurrentSelectedButtonId = -1;

    RandomizationData CurrentRando = new RandomizationData();

    public bool Initialize(MainWindow parent)
    {
        Instance = this;
        GodotTreeNode = parent;
        if (MS.HookMemory() == false)
        {
            return false;
        }

        MS.MemoryError += OnMemoryError;
        AttachLevelProfileEvents();

        ItemManager = new ItemManager(MS);
        ItemManager.ItemAdded += AddToRandoLog;

        GoalManager = new GoalManager(MS);

        ApManager = new ArchipelagoManager(MS, ItemManager);
        ApManager.APConnectionEstablished += OnAPConnectionEstablished;

        MS.LevelVictories.UpdateValue();
        MS.EpisodeVictories.UpdateValue();

        Thread passiveMemoryCheckingThread = new Thread(UpdateThread);
        passiveMemoryCheckingThread.Start();

        return true;
    }

    void OnMemoryError()
    {
        GodotTreeNode.OnMemoryError();
    }

    bool Loop;
    public void UpdateThread()
    {
        Loop = true;
        MS.LevelVictories.ValueChanged += OnExitsChanged;
        MS.EpisodeVictories.ValueChanged += OnExitsChanged;
        // just run forever lmao
        while (Loop)
        {
            MS.LevelVictories.UpdateValue();
            MS.EpisodeVictories.UpdateValue();
            MS.GoldCollectedInCurrentLevel.UpdateValue();
        }
    }

    public void OnQuit()
    {
        Loop = false;

        if (!MemorySource.ConnectedToGame)
            return;

        // TODO force quit the game, that should fix everything that needs to be fixed
        foreach (var level in MS.OriginalLevelMapping)
        {
            MS.SwapLevels(level.Key, MS.NewLevelMapping[MS.OriginalLevelMapping[level.Key]]);
        }

        MS.LevelStartTime.SetValue(90f);
        MS.TimeGrantedByGold.SetValue(2f);
        MS.ReenableScoreSubmission();
    }

    void AttachLevelProfileEvents()
    {
        foreach (var levelProfile in MS.LevelProfile)
        {
            levelProfile.ValueUpdated += OnLevelProfileUpdate;
            levelProfile.OnChallengeCompleted += OnLevelChallengeCompleted;
        }
        foreach (var episodeProfile in MS.EpisodeProfile)
            episodeProfile.ValueUpdated += OnEpisodeProfileUpdate;
    }

    void AddToRandoLog(ItemData item)
    {
        GodotTreeNode.AddToRandoLog(item);
    }

    void OnAPConnectionEstablished(LoginSuccessful loginSuccess)
    {
        var levelOrder = JsonConvert.DeserializeObject<List<int>>(loginSuccess.SlotData["level_data"].ToString());
        CurrentRando.LevelOrder = levelOrder;
        var challenges = JsonConvert.DeserializeObject<List<List<int>>>(loginSuccess.SlotData["challenge_data"].ToString());
        CurrentRando.Challenges = challenges;

        var objective = JsonConvert.DeserializeObject<int>(loginSuccess.SlotData["objective"].ToString());
        CurrentRando.Goal = (GoalType)objective;
        var startingTime = JsonConvert.DeserializeObject<int>(loginSuccess.SlotData["initial_starting_time"].ToString());
        CurrentRando.StartingLevelTime = startingTime;
        var maxTime = JsonConvert.DeserializeObject<int>(loginSuccess.SlotData["initial_max_time"].ToString());
        CurrentRando.InitialMaxTime = maxTime;
        var goldValue = JsonConvert.DeserializeObject<int>(loginSuccess.SlotData["initial_gold_time"].ToString());
        CurrentRando.StartingGoldValue = goldValue;

        //Right now we simply start with the first level unlocked
        CurrentRando.InitialLevels = new List<int> { 0, 5, 10, 15, 20 };
        RandomizeLevels();
    }

    void RandomizeLevels()
    {
        ItemManager.Initializing = true;
        GoalManager.Initializing = true;

        MS.LevelStartTime.SetValue(CurrentRando.StartingLevelTime);
        MS.TimeGrantedByGold.SetValue(CurrentRando.StartingGoldValue);
        ItemManager.SetMaxTime(CurrentRando.InitialMaxTime);
        GoalManager.SetGoal(CurrentRando.Goal);

        for (var id = 0; id < CurrentRando.LevelOrder.Count; id++)
        {
            MS.SwapLevels(id, MS.NewLevelMapping[MS.OriginalLevelMapping[CurrentRando.LevelOrder[id]]]);
        }

        foreach (var levelProfile in MS.LevelProfile)
        {
            levelProfile.RevokeAllGold();
            if (CurrentRando.InitialLevels.Contains(levelProfile.GetLevelId()))
            {
                ItemManager.LevelUnlockManager.AddLevelToUnlocks(levelProfile.GetLevelId());
                levelProfile.UnlockLevel();
            }
            else
            {
                ItemManager.LevelUnlockManager.RemoveLevelFromUnlocks(levelProfile.GetLevelId());
                levelProfile.LockLevel();
            }
            levelProfile.UpdateValue();
        }

        foreach (var episodeProfile in MS.EpisodeProfile)
        {
            episodeProfile.LockEpisode();
        }

        GodotTreeNode.OnUIRefresh();
        GodotTreeNode.AddToRandoLog("Randomizer began!");

        ItemManager.Initializing = false;
        GoalManager.Initializing = false;

        ItemManager.ApplyPreviouslyReceivedItemsToRando();
        AssignChallengeData();
    }

    void AssignChallengeData()
    {
        for (var i = 0; i < CurrentRando.Challenges.Count; i++)
        {
            MS.LevelProfile[i].SetChallengeData(CurrentRando.Challenges[i]);
        }
    }

    void OnExitsChanged()
    {
        UpdateLevelStatus();
    }

    void UpdateLevelStatus()
    {
        foreach (var levelProfile in MS.LevelProfile)
        {
            levelProfile.UpdateValue();
        }

        foreach (var episodeProfile in MS.EpisodeProfile)
        {
            episodeProfile.UpdateValue();
        }

        if (CurrentSelectedLevelId != -1)
            GodotTreeNode.UpdateLevelText(CurrentSelectedLevelId);
        GodotTreeNode.OnUIRefresh();
    }

    void OnLevelProfileUpdate(LevelProfileMemoryBridge updatedLevel)
    {
        // check for level becoming unlocked when it shouldn't be
        if (updatedLevel.GetLevelCompleteState() >= LevelCompleteState.AVAILABLE)
            if (!ItemManager.LevelUnlockManager.ShouldLevelUnlock(updatedLevel))
                return;

        // check for completion
        if (updatedLevel.GetLevelCompleteState() >= LevelCompleteState.COMPLETED)
        {
            var completionCondition = new RandomizationData.CompletionCondition()
            {
                Id = updatedLevel.GetLevelId(),
                State = ProgressState.LevelComplete
            };

            if (CurrentRando.UnlockConditions.ContainsKey(completionCondition))
            {
                ItemManager.HandleCondition(CurrentRando.UnlockConditions[completionCondition]);
                CurrentRando.UnlockConditions.Remove(completionCondition);
            }

            if (ApManager.IsConnected())
            {
                ApManager.SendItem(completionCondition);
            }
        }

        // check for all gold
        if (updatedLevel.GetLevelCompleteState() == LevelCompleteState.ALLGOLD)
        {
            var completionCondition = new RandomizationData.CompletionCondition()
            {
                Id = updatedLevel.GetLevelId(),
                State = ProgressState.LevelAllGold
            };

            if (CurrentRando.UnlockConditions.ContainsKey(completionCondition))
            {
                ItemManager.HandleCondition(CurrentRando.UnlockConditions[completionCondition]);
                CurrentRando.UnlockConditions.Remove(completionCondition);
            }
        }

        if (GoalManager.CheckMetGoal())
        {
            HandleGoalCompletion();
        }
        GodotTreeNode.OnUIRefresh();
    }

    void OnLevelChallengeCompleted(int levelId, int challengeIndex)
    {
        var completionCondition = new CompletionCondition()
        {
            Id = levelId,
            State = challengeIndex == 0 ? ProgressState.LevelChallenge1 :
                    challengeIndex == 1 ? ProgressState.LevelChallenge2 :
                    ProgressState.LevelChallenge3
        };

        if (CurrentRando.UnlockConditions.ContainsKey(completionCondition))
        {
            ItemManager.HandleCondition(CurrentRando.UnlockConditions[completionCondition]);
            CurrentRando.UnlockConditions.Remove(completionCondition);
        }

        if (ApManager.IsConnected()) 
        { 
            ApManager.SendItem(completionCondition);
        }
    }

    void OnEpisodeProfileUpdate(EpisodeProfileMemoryBridge updatedEpisode)
    {
        // check for level becoming unlocked when it shouldn't be
        if (updatedEpisode.GetEpisodeCompleteState() >= EpisodeCompleteState.AVAILABLE)
            if (!ItemManager.LevelUnlockManager.ShouldEpisodeUnlock(updatedEpisode))
                return;

        // check for completion
        if (updatedEpisode.GetEpisodeCompleteState() >= EpisodeCompleteState.COMPLETED)
        {
            var completionCondition = new RandomizationData.CompletionCondition()
            {
                Id = updatedEpisode.GetEpisodeId(),
                State = ProgressState.EpisodeComplete
            };

            if (CurrentRando.UnlockConditions.ContainsKey(completionCondition))
            {
                ItemManager.HandleCondition(CurrentRando.UnlockConditions[completionCondition]);
                CurrentRando.UnlockConditions.Remove(completionCondition);
            }

            if (ApManager.IsConnected())
            {
                ApManager.SendItem(completionCondition);
            }
        }

        if (GoalManager.CheckMetGoal())
        {
            HandleGoalCompletion();
        }
        GodotTreeNode.OnUIRefresh();
    }

    void HandleGoalCompletion()
    {
        GodotTreeNode.AddToRandoLog("Goal met!!! 🎉");
        if (ApManager.IsConnected())
        {
            ApManager.SendGoalMet();
        }
    }

    public void LoadLocalRandoFile(string filePath)
    {
        var randoString = File.ReadAllText(filePath);
        var randoObject = JsonConvert.DeserializeObject<RandomizationData>(randoString, new RandomizationDataConverter());

        CurrentRando = randoObject;
        RandomizeLevels();
    }

    public void ConnectToServer(string url, string slot, string password)
    {
        if (!ApManager.TryConnect(url, slot, password))
            return;
    }

    void CycleLevelStatus()
    {
        if (CurrentSelectedLevelId == -1)
        {
            return;
        }

        var profileData = MS.LevelProfile[CurrentSelectedLevelId];
        if (profileData.GetLevelCompleteState() == LevelCompleteState.LOCKED)
        {
            profileData.UnlockLevel();
        }
        else if (profileData.GetLevelCompleteState() == LevelCompleteState.AVAILABLE)
        {
            profileData.SetLevelBeaten();
        }
        else if (profileData.GetLevelCompleteState() == LevelCompleteState.COMPLETED)
        {
            profileData.SetAllGold();
        }
        else
        {
            profileData.LockLevel();
            profileData.RevokeAllGold();
        }

        profileData.UpdateValue();
        GodotTreeNode.UpdateLevelText(CurrentSelectedLevelId);
        GodotTreeNode.RefreshLevelButtonColors();
        return;
    }
}
