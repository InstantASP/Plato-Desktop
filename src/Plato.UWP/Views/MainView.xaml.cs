using Plato.UWP.DependencyInjection;
using Plato.UWP.Models;
using Plato.UWP.Services;
using Plato.UWP.ViewModels;
using System;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
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

            // Load view model
            await ViewModel.LoadAsync();

            // Make frame aware of configured theme
            var frame = GetFrame();
            frame.AppTheme = ViewModel.Theme;

            // Restore the background image
            if (!string.IsNullOrEmpty(ViewModel.BackgroundImage))
            {
                var bitmap = new BitmapImage(new Uri($"ms-appx-web:///{ViewModel.BackgroundImage}"));
                main.Background = new ImageBrush
                {
                    ImageSource = bitmap,
                    Stretch = Stretch.Fill
                };
            }

            // Wire up web view events
            webView1.NavigationStarting += WebView1_NavigationStarting;
            webView1.NavigationCompleted += webView1_NavigationCompleted;
            webView1.NavigationFailed += WebView1_NavigationFailed;
            webView1.ScriptNotify += WebView1_ScriptNotify;

            // Perform the first reuqest navigating with headers to indicate the theme
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

            // Update cursor
            SetCursor(Windows.UI.Core.CoreCursorType.Wait);

            // Activate loader
            loading.IsActive = true;          

        }

        private void webView1_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {

            // Hide the initial loader
            initialLoader.Visibility = Visibility.Collapsed;

            // Show the loader we will be using going forward
            loading.Visibility = Visibility.Visible;
            loading.IsActive = false;

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
                    webView1.Navigate(new Uri("ms-appx-web:///assets/web/NotFound.html"));
                    break;             
            }

            // Inject client script into web view
            InjectClientScripts(args.Uri);

            // Update address bar
            UpdateAddressBa(args.Uri);

            // Indicate this is no londer the first request
            _firstRequest = false;

        }

        private void WebView1_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            SetCursor(Windows.UI.Core.CoreCursorType.Arrow);
        }

        private async void WebView1_ScriptNotify(object sender, NotifyEventArgs e)
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
                dialog.Commands.Add(new UICommand(
                 "Yes",
                 new UICommandInvokedHandler(this.CommandInvokedHandler)));
                dialog.Commands.Add(new UICommand(
                "Cancel",
               new UICommandInvokedHandler(this.CommandInvokedHandler)));
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

        async void Settings_Click(object sender, RoutedEventArgs e)
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

        void UpdateAddressBa(Uri uri)
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
                txtFullAddress.Text = url;
                txtTidyAddress.Text = tidyUrl;
            }

        }

        void InjectClientScripts(Uri uri)
        {
            var frame = GetFrame();
            var theme = frame.AppTheme;

            // Add theme css into local pages
            if (IsLocalUrl(uri))
            {
                InsokeScript("addCss", new string[] { $"css/app/themes/{theme}.css" });
                InsokeScript("addUrl", new string[] { ViewModel.Url });
                return;
            }

            // Upon the first request set a client cookie to indicate
            // the plato theme that will be used going forward
            if (_firstRequest)
            {
                InsokeScript($"if (window.$.Plato) {{ window.$.Plato.storage.setCookie('plato-theme', '{theme}'); }}");
            }

            var script = @"                
                if (window.$.Plato) {    

                    // Add alert & confirm support
                    window.alert = function(msg) { window.external.notify('typeAlert:' + msg) }
                    window.confirm = function() { return true; } // window.confirm is not supported in UWP, always return true

                    // Wrapped fixed content within a dummy div to prevent stickyness
                    $('.layout-header-content').wrap($('<div>'));
                    $('.layout-sidebar-content').wrap($('<div>'));
                    $('.layout-asides-content').wrap($('<div>'));
                    // Add custom CSS
                    window.$.Plato.utils.addCss('{css}'); 
                }                
            ";
            script = script.Replace("{css}", new Uri($"ms-appx-web:///assets/web/css/plato/{theme}.css").ToString());
            InsokeScript(script);

        }

        ThemeAwareFrame GetFrame()
        {
            var frame = (Window.Current.Content as ThemeAwareFrame);
            if (frame != null)
            {
                return frame;
            }
            throw new Exception("Could not cast the frame to type ThemeAwareFrame");
        }

        void SetCursor(Windows.UI.Core.CoreCursorType cursorType)
        {            
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(cursorType, 0);
        }

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
        
        void NavigateWithHeader(Uri uri)
        {
            _firstRequest = true;
            var frame = GetFrame();
            var theme = frame.AppTheme;
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

        bool IsLocalUrl(Uri uri)
        {
            try
            {
                return IsLocalUrl(uri.ToString());
            } catch
            {

            }
            return false; 
        }

        bool IsLocalUrl(string url)
        {
            return url.ToLower().IndexOf("ms-appx-web") >= 0 ? true : false;
        }

        #endregion

    }

}
