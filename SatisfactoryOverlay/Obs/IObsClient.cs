namespace SatisfactoryOverlay.Obs
{
    using System;
    using System.Threading.Tasks;

    public interface IObsClient
    {
        bool IsConnected { get; }

        Task ConnectAsync();

        Task DisconnectAsync();

        Task<Version> GetVersionAsync();

        Task UpdateDisplayAsync(string elementName, string text);

        event EventHandler OnConnected;

        event EventHandler OnDisconnected;

        event EventHandler<Exception> OnClientError;
    }
}
