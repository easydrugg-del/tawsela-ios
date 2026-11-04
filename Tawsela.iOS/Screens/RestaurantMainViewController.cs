using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantMainActivity.cs — لوحة المطعم:
    /// بيانات المطعم + توصيلاته الجارية + الإجراءات (قبول/وصول/استلام/تسليم).
    /// </summary>
    public class RestaurantMainViewController : BaseViewController
    {
        private UIStackView _stack;
        private UILabel _header;

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
        }

        private void BuildUi()
        {
            _stack = CreateScrollContent(12f, 16f);
            _header = MakeLabel("جارٍ التحميل…", 16f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            _stack.AddArrangedSubview(_header);

            var newDelivery = Controls.PrimaryButton("طلب توصيلة جديدة");
            newDelivery.TouchUpInside += (s, e) => OpenNewDelivery();
            _stack.AddArrangedSubview(newDelivery);

            var available = Controls.OutlineButton("توصيلات متاحة للسائقين");
            available.TouchUpInside += (s, e) => Push(new RestaurantRequestsViewController());
            _stack.AddArrangedSubview(available);

            var mine = Controls.OutlineButton("توصيلات مطعمي");
            mine.TouchUpInside += (s, e) => Push(new RestaurantDeliveryMyOrdersViewController());
            _stack.AddArrangedSubview(mine);

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
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                Dictionary<string, object> restaurant = null;
                List<Dictionary<string, object>> orders = null;
                try { restaurant = Api.MyRestaurant(); } catch { }
                try { orders = Api.GetMyRestaurantDeliveries(); } catch { }
                Ui.BeginOnMain(() => Render(restaurant, orders));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(Dictionary<string, object> restaurant, List<Dictionary<string, object>> orders)
        {
            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 1; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            // أعد زر التحديث والخروج بعد إعادة البناء
            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(refresh);

            if (restaurant != null)
            {
                var name = Tawsela.Data.Row.Str(restaurant, "Name");
                _header.Text = string.IsNullOrEmpty(name) ? "مطعمي" : name;

                var phone = Tawsela.Data.Row.Str(restaurant, "Phone");
                var address = Tawsela.Data.Row.Str(restaurant, "Address");
                if (!string.IsNullOrEmpty(phone)) _stack.AddArrangedSubview(Controls.InfoRow("الهاتف", phone));
                if (!string.IsNullOrEmpty(address)) _stack.AddArrangedSubview(Controls.InfoRow("العنوان", address));
            }
            else
            {
                _header.Text = "لوحة المطعم";
            }

            _stack.AddArrangedSubview(Controls.SectionTitle("توصيلاتي"));

            if (orders == null || orders.Count == 0)
            {
                _stack.AddArrangedSubview(MakeLabel("لا توجد توصيلات حاليًا", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var o in orders) _stack.AddArrangedSubview(Row(o));
        }

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

            // إجراءات حسب الحالة (نفس حالات RestaurantDeliveryExecuteActivity)
            var actions = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            if (string.IsNullOrEmpty(status) || status == "قيد الانتظار")
            {
                var accept = Controls.PrimaryButton("قبول التوصيلة", 42f);
                accept.TouchUpInside += (s, e) => Run(() => Api.AcceptRestaurantDelivery(id));
                actions.AddArrangedSubview(accept);
            }
            if (status == "مخصص للسائق")
            {
                var arrived = Controls.OutlineButton("وصلت للمحل");
                arrived.TouchUpInside += (s, e) => Run(() => Api.ArrivedAtRestaurant(id));
                actions.AddArrangedSubview(arrived);
            }
            if (status == "وصل للمحل")
            {
                var picked = Controls.OutlineButton("تم استلام الطلب");
                picked.TouchUpInside += (s, e) => Run(() => Api.PickedUpRestaurantDelivery(id));
                actions.AddArrangedSubview(picked);
            }
            if (status == "استلمت الطلب")
            {
                var delivered = Controls.PrimaryButton("تم التسليم للعميل", 42f);
                delivered.TouchUpInside += (s, e) => Run(() => Api.DeliveredRestaurantDelivery(id));
                actions.AddArrangedSubview(delivered);
            }

            if (actions.ArrangedSubviews.Length > 0)
            {
                card.AddSubview(actions);
                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(actions, NSLayoutAttribute.Top, NSLayoutRelation.Equal, price, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(actions, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(actions, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(actions, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                });
            }

            // فتح شاشة التنفيذ عند لمس البطاقة — فقط إن لم تكن هناك أزرار داخلية
            // (وإلا فالإيماءة قد تسرق لمس الأزرار).
            if (actions.ArrangedSubviews.Length == 0)
            {
                card.UserInteractionEnabled = true;
                card.AddGestureRecognizer(new UITapGestureRecognizer(
                    () => Push(new RestaurantDeliveryExecuteViewController(id))));
            }

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

        private void Run(Action action)
        {
            var progress = Ui.ShowProgress("جارٍ التنفيذ…");
            Ui.Run(action, () =>
            {
                progress.Dismiss();
                Ui.Toast("تم");
                Load();
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
