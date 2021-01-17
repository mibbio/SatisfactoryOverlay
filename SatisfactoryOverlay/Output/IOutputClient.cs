namespace SatisfactoryOverlay.Output
{
    using System;
    using System.Threading.Tasks;

    public interface IOutputClient
    {
        bool IsConnected { get; }

        bool NeedsAuthentication { get; }

        Task ConnectAsync();

        Task DisconnectAsync();

        Task<Version> GetVersionAsync();

        Task UpdateDisplayAsync(string elementName, string text);

        event EventHandler OnConnected;

        event EventHandler OnAuthenticated;

        event EventHandler<string> OnDisconnected;

        event EventHandler<ObsClientError> OnClientError;
    }
}
