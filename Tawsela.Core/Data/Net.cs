using Tawsela.Core.Platform;

namespace Tawsela.Data
{
    /// <summary>
    /// حالة الشبكة — نسخة مشتركة.
    /// أندرويد: ConnectivityManager (عبر IAppHost) / iOS: NWPathMonitor أو NetworkInterface.
    /// </summary>
    public static class Net
    {
        public static bool IsOnline()
        {
            return AppServices.IsOnline();
        }
    }
}
