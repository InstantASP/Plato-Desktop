using Plato.UWP.DependencyInjection;
using Plato.UWP.Models;
using Plato.UWP.Services;
using Plato.UWP.Views;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Plato.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new ThemeAwareFrame(ElementTheme.Default); ;

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                // Configure DI
                ConfigureServices();

            }

            if (e.PrelaunchActivated == false)
            {

                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainView), e.Arguments);
                }

                // Ensure the current window is active
                Window.Current.Activate();


            }
        }


        async void ConfigureServices()
        {
            await Startup.ConfigureAsync();
        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            SaveStateAsync();
            deferral.Complete();

        }

        async void SaveStateAsync()
        {
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
        }

    }

}
