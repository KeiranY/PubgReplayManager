﻿namespace PubgReplayManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using Properties;

    public partial class Form1 : Form
    {
        private const string ReplayInfoFile = @"\\PUBG.replayinfo";

        private static readonly string keepFalse = "\"bShouldKeep\": false";
        private static readonly string keepTrue = "\"bShouldKeep\": true";

        public Form1()
        {
            InitializeComponent();
            // On first open, set these settings to their defaults
            if (string.IsNullOrEmpty(Settings.Default.ReplaysFolder))
            {
                Settings.Default.ReplaysFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TslGame\\Saved\\Demos\\";
            }
            if (string.IsNullOrEmpty(Settings.Default.BackupsFolder))
            {
                Settings.Default.BackupsFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\PubgReplayManager\\";
            }
            // Create the backup folder if it doesn't exist
            Directory.CreateDirectory(Settings.Default.BackupsFolder);
            // Check for the existence of the ReplaysFolder
            if (!Directory.Exists(Settings.Default.ReplaysFolder))
            {
                // If ReplaysFolder does not exist, have the user search for a new one, or close
                if (MessageBox.Show(@"PUBG replays folder does not exist in default location, would you like to manually find the folder?", @"Replay Folder missing", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var replayBrowser = new FolderBrowserDialog();
                    if (replayBrowser.ShowDialog() == DialogResult.OK)
                    {
                        Settings.Default.ReplaysFolder = replayBrowser.SelectedPath;
                    }
                    else
                    {
                        Close();
                    }
                }
                else
                {
                    Close();
                }
            }

            LoadReplays();
            RefreshButton.Click += (sender, args) => LoadReplays();

            // Add Replays to DataTable
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = Replay.LoadedReplays;
            dataGridView1.CellMouseUp += dataGridView_CellValueChanged;
            dataGridView2.AutoGenerateColumns = false;
            dataGridView2.DataSource = Replay.BackupReplays;
            dataGridView2.CellMouseUp += dataGridView_CellValueChanged;

            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn {HeaderText = @"Locked", DataPropertyName = "locked", Width = 50});

            dataGridView1.Columns.Add("Time", "Time");
            dataGridView1.Columns["Time"].DataPropertyName = "timestamp";
            dataGridView1.Columns["Time"].DefaultCellStyle.Format = "dd/MM/yy HH:mm";

            dataGridView1.Columns.Add("Length", "Length");
            dataGridView1.Columns["Length"].DataPropertyName = "length";
            dataGridView1.Columns["Length"].DefaultCellStyle.Format = @"mm\:ss";
            dataGridView1.Columns["Length"].Width = 50;

            dataGridView1.Columns.Add("Map", "Map");
            dataGridView1.Columns["Map"].DataPropertyName = "map";
            dataGridView1.Columns["Map"].Width = 50;

            dataGridView1.Columns.Add("Mode", "Mode");
            dataGridView1.Columns["Mode"].DataPropertyName = "mode";
            dataGridView1.Columns["Mode"].Width = 50;

            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn {HeaderText = @"FPP", DataPropertyName = "fpp", Width = 50});

            // Hidden column containing folder location
            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn {Name = "dir", DataPropertyName = "dir", Visible = false});

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Automatic;
                column.ReadOnly = column.HeaderText != "Locked";
                // Add the same layout to backup grid view
                dataGridView2.Columns.Add((DataGridViewColumn) column.Clone());
            }

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            RefreshDisplays();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                string name = row.Cells["dir"].Value.ToString();
                string source = Settings.Default.ReplaysFolder + name;
                string dest = Settings.Default.BackupsFolder + name;
                if (Directory.Exists(dest))
                {
                    MessageBox.Show(@"The backup directory for this replay already exists, have you backed this up before?\nLocation: " + dest, @"Directory Exists", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    try
                    {
                        Directory.Move(source, dest);
                        Replay movedReplay = Replay.LoadedReplays.First(x => x.dir == name);
                        Replay.BackupReplays.Add(movedReplay);
                        Replay.LoadedReplays.Remove(movedReplay);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show(@"Unable to backup the file, if the problem persists try closing PUBG or running this program as Administrator.", @"Unable to move", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            RefreshDisplays();
        }

        private void restoreButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView2.SelectedRows)
            {
                string name = row.Cells["dir"].Value.ToString();
                string dest = Settings.Default.ReplaysFolder + name;
                string source = Settings.Default.BackupsFolder + name;
                if (Directory.Exists(dest))
                {
                    MessageBox.Show(@"The restore directory for this replay already exists, have you restored this before?\nLocation: " + dest, @"Directory Exists", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    try
                    {
                        Directory.Move(source, dest);
                        Replay movedReplay = Replay.BackupReplays.First(x => x.dir == name);
                        Replay.LoadedReplays.Add(movedReplay);
                        Replay.BackupReplays.Remove(movedReplay);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show(@"Unable to backup the file, if the problem persists try closing PUBG or running this program as Administrator.", @"Unable to move", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            RefreshDisplays();
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                return;
            }

            var grid = (DataGridView) sender;
            if (grid.Columns[e.ColumnIndex].HeaderText == "Locked")
            {
                object dir = grid.Rows[e.RowIndex].Cells["dir"].Value;
                string file = (grid.Name == "dataGridView1" ? Settings.Default.ReplaysFolder : Settings.Default.BackupsFolder) + dir + ReplayInfoFile;
                string text = File.ReadAllText(file);
                if (text.Contains(keepTrue))
                {
                    File.WriteAllText(file, text.Replace(keepTrue, keepFalse));
                }
                else
                {
                    File.WriteAllText(file, text.Replace(keepFalse, keepTrue));
                }
            }
        }

        private void LoadReplays()
        {
            Replay.LoadedReplays.Clear();
            Replay.BackupReplays.Clear();

            // Enumerate Replays
            foreach (string replay in Directory.GetDirectories(Settings.Default.ReplaysFolder))
            {
                //Check for the replay info file to make sure this is a real replay folder
                if (File.Exists(replay + ReplayInfoFile))
                {
                    Replay.LoadedReplays.Add(new Replay(replay));
                }
            }

            // Enumerate Backups
            foreach (string replay in Directory.GetDirectories(Settings.Default.BackupsFolder))
            {
                //Check for the replay info file to make sure this is a real replay folder
                if (File.Exists(replay + ReplayInfoFile))
                {
                    Replay.BackupReplays.Add(new Replay(replay));
                }
            }
        }

        private void RefreshDisplays()
        {
            // Display replay Count
            label1.Text = @"Replays: " + Replay.LoadedReplays.Count + @"/20";
            label2.Text = @"Backups: " + Replay.BackupReplays.Count;
        }

        // When the program is edited save our settings
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.Save();
        }

        private void Export_Click(object sender, EventArgs e)
        {
            // Export all backed up replays. TODO: Support both loaded and backed up replays
            IEnumerable<Replay> selected = dataGridView2.SelectedRows.Cast<DataGridViewRow>().Select(row => row.Cells["dir"].Value.ToString()).Select(name => Replay.BackupReplays.First(x => x.dir == name)).ToList();

            // Ensure that only zip files can be chosen
            saveFileDialog1.Filter = @"Zip File (*.zip)|*.zip";

            // Only proceed if a file has been successfully selected
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            // The name of the file we'll be exporting to
            string zipFileName = saveFileDialog1.FileName;

            // Run the archiving in a seperate non-blocking thread. TODO: Could this be shaven down?
            new Thread(() =>
                       {
                           // Create a MemoryStream that holds an in-memory zip archive
                           using (var memoryStream = new MemoryStream())
                           {
                               // Create a ZipArchive that writes to the MemoryStream
                               using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                               {
                                   // Get the name of all the replays that should be saved
                                   foreach (string replay in selected.Select(r => r.dir))
                                   {
                                       // Get the full path of the replay
                                       string source = Path.Combine(Settings.Default.BackupsFolder, replay);

                                       // Get all the files in all subdirectories
                                       string[] allFiles = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

                                       // Loop through each file contained in each replay
                                       foreach (string file in allFiles)
                                       {
                                           // Create an entry in the archive for the file. The archive uses paths relative to the root, so we remove everything from the path that is higher or equal to the root
                                           ZipArchiveEntry entry = archive.CreateEntry(file.Remove(0, Settings.Default.BackupsFolder.Length));

                                           // Open the stream associated with the new entry
                                           using (Stream entryStream = entry.Open())
                                           {
                                               // Create a StreamWriter to easily write to the stream
                                               using (var streamWriter = new StreamWriter(entryStream))
                                               {
                                                   // Open the file (using the full path)
                                                   using (FileStream fileStream = File.OpenRead(file))
                                                   {
                                                       // Use a StreamReader to make reading easier
                                                       using (var streamReader = new StreamReader(fileStream))
                                                       {
                                                           // Write the whole FileStream into the zip stream
                                                           streamWriter.Write(streamReader.ReadToEnd());
                                                       }
                                                   }
                                               }
                                           }
                                       }
                                   }
                               }

                               // Open the output zip file
                               using (var fileStream = new FileStream(zipFileName, FileMode.Create))
                               {
                                   // Revert to the beginning of the zip stream
                                   memoryStream.Seek(0, SeekOrigin.Begin);

                                   // Copy the zip archive to the file on disk
                                   memoryStream.CopyTo(fileStream);
                               }
                           }
                       }).Start();
        }
    }
}
