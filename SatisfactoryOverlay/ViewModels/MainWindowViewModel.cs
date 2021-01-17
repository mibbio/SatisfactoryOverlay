namespace SatisfactoryOverlay.ViewModels
{
    using mvvmlib;

    using SatisfactoryOverlay.Models;
    using SatisfactoryOverlay.Properties;
    using SatisfactoryOverlay.Savegame;
    using SatisfactoryOverlay.Updater;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Forms;
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
            set
            {
                monitor.SessionNameVisible = (bool)value;
                OnPropertyChanged();
            }
        }

        public bool? PlaytimeVisible
        {
            get => monitor?.PlaytimeVisible;
            set
            {
                monitor.PlaytimeVisible = (bool)value;
                OnPropertyChanged();
            }
        }

        public bool? TotalPlaytimeVisible
        {
            get => monitor?.TotalPlaytimeVisible;
            set
            {
                monitor.TotalPlaytimeVisible = (bool)value;
                OnPropertyChanged();
            }
        }

        public bool? StartingZoneVisible
        {
            get => monitor?.StartingZoneVisible;
            set
            {
                monitor.StartingZoneVisible = (bool)value;
                OnPropertyChanged();
            }
        }

        public bool? ModsVisible
        {
            get => monitor?.ModsVisible;
            set
            {
                monitor.ModsVisible = (bool)value;
                OnPropertyChanged();
            }
        }

        public ObsVariant StreamingTool
        {
            get => (monitor != null) ? monitor.StreamingTool : ObsVariant.Studio;
            set
            {
                if (monitor != null)
                {
                    monitor.StreamingTool = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowFileSettings));
                    OnPropertyChanged(nameof(ShowObsSettings));
                }
            }
        }

        public string ObsElementName
        {
            get => monitor?.ObsElementName;
            set
            {
                monitor.ObsElementName = value;
                OnPropertyChanged();
            }
        }

        public string OutputFilepath
        {
            get => monitor?.OutputFilepath;
            set
            {
                monitor.OutputFilepath = value;
                OnPropertyChanged();
            }
        }

        public string ObsIpAddress
        {
            get => monitor?.ObsIpAddress;
            set
            {
                monitor.ObsIpAddress = value;
                OnPropertyChanged();
            }
        }

        public int ObsPort
        {
            get => (monitor != null) ? monitor.ObsPort : -1;
            set
            {
                monitor.ObsPort = value;
                OnPropertyChanged();
            }
        }

        public string WebsocketPassword
        {
            get => monitor?.WebsocketPassword;
            set
            {
                monitor.WebsocketPassword = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected => monitor.IsConnected;

        private bool _canConnect;

        public bool CanConnect
        {
            get => _canConnect;
            private set
            {
                if (_canConnect == value) return;
                _canConnect = value;
                OnPropertyChanged();
            }
        }

        private string _statusText = string.Empty;

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFileSettings => monitor?.StreamingTool == ObsVariant.Textfile;

        public bool ShowObsSettings => monitor?.StreamingTool != ObsVariant.Textfile;

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

        public RotatingDisplayLog EventLog { get; } = new RotatingDisplayLog(5, Path.Combine(Directory.GetCurrentDirectory(), "log.txt"));

        public MainWindowViewModel()
        {
            if (System.Windows.Application.Current is IUpdateNotifier notifier)
            {
                notifier.OnUpdateAvailable += HandleUpdateAvailable;
            }

            try
            {
                monitor = new MonitoringModel();
                monitor.OnConnected += (s, e) =>
                {
                    CanConnect = false;
                    SetInfo(Resources.Message_Connected);
                    OnPropertyChanged(nameof(IsConnected));
                };
                monitor.OnDisconnected += (s, e) =>
                {
                    CanConnect = true;
                    if (string.IsNullOrWhiteSpace(e))
                    {
                        SetInfo($"{Resources.Message_Disconnected}");
                    }
                    else
                    {
                        SetInfo($"{Resources.Message_Disconnected} ({e})");
                    }
                    OnPropertyChanged(nameof(IsConnected));
                };
                monitor.OnObsError += (s, e) =>
                {
                    SetInfo(e.Message);
                    CanConnect = !monitor.IsConnected;
                    OnPropertyChanged(nameof(IsConnected));
                };
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

            LoadSessions();

            PropertyChanged += HandlePropertyChanged;

            CanConnect = true;
        }

        private void LoadSessions()
        {
            Sessions.Clear();

            List<SavegameHeader> savegames = new List<SavegameHeader>();
            var folder = ServiceLocator.Default.GetService<SettingsModel>().SavegameFolder;
            foreach (var file in Directory.EnumerateFiles(ServiceLocator.Default.GetService<SettingsModel>().SavegameFolder, "*.sav"))
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

        private async void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MonitoredSession):
                case nameof(SessionNameVisible):
                case nameof(PlaytimeVisible):
                case nameof(TotalPlaytimeVisible):
                case nameof(StartingZoneVisible):
                case nameof(ModsVisible):
                    await monitor.UpdateDisplayOptionsAsync();
                    break;
                default:
                    break;
            }
        }

        private bool IsValidIP(string ip) => IPAddress.TryParse(ip, out var _);

        private void SetInfo(string info)
        {
            StatusText = info;
            EventLog.AddLine(info);
        }

        private ICommand _cmdStartFileOutput;
        public ICommand CmdStartFileOutput => _cmdStartFileOutput ??= new RelayCommand(async () =>
        {
            try
            {
                await monitor.StartAsync();
            }
            catch (Exception) { }
        });

        private ICommand _cmdConnectOBS;
        public ICommand CmdConnectOBS => _cmdConnectOBS ??= new RelayCommand(
            async () =>
            {
                try
                {
                    CanConnect = false;
                    await monitor.StartAsync();
                }
                catch (Exception) { }
            }, () => CanConnect && !IsConnected && IsValidIP(ObsIpAddress));

        private ICommand _cmdRefreshSessions;
        public ICommand CmdRefreshSessions => _cmdRefreshSessions ??= new RelayCommand(LoadSessions);

        private ICommand _cmdOpenUpdate;
        public ICommand CmdOpenUpdate => _cmdOpenUpdate ??= new RelayCommand(() =>
        {
            Helper.OpenUrlInBrowser(Release.Link);
            Release = null;
        });

        private ICommand _cmdSelectFilepath;
        public ICommand CmdSelectFilepath => _cmdSelectFilepath ??= new RelayCommand(() =>
        {
            var dialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                AutoUpgradeEnabled = true,
                Multiselect = false,
                ValidateNames = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                OutputFilepath = dialog.FileName;
            }
        });
    }
}
