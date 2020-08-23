namespace SatisfactoryOverlay.Updater
{
    using Newtonsoft.Json;

    using System;

    [JsonObject()]
    public class ReleaseData
    {
        [JsonProperty("prerelease")]
        public bool IsPrerelease { get; set; }

        [JsonProperty("html_url")]
        public Uri Link { get; set; }

        [JsonProperty("tag_name")]
        [JsonConverter(typeof(TagVersionConverter))]
        public Version Version { get; set; }

        [JsonProperty("published_at")]
        public DateTime Date { get; set; }
    }
}
