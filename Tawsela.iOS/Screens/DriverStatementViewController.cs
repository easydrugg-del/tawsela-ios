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
    /// بديل Activities/DriverStatementActivity.cs — كشف حساب السائق
    /// (Api.DriverTxns لنفس المستخدم الحالي).
    /// </summary>
    public class DriverStatementViewController : ListViewController<WalletTxn>
    {
        private readonly int _driverId;
        private readonly string _driverName;
        private readonly DateTime? _from, _to;

        public DriverStatementViewController() : base("كشف الحساب")
        {
            _driverId = 0;
            _driverName = "";
        }

        /// <summary>كشف حساب سائق محدد من لوحة الإدارة (بديل وضع الأدمن في أندرويد).</summary>
        public DriverStatementViewController(int driverId, string driverName,
            DateTime? from = null, DateTime? to = null)
            : base(string.IsNullOrWhiteSpace(driverName) ? "كشف الحساب" : "كشف حساب " + driverName)
        {
            _driverId = driverId;
            _driverName = driverName;
            _from = from;
            _to = to;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // الرجوع لشاشة البحث (بديل btnBackSearch)
            if (_driverId > 0)
            {
                NavigationItem.RightBarButtonItem = new UIBarButtonItem("🔙 بحث جديد",
                    UIBarButtonItemStyle.Plain, (s, e) => NavigationController?.PopViewController(true));
            }
        }

        protected override string EmptyText
        {
            get { return _driverId > 0 ? "لا توجد حركات على حساب هذا السائق" : "لا توجد حركات على حسابك بعد"; }
        }

        protected override List<WalletTxn> Fetch()
        {
            var list = Api.DriverTxns(_driverId > 0 ? _driverId : Session.UserId);

            // فلترة المدة المختارة في شاشة البحث (بديل btnFromDate/btnToDate)
            if (list != null && (_from.HasValue || _to.HasValue))
            {
                list = list.FindAll(t =>
                    (!_from.HasValue || t.CreatedAt >= _from.Value.Date) &&
                    (!_to.HasValue || t.CreatedAt < _to.Value.Date.AddDays(1)));
            }

            return list;
        }

        protected override UIView BuildRow(WalletTxn t)
        {
            var card = Controls.Card();

            var amount = MakeLabel(t.AmountText, 16f, t.Amount >= 0 ? Theme.Green : Theme.Red,
                UIFontWeight.Bold, UITextAlignment.Right);
            var type = MakeLabel(string.IsNullOrEmpty(t.Type) ? "حركة" : t.Type, 13f,
                Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
            var note = MakeLabel(t.Note, 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            var date = MakeLabel(t.CreatedAt == default(DateTime) ? "" : t.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                11f, Theme.Gray, UIFontWeight.Regular, UITextAlignment.Right);

            card.AddSubview(amount);
            card.AddSubview(type);
            card.AddSubview(note);
            card.AddSubview(date);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(amount, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(amount, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(type, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, amount, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(type, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(note, NSLayoutAttribute.Top, NSLayoutRelation.Equal, amount, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(note, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(note, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(date, NSLayoutAttribute.Top, NSLayoutRelation.Equal, note, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(date, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(date, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(date, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            return card;
        }
    }

    /// <summary>
    /// اختيار سائق لعرض كشف حسابه من لوحة الإدارة
    /// (بديل قائمة البحث في DriverStatementActivity).
    /// </summary>
    public class DriverStatementPickerViewController : ListViewController<Driver>
    {
        private UITextField _query;
        private UIButton _btnFrom, _btnTo;
        private DateTime? _from, _to;

        public DriverStatementPickerViewController() : base("📒 كشف حساب سائق") { }

        protected override string EmptyText
        {
            get
            {
                return (_query != null && !string.IsNullOrWhiteSpace(_query.Text))
                    ? "لا يوجد سائق مطابق للبحث"
                    : "ابحث برقم الحساب أو السيارة أو الاسم";
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildSearchBar();
        }

        /// <summary>شريط البحث والتواريخ (بديل laySearch في activity_driver_statement).</summary>
        private void BuildSearchBar()
        {
            const float headerHeight = 142f;

            var header = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 6,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(header);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, headerHeight),
            });

            _query = Controls.TextField("رقم الحساب / رقم السيارة / اسم السائق");
            _query.ReturnKeyType = UIReturnKeyType.Search;
            _query.EditingChanged += (s, e) => Reload();
            header.AddArrangedSubview(_query);

            var dates = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _btnFrom = Controls.OutlineButton("من: —", null, 36f);
            _btnFrom.TouchUpInside += (s, e) => PickDate(true);
            dates.AddArrangedSubview(_btnFrom);

            _btnTo = Controls.OutlineButton("إلى: —", null, 36f);
            _btnTo.TouchUpInside += (s, e) => PickDate(false);
            dates.AddArrangedSubview(_btnTo);

            var clear = Controls.OutlineButton("🧹 الكل", null, 36f);
            clear.TouchUpInside += (s, e) =>
            {
                _query.Text = "";
                _from = null;
                _to = null;
                _btnFrom.SetTitle("من: —", UIControlState.Normal);
                _btnTo.SetTitle("إلى: —", UIControlState.Normal);
                Reload();
            };
            dates.AddArrangedSubview(clear);

            header.AddArrangedSubview(dates);

            var search = Controls.PrimaryButton("🔍 بحث", 38f);
            search.TouchUpInside += (s, e) => Reload();
            header.AddArrangedSubview(search);

            List.TopInset = headerHeight + 8;
        }

        private void PickDate(bool isFrom)
        {
            var initial = isFrom
                ? (_from ?? DateTime.Today)
                : (_to ?? DateTime.Today);

            Push(new DatePickViewController(isFrom ? "من تاريخ" : "إلى تاريخ", initial, picked =>
            {
                if (isFrom)
                {
                    _from = picked;
                    _btnFrom.SetTitle("من: " + picked.ToString("dd/MM/yyyy"), UIControlState.Normal);
                }
                else
                {
                    _to = picked;
                    _btnTo.SetTitle("إلى: " + picked.ToString("dd/MM/yyyy"), UIControlState.Normal);
                }

                Reload();
            }));
        }

        protected override List<Driver> Fetch()
        {
            var query = (_query == null ? "" : (_query.Text ?? "")).Trim();
            return query.Length == 0 ? Api.AllDrivers() : Api.SearchDrivers(query);
        }

        protected override UIView BuildRow(Driver d)
        {
            var row = Controls.InfoRow(d.FullName,
                (string.IsNullOrEmpty(d.DriverNumber) ? "" : "رقم: " + d.DriverNumber + " · ") +
                (string.IsNullOrEmpty(d.CarPlate) ? "" : d.CarPlate + "\n") +
                d.Phone);

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(
                () => Push(new DriverStatementViewController(d.UserId, d.FullName, _from, _to))));

            return row;
        }
    }
}
