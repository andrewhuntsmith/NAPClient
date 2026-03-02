using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

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

        public string SlotName = "P1";

        public MainWindow()
        {
            InitializeComponent();
            MS.HookMemory();
            MS.PlayerFinished += UpdatePlayersFinished;
            MS.LevelFinished += FinishCurrentLevel;
            MS.StartNewLevel += StartNewLevel;
            MS.EpisodeStarted += StartNewEpisode;
            UpdateTimeDisplay();
            Thread t = new Thread(UpdateThread);
            t.Start();
        }

        void UpdateThread()
        {
            
        }

        void UpdateTimeDisplay()
        {
            MS.CurrentTimeRemaining.UpdateValue();
            StartTimeEntry.Text = MS.CurrentTimeRemaining.Value.ToString();
            PlayerTimes.Items.Clear();
            var names = new List<string>();
            var scores = new List<string>();

            names.Add(SlotName);
            
            Log.UpdateNamesFile(names);
            Log.UpdateScoresFile(scores);
        }

        void ResetMatchButtonPressed(object sender, RoutedEventArgs e)
        {
            ResetMatch();
        }

        void ResetEpisodeButtonPressed(object sender, RoutedEventArgs e)
        {
            MS.ResetValues();
            UpdateTimeDisplay();
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
        }

        void EndMatchButtonPressed(object sender, RoutedEventArgs e)
        {
            ResetMatch();
        }

        void ResetMatch()
        {
            MS.ResetValues();
            UpdateTimeDisplay();
        }

        void UpdateNamesButtonPressed(object sender, RoutedEventArgs e)
        {
            
        }

        void HookToGameButtonPressed(object sender, RoutedEventArgs e)
        {

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
        }

        void FinishCurrentLevel()
        {
            
        }

        void StartNewEpisode()
        {
            
        }

        void StartNewLevel()
        {
            
        }

        void UpdatePlayersFinished(int playerIndex, int frameCount, double bonus, int goldCollected)
        {
            
        }
    }
}
