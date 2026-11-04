using UIKit;

namespace Tawsela.iOS
{
    public static class Program
    {
        // نقطة البداية — بديل MainActivity/SplashActivity في أندرويد (الإقلاع يديره iOS)
        public static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
