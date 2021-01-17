

namespace SatisfactoryOverlay.Output
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    public class FileClient : IOutputClient
    {
        public bool IsConnected { get; } = true;

        public bool NeedsAuthentication { get; } = false;

        public event EventHandler OnConnected;

        public event EventHandler OnAuthenticated;

        public event EventHandler<string> OnDisconnected;

        public event EventHandler<ObsClientError> OnClientError;

        public async Task ConnectAsync()
        {
            OnConnected?.Invoke(this, EventArgs.Empty);
            OnAuthenticated?.Invoke(this, EventArgs.Empty);
            await Task.CompletedTask;
        }

        public async Task DisconnectAsync() => await Task.CompletedTask;

        public async Task<Version> GetVersionAsync()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return await Task.FromResult(version);
        }

        public async Task UpdateDisplayAsync(string filepath, string text)
        {
            if (!Path.IsPathRooted(filepath))
            {
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    var directory = Path.GetDirectoryName(filepath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(filepath, text);
                }
                catch (Exception ex)
                {
                    OnClientError?.Invoke(this, new ObsClientError(ObsClientErrorType.Filesystem, ex.Message));
                    OnDisconnected?.Invoke(this, ex.Message);
                }
            });
        }
    }
}
