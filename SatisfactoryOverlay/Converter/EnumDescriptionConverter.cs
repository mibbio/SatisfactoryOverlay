﻿namespace SatisfactoryOverlay.Converter
{
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
                        return ((attributes.Length > 0) && (!string.IsNullOrWhiteSpace(attributes[0].Description))) ? attributes[0].Description : value.ToString();
                    }
                    return string.Empty;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
