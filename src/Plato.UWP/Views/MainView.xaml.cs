using Plato.UWP.Configuration;
using Plato.UWP.DependencyInjection;
using Plato.UWP.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace Plato.UWP.Views
{

    public sealed partial class MainView : Page
    {

        bool _navigateWithHeader = true;

        public MainViewModel ViewModel { get; }

        public MainView()
        {
            ViewModel = ServiceLocator.Current.GetService<MainViewModel>();
            InitializeComponent();
        }

        // Navigation events
        // -------------------        

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.LoadAsync();
            this.RequestedTheme = ViewModel.Theme;

            webView1.NavigationCompleted += webView1_NavigationCompleted;
            NavigateWithHeader(new Uri(ViewModel.Url));

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.Unload();
        }

        // Web view helpers
        // -------------------

        private void NavigateWithHeader(Uri uri)
        {
            _navigateWithHeader = true;
            var requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMsg.Headers.Add("X-Plato-Theme", this.RequestedTheme == ElementTheme.Dark ? "dark" : "light");
            webView1.NavigateWithHttpRequestMessage(requestMsg);            
        }

        private async void webView1_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (_navigateWithHeader)
            {
                // Upon the first request persist the theme selection within a client cookie                
                var theme = this.RequestedTheme == ElementTheme.Dark ? "dark" : "light";
                var script =$"if (window.$.Plato) {{ window.$.Plato.storage.setCookie('plato-theme', '{theme}'); }}";
                await sender.InvokeScriptAsync("eval", new string[] { script });
                _navigateWithHeader = false;
            }         
        }

        // Button events
        // -------------------

        void btnHome_Click(object sender, RoutedEventArgs e)
        {
            webView1.Navigate(new Uri(ViewModel.Url));
        }

        void btnBack_Click(object sender, RoutedEventArgs e)
        {
         
            if (webView1.CanGoBack)
            {
                webView1.GoBack();
            }            
        }

        void btnForward_Click(object sender, RoutedEventArgs e)
        {
            if (webView1.CanGoForward)
            {
                webView1.GoForward();
            }
        }

        void btnRefresh_Click(object sender, RoutedEventArgs e)
        {        
            webView1.Refresh();
        }

        void Settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsView), e);
        }

        void btnAbout_Click(object sender, RoutedEventArgs e)
        {

            FlyoutBase.ShowAttachedFlyout(this);

            //return null;
        }

        

    }

}
