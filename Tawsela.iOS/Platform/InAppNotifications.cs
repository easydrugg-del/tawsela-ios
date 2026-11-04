using System;
using System.Threading;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using UIKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل Data/AppNotifications.cs على iOS:
    /// استطلاع الإشعارات أثناء فتح التطبيق فقط (بدون خدمة خلفية)،
    /// يعرض الإشعارات غير المقروءة ثم يعلّمها كمقروءة،
    /// ويعرض تنبيهات الطلبات المجدولة للعميل.
    /// </summary>
    public static class InAppNotifications
    {
        private const int IntervalMs = 30000;
        private const string PrefName = "tawsela";
        private const string KeySilent = "silent";

        private static Timer _timer;
        private static int _busy;
        private static bool _started;

        private static bool IsSilent
        {
            get
            {
                try { return AppServices.Prefs.GetBool(PrefName + "." + KeySilent, false); }
                catch { return false; }
            }
        }

        public static void Start()
        {
            if (_started) return;
            _started = true;
            _timer = new Timer(Tick, null, 0, IntervalMs);
        }

        public static void Stop()
        {
            _started = false;
            var t = Interlocked.Exchange(ref _timer, null);
            if (t != null)
            {
                try { t.Dispose(); } catch { }
            }
        }

        /// <summary>التطبيق في المقدمة؟ (بديل PowerManager.IsInteractive + ActivityLifecycle)</summary>
        private static bool IsForeground
        {
            get
            {
                try
                {
                    return UIApplication.SharedApplication.ApplicationState == UIApplicationState.Active;
                }
                catch { return false; }
            }
        }

        private static void Tick(object state)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;
            try
            {
                // لا نعمل إلا والتطبيق مفتوح (كما في أندرويد)
                if (!IsForeground) return;

                if (Session.UserId <= 0)
                {
                    try { Api.RestoreSession(); } catch { }
                }
                if (Session.UserId <= 0) return;

                var all = Api.UserNotifications();
                var any = false;
                if (all != null)
                {
                    foreach (var n in all)
                    {
                        if (n == null || n.IsRead) continue;
                        any = true;
                        PushService.ShowLocal(n.Title, n.Body, "notif_" + n.Id, 0, IsSilent);
                    }
                }
                if (any) Api.MarkAllRead();

                if (Session.IsCustomer)
                {
                    var due = Api.ScheduledDue();
                    if (due != null)
                    {
                        foreach (var o in due)
                        {
                            PostReminder(o);
                            Api.MarkReminded(o.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppServices.Log("TawselaNotif", ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        private static void PostReminder(Order o)
        {
            if (o == null) return;
            var service = string.IsNullOrEmpty(o.ServiceTypeName) ? "توصيلة" : o.ServiceTypeName;
            var time = o.ScheduledAt == default(DateTime) ? "" : o.ScheduledAt.ToString("HH:mm");
            var text = "طلبك المجدول (" + service + ") الساعة " + time;

            PushService.ShowLocal("تذكير بطلب مجدول", text, "reminder_" + o.Id, o.Id, IsSilent);
        }
    }
}
