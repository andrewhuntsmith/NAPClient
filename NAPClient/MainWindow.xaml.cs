using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        LogWriter Log = new LogWriter();

        SolidColorBrush InaccessibleColor = Brushes.Black;
        SolidColorBrush AccessibleColor = Brushes.LightGray;
        SolidColorBrush BeatenColor = Brushes.DarkGray;
        SolidColorBrush AllGoldColor = Brushes.Gold;

        SolidColorBrush RectangleBackground = Brushes.DarkGray;
        SolidColorBrush RectangleOutline = Brushes.Black;

        List<Button> LevelButtonList = new List<Button>();
        List<Button> EpisodeButtonList = new List<Button>();

        int CurrentSelectedLevelId = -1;

        public MainWindow()
        {
            InitializeComponent();
            MS.HookMemory();

            GenerateButtonGrid();
            RefreshLevelButtonColors();
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

                var profileData = MS.LevelProfile[tag];
                button.Background = profileData[28] != 0 ? AllGoldColor :
                    profileData[20] == 0 ? InaccessibleColor :
                    profileData[20] == 1 ? AccessibleColor :
                    BeatenColor;
            }
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

            CurrentSelectedLevelId = tag;
            var levelData = MS.LevelData[tag];
            var profileData = MS.LevelProfile[tag];
            var nameArray = new byte[129];
            Array.Copy(levelData, nameArray, nameArray.Length);

            LevelIDLabel.Content = profileData[0];
            LevelNameLabel.Content = System.Text.Encoding.UTF8.GetString(nameArray);
            AvailableLabel.Content = profileData[20] == 0 ? "LOCKED" : "Available";
            AllGoldLabel.Content = profileData[28] == 0 ? "No" : "Yes";
        }

        private void EpisodeButtonPressed(object sender, RoutedEventArgs e) 
        {
            LevelNameLabel.Content = "Episode functionality not yet hooked up";
        }

        private void CycleLevelStatusPressed(object sender, RoutedEventArgs e)
        {
            CycleLevelStatus();
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
            if (profileData[20] == 0) // level is locked, set it to available
            {
                MS.UpdateLevelProfileValue(CurrentSelectedLevelId, 20, 1);
                profileData[20] = 1;
                RefreshLevelButtonColors();
                return;
            }
            else if (profileData[20] == 1) // level is available, set it to beaten
            {
                MS.UpdateLevelProfileValue(CurrentSelectedLevelId, 20, 2);
                profileData[20] = 2;
                RefreshLevelButtonColors();
                return;
            }
            else if (profileData[28] == 0)
            {
                MS.UpdateLevelProfileValue(CurrentSelectedLevelId, 28, 1);
                profileData[28] = 1;
                RefreshLevelButtonColors();
                return;
            }
            else
            {
                MS.UpdateLevelProfileValue(CurrentSelectedLevelId, 20, 0);
                MS.UpdateLevelProfileValue(CurrentSelectedLevelId, 28, 0);
                profileData[20] = 0;
                profileData[28] = 0;
                RefreshLevelButtonColors();
                return;
            }
        }
    }
}
