
using NAPClient;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

        ItemManager.Initializing = false;
        GoalManager.Initializing = false;

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
            levelProfile.ValueUpdated += OnLevelProfileUpdate;
        foreach (var episodeProfile in MS.EpisodeProfile)
            episodeProfile.ValueUpdated += OnEpisodeProfileUpdate;
    }

    void AddToRandoLog(ItemData item)
    {
        GodotTreeNode.AddToRandoLog(item);
    }

    void OnAPConnectionEstablished(List<int> levelOrder)
    {
        CurrentRando.LevelOrder = levelOrder;
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
        }

        if (GoalManager.CheckMetGoal())
        {
            HandleGoalCompletion();
        }
    }

    void HandleGoalCompletion()
    {
        GodotTreeNode.AddToRandoLog("Goal met!!! 🎉");
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
