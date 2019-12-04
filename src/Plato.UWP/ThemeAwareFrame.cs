using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Plato.UWP
{

    public class ThemeAwareFrame : Frame
    {

        private static readonly ThemeProxyClass _themeProxyClass = new ThemeProxyClass();

        public static readonly DependencyProperty AppThemeProperty = DependencyProperty.Register(
            "AppTheme", typeof(ElementTheme), typeof(ThemeAwareFrame), new PropertyMetadata(default(ElementTheme), (d, e) => _themeProxyClass.Theme = (ElementTheme)e.NewValue));
        
        public ElementTheme AppTheme
        {
            get
            {
                var theme = (ElementTheme)GetValue(AppThemeProperty);
                if (theme == ElementTheme.Default)
                {
                    // If "Default" detect underlying OS theme
                    var uiSettings = new Windows.UI.ViewManagement.UISettings();
                    Windows.UI.Color color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                    return color.R == 0 && color.G == 0 && color.B == 0
                        ? ElementTheme.Dark
                        : ElementTheme.Light;                   
                }
                return theme;
            }
            set => SetValue(AppThemeProperty, value);
        }

        public ThemeAwareFrame(ElementTheme appTheme)
        {
            var themeBinding = new Binding { Source = _themeProxyClass, Path = new PropertyPath("Theme"), Mode = BindingMode.OneWay };
            SetBinding(RequestedThemeProperty, themeBinding);
            AppTheme = appTheme;

        }

        sealed class ThemeProxyClass : INotifyPropertyChanged
        {
            private ElementTheme _theme;

            public ElementTheme Theme
            {
                get { return _theme; }
                set
                {
                    _theme = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

}
