using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// قاعدة كل شاشات iOS:
    /// - تحترم Safe Area (النوتش وشريط الهوم) فلا يختفي زر أو عنوان.
    /// - ترفع المحتوى فوق الكيبورد تلقائيًا (شاشات فيها حقول كثيرة مثل شاشة الطلب).
    /// - تثبّت الاتجاه من اليمين لليسار.
    /// </summary>
    public class BaseViewController : UIViewController
    {
        private NSObject _keyboardShow;
        private NSObject _keyboardHide;
        private UIScrollView _scrollForKeyboard;
        private nfloat _scrollInsetBase;

        protected BaseViewController()
        {
            // التطبيق عربي: الاتجاه ثابت لكل شاشة
            View.SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft;
        }

        protected BaseViewController(string title) : this()
        {
            Title = title;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.Surface;
            EdgesForExtendedLayout = UIRectEdge.None;
            ExtendedLayoutIncludesOpaqueBars = false;
            if (NavigationController != null)
            {
                NavigationItem.BackButtonTitle = "";
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            if (NavigationController != null)
            {
                NavigationController.SetNavigationBarHidden(ShowNavigationBar == false, animated);
            }
            RegisterKeyboard();

            if (FlatHeader && !_flatApplied)
            {
                _flatApplied = true;
                ApplyFlatHeader();
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            UnregisterKeyboard();
        }

        /// <summary>الشاشات ذات الهيدر المخصص تُخفي شريط التنقل.</summary>
        public virtual bool ShowNavigationBar
        {
            get { return true; }
        }

        /// <summary>شكل أندرويد: عنوان برتقالي على خلفية فاتحة بدل شريط التنقل.</summary>
        protected virtual bool FlatHeader
        {
            get { return false; }
        }

        private bool _flatApplied;

        /// <summary>شريط عنوان مخصص: عنوان برتقالي + زر رجوع، ويزيح المحتوى تحته.</summary>
        private void ApplyFlatHeader()
        {
            var bar = new UIView
            {
                BackgroundColor = Theme.White,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(bar);

            var label = MakeLabel(Title ?? "", 20f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            var back = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            back.SetTitle("‹", UIControlState.Normal);
            back.SetTitleColor(Theme.Primary, UIControlState.Normal);
            back.TitleLabel.Font = Theme.FontBold(30);
            back.TouchUpInside += (s, e) =>
            {
                if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                    NavigationController.PopViewController(true);
            };

            bar.AddSubview(label);
            bar.AddSubview(back);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(bar, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(bar, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(bar, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(bar, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 56),
            });

            bar.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, bar, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, bar, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, bar, NSLayoutAttribute.Leading, 1, 56),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, bar, NSLayoutAttribute.Trailing, 1, -16),

                NSLayoutConstraint.Create(back, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, bar, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(back, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, bar, NSLayoutAttribute.Leading, 1, 8),
                NSLayoutConstraint.Create(back, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
                NSLayoutConstraint.Create(back, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
            });

            // إزاحة المحتوى الممرَّر من أسفل الشريط
            foreach (var c in View.Constraints)
            {
                if (c.FirstItem is UIScrollView && c.FirstAttribute == NSLayoutAttribute.Top)
                    c.Constant = 56;
            }

            View.BringSubviewToFront(bar);
        }

        /* ---------- Keyboard ---------- */

        private void RegisterKeyboard()
        {
            if (_keyboardShow != null) return;
            _keyboardShow = UIKeyboard.Notifications.ObserveWillChangeFrame(OnKeyboardChanged);
            _keyboardHide = UIKeyboard.Notifications.ObserveWillHide(OnKeyboardHidden);
        }

        private void UnregisterKeyboard()
        {
            if (_keyboardShow != null) { NSNotificationCenter.DefaultCenter.RemoveObserver(_keyboardShow); _keyboardShow.Dispose(); _keyboardShow = null; }
            if (_keyboardHide != null) { NSNotificationCenter.DefaultCenter.RemoveObserver(_keyboardHide); _keyboardHide.Dispose(); _keyboardHide = null; }
        }

        /// <summary>سجّل UIScrollView ليتم رفع محتواه فوق الكيبورد.</summary>
        protected void AttachKeyboardScroll(UIScrollView scroll)
        {
            _scrollForKeyboard = scroll;
            _scrollInsetBase = scroll == null ? 0 : scroll.ContentInset.Bottom;
        }

        private void OnKeyboardChanged(object sender, UIKeyboardEventArgs e)
        {
            if (_scrollForKeyboard == null) return;
            var frame = e.FrameEnd;
            var overlap = View.Bounds.Height - frame.Y;
            nfloat inset = (nfloat)Math.Max(0, (double)overlap);
            var content = _scrollForKeyboard.ContentInset;
            content.Bottom = _scrollInsetBase + inset;
            _scrollForKeyboard.ContentInset = content;
            _scrollForKeyboard.ScrollIndicatorInsets = content;
        }

        private void OnKeyboardHidden(object sender, UIKeyboardEventArgs e)
        {
            if (_scrollForKeyboard == null) return;
            var content = _scrollForKeyboard.ContentInset;
            content.Bottom = _scrollInsetBase;
            _scrollForKeyboard.ContentInset = content;
            _scrollForKeyboard.ScrollIndicatorInsets = content;
        }

        /* ---------- عناصر جاهزة ---------- */

        /// <summary>
        /// إنشاء شاشة قابلة للتمرير بمحتوى رأسي (بديل ScrollView + LinearLayout في AXML).
        /// ترجع الحاوية الرأسية التي تُضاف داخلها العناصر.
        /// </summary>
        protected UIStackView CreateScrollContent(float spacing = 12f, float padding = 16f)
        {
            var scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AlwaysBounceVertical = true,
                KeyboardDismissMode = UIScrollViewKeyboardDismissMode.Interactive
            };
            View.AddSubview(scroll);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            var stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = (nfloat)spacing,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill
            };
            scroll.AddSubview(stack);
            scroll.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, (nfloat)padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -(nfloat)padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, (nfloat)padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, -(nfloat)padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, -2 * (nfloat)padding),
            });

            AttachKeyboardScroll(scroll);
            return stack;
        }

        /// <summary>محتوى ثابت بلا تمرير (شاشات قصيرة مثل شاشة القفل).</summary>
        protected UIView CreateCenteredContainer(float padding = 24f)
        {
            var container = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            View.AddSubview(container);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(container, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(container, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, (nfloat)padding),
                NSLayoutConstraint.Create(container, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -(nfloat)padding),
            });
            return container;
        }

        /* ---------- التنقل ---------- */

        protected void Push(UIViewController controller)
        {
            if (controller == null) return;
            if (NavigationController != null) NavigationController.PushViewController(controller, true);
            else PresentViewController(controller, true, null);
        }

        /// <summary>جذر جديد (بعد الدخول/الخروج) — يمنع الرجوع للشاشة السابقة.</summary>
        protected void SetRoot(UIViewController controller)
        {
            var app = UIApplication.SharedApplication;
            UIWindow window = Ui.KeyWindow();
            if (window == null) return;
            var nav = new UINavigationController(controller);
            nav.SetNavigationBarHidden(controller is BaseViewController bc && bc.ShowNavigationBar == false, false);
            window.RootViewController = nav;
            UIView.Transition(window, 0.3, UIViewAnimationOptions.TransitionCrossDissolve, () => { }, null);
        }

        protected void Back()
        {
            if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                NavigationController.PopViewController(true);
            else
                NavigationController.SetNavigationBarHidden(false, true);
        }

        /* ---------- أدوات ---------- */

        protected UILabel MakeLabel(string text, float size = 15f, UIColor color = null,
            UIFontWeight weight = UIFontWeight.Regular, UITextAlignment align = UITextAlignment.Natural)
        {
            return new UILabel
            {
                Text = text ?? "",
                Font = Theme.Font(size, weight),
                TextColor = color ?? Theme.TextPrimary,
                TextAlignment = align,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
        }

        protected void ShowError(string message)
        {
            Ui.Alert("خطأ", message ?? "");
        }

        protected bool RequireOnline()
        {
            if (Data.Net.IsOnline()) return true;
            Ui.Toast("لا يوجد اتصال بالإنترنت");
            return false;
        }
    }
}
