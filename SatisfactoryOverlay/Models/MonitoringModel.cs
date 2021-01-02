namespace SatisfactoryOverlay.Models
{
    using mvvmlib;

    using SatisfactoryOverlay.Obs;
    using SatisfactoryOverlay.Properties;
    using SatisfactoryOverlay.Savegame;

    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public class MonitoringModel
    {
        private readonly FileSystemWatcher watcher;

        private readonly SettingsModel settings;

        private IObsClient obsClient;

        private SavegameHeader activeSavegame;

        private TimeSpan totalPlaytime;

        public string MonitoredSession
        {
            get => settings.LastSessionName;
            set
            {
                settings.LastSessionName = value;
                settings.Save();
            }
        }

        public bool SessionNameVisible
        {
            get => settings.SessionNameVisible;
            set
            {
                if (settings.SessionNameVisible == value) return;
                settings.SessionNameVisible = value;
                settings.Save();
            }
        }

        public bool PlaytimeVisible
        {
            get => settings.PlaytimeVisivble;
            set
            {
                if (settings.PlaytimeVisivble == value) return;
                settings.PlaytimeVisivble = value;
                settings.Save();
            }
        }

        public bool TotalPlaytimeVisible
        {
            get => settings.TotalPlaytimeVisible;
            set
            {
                if (settings.TotalPlaytimeVisible == value) return;
                settings.TotalPlaytimeVisible = value;
                settings.Save();
            }
        }

        public bool StartingZoneVisible
        {
            get => settings.StartingZoneVisible;
            set
            {
                if (settings.StartingZoneVisible == value) return;
                settings.StartingZoneVisible = value;
                settings.Save();
            }
        }

        public bool ModsVisible
        {
            get => settings.ModsVisible;
            set
            {
                if (settings.ModsVisible == value) return;
                settings.ModsVisible = value;
                settings.Save();
            }
        }

        public ObsVariant StreamingTool
        {
            get => settings.StreamingTool;
            set
            {
                if (settings.StreamingTool == value) return;
                settings.StreamingTool = value;
                settings.Save();
            }
        }

        public string ObsElementName
        {
            get => settings.ObsElementName;
            set
            {
                if (settings.ObsElementName == value) return;
                settings.ObsElementName = value;
                settings.Save();
            }
        }

        public string ObsIpAddress
        {
            get => settings.ObsIpAddress;
            set
            {
                if (settings.ObsIpAddress == value) return;
                settings.ObsIpAddress = value;
                settings.Save();
            }
        }

        public int ObsPort
        {
            get => settings.ObsPort;
            set
            {
                if (settings.ObsPort == value) return;
                settings.ObsPort = value;
                settings.Save();
            }
        }

        public string WebsocketPassword
        {
            get => settings.WebsocketPassword;
            set
            {
                if (settings.WebsocketPassword == value) return;
                settings.WebsocketPassword = value;
                settings.Save();
            }
        }

        public bool IsConnected => (obsClient != null) && obsClient.IsConnected;

        public MonitoringModel()
        {
            settings = ServiceLocator.Default.GetService<SettingsModel>();

            watcher = new FileSystemWatcher(settings.SavegameFolder, "*.sav")
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = false
            };

            watcher.Changed += OnSavefileWatcherEvent;
            watcher.Created += OnSavefileWatcherEvent;
            watcher.Renamed += OnSavefileWatcherEvent;
        }

        public async Task StartAsync()
        {
            if (obsClient != null)
            {
                await StopAsync();
                obsClient.OnConnected -= OnConnected;
                obsClient.OnAuthenticated -= OnConnected;
                obsClient.OnDisconnected -= OnDisconnected;
                obsClient.OnClientError -= OnObsError;
            }

            switch (StreamingTool)
            {
                case ObsVariant.Studio:
                    obsClient = new ObsStudioClient(IPAddress.Parse(ObsIpAddress), (ushort)ObsPort, WebsocketPassword);
                    break;
                case ObsVariant.Streamelements:
                    obsClient = new ObsStreamelementsClient(IPAddress.Parse(ObsIpAddress), (ushort)ObsPort, WebsocketPassword);
                    break;
                default:
                    break;
            }

            if (obsClient.NeedsAuthentication)
            {
                obsClient.OnAuthenticated += OnConnected;
            }
            else
            {
                obsClient.OnConnected += OnConnected;
            }
            
            obsClient.OnDisconnected += OnDisconnected;
            obsClient.OnClientError += OnObsError;

            await obsClient.ConnectAsync();

            UpdateSavegameData();

            await UpdateObsOverlayAsync();

            watcher.EnableRaisingEvents = true;
        }

        public async Task StopAsync()
        {
            watcher.EnableRaisingEvents = false;
            await obsClient.DisconnectAsync();
        }

        private async void OnSavefileWatcherEvent(object sender, FileSystemEventArgs e)
        {
            UpdateSavegameData();

            var header = SavegameHeader.Read(e.FullPath);
            if (header.SessionName == MonitoredSession)
            {
                await UpdateObsOverlayAsync();
            }
        }

        public async Task UpdateDisplayOptionsAsync()
        {
            settings.Save();
            UpdateSavegameData();
            await UpdateObsOverlayAsync();
        }

        private void UpdateSavegameData()
        {
            var savegames = from file in Directory.EnumerateFiles(settings.SavegameFolder, "*.sav")
                            select SavegameHeader.Read(file) into header
                            group header by header.SessionName into session
                            select session.OrderByDescending(sh => sh.SaveDate).First();

            activeSavegame = savegames.Where(header => header.SessionName == MonitoredSession).FirstOrDefault();
            totalPlaytime = savegames.Aggregate(TimeSpan.Zero, (result, header) => result += header.PlayTime);
        }

        private async Task UpdateObsOverlayAsync()
        {
            if (activeSavegame == null || obsClient == null || !obsClient.IsConnected || string.IsNullOrWhiteSpace(settings.ObsElementName))
            {
                return;
            }

            string text = string.Empty;
            if (settings.TotalPlaytimeVisible)
            {
                text += $"{Resources.Label_TotalPlaytime}: {totalPlaytime.TotalHours:F0}h {totalPlaytime.Minutes:D2}m {totalPlaytime.Seconds:D2}s\n";
            }
            if (settings.SessionNameVisible)
                text += $"{Resources.CheckBox_SessionName}: {activeSavegame.SessionName}\n";

            if (settings.StartingZoneVisible)
                text += $"{Resources.CheckBox_StartingArea}: {activeSavegame.StartLocation}\n";

            if (settings.PlaytimeVisivble)
                text += $"{Resources.Label_Playtime}: {activeSavegame.PlayTime.TotalHours:F0}h {activeSavegame.PlayTime.Minutes:D2}m {activeSavegame.PlayTime.Seconds:D2}s\n";

            if (settings.ModsVisible)
            {
                string modString = string.Empty;
                foreach (var mod in activeSavegame.Mods)
                {
                    modString += $"   {mod.Key} ({mod.Value})\n";
                }
                text += $"{Resources.CheckBox_Modlist}:\n{modString}\n";
            }

            try
            {
                await obsClient.UpdateDisplayAsync(settings.ObsElementName, text);
            }
            catch (Exception) { } //TODO handle exceptions

        }

        public event EventHandler OnConnected;

        public event EventHandler OnDisconnected;

        public event EventHandler<ObsClientError> OnObsError;
    }
}
