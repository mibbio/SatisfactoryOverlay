namespace mvvmlib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    public static class PasswordBoxAssistant
    {
        public static readonly DependencyProperty BoundPassword = DependencyProperty.RegisterAttached(
            nameof(BoundPassword),
            typeof(string),
            typeof(PasswordBoxAssistant),
            new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPassword = DependencyProperty.RegisterAttached(
            nameof(BindPassword),
            typeof(bool),
            typeof(PasswordBoxAssistant),
            new PropertyMetadata(false, OnBindPasswordChanged));

        public static readonly DependencyProperty UpdatingPassword = DependencyProperty.RegisterAttached(
            nameof(UpdatingPassword),
            typeof(bool),
            typeof(PasswordBoxAssistant),
            new PropertyMetadata(false));

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox box = d as PasswordBox;

            if (box == null || !GetBindPassword(d)) { return; }

            box.PasswordChanged -= HandlePasswordChanged;

            string newPassword = (string)e.NewValue;

            if (!GetUpdatingPassword(box))
            {
                box.Password = newPassword;
            }

            box.PasswordChanged += HandlePasswordChanged;
        }

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox box = d as PasswordBox;

            if (box == null) { return; }

            bool wasBound = (bool)e.OldValue;
            bool needToBind = (bool)e.NewValue;

            if (wasBound)
            {
                box.PasswordChanged -= HandlePasswordChanged;
            }

            if (needToBind)
            {
                box.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox box = sender as PasswordBox;

            SetUpdatingPassword(box, true);
            SetBoundPassword(box, box.Password);
            SetUpdatingPassword(box, false);
        }

        public static bool GetBindPassword(DependencyObject d)
        {
            return (bool)d.GetValue(BindPassword);
        }

        public static void SetBindPassword(DependencyObject d, bool value)
        {
            d.SetValue(BindPassword, value);
        }

        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPassword);
        }

        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPassword, value);
        }

        public static bool GetUpdatingPassword(DependencyObject d)
        {
            return (bool)d.GetValue(UpdatingPassword);
        }

        public static void SetUpdatingPassword(DependencyObject d, bool value)
        {
            d.SetValue(UpdatingPassword, value);
        }
    }
}
