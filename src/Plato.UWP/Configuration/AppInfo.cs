using Windows.ApplicationModel;
using Windows.Storage;

namespace Plato.UWP.Configuration
{
    public static class AppInfo
    {

        public static StorageFolder LocalFolder
        {
            get
            {
                return ApplicationData.Current.LocalFolder;        
            }
        }

        public static string Version
        {
            get
            {
                var ver = Package.Current.Id.Version;
                return $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
            }
        }


    }
}
