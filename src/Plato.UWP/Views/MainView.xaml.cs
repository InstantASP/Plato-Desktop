using Plato.UWP.Configuration;
using Plato.UWP.DependencyInjection;
using Plato.UWP.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace Plato.UWP.Views
{

    public sealed partial class MainView : Page
    {

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
            var theme = this.RequestedTheme;
            var requestMsg = new Windows.Web.Http.HttpRequestMessage(HttpMethod.Get, uri);
            requestMsg.Headers.Add("X-Plato-Theme", this.RequestedTheme == ElementTheme.Dark ? "dark" : "light");
            webView1.NavigateWithHttpRequestMessage(requestMsg);
            webView1.NavigationStarting += webView1_NavigationStarting;
        }

        private void webView1_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            webView1.NavigationStarting -= webView1_NavigationStarting;
            args.Cancel = true;//cancel navigation in a handler for this event by setting the WebViewNavigationStartingEventArgs.Cancel property to true
            NavigateWithHeader(args.Uri);
        }

        // Button events
        // -------------------

        void btnHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateWithHeader(new Uri(ViewModel.Url));
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

        void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle button click.
        }

        void Settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsView), e);
        }


    }


}
