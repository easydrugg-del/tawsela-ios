using System;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/SettingsActivity.cs — إعدادات المستخدم:
    /// كتم الإشعارات، حالة إذن الإشعارات، المستندات القانونية، والإصدار.
    /// </summary>
    public class SettingsViewController : BaseViewController
    {
        private const string PrefName = "tawsela";
        private const string KeySilent = "silent";

        private UISwitch _silent;
        private UILabel _permissionState;

        public SettingsViewController() : base("الإعدادات") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            RefreshPermission();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(Controls.SectionTitle("الإشعارات"));

            var silentRow = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            var silentLabel = MakeLabel("كتم صوت الإشعارات", 15f, Theme.TextPrimary,
                UIFontWeight.Regular, UITextAlignment.Right);
            _silent = new UISwitch
            {
                OnTintColor = Theme.Primary,
                TranslatesAutoresizingMaskIntoConstraints = false,
                On = AppServices.Prefs.GetBool(PrefName + "." + KeySilent, false)
            };
            _silent.ValueChanged += (s, e) =>
            {
                AppServices.Prefs.SetBool(PrefName + "." + KeySilent, _silent.On);
                Ui.Toast(_silent.On ? "تم كتم صوت الإشعارات" : "تم تشغيل صوت الإشعارات");
            };

            silentRow.AddSubview(silentLabel);
            silentRow.AddSubview(_silent);
            silentRow.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_silent, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, silentRow, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_silent, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, silentRow, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(silentLabel, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, silentRow, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(silentLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _silent, NSLayoutAttribute.Trailing, 1, 10),
                NSLayoutConstraint.Create(silentLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, silentRow, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(silentRow, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
            });
            stack.AddArrangedSubview(silentRow);

            _permissionState = MakeLabel("", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_permissionState);

            var openSettings = Controls.OutlineButton("إعدادات إشعارات النظام");
            openSettings.TouchUpInside += (s, e) => OpenSystemSettings();
            stack.AddArrangedSubview(openSettings);

            var requestPermission = Controls.OutlineButton("السماح بالإشعارات");
            requestPermission.TouchUpInside += (s, e) =>
            {
                PushService.RequestAuthorization(granted =>
                {
                    Ui.Toast(granted ? "تم السماح بالإشعارات" : "لم يتم السماح بالإشعارات");
                    RefreshPermission();
                });
            };
            stack.AddArrangedSubview(requestPermission);

            stack.AddArrangedSubview(Controls.SectionTitle("قانوني"));

            var privacy = Controls.OutlineButton("سياسة الخصوصية");
            privacy.TouchUpInside += (s, e) => Legal.Open(Legal.PrivacyUrl, err => Ui.Toast(err));
            stack.AddArrangedSubview(privacy);

            var terms = Controls.OutlineButton("شروط الاستخدام");
            terms.TouchUpInside += (s, e) => Legal.Open(Legal.TermsUrl, err => Ui.Toast(err));
            stack.AddArrangedSubview(terms);

            stack.AddArrangedSubview(Controls.SectionTitle("عن التطبيق"));

            stack.AddArrangedSubview(Controls.InfoRow("الإصدار", CurrentVersion()));
            stack.AddArrangedSubview(Controls.InfoRow("المنصة", "iOS " + UIDevice.CurrentDevice.SystemVersion));

            var about = MakeLabel("توصيلة — خدمة توصيل داخل المدن. تصميم وتطوير: Ahmed Ali.",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            stack.AddArrangedSubview(about);
        }

        private void RefreshPermission()
        {
            PushService.GetSettings(allowed =>
            {
                _permissionState.Text = allowed
                    ? "الإشعارات مسموحة من النظام"
                    : "الإشعارات غير مسموحة — لن تصلك تنبيهات الطلبات";
                _permissionState.TextColor = allowed ? Theme.Green : Theme.Red;
            });
        }

        private void OpenSystemSettings()
        {
            try
            {
                var url = new NSUrl(UIApplication.OpenSettingsUrlString);
                UIApplication.SharedApplication.OpenUrl(url, new NSDictionary(), null);
            }
            catch { }
        }

        private static string CurrentVersion()
        {
            try
            {
                var v = NSBundle.MainBundle.InfoDictionary.ObjectForKey(
                    new NSString("CFBundleShortVersionString")) as NSString;
                var b = NSBundle.MainBundle.InfoDictionary.ObjectForKey(
                    new NSString("CFBundleVersion")) as NSString;
                if (v == null) return "—";
                return v.ToString() + (b == null ? "" : " (" + b.ToString() + ")");
            }
            catch { return "—"; }
        }
    }
}
