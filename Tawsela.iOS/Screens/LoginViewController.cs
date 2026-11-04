using System;
using CoreGraphics;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/LoginActivity.cs — نفس السلوك:
    /// تذكر المستخدم، إظهار كلمة المرور، Face ID/Touch ID، حماية من المحاولات المتكررة،
    /// وروابط المستندات القانونية.
    /// </summary>
    public class LoginViewController : BaseViewController
    {
        private UITextField _username;
        private UITextField _password;
        private UILabel _message;
        private UIButton _btnLogin;
        private UIButton _btnFingerprint;
        private UISwitch _swShowPass;
        private UISwitch _swRemember;

        private const string PrefFailCount = "tawsela.fail_count";
        private const string PrefFailTime = "tawsela.fail_time";
        private const int MaxAttempts = 5;
        private const int BlockMinutes = 5;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public LoginViewController() : base()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadSavedUser();
            ApplyLoginBlock();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            // فتح البصمة تلقائيًا بعد لحظة (كما في أندرويد: PostDelayed 800ms)
            if (_btnFingerprint != null && !_btnFingerprint.Hidden && !_autoBioDone)
            {
                _autoBioDone = true;
                NSTimer.CreateScheduledTimer(0.8, _ => AuthenticateWithBiometric());
            }
        }

        private bool _autoBioDone;

        private void BuildUi()
        {
            var stack = CreateScrollContent(14f, 20f);

            var logo = new UIImageView
            {
                Image = IconFactory.Image("ic_logo", 72f),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(logo);
            stack.SetCustomSpacing(8f, logo);

            var title = MakeLabel("توصيلة", 28f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            stack.AddArrangedSubview(title);

            var subtitle = MakeLabel("خدمة توصيل سريعة", 15f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            stack.AddArrangedSubview(subtitle);
            stack.SetCustomSpacing(26f, subtitle);

            var heading = MakeLabel("تسجيل الدخول", 18f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            stack.AddArrangedSubview(heading);

            _username = Controls.TextField("اسم المستخدم أو رقم الهاتف");
            _username.AutocapitalizationType = UITextAutocapitalizationType.None;
            _username.KeyboardType = UIKeyboardType.Default;
            stack.AddArrangedSubview(_username);

            _password = Controls.TextField("كلمة المرور", true);
            stack.AddArrangedSubview(_password);

            // إظهار كلمة المرور
            var showRow = MakeSwitchRow("إظهار كلمة المرور", out _swShowPass);
            _swShowPass.ValueChanged += (s, e) => _password.SecureTextEntry = !_swShowPass.On;
            stack.AddArrangedSubview(showRow);

            // تذكر اسم المستخدم
            var rememberRow = MakeSwitchRow("تذكرني", out _swRemember, true);
            stack.AddArrangedSubview(rememberRow);

            _message = MakeLabel("", 14f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Right);
            _message.Hidden = true;
            stack.AddArrangedSubview(_message);

            _btnLogin = Controls.PrimaryButton("دخول");
            _btnLogin.TouchUpInside += (s, e) => DoLogin();
            stack.AddArrangedSubview(_btnLogin);

            var btnRegister = Controls.OutlineButton("إنشاء حساب جديد");
            btnRegister.TouchUpInside += (s, e) => Push(new RegisterViewController());
            stack.AddArrangedSubview(btnRegister);

            // زر البصمة — يظهر فقط إذا كان الجهاز يدعمها
            _btnFingerprint = Controls.IconButton("تسجيل بالبصمة", "ic_shield", Theme.White, Theme.Primary);
            _btnFingerprint.Layer.BorderWidth = 1f;
            _btnFingerprint.Layer.BorderColor = Theme.Primary.CGColor;
            _btnFingerprint.TouchUpInside += (s, e) => AuthenticateWithBiometric();
            _btnFingerprint.Hidden = true;
            stack.AddArrangedSubview(_btnFingerprint);

            var legal = new UIButton(UIButtonType.System);
            legal.SetTitle("سياسة الخصوصية · شروط الاستخدام", UIControlState.Normal);
            legal.SetTitleColor(Theme.TextSecondary, UIControlState.Normal);
            legal.TitleLabel.Font = Theme.Font(13);
            legal.TranslatesAutoresizingMaskIntoConstraints = false;
            legal.TouchUpInside += (s, e) => ShowLegal();
            stack.AddArrangedSubview(legal);

            var footer = MakeLabel("Designed @ Ahmed Ali", 10f,
                Theme.TextSecondary.ColorWithAlpha(0.6f), UIFontWeight.Regular, UITextAlignment.Center);
            stack.AddArrangedSubview(footer);

            // البصمة متاحة؟
            string kind;
            if (Biometrics.IsAvailable(out kind) && AppServices.Prefs.Get("tawsela.remember_user", "") != ""
                && Api.HasValidSession())
            {
                _btnFingerprint.SetTitle("تسجيل بـ " + kind, UIControlState.Normal);
                _btnFingerprint.Hidden = false;
            }
        }

        private UIView MakeSwitchRow(string text, out UISwitch toggle, bool rightAligned = false)
        {
            var row = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            var label = MakeLabel(text, 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            toggle = new UISwitch
            {
                OnTintColor = Theme.Primary,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            row.AddSubview(label);
            row.AddSubview(toggle);
            row.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(label, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, toggle, NSLayoutAttribute.Leading, 1, -10),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, row, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(toggle, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),
            });

            // في RTL: المفتاح على اليسار والنص على اليمين
            row.AddConstraint(NSLayoutConstraint.Create(toggle, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, row, NSLayoutAttribute.Leading, 1, 0));

            return row;
        }

        /* ---------- بيانات محفوظة ---------- */

        private void LoadSavedUser()
        {
            var prefs = AppServices.Prefs;

            // ترحيل لمرة واحدة: حذف أي كلمة مرور محفوظة قديمة
            if (!prefs.GetBool("tawsela.remember_plain", false))
            {
                prefs.Remove("tawsela.remember_user");
                prefs.Remove("tawsela.remember_pass");
                prefs.SetBool("tawsela.remember_plain", true);
            }

            var savedUser = prefs.Get("tawsela.remember_user", "");
            if (!string.IsNullOrEmpty(savedUser))
            {
                _username.Text = savedUser;
                _swRemember.On = true;
            }
        }

        /* ---------- الدخول ---------- */

        private void DoLogin()
        {
            if (!Net.IsOnline())
            {
                ShowMessage("لا يوجد اتصال بالإنترنت");
                return;
            }

            if (IsBlocked())
            {
                ShowMessage("محظور مؤقتاً، حاول بعد " + RemainingMinutes() + " دقيقة");
                return;
            }

            var user = (_username.Text ?? "").Trim();
            var pass = _password.Text ?? "";

            if (user == "")
            {
                ShowMessage("أدخل اسم المستخدم أو رقم الهاتف");
                return;
            }
            if (pass == "")
            {
                ShowMessage("أدخل كلمة المرور");
                return;
            }

            _btnLogin.Enabled = false;
            ShowMessage("");

            var progress = Ui.ShowProgress("جارٍ تسجيل الدخول…");

            Ui.Run(
                () => Api.Login(user, pass),
                () =>
                {
                    progress.Dismiss();
                    ResetFailCount();

                    var prefs = AppServices.Prefs;
                    if (_swRemember.On) prefs.Set("tawsela.remember_user", user);
                    else prefs.Remove("tawsela.remember_user");
                    prefs.Remove("tawsela.remember_pass");

                    // تسجيل توكن الإشعارات للخادم
                    try { PushService.RegisterCurrentToken(); } catch { }

                    // قفل PIN إن كان مفعلًا، وإلا الشاشة الرئيسية حسب الدور
                    if (AppLock.IsActive()) Router.ShowLock(true);
                    else Router.GoHome();

                    _btnLogin.Enabled = true;
                },
                ex =>
                {
                    progress.Dismiss();
                    RegisterFail();

                    var msg = ex == null ? "خطأ غير معروف" : ex.Message;
                    var inner = ex == null ? null : ex.InnerException;
                    while (inner != null)
                    {
                        msg += " ← " + inner.Message;
                        inner = inner.InnerException;
                    }

                    if (IsBlocked())
                        ShowMessage("محاولات كثيرة فاشلة، محظور " + BlockMinutes + " دقائق");
                    else
                    {
                        var left = Math.Max(0, MaxAttempts - AppServices.Prefs.GetLong(PrefFailCount, 0));
                        ShowMessage("خطأ: " + msg + (left > 0 ? " (بقي " + left + " محاولات)" : ""));
                    }

                    _btnLogin.Enabled = true;
                });
        }

        private void ShowMessage(string text)
        {
            _message.Text = text ?? "";
            _message.Hidden = string.IsNullOrEmpty(text);
        }

        /* ---------- البصمة ---------- */

        private void AuthenticateWithBiometric()
        {
            Biometrics.Authenticate("تسجيل الدخول إلى توصيلة", (success, message) =>
            {
                if (!success)
                {
                    if (!string.IsNullOrEmpty(message) && message != "تم الإلغاء")
                        Ui.Toast(message);
                    return;
                }

                // فتح الجلسة المحفوظة (نفس Api.RestoreSession في أندرويد)
                var ok = false;
                try { ok = Api.RestoreSession(); } catch { }

                if (ok)
                {
                    ResetFailCount();
                    try { PushService.RegisterCurrentToken(); } catch { }
                    Router.GoHome();
                }
                else
                {
                    ShowMessage("انتهت صلاحية الجلسة، سجّل الدخول بكلمة المرور");
                }
            });
        }

        /* ---------- الحماية من المحاولات المتكررة ---------- */

        private bool IsBlocked()
        {
            var prefs = AppServices.Prefs;
            var count = prefs.GetLong(PrefFailCount, 0);
            var failTime = prefs.GetLong(PrefFailTime, 0);
            if (count >= MaxAttempts)
            {
                if (DateTime.UtcNow.Ticks - failTime > TimeSpan.FromMinutes(BlockMinutes).Ticks)
                {
                    ResetFailCount();
                    return false;
                }
                return true;
            }
            return false;
        }

        private int RemainingMinutes()
        {
            var failTime = AppServices.Prefs.GetLong(PrefFailTime, 0);
            var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - failTime);
            var left = TimeSpan.FromMinutes(BlockMinutes) - elapsed;
            return left.TotalMinutes > 0 ? (int)Math.Ceiling(left.TotalMinutes) : 0;
        }

        private void RegisterFail()
        {
            var prefs = AppServices.Prefs;
            var count = prefs.GetLong(PrefFailCount, 0) + 1;
            if (count >= MaxAttempts) prefs.SetLong(PrefFailTime, DateTime.UtcNow.Ticks);
            prefs.SetLong(PrefFailCount, count);
        }

        private void ResetFailCount()
        {
            var prefs = AppServices.Prefs;
            prefs.SetLong(PrefFailCount, 0);
            prefs.SetLong(PrefFailTime, 0);
        }

        private void ApplyLoginBlock()
        {
            if (IsBlocked())
            {
                _btnLogin.Enabled = false;
                ShowMessage("محظور مؤقتاً، حاول بعد " + RemainingMinutes() + " دقيقة");
            }
        }

        /* ---------- المستندات القانونية ---------- */

        private void ShowLegal()
        {
            Ui.Choose("المستندات القانونية",
                new[] { "سياسة الخصوصية", "شروط الاستخدام" },
                index =>
                {
                    Legal.Open(index == 0 ? Legal.PrivacyUrl : Legal.TermsUrl,
                        err => Ui.Toast(err));
                });
        }
    }
}
