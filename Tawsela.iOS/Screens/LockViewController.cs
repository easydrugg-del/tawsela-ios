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
    /// بديل Activities/LockActivity.cs — قفل التطبيق برقم سري 4-6 أرقام
    /// مع زر Face ID / Touch ID. لا يمكن إغلاقه بسحب (مثل أندرويد).
    /// </summary>
    public class LockViewController : BaseViewController
    {
        private readonly bool _forward;
        private UILabel _pinDots;
        private UILabel _message;
        private UIButton _bioButton;
        private string _entered = "";
        private int _attempts;
        private bool _closing;

        public LockViewController(bool forward) : base()
        {
            _forward = forward;
            ModalInPresentation = true; // يمنع السحب للإغلاق
        }

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.White;
            BuildUi();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            TryBiometric();
        }

        private void BuildUi()
        {
            var stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Fill,
                Spacing = 14,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(stack);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 28),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -28),
            });

            var icon = new UIImageView
            {
                Image = IconFactory.Image("ic_shield", 64f, Theme.Primary),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            icon.HeightAnchor.ConstraintEqualTo(72).Active = true;

            var title = MakeLabel("التطبيق مقفل", 22f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Center);
            var hint = MakeLabel("أدخل الرقم السري للمتابعة", 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);

            _pinDots = MakeLabel("", 26f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _pinDots.Text = Dots(0);

            _message = MakeLabel("", 13f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Center);
            _message.Hidden = true;

            var keypad = BuildKeypad();

            _bioButton = Controls.OutlineButton("فتح بالبصمة");
            _bioButton.TouchUpInside += (s, e) => TryBiometric();

            stack.AddArrangedSubview(icon);
            stack.AddArrangedSubview(title);
            stack.AddArrangedSubview(hint);
            stack.AddArrangedSubview(_pinDots);
            stack.AddArrangedSubview(_message);
            stack.AddArrangedSubview(keypad);
            stack.AddArrangedSubview(_bioButton);

            stack.SetCustomSpacing(20f, _pinDots);

            string kind;
            if (!Biometrics.IsAvailable(out kind)) _bioButton.Hidden = true;
            else _bioButton.SetTitle("فتح بـ " + kind, UIControlState.Normal);
        }

        private UIView BuildKeypad()
        {
            var grid = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var rows = new[]
            {
                new[] { "1", "2", "3" },
                new[] { "4", "5", "6" },
                new[] { "7", "8", "9" },
                new[] { "⌫", "0", "OK" }
            };

            foreach (var row in rows)
            {
                var line = new UIStackView
                {
                    Axis = UILayoutConstraintAxis.Horizontal,
                    Spacing = 10,
                    Distribution = UIStackViewDistribution.FillEqually,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };

                foreach (var key in row)
                {
                    var label = key;
                    var b = new UIButton(UIButtonType.System);
                    b.SetTitle(label == "OK" ? "تأكيد" : label, UIControlState.Normal);
                    b.SetTitleColor(label == "OK" ? UIColor.White : Theme.TextPrimary, UIControlState.Normal);
                    b.TitleLabel.Font = Theme.FontSemiBold(label == "OK" ? 15 : 22);
                    b.BackgroundColor = label == "OK" ? Theme.Primary : Theme.GrayLight;
                    b.Layer.CornerRadius = 12f;
                    b.Layer.MasksToBounds = true;
                    b.TranslatesAutoresizingMaskIntoConstraints = false;
                    b.HeightAnchor.ConstraintEqualTo(54).Active = true;
                    b.TouchUpInside += (s, e) => OnKey(label);
                    line.AddArrangedSubview(b);
                }

                grid.AddArrangedSubview(line);
            }
            return grid;
        }

        private void OnKey(string key)
        {
            if (_closing) return;

            if (key == "⌫")
            {
                if (_entered.Length > 0) _entered = _entered.Substring(0, _entered.Length - 1);
                _pinDots.Text = Dots(_entered.Length);
                return;
            }

            if (key == "OK")
            {
                Verify();
                return;
            }

            if (_entered.Length >= 6) return;
            _entered += key;
            _pinDots.Text = Dots(_entered.Length);

            // تحقق تلقائي عند 4 أرقام إذا كان الرقم الصحيح 4
            if (_entered.Length >= 4) TryAutoVerify();
        }

        private void TryAutoVerify()
        {
            // لا نتحقق تلقائيًا إلا بعد 4 أرقام (الحد الأدنى) وما لم يزد المستخدم
            if (_entered.Length != 4) return;
            if (!AppLock.HasPin()) return;

            // لتفادي قفل المستخدم برقم 5-6، نتحقق فقط إذا كان صحيحًا
            if (AppLock.Verify(_entered)) { Unlock(); }
        }

        private void Verify()
        {
            if (string.IsNullOrEmpty(_entered))
            {
                ShowMessage("أدخل الرقم السري");
                return;
            }

            if (AppLock.Verify(_entered))
            {
                Unlock();
                return;
            }

            _attempts++;
            _entered = "";
            _pinDots.Text = Dots(0);

            if (_attempts >= AppLock.MaxAttempts)
            {
                // خروج كامل بعد تجاوز المحاولات
                ShowMessage("محاولات كثيرة — سجّل الدخول من جديد");
                AppLock.ClearBackground();
                try { Api.ClearSession(); } catch { }
                NSTimer.CreateScheduledTimer(1.0, _ =>
                {
                    Close();
                    Router.ShowLogin();
                });
                return;
            }

            ShowMessage("رقم سري غير صحيح (بقي " + (AppLock.MaxAttempts - _attempts) + ")");
        }

        private void TryBiometric()
        {
            if (_closing) return;
            string kind;
            if (!Biometrics.IsAvailable(out kind)) return;

            Biometrics.Authenticate("افتح تطبيق توصيلة", (ok, msg) =>
            {
                if (ok) Unlock();
                else if (!string.IsNullOrEmpty(msg) && msg != "تم الإلغاء") ShowMessage(msg);
            });
        }

        private void Unlock()
        {
            if (_closing) return;
            _closing = true;
            AppLock.ClearBackground();
            Close();

            if (_forward || Session.UserId <= 0) Router.GoHome();
        }

        private void Close()
        {
            Router.LockDismissed();
            if (PresentingViewController != null) DismissViewController(true, null);
        }

        private void ShowMessage(string text)
        {
            _message.Text = text ?? "";
            _message.Hidden = string.IsNullOrEmpty(text);
        }

        private static string Dots(int count)
        {
            var s = "";
            for (int i = 0; i < count; i++) s += "● ";
            return s.TrimEnd();
        }
    }
}
