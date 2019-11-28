using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plato.UWP.Configuration;
using Plato.UWP.Models;

namespace Plato.UWP.Services
{

    public class AppSettingsManager : IAppSettingsManager
    {  

        public async Task<AppSettings> GetSettings()
        {

            var path = Path.Combine(AppInfo.LocalFolder.Path, AppSettingsConstants.FileName);

            var text = await ReadFileAsync(path);
            if (!string.IsNullOrEmpty(text))
            {
                return JsonConvert.DeserializeObject<AppSettings>(text);
            }

            return null;
            
        }

        public async Task<AppSettings> SaveSettings(AppSettings settings)
        {

            var path = Path.Combine(AppInfo.LocalFolder.Path, AppSettingsConstants.FileName);
            var text = JsonConvert.SerializeObject(settings);

            try
            {
                await SaveFileAsync(path, text);                
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return await GetSettings();

        }

        async Task<string> ReadFileAsync(string path)
        {
            using (var reader = File.OpenText(path))
            {
                return await reader.ReadToEndAsync();
            }
        }

        async Task SaveFileAsync(string path, string content)
        {
            using (var writer = File.CreateText(path))
            {
                await writer.WriteAsync(content);
            }
        }

    }

}
