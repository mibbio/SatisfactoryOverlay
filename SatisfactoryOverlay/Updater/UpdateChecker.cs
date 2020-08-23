
namespace SatisfactoryOverlay.Updater
{
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    internal class UpdateChecker : IUpdateNotifier
    {
        private static readonly HttpClient client = new HttpClient();

        private DispatcherTimer _timer;

        private readonly string _username;

        private readonly string _repository;

        static UpdateChecker()
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(assemblyName.Name, assemblyName.Version.ToString()));
        }

        public UpdateChecker(string username, string repository)
        {
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ReleaseData> CheckForUpdateAsync()
        {
            try
            {
                var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
                var body = await client.GetStringAsync(GetRequestUrl(_username, _repository));
                var releases = JsonConvert.DeserializeObject<List<ReleaseData>>(body);

                return releases
                    ?.Where(rd => rd.Version > currentVersion)
                    ?.OrderByDescending(rd => rd.Version)
                    ?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void CheckWithInterval(TimeSpan interval)
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Background);
                _timer.Tick += new EventHandler(async (s, e) =>
                {
                    var update = await CheckForUpdateAsync();
                    if (update != null)
                    {
                        OnUpdateAvailable?.Invoke(this, update);
                    }
                });
            }

            _timer.Stop();
            _timer.Interval = interval;
            _timer.Start();
        }

        private static string GetRequestUrl(string username, string repository)
        {
            return $"https://api.github.com/repos/{username}/{repository}/releases";
        }

        public event EventHandler<ReleaseData> OnUpdateAvailable;
    }
}
