namespace SatisfactoryOverlay.Converter
{
    using SatisfactoryOverlay.Properties;

    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;

    internal class EnumDescriptionConverter : EnumConverter
    {
        public EnumDescriptionConverter(Type type) : base(type) { }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    FieldInfo fi = value.GetType().GetField(value.ToString());
                    if (fi != null)
                    {
                        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if ((attributes.Length > 0) && (!string.IsNullOrWhiteSpace(attributes[0].Description)))
                        {
                            var result = Resources.ResourceManager.GetString(attributes[0].Description);
                            if (string.IsNullOrWhiteSpace(result))
                            {
                                return attributes[0].Description;
                            }
                            return result;
                        }
                        else
                        {
                            return value.ToString();
                        }
                    }
                    return string.Empty;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
