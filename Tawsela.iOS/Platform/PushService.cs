using System;
using System.Text;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using UIKit;
using UserNotifications;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل FirebaseService + POST_NOTIFICATIONS على iOS.
    ///
    /// ملاحظة مهمة: على iOS لا يوجد FCM بدون APNs.
    /// - الإشعارات المحلية (أمامية) تعمل مباشرة عبر UNUserNotificationCenter.
    /// - الإشعارات من الخادم تحتاج: GoogleService-Info.plist + APNs Auth Key في Firebase
    ///   + تفعيل EnableFirebase=true في المشروع.
    /// </summary>
    public static class PushService
    {
        private const string PrefName = "tawsela";
        private const string PrefToken = "push_token";
        private const string PrefAsked = "push_asked";

        private static bool _initialized;

        /// <summary>يُسجَّل من Router لفتح الشاشة المناسبة عند لمس الإشعار (بديل PendingIntent).</summary>
        public static Action<int> TapHandler;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                UNUserNotificationCenter.Current.Delegate = new NotificationDelegate();
            }
            catch { }
        }

        /// <summary>طلب إذن الإشعارات — يجب أن يُستدعى في سياق مفهوم للمستخدم لا عند الإقلاع فجأة.</summary>
        public static void RequestAuthorization(Action<bool> done = null)
        {
            Init();
            try
            {
                UNUserNotificationCenter.Current.RequestAuthorization(
                    UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge,
                    (granted, error) =>
                    {
                        AppServices.Prefs.SetBool(PrefName + "." + PrefAsked, true);
                        Ui.BeginOnMain(() =>
                        {
                            if (granted) RegisterForRemote();
                            if (done != null) done(granted);
                        });
                    });
            }
            catch
            {
                if (done != null) Ui.BeginOnMain(() => done(false));
            }
        }

        /// <summary>طلب الإذن مرة واحدة فقط (لتفادي إزعاج المستخدم في كل فتح).</summary>
        public static void RequestAuthorizationOnce()
        {
            // وضع التصوير الآلي (CI): لا نطلب إذنًا حتى لا يغطي البيلوك الشاشة ويبتلع اللمسات.
            // يُفعَّل فقط عند تشغيل تطبيق المحاكي بهذه المتغيرات، ولا تأثير له على النسخة النهائية.
            try
            {
                if (Environment.GetEnvironmentVariable("TAWSELA_UI_TEST") == "1") return;
            }
            catch { }

            if (AppServices.Prefs.GetBool(PrefName + "." + PrefAsked, false)) return;
            RequestAuthorization(null);
        }

        public static void RegisterForRemote()
        {
            try
            {
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
            catch { }
        }

        /// <summary>يُستدعى من AppDelegate بعد نجاح التسجيل — يحوّل توكن APNs ويحفظه ويرسله للخادم.</summary>
        public static void HandleRegistered(NSData deviceToken)
        {
            if (deviceToken == null) return;
            try
            {
                var bytes = deviceToken.ToArray();
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                var token = sb.ToString();

                AppServices.Prefs.Set(PrefName + "." + PrefToken, token);

                // Firebase للـ iOS يعيد توكن FCM مختلفًا عن توكن APNs؛
                // عند تفعيله نستخدم FCM token، وإلا نرسل توكن APNs كما هو.
                SendToServer(token);
            }
            catch { }
        }

        public static void HandleFailedToRegister(NSError error)
        {
            AppServices.Log("TawselaPush", "APNs registration failed: " + (error == null ? "?" : error.LocalizedDescription));
        }

        /// <summary>تسجيل التوكن للخادم (نفس endpoint أندرويد /api/device-token).</summary>
        public static void SendToServer(string token)
        {
            if (string.IsNullOrEmpty(token) || Session.UserId <= 0) return;
            Ui.Run(() => Api.SetDeviceToken(token));
        }

        /// <summary>يعيد إرسال التوكن المحفوظ (يُستدعى بعد تسجيل الدخول).</summary>
        public static void RegisterCurrentToken()
        {
            try
            {
                var token = AppServices.Prefs.Get(PrefName + "." + PrefToken, "");
                SendToServer(token);
            }
            catch { }
        }

        /// <summary>عرض إشعار محلي (بديل Notification.Builder).</summary>
        public static void ShowLocal(string title, string body, string identifier = null, int orderId = 0, bool silent = false)
        {
            try
            {
                var content = new UNMutableNotificationContent
                {
                    Title = string.IsNullOrEmpty(title) ? "توصيلة" : title,
                    Body = body ?? "",
                    Sound = silent ? null : UNNotificationSound.Default
                };

                if (orderId > 0)
                {
                    content.UserInfo = NSDictionary.FromObjectAndKey(
                        new NSNumber(orderId), new NSString("orderId"));
                }

                var request = UNNotificationRequest.FromIdentifier(
                    identifier ?? Guid.NewGuid().ToString("N"),
                    content,
                    null); // null trigger = فوري

                UNUserNotificationCenter.Current.AddNotificationRequest(request, err =>
                {
                    if (err != null)
                        AppServices.Log("TawselaPush", "add notification failed: " + err.LocalizedDescription);
                });
            }
            catch { }
        }

        /// <summary>تصفير شعار الإشعارات.</summary>
        public static void ClearBadge()
        {
            try
            {
                UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
            }
            catch { }
        }

        /// <summary>هل الإشعارات مسموحة من إعدادات النظام؟</summary>
        public static void GetSettings(Action<bool> done)
        {
            try
            {
                UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
                {
                    var ok = settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
                    Ui.BeginOnMain(() => { if (done != null) done(ok); });
                });
            }
            catch
            {
                if (done != null) Ui.BeginOnMain(() => done(false));
            }
        }
    }

    /// <summary>عرض الإشعارات أثناء فتح التطبيق + فتح الشاشة المناسبة عند اللمس.</summary>
    public sealed class NotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(UNUserNotificationCenter center,
            UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            // إظهار الإشعار حتى والتطبيق مفتوح (كما في أندرويد)
            completionHandler(UNNotificationPresentationOptions.Alert |
                              UNNotificationPresentationOptions.Sound |
                              UNNotificationPresentationOptions.Badge);
        }

        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center,
            UNNotificationResponse response, Action completionHandler)
        {
            try
            {
                var userInfo = response.Notification?.Request?.Content?.UserInfo;
                int orderId = 0;
                if (userInfo != null && userInfo.ContainsKey(new NSString("orderId")))
                {
                    var num = userInfo.ObjectForKey(new NSString("orderId")) as NSNumber;
                    if (num != null) orderId = num.Int32Value;
                }

                Ui.BeginOnMain(() =>
                {
                    try
                    {
                        // التوجيه يُسجَّل من طبقة الشاشات (Router) بعد إقلاعها
                        var handler = PushService.TapHandler;
                        if (handler != null) handler(orderId);
                        PushService.ClearBadge();
                    }
                    catch { }
                    completionHandler();
                });
            }
            catch
            {
                completionHandler();
            }
        }
    }
}
