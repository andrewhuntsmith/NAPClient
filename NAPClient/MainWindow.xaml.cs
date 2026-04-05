using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAPClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static List<string> PlayerNames;

        public static MemorySource MS = new MemorySource();
        ItemManager ItemManager;
        GoalManager GoalManager;
        ArchipelagoManager ApManager;

        Dictionary<LevelCompleteState, SolidColorBrush> LevelStateColorPalette = new Dictionary<LevelCompleteState, SolidColorBrush>();
        Dictionary<EpisodeCompleteState, SolidColorBrush> EpisodeStateColorPalette = new Dictionary<EpisodeCompleteState, SolidColorBrush>();

        SolidColorBrush InaccessibleColor = Brushes.Red;
        SolidColorBrush AccessibleColor = Brushes.LightGray;
        SolidColorBrush BeatenColor = Brushes.DarkGray;
        SolidColorBrush AllGoldColor = Brushes.Gold;

        SolidColorBrush RectangleBackground = Brushes.DarkGray;
        SolidColorBrush RectangleOutline = Brushes.Black;

        List<Button> LevelButtonList = new List<Button>();
        List<Button> EpisodeButtonList = new List<Button>();

        int CurrentSelectedLevelId = -1;
        int CurrentSelectedButtonId = -1;

        RandomizationData CurrentRando = new RandomizationData();

        public MainWindow()
        {
            InitializeComponent();
            if (MS.HookMemory() == false)
            {
                Close();
                return;
            }
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
            // the caller of this function does not own the UI, because it is a different thread
            // therefore we need to call methods through dispatchers like this
            LevelGrid.Dispatcher.Invoke(() => UpdateLevelStatus());
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
            for (var i = 0; i < 25; i++)
            {
                var newRect = new Rectangle()
                {
                    StrokeThickness = 1,
                    Stroke = RectangleOutline,
                    Fill = RectangleBackground
                };
                Grid.SetRow(newRect, i % 5);
                Grid.SetColumn(newRect, i / 5);
                LevelGrid.Children.Add(newRect);

                var levelColumnDef1 = new ColumnDefinition() { Width = new GridLength(24, GridUnitType.Pixel) };
                var levelColumnDef2 = new ColumnDefinition() { Width = new GridLength(24, GridUnitType.Pixel) };
                var levelColumnDef3 = new ColumnDefinition() { Width = new GridLength(24, GridUnitType.Pixel) };
                var levelRowDef1 = new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) };
                var levelRowDef2 = new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) };
                var levelRowDef3 = new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) };

                var episodeGrid = new Grid();
                episodeGrid.ColumnDefinitions.Add(levelColumnDef1);
                episodeGrid.ColumnDefinitions.Add(levelColumnDef2);
                episodeGrid.ColumnDefinitions.Add(levelColumnDef3);
                episodeGrid.RowDefinitions.Add(levelRowDef1);
                episodeGrid.RowDefinitions.Add(levelRowDef2);
                episodeGrid.RowDefinitions.Add(levelRowDef3);
                Grid.SetRow(episodeGrid, i % 5);
                Grid.SetColumn(episodeGrid, i / 5);
                LevelGrid.Children.Add(episodeGrid);

                for (var j = 0; j < 5; j++)
                {
                    var levelButton = new Button()
                    {
                        Margin = new Thickness(2),
                        Name = "BtnLevel" + (i * 5 + j).ToString(),
                        IsEnabled = true,
                        Tag = i * 5 + j
                    };

                    levelButton.Click += LevelButtonPressed;
                    Grid.SetRow(levelButton, j / 3);
                    Grid.SetColumn(levelButton, j % 3);
                    episodeGrid.Children.Add(levelButton);
                    LevelButtonList.Add(levelButton);
                }

                var episodeButton = new Button()
                {
                    Height = 20,
                    Margin = new Thickness(2),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = 12,
                    Name = "BtnEpisode" + i.ToString(),
                    Tag = i,
                    Content = GenerateEpisodeName(i)
                };
                episodeButton.Click += EpisodeButtonPressed;
                Grid.SetRow(episodeButton, 2);
                Grid.SetColumnSpan(episodeButton, 3);
                episodeGrid.Children.Add(episodeButton);
                EpisodeButtonList.Add(episodeButton);
            }
        }

        void InitializeColorDictionary()
        {
            LevelStateColorPalette = new Dictionary<LevelCompleteState, SolidColorBrush>();
            LevelStateColorPalette[LevelCompleteState.LOCKED] = InaccessibleColor;
            LevelStateColorPalette[LevelCompleteState.AVAILABLE] = AccessibleColor;
            LevelStateColorPalette[LevelCompleteState.COMPLETED] = BeatenColor;
            LevelStateColorPalette[LevelCompleteState.ALLGOLD] = AllGoldColor;

            EpisodeStateColorPalette = new Dictionary<EpisodeCompleteState, SolidColorBrush>();
            EpisodeStateColorPalette[EpisodeCompleteState.LOCKED] = InaccessibleColor;
            EpisodeStateColorPalette[EpisodeCompleteState.AVAILABLE] = AccessibleColor;
            EpisodeStateColorPalette[EpisodeCompleteState.COMPLETED] = BeatenColor;
        }

        void RefreshLevelButtonColors()
        {
            foreach (var button in LevelButtonList)
            {
                var tag = -1;
                int.TryParse(button.Tag.ToString(), out tag);
                if (tag == -1)
                {
                    LevelIDLabel.Content = "Error getting level ID";
                    return;
                }

                var profileData = MS.LevelProfile[GetLevelIdFromButtonTag(tag)];
                button.Background = LevelStateColorPalette[profileData.GetLevelCompleteState()];
            }

            foreach (var button in EpisodeButtonList)
            {
                var tag = -1;
                int.TryParse(button.Tag.ToString(), out tag);
                if (tag == -1)
                {
                    LevelIDLabel.Content = "Error getting episode ID";
                    return;
                }

                var profileData = MS.EpisodeProfile[tag];
                button.Background = EpisodeStateColorPalette[profileData.GetEpisodeCompleteState()];
            }
        }

        void RefreshGameStatText()
        {
            StartTimeDisplay.Content = MS.LevelStartTime.Value.ToString();
            GoldValueDisplay.Content = MS.TimeGrantedByGold.Value.ToString();
            MaxTimeDisplay.Content = ItemManager.MaxTime.ToString();
        }

        public void AddToRandoLog(string message)
        {
            RandoLog.Items.Add(message);
            RandoLog.ScrollIntoView(RandoLog.Items[RandoLog.Items.Count-1]);
        }

        int GetLevelIdFromButtonTag(int tag)
        {
            return MS.LevelData[tag].GetLevelId();
        }

        void ApplyStartTimeValueButtonPressed(object sender, RoutedEventArgs e)
        {
            var textEntry = string.IsNullOrEmpty(StartTimeEntry.Text) ? "90" : StartTimeEntry.Text;
            var textAsDouble = double.Parse(textEntry);
            MS.ApplyStartTimeValue(textAsDouble);
        }

        void SwapTwoLevelsButtonPressed(object sender, RoutedEventArgs e)
        {
            var firstLevelText = string.IsNullOrEmpty(FirstLevelEntry.Text) ? "0" : FirstLevelEntry.Text;
            var secondLevelText = string.IsNullOrEmpty(SecondLevelEntry.Text) ? "1" : SecondLevelEntry.Text;
            var firstTextAsInt = int.Parse(firstLevelText);
            var secondTextAsInt = int.Parse(secondLevelText);
            MS.SwapLevels(firstTextAsInt, secondTextAsInt);

            RefreshLevelButtonColors();
        }

        void HookToGameButtonPressed(object sender, RoutedEventArgs e)
        {

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Loop = false;

            // TODO put levels back in the right order
            // alternately, just force quit the game? that should fix everything that needs to be fixed
            foreach (var level in MS.OriginalLevelMapping)
            {
                MS.SwapLevels(level.Key, MS.NewLevelMapping[MS.OriginalLevelMapping[level.Key]]);
            }

            MS.LevelStartTime.SetValue(90f);
            MS.TimeGrantedByGold.SetValue(2f);
            MS.ReenableScoreSubmission();
        }

        private void LevelButtonPressed(object sender, RoutedEventArgs e)
        {
            var tag = -1;
            int.TryParse((sender as Button).Tag.ToString(), out tag);
            if (tag == -1)
            {
                LevelIDLabel.Content = "Error getting level ID";
                return;
            }

            CurrentSelectedButtonId = tag;
            UpdateLevelText(MS.LevelData[tag].GetLevelId());
        }

        private void EpisodeButtonPressed(object sender, RoutedEventArgs e)
        {
            LevelNameLabel.Content = "Episode functionality not yet hooked up";
        }

        private void CycleLevelStatusPressed(object sender, RoutedEventArgs e)
        {
            CycleLevelStatus();
        }

        private void UpdateLevelStatusPressed(object sender, RoutedEventArgs e)
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
                LevelIDLabel.Content = "Error getting level ID";
                return;
            }

            CurrentSelectedLevelId = levelId;
            var levelData = MS.LevelData[CurrentSelectedButtonId];
            var profileData = MS.LevelProfile[levelId];

            LevelIDLabel.Content = profileData.GetLevelId();
            LevelNameLabel.Content = levelData.GetLevelName();
            AvailableLabel.Content = profileData.GetLevelCompleteState() == LevelCompleteState.LOCKED ? "LOCKED" : "Available";
            AllGoldLabel.Content = profileData.GetLevelCompleteState() != LevelCompleteState.ALLGOLD ? "No" : "Yes";
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
                LevelIDLabel.Content = "Error getting level ID";
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

        private void ConnectToServerPressed(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag.ToString() == "Connect")
            {
                if (UrlEntry.Text.Length == 0 || SlotNameEntry.Text.Length == 0)
                    return;
                if (!ApManager.TryConnect(UrlEntry.Text, SlotNameEntry.Text, PasswordEntry.Password))
                    return;
                ((Button)sender).Content = "Disconnect";
                ((Button)sender).Tag = "Disconnect";
            }
            else
            {
                ((Button)sender).Content = "Connect";
                ((Button)sender).Tag = "Connect";
            }
        }

        private void BrowseLocalFiles(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileBrowser = new OpenFileDialog();

            if (fileBrowser.ShowDialog() == true)
            {
                FilePath.Text = fileBrowser.FileName;
            }
        }

        private void LoadLocalRandoFile(object sender, RoutedEventArgs e)
        {
            var randoString = File.ReadAllText(FilePath.Text);
            var randoObject = JsonConvert.DeserializeObject<RandomizationData>(randoString, new RandomizationDataConverter());

            CurrentRando = randoObject;
            RandomizeLevels();
        }
    }
}


