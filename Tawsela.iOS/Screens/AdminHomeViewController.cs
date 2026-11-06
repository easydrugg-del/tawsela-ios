using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/AdminActivity.cs — لوحة الإدارة: إحصائيات حية
    /// (Api.AdminStats) + الوصول للأقسام المُنقلة.
    /// </summary>
    public class AdminHomeViewController : BaseViewController
    {
        private UIStackView _stats;
        private UIStackView _sections;

        public AdminHomeViewController() : base("لوحة الإدارة") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadStats();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(14f, 16f);

            stack.AddArrangedSubview(Controls.SectionTitle("🛠️ لوحة الإدارة"));

            _stats = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_stats);
            _stats.AddArrangedSubview(MakeLabel("جارٍ التحميل…", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));

            // ===== نفس ترتيب أقسام لوحة الإدارة في أندرويد =====
            _sections = NewGroup(stack);
            AddSection("🎟️ إدارة الأكواد والخصومات", "ic_ticket", () => Push(new CouponsAdminViewController()));

            stack.AddArrangedSubview(Controls.SectionTitle("إدارة المستخدمين"));
            _sections = NewGroup(stack);
            AddSection("السائقون", "ic_taxi", () => Push(new DriversAdminViewController()));
            AddSection("العملاء", "ic_group", () => Push(new CustomersAdminViewController()));
            AddSection("بحث سائق", "ic_search", () => Push(new DriverSearchViewController()));
            AddSection("مشرفو التكاتك", "ic_group", () => Push(new SupervisorAdminViewController()));
            AddSection("حركات سائق", "ic_action_map", () => Push(new DriverMovementsViewController()));
            AddSection("كشف حساب", "ic_money", () => Push(new DriverStatementPickerViewController()));

            stack.AddArrangedSubview(Controls.SectionTitle("الخدمات والعروض"));
            _sections = NewGroup(stack);
            AddSection("إدارة الأكواد", "ic_ticket", () => Push(new CouponsAdminViewController()));
            AddSection("الخدمات", "ic_work", () => Push(new ServiceAdminViewController()));
            AddSection("الاشتراكات", "ic_work", () => Push(new SubscriptionsAdminViewController()));
            AddSection("المناطق", "ic_action_map", () => Push(new AreasAdminViewController()));
            AddSection("الشوارع", "ic_pin", () => Push(new StreetsAdminViewController()));
            AddSection("توصيلات المطاعم", "ic_action_receipt", () => Push(new RestaurantDeliveryAdminViewController()));

            stack.AddArrangedSubview(Controls.SectionTitle("المتابعة والدعم"));
            _sections = NewGroup(stack);
            AddSection("الشكاوى", "ic_support", () => Push(new ComplaintsAdminViewController()));
            AddSection("التوثيق", "ic_doc", () => Push(new DocsAdminViewController()));
            AddSection("تحليلات", "ic_action_receipt", () => Push(new AdminAnalyticsViewController()));

            stack.AddArrangedSubview(Controls.SectionTitle("الخرائط والتقارير"));
            _sections = NewGroup(stack);
            AddSection("الحرارية", "ic_action_map", () => Push(new HeatmapViewController()));
            AddSection("خريطة السائقين", "ic_action_map", () => Push(new DriverMovementsViewController()));
            AddSection("تقرير الرحلات", "ic_doc", () => Push(new TripReportViewController()));

            stack.AddArrangedSubview(Controls.SectionTitle("المالية والإعلانات"));
            _sections = NewGroup(stack);
            AddSection("طلبات الشحن", "ic_money", () => Push(new TopUpsAdminViewController()));
            AddSection("خصم السائق", "ic_money", () => Push(new DriverFeeAdminViewController()));
            AddSection("الإعلانات", "ic_bell", () => Push(new AdsAdminViewController()));

            stack.AddArrangedSubview(Controls.SectionTitle("الطوارئ والنظام"));
            _sections = NewGroup(stack);
            AddSection("بيانات الطوارئ", "ic_action_warning", () => Push(new EmergencyTypesAdminViewController()));
            AddSection("تفاصيل الطوارئ", "ic_action_warning", () => Push(new EmergenciesAdminViewController()));
            AddSection("المديرين", "ic_person", () => Push(new AdminAccountsViewController()));
            AddSection("⬆️ تحديث الاصدار", "ic_work", () => Push(new AppUpdateAdminViewController()));

            // شاشات إضافية موجودة في iOS (مش في لوحة أندرويد الرئيسية)
            stack.AddArrangedSubview(Controls.SectionTitle("إضافي"));
            _sections = NewGroup(stack);
            AddSection("الطلبات", "ic_inbox", () => Push(new OrdersViewController("", "", "الطلبات")));
            AddSection("المطاعم وحساباتها", "ic_work", () => Push(new RestaurantsAdminViewController()));
            AddSection("السائقون المحظورون", "ic_action_close", () => Push(new BlockedDriversViewController()));
            AddSection("سجل تأكيدات الدفع", "ic_action_receipt", () => Push(new TopUpHistoryViewController(true)));
            AddSection("الإشعارات", "ic_bell", () => Push(new NotificationsViewController()));
            AddSection("حسابي", "ic_person", () => Push(new ProfileViewController()));
        }

        /// <summary>حاوية قسم جديدة داخل الصفحة (بديل LinearLayout لكل قسم في أندرويد).</summary>
        private static UIStackView NewGroup(UIStackView stack)
        {
            var g = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(g);
            return g;
        }

        private void AddSection(string title, string icon, Action onTap)
        {
            AddExtra(_sections, title, icon, onTap);
        }

        /// <summary>بطاقة قسم داخل أي حاوية (نفس شكل AddSection).</summary>
        private void AddExtra(UIStackView host, string title, string icon, Action onTap)
        {
            var card = Controls.Card();
            card.UserInteractionEnabled = true;
            card.AddGestureRecognizer(new UITapGestureRecognizer(onTap));

            var img = new UIImageView
            {
                Image = IconFactory.Image(icon, 20f, Theme.Primary),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            var label = MakeLabel(title, 15f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
            var chevron = MakeLabel("‹", 20f, Theme.Gray, UIFontWeight.Regular, UITextAlignment.Left);

            card.AddSubview(img);
            card.AddSubview(label);
            card.AddSubview(chevron);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(img, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(img, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(img, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 20),
                NSLayoutConstraint.Create(img, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 20),

                NSLayoutConstraint.Create(label, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, img, NSLayoutAttribute.Leading, 1, -10),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, chevron, NSLayoutAttribute.Trailing, 1, 8),

                NSLayoutConstraint.Create(chevron, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(chevron, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(card, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 56),
            });

            host.AddArrangedSubview(card);
        }

        private void LoadStats()
        {
            Ui.Run(() =>
            {
                // نفس أندرويد: حساب الحالات من قائمة الطلبات + عدّ السائقين المتاحين
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
            var old = new UIView[_stats.ArrangedSubviews.Length];
            _stats.ArrangedSubviews.CopyTo(old, 0);
            foreach (var v in old)
            {
                _stats.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            var line1 = MakeLabel(
                "إجمالي الطلبات: " + total +
                "  |  جديد: " + Count(counts, "جديد") +
                "  |  مكتمل: " + Count(counts, "مكتمل") +
                "  |  ملغي: " + Count(counts, "ملغي"),
                14f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right);
            line1.Lines = 0;
            _stats.AddArrangedSubview(line1);

            _stats.AddArrangedSubview(MakeLabel(
                "السائقون المتاحون: " + (availableDrivers >= 0 ? availableDrivers : 0),
                14f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right));
        }
    }
}
