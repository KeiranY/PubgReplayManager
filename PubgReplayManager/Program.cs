using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PubgReplayManager.Library.Forms;

namespace PubgReplayManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    class Replay
    {

        public static SortableBindingList<Replay> LoadedReplays = new SortableBindingList<Replay>();
        public static SortableBindingList<Replay> BackupReplays = new SortableBindingList<Replay>();

        public string         dir { get; set; }

        public TimeSpan       length { get; set; }
        public DateTimeOffset timestamp { get; set; }
        public string         mode { get; set; }
        public bool           fpp { get; set; }
        public string         steamID { get; set; }
        public string         username { get; set; }
        public string         map { get; set; }
        public bool           locked { get; set; }

        public Replay(string directory)
        {
	    // Name of the folder
	    dir = new DirectoryInfo(directory).Name;
            var infoFile = File.ReadAllText(Path.Combine(directory, Form1.ReplayInfoFile), Encoding.UTF8);
            length    = TimeSpan.FromMilliseconds(double.Parse(infoFile.Split(new[] {"\"LengthInMS\": "}, StringSplitOptions.None)[1].Split(',')[0]));
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(infoFile.Split(new[] { "\"Timestamp\":" }, StringSplitOptions.None)[1].Split(',')[0]));
            var _mode = infoFile.Split(new[] {"\"Mode\": "}, StringSplitOptions.None)[1].Split('"')[1];
            fpp       = _mode.Contains('-');
            mode      = fpp ? _mode.Split('-')[0] : _mode;
            mode = char.ToUpper(mode[0]) + mode.Substring(1);
            steamID   = infoFile.Split(new[] { "\"RecordUserId\": " }, StringSplitOptions.None)[1].Split('"')[1];
            username  = infoFile.Split(new[] { "\"RecordUserNickName\": " }, StringSplitOptions.None)[1].Split('"')[1];
            map       = infoFile.Split(new[] { "\"MapName\": " }, StringSplitOptions.None)[1].Split('"')[1] == "Desert_Main" ? "Miramar" : "Erangel";
            locked    = infoFile.Split(new[] { "\"bShouldKeep\": " }, StringSplitOptions.None)[1].Split(',')[0] == "true";
        }
    }


}
