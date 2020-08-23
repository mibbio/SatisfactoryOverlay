namespace SatisfactoryOverlay.Updater
{
    using Newtonsoft.Json;

    using System;
    using System.Globalization;

    internal class TagVersionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Version);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize<string>(reader);
            if (value.StartsWith("v", true, CultureInfo.InvariantCulture))
            {
                value = value.Substring(1);
            }

            if (Version.TryParse(value, out var result))
            {
                return result;
            }
            else
            {
                return new Version();
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
