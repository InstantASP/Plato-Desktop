﻿using Plato.UWP.DependencyInjection;
using Plato.UWP.ViewModels;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace Plato.UWP.Views
{

    public sealed partial class MainView : Page
    {

        bool _firstRequest = true;     

        public MainViewModel ViewModel { get; }

        public MainView()
        {      
            ViewModel = ServiceLocator.Current.GetService<MainViewModel>();
            InitializeComponent();
        }
       
        #region "Page Events"

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            await ViewModel.LoadAsync();
            (Window.Current.Content as ThemeAwareFrame).AppTheme = ViewModel.Theme;

            if (!string.IsNullOrEmpty(ViewModel.BackgroundImage))
            {
                var bitmap = new BitmapImage(new Uri($"ms-appx-web:///{ViewModel.BackgroundImage}"));
                main.Background = new ImageBrush
                {
                    ImageSource = bitmap,
                    Stretch = Stretch.Fill
                };
            }

            webView1.NavigationStarting += WebView1_NavigationStarting;
            webView1.NavigationCompleted += webView1_NavigationCompleted;
            webView1.NavigationFailed += WebView1_NavigationFailed;
            
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

            if (IsLocalUrl(args.Uri.ToString()))
            {                
                txtTidyAddress.Visibility = Visibility.Collapsed;
            } 
            else
            {                
                txtTidyAddress.Visibility = Visibility.Visible;
            }
         

            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 0);
            myProgressRing.IsActive = true;

        }

        private void webView1_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {

            myProgressRing.IsActive = false;
            initialLoader.Visibility = Visibility.Collapsed;

            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);

            switch (args.WebErrorStatus)
            {
                case Windows.Web.WebErrorStatus.NotFound:
                    webView1.Navigate(new Uri("ms-appx-web:///assets/web/NotFound.html"));
                    break;
                default:

                    break;
            }

            var theme = GetCurrentTheme();
            // Upon the first navigation set a client cookie to indicate the theme
                if (_firstRequest)
            {
                InsokeScript($"if (window.$.Plato) {{ window.$.Plato.storage.setCookie('plato-theme', '{theme}'); }}");
            }
              
            var script = @"
                // Wrapped fixed content within a dummy div to prevent stickyness
                if (window.$.Plato) {     
                    var dummy = $('<div>', {
                        'class': 'd-block'
                    });                    

                    $('.layout-header-content').wrap(dummy);
                    $('.layout-sidebar-content').wrap(dummy);
                    $('.layout-asides-content').wrap(dummy);

                    // Add custom CSS
                    window.$.Plato.utils.addCss('{css}'); 
                }
            ";
            script = script.Replace("{css}", new Uri($"ms-appx-web:///assets/web/css/plato/{theme}.css").ToString());            
            InsokeScript(script);

            // Add theme css into local pages
            InsokeScript("addCss", new string[] { $"css/app/themes/{theme}.css" });


            InsokeScript("addUrl", new string[] { ViewModel.Url });

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

            myProgressRing.Visibility = Visibility.Visible;
            _firstRequest = false;

        }
        
        private void WebView1_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {            
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

        #region "Address Bar Events"

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

        #endregion

        #region "Private Methods"               

        void InsokeScript(string  script)
        {
            InsokeScript("eval", new string[] { script } );
        }

        async void InsokeScript(string functionName, string[] args)
        {
            try
            {
                await webView1.InvokeScriptAsync(functionName, args);       
            }
            catch
            {
            }
        }

        ElementTheme GetCurrentTheme()
        {

            var currentTheme = (Window.Current.Content as ThemeAwareFrame).AppTheme;
            ElementTheme theme = ElementTheme.Default;

            if (currentTheme == ElementTheme.Dark)
            {
                theme = ElementTheme.Dark;
            }
            else if (currentTheme == ElementTheme.Light)
            {
                theme = ElementTheme.Light;
            }
            else
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                Windows.UI.Color color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                if (color.R == 0 && color.G == 0 && color.B == 0)
                {
                    theme = ElementTheme.Dark;
                }
                else
                {
                    theme = ElementTheme.Light;
                }
            }
            return theme;
        }

        void NavigateWithHeader(Uri uri)
        {
            _firstRequest = true;
            var theme = GetCurrentTheme();
            var requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMsg.Headers.Add("X-Plato-Theme", theme == ElementTheme.Dark ? "dark" : "light");
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

        bool IsLocalUrl(string url)
        {
            return url.ToLower().IndexOf("ms-appx-web") >= 0 ? true : false;
        }

        #endregion

    }

}
