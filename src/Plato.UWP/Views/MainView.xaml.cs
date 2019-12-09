using System;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Plato.UWP.DependencyInjection;
using Plato.UWP.Models;
using Plato.UWP.Services;
using Plato.UWP.ViewModels;

namespace Plato.UWP.Views
{

    public sealed partial class MainView : Page
    {

        private bool _firstRequest = true;

        public MainViewModel ViewModel { get; }

        public MainView()
        {
            ViewModel = ServiceLocator.Current.GetService<MainViewModel>();
            InitializeComponent();
        }

        #region "Page Events"

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            // Load view model
            await ViewModel.LoadAsync();

            // Make frame aware of configured theme
            var frame = GetFrame();
            frame.AppTheme = ViewModel.Theme;

            // Restore the background image
            if (!string.IsNullOrEmpty(ViewModel.BackgroundImage))
            {
                var bitmap = new BitmapImage(new Uri($"ms-appx-web:///{ViewModel.BackgroundImage}"));
                Main.Background = new ImageBrush
                {
                    ImageSource = bitmap,
                    Stretch = Stretch.Fill
                };
            }

            // Wire up web view events
            WebView.NavigationStarting += WebView_NavigationStarting;
            WebView.NavigationCompleted += WebView_NavigationCompleted;
            WebView.NavigationFailed += WebView_NavigationFailed;
            WebView.ScriptNotify += WebView_ScriptNotify;

            // Perform the first request navigating with headers to indicate the theme
            NavigateWithHeader(new Uri(ViewModel.Url));

        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.Unload();
        }

        #endregion

        #region "webView Events"

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {

            TidyAddress.Visibility = IsLocalUrl(args.Uri.ToString()) 
                ? Visibility.Collapsed 
                : Visibility.Visible;

            // Update cursor
            SetCursor(Windows.UI.Core.CoreCursorType.Wait);

            // Activate loader
            Loader.IsActive = true;

        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {

            // Hide the initial loader
            InitialLoader.Visibility = Visibility.Collapsed;

            // Show the loader we will be using going forward
            Loader.Visibility = Visibility.Visible;
            Loader.IsActive = false;

            // Reset the cursor
            SetCursor(Windows.UI.Core.CoreCursorType.Arrow);

            // Save current url within roaming settings so this can be 
            // persisted during the OnSuspending event within App.xaml.cs
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            if (!IsLocalUrl(sender.Source.ToString()))
            {
                roamingSettings.Values[nameof(AppSettings.PreviousUrl)] = sender.Source.ToString();
            }

            // Handle not found 
            switch (args.WebErrorStatus)
            {
                case Windows.Web.WebErrorStatus.NotFound:
                    WebView.Navigate(new Uri("ms-appx-web:///assets/web/NotFound.html"));
                    break;
            }

            // Inject client script into web view
            InjectClientScripts(args.Uri);

            // Update address bar
            UpdateAddressBa(args.Uri);

            // Indicate this is no longer the first request
            _firstRequest = false;

        }

        private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            SetCursor(Windows.UI.Core.CoreCursorType.Arrow);
        }

        private async void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {

            var messageArray = e.Value.Split(':');
            string message, type;
            if (messageArray.Length > 1)
            {
                message = messageArray[1];
                type = messageArray[0];
            }
            else
            {
                message = e.Value;
                type = "typeAlert";
            }

            var dialog = new Windows.UI.Popups.MessageDialog(message);
            if (type.Equals("typeConfirm"))
            {
                var cmd = new UICommandInvokedHandler(this.CommandInvokedHandler);
                dialog.Commands.Add(new UICommand("Yes", cmd));
                dialog.Commands.Add(new UICommand("Cancel",  cmd));
                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;
            }
            else if (type.Equals("typeLog"))
            {
                //Debug.WriteLine("type=" + type + " ,message=" + message);
            }

            var result = await dialog.ShowAsync();

        }

        private void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label.Equals("Yes"))
            {
                //Debug.WriteLine("got choose yes message - CommandInvokedHandler ");
            }
            else
            {
                //Debug.WriteLine("got choose no message - CommandInvokedHandler ");
            }
        }

        #endregion

        #region "Button events"

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoBack)
            {
                WebView.GoBack();
            }
        }

        void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoForward)
            {
                WebView.GoForward();
            }
        }

        void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WebView.Refresh();
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {

            // Save current url before navigating to settings
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey(nameof(AppSettings.PreviousUrl)))
            {
                var value = roamingSettings.Values[nameof(AppSettings.PreviousUrl)].ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    var settingsManager = ServiceLocator.Current.GetService<IAppSettingsManager>();
                    var settings = await settingsManager.GetSettings() ?? new AppSettings();
                    settings.PreviousUrl = value;
                    await settingsManager.SaveSettings(settings);
                }
            }

            Frame.Navigate(typeof(SettingsView), e);

        }

        void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(this);
        }

        #endregion

        #region "Address Bar Events"

        private void TidyAddress_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FullAddress.Visibility = Visibility.Visible;
            FullAddress.Select(FullAddress.ContentStart, FullAddress.ContentEnd);
            FullAddress.Focus(FocusState.Programmatic);
        }

        private void FullAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            FullAddress.Visibility = Visibility.Collapsed;
            TidyAddress.Visibility = Visibility.Visible;
        }

        #endregion

        #region "Private Methods"               

        private void UpdateAddressBa(Uri uri)
        {

            // Update the address
            var url = string.Empty;
            var tidyUrl = string.Empty;
            try
            {
                url = uri.ToString();
                tidyUrl = TidyUrl(url);
            }
            finally
            {
                FullAddress.Text = url;
                TidyAddress.Text = tidyUrl;
            }

        }

        private void InjectClientScripts(Uri uri)
        {
            var frame = GetFrame();
            var theme = frame.AppTheme;

            // Add theme css into local pages
            if (IsLocalUrl(uri))
            {
                InvokeScript("addCss", new string[] {$"css/app/themes/{theme}.css"});
                InvokeScript("addUrl", new string[] {ViewModel.Url});
                return;
            }

            // Upon the first request set a client side cookie to indicate
            // the plato theme that will be used for further requests
            if (_firstRequest)
            {
                InvokeScript($"if (window.$.Plato) {{ window.$.Plato.storage.setCookie('plato-theme', '{theme}'); }}");
            }

            /* window.external.notify usage below requires the apps URL
             * to be listed within the packages manifest file as shown below...
                <uap:ApplicationContentUriRules>
                    <uap:Rule Type="include" Match="http://localhost:50440/" WindowsRuntimeAccess="none"/>        
                </uap:ApplicationContentUriRules>
            */
            var script = @"                

                // Replace native functions that are not supported by UWP web view
                window.alert = function(msg) { window.external.notify('typeAlert:' + msg); }
                window.confirm = function() { return true } // window.confirm is not supported in webview for UWP
                
                // Is Plato available?
                if (window.$.Plato) {    
                    
                    // Wrap fixed content within a dummy div to prevent sticky layout
                    $('.layout-header-content').wrap($('<div>'));
                    $('.layout-sidebar-content').wrap($('<div>'));
                    $('.layout-asides-content').wrap($('<div>'));

                    // Add CSS to override default Plato styles
                    window.$.Plato.utils.addCss('{css}'); 

                }                
            ";
            script = script.Replace("{css}", new Uri($"ms-appx-web:///assets/web/css/plato/{theme}.css").ToString());
            InvokeScript(script);

        }

        private ThemeAwareFrame GetFrame()
        {
            if (Window.Current.Content is ThemeAwareFrame frame)
            {
                return frame;
            }

            throw new Exception("Could not cast the frame to type ThemeAwareFrame");
        }

        private void SetCursor(Windows.UI.Core.CoreCursorType cursorType)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(cursorType, 0);
        }

        private void InvokeScript(string script)
        {
            InvokeScript("eval", new string[] {script});
        }

        private async void InvokeScript(string functionName, string[] args)
        {
            try
            {
                await WebView.InvokeScriptAsync(functionName, args);
            }
            catch
            {
                // ignored
            }
        }

        private void NavigateWithHeader(Uri uri)
        {
            _firstRequest = true;
            var frame = GetFrame();
            var theme = frame.AppTheme;
            var requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMsg.Headers.Add("X-Plato-Theme", theme == ElementTheme.Dark ? "dark" : "light");
            WebView.NavigateWithHttpRequestMessage(requestMsg);
        }

        private string TidyUrl(string url)
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

        private bool IsLocalUrl(Uri uri)
        {
            try
            {
                return IsLocalUrl(uri.ToString());
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private bool IsLocalUrl(string url)
        {
            return url.ToLower().IndexOf("ms-appx-web", StringComparison.OrdinalIgnoreCase) >= 0 ? true : false;
        }

        #endregion

    }

}
