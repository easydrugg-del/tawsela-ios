using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Screens;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// وضع التقاط الشاشات (للاختبار فقط): يُشغَّل بوسيطة --shots
    /// يفتح الشاشات واحدة واحدة ويحفظ صورًا PNG في Documents/shots
    /// ثم يكتب done.txt — بلا حاجة إلى tunnel أو نقر خارجي.
    /// </summary>
    public static class ScreenshotRunner
    {
        public static bool Enabled
        {
            get
            {
                try
                {
                    foreach (var a in NSProcessInfo.ProcessInfo.Arguments)
                        if (a == "--shots") return true;
                }
                catch { }
                return false;
            }
        }

        /// <summary>تفعيل فحص الأزرار مع اللقطات (--taps) — يطيل الوقت.</summary>
        public static bool TapsEnabled
        {
            get
            {
                try
                {
                    foreach (var a in NSProcessInfo.ProcessInfo.Arguments)
                        if (a == "--taps") return true;
                }
                catch { }
                return false;
            }
        }

        public static string Dir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "shots");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Start(UIWindow window)
        {
            window.RootViewController = new ScreenshotHostViewController(window);
        }
    }

    public class ScreenshotHostViewController : UIViewController
    {
        private readonly UIWindow _window;
        private bool _started;

        public ScreenshotHostViewController(UIWindow window)
        {
            _window = window;
            View.BackgroundColor = UIColor.White;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (_started) return;
            _started = true;
            _ = RunAll();
        }

        private async Task RunAll()
        {
            var dir = ScreenshotRunner.Dir;
            var ok = new List<string>();

            try
            {
                // شاشات لا تحتاج تسجيل دخول
                await Shot(dir, ok, "01-intro", () => new IntroViewController());
                await Shot(dir, ok, "02-login", () => new LoginViewController());
                await Shot(dir, ok, "03-register", () => new RegisterViewController());

                // ===== عميل =====
                await Login("customer1", "123456");
                await Shot(dir, ok, "10-home-customer", () => new MainViewController());
                await Shot(dir, ok, "11-new-order", () => new NewOrderViewController());
                await Shot(dir, ok, "12-wallet", () => new WalletViewController());
                await Shot(dir, ok, "13-coupons", () => new CouponsViewController());
                await Shot(dir, ok, "14-referral", () => new ReferralViewController());
                await Shot(dir, ok, "15-loyalty", () => new LoyaltyViewController());
                await Shot(dir, ok, "16-challenges", () => new ChallengesViewController());
                await Shot(dir, ok, "17-subscription", () => new SubscriptionViewController());
                await Shot(dir, ok, "18-support", () => new ComplaintViewController());
                await Shot(dir, ok, "19-notifications", () => new NotificationsViewController());
                await Shot(dir, ok, "20-profile", () => new ProfileViewController());
                await Shot(dir, ok, "21-settings", () => new SettingsViewController());
                await Shot(dir, ok, "22-topup-history", () => new TopUpHistoryViewController());
                await Shot(dir, ok, "23-customer-stats", () => new CustomerStatsViewController());
                await Shot(dir, ok, "24-my-orders",
                    () => new OrdersViewController("customer", "", "طلباتي"));
                await Shot(dir, ok, "25-track", () =>
                {
                    var o = new Order
                    {
                        Id = 37,
                        Status = "جديد",
                        PaymentMethod = "نقدي",
                        FromAddress = "شارع عبد الباقي غراب، كفر عنان، الغربية",
                        ToAddress = "شارع البحر، زفتى",
                        PinCode = "7505",
                        EstimatedFare = 16m
                    };
                    return new TrackOrderViewController(o);
                });

                // ===== سائق =====
                await Login("playdrv094621", "Tw!c5ecdb89e37f");
                await Shot(dir, ok, "30-home-driver", () => new MainViewController());
                await Shot(dir, ok, "31-driver-stats", () => new DriverStatsViewController());
                await Shot(dir, ok, "32-driver-shifts", () => new DriverShiftsViewController());
                await Shot(dir, ok, "33-driver-docs", () => new DriverDocsViewController());
                await Shot(dir, ok, "34-driver-statement", () => new DriverStatementViewController());
                await Shot(dir, ok, "35-driver-orders",
                    () => new OrdersViewController("driver", "", "توصيلاتي"));
                await Shot(dir, ok, "36-restaurant-requests", () => new RestaurantRequestsViewController());

                // ===== مطعم =====
                await Login("s", "12345678");
                await Shot(dir, ok, "40-restaurant-home", () => new RestaurantMainViewController());
                await Shot(dir, ok, "41-restaurant-profile", () => new RestaurantProfileViewController());
                await Shot(dir, ok, "42-restaurant-new",
                    () => new RestaurantNewDeliveryViewController(new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "Id", 1 },
                            { "Name", "زفتى" },
                            { "Price", 25.0 },
                            { "City", "زفتى" },
                            { "Governorate", "الغربية" },
                        }
                    }));
                await Shot(dir, ok, "43-restaurant-requests", () => new RestaurantRequestsViewController());

                // ===== الإدارة =====
                await Login("admin", "admin123");
                await Shot(dir, ok, "50-admin-home", () => new AdminHomeViewController());
                await Shot(dir, ok, "51-admin-customers", () => new CustomersAdminViewController());
                await Shot(dir, ok, "52-admin-drivers", () => new DriversAdminViewController());
                await Shot(dir, ok, "53-admin-areas", () => new AreasAdminViewController());
                await Shot(dir, ok, "54-admin-streets", () => new StreetsAdminViewController());
                await Shot(dir, ok, "55-admin-coupons", () => new CouponsAdminViewController());
                await Shot(dir, ok, "56-admin-docs", () => new DocsAdminViewController());
                await Shot(dir, ok, "57-admin-complaints", () => new ComplaintsAdminViewController());
                await Shot(dir, ok, "58-admin-subscriptions", () => new SubscriptionsAdminViewController());
                await Shot(dir, ok, "59-admin-topups", () => new TopUpsAdminViewController());
                await Shot(dir, ok, "60-admin-trips", () => new TripReportViewController());
                await Shot(dir, ok, "61-admin-analytics", () => new AdminAnalyticsViewController());
                await Shot(dir, ok, "67-admin-driver-fee", () => new DriverFeeAdminViewController());
                await Shot(dir, ok, "68-admin-app-update", () => new AppUpdateAdminViewController());
                await Shot(dir, ok, "69-admin-heatmap", () => new HeatmapViewController());
                await Shot(dir, ok, "70-admin-driver-search", () => new DriverSearchViewController());
                await Shot(dir, ok, "71-supervisor-earnings", () => new SupervisorEarningsViewController(null));
                await Shot(dir, ok, "62-admin-ads", () => new AdsAdminViewController());
                await Shot(dir, ok, "63-admin-restaurants", () => new RestaurantsAdminViewController());
                await Shot(dir, ok, "72-admin-restaurant-delivery", () => new RestaurantDeliveryAdminViewController());
                await Shot(dir, ok, "64-admin-services", () => new ServiceAdminViewController());
                await Shot(dir, ok, "65-admin-supervisors", () => new SupervisorAdminViewController());
                await Shot(dir, ok, "66-admin-emergencies", () => new EmergenciesAdminViewController());
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(dir, "error.txt"), ex.ToString());
            }

            SafeWrite(Path.Combine(dir, "done.txt"), string.Join("\n", ok));
        }

        private async Task Login(string user, string pass)
        {
            try
            {
                await Task.Run(() => Api.Login(user, pass));
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(ScreenshotRunner.Dir, "login-errors.txt"),
                    user + ": " + ex.Message + "\n");
            }

            await Task.Delay(600);
        }

        private async Task Shot(string dir, List<string> ok, string name, Func<UIViewController> make)
        {
            try
            {
                var vc = make();
                var nav = new UINavigationController(vc);
                nav.NavigationBar.TintColor = UIColor.White;
                _window.RootViewController = nav;
                _window.MakeKeyAndVisible();

                await Task.Delay(3200);

                var image = Snapshot(_window);
                using (var data = image.AsPNG())
                {
                    data.Save(Path.Combine(dir, name + ".png"), true);
                    ok.Add(name);
                }

                // فحص الشاشة (نصوص أخطاء، أزرار من غير أوامر، عدد الصفوف)
                Audit(dir, name, vc);

                // ملاحظة: فحص الأزرار (TapSafe) يُشغَّل في وضع منفصل --taps حتى لا يطيل جمع اللقطات
                if (ScreenshotRunner.TapsEnabled) await TapSafe(dir, name, nav, vc);

                // إغلاق أي حوار متبقٍ حتى لا يظهر فوق الشاشة التالية
                try
                {
                    var root = _window.RootViewController;
                    if (root != null && root.PresentedViewController != null)
                        root.DismissViewController(false, null);
                }
                catch { }
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(dir, "shot-errors.txt"), name + ": " + ex.ToString());
            }
        }

        /* ---------- الفحص الشامل ---------- */

        private class AuditInfo
        {
            public int Buttons, DeadButtons, Fields, Labels, Tables, Rows, Controls;
            public readonly List<string> DeadTitles = new List<string>();
            public readonly List<string> Errors = new List<string>();
        }

        private static readonly string[] ErrorTokens =
        {
            "تعذر", "خطا", "فشل", "غير متاح", "لم يتم", "مشكله في", "غير مصرح",
            "لا يمكن", "غير صحيح", "مرفوض", "حدث خطا", "الطلب غير", "منتهي الصلاحيه"
        };

        /// <summary>إزالة التشكيل حتى يطابق «تعذّر» مع «تعذر».</summary>
        private static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var chars = new char[text.Length];
            var n = 0;
            foreach (var ch in text)
            {
                if ((ch >= '\u0610' && ch <= '\u061A') ||
                    (ch >= '\u064B' && ch <= '\u065F') ||
                    ch == '\u0670' ||
                    (ch >= '\u06D6' && ch <= '\u06ED') ||
                    ch == '\u0640')
                    continue;
                chars[n++] = ch;
            }

            return new string(chars, 0, n).Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا')
                .Replace('ى', 'ي').Replace('ة', 'ه').ToLowerInvariant();
        }

        private static bool LooksLikeError(string text)
        {
            var normalized = Normalize(text);
            foreach (var token in ErrorTokens)
                if (normalized.IndexOf(token, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        private static void WalkViews(UIView view, AuditInfo info)
        {
            if (view == null || info.Controls > 4000) return;
            info.Controls++;

            var label = view as UILabel;
            if (label != null && !string.IsNullOrEmpty(label.Text))
            {
                info.Labels++;
                if (LooksLikeError(label.Text) && info.Errors.Count < 6)
                    info.Errors.Add(label.Text.Trim().Replace("\n", " "));
            }

            var field = view as UITextField;
            if (field != null) info.Fields++;

            var table = view as UITableView;
            if (table != null)
            {
                info.Tables++;
                try { info.Rows += (int)table.NumberOfRowsInSection(0); } catch { }
            }

            var collection = view as UICollectionView;
            if (collection != null)
            {
                info.Tables++;
                try { info.Rows += (int)collection.NumberOfItemsInSection(0); } catch { }
            }

            var button = view as UIButton;
            if (button != null)
            {
                info.Buttons++;
                var title = (button.CurrentTitle ?? "").Trim();
                var targets = button.AllTargets;
                if (targets == null || targets.Count == 0)
                {
                    info.DeadButtons++;
                    if (title != "" && info.DeadTitles.Count < 8) info.DeadTitles.Add(title);
                }
            }

            foreach (var sub in view.Subviews) WalkViews(sub, info);
        }

        private void Audit(string dir, string name, UIViewController vc)
        {
            try
            {
                var info = new AuditInfo();
                WalkViews(vc.View, info);

                // تنبيه/حوار مفتوح بعد التحميل = غالبًا رسالة خطأ
                var presented = vc.PresentedViewController as UIAlertController;
                if (presented != null)
                    info.Errors.Add("ALERT: " + (presented.Title ?? "") + " / " + (presented.Message ?? ""));

                var line = string.Format(
                    "{0} | buttons={1} dead={2} fields={3} labels={4} tables={5} rows={6}",
                    name, info.Buttons, info.DeadButtons, info.Fields, info.Labels, info.Tables, info.Rows);

                if (info.DeadTitles.Count > 0)
                    line += " | deadTitles=" + string.Join("; ", info.DeadTitles);
                if (info.Errors.Count > 0)
                    line += " | ERRORS=" + string.Join(" ; ", info.Errors);

                SafeWrite(Path.Combine(dir, "audit.txt"), line);
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(dir, "audit-errors.txt"), name + ": " + ex.Message);
            }
        }

        /// <summary>أزرار آمنة للضغط: تفتح/تغلق/تعرض/تحدّث فقط (ممنوع أي إجراء مُدمّر).</summary>
        private static readonly string[] SafePrefixes =
        {
            "‹", "رجوع", "إغلاق", "إلغاء", "تحديث", "عرض", "الكل", "المزيد", "تفاصيل",
            "التالي", "السابق", "نسخ", "مشاركة", "إعادة المحاولة", "تحديد", "اختر", "تغيير"
        };

        /// <summary>كلمات تمنع الضغط: إجراءات مُدمّرة أو تفتح تطبيقًا خارجيًا (Safari/هاتف).</summary>
        private static readonly string[] DangerWords =
        {
            "حذف", "حظر", "دفع", "سحب", "شراء", "إرسال", "تأكيد", "تقييم", "خروج",
            "شكوى", "تفعيل", "إيقاف", "إلغاء الطلب", "خصم", "الغاء", "موافقة", "رفض",
            // تفتح تطبيقًا خارجيًا فيتوقف وضع اللقطات
            "سياسة", "شروط", "اتصل", "اتصال", "واتساب", "مكالمة", "هاتف", "البريد", "دعم", "معرفة"
        };

        private static bool IsSafeTitle(string title)
        {
            if (string.IsNullOrEmpty(title) || title.Length > 30) return false;
            foreach (var d in DangerWords)
                if (title.IndexOf(d, StringComparison.Ordinal) >= 0) return false;
            foreach (var s in SafePrefixes)
                if (title.StartsWith(s, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void CollectButtons(UIView view, List<UIButton> list, int depth)
        {
            if (view == null || depth > 12) return;
            var b = view as UIButton;
            if (b != null) list.Add(b);
            foreach (var sub in view.Subviews) CollectButtons(sub, list, depth + 1);
        }

        private async Task TapSafe(string dir, string name, UINavigationController nav, UIViewController vc)
        {
            try
            {
                var list = new List<UIButton>();
                CollectButtons(vc.View, list, 0);

                var taken = 0;
                for (var i = 0; i < list.Count && taken < 2; i++)
                {
                    var b = list[i];
                    if (b == null || b.Hidden || b.Alpha < 0.1f) continue;

                    var title = (b.CurrentTitle ?? "").Trim();
                    if (!IsSafeTitle(title)) continue;

                    // لا تضغط إن غادر التطبيق الواجهة (Safari/الهاتف) — اللقطات تتوقف
                    if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
                    {
                        SafeWrite(Path.Combine(dir, "audit.txt"), name + " | taps stopped (app not active)");
                        break;
                    }

                    taken++;
                    try
                    {
                        b.SendActionForControlEvents(UIControlEvent.TouchUpInside);
                    }
                    catch (Exception ex)
                    {
                        SafeWrite(Path.Combine(dir, "audit.txt"),
                            name + " | TAP-ERROR «" + title + "»: " + ex.Message);
                        continue;
                    }

                    await Task.Delay(1600);

                    UIViewController modal = nav.PresentedViewController ?? vc.PresentedViewController;
                    UIViewController top = null;
                    if (modal != null) top = modal;
                    else if (nav.ViewControllers.Length > 1)
                        top = nav.ViewControllers[nav.ViewControllers.Length - 1];

                    SafeWrite(Path.Combine(dir, "audit.txt"),
                        name + " | tap «" + title + "» -> " +
                        (top == null ? "no-change" : top.GetType().Name));

                    if (top != null)
                    {
                        try
                        {
                            var img = Snapshot(_window);
                            using (var d = img.AsPNG())
                                d.Save(Path.Combine(dir, name + "-tap" + taken + ".png"), true);
                        }
                        catch { }

                        if (modal != null)
                            modal.DismissViewController(false, null);
                        else if (nav.ViewControllers.Length > 1)
                            nav.PopViewController(false);

                        await Task.Delay(900);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(dir, "audit-errors.txt"), name + " (taps): " + ex.Message);
            }
        }

        private static UIImage Snapshot(UIView view)
        {
            var bounds = view.Bounds;
            var format = new UIGraphicsImageRendererFormat { Scale = 3f, Opaque = true };
            var renderer = new UIGraphicsImageRenderer(bounds.Size, format);
            return renderer.CreateImage(ctx => view.DrawViewHierarchy(bounds, true));
        }

        private static void SafeWrite(string path, string text)
        {
            try { File.AppendAllText(path, text + "\n"); } catch { }
        }
    }
}
