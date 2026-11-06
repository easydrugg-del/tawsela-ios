using System;
using System.Collections.Generic;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/DriverOrdersActivity.cs + قائمة طلبات العميل:
    /// قائمة الطلبات حسب النطاق (عميل/سائق) والحالة، مع بحث وتصفية بالتاريخ
    /// (بديل txtSearch + btnDateFrom/btnDateTo في أندرويد).
    /// </summary>
    public class OrdersViewController : BaseViewController
    {
        private readonly string _scope;
        private readonly string _emptyText;
        private readonly string _status;
        private UIScrollView _scroll;
        private UIStackView _stack;
        private UIRefreshControl _refresh;
        private UILabel _empty;

        private UITextField _search;
        private UIButton _btnFrom, _btnTo;
        private List<Order> _all = new List<Order>();
        private DateTime? _dateFrom, _dateTo;

        /// <summary>scope: "customer" أو "driver". status: "" لكل الحالات.</summary>
        public OrdersViewController(string scope, string status, string title, string emptyText = null)
            : base(title ?? "الطلبات")
        {
            _scope = scope;
            _status = status ?? "";
            _emptyText = string.IsNullOrEmpty(emptyText) ? "لا توجد طلبات حاليًا" : emptyText;
        }

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
            /* ---------- شريط الفلاتر ---------- */
            var filters = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            View.AddSubview(filters);

            _search = Controls.TextField("بحث برقم الطلب أو العنوان أو الحالة");
            _search.EditingChanged += (s, e) => ApplyFilter();
            _search.ReturnKeyType = UIReturnKeyType.Search;
            filters.AddSubview(_search);

            _btnFrom = Controls.OutlineButton("من: —", null, 38f);
            _btnFrom.TouchUpInside += (s, e) => PickDate(true);

            _btnTo = Controls.OutlineButton("إلى: —", null, 38f);
            _btnTo.TouchUpInside += (s, e) => PickDate(false);

            var clear = Controls.OutlineButton("مسح", Theme.Gray, 38f);
            clear.TouchUpInside += (s, e) =>
            {
                _dateFrom = null;
                _dateTo = null;
                _search.Text = "";
                _btnFrom.SetTitle("من: —", UIControlState.Normal);
                _btnTo.SetTitle("إلى: —", UIControlState.Normal);
                ApplyFilter();
            };

            var row = new UIStackView(new UIView[] { _btnFrom, _btnTo, clear })
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            filters.AddSubview(row);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(filters, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(filters, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(filters, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            filters.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_search, NSLayoutAttribute.Top, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(_search, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Leading, 1, 14),
                NSLayoutConstraint.Create(_search, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Trailing, 1, -14),

                NSLayoutConstraint.Create(row, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _search, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Leading, 1, 14),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Trailing, 1, -14),
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Bottom, 1, -8),
            });

            /* ---------- القائمة ---------- */
            _scroll = new UIScrollView { TranslatesAutoresizingMaskIntoConstraints = false, AlwaysBounceVertical = true };
            View.AddSubview(_scroll);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, filters, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            _refresh = new UIRefreshControl();
            _refresh.ValueChanged += (s, e) => Load();
            _scroll.RefreshControl = _refresh;

            _stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _scroll.AddSubview(_stack);
            _scroll.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Bottom, 1, -14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Leading, 1, 14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Trailing, 1, -14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Width, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Width, 1, -28),
            });

            _empty = MakeLabel(_emptyText, 15f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            _empty.Hidden = true;
            _stack.AddArrangedSubview(_empty);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var list = Api.GetOrders(_status, _scope);
                Ui.BeginOnMain(() =>
                {
                    _all = list ?? new List<Order>();
                    ApplyFilter();
                });
            }, () => _refresh.EndRefreshing(), ex =>
            {
                _refresh.EndRefreshing();
                Ui.Toast(ex.Message);
            });
        }

        /// <summary>اختيار تاريخ من/إلى (بديل DatePickerDialog).</summary>
        private void PickDate(bool isFrom)
        {
            var current = isFrom ? (_dateFrom ?? DateTime.Today) : (_dateTo ?? DateTime.Today);

            Push(new DatePickViewController(isFrom ? "من تاريخ" : "إلى تاريخ", current, picked =>
            {
                if (isFrom)
                {
                    _dateFrom = picked;
                    _btnFrom.SetTitle("من: " + picked.ToString("dd/MM/yyyy"), UIControlState.Normal);
                }
                else
                {
                    _dateTo = picked;
                    _btnTo.SetTitle("إلى: " + picked.ToString("dd/MM/yyyy"), UIControlState.Normal);
                }

                ApplyFilter();
            }));
        }

        /// <summary>تطبيق البحث والتاريخ على القائمة الكاملة (بديل ApplyFilter).</summary>
        private void ApplyFilter()
        {
            var query = (_search == null ? "" : (_search.Text ?? "")).Trim();

            var filtered = new List<Order>();
            foreach (var o in _all)
            {
                if (o == null) continue;

                if (query != "")
                {
                    var hit =
                        (o.OrderNumber ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (o.FromAddress ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (o.ToAddress ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (o.StatusText ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (o.CustomerName ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (o.DriverName ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!hit) continue;
                }

                if (_dateFrom.HasValue || _dateTo.HasValue)
                {
                    // نفس أندرويد: تاريخ الإكمال إن وُجد وإلا تاريخ الإنشاء
                    var date = o.CompletedAt != default(DateTime) ? o.CompletedAt : o.CreatedAt;
                    if (_dateFrom.HasValue && date.Date < _dateFrom.Value.Date) continue;
                    if (_dateTo.HasValue && date.Date > _dateTo.Value.Date) continue;
                }

                filtered.Add(o);
            }

            Render(filtered);
        }

        private void Render(List<Order> list)
        {
            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (list == null || list.Count == 0)
            {
                _empty.Text = _all.Count == 0 ? _emptyText : "لا توجد طلبات مطابقة للبحث";
                _empty.Hidden = false;
                _stack.AddArrangedSubview(_empty);
                return;
            }

            foreach (var o in list) _stack.AddArrangedSubview(Row(o));
        }

        private UIView Row(Order o)
        {
            var card = Controls.Card();

            var number = MakeLabel(o.OrderNumber, 13f, Theme.TextSecondary, UIFontWeight.Medium, UITextAlignment.Right);
            var status = MakeLabel(o.StatusText, 12f, Theme.FromArgb(o.StatusColor), UIFontWeight.Semibold, UITextAlignment.Left);
            var from = MakeLabel("من: " + o.FromAddress, 14f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right);
            var to = MakeLabel("إلى: " + o.ToAddress, 14f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right);

            var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
            var price = MakeLabel(fare.ToString("0.00") + " ج.م", 15f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Left);

            var date = MakeLabel(o.CreatedAt == default(DateTime) ? "" : o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                11f, Theme.Gray, UIFontWeight.Regular, UITextAlignment.Right);

            card.AddSubview(number);
            card.AddSubview(status);
            card.AddSubview(from);
            card.AddSubview(to);
            card.AddSubview(price);
            card.AddSubview(date);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(number, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(number, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(status, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, number, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(status, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(from, NSLayoutAttribute.Top, NSLayoutRelation.Equal, number, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(from, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(from, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(to, NSLayoutAttribute.Top, NSLayoutRelation.Equal, from, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(to, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(to, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(price, NSLayoutAttribute.Top, NSLayoutRelation.Equal, to, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(price, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(date, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, price, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(date, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(price, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            // فتح التفاصيل حسب الدور
            var tap = new UITapGestureRecognizer(() =>
            {
                if (Session.IsDriver) Push(new OrderDetailsViewController(o.Id));
                else Push(new TrackOrderViewController(o.Id));
            });
            card.UserInteractionEnabled = true;
            card.AddGestureRecognizer(tap);

            return card;
        }
    }

    /// <summary>اختيار تاريخ فقط (بديل DatePickerDialog في فلتر الطلبات).</summary>
    public class DatePickViewController : BaseViewController
    {
        private readonly Action<DateTime> _onPicked;
        private readonly UIDatePicker _picker;

        public DatePickViewController(string title, DateTime initial, Action<DateTime> onPicked) : base(title)
        {
            _onPicked = onPicked;
            _picker = new UIDatePicker
            {
                Mode = UIDatePickerMode.Date,
                PreferredDatePickerStyle = UIDatePickerStyle.Wheels,
                Date = (NSDate)initial,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var stack = CreateScrollContent(12f, 16f);
            stack.AddArrangedSubview(_picker);

            var confirm = Controls.PrimaryButton("تأكيد التاريخ");
            confirm.TouchUpInside += (s, e) =>
            {
                var picked = (DateTime)_picker.Date;
                var callback = _onPicked;

                if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                    NavigationController.PopViewController(true);

                if (callback != null) callback(picked);
            };
            stack.AddArrangedSubview(confirm);
        }
    }
}
