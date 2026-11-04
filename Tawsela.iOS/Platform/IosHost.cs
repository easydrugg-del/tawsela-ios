using System;
using Foundation;
using Tawsela.Core.Platform;
using UIKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// خدمة المنصة على iOS: الشبكة، السجل، فتح الروابط.
    /// بديل ConnectivyManager و Android.Util.Log و Intent.ActionView.
    /// </summary>
    public sealed class IosHost : PortableHost
    {
        public IosHost() : base("ios") { }

        public override bool IsOnline()
        {
            // NetworkInterface يحترم حالة الاتصال الفعلية (Wi-Fi / بيانات)
            return base.IsOnline();
        }

        public override void Log(string tag, string message)
        {
            Console.WriteLine(string.Format("[{0}] {1}", tag, message));
        }

        public override bool OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            try
            {
                var nsUrl = NSUrl.FromString(url);
                if (nsUrl == null) return false;

                var app = UIApplication.SharedApplication;
                if (!app.CanOpenUrl(nsUrl)) return false;

                // OpenUrl(NSUrl, options, completion) — غير مهجورة بعكس OpenUrl(NSUrl)
                app.OpenUrl(nsUrl, new NSDictionary(), null);
                return true;
            }
            catch { return false; }
        }
    }
}
