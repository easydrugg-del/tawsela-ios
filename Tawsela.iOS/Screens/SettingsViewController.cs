using System;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/SettingsActivity.cs بنفس ترتيب ونصوص أندرويد:
    /// الوضع الليلي، اللغة، مفتاح FCM (للمدير فقط)، الصامت، البطارية،
    /// خدمات الإشعارات، قفل التطبيق، ثم القانوني والخصوصية.
    /// </summary>
    public class SettingsViewController : BaseViewController
    {
        private const string PrefName = "tawsela";
        private const string KeySilent = "silent";
        private const string KeyBattery = "battery_saver";
        private const string KeyDark = "dark";
        private const string KeyLang = "lang";

        private UISwitch _dark, _silent, _battery, _google, _appLock;
        private UILabel _googleState;
        private UIButton _btnLang;

        public SettingsViewController() : base("الإعدادات") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            RefreshGoogleState();
            RefreshLockState();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            /* ---------- الوضع الليلي ---------- */
            _dark = NewSwitch(AppServices.Prefs.GetBool(PrefName + "." + KeyDark, false));
            _dark.ValueChanged += (s, e) =>
            {
                AppServices.Prefs.SetBool(PrefName + "." + KeyDark, _dark.On);
                ApplyDark(_dark.On);
                Ui.Toast(_dark.On ? "تم تفعيل الوضع الليلي" : "تم إيقاف الوضع الليلي");
            };
            stack.AddArrangedSubview(Row("الوضع الليلي", _dark));

            /* ---------- اللغة ---------- */
            _btnLang = Controls.OutlineButton("اللغة: " +
                (AppServices.Prefs.Get(PrefName + "." + KeyLang, "ar") == "en" ? "English" : "العربية"));
            _btnLang.TouchUpInside += (s, e) =>
            {
                Ui.Choose("اللغة", new[] { "العربية", "English" }, index =>
                {
                    var code = index == 1 ? "en" : "ar";
                    AppServices.Prefs.Set(PrefName + "." + KeyLang, code);
                    _btnLang.SetTitle("اللغة: " + (index == 1 ? "English" : "العربية"), UIControlState.Normal);
                    Ui.Toast(index == 1 ? "تم اختيار English" : "تم اختيار العربية");
                });
            };
            stack.AddArrangedSubview(_btnLang);

            /* ---------- مفتاح FCM (للمدير فقط، زي أندرويد) ---------- */
            if (Session.IsAdmin)
            {
                stack.AddArrangedSubview(MakeLabel("مفتاح خادم FCM (للدفع الحقيقي عبر الإنترنت)",
                    13f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right));

                var fcm = Controls.TextField("الصق مفتاح الخادم من Firebase");
                stack.AddArrangedSubview(fcm);

                var saveFcm = Controls.PrimaryButton("حفظ المفتاح");
                saveFcm.TouchUpInside += (s, e) =>
                {
                    var key = (fcm.Text ?? "").Trim();
                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                    Ui.Run(() => Api.AdminSaveSettings(
                        new System.Collections.Generic.Dictionary<string, string> { { "FcmServerKey", key } }), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم حفظ المفتاح");
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                };
                stack.AddArrangedSubview(saveFcm);
            }

            /* ---------- الوضع الصامت / البطارية ---------- */
            _silent = NewSwitch(AppServices.Prefs.GetBool(PrefName + "." + KeySilent, false));
            _silent.ValueChanged += (s, e) =>
            {
                AppServices.Prefs.SetBool(PrefName + "." + KeySilent, _silent.On);
                Ui.Toast(_silent.On ? "تم تفعيل الوضع الصامت" : "تم إيقاف الوضع الصامت");
            };
            stack.AddArrangedSubview(Row("الوضع الصامت للسائق", _silent));

            _battery = NewSwitch(AppServices.Prefs.GetBool(PrefName + "." + KeyBattery, false));
            _battery.ValueChanged += (s, e) =>
            {
                AppServices.Prefs.SetBool(PrefName + "." + KeyBattery, _battery.On);
                Ui.Toast(_battery.On ? "تم تفعيل توفير البطارية" : "تم إيقاف توفير البطارية");
            };
            stack.AddArrangedSubview(Row("توفير البطارية للسائق", _battery));

            /* ---------- خدمات الإشعارات (بديل خدمات جوجل في أندرويد) ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("خدمات جوجل (الإشعارات)"));
            stack.AddArrangedSubview(MakeLabel(
                "الإشعارات على iOS تعمل عبر APNs. فعّلها من إعدادات النظام أو من الزر بالأسفل.",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            _google = NewSwitch(AppServices.Prefs.GetBool(PrefName + ".notif_allowed", false));
            _google.ValueChanged += (s, e) =>
            {
                if (_google.On)
                {
                    PushService.RequestAuthorization(granted =>
                    {
                        AppServices.Prefs.SetBool(PrefName + ".notif_allowed", granted);
                        Ui.Toast(granted ? "تم تفعيل الإشعارات" : "لم يتم السماح بالإشعارات");
                        RefreshGoogleState();
                    });
                    return;
                }
                OpenSystemSettings();
                RefreshGoogleState();
            };
            stack.AddArrangedSubview(Row("تفعيل إشعارات النظام", _google));

            _googleState = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_googleState);

            var openSettings = Controls.OutlineButton("إعدادات إشعارات النظام");
            openSettings.TouchUpInside += (s, e) => OpenSystemSettings();
            stack.AddArrangedSubview(openSettings);

            /* ---------- قفل التطبيق برقم سري ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("قفل التطبيق برقم سري"));
            stack.AddArrangedSubview(MakeLabel("تفعيل القفل (يُطلب بعد الدخول وعند العودة)",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            _appLock = NewSwitch(AppLock.IsActive());
            _appLock.ValueChanged += (s, e) =>
            {
                if (_appLock.On && !AppLock.HasPin())
                {
                    SetPin();
                    _appLock.On = AppLock.IsActive();
                    return;
                }
                AppLock.SetEnabled(_appLock.On);
                Ui.Toast(_appLock.On ? "تم تفعيل قفل التطبيق" : "تم إيقاف قفل التطبيق");
                RefreshLockState();
            };
            stack.AddArrangedSubview(Row("قفل التطبيق", _appLock));

            var changePin = Controls.OutlineButton("تعيين / تغيير الرقم السري");
            changePin.TouchUpInside += (s, e) => SetPin();
            stack.AddArrangedSubview(changePin);

            /* ---------- القانوني والخصوصية ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("القانوني والخصوصية"));

            var privacy = Controls.OutlineButton("سياسة الخصوصية");
            privacy.TouchUpInside += (s, e) => Legal.Open(Legal.PrivacyUrl, err => Ui.Toast(err));
            stack.AddArrangedSubview(privacy);

            var terms = Controls.OutlineButton("شروط الاستخدام");
            terms.TouchUpInside += (s, e) => Legal.Open(Legal.TermsUrl, err => Ui.Toast(err));
            stack.AddArrangedSubview(terms);

            var deleteWeb = Controls.OutlineButton("طلب حذف الحساب عبر الويب");
            deleteWeb.TouchUpInside += (s, e) => Legal.Open(Legal.DeleteAccountUrl, err => Ui.Toast(err));
            stack.AddArrangedSubview(deleteWeb);

            stack.AddArrangedSubview(MakeLabel(
                "ملاحظة: تبديل اللغة يطبّق الإعدادات العامة؛ الترجمة الكاملة للنصوص تتطلب موارد نصوص (strings).",
                11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
        }

        private static UISwitch NewSwitch(bool on)
        {
            return new UISwitch
            {
                OnTintColor = Theme.Primary,
                TranslatesAutoresizingMaskIntoConstraints = false,
                On = on
            };
        }

        /// <summary>صف عنوان + مفتاح (بديل Switch في AXML).</summary>
        private static UIView Row(string title, UISwitch toggle)
        {
            var row = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            var label = new UILabel
            {
                Text = title,
                Font = Theme.FontMedium(15),
                TextColor = Theme.TextPrimary,
                TextAlignment = UITextAlignment.Right,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            row.AddSubview(label);
            row.AddSubview(toggle);
            row.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(toggle, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, row, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(toggle, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, toggle, NSLayoutAttribute.Trailing, 1, 10),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, row, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 44),
            });
            return row;
        }

        private void ApplyDark(bool dark)
        {
            try
            {
                var app = UIApplication.SharedApplication;
                var window = app.KeyWindow;
                if (window == null && app.Windows != null && app.Windows.Length > 0) window = app.Windows[0];
                if (window != null)
                    window.OverrideUserInterfaceStyle = dark ? UIUserInterfaceStyle.Dark : UIUserInterfaceStyle.Light;
            }
            catch { }
        }

        private void RefreshGoogleState()
        {
            PushService.GetSettings(allowed =>
            {
                _google.On = allowed;
                AppServices.Prefs.SetBool(PrefName + ".notif_allowed", allowed);
                _googleState.Text = allowed
                    ? "الإشعارات مسموحة من النظام"
                    : "الإشعارات غير مسموحة — لن تصلك تنبيهات الطلبات";
                _googleState.TextColor = allowed ? Theme.Green : Theme.Red;
            });
        }

        private void RefreshLockState()
        {
            _appLock.On = AppLock.IsActive();
        }

        private void SetPin()
        {
            Ui.Prompt("رقم سري جديد", "من 4 إلى 6 أرقام", "", "••••", pin =>
            {
                if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 6)
                {
                    Ui.Toast("الرقم من 4 إلى 6 أرقام");
                    return;
                }

                Ui.Prompt("تأكيد الرقم السري", "أعد إدخال الرقم", "", "••••", again =>
                {
                    if (pin != again)
                    {
                        Ui.Toast("الرقمان غير متطابقين");
                        return;
                    }

                    try
                    {
                        AppLock.SetPin(pin);
                        Ui.Toast("تم حفظ الرقم السري");
                        RefreshLockState();
                    }
                    catch (Exception ex) { Ui.Toast(ex.Message); }
                }, UIKeyboardType.NumberPad);
            }, UIKeyboardType.NumberPad);
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
    }
}
