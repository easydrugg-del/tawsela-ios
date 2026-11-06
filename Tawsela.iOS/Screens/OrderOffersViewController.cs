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
    /// بديل Activities/OrderOffersActivity.cs — عروض السائقين على طلب بمزايدة.
    /// العميل يرى السعر وبيانات السائق ويقبل العرض المناسب.
    /// </summary>
    public class OrderOffersViewController : ListViewController<OrderOffer>
    {
        private readonly int _orderId;

        public OrderOffersViewController(int orderId) : base("عروض السائقين")
        {
            _orderId = orderId;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // «تحديث العروض» (بديل btnRefresh في أندرويد)
            NavigationItem.RightBarButtonItem = new UIBarButtonItem("تحديث العروض",
                UIBarButtonItemStyle.Plain, (s, e) => Reload());
        }

        protected override string EmptyText
        {
            get { return "لم يصل أي عرض بعد — انتظر قليلًا"; }
        }

        protected override List<OrderOffer> Fetch()
        {
            return Api.GetOffers(_orderId);
        }

        protected override UIView BuildRow(OrderOffer offer)
        {
            var card = Controls.Card();

            var price = MakeLabel(offer.OfferPrice.ToString("0.00") + " ج.م", 18f, Theme.Primary,
                UIFontWeight.Bold, UITextAlignment.Right);
            var driver = MakeLabel(offer.DriverName, 14f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right);
            var car = MakeLabel(offer.CarInfo, 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            var status = MakeLabel(offer.Status, 12f, Theme.Accent,
                UIFontWeight.Medium, UITextAlignment.Left);

            var accept = Controls.PrimaryButton("قبول العرض", 42f);
            accept.TouchUpInside += (s, e) =>
            {
                Ui.Confirm("قبول العرض",
                    "قبول عرض " + offer.DriverName + " بمبلغ " + offer.OfferPrice.ToString("0.00") + " ج.م؟",
                    "قبول", () =>
                    {
                        var progress = Ui.ShowProgress("جارٍ قبول العرض…");
                        Ui.Run(() => Api.AcceptOffer(offer.Id), () =>
                        {
                            progress.Dismiss();
                            Ui.Toast("تم قبول العرض");
                            NavigationController?.PopViewController(true);
                        }, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    });
            };

            card.AddSubview(price);
            card.AddSubview(driver);
            card.AddSubview(car);
            card.AddSubview(status);
            card.AddSubview(accept);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(price, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(price, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(status, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, price, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(status, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(driver, NSLayoutAttribute.Top, NSLayoutRelation.Equal, price, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(driver, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(driver, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(car, NSLayoutAttribute.Top, NSLayoutRelation.Equal, driver, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(car, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(car, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(accept, NSLayoutAttribute.Top, NSLayoutRelation.Equal, car, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(accept, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(accept, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(accept, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            return card;
        }
    }
}
