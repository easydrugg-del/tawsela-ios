using System;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// بديل Activities/TawselaApplication.cs + دورة حياة التطبيق في أندرويد.
    /// يهيّئ طبقة المنصة، يطبّق الهوية البصرية، ويدير الإشعارات مع الخلفية.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // 1) طبقة المنصة قبل أي استخدام لـ Api / SecureStore / AppLock
            IosPlatform.Init();

            // 2) تثبيت تثبيت الشهادة (Pinning) مع HttpClientHandler بدل NSUrlSessionHandler
            try { System.AppContext.SetSwitch("System.Net.Http.UseNativeHttpHandler", false); } catch { }

            // 3) الهوية البصرية + اتجاه RTL
            Theme.ApplyGlobal();

            // 4) الإشعارات (بديل FirebaseService + قناة الإشعارات)
#if FIREBASE_IOS
            // Firebase Core قبل أي استخدام لـ Messaging (يقرأ GoogleService-Info.plist)
            try { Firebase.Core.App.Configure(); } catch { }
#endif
            if (!ScreenshotRunner.Enabled)
            {
                PushService.Init();
                Router.RegisterPushRouting();
            }

            // 5) ترحيل أمني: مسح أي بيانات اتصال مباشر بقاعدة من إصدارات قديمة
            try
            {
                var prefs = AppServices.Prefs;
                prefs.Remove("connection_settings");
            }
            catch { }

            // 6) الشبكة تُهيّأ للاستخدام (آخر موقع معروف)
            if (!ScreenshotRunner.Enabled)
            {
                try { LocationService.Init(); } catch { }
            }

            return true;
        }

        public override void OnActivated(UIApplication application)
        {
            // بديل TawselaApplication.OnActivityStarted → استطلاع الإشعارات أثناء الفتح
            try { InAppNotifications.Start(); } catch { }
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // بديل OnActivityStopped → إيقاف الاستطلاع وحفظ وقت الخلفية لقفل التطبيق
            try { InAppNotifications.Stop(); } catch { }
            try { if (AppLock.IsActive()) AppLock.MarkBackground(); } catch { }
        }

        public override void WillEnterForeground(UIApplication application)
        {
            try { InAppNotifications.Start(); } catch { }
        }

        /* ---------- Push ---------- */

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            PushService.HandleRegistered(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            PushService.HandleFailedToRegister(error);
        }

        public override void DidReceiveRemoteNotification(UIApplication application,
            NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            // APNs صامت / دفعة FCM في الخلفية
            try
            {
                var aps = userInfo != null ? userInfo.ObjectForKey(new NSString("aps")) as NSDictionary : null;
                var alert = aps != null ? aps.ObjectForKey(new NSString("alert")) as NSDictionary : null;
                var title = alert != null ? (alert.ObjectForKey(new NSString("title")) as NSString)?.ToString() : null;
                var body = alert != null ? (alert.ObjectForKey(new NSString("body")) as NSString)?.ToString() : null;

                if (!string.IsNullOrEmpty(body))
                    PushService.ShowLocal(title ?? "توصيلة", body);

                if (completionHandler != null) completionHandler(UIBackgroundFetchResult.NewData);
            }
            catch
            {
                if (completionHandler != null) completionHandler(UIBackgroundFetchResult.Failed);
            }
        }
    }
}
