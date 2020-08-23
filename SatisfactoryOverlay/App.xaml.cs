﻿namespace SatisfactoryOverlay
{
    using SatisfactoryOverlay.Updater;
    using SatisfactoryOverlay.Views;

    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    public partial class App : Application, IUpdateNotifier
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;

        private UpdateChecker updateChecker;

        private bool isExit;

        public static string SavegameFolder
        {
            get
            {
                string savegameFolder = string.Empty;
                string savegameRootFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FactoryGame", "Saved", "SaveGames");
                try
                {
                    savegameFolder = Directory.EnumerateDirectories(savegameRootFolder)
                    .Where(d => !d.Equals("common")).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(savegameFolder))
                    {
                        return string.Empty;
                    }
                }
                catch (IOException)
                {
                    return string.Empty;
                }

                return savegameFolder;
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindowView();
            MainWindow.Closing += MainWindow_Closing;

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            notifyIcon.Icon = SatisfactoryOverlay.Properties.Resources.app;
            notifyIcon.Text = "Satisfactory Overlay Manager";
            notifyIcon.Visible = true;

            await SetupUpdateCheckAsync();

            CreateContextMenu();
            ShowMainWindow();
        }

        private async Task SetupUpdateCheckAsync()
        {
            updateChecker = new UpdateChecker("mibbio", "satisfactoryoverlay");
            updateChecker.OnUpdateAvailable += HandleUpdateAvailable;

            var latestUpdate = await updateChecker.CheckForUpdateAsync();
            if (latestUpdate != null)
            {
                OnUpdateAvailable?.Invoke(this, latestUpdate);
            }

            updateChecker.CheckWithInterval(TimeSpan.FromMinutes(15));
        }

        private void HandleUpdateAvailable(object sender, ReleaseData update)
        {
            OnUpdateAvailable?.Invoke(sender, update);
        }

        private void CreateContextMenu()
        {
            var langMenu = new System.Windows.Forms.ToolStripMenuItem(SatisfactoryOverlay.Properties.Resources.Label_Locale, SatisfactoryOverlay.Properties.Resources.Localize_16x);
            var langEntryDe = new System.Windows.Forms.ToolStripMenuItem(SatisfactoryOverlay.Properties.Resources.Label_LocaleDE, SatisfactoryOverlay.Properties.Resources.flag_de)
            {
                Tag = CultureInfo.GetCultureInfo("de")
            };
            langEntryDe.Click += ChangeLocale;

            var langEntryEn = new System.Windows.Forms.ToolStripMenuItem(SatisfactoryOverlay.Properties.Resources.Label_LocaleEN, SatisfactoryOverlay.Properties.Resources.flag_en)
            {
                Tag = CultureInfo.GetCultureInfo("en")
            };
            langEntryEn.Click += ChangeLocale;

            langMenu.DropDownItems.Add(langEntryDe);
            langMenu.DropDownItems.Add(langEntryEn);

            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add(SatisfactoryOverlay.Properties.Resources.Label_ShowSettings, SatisfactoryOverlay.Properties.Resources.ShowDetailsPane_16x)
                .Click += (s, e) => ShowMainWindow();
            notifyIcon.ContextMenuStrip.Items.Add(langMenu);
            notifyIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            notifyIcon.ContextMenuStrip.Items.Add(SatisfactoryOverlay.Properties.Resources.Label_CloseApplication, SatisfactoryOverlay.Properties.Resources.CloseSolution_16x)
                .Click += (s, e) => ExitApplication();
        }

        private void ChangeLocale(object sender, System.EventArgs e)
        {
            if (sender is System.Windows.Forms.ToolStripMenuItem mi && mi.Tag is CultureInfo ci)
            {
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;

                var oldWindow = MainWindow;
                oldWindow.Closing -= MainWindow_Closing;

                MainWindow = new MainWindowView();
                MainWindow.Closing += MainWindow_Closing;

                if(oldWindow.IsVisible)
                {
                    MainWindow.Show();
                }
                oldWindow.Close();

                CreateContextMenu();
            }
        }

        public void ExitApplication()
        {
            isExit = true;
            MainWindow.Close();
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        private void ShowMainWindow()
        {
            if (MainWindow.IsVisible)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
            else
            {
                MainWindow.Show();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isExit)
            {
                e.Cancel = true;
                MainWindow.Hide();
            }
        }

        public event EventHandler<ReleaseData> OnUpdateAvailable;
    }
}
