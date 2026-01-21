using System.Collections.Generic;
using System.IO;

namespace NAPClient
{
    class LogWriter
    {
        public string MatchPath = "LastMatch.txt";
        public string NamesPath = "Names.txt";
        public string ScoresPath = "Scores.txt";
        public string LogPath = "TimerLog.txt";

        public string P1Name = "1p name.txt";
        public string P2Name = "2p name.txt";
        public string P3Name = "3p name.txt";
        public string P4Name = "4p name.txt";

        public string P1Score = "1p score.txt";
        public string P2Score = "2p score.txt";
        public string P3Score = "3p score.txt";
        public string P4Score = "4p score.txt";

        public void WriteCurrentMatchToFile(string matchData)
        {
        }

        public void UpdateNamesFile(List<string> names)
        {

        }

        public void UpdateScoresFile(List<string> scores)
        {

        }

        public void AppendLocalStatsFile(string matchData)
        {
        }
    }
}
