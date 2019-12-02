using Plato.UWP.DependencyInjection;
using Plato.UWP.ViewModels;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
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

            //then add this line
            webView1.AddHandler(PointerPressedEvent,
              new PointerEventHandler((s, e) => {
                  //txtTidyAddress.IsTextSelectionEnabled = false;
                  //this.Focus(FocusState.Programmatic);
              }), true);

        }
       
        #region "Page Events"

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.LoadAsync();
            this.RequestedTheme = ViewModel.Theme;
                        
            webView1.NavigationStarting += WebView1_NavigationStarting;
            webView1.NavigationCompleted += webView1_NavigationCompleted;
            NavigateWithHeader(new Uri(ViewModel.Url));

        }
               
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.Unload();
        }

        #endregion

        #region "webView Events"

        private void WebView1_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 0);
            txtFullAddress.Visibility = Visibility.Collapsed;
            txtTidyAddress.Visibility = Visibility.Visible;
        }
        
        private async void webView1_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {

            if (_navigateWithHeader)
            {

                // Upon the first request persist the theme selection within a client cookie                
                var theme = this.RequestedTheme == ElementTheme.Dark ? "dark" : "light";
                var script =$"if (window.$.Plato) {{ window.$.Plato.storage.setCookie('plato-theme', '{theme}'); }}";

                try
                {
                    await sender.InvokeScriptAsync("eval", new string[] { script });
                }
                catch
                {                    
                }

                _navigateWithHeader = false;

            }            

            var url = string.Empty;
            var tidyUrl = string.Empty;
            try
            {
                url = args.Uri.ToString();
                tidyUrl = TidyUrl(url);                
            }
            finally
            {
                txtFullAddress.Text = url;
                txtTidyAddress.Text = tidyUrl;           
            }

            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);    

        }

        #endregion

        #region "Button events"

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
        }

        #endregion

        #region "Private Methods"               

        void NavigateWithHeader(Uri uri)
        {
            _navigateWithHeader = true;
            var requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMsg.Headers.Add("X-Plato-Theme", this.RequestedTheme == ElementTheme.Dark ? "dark" : "light");
            webView1.NavigateWithHttpRequestMessage(requestMsg);

        }

        string TidyUrl(string url)
        {

            if (url == null)
            {
                return string.Empty;
            }

            url = url
                .ToLower()
                .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                .Replace("https://", "", StringComparison.OrdinalIgnoreCase);

            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return url;

        }

        #endregion

        private void txtTidyAddress_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            txtFullAddress.Visibility = Visibility.Visible;
            txtFullAddress.Select(txtFullAddress.ContentStart, txtFullAddress.ContentEnd);
            txtFullAddress.Focus(FocusState.Programmatic);            
        }

        private void txtFullAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            txtFullAddress.Visibility = Visibility.Collapsed;
            txtTidyAddress.Visibility = Visibility.Visible;
        }

    }

}
