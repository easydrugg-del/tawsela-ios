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
    /// بديل Activities/OffersActivity.cs — كوبونات الخصم المتاحة للعميل،
    /// مع نسخ الكود ومشاركته.
    /// </summary>
    public class CouponsViewController : ListViewController<Coupon>
    {
        public CouponsViewController() : base("العروض والخصومات") { }

        protected override string EmptyText
        {
            get { return "لا توجد عروض حالياً"; }
        }

        protected override List<Coupon> Fetch()
        {
            return Api.CouponsList();
        }

        protected override UIView BuildRow(Coupon c)
        {
            var card = Controls.Card();

            var code = MakeLabel(c.Code, 18f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            var value = MakeLabel(
                c.Type == "percent"
                    ? "خصم " + c.Value.ToString("0.#") + "%" + (c.MaxOff > 0 ? " حتى " + c.MaxOff.ToString("0") + " ج.م" : "")
                    : "خصم " + c.Value.ToString("0.00") + " ج.م",
                14f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);

            var expiry = MakeLabel(
                c.IsExpired ? "منتهي الصلاحية"
                            : (c.ExpiresAt == default(DateTime) ? "بدون تاريخ انتهاء" : "ينتهي " + c.ExpiresAt.ToString("dd/MM/yyyy")),
                12f, c.IsExpired ? Theme.Red : Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            card.AddSubview(code);
            card.AddSubview(value);
            card.AddSubview(expiry);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(code, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(code, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(code, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(value, NSLayoutAttribute.Top, NSLayoutRelation.Equal, code, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(value, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(value, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(expiry, NSLayoutAttribute.Top, NSLayoutRelation.Equal, value, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(expiry, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(expiry, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(expiry, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            // نسخ الكود عند اللمس (بديل النسخ إلى الحافظة)
            card.UserInteractionEnabled = true;
            card.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                try
                {
                    UIPasteboard.General.String = c.Code;
                    Ui.Toast("تم نسخ الكود: " + c.Code);
                }
                catch { }
            }));

            return card;
        }
    }
}
