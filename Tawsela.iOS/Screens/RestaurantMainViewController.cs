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
        private UILabel _header, _info, _status;
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

            /* ---------- بطاقة المطعم (اسم + حالة + هاتف/عنوان) ---------- */
            var card = Controls.Card();
            _header = MakeLabel("جارٍ التحميل…", 17f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Right);
            _info = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _status = MakeLabel("", 11f, Theme.Green, UIFontWeight.Semibold, UITextAlignment.Left);

            card.AddSubview(_header);
            card.AddSubview(_info);
            card.AddSubview(_status);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 12),
                NSLayoutConstraint.Create(_header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_status, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _header, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_status, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _header, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, _status, NSLayoutAttribute.Trailing, 1, 8),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -12),
            });
            _stack.AddArrangedSubview(card);

            /* ---------- الإجراء الأساسي ---------- */
            var newDelivery = Controls.PrimaryButton("＋ طلب توصيل جديد");
            newDelivery.TouchUpInside += (s, e) => OpenNewDelivery();
            _newDelivery = newDelivery;
            _stack.AddArrangedSubview(newDelivery);

            /* ---------- شبكة الأقسام (2×2) ---------- */
            _stack.AddArrangedSubview(Controls.SectionTitle("الأقسام"));

            var grid = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 12,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            grid.AddArrangedSubview(GridRow(
                Controls.Tile("ic_action_receipt", "طلباتي", Theme.Blue, Theme.PrimaryDark,
                    () => Push(new RestaurantDeliveryMyOrdersViewController())),
                Controls.Tile("ic_person", "بيانات المطعم", Theme.Hex(0x8E24AA), Theme.PrimaryDark,
                    () => Push(new RestaurantProfileViewController()))));

            grid.AddArrangedSubview(GridRow(
                Controls.Tile("ic_bell", "الإشعارات", Theme.Hex(0xEF6C00), Theme.PrimaryDark,
                    () => Push(new NotificationsViewController())),
                Controls.Tile("ic_replay", "تحديث", Theme.Green, Theme.PrimaryDark,
                    () => Load())));

            _stack.AddArrangedSubview(grid);

            /* ---------- المحتوى المتغيّر (ملخص + آخر التوصيلات) ---------- */
            _content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _stack.AddArrangedSubview(_content);

            /* ---------- الخروج ---------- */
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
        }

        /// <summary>صف من بلاطتين متساويتين (بديل LinearLayout horizontal في أندرويد).</summary>
        private static UIStackView GridRow(UIControl left, UIControl right)
        {
            var row = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 12,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            row.AddArrangedSubview(left);
            row.AddArrangedSubview(right);
            row.HeightAnchor.ConstraintEqualTo(96).Active = true;
            return row;
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
                _header.Text = "مرحباً " + (string.IsNullOrEmpty(name) ? "مطعمي" : name);

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
                if (_status != null)
                {
                    _status.Text = active ? "● مفعّل" : "● قيد المراجعة";
                    _status.TextColor = active ? Theme.Green : Theme.Accent;
                }
            }
            else
            {
                _header.Text = "لوحة المطعم";
                _info.Text = "";
                if (_status != null) _status.Text = "";
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

            _content.AddArrangedSubview(Controls.SectionTitle("آخر التوصيلات"));

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
