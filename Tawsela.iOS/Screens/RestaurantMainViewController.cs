using System;
using System.Collections.Generic;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantMainActivity.cs — لوحة المطعم:
    /// بيانات المطعم + ملخص الإحصائيات + توصيلاته + الإجراءات.
    /// </summary>
    public class RestaurantMainViewController : BaseViewController
    {
        private UIStackView _stack;
        private UIStackView _content;
        private UILabel _header, _info;
        private UIButton _newDelivery;
        private NSTimer _timer;
        private int _lastPending = -1;

        public override bool ShowNavigationBar
        {
            get { return true; }
        }

        public RestaurantMainViewController() : base("لوحة المطعم") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            Load();
            // تحديث تلقائي كل 20 ثانية حتى تظهر التوصيلات الجديدة
            _timer?.Invalidate();
            _timer = NSTimer.CreateRepeatingScheduledTimer(20.0, _ => Load());
        }

        public override void ViewWillDisappear(bool animated)
        {
            _timer?.Invalidate();
            _timer = null;
            base.ViewWillDisappear(animated);
        }

        private void BuildUi()
        {
            _stack = CreateScrollContent(12f, 16f);

            _header = MakeLabel("جارٍ التحميل…", 16f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            _info = MakeLabel("", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _stack.AddArrangedSubview(_header);
            _stack.AddArrangedSubview(_info);

            var newDelivery = Controls.PrimaryButton("طلب توصيلة جديدة");
            newDelivery.TouchUpInside += (s, e) => OpenNewDelivery();
            _newDelivery = newDelivery;
            _stack.AddArrangedSubview(newDelivery);

            var mine = Controls.OutlineButton("توصيلات مطعمي");
            mine.TouchUpInside += (s, e) => Push(new RestaurantDeliveryMyOrdersViewController());
            _stack.AddArrangedSubview(mine);

            var profile = Controls.OutlineButton("بيانات المطعم");
            profile.TouchUpInside += (s, e) => Push(new RestaurantProfileViewController());
            _stack.AddArrangedSubview(profile);

            var notifications = Controls.OutlineButton("الإشعارات");
            notifications.TouchUpInside += (s, e) => Push(new NotificationsViewController());
            _stack.AddArrangedSubview(notifications);

            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(refresh);

            var logout = Controls.DangerButton("تسجيل الخروج");
            logout.TouchUpInside += (s, e) =>
            {
                Ui.Confirm("تسجيل الخروج", "إنهاء الجلسة على هذا الجهاز؟", "خروج", () =>
                {
                    try { Api.ClearSession(); } catch { }
                    Router.ShowLogin();
                });
            };
            _stack.AddArrangedSubview(logout);

            // المحتوى المتغيّر فقط (الإحصائيات والتوصيلات) — الأزرار أعلاه تبقى ثابتة
            _content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _stack.AddArrangedSubview(_content);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                Dictionary<string, object> restaurant = null;
                List<Dictionary<string, object>> orders = null;
                Dictionary<string, object> stats = null;
                try { restaurant = Api.MyRestaurant(); } catch { }
                try { orders = Api.GetMyRestaurantDeliveries(); } catch { }
                try { stats = Api.RestaurantStats(); } catch { }
                Ui.BeginOnMain(() => Render(restaurant, orders, stats));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(Dictionary<string, object> restaurant, List<Dictionary<string, object>> orders,
            Dictionary<string, object> stats)
        {
            ClearContent();

            if (restaurant != null)
            {
                var name = Tawsela.Data.Row.Str(restaurant, "Name");
                _header.Text = string.IsNullOrEmpty(name) ? "مطعمي" : name;

                var phone = Tawsela.Data.Row.Str(restaurant, "Phone");
                var address = Tawsela.Data.Row.Str(restaurant, "Address");
                var line = "";
                if (!string.IsNullOrEmpty(phone)) line = phone;
                if (!string.IsNullOrEmpty(address)) line = line == "" ? address : line + " · " + address;
                _info.Text = line;

                // المطعم المسجّل ذاتيًا يبقى موقوفًا حتى تعتمده الإدارة
                var activeStr = Tawsela.Data.Row.Str(restaurant, "IsActive");
                var active = activeStr == "True" || activeStr == "1" ||
                             string.Equals(activeStr, "true", StringComparison.OrdinalIgnoreCase);
                if (_newDelivery != null) _newDelivery.Enabled = active;
                if (!active)
                    _info.Text = "قيد مراجعة الإدارة — لن تتمكن من إنشاء توصيلات حتى اعتماد حسابك";
            }
            else
            {
                _header.Text = "لوحة المطعم";
                _info.Text = "";
            }

            if (stats != null)
            {
                var pending = Tawsela.Data.Row.Int(stats, "pending");

                _content.AddArrangedSubview(Controls.SectionTitle("ملخص التوصيلات"));
                _content.AddArrangedSubview(StatsRow(
                    Tawsela.Data.Row.Int(stats, "today").ToString(),
                    pending.ToString(),
                    Tawsela.Data.Row.Int(stats, "delivered").ToString(),
                    Tawsela.Data.Row.Num(stats, "revenue").ToString("0") + " ج.م"));

                // تنبيه عند وصول توصيلة جديدة أثناء فتح اللوحة
                if (_lastPending >= 0 && pending > _lastPending)
                    Ui.Toast("لديك توصيلة جديدة");
                _lastPending = pending;
            }

            _content.AddArrangedSubview(Controls.SectionTitle("توصيلاتي"));

            if (orders == null || orders.Count == 0)
            {
                _content.AddArrangedSubview(MakeLabel("لا توجد توصيلات حاليًا", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var o in orders) _content.AddArrangedSubview(Row(o));
        }

        private void ClearContent()
        {
            for (int i = _content.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _content.ArrangedSubviews[i];
                _content.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }
        }

        /// <summary>صف الإحصائيات: اليوم / قيد الانتظار / تم التسليم / الإيراد.</summary>
        private UIView StatsRow(string today, string pending, string delivered, string revenue)
        {
            var card = Controls.Card();
            var row = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Distribution = UIStackViewDistribution.FillEqually,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            row.AddArrangedSubview(StatCell("اليوم", today));
            row.AddArrangedSubview(StatCell("قيد الانتظار", pending));
            row.AddArrangedSubview(StatCell("تم التسليم", delivered));
            row.AddArrangedSubview(StatCell("الإيراد", revenue));

            card.AddSubview(row);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 10),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -10),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });
            return card;
        }

        private UIView StatCell(string label, string value)
        {
            var cell = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 2,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            cell.AddArrangedSubview(MakeLabel(value, 17f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center));
            cell.AddArrangedSubview(MakeLabel(label, 11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center));
            return cell;
        }

        /// <summary>بطاقة توصيلة (للعرض فقط) — الضغط يفتح شاشة التفاصيل الخاصة بالمطعم.</summary>
        private UIView Row(Dictionary<string, object> o)
        {
            var card = Controls.Card();
            var id = Tawsela.Data.Row.Int(o, "Id");
            var status = Tawsela.Data.Row.Str(o, "Status");
            var customer = Tawsela.Data.Row.Str(o, "CustomerName");
            var addr = Tawsela.Data.Row.Str(o, "DeliveryAddress");
            var fare = Tawsela.Data.Row.Num(o, "Fare");

            var title = MakeLabel(string.IsNullOrEmpty(customer) ? "توصيلة" : customer, 15f,
                Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            var st = MakeLabel(string.IsNullOrEmpty(status) ? "—" : status, 12f,
                Theme.Accent, UIFontWeight.Medium, UITextAlignment.Left);
            var a = MakeLabel(addr, 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            var price = MakeLabel(fare.ToString("0.00") + " ج.م", 14f, Theme.Primary,
                UIFontWeight.Bold, UITextAlignment.Left);

            card.AddSubview(title);
            card.AddSubview(st);
            card.AddSubview(a);
            card.AddSubview(price);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(st, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(st, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(a, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(a, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(a, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(price, NSLayoutAttribute.Top, NSLayoutRelation.Equal, a, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(price, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(price, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            card.UserInteractionEnabled = true;
            card.AddGestureRecognizer(new UITapGestureRecognizer(
                () => Push(new RestaurantOrderDetailViewController(id))));

            return card;
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
