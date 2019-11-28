using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Plato.UWP.DependencyInjection;
using Plato.UWP.Services;
using Plato.UWP.ViewModels;
using Plato.UWP.Models;

namespace Plato.UWP.Views
{

    public sealed partial class SettingsView : Page
    {
        
        public SettingsViewModel ViewModel { get; }

        public SettingsView()
        {
            ViewModel = ServiceLocator.Current.GetService<SettingsViewModel>();            
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.LoadAsync();
            this.RequestedTheme = ViewModel.Theme;

            if (!string.IsNullOrEmpty(ViewModel.Url))
            {
                txtSettingsUrl.Text = ViewModel.Url;
            }

            if (!string.IsNullOrEmpty(ViewModel.Theme.ToString()))
            {
                ddlTheme.SelectedItem = ViewModel.Theme.ToString();
            }

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.Unload();
        }

        void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainView), e);
        }

        async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            
            // Get settings sergice
            var settingsManager = ServiceLocator.Current.GetService<IAppSettingsManager>();

            // Get current settings
            var settings = await settingsManager.GetSettings() ?? new AppSettings();

            // Validate url
            var ok = Uri.TryCreate(txtSettingsUrl.Text, UriKind.Absolute, out Uri uriResult);
            if (ok)
            {
                settings.Url = uriResult.ToString();
            }

            // Validate theme
            if (!string.IsNullOrEmpty(ddlTheme.SelectedValue.ToString()))
            {
                settings.Theme = ddlTheme.SelectedValue.ToString();
            }            

            // Save settings
            await settingsManager.SaveSettings(settings);

            // Reload
            Frame.Navigate(typeof(MainView), e);

        }

    }

}
