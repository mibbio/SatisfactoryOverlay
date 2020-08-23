namespace mvvmlib
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null, object context = null)
        {
            VerifyPropertyName(propertyName, context);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [Conditional("DEBUG")]
        [ExcludeFromCodeCoverage]
        private void VerifyPropertyName(string propertyName, object context = null)
        {
            var checkedObj = context ?? this;

            if (TypeDescriptor.GetProperties(checkedObj)[propertyName] == null)
            {
                throw new ArgumentNullException($"{GetType().Name} does not contain property: {propertyName}");
            }
        }
    }
}
