using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace QRTool.ViewModel.Converters
{
    class BoolToVisibleConverter : IValueConverter
    {
        private bool isInverted = false;

        public bool IsInverted
        {
            get { return isInverted; }
            set { isInverted = value; }
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inputBool = (IsInverted) ? !System.Convert.ToBoolean(value) : System.Convert.ToBoolean(value);

            return inputBool ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((Visibility)value == Visibility.Visible)
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
