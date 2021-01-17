namespace SatisfactoryOverlay.Models
{
    using Newtonsoft.Json;

    using SatisfactoryOverlay.Converter;

    using System.ComponentModel;
    using System.IO;

    [TypeConverter(typeof(EnumDescriptionConverter))]
    public enum ObsVariant
    {
        [Description("Enum_Output_Studio")]
        Studio,
        [Description("Enum_Output_Streamlabs")]
        Streamelements,
        [Description("Enum_Output_File")]
        Textfile
    }

    [JsonObject(MissingMemberHandling = MissingMemberHandling.Ignore, ItemNullValueHandling = NullValueHandling.Ignore, MemberSerialization = MemberSerialization.OptOut)]
    public class SettingsModel
    {
        private static readonly string filePath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string SavegameFolder { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string LastSessionName { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool SessionNameVisible { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool PlaytimeVisivble { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool StartingZoneVisible { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ModsVisible { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool TotalPlaytimeVisible { get; set; }

        [DefaultValue("satisfactoryInfo")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string ObsElementName { get; set; }

        [DefaultValue("127.0.0.1")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string ObsIpAddress { get; set; }

        [DefaultValue(4444)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ObsPort { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string WebsocketPassword { get; set; }

        [DefaultValue(@"d:\Debug\out.txt")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string OutputFilepath { get; set; }

        [DefaultValue(ObsVariant.Studio)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public ObsVariant StreamingTool { get; set; }

        private SettingsModel() { }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static SettingsModel PopulateSettings()
        {
            var result = new SettingsModel();

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                JsonConvert.PopulateObject(json, result);
            }
            else
            {
                JsonConvert.PopulateObject("{}", result);
                result.Save();
            }

            return result;
        }
    }
}
