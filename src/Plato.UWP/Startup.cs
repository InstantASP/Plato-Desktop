using Microsoft.Extensions.DependencyInjection;
using Plato.UWP.Configuration;
using Plato.UWP.DependencyInjection;
using Plato.UWP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Plato.UWP
{
    public static class Startup
    {

        static private readonly ServiceCollection _serviceCollection = 
            new ServiceCollection();

        public static async Task ConfigureAsync()
        {

            ServiceLocator.Configure(_serviceCollection);

            await EnsureSettingsAsync();

        }
        
        static async Task EnsureSettingsAsync()
        {

            var localFolder = AppInfo.LocalFolder;            
            if (await localFolder.TryGetItemAsync(AppInfo.SettingsFileName) == null)
            {
                var sourceSettings = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{AppInfo.SettingsFileName}"));
                var tagetSettings = await localFolder.CreateFileAsync(AppInfo.SettingsFileName, CreationCollisionOption.ReplaceExisting);
                await sourceSettings.CopyAndReplaceAsync(tagetSettings);
            }
        }

    }

}
