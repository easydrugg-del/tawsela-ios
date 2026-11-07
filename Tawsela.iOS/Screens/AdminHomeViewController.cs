using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/AdminActivity.cs — نفس شكل أندرويد بالضبط:
    /// هيدر برتقالي (عنوان + إحصائيات + اختصار الأكواد) + أقسام ببلاطات بنفس عدد الأعمدة.
    /// </summary>
    public class AdminHomeViewController : BaseViewController
    {
        private UILabel _stats;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public AdminHomeViewController() : base() { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.Surface;
            BuildUi();
            LoadStats();
        }

        private void BuildUi()
        {
            // ===== الهيدر (بديل bg_header) =====
            var header = Shapes.Header(210f);
            header.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(header);

            var title = MakeLabel("🛠️ لوحة الإدارة", 20f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Right);
            _stats = MakeLabel("جارٍ التحميل…", 14f, Theme.Hex(0xDDDDDD), UIFontWeight.Regular, UITextAlignment.Center);
            _stats.Lines = 0;

            var coupons = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            coupons.SetTitle("🎟️ إدارة الأكواد والخصومات", UIControlState.Normal);
            coupons.SetTitleColor(Theme.Primary, UIControlState.Normal);
            coupons.TitleLabel.Font = Theme.FontBold(15);
            coupons.BackgroundColor = Theme.White;
            coupons.Layer.CornerRadius = Theme.CornerTile;
            coupons.Layer.BorderWidth = 1f;
            coupons.Layer.BorderColor = Theme.CardStroke.CGColor;
            coupons.Layer.MasksToBounds = true;
            coupons.TouchUpInside += (s, e) => Push(new CouponsAdminViewController());

            header.AddSubview(title);
            header.AddSubview(_stats);
            header.AddSubview(coupons);

            // زر رجوع (أندرويد يستخدم زر الرجوع بالنظام)
            var back = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            back.SetTitle("‹", UIControlState.Normal);
            back.SetTitleColor(UIColor.White, UIControlState.Normal);
            back.TitleLabel.Font = Theme.FontBold(30);
            back.TouchUpInside += (s, e) =>
            {
                if (NavigationController != null) NavigationController.PopViewController(true);
            };
            header.AddSubview(back);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 210),

                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 12),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, header, NSLayoutAttribute.Trailing, 1, -16),

                NSLayoutConstraint.Create(_stats, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_stats, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(_stats, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, header, NSLayoutAttribute.Trailing, 1, -16),

                NSLayoutConstraint.Create(coupons, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _stats, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(coupons, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(coupons, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, header, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(coupons, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 48),

                NSLayoutConstraint.Create(back, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(back, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 8),
                NSLayoutConstraint.Create(back, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
                NSLayoutConstraint.Create(back, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
            });

            // ===== المحتوى =====
            var scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AlwaysBounceVertical = true
            };
            View.AddSubview(scroll);

            var content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            scroll.AddSubview(content);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(content, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, 4),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -10),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, 10),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, -10),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, -20),
            });

            // ===== نفس أقسام أندرويد وترتيب الصفوف =====
            var s1 = NewSection(content, "إدارة المستخدمين", first: true);
            AddRow(s1, 3,
                ("🚕", "السائقون", () => Push(new DriversAdminViewController())),
                ("👥", "العملاء", () => Push(new CustomersAdminViewController())),
                ("🔍", "بحث سائق", () => Push(new DriverSearchViewController())));
            AddRow(s1, 3,
                ("🧑‍✈️", "مشرفو التكاتك", () => Push(new SupervisorAdminViewController())));
            AddRow(s1, 2,
                ("🚗", "حركات سائق", () => Push(new DriverMovementsViewController())),
                ("📒", "كشف حساب", () => Push(new DriverStatementPickerViewController())));

            var s2 = NewSection(content, "الخدمات والعروض");
            AddRow(s2, 3,
                ("🎟️", "إدارة الأكواد", () => Push(new CouponsAdminViewController())),
                ("🛎️", "الخدمات", () => Push(new ServiceAdminViewController())),
                ("💳", "الاشتراكات", () => Push(new SubscriptionsAdminViewController())));
            AddRow(s2, 3,
                ("📍", "المناطق", () => Push(new AreasAdminViewController())),
                ("🛣️", "الشوارع", () => Push(new StreetsAdminViewController())));
            AddRow(s2, 1,
                ("🏪", "توصيلات المطاعم", () => Push(new RestaurantDeliveryAdminViewController())));

            var s3 = NewSection(content, "المتابعة والدعم");
            AddRow(s3, 3,
                ("📢", "الشكاوى", () => Push(new ComplaintsAdminViewController())),
                ("📄", "التوثيق", () => Push(new DocsAdminViewController())),
                ("📊", "تحليلات", () => Push(new AdminAnalyticsViewController())));

            var s4 = NewSection(content, "الخرائط والتقارير");
            AddRow(s4, 3,
                ("🔥", "الحرارية", () => Push(new HeatmapViewController())),
                ("🗺️", "خريطة السائقين", OpenDriversMap),
                ("🧾", "تقرير الرحلات", () => Push(new TripReportViewController())));

            var s5 = NewSection(content, "المالية والإعلانات");
            AddRow(s5, 3,
                ("💰", "طلبات الشحن", () => Push(new TopUpsAdminViewController())),
                ("💸", "خصم السائق", () => Push(new DriverFeeAdminViewController())),
                ("📣", "الإعلانات", () => Push(new AdsAdminViewController())));

            var s6 = NewSection(content, "الطوارئ والنظام");
            AddRow(s6, 3,
                ("🚨", "بيانات الطوارئ", () => Push(new EmergencyTypesAdminViewController())),
                ("🆘", "تفاصيل الطوارئ", () => Push(new EmergenciesAdminViewController())),
                ("👔", "المديرين", () => Push(new AdminAccountsViewController())));

            // شاشات إضافية موجودة في iOS فقط
            var s7 = NewSection(content, "إضافي");
            AddRow(s7, 3,
                ("🧾", "الطلبات", () => Push(new OrdersViewController("", "", "الطلبات"))),
                ("🏪", "المطاعم وحساباتها", () => Push(new RestaurantsAdminViewController())),
                ("🚫", "السائقون المحظورون", () => Push(new BlockedDriversViewController())));
            AddRow(s7, 3,
                ("📜", "سجل تأكيدات الدفع", () => Push(new TopUpHistoryViewController(true))),
                ("🔔", "الإشعارات", () => Push(new NotificationsViewController())),
                ("👤", "حسابي", () => Push(new ProfileViewController())));

            // زر تحديث الإصدار (بديل btnAppUpdate)
            if (Session.IsAdmin)
            {
                var update = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
                update.SetTitle("⬆️ تحديث الاصدار", UIControlState.Normal);
                update.SetTitleColor(UIColor.White, UIControlState.Normal);
                update.TitleLabel.Font = Theme.FontBold(14);
                update.BackgroundColor = Theme.Red;
                update.Layer.CornerRadius = Theme.CornerTile;
                update.Layer.MasksToBounds = true;
                update.TouchUpInside += (s, e) => Push(new AppUpdateAdminViewController());
                update.HeightAnchor.ConstraintEqualTo(52f).Active = true;

                var wrap = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
                wrap.AddSubview(update);
                wrap.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(update, NSLayoutAttribute.Top, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Top, 1, 4),
                    NSLayoutConstraint.Create(update, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Bottom, 1, -4),
                    NSLayoutConstraint.Create(update, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Leading, 1, 4),
                    NSLayoutConstraint.Create(update, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Trailing, 1, -4),
                });
                content.AddArrangedSubview(wrap);
            }

            var credit = MakeLabel("Designed @ Ahmed Ali", 10f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Center);
            credit.Alpha = 0.6f;
            content.AddArrangedSubview(credit);
        }

        /// <summary>قسم زي أندرويد: عنوان برتقالي ثم صفوف بلاطات.</summary>
        private UIStackView NewSection(UIStackView content, string title, bool first = false)
        {
            var section = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            section.AddArrangedSubview(MakeLabel(title, 15f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right));

            if (!first && content.ArrangedSubviews.Length > 0)
            {
                var prev = content.ArrangedSubviews[content.ArrangedSubviews.Length - 1];
                content.SetCustomSpacing(14f, prev);
            }
            content.AddArrangedSubview(section);
            return section;
        }

        /// <summary>
        /// صف بلاطات بعدد أعمدة ثابت (زي weightSum في أندرويد):
        /// cols=3 → كل بلاطة ثلث العرض، والباقي فراغ (محاذاة لليمين في RTL).
        /// </summary>
        private void AddRow(UIStackView section, int cols, params (string emoji, string label, Action tap)[] items)
        {
            var row = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            foreach (var it in items)
                row.AddArrangedSubview(Tile(it.emoji, it.label, it.tap));

            for (int i = items.Length; i < cols; i++)
                row.AddArrangedSubview(new UIView { TranslatesAutoresizingMaskIntoConstraints = false });

            row.HeightAnchor.ConstraintEqualTo(96f).Active = true;
            section.AddArrangedSubview(row);
        }

        /// <summary>بلاطة 96 زي أندرويد: إيموجي 30 + نص 12 عريض.</summary>
        private UIControl Tile(string emoji, string label, Action onTap)
        {
            var b = new UIControl { TranslatesAutoresizingMaskIntoConstraints = false };
            b.BackgroundColor = Theme.White;
            b.Layer.CornerRadius = Theme.CornerTile;
            b.Layer.BorderWidth = 1f;
            b.Layer.BorderColor = Theme.CardStroke.CGColor;
            b.Layer.MasksToBounds = true;

            var e = MakeLabel(emoji, 30f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Center);
            e.UserInteractionEnabled = false;

            var l = MakeLabel(label, label.Length > 12 ? 11f : 12f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Center);
            l.Lines = 2;
            l.UserInteractionEnabled = false;

            b.AddSubview(e);
            b.AddSubview(l);
            b.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(e, NSLayoutAttribute.Top, NSLayoutRelation.Equal, b, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(e, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, b, NSLayoutAttribute.CenterX, 1, 0),

                NSLayoutConstraint.Create(l, NSLayoutAttribute.Top, NSLayoutRelation.Equal, e, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(l, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, b, NSLayoutAttribute.Leading, 1, 4),
                NSLayoutConstraint.Create(l, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, b, NSLayoutAttribute.Trailing, 1, -4),
            });

            b.TouchUpInside += (s, ev) => onTap();
            return b;
        }

        /// <summary>خريطة السائقين الحية (بديل DriverMapActivity).</summary>
        private void OpenDriversMap()
        {
            Ui.Run(() =>
            {
                List<Driver> list = null;
                try { list = Api.DriverLocations(); } catch { }

                var points = new List<double[]>();
                if (list != null)
                {
                    foreach (var d in list)
                    {
                        if (d.Lat != 0 || d.Lng != 0) points.Add(new[] { d.Lat, d.Lng });
                    }
                }

                Ui.BeginOnMain(() =>
                {
                    if (points.Count == 0)
                    {
                        Ui.Toast("لا يوجد سائقون على الخريطة الآن");
                        return;
                    }
                    Push(new PointsMapViewController("خريطة السائقين",
                        points[0][0], points[0][1], points, "#1976D2"));
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void LoadStats()
        {
            Ui.Run(() =>
            {
                var all = Api.GetOrders("");
                var counts = new Dictionary<string, int>();
                if (all != null)
                {
                    foreach (var o in all)
                    {
                        if (!counts.ContainsKey(o.Status)) counts[o.Status] = 0;
                        counts[o.Status]++;
                    }
                }

                var available = -1;
                try { available = Api.AvailableDriversCount(); } catch { }

                Ui.BeginOnMain(() => RenderStats(all == null ? 0 : all.Count, counts, available));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private static int Count(Dictionary<string, int> d, string key)
        {
            int v;
            return d.TryGetValue(key, out v) ? v : 0;
        }

        private void RenderStats(int total, Dictionary<string, int> counts, int availableDrivers)
        {
            if (_stats == null) return;

            _stats.Text =
                "إجمالي الطلبات: " + total +
                "  |  جديد: " + Count(counts, "جديد") +
                "  |  مكتمل: " + Count(counts, "مكتمل") +
                "  |  ملغي: " + Count(counts, "ملغي") +
                "\nالسائقون المتاحون: " + (availableDrivers >= 0 ? availableDrivers : 0);
        }
    }
}
