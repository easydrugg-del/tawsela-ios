using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// أدوات واجهة مشتركة — بديل Activities/Ui.cs وأدوات أندرويد
    /// (Toast / AlertDialog / ProgressDialog) على iOS.
    /// </summary>
    public static class Ui
    {
        /* ---------- Toast (بديل Toast.MakeText) ---------- */

        private static UIView _toastView;
        private static NSTimer _toastTimer;

        public static void Toast(string message)
        {
            Toast(message, 2.2);
        }

        public static void Toast(string message, double seconds)
        {
            if (string.IsNullOrEmpty(message)) return;

            BeginOnMain(() =>
            {
                try
                {
                    var window = KeyWindow();
                    if (window == null) return;

                    DismissToast();

                    var label = new UILabel
                    {
                        Text = message,
                        TextColor = UIColor.White,
                        Font = Theme.Font(14),
                        TextAlignment = UITextAlignment.Center,
                        Lines = 0,
                        LineBreakMode = UILineBreakMode.WordWrap,
                        TranslatesAutoresizingMaskIntoConstraints = false
                    };

                    var box = new UIView
                    {
                        BackgroundColor = UIColor.Black.ColorWithAlpha(0.82f),
                        TranslatesAutoresizingMaskIntoConstraints = false
                    };
                    box.Layer.CornerRadius = 12f;
                    box.Layer.MasksToBounds = true;
                    box.AddSubview(label);

                    var container = new UIView { TranslatesAutoresizingMaskIntoConstraints = false, UserInteractionEnabled = false };
                    container.AddSubview(box);
                    window.AddSubview(container);

                    window.AddConstraints(new[]
                    {
                        NSLayoutConstraint.Create(container, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, window, NSLayoutAttribute.Leading, 1, 0),
                        NSLayoutConstraint.Create(container, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, window, NSLayoutAttribute.Trailing, 1, 0),
                        NSLayoutConstraint.Create(container, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, window, NSLayoutAttribute.Bottom, 1, -90),
                    });

                    window.AddConstraints(new[]
                    {
                        NSLayoutConstraint.Create(box, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, container, NSLayoutAttribute.CenterX, 1, 0),
                        NSLayoutConstraint.Create(box, NSLayoutAttribute.Top, NSLayoutRelation.GreaterThanOrEqual, container, NSLayoutAttribute.Top, 1, 0),
                        NSLayoutConstraint.Create(box, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, container, NSLayoutAttribute.Bottom, 1, 0),
                        NSLayoutConstraint.Create(box, NSLayoutAttribute.Width, NSLayoutRelation.LessThanOrEqual, container, NSLayoutAttribute.Width, 1, -40),
                    });

                    box.AddConstraints(new[]
                    {
                        NSLayoutConstraint.Create(label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, box, NSLayoutAttribute.Top, 1, 10),
                        NSLayoutConstraint.Create(label, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, box, NSLayoutAttribute.Bottom, 1, -10),
                        NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, box, NSLayoutAttribute.Leading, 1, 16),
                        NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, box, NSLayoutAttribute.Trailing, 1, -16),
                    });

                    _toastView = container;
                    container.Alpha = 0;
                    UIView.Animate(0.18, () => container.Alpha = 1);

                    _toastTimer = NSTimer.CreateScheduledTimer(seconds, _ => DismissToast());
                }
                catch { }
            });
        }

        private static void DismissToast()
        {
            try
            {
                if (_toastTimer != null) { _toastTimer.Invalidate(); _toastTimer.Dispose(); _toastTimer = null; }
                var v = _toastView;
                _toastView = null;
                if (v == null) return;
                UIView.Animate(0.18, () => v.Alpha = 0, () => v.RemoveFromSuperview());
            }
            catch { }
        }

        /* ---------- AlertDialog (بديل AlertDialog.Builder) ---------- */

        public static void Alert(string title, string message, string okText = "موافق", Action onOk = null)
        {
            BeginOnMain(() =>
            {
                var top = TopViewController();
                if (top == null) return;
                var alert = UIAlertController.Create(title ?? "", message ?? "", UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create(okText ?? "موافق", UIAlertActionStyle.Default, _ =>
                {
                    if (onOk != null) { try { onOk(); } catch { } }
                }));
                Present(top, alert);
            });
        }

        public static void Confirm(string title, string message, string yesText, Action onYes, string noText = "إلغاء")
        {
            BeginOnMain(() =>
            {
                var top = TopViewController();
                if (top == null) return;
                var alert = UIAlertController.Create(title ?? "", message ?? "", UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create(noText ?? "إلغاء", UIAlertActionStyle.Cancel, null));
                alert.AddAction(UIAlertAction.Create(yesText ?? "موافق", UIAlertActionStyle.Default, _ =>
                {
                    if (onYes != null) { try { onYes(); } catch { } }
                }));
                Present(top, alert);
            });
        }

        /// <summary>حوار إدخال نصي واحد (بديل AlertDialog + EditText).</summary>
        public static void Prompt(string title, string message, string initial, string placeholder,
            Action<string> onOk, UIKeyboardType keyboard = UIKeyboardType.Default, string okText = "موافق")
        {
            BeginOnMain(() =>
            {
                var top = TopViewController();
                if (top == null) return;
                var alert = UIAlertController.Create(title ?? "", message ?? "", UIAlertControllerStyle.Alert);
                alert.AddTextField(field =>
                {
                    field.Text = initial ?? "";
                    field.Placeholder = placeholder ?? "";
                    field.KeyboardType = keyboard;
                    field.TextAlignment = UITextAlignment.Right;
                });
                alert.AddAction(UIAlertAction.Create("إلغاء", UIAlertActionStyle.Cancel, null));
                alert.AddAction(UIAlertAction.Create(okText ?? "موافق", UIAlertActionStyle.Default, _ =>
                {
                    try
                    {
                        var text = alert.TextFields != null && alert.TextFields.Length > 0
                            ? (alert.TextFields[0].Text ?? "")
                            : "";
                        if (onOk != null) onOk(text);
                    }
                    catch { }
                }));
                Present(top, alert);
            });
        }

        /// <summary>قائمة اختيار (بديل AlertDialog بـ items).</summary>
        public static void Choose(string title, IList<string> items, Action<int> onPick)
        {
            BeginOnMain(() =>
            {
                var top = TopViewController();
                if (top == null || items == null) return;
                var sheet = UIAlertController.Create(title ?? "", null, UIAlertControllerStyle.ActionSheet);
                for (int i = 0; i < items.Count; i++)
                {
                    var index = i;
                    sheet.AddAction(UIAlertAction.Create(items[i] ?? "", UIAlertActionStyle.Default, _ =>
                    {
                        if (onPick != null) { try { onPick(index); } catch { } }
                    }));
                }
                sheet.AddAction(UIAlertAction.Create("إلغاء", UIAlertActionStyle.Cancel, null));

                // على iPad يجب تحديد مصدر العرض
                if (sheet.PopoverPresentationController != null)
                {
                    sheet.PopoverPresentationController.SourceView = top.View;
                    sheet.PopoverPresentationController.SourceRect =
                        new CGRect(top.View.Bounds.GetMidX(), top.View.Bounds.GetMidY(), 1, 1);
                    sheet.PopoverPresentationController.PermittedArrowDirections = 0;
                }
                Present(top, sheet);
            });
        }

        private static void Present(UIViewController top, UIAlertController alert)
        {
            try
            {
                // لا تفتح حوارًا فوق حوار آخر — يسبب تحذير UIKit واختفاء الحوار
                if (top is UIAlertController) return;
                if (top.PresentedViewController is UIAlertController) return;
                top.PresentViewController(alert, true, null);
            }
            catch { }
        }

        /* ---------- ProgressDialog (بديل ProgressDialog المهجور) ---------- */

        public static ProgressOverlay ShowProgress(string message = "جارٍ التحميل…")
        {
            var overlay = new ProgressOverlay(message);
            BeginOnMain(() =>
            {
                var window = KeyWindow();
                if (window != null) overlay.Show(window);
            });
            return overlay;
        }

        /* ---------- أدوات مساعدة ---------- */

        public static void BeginOnMain(Action action)
        {
            if (action == null) return;
            if (NSThread.IsMain)
            {
                action();
                return;
            }
            BeginInvokeOnMainThreadSafe(action);
        }

        public static void BeginInvokeOnMainThreadSafe(Action action)
        {
            try { NSRunLoop.Main.BeginInvokeOnMainThread(action); }
            catch { }
        }

        /// <summary>تنفيذ عمل في الخلفية ثم العودة للواجهة (بديل Task.Run + RunOnUiThread).</summary>
        public static void Run(Action work, Action onDone = null, Action<Exception> onError = null)
        {
            Task.Run(() =>
            {
                try
                {
                    work();
                    if (onDone != null) BeginInvokeOnMainThreadSafe(onDone);
                }
                catch (Exception ex)
                {
                    var handler = onError ?? (e => Toast("خطأ: " + e.Message));
                    BeginInvokeOnMainThreadSafe(() => handler(ex));
                }
            });
        }

        public static UIWindow KeyWindow()
        {
            try
            {
                var app = UIApplication.SharedApplication;
                if (app.Windows != null)
                {
                    foreach (var w in app.Windows)
                        if (w.IsKeyWindow) return w;
                    if (app.Windows.Length > 0) return app.Windows[0];
                }
                foreach (var scene in app.ConnectedScenes.ToArray())
                {
                    var ws = scene as UIWindowScene;
                    if (ws == null) continue;
                    foreach (var w in ws.Windows)
                        if (w.IsKeyWindow) return w;
                }
            }
            catch { }
            return null;
        }

        public static UIViewController TopViewController()
        {
            var window = KeyWindow();
            var root = window == null ? null : window.RootViewController;
            if (root == null) return null;

            var current = root;
            while (true)
            {
                if (current.PresentedViewController != null)
                {
                    current = current.PresentedViewController;
                    continue;
                }
                var nav = current as UINavigationController;
                if (nav != null && nav.VisibleViewController != null)
                {
                    current = nav.VisibleViewController;
                    continue;
                }
                var tab = current as UITabBarController;
                if (tab != null && tab.SelectedViewController != null)
                {
                    current = tab.SelectedViewController;
                    continue;
                }
                break;
            }
            return current;
        }

        /// <summary>تطبيق أيقونة على زر (بديل drawableStart/ImageView).</summary>
        public static void SetButtonIcon(UIButton button, string iconName, float size = 20f, UIColor tint = null)
        {
            var img = IconFactory.Image(iconName, (nfloat)size, tint);
            if (img == null) return;
            button.SetImage(img.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal), UIControlState.Normal);
        }

        /// <summary>شريط فاصل رفيع.</summary>
        public static UIView Separator(UIColor color = null, float height = 1f)
        {
            return new UIView { BackgroundColor = color ?? Theme.CardStroke };
        }
    }

    /// <summary>بديل ProgressDialog: طبقة شفافة مع مؤشر ورسالة، تُغلق بـ Dismiss().</summary>
    public sealed class ProgressOverlay : IDisposable
    {
        private readonly UIView _overlay;
        private readonly UILabel _label;

        public ProgressOverlay(string message)
        {
            _overlay = new UIView { BackgroundColor = UIColor.Black.ColorWithAlpha(0.28f), Alpha = 0 };

            var box = new UIView { BackgroundColor = Theme.White };
            box.Layer.CornerRadius = 14f;
            box.Layer.MasksToBounds = true;
            box.TranslatesAutoresizingMaskIntoConstraints = false;

            var spinner = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Large)
            {
                Color = Theme.Primary,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            spinner.StartAnimating();

            _label = new UILabel
            {
                Text = message ?? "",
                Font = Theme.Font(14),
                TextColor = Theme.TextPrimary,
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            box.AddSubview(spinner);
            box.AddSubview(_label);
            _overlay.AddSubview(box);

            box.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(spinner, NSLayoutAttribute.Top, NSLayoutRelation.Equal, box, NSLayoutAttribute.Top, 1, 18),
                NSLayoutConstraint.Create(spinner, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, box, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, spinner, NSLayoutAttribute.Bottom, 1, 10),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, box, NSLayoutAttribute.Leading, 1, 18),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, box, NSLayoutAttribute.Trailing, 1, -18),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, box, NSLayoutAttribute.Bottom, 1, -16),
            });

            _overlay.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(box, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, _overlay, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(box, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _overlay, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(box, NSLayoutAttribute.Width, NSLayoutRelation.GreaterThanOrEqual, null, NSLayoutAttribute.NoAttribute, 1, 160),
                NSLayoutConstraint.Create(box, NSLayoutAttribute.Width, NSLayoutRelation.LessThanOrEqual, _overlay, NSLayoutAttribute.Width, 1, -80),
            });
        }

        public void SetMessage(string message)
        {
            Ui.BeginOnMain(() => { if (_label != null) _label.Text = message ?? ""; });
        }

        public void Show(UIView host)
        {
            if (host == null) return;
            _overlay.Frame = host.Bounds;
            _overlay.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            host.AddSubview(_overlay);
            UIView.Animate(0.15, () => _overlay.Alpha = 1);
        }

        public void Dismiss()
        {
            Ui.BeginOnMain(() =>
            {
                try
                {
                    UIView.Animate(0.15, () => _overlay.Alpha = 0, () => _overlay.RemoveFromSuperview());
                }
                catch { }
            });
        }

        public void Dispose() { Dismiss(); }
    }
}
