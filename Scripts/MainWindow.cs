using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NAPClient
{
	public partial class MainWindow : Node
	{
		public static MainWindow Instance { get; private set; }

		[Export] Control LevelGrid;
		[Export] PackedScene EpisodeGridScene;
		[Export] TextEdit FilePath;
		[Export] LineEdit UrlEntry;
		[Export] LineEdit SlotNameEntry;
		[Export] LineEdit PasswordEntry;
        [Export] Button LoadFileButton;
		[Export] Button ConnectToApButton;
		[Export] Button HideSetupButton;
		[Export] Button SwitchPlandoAPButton;
		[Export] Control PlandoLoading;
		[Export] Control APLoading;
		[Export] Label StartTimeDisplay;
		[Export] Label GoldValueDisplay;
		[Export] Label MaxTimeDisplay;
		[Export] VBoxContainer RandoLog;
		[Export] ScrollContainer RandoLogContainer;
		[Export] Label LevelIDLabel;
		[Export] Label LevelNameLabel;
		[Export] Label AvailableLabel;
		[Export] Label AllGoldLabel;
		[Export] Label CurrentItemLabel;

		[Export] ConfirmationDialog ConfirmationDialog;
		[Export] FileDialog LoadFileDialog;

		Dictionary<LevelCompleteState, StyleBoxFlat> LevelStateColorPalette = new Dictionary<LevelCompleteState, StyleBoxFlat>();
		Dictionary<EpisodeCompleteState, StyleBoxFlat> EpisodeStateColorPalette = new Dictionary<EpisodeCompleteState, StyleBoxFlat>();

        [Export] Color InaccessibleColor = Colors.Red;
        [Export] Color AccessibleColor = Colors.LightGray;
        [Export] Color BeatenColor = Colors.DarkGray;
        [Export] Color AllGoldColor = Colors.Gold;
		[Export] ColorKey ColorKey;
		[Export] PackedScene LogEntryScene;

		List<Button> LevelButtonList = new List<Button>();
		List<Button> EpisodeButtonList = new List<Button>();

		bool SetupHidden = false;
		bool LoadWithPlando = true;
		bool ServerConnection = false;

		MainLogic Main;

		public override void _Ready()
		{
			Instance = this;
			Main = new MainLogic();
			if (!Main.Initialize(this))
				ForceQuit();

            InitializeColorDictionary();
            GenerateButtonGrid();
            RefreshLevelButtonColors();
        }

        void ForceQuit()
		{
			GetTree().Quit();
        }

		public void OnMemoryError()
		{
            CallDeferred(nameof(ForceQuit));
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
					episode.LevelButtons[j].Text = "-" + j.ToString();
					LevelButtonList.Add(episode.LevelButtons[j]);
					episode.LevelButtons[j].Pressed += () => LevelButtonPressed(levelId);
				}
				episode.EpisodeButton.TooltipText = i.ToString();
				EpisodeButtonList.Add(episode.EpisodeButton);
                episode.EpisodeButton.Pressed += () => EpisodeButtonPressed(i);
				episode.EpisodeButton.Text = LogEntry.GenerateEpisodeName(i);
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
			LevelStateColorPalette[LevelCompleteState.ALLGOLD] = beatenColor;
			LevelStateColorPalette[LevelCompleteState.ALLCHECKS] = allGoldColor;

            EpisodeStateColorPalette = new Dictionary<EpisodeCompleteState, StyleBoxFlat>();
			EpisodeStateColorPalette[EpisodeCompleteState.LOCKED] = inaccessibleColor;
			EpisodeStateColorPalette[EpisodeCompleteState.AVAILABLE] = accessibleColor;
			EpisodeStateColorPalette[EpisodeCompleteState.COMPLETED] = beatenColor;
			EpisodeStateColorPalette[EpisodeCompleteState.ALLCHECKS] = allGoldColor;

            ColorKey.SetColors(InaccessibleColor, AccessibleColor, BeatenColor, AllGoldColor);
		}

		public void OnUIRefresh()
		{
			CallDeferred(nameof(RefreshLevelButtonColors));
			CallDeferred(nameof(RefreshGameStatText));
		}

		public void RefreshLevelButtonColors()
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

				var profileData = MainLogic.MS.LevelProfile[GetLevelIdFromButtonTag(tag)];
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

				var profileData = MainLogic.MS.EpisodeProfile[tag];
				button.AddThemeStyleboxOverride("normal", EpisodeStateColorPalette[profileData.GetEpisodeCompleteState()]);
			}
		}

		public void RefreshGameStatText()
		{
			StartTimeDisplay.Text = MainLogic.MS.LevelStartTime.Value.ToString();
			GoldValueDisplay.Text = MainLogic.MS.TimeGrantedByGold.Value.ToString();
			MaxTimeDisplay.Text = Main.ItemManager.MaxTime.ToString();
		}

		public async void AddToRandoLog(ItemData itemData)
		{
			var newLabel = (LogEntry)LogEntryScene.Instantiate();
			newLabel.SetData(itemData);
            RandoLog.CallDeferred(Node.MethodName.AddChild, newLabel);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CallDeferred(nameof(ScrollToBottom));
        }

		public async void AddToRandoLog(string message)
		{
            var newLabel = (LogEntry)LogEntryScene.Instantiate();
            newLabel.SetData(message);
            RandoLog.CallDeferred(Node.MethodName.AddChild, newLabel);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CallDeferred(nameof(ScrollToBottom));
        }

		void ScrollToBottom()
		{
			RandoLogContainer.SetDeferred("scroll_vertical", (int)RandoLogContainer.GetVScrollBar().MaxValue);
		}

		int GetLevelIdFromButtonTag(int tag)
		{
			return MainLogic.MS.LevelData[tag].GetLevelId();
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
			Main.OnQuit();
		}

		private void LevelButtonPressed(int tag)
		{
            Main.CurrentSelectedButtonId = tag;
			UpdateLevelText(MainLogic.MS.LevelData[tag].GetLevelId());
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

		public void UpdateLevelText(int levelId)
		{
			if (levelId == -1)
			{
				LevelIDLabel.Text = "Error getting level ID";
				return;
			}

			Main.CurrentSelectedLevelId = levelId;
			var levelData = MainLogic.MS.LevelData[Main.CurrentSelectedButtonId];
			var profileData = MainLogic.MS.LevelProfile[levelId];

			LevelIDLabel.Text = LogEntry.GenerateLevelName(levelId);
			LevelNameLabel.Text = levelData.GetLevelName();
			AvailableLabel.Text = profileData.GetLevelCompleteState() == LevelCompleteState.LOCKED ? "LOCKED" : "Available";
			AllGoldLabel.Text = profileData.GetLevelCompleteState() != LevelCompleteState.ALLGOLD ? "No" : "Yes";
		}

		public void UpdateCurrentLevelText(string displayString)
		{
			CallDeferred(nameof(SetCurrentLevelText), displayString);
		}

		void SetCurrentLevelText(string displayString)
		{
            CurrentItemLabel.Text = displayString;
        }

		private void ConnectToServerPressed()
		{
			if (UrlEntry.Text.Length == 0 || SlotNameEntry.Text.Length == 0 || ServerConnection)
				return;
			ServerConnection = true;
			ConnectToApButton.Disabled = true;
			new Thread(TryConnect).Start();
		}

		void TryConnect()
		{
			Main.ConnectToServer(UrlEntry.Text, SlotNameEntry.Text, PasswordEntry.Text);
        }

		public void OnApConnectionEstablished()
		{
			CallDeferred(nameof(SetApButtonAfterConnect));
		}

		void SetApButtonAfterConnect()
		{
            ConnectToApButton.Text = "Disconnect";
            ConnectToApButton.Disabled = false;
            ConnectToApButton.Pressed -= ConnectToServerPressed;
            ConnectToApButton.Pressed += DisconnectFromServerPressed;
        }

		public void OnApConnectionFailed()
		{
			CallDeferred(nameof(SetApButtonAfterFailedConnect));
        }
        void SetApButtonAfterFailedConnect()
        {
            ServerConnection = false;
            ConnectToApButton.Disabled = false;
            AddToRandoLog("Connection failed");
        }

        void DisconnectFromServerPressed()
		{
            CallDeferred(nameof(SetApButtonAfterDisconnect));
        }

        void SetApButtonAfterDisconnect()
        {
            ServerConnection = false;
            ConnectToApButton.Text = "Connect";
            Main.DisconnectFromServer();
            ConnectToApButton.Pressed += ConnectToServerPressed;
            ConnectToApButton.Pressed -= DisconnectFromServerPressed;
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
			Main.LoadLocalRandoFile(FilePath.Text);
		}

		static public void DisplayErrorPopup(string title, string dialogText, Action confirmMethod, Action denyMethod = null)
		{
			Instance.ConfirmationDialog.Title = title;
			Instance.ConfirmationDialog.DialogText = dialogText;
            Instance.ConfirmationDialog.Canceled += denyMethod;
            Instance.ConfirmationDialog.Confirmed += confirmMethod;
            Instance.ConfirmationDialog.PopupCentered();
		}

		private void HideSetupPressed()
		{
			if (SetupHidden)
			{
				if (LoadWithPlando)
					PlandoLoading.Show();
				else
					APLoading.Show();
				SwitchPlandoAPButton.Show();
			}
            else
            {
                if (LoadWithPlando)
                    PlandoLoading.Hide();
                else
                    APLoading.Hide();
				SwitchPlandoAPButton.Hide();
            }

            SetupHidden = !SetupHidden;
			HideSetupButton.Text = SetupHidden ? "Show Setup" : "Hide Setup";
        }

		private void SwitchPlandoAPPressed()
		{
			LoadWithPlando = !LoadWithPlando;
			PlandoLoading.Visible = LoadWithPlando;
			APLoading.Visible = !LoadWithPlando;

			SwitchPlandoAPButton.Text = LoadWithPlando ? "Archipelago Menu" : "Plando Menu";
        }
	}
}
