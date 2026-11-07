using System;
using System.Collections.Generic;
using CoreGraphics;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantMainActivity.cs — نفس شكل أندرويد بالضبط:
    /// هيدر برتقالي (عنوان + اسم + حالة) ثم أزرار ملونة كبيرة.
    /// </summary>
    public class RestaurantMainViewController : BaseViewController
    {
        private UILabel _nameLabel;
        private UILabel _statusLabel;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public RestaurantMainViewController() : base() { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.Surface;
            BuildUi();
            LoadRestaurant();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            LoadRestaurant();
        }

        private void BuildUi()
        {
            // ===== الترويسة: اسم المطعم + حالته (بديل bg_header) =====
            var header = Shapes.Header(120f);
            header.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(header);

            var title = MakeLabel("🏪 لوحة المطعم", 22f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Right);
            _nameLabel = MakeLabel("مرحباً", 15f, UIColor.White.ColorWithAlpha(0.8f), UIFontWeight.Regular, UITextAlignment.Right);
            _statusLabel = MakeLabel("", 12f, UIColor.White.ColorWithAlpha(0.87f), UIFontWeight.Bold, UITextAlignment.Right);

            header.AddSubview(title);
            header.AddSubview(_nameLabel);
            header.AddSubview(_statusLabel);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 120),

                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 20),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, header, NSLayoutAttribute.Trailing, 1, -20),

                NSLayoutConstraint.Create(_nameLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_nameLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, title, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_nameLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, title, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _nameLabel, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, title, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, title, NSLayoutAttribute.Trailing, 1, 0),
            });

            // ===== المحتوى (بديل ScrollView + padding 16) =====
            var scroll = new UIScrollView { TranslatesAutoresizingMaskIntoConstraints = false, AlwaysBounceVertical = true };
            View.AddSubview(scroll);

            var content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            scroll.AddSubview(content);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(content, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, 16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, -32),
            });

            // الإجراء الأساسي (بديل btnNewRestaurantDelivery)
            var newBtn = BigButton("＋ طلب توصيل جديد", Theme.Primary, 60f);
            newBtn.TouchUpInside += (s, e) => OpenNewDelivery();
            content.AddArrangedSubview(newBtn);

            // الأقسام (بديل TextView الأقسام — بلون النص الأساسي)
            var sections = MakeLabel("الأقسام", 15f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Right);
            content.AddArrangedSubview(PadTab(sections, 16, 8));

            // صف أول: طلباتي (أزرق) / الإشعارات (برتقالي)
            var row1 = new UIStackView { Axis = UILayoutConstraintAxis.Horizontal, Spacing = 12, TranslatesAutoresizingMaskIntoConstraints = false };
            var myOrders = BigButton("📋 طلباتي", Theme.Blue, 92f);
            myOrders.TouchUpInside += (s, e) => Push(new RestaurantDeliveryMyOrdersViewController());
            var notifs = BigButton("🔔 الإشعارات", Theme.Primary, 92f);
            notifs.TouchUpInside += (s, e) => Push(new NotificationsViewController());
            row1.AddArrangedSubview(myOrders);
            row1.AddArrangedSubview(notifs);
            content.AddArrangedSubview(row1);

            // صف ثانٍ: تحديث (أخضر) / خروج (أحمر)
            var row2 = new UIStackView { Axis = UILayoutConstraintAxis.Horizontal, Spacing = 12, TranslatesAutoresizingMaskIntoConstraints = false };
            var refresh = BigButton("🔄 تحديث", Theme.Green, 92f);
            refresh.TouchUpInside += (s, e) => LoadRestaurant();
            var logout = BigButton("🚪 خروج", Theme.Red, 92f);
            logout.TouchUpInside += (s, e) =>
            {
                Ui.Confirm("تسجيل الخروج", "سيتم إنهاء الجلسة على هذا الجهاز.", "خروج", () =>
                {
                    try { Api.ClearSession(); } catch { }
                    Router.ShowLogin();
                });
            };
            row2.AddArrangedSubview(refresh);
            row2.AddArrangedSubview(logout);
            content.AddArrangedSubview(PadTab(row2, 12, 0));

            // بيانات المطعم (رابط صغير — موجود في iOS فقط للوصول للبروفايل)
            var profile = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            profile.SetTitle("🏪 بيانات المطعم", UIControlState.Normal);
            profile.SetTitleColor(Theme.Primary, UIControlState.Normal);
            profile.TitleLabel.Font = Theme.FontBold(14);
            profile.TouchUpInside += (s, e) => Push(new RestaurantProfileViewController());
            content.AddArrangedSubview(PadTab(profile, 14, 0));
        }

        private UIView PadTab(UIView child, float top, float bottom)
        {
            var box = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            child.TranslatesAutoresizingMaskIntoConstraints = false;
            box.AddSubview(child);
            box.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Top, NSLayoutRelation.Equal, box, NSLayoutAttribute.Top, 1, top),
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, box, NSLayoutAttribute.Bottom, 1, -bottom),
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, box, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, box, NSLayoutAttribute.Trailing, 1, 0),
            });
            return box;
        }

        /// <summary>زر كبير ملون زي أندرويد: نص أبيض عريض بزوايا 16.</summary>
        private UIButton BigButton(string title, UIColor bg, float height)
        {
            var b = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(UIColor.White, UIControlState.Normal);
            b.SetTitleColor(UIColor.White.ColorWithAlpha(0.6f), UIControlState.Disabled);
            b.TitleLabel.Font = Theme.FontBold(16);
            b.BackgroundColor = bg;
            b.Layer.CornerRadius = 16f;
            b.Layer.MasksToBounds = true;
            b.HeightAnchor.ConstraintEqualTo(height).Active = true;
            b.SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft;
            return b;
        }

        /// <summary>اسم المطعم من الخادم + حالة الاعتماد (بديل LoadRestaurant في أندرويد).</summary>
        private void LoadRestaurant()
        {
            Ui.Run(() =>
            {
                Dictionary<string, object> rest = null;
                try { rest = Api.MyRestaurant(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    if (rest == null)
                    {
                        _nameLabel.Text = string.IsNullOrWhiteSpace(Session.FullName) ? "مرحباً" : Session.FullName;
                        _statusLabel.Text = "";
                        return;
                    }

                    var name = Row.Str(rest, "Name");
                    _nameLabel.Text = string.IsNullOrWhiteSpace(name) ? Session.FullName : name;

                    var active = Row.Str(rest, "IsActive");
                    var isActive = active == "True" || active == "1" ||
                                   string.Equals(active, "true", StringComparison.OrdinalIgnoreCase);
                    _statusLabel.Text = isActive ? "● مفعّل" : "● قيد المراجعة";
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        /// <summary>فتح شاشة توصيلة جديدة (تُحمّل مناطق التوصيل أولًا).</summary>
        private void OpenNewDelivery()
        {
            var progress = Ui.ShowProgress("جارٍ التحميل…");
            Ui.Run(() =>
            {
                var areas = Api.RestaurantDeliveryAreas();
                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();
                    Push(new RestaurantNewDeliveryViewController(areas));
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
