namespace SatisfactoryOverlay
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows;

    public class RotatingDisplayLog
    {
        private readonly int maxLines;

        public ObservableCollection<string> Entries { get; private set; } = new ObservableCollection<string>();

        public string Logfile { get; private set; }

        public RotatingDisplayLog(int maximumLines, string logfile = "")
        {
            if (maximumLines <= 0)
            {
                throw new ArgumentException("Number has to be greater than 0.");
            }

            maxLines = maximumLines;
            Logfile = logfile;
        }

        public void AddLine(string line, bool bypassDisplay = false)
        {
            if (!string.IsNullOrWhiteSpace(Logfile))
            {
                try
                {
                    using var writer = File.AppendText(Logfile);
                    writer.WriteLine(line);
                }
                catch (IOException) { }
            }

            if (bypassDisplay) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (Entries)
                {
                    if (Entries.Count >= maxLines)
                    {
                        var remainingLines = Entries.Skip(1).Take(maxLines - 1).ToList();
                        Entries.Clear();
                        foreach (var entry in remainingLines)
                        {
                            Entries.Add(entry);
                        }
                        Entries.Add(line);
                    }
                    else
                    {
                        Entries.Add(line);
                    }
                }
            });
        }
    }
}
