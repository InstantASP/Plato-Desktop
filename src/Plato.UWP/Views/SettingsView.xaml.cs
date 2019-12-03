﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Plato.UWP.DependencyInjection;
using Plato.UWP.Services;
using Plato.UWP.ViewModels;
using Plato.UWP.Models;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Plato.UWP.Configuration;
using Windows.Storage;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
            //this.RequestedTheme = ViewModel.Theme;
            (Window.Current.Content as ThemeAwareFrame).AppTheme = ViewModel.Theme;

            if (!string.IsNullOrEmpty(ViewModel.Url))
            {
                txtSettingsUrl.Text = ViewModel.Url;
            }

            if (!string.IsNullOrEmpty(ViewModel.Theme.ToString()))
            {
                ddlTheme.SelectedItem = ViewModel.Theme.ToString();
            }
            
            LoadImages();

            if (!string.IsNullOrEmpty(ViewModel.BackgroundImage))
            {
                var bitmap = new BitmapImage(new Uri($"ms-appx-web:///{ViewModel.BackgroundImage}"));
                main.Background = new ImageBrush
                {
                    ImageSource = bitmap,
                    Stretch = Stretch.Fill
                };
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

        async void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            await WebView.ClearTemporaryWebDataAsync();
            Frame.Navigate(typeof(MainView), e);
        }

        async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            
            // Get settings sergice
            var settingsManager = ServiceLocator.Current.GetService<IAppSettingsManager>();

            // Get current settings
            var settings = await settingsManager.GetSettings() ?? new AppSettings();

            // Default to http if no protocol is provided
            var url = txtSettingsUrl.Text;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }

            // Validate url
            var ok = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult);
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


        async void LoadImages()
        {
        
            var filePaths = await GetAssetFiles("Assets/Backgrounds");
            if (filePaths != null)
            {

                if (filePaths != null)
                {
                    var i = 0;
                    foreach (var filePath in filePaths)
                    {
                        var random = await Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri($"ms-appx:///{filePath}")).OpenReadAsync();
                        var image = new Image();
                        var bitmapImage = new BitmapImage();
                        var mystackPanel = new StackPanel();
                        mystackPanel.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        mystackPanel.BorderThickness = new Thickness(1);                  
                        image.Name = filePath;
                        image.Margin = new Thickness(10);
                        bitmapImage.SetSource(random);
                        image.Source = bitmapImage;                 
                        image.Stretch = Stretch.UniformToFill;               
                        mystackPanel.Children.Add(image);
                        image.PointerEntered += Image_PointerEntered;
                        image.PointerExited += Image_PointerExited;
                        pnlBackgrounds.Children.Add(mystackPanel);
                        i++;
                    }
                }
  
            }

            SetSelectedImage();

        }

        private void Image_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var image = sender as Image;
            var parent = VisualTreeHelper.GetParent(image) as StackPanel;

            if (image.Name != ViewModel.BackgroundImage)
            {
                parent.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
            
            image.Tapped -= Image_Tapped;
        }

        private void Image_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var image = sender as Image;            
            var parent = VisualTreeHelper.GetParent(image) as StackPanel;
            parent.BorderBrush = new SolidColorBrush(Colors.MediumSlateBlue);     
            image.Tapped += Image_Tapped;
        }

        async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var image = sender as Image;

            // Get current settings
            var settingsManager = ServiceLocator.Current.GetService<IAppSettingsManager>();
            var settings = await settingsManager.GetSettings() ?? new AppSettings();            
            settings.BackgroundImage = image.Name;
            await settingsManager.SaveSettings(settings);

            ViewModel.BackgroundImage = image.Name;

            Frame.Navigate(typeof(SettingsView), e);
        }

        void ClearSelectedImage()
        {         
            foreach (StackPanel stackPanel in pnlBackgrounds.Children)
            {
                stackPanel.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }      
        }


        void SetSelectedImage()
        {

            ClearSelectedImage();

            if (string.IsNullOrEmpty(ViewModel.BackgroundImage))
            {
                return;
            }
        
            foreach (StackPanel stackPanel in pnlBackgrounds.Children)
            {
                var image = stackPanel.Children[0] as Image;
                if (image != null)
                {
                    if (image.Name == ViewModel.BackgroundImage)
                    {
                        stackPanel.BorderBrush = new SolidColorBrush(Colors.MediumSlateBlue);
                    }
                }
            }

        }

        string GetSelectedImageFileName()
        {

            var fileName = string.Empty;
            foreach (StackPanel stackPanel in pnlBackgrounds.Children)
            {
                if (stackPanel.BorderBrush != new SolidColorBrush(Colors.Transparent))
                {
                    var image = stackPanel.Children[0] as Image;
                    if (image != null)
                    {
                        return image.Name;                   
                    }
                }
            }
            return fileName;
        }

        static async Task<IEnumerable<string>> GetAssetFiles(string rootPath)
        {

            var folderPath = System.IO.Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                rootPath.Replace('/', '\\').TrimStart('\\')
            );

            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var files = await folder.GetFilesAsync();
            var relativePaths = from file in files select (rootPath + "/" + file.Name);
            return relativePaths;

        }


    }

}
