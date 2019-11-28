using Plato.UWP.Models;
using System.Threading.Tasks;

namespace Plato.UWP.Services
{
    public interface IAppSettingsManager
    {
        Task<AppSettings> GetSettings();

        Task<AppSettings> SaveSettings(AppSettings settings);
    }

}
