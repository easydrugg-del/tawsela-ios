using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/GiftShopActivity.cs — استبدال نقاط الولاء بهدايا
    /// (Api.GiftsInfo / Api.RedeemGift).
    /// </summary>
    public class GiftShopViewController : BaseViewController
    {
        private UILabel _points;
        private UIStackView _list;

        public GiftShopViewController() : base("متجر الهدايا") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            _points = MakeLabel("نقاطك: —", 16f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            stack.AddArrangedSubview(_points);

            _list = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_list);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                int points;
                List<Dictionary<string, object>> gifts;
                Api.GiftsInfo(out points, out gifts);

                Ui.BeginOnMain(() =>
                {
                    _points.Text = "نقاطك: " + points;
                    Render(gifts, points);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(List<Dictionary<string, object>> gifts, int points)
        {
            for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _list.ArrangedSubviews[i];
                _list.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (gifts == null || gifts.Count == 0)
            {
                _list.AddArrangedSubview(MakeLabel("لا توجد هدايا متاحة حاليًا", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var g in gifts)
            {
                var card = Controls.Card();
                var id = Row.Int(g, "Id");
                var title = MakeLabel(Row.Str(g, "Title"), 15f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
                var cost = Row.Int(g, "Points");
                var desc = MakeLabel(Row.Str(g, "Description"), 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

                var costLabel = MakeLabel(cost + " نقطة", 12f,
                    points >= cost ? Theme.Green : Theme.Red, UIFontWeight.Medium, UITextAlignment.Left);

                var redeem = Controls.OutlineButton(points >= cost ? "استبدال" : "نقاط غير كافية");
                redeem.Enabled = points >= cost;
                redeem.TouchUpInside += (s, e) =>
                {
                    Ui.Confirm("تأكيد الاستبدال",
                        "سيتم خصم " + cost + " نقطة مقابل: " + Row.Str(g, "Title"), "استبدال", () =>
                        {
                            var progress = Ui.ShowProgress("جارٍ الاستبدال…");
                            Ui.Run(() => Api.RedeemGift(id), () =>
                            {
                                progress.Dismiss();
                                Ui.Toast("تم الاستبدال بنجاح");
                                Load();
                            }, ex =>
                            {
                                progress.Dismiss();
                                Ui.Toast(ex.Message);
                            });
                        });
                };

                card.AddSubview(title);
                card.AddSubview(costLabel);
                card.AddSubview(desc);
                card.AddSubview(redeem);

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(costLabel, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(costLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                    NSLayoutConstraint.Create(desc, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 4),
                    NSLayoutConstraint.Create(desc, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(desc, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(redeem, NSLayoutAttribute.Top, NSLayoutRelation.Equal, desc, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(redeem, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(redeem, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(redeem, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                });

                _list.AddArrangedSubview(card);
            }
        }
    }
}
