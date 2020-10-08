namespace SatisfactoryOverlay.Obs
{
    using System;
    using System.Threading.Tasks;

    public interface IObsClient
    {
        bool IsConnected { get; }

        bool NeedsAuthentication { get; }

        Task ConnectAsync();

        Task DisconnectAsync();

        Task<Version> GetVersionAsync();

        Task UpdateDisplayAsync(string elementName, string text);

        event EventHandler OnConnected;

        event EventHandler OnAuthenticated;

        event EventHandler OnDisconnected;

        event EventHandler<ObsClientError> OnClientError;
    }
}
