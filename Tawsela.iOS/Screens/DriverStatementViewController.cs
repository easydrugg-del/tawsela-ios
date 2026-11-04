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

        public DriverStatementViewController() : base("كشف الحساب")
        {
            _driverId = 0;
            _driverName = "";
        }

        /// <summary>كشف حساب سائق محدد من لوحة الإدارة (بديل وضع الأدمن في أندرويد).</summary>
        public DriverStatementViewController(int driverId, string driverName)
            : base(string.IsNullOrWhiteSpace(driverName) ? "كشف الحساب" : "كشف حساب " + driverName)
        {
            _driverId = driverId;
            _driverName = driverName;
        }

        protected override string EmptyText
        {
            get { return _driverId > 0 ? "لا توجد حركات على حساب هذا السائق" : "لا توجد حركات على حسابك بعد"; }
        }

        protected override List<WalletTxn> Fetch()
        {
            return Api.DriverTxns(_driverId > 0 ? _driverId : Session.UserId);
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
        public DriverStatementPickerViewController() : base("كشف حساب سائق") { }

        protected override string EmptyText
        {
            get { return "لا يوجد سائقون"; }
        }

        protected override List<Driver> Fetch()
        {
            return Api.AllDrivers();
        }

        protected override UIView BuildRow(Driver d)
        {
            var row = Controls.InfoRow(d.FullName,
                (string.IsNullOrEmpty(d.DriverNumber) ? "" : "رقم: " + d.DriverNumber + " · ") +
                (string.IsNullOrEmpty(d.CarPlate) ? "" : d.CarPlate + "\n") +
                d.Phone);

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(
                () => Push(new DriverStatementViewController(d.UserId, d.FullName))));

            return row;
        }
    }
}
