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

            stack.AddArrangedSubview(Controls.SectionTitle("نظرة عامة"));

            _stats = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_stats);
            _stats.AddArrangedSubview(MakeLabel("جارٍ التحميل…", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));

            stack.AddArrangedSubview(Controls.SectionTitle("الأقسام"));

            _sections = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_sections);

            AddSection("الطلبات", "ic_inbox", () => Push(new OrdersViewController("", "", "الطلبات")));
            AddSection("السائقون", "ic_taxi", () => Push(new DriversAdminViewController()));
            AddSection("العملاء", "ic_group", () => Push(new CustomersAdminViewController()));
            AddSection("المناطق", "ic_action_map", () => Push(new AreasAdminViewController()));
            AddSection("الشوارع", "ic_pin", () => Push(new StreetsAdminViewController()));
            AddSection("الكوبونات", "ic_ticket", () => Push(new CouponsAdminViewController()));
            AddSection("أنواع الطوارئ", "ic_action_warning", () => Push(new EmergencyTypesAdminViewController()));
            AddSection("مراجعة المستندات", "ic_doc", () => Push(new DocsAdminViewController()));
            AddSection("الشكاوى", "ic_support", () => Push(new ComplaintsAdminViewController()));
            AddSection("الاشتراكات", "ic_work", () => Push(new SubscriptionsAdminViewController()));
            AddSection("طلبات الشحن", "ic_money", () => Push(new TopUpsAdminViewController()));
            AddSection("الإشعارات", "ic_bell", () => Push(new NotificationsViewController()));
            AddSection("حسابي", "ic_person", () => Push(new ProfileViewController()));

            // شاشات الإدارة الإضافية (بديل Activities/Admin* و*AdminActivity)
            stack.AddArrangedSubview(Controls.SectionTitle("الإدارة والتقارير"));

            var extra = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(extra);

            AddExtra(extra, "التحليلات", "ic_action_receipt", () => Push(new AdminAnalyticsViewController()));
            AddExtra(extra, "إدارة الخدمات والأسعار", "ic_work", () => Push(new ServiceAdminViewController()));
            AddExtra(extra, "حسابات الإدارة", "ic_person", () => Push(new AdminAccountsViewController()));
            AddExtra(extra, "المشرفون", "ic_group", () => Push(new SupervisorAdminViewController()));
            AddExtra(extra, "الإعلانات", "ic_bell", () => Push(new AdsAdminViewController()));
            AddExtra(extra, "مناطق توصيل المطاعم", "ic_action_map", () => Push(new RestaurantDeliveryAdminViewController()));
            AddExtra(extra, "المطاعم وحساباتها", "ic_work", () => Push(new RestaurantsAdminViewController()));
            AddExtra(extra, "رسم قبول الطلب", "ic_money", () => Push(new DriverFeeAdminViewController()));
            AddExtra(extra, "تحديث التطبيق", "ic_work", () => Push(new AppUpdateAdminViewController()));
            AddExtra(extra, "السائقون المحظورون", "ic_action_close", () => Push(new BlockedDriversViewController()));
            AddExtra(extra, "بحث عن سائق", "ic_search", () => Push(new DriverSearchViewController()));
            AddExtra(extra, "كشف حساب سائق", "ic_money", () => Push(new DriverStatementPickerViewController()));
            AddExtra(extra, "سجل تأكيدات الدفع", "ic_action_receipt", () => Push(new TopUpHistoryViewController(true)));
            AddExtra(extra, "تحركات السائقين", "ic_action_map", () => Push(new DriverMovementsViewController()));
            AddExtra(extra, "الخريطة الحرارية", "ic_action_map", () => Push(new HeatmapViewController()));
            AddExtra(extra, "تقرير الرحلات", "ic_doc", () => Push(new TripReportViewController()));
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
                var stats = Api.AdminStats();

                // نفس سطر أندرويد: «السائقون المتاحون: N» من نقطة نهاية مستقلة
                var available = -1;
                try { available = Api.AvailableDriversCount(); } catch { }

                Ui.BeginOnMain(() => RenderStats(stats, available));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void RenderStats(Dictionary<string, object> stats, int availableDrivers)
        {
            for (int i = _stats.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _stats.ArrangedSubviews[i];
                _stats.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (stats == null || stats.Count == 0)
            {
                _stats.AddArrangedSubview(MakeLabel("لا توجد إحصائيات", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            // اعرض كل القيم الرقمية القادمة من الخادم بأسماء عربية معروفة
            var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "orders", "الطلبات" },
                { "totalOrders", "إجمالي الطلبات" },
                { "todayOrders", "طلبات اليوم" },
                { "activeOrders", "طلبات جارية" },
                { "completedOrders", "طلبات مكتملة" },
                { "cancelledOrders", "طلبات ملغاة" },
                { "customers", "العملاء" },
                { "totalCustomers", "إجمالي العملاء" },
                { "drivers", "السائقون" },
                { "totalDrivers", "إجمالي السائقين" },
                { "availableDrivers", "سائقون متاحون" },
                { "revenue", "الإيراد" },
                { "totalRevenue", "إجمالي الإيراد" },
                { "todayRevenue", "إيراد اليوم" },
                { "walletBalance", "أرصدة المحافظ" },
            };

            var shown = 0;
            foreach (var kv in stats)
            {
                var label = kv.Key;
                string arabic;
                if (known.TryGetValue(kv.Key, out arabic)) label = arabic;

                var value = kv.Value == null ? "—" : kv.Value.ToString();
                _stats.AddArrangedSubview(Controls.InfoRow(label, value));
                shown++;
                if (shown >= 12) break;
            }

            // إن لم يُرجِع الخادم العدد ضمن الإحصائيات، نضيفه من نقطة النهاية المستقلة
            var hasAvailable = false;
            foreach (var k in stats.Keys)
            {
                if (string.Equals(k, "availableDrivers", StringComparison.OrdinalIgnoreCase))
                {
                    hasAvailable = true;
                    break;
                }
            }

            if (!hasAvailable && availableDrivers >= 0)
            {
                _stats.AddArrangedSubview(Controls.InfoRow("سائقون متاحون", availableDrivers.ToString()));
            }
        }
    }
}
