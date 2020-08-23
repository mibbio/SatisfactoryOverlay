namespace SatisfactoryOverlay.Savegame
{
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    internal sealed class SavegameHeader
    {
        public string FilePath { get; set; }

        public string FileName => Path.GetFileName(FilePath);

        public int HeaderVersion { get; set; }

        public int SaveVersion { get; set; }

        public int BuildVersion { get; set; }

        public string SessionName { get; set; }

        public string StartLocation { get; set; }

        public TimeSpan PlayTime { get; set; }

        public DateTime SaveDate { get; set; }

        public Dictionary<string, Version> Mods { get; private set; } = new Dictionary<string, Version>()
        {
            {"not implemented", new Version() }
        };

        private SavegameHeader() { }

        public static SavegameHeader Read(string path)
        {
            if (!File.Exists(path)) throw new ArgumentException("File does not exist!");

            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            var header = new SavegameHeader
            {
                FilePath = path
            };

            while (true)
            {
                try
                {
                    using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        header.HeaderVersion = input.ReadInt();
                        header.SaveVersion = input.ReadInt();
                        header.BuildVersion = input.ReadInt();
                        // skip world type
                        input.ReadSatisfactoryString();

                        var rawProps = input.ReadSatisfactoryString().Trim('\0', '?');
                        foreach (var p in rawProps.Split('?'))
                        {
                            var kv = p.Split('=');
                            if (kv[0] == "startloc")
                            {
                                header.StartLocation = kv[1];
                            }
                            if (kv[0] == "SML_ModList")
                            {
                                var decodedBytes = Convert.FromBase64String(GetValidBase64(kv[1]));
                                var decodedString = Encoding.UTF8.GetString(decodedBytes);

                                header.Mods = JsonConvert.DeserializeObject<Dictionary<string, Version>>(decodedString);
                            }
                        }

                        header.SessionName = input.ReadSatisfactoryString();
                        header.PlayTime = TimeSpan.FromSeconds(input.ReadInt());
                        header.SaveDate = new DateTime(input.ReadLong(), DateTimeKind.Utc);

                        return header;
                    }
                }
                catch (IOException)
                {
                    int win32Error = Marshal.GetLastWin32Error();

                    if (win32Error == 32)
                    {
                        using (var lockWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), "*" + Path.GetExtension(path)) { EnableRaisingEvents = true })
                        {
                            lockWatcher.Changed += (o, e) =>
                            {
                                if (Path.GetFullPath(e.FullPath) == Path.GetFullPath(path))
                                {
                                    autoResetEvent.Set();
                                }
                            };

                            autoResetEvent.WaitOne();
                        }
                    }
                }
            }
        }

        private static string GetValidBase64(string input)
        {
            if (input.Length % 4 > 0)
            {
                return input.PadRight(input.Length + 4 - input.Length % 4, '=');
            }
            return input;
        }
    }
}
