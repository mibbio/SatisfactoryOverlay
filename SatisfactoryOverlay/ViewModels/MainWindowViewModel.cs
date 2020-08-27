namespace SatisfactoryOverlay.ViewModels
{
    using mvvmlib;

    using SatisfactoryOverlay.EventArgs;
    using SatisfactoryOverlay.Models;
    using SatisfactoryOverlay.Properties;
    using SatisfactoryOverlay.Savegame;
    using SatisfactoryOverlay.Updater;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Input;

    public class MainWindowViewModel : NotifyPropertyChangedBase
    {
        private readonly MonitoringModel monitor;

        public ObservableCollection<string> Sessions { get; } = new ObservableCollection<string>();

        public string MonitoredSession
        {
            get => monitor?.MonitoredSession;
            set
            {
                if (string.Equals(monitor?.MonitoredSession, value)) return;
                monitor.MonitoredSession = value;
                OnPropertyChanged();
            }
        }

        public bool? SessionNameVisible
        {
            get => monitor?.SessionNameVisible;
            set => monitor.SessionNameVisible = (bool)value;
        }

        public bool? PlaytimeVisible
        {
            get => monitor?.PlaytimeVisible;
            set => monitor.PlaytimeVisible = (bool)value;
        }

        public bool? TotalPlaytimeVisible
        {
            get => monitor?.TotalPlaytimeVisible;
            set => monitor.TotalPlaytimeVisible = (bool)value;
        }

        public bool? StartingZoneVisible
        {
            get => monitor?.StartingZoneVisible;
            set => monitor.StartingZoneVisible = (bool)value;
        }

        public bool? ModsVisible
        {
            get => monitor?.ModsVisible;
            set => monitor.ModsVisible = (bool)value;
        }

        public string ObsElementName
        {
            get => monitor?.ObsElementName;
            set => monitor.ObsElementName = value;
        }

        public string ObsIpAddress
        {
            get => monitor?.ObsIpAddress;
            set => monitor.ObsIpAddress = value;
        }

        public int ObsPort
        {
            get => (monitor != null) ? monitor.ObsPort : -1;
            set => monitor.ObsPort = value;
        }

        public string WebsocketPassword
        {
            get => monitor?.WebsocketPassword;
            set => monitor.WebsocketPassword = value;
        }

        private string obsInfo = Resources.Message_Disconnected;

        public string OBSInfo
        {
            get => obsInfo;
            set
            {
                obsInfo = value;
                OnPropertyChanged();
            }
        }

        private ReleaseData _release;

        public ReleaseData Release
        {
            get => _release;
            set
            {
                _release = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUpdate));
            }
        }

        public bool HasUpdate => Release != null;

        public Resources ResData { get; } = new Resources();

        public MainWindowViewModel()
        {
            if (Application.Current is IUpdateNotifier notifier)
            {
                notifier.OnUpdateAvailable += HandleUpdateAvailable;
            }

            try
            {
                monitor = new MonitoringModel();
            }
            catch (Exception)
            {
#if NDEBUG
                MessageBox.Show(Resources.Message_SavegameError, Resources.Message_SavegameErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);

                Application.Current.Shutdown();

                return;
#endif
            }

            monitor.OBSInfoChanged += Model_OBSInfoChanged;
            monitor.OBSConnectFailed += (sender, message) =>
            {
                OBSInfo = message;
            };

            List<SavegameHeader> savegames = new List<SavegameHeader>();
            foreach (var file in Directory.EnumerateFiles(App.SavegameFolder))
            {
                savegames.Add(SavegameHeader.Read(file));
            }

            var grouped = savegames.GroupBy(sg => sg.SessionName);
            foreach (var s in grouped.Select(group => group.Key))
            {
                Sessions.Add(s);
            }
        }

        private void HandleUpdateAvailable(object sender, ReleaseData update)
        {
            Release = update;
        }

        private void Model_OBSInfoChanged(object sender, ConnectionEventArg e)
        {
            switch (e.Status)
            {
                case ConnectionEventArg.ConnectionStatus.Connected:
                    OBSInfo = $"{Resources.Message_Connected} | OBS v{e.ObsVersion} | Plugin v{e.PluginVersion}";
                    break;
                case ConnectionEventArg.ConnectionStatus.Disconnected:
                    OBSInfo = Resources.Message_Disconnected;
                    break;
                default:
                    break;
            }
        }

        private bool IsValidIP(string ip) => IPAddress.TryParse(ip, out var _);

        private ICommand _cmdConnectOBS;

        public ICommand CmdConnectOBS => _cmdConnectOBS ?? (_cmdConnectOBS = new RelayCommand(
            () => monitor.ConnectOBS($"ws://{ObsIpAddress}:{ObsPort}", WebsocketPassword),
            () => (monitor?.IsConnected != true) && IsValidIP(ObsIpAddress)));

        private ICommand _cmdOpenUpdate;

        public ICommand CmdOpenUpdate => _cmdOpenUpdate ?? (_cmdOpenUpdate = new RelayCommand(() =>
        {
            Helper.OpenUrlInBrowser(Release.Link);
            Release = null;
        }));
    }
}
