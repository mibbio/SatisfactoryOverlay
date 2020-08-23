namespace SatisfactoryOverlay.EventArgs
{
    using System;

    public sealed class ConnectionEventArg
    {
        public enum ConnectionStatus
        {
            Connected,
            Disconnected
        }

        public ConnectionStatus Status { get; private set; }

        public Version ObsVersion { get; private set; }

        public Version PluginVersion { get; private set; }

        public ConnectionEventArg(ConnectionStatus status, Version obsVersion, Version pluginVersion)
        {
            Status = status;
            ObsVersion = obsVersion ?? throw new ArgumentNullException(nameof(obsVersion));
            PluginVersion = pluginVersion ?? throw new ArgumentNullException(nameof(pluginVersion));
        }

        public ConnectionEventArg(ConnectionStatus status, string obsVersion, string pluginVersion)
        {
            Status = status;

            Version.TryParse(obsVersion, out var parsedObsVersion);
            Version.TryParse(pluginVersion, out var parsedPluginVersion);

            ObsVersion = parsedObsVersion;
            PluginVersion = parsedPluginVersion;
        }
    }
}
