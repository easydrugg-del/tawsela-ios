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
    /// بديل Activities/TopUpHistoryActivity.cs — سجل تأكيدات الدفع.
    /// الأدمن يرى كل الطلبات (AllTopUps)، والمستخدم يرى طلباته فقط (MyTopUps).
    /// </summary>
    public class TopUpHistoryViewController : ListViewController<Dictionary<string, object>>
    {
        private readonly bool _admin;

        public TopUpHistoryViewController() : base("سجل تأكيدات الدفع")
        {
            _admin = Session.IsAdmin;
        }

        /// <summary>true = سجل الإدارة لكل الطلبات، false = سجل المستخدم.</summary>
        public TopUpHistoryViewController(bool admin)
            : base(admin ? "سجل تأكيدات الدفع" : "سجل شحن الرصيد")
        {
            _admin = admin;
        }

        protected override string EmptyText
        {
            get { return "لا يوجد سجل بعد"; }
        }

        protected override string Subtitle
        {
            get { return "من اعتمد كل شحنة ومتى"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return _admin ? Api.AllTopUps() : Api.MyTopUps();
        }

        protected override UIView BuildRow(Dictionary<string, object> t)
        {
            var card = Controls.Card();

            var amount = MakeLabel(Row.Num(t, "Amount").ToString("0.00") + " ج.م",
                16f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);

            var status = Row.Str(t, "Status");
            var confirmed = status == "مؤكد" || status == "مقبول" || status == "مكتمل";

            var statusLabel = MakeLabel(string.IsNullOrEmpty(status) ? "قيد المراجعة" : status,
                12f,
                confirmed ? Theme.Green
                    : status == "مرفوض" ? Theme.Red : Theme.Accent,
                UIFontWeight.Medium, UITextAlignment.Left);

            var header = new UIStackView(new UIView[] { amount, statusLabel })
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Distribution = UIStackViewDistribution.EqualSpacing,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var inner = new UIStackView { Axis = UILayoutConstraintAxis.Vertical, Spacing = 4, TranslatesAutoresizingMaskIntoConstraints = false };
            inner.AddArrangedSubview(header);

            // اسم صاحب الطلب (يظهر في سجل الإدارة)
            var user = Row.Str(t, "UserName");
            if (!string.IsNullOrEmpty(user))
            {
                inner.AddArrangedSubview(MakeLabel("العميل: " + user, 13f,
                    Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right));
            }

            var sender = Row.Str(t, "SenderPhone");
            var reference = Row.Str(t, "ReceiptRef");
            inner.AddArrangedSubview(MakeLabel(
                (string.IsNullOrEmpty(sender) ? "" : "من: " + sender) +
                (string.IsNullOrEmpty(reference) ? "" : " · عملية: " + reference),
                13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            // تاريخ + مَن اعتمد الطلب (بديل ConfirmedBy/ConfirmedAt في أندرويد)
            var by = Row.Str(t, "ConfirmedBy");
            var at = Row.Date(t, "ConfirmedAt");
            var footer = Row.Date(t, "CreatedAt");

            if (confirmed && (!string.IsNullOrEmpty(by) || !string.IsNullOrEmpty(at)))
            {
                footer += " · اعتُمد بواسطة: " + (string.IsNullOrEmpty(by) ? "؟" : by) +
                          (string.IsNullOrEmpty(at) ? "" : " — " + at);
            }
            else if (!confirmed)
            {
                footer += " · بانتظار البت";
            }

            inner.AddArrangedSubview(MakeLabel(footer, 11f, Theme.Gray,
                UIFontWeight.Regular, UITextAlignment.Right));

            card.AddSubview(inner);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(inner, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(inner, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(inner, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(inner, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            // فتح تفاصيل الطلب (إيصال + تأكيد/رفض) للأدمن فقط
            if (_admin)
            {
                card.UserInteractionEnabled = true;
                card.AddGestureRecognizer(new UITapGestureRecognizer(
                    () => Push(new TopUpDetailViewController(t, Reload))));
            }

            return card;
        }
    }
}
