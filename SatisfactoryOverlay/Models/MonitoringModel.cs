namespace SatisfactoryOverlay.Models
{
    using OBS.WebSocket.NET;

    using SatisfactoryOverlay.EventArgs;
    using SatisfactoryOverlay.Properties;
    using SatisfactoryOverlay.Savegame;

    using System;
    using System.Data;
    using System.IO;
    using System.Linq;

    public class MonitoringModel
    {
        private readonly FileSystemWatcher watcher;

        private readonly ObsWebSocket obs;

        private readonly SettingsModel settings;

        private SavegameHeader activeSavegame;

        public string MonitoredSession
        {
            get => settings.LastSessionName;
            set
            {
                settings.LastSessionName = value;
                UpdateSavegame();
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
                UpdateOBS();
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
                UpdateOBS();
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
                UpdateOBS();
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
                UpdateOBS();
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
                UpdateOBS();
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

        public bool IsConnected { get; private set; }

        public MonitoringModel()
        {
            watcher = new FileSystemWatcher(App.SavegameFolder, "*.sav")
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.FileName,
                IncludeSubdirectories = false
            };

            watcher.Changed += OnSavefileWatcherEvent;
            watcher.Created += OnSavefileWatcherEvent;
            watcher.Renamed += OnSavefileWatcherEvent;
            watcher.EnableRaisingEvents = true;

            obs = new ObsWebSocket() { Timeout = TimeSpan.FromSeconds(30) };

            obs.Connected += Obs_Connected;
            obs.Disconnected += Obs_Disconnected;

            settings = SettingsModel.PopulateSettings();

            UpdateSavegame();
        }

        private void OnSavefileWatcherEvent(object sender, FileSystemEventArgs e)
        {
            var header = SavegameHeader.Read(e.FullPath);
            if (header.SessionName == MonitoredSession)
            {
                UpdateOBS(header);
            }
        }

        public void ConnectOBS(string url, string password)
        {
            try
            {
                obs.Connect(url, password);
                if (obs.Connection.State != WebSocket4Net.WebSocketState.Open)
                {
                    OBSConnectFailed?.Invoke(this, Resources.Message_CanNotConnect);
                }
            }
            catch (AuthFailureException)
            {
                OBSConnectFailed?.Invoke(this, Resources.Message_WrongPassword);
            }
        }

        private void UpdateSavegame()
        {
            activeSavegame = GetLatestSaveForSession(MonitoredSession);
            UpdateOBS();
        }

        private void UpdateOBS() => UpdateOBS(activeSavegame);

        private void UpdateOBS(SavegameHeader header)
        {
            if (obs.IsConnected && !string.IsNullOrWhiteSpace(settings.ObsElementName))
            {
                try
                {
                    var props = obs.Api.GetTextGDIPlusProperties(settings.ObsElementName);
                    if (header == null)
                    {
                        props.Text = string.Empty;
                    }
                    else
                    {
                        props.Text = string.Empty;
                        if (settings.SessionNameVisible)
                            props.Text += $"{Resources.CheckBox_SessionName}: {header.SessionName}\n";

                        if (settings.StartingZoneVisible)
                            props.Text += $"{Resources.CheckBox_StartingArea}: {header.StartLocation}\n";

                        if (settings.PlaytimeVisivble)
                            props.Text += $"{Resources.CheckBox_Playtime}: {header.PlayTime.TotalHours:F0}h {header.PlayTime.Minutes:D2}m {header.PlayTime.Seconds:D2}s\n";

                        if (settings.ModsVisible)
                        {
                            string modString = string.Empty;
                            foreach (var mod in header.Mods)
                            {
                                modString += $"   {mod.Key} ({mod.Value})\n";
                            }
                            props.Text += $"{Resources.CheckBox_Modlist}:\n{modString}\n";
                        }
                    }

                    obs.Api.SetTextGDIPlusProperties(props);
                }
                catch (ErrorResponseException) { }
            }
        }

        private SavegameHeader GetLatestSaveForSession(string sessionName)
        {
            return Directory.EnumerateFiles(App.SavegameFolder)
                .Select(file => SavegameHeader.Read(file))
                .Where(header => header.SessionName == sessionName)
                .OrderByDescending(header => header.SaveDate).FirstOrDefault();
        }

        public event EventHandler<ConnectionEventArg> OBSInfoChanged;

        public event EventHandler<string> OBSConnectFailed;

        private void Obs_Disconnected(object sender, EventArgs e)
        {
            IsConnected = false;
            OBSInfoChanged?.Invoke(this, new ConnectionEventArg(ConnectionEventArg.ConnectionStatus.Disconnected, string.Empty, string.Empty));
        }

        private void Obs_Connected(object sender, EventArgs e)
        {
            IsConnected = true;

            UpdateOBS();

            var v = obs.GetVersion();
            var args = new ConnectionEventArg(ConnectionEventArg.ConnectionStatus.Connected, v.OBSStudioVersion, v.PluginVersion);
            OBSInfoChanged?.Invoke(this, args);
        }
    }
}
