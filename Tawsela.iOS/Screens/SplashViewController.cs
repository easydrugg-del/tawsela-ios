using System;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/SplashActivity.cs:
    /// شعار + استعادة الجلسة + فحص تحديث الإصدار + التوجيه للشاشة التالية.
    /// </summary>
    public class SplashViewController : BaseViewController
    {
        private UIImageView _logo;
        private UIActivityIndicatorView _spinner;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = Theme.Hex(0xE2E2E2);

            var container = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Center,
                Spacing = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(container);

            _logo = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            var logoImg = LoadLogo();
            if (logoImg != null) _logo.Image = logoImg;

            var title = MakeLabel("توصيلة", 36f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            title.TranslatesAutoresizingMaskIntoConstraints = false;

            var subtitle = MakeLabel("خدمة توصيل سريعة", 16f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            subtitle.TranslatesAutoresizingMaskIntoConstraints = false;

            _spinner = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Medium) { Color = Theme.Primary };
            _spinner.StartAnimating();

            var footer = MakeLabel("Designed @ Ahmed Ali", 10f,
                Theme.TextSecondary.ColorWithAlpha(0.6f), UIFontWeight.Regular, UITextAlignment.Center);

            container.AddArrangedSubview(_logo);
            container.AddArrangedSubview(title);
            container.AddArrangedSubview(subtitle);
            container.AddArrangedSubview(_spinner);
            container.AddArrangedSubview(footer);

            container.SetCustomSpacing(12f, title);
            container.SetCustomSpacing(10f, subtitle);
            container.SetCustomSpacing(28f, _spinner);
            container.SetCustomSpacing(8f, footer);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(container, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(container, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.CenterY, 1, -20),
                NSLayoutConstraint.Create(container, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 32),
                NSLayoutConstraint.Create(container, NSLayoutAttribute.Trailing, NSLayoutRelation.LessThanOrEqual, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -32),

                NSLayoutConstraint.Create(_logo, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 176),
                NSLayoutConstraint.Create(_logo, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 123),
            });
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            StartFlow();
        }

        private UIImage LoadLogo()
        {
            // splash_logo.png من Resources/drawable (مضاف كـ BundleResource في images/)
            var img = UIImage.FromFile("images/splash_logo.png");
            if (img != null) return img;

            // بديل: ارسم أيقونة التطبيق Vector بنفس الحجم
            return IconFactory.Image("ic_logo", 176f);
        }

        private void StartFlow()
        {
            // لا تُكرر التدفق لو عادت الشاشة للمقدمة
            if (_started) return;
            _started = true;

            // طلب إذن الإشعارات (بديل POST_NOTIFICATIONS في أندرويد)
            try { PushService.RequestAuthorizationOnce(); } catch { }

            var prefs = AppServices.Prefs;
            var introSeen = prefs.GetBool("tawsela.intro_seen", false);

            bool restored = false;
            try { restored = Api.RestoreSession(); } catch { }

            // فحص الإصدار في الخلفية (صامت عند الفشل) — كما في أندرويد
            var updateTask = Task.Run(() =>
            {
                try
                {
                    string v, u;
                    Api.GetAppUpdate(out v, out u);
                    return new[] { v ?? "", u ?? "" };
                }
                catch { return null; }
            });

            Task.Delay(1200).ContinueWith(_ =>
            {
                string latestVersion = "", latestUrl = "";
                try
                {
                    if (updateTask.Wait(400) && updateTask.Result != null)
                    {
                        latestVersion = updateTask.Result[0];
                        latestUrl = updateTask.Result[1];
                    }
                }
                catch { }

                Ui.BeginInvokeOnMainThreadSafe(() =>
                {
                    try { _spinner.StopAnimating(); } catch { }

                    if (IsNewer(latestVersion, CurrentVersion()))
                        ShowUpdateDialog(latestVersion, latestUrl, introSeen, restored);
                    else
                        Router.GoAfterSplash(restored, introSeen);
                });
            });
        }

        private bool _started;

        private void ShowUpdateDialog(string latestVersion, string latestUrl, bool introSeen, bool restored)
        {
            var alert = UIAlertController.Create(
                "يتوفر إصدار جديد (" + latestVersion + ")",
                "يوجد تحديث جديد للتطبيق. يمكنك التحديث الآن أو الاستمرار في النسخة الحالية.",
                UIAlertControllerStyle.Alert);

            alert.AddAction(UIAlertAction.Create("تحديث الآن", UIAlertActionStyle.Default, _ =>
            {
                // على iOS لا يجوز توجيه المستخدم إلى Google Play — نفتح App Store فقط
                OpenStore(latestUrl);
                Router.GoAfterSplash(restored, introSeen);
            }));
            alert.AddAction(UIAlertAction.Create("الاستمرار", UIAlertActionStyle.Cancel, _ =>
            {
                Router.GoAfterSplash(restored, introSeen);
            }));

            PresentViewController(alert, true, null);
        }

        private void OpenStore(string latestUrl)
        {
            try
            {
                var link = (latestUrl ?? "").Trim();

                // لو السيرفر أعطى رابط App Store نستخدمه
                if (!string.IsNullOrEmpty(link) &&
                    link.IndexOf("apps.apple.com", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AppServices.OpenUrl(link);
                    return;
                }

                // رقم التطبيق يُضبط بعد إنشائه على App Store Connect
                var appStoreId = AppServices.Prefs.Get("tawsela.app_store_id", "");
                if (!string.IsNullOrEmpty(appStoreId))
                {
                    AppServices.OpenUrl("https://apps.apple.com/app/id" + appStoreId.Trim());
                    return;
                }

                Ui.Alert("تحديث التطبيق", "افتح App Store وحدّث تطبيق توصيلة إلى آخر إصدار.");
            }
            catch { }
        }

        /// <summary>الإصدار الحالي من Info.plist (بديل PackageManager.VersionName).</summary>
        private static string CurrentVersion()
        {
            try
            {
                var v = NSBundle.MainBundle.InfoDictionary.ObjectForKey(new NSString("CFBundleShortVersionString")) as NSString;
                return v == null ? "" : v.ToString();
            }
            catch { return ""; }
        }

        /// <summary>مقارنة نسخ رقمية (1.2.0 مقابل 1.10.0).</summary>
        private static bool IsNewer(string latest, string current)
        {
            if (string.IsNullOrEmpty(latest)) return false;
            try
            {
                var l = latest.Trim().Split('.');
                var c = (current ?? "").Trim().Split('.');
                int n = Math.Max(l.Length, c.Length);
                for (int i = 0; i < n; i++)
                {
                    int lv, cv;
                    if (!(i < l.Length && int.TryParse(l[i], out lv))) lv = 0;
                    if (!(i < c.Length && int.TryParse(c[i], out cv))) cv = 0;
                    if (lv != cv) return lv > cv;
                }
                return false;
            }
            catch { return false; }
        }
    }
}
