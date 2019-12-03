using Plato.UWP.Services;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Plato.UWP.ViewModels
{
    public class MainViewModel
    {

        public string Url { get; set; } = "https://plato.instantasp.co.uk/support";

        ElementTheme _theme = ElementTheme.Default;

        public ElementTheme Theme => _theme;

        public string BackgroundImage { get; set; }

        private readonly IAppSettingsManager _settingsManager;

        public MainViewModel(IAppSettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public async Task LoadAsync()
        {
            await LoadSettingsAsync();
        }

        public void Unload()
        {
            Url = null;
        }


        private async Task LoadSettingsAsync()
        {
            try
            {

                var settings = await _settingsManager.GetSettings();

                if (!string.IsNullOrEmpty(settings.Url))
                {
                    Url = settings.Url;
                }

                if (!string.IsNullOrEmpty(settings.BackgroundImage))
                {
                    BackgroundImage = settings.BackgroundImage;
                }

                var theme = settings.Theme.ToLower();
                if (!string.IsNullOrEmpty(settings.Theme))
                {
                    if (theme.IndexOf("dark") >= 0)
                    {
                        _theme = ElementTheme.Dark;
                    }
                    if (theme.IndexOf("light") >= 0)
                    {
                        _theme = ElementTheme.Light;
                    }
                    if (theme.IndexOf("default") >= 0)
                    {
                        _theme = ElementTheme.Default;
                    }
                }

            }
            catch
            {
                
            }
        }

    }

}
