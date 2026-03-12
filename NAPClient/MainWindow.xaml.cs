using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        Dictionary<LevelCompleteState, SolidColorBrush> LevelStateColorPalette = new Dictionary<LevelCompleteState, SolidColorBrush>();

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
            MS.HookMemory();
            AttachLevelProfileEvents();

            DEBUG_InitializeRandomData();
            InitializeColorDictionary();
            GenerateButtonGrid();
            RefreshLevelButtonColors();

            ItemManager = new ItemManager(MS);
            MS.ExitsEntered.UpdateValue();
            ItemManager.Initializing = false;

            Thread passiveMemoryCheckingThread = new Thread(UpdateThread);
            passiveMemoryCheckingThread.Start();
        }

        bool Loop;
        void UpdateThread()
        {
            Loop = true;
            MS.ExitsEntered.ValueChanged += OnExitsChanged;
            // just run forever lmao
            while (Loop)
            {
                Thread.Sleep(1000);
                MS.ExitsEntered.UpdateValue();
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
                levelProfile.ValueUpdated += OnProfileUpdate;
        }

        // This method only currently exists to have some data to test with
        // In the future this should be generated somewhere else
        void DEBUG_InitializeRandomData()
        {
            CurrentRando.LevelOrder = new List<int>{ 
                10, 76, 105, 71, 82, 84, 97, 87, 63, 116,
                66, 62, 13, 96, 102, 57, 46, 58, 18, 19,
                22, 92, 5, 112, 8, 89, 103, 93, 41, 83,
                33, 40, 23, 88, 4, 31, 80, 55, 48, 85,
                106, 124, 14, 1, 121, 25, 61, 117, 20, 43,
                56, 67, 90, 65, 45, 122, 29, 53, 38, 95,
                123, 81, 0, 28, 42, 36, 69, 94, 64, 98,
                101, 104, 6, 37, 115, 35, 75, 109, 17, 11,
                60, 72, 24, 114, 34, 70, 9, 86, 68, 47,
                7, 16, 51, 77, 107, 59, 26, 32, 54, 99,
                44, 110, 15, 2, 79, 118, 52, 3, 113, 119,
                27, 74, 73, 108, 100, 50, 30, 12, 111, 78,
                120, 49, 21, 91, 39 };

            CurrentRando.InitialLevels = new List<int> { 22, 16, 92 };

            var cond1 = new RandomizationData.CompletionCondition() { Id = 22, State = LevelCompleteState.COMPLETED };
            var cond2 = new RandomizationData.CompletionCondition() { Id = 22, State = LevelCompleteState.ALLGOLD };
            var cond3 = new RandomizationData.CompletionCondition() { Id = 16, State = LevelCompleteState.COMPLETED };
            var cond4 = new RandomizationData.CompletionCondition() { Id = 16, State = LevelCompleteState.ALLGOLD };
            var cond5 = new RandomizationData.CompletionCondition() { Id = 92, State = LevelCompleteState.COMPLETED };
            var cond6 = new RandomizationData.CompletionCondition() { Id = 92, State = LevelCompleteState.ALLGOLD };
            var cond7 = new RandomizationData.CompletionCondition() { Id = 58, State = LevelCompleteState.COMPLETED };

            CurrentRando.UnlockConditions[cond1] = new ItemData() { Value = 69, Type = ItemType.LevelUnlock };
            CurrentRando.UnlockConditions[cond2] = new ItemData() { Value = 76, Type = ItemType.LevelUnlock };
            CurrentRando.UnlockConditions[cond3] = new ItemData() { Value = 122, Type = ItemType.LevelUnlock };
            CurrentRando.UnlockConditions[cond4] = new ItemData() { Value = 79, Type = ItemType.LevelUnlock };
            CurrentRando.UnlockConditions[cond5] = new ItemData() { Value = 58, Type = ItemType.LevelUnlock };
            CurrentRando.UnlockConditions[cond6] = new ItemData() { Value = 102, Type = ItemType.LevelUnlock };
            CurrentRando.UnlockConditions[cond7] = new ItemData() { Value = 21, Type = ItemType.ChangeColorPalette };
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

            if (CurrentSelectedLevelId != -1)
                UpdateLevelText(CurrentSelectedLevelId);
            RefreshLevelButtonColors();
        }

        private void RandomizePressed(object sender, RoutedEventArgs e)
        {
            for (var id = 0; id < CurrentRando.LevelOrder.Count; id++)
            {
                for (var jd = id; jd < MS.LevelProfile.Count; jd++)
                {
                    if (id != jd && MS.LevelData[jd].GetLevelId() == CurrentRando.LevelOrder[id])
                    {
                        MS.SwapLevels(id, jd);
                    }
                }
            }

            foreach (var levelProfile in MS.LevelProfile)
            {
                levelProfile.RevokeAllGold();
                if (CurrentRando.InitialLevels.Contains(levelProfile.GetLevelId()))
                    levelProfile.UnlockLevel();
                else
                    levelProfile.LockLevel();
                levelProfile.UpdateValue();
            }

            RefreshLevelButtonColors();
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

        void OnProfileUpdate(LevelProfileMemoryBridge updatedLevel)
        {
            // check for completion
            if (updatedLevel.GetLevelCompleteState() >= LevelCompleteState.COMPLETED)
            {
                var completionCondition = new RandomizationData.CompletionCondition()
                {
                    Id = updatedLevel.GetLevelId(),
                    State = LevelCompleteState.COMPLETED
                };
                
                if (CurrentRando.UnlockConditions.ContainsKey(completionCondition))
                {
                    ItemManager.HandleCondition(CurrentRando.UnlockConditions[completionCondition]);
                }
            }

            // check for all gold
            if (updatedLevel.GetLevelCompleteState() == LevelCompleteState.ALLGOLD)
            {
                var completionCondition = new RandomizationData.CompletionCondition()
                {
                    Id = updatedLevel.GetLevelId(),
                    State = LevelCompleteState.ALLGOLD
                };

                if (CurrentRando.UnlockConditions.ContainsKey(completionCondition))
                {
                    ItemManager.HandleCondition(CurrentRando.UnlockConditions[completionCondition]);
                }
            }
        }
    }
}
