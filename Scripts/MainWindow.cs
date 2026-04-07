using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace NAPClient
{
	public partial class MainWindow : Node
	{
		public static MainWindow Instance { get; private set; }

		[Export] Control LevelGrid;
		[Export] PackedScene EpisodeGridScene;
		[Export] TextEdit FilePath;
		[Export] Button LoadFileButton;
		[Export] Label StartTimeDisplay;
		[Export] Label GoldValueDisplay;
		[Export] Label MaxTimeDisplay;
		[Export] VBoxContainer RandoLog;
		[Export] ScrollContainer RandoLogContainer;
		[Export] Label LevelIDLabel;
		[Export] Label LevelNameLabel;
		[Export] Label AvailableLabel;
		[Export] Label AllGoldLabel;

		[Export] ConfirmationDialog ConfirmationDialog;
		[Export] FileDialog LoadFileDialog;

        public static MemorySource MS = new MemorySource();
		ItemManager ItemManager;
		GoalManager GoalManager;
		ArchipelagoManager ApManager;

		Dictionary<LevelCompleteState, StyleBoxFlat> LevelStateColorPalette = new Dictionary<LevelCompleteState, StyleBoxFlat>();
		Dictionary<EpisodeCompleteState, StyleBoxFlat> EpisodeStateColorPalette = new Dictionary<EpisodeCompleteState, StyleBoxFlat>();

		Color InaccessibleColor = Colors.Red;
		Color AccessibleColor = Colors.LightGray;
		Color BeatenColor = Colors.DarkGray;
		Color AllGoldColor = Colors.Gold;

		Color RectangleBackground = Colors.DarkGray;
		Color RectangleOutline = Colors.Black;
		
		List<Button> LevelButtonList = new List<Button>();
		List<Button> EpisodeButtonList = new List<Button>();

		int CurrentSelectedLevelId = -1;
		int CurrentSelectedButtonId = -1;

		RandomizationData CurrentRando = new RandomizationData();

		public override void _Ready()
		{
			Instance = this;
			if (MS.HookMemory() == false)
			{
				ForceQuit();
				return;
			}

			MS.MemoryError += OnMemoryError;
			AttachLevelProfileEvents();

			InitializeColorDictionary();
			GenerateButtonGrid();
			RefreshLevelButtonColors();

			ItemManager = new ItemManager(MS);
			ItemManager.ItemAdded += AddToRandoLog;

			GoalManager = new GoalManager(MS);

			ApManager = new ArchipelagoManager(MS, ItemManager);
			ApManager.APConnectionEstablished += OnAPConnectionEstablished;

			MS.LevelVictories.UpdateValue();
			MS.EpisodeVictories.UpdateValue();
			ItemManager.Initializing = false;

			Thread passiveMemoryCheckingThread = new Thread(UpdateThread);
			passiveMemoryCheckingThread.Start();

		}

		void OnMemoryError()
		{
			 CallDeferred(nameof(ForceQuit));
		}

		void ForceQuit()
		{
			GetTree().Quit();
        }

		bool Loop;
		void UpdateThread()
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

		void OnExitsChanged()
		{
			CallDeferred(nameof(UpdateLevelStatus));
		}

		void AttachLevelProfileEvents()
		{
			foreach (var levelProfile in MS.LevelProfile)
				levelProfile.ValueUpdated += OnLevelProfileUpdate;
			foreach (var episodeProfile in MS.EpisodeProfile)
				episodeProfile.ValueUpdated += OnEpisodeProfileUpdate;
		}

		void GenerateButtonGrid()
		{
			for (int i = 0; i < 25; i++)
			{
				var episode = (EpisodeGrid)EpisodeGridScene.Instantiate();
				LevelGrid.AddChild(episode);

				for (int j = 0; j < 5; j++)
				{
					var levelId = (i * 5) + j;
					episode.LevelButtons[j].TooltipText = levelId.ToString();
					LevelButtonList.Add(episode.LevelButtons[j]);
					episode.LevelButtons[j].Pressed += () => LevelButtonPressed(levelId);
				}
				episode.EpisodeButton.TooltipText = i.ToString();
				EpisodeButtonList.Add(episode.EpisodeButton);
                episode.EpisodeButton.Pressed += () => EpisodeButtonPressed(i);
            }
        }

		void InitializeColorDictionary()
		{
			var inaccessibleColor = new StyleBoxFlat();
			inaccessibleColor.BgColor = InaccessibleColor;
            inaccessibleColor.CornerDetail = 4;
            inaccessibleColor.SetCornerRadiusAll(4);

            var accessibleColor = new StyleBoxFlat();
            accessibleColor.BgColor = AccessibleColor;
            accessibleColor.CornerDetail = 4;
            accessibleColor.SetCornerRadiusAll(4);

            var beatenColor = new StyleBoxFlat();
            beatenColor.BgColor = BeatenColor;
            beatenColor.CornerDetail = 4;
			beatenColor.SetCornerRadiusAll(4);

            var allGoldColor = new StyleBoxFlat();
            allGoldColor.BgColor = AllGoldColor;
			allGoldColor.CornerDetail = 4;
            allGoldColor.SetCornerRadiusAll(4);

            LevelStateColorPalette = new Dictionary<LevelCompleteState, StyleBoxFlat>();
			LevelStateColorPalette[LevelCompleteState.LOCKED] = inaccessibleColor;
			LevelStateColorPalette[LevelCompleteState.AVAILABLE] = accessibleColor;
			LevelStateColorPalette[LevelCompleteState.COMPLETED] = beatenColor;
			LevelStateColorPalette[LevelCompleteState.ALLGOLD] = allGoldColor;

			EpisodeStateColorPalette = new Dictionary<EpisodeCompleteState, StyleBoxFlat>();
			EpisodeStateColorPalette[EpisodeCompleteState.LOCKED] = inaccessibleColor;
			EpisodeStateColorPalette[EpisodeCompleteState.AVAILABLE] = accessibleColor;
			EpisodeStateColorPalette[EpisodeCompleteState.COMPLETED] = beatenColor;
		}

		void RefreshLevelButtonColors()
		{
			foreach (var button in LevelButtonList)
			{
				var tag = -1;
				int.TryParse(button.TooltipText, out tag);
				if (tag == -1)
				{
					LevelIDLabel.Text = "Error getting level ID";
					return;
				}

				var profileData = MS.LevelProfile[GetLevelIdFromButtonTag(tag)];
                button.AddThemeStyleboxOverride("normal", LevelStateColorPalette[profileData.GetLevelCompleteState()]);
			}

			foreach (var button in EpisodeButtonList)
			{
				var tag = -1;
				int.TryParse(button.TooltipText, out tag);
				if (tag == -1)
				{
					LevelIDLabel.Text = "Error getting episode ID";
					return;
				}

				var profileData = MS.EpisodeProfile[tag];
				button.AddThemeStyleboxOverride("normal", EpisodeStateColorPalette[profileData.GetEpisodeCompleteState()]);
			}
		}

		void RefreshGameStatText()
		{
			StartTimeDisplay.Text = MS.LevelStartTime.Value.ToString();
			GoldValueDisplay.Text = MS.TimeGrantedByGold.Value.ToString();
			MaxTimeDisplay.Text = ItemManager.MaxTime.ToString();
		}

		public void AddToRandoLog(string message)
		{
			var newLabel = new Label() { Text = message };
			RandoLog.AddChild(newLabel);
			CallDeferred(nameof(ScrollToBottom));
		}

		void ScrollToBottom()
		{
			RandoLogContainer.ScrollVertical = (int)RandoLogContainer.GetVScrollBar().MaxValue;
		}

		int GetLevelIdFromButtonTag(int tag)
		{
			return MS.LevelData[tag].GetLevelId();
		}

		//void ApplyStartTimeValueButtonPressed(object sender, RoutedEventArgs e)
		//{
			//var textEntry = string.IsNullOrEmpty(StartTimeEntry.Text) ? "90" : StartTimeEntry.Text;
			//var textAsDouble = double.Parse(textEntry);
			//MS.ApplyStartTimeValue(textAsDouble);
		//}
//
		//void SwapTwoLevelsButtonPressed(object sender, RoutedEventArgs e)
		//{
			//var firstLevelText = string.IsNullOrEmpty(FirstLevelEntry.Text) ? "0" : FirstLevelEntry.Text;
			//var secondLevelText = string.IsNullOrEmpty(SecondLevelEntry.Text) ? "1" : SecondLevelEntry.Text;
			//var firstTextAsInt = int.Parse(firstLevelText);
			//var secondTextAsInt = int.Parse(secondLevelText);
			//MS.SwapLevels(firstTextAsInt, secondTextAsInt);
//
			//RefreshLevelButtonColors();
		//}
//
		//void HookToGameButtonPressed(object sender, RoutedEventArgs e)
		//{
//
		//}

		public override void _ExitTree()
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

		private void LevelButtonPressed(int tag)
		{
			CurrentSelectedButtonId = tag;
			UpdateLevelText(MS.LevelData[tag].GetLevelId());
		}

		private void EpisodeButtonPressed(int tag)
		{
			LevelNameLabel.Text = "Episode functionality not yet hooked up";
		}

		//private void CycleLevelStatusPressed()
		//{
			//CycleLevelStatus();
		//}
//
		//private void UpdateLevelStatusPressed()
		//{
			//UpdateLevelStatus();
		//}

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
				UpdateLevelText(CurrentSelectedLevelId);
			RefreshLevelButtonColors();
		}

		void OnAPConnectionEstablished(List<int> levelOrder)
		{
			CurrentRando.LevelOrder = levelOrder;
			RandomizeLevels();
		}

		void RandomizeLevels()
		{
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

			RefreshLevelButtonColors();
			RefreshGameStatText();
			AddToRandoLog("Randomizer began!");
		}

		void UpdateLevelText(int levelId)
		{
			if (levelId == -1)
			{
				LevelIDLabel.Text = "Error getting level ID";
				return;
			}

			CurrentSelectedLevelId = levelId;
			var levelData = MS.LevelData[CurrentSelectedButtonId];
			var profileData = MS.LevelProfile[levelId];

			LevelIDLabel.Text = profileData.GetLevelId().ToString();
			LevelNameLabel.Text = levelData.GetLevelName();
			AvailableLabel.Text = profileData.GetLevelCompleteState() == LevelCompleteState.LOCKED ? "LOCKED" : "Available";
			AllGoldLabel.Text = profileData.GetLevelCompleteState() != LevelCompleteState.ALLGOLD ? "No" : "Yes";
		}

		string GenerateEpisodeName(int index)
		{
			var letters = "ABCDE";
			var letter = letters[index % 5];
			var number = index / 5;
			return "SI-" + letter + "-" + number.ToString();
		}

		void CycleLevelStatus()
		{
			if (CurrentSelectedLevelId == -1)
			{
				LevelIDLabel.Text = "Error getting level ID";
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
			UpdateLevelText(CurrentSelectedLevelId);
			RefreshLevelButtonColors();
			return;
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
			RefreshGameStatText();
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
			AddToRandoLog("Goal met!!! 🎉");
		}

		private void ConnectToServerPressed()
		{
			//if (((Button)sender).Tag.ToString() == "Connect")
			//{
			//	if (UrlEntry.Text.Length == 0 || SlotNameEntry.Text.Length == 0)
			//		return;
			//	if (!ApManager.TryConnect(UrlEntry.Text, SlotNameEntry.Text, PasswordEntry.Password))
			//		return;
			//	((Button)sender).Content = "Disconnect";
			//	((Button)sender).Tag = "Disconnect";
			//}
			//else
			//{
			//	((Button)sender).Content = "Connect";
			//	((Button)sender).Tag = "Connect";
			//}
		}

		private void BrowseLocalFiles()
		{
            LoadFileDialog.PopupCentered();
		}

		private void LocalFileSelected(string path)
		{
            FilePath.Text = path;
            LoadFileButton.Disabled = false;
        }

		private void LoadLocalRandoFile()
		{
			var randoString = File.ReadAllText(FilePath.Text);
			var randoObject = JsonConvert.DeserializeObject<RandomizationData>(randoString, new RandomizationDataConverter());

			CurrentRando = randoObject;
			RandomizeLevels();
		}

		static public void DisplayErrorPopup(string title, string dialogText, Action confirmMethod, Action denyMethod = null)
		{
			Instance.ConfirmationDialog.Title = title;
			Instance.ConfirmationDialog.DialogText = dialogText;
            Instance.ConfirmationDialog.Canceled += denyMethod;
            Instance.ConfirmationDialog.Confirmed += confirmMethod;
            Instance.ConfirmationDialog.PopupCentered();
		}
	}
}
