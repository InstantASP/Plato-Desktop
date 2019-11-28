using Newtonsoft.Json;
using Plato.UWP.Configuration;
using Plato.UWP.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Plato.UWP.Services
{

    public interface IAppSettingsManager
    {
        Task<AppSettings> GetSettings();

        Task<AppSettings> SaveSettings(AppSettings settings);
    }

    public class AppSettingsManager : IAppSettingsManager
    {

        public const string FileName = "settings.json";

        public async Task<AppSettings> GetSettings()
        {

            var path = Path.Combine(AppInfo.LocalFolder.Path, FileName);

            var text = await ReadFileAsync(path);
            if (!string.IsNullOrEmpty(text))
            {
                return JsonConvert.DeserializeObject<AppSettings>(text);
            }

            return null;
            
        }

        public async Task<AppSettings> SaveSettings(AppSettings settings)
        {

            var path = Path.Combine(AppInfo.LocalFolder.Path, FileName);
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
