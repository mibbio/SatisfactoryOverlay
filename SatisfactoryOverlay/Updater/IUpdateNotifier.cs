namespace SatisfactoryOverlay.Updater
{
    using System;

    interface IUpdateNotifier
    {
        event EventHandler<ReleaseData> OnUpdateAvailable;
    }
}
