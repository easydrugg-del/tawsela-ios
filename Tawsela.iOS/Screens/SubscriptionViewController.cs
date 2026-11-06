using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/SubscriptionActivity.cs — اشتراك السائق الشهري/السنوي
    /// مع نفس النصوص والحساب (السنوي = 10 أشهر).
    /// </summary>
    public class SubscriptionViewController : BaseViewController
    {
        private UILabel _info;
        private UIStackView _history;

        public SubscriptionViewController()
            : base(Session.IsDriver ? "اشتراك السائق VIP" : "الاشتراك VIP") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Refresh();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(14f, 18f);

            var card = Controls.Card();
            _info = MakeLabel("جارٍ التحميل…", 15f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right);
            card.AddSubview(_info);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 12),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -12),
            });
            stack.AddArrangedSubview(card);

            stack.AddArrangedSubview(Controls.SectionTitle("سجل الاشتراكات"));
            _history = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_history);

            var monthly = Controls.PrimaryButton("اشتراك شهري");
            monthly.TouchUpInside += (s, e) => Subscribe("شهري");
            stack.AddArrangedSubview(monthly);

            var yearly = Controls.OutlineButton("اشتراك سنوي (شهران مجانًا)");
            yearly.TouchUpInside += (s, e) => Subscribe("سنوي");
            stack.AddArrangedSubview(yearly);

            var cancel = Controls.DangerButton("إلغاء الاشتراك");
            cancel.TouchUpInside += (s, e) =>
            {
                Ui.Confirm("إلغاء الاشتراك", "هل تريد إلغاء اشتراكك الحالي؟", "إلغاء الاشتراك", () =>
                {
                    var progress = Ui.ShowProgress("جارٍ الإلغاء…");
                    Ui.Run(() => Api.CancelSubscription(Session.UserId), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم إلغاء الاشتراك");
                        Refresh();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            };
            stack.AddArrangedSubview(cancel);
        }

        private void Refresh()
        {
            Ui.Run(() =>
            {
                decimal price;
                int discount;
                bool active;
                System.Collections.Generic.List<Tawsela.Models.Subscription> history;
                Api.SubscriptionInfoFull(out price, out discount, out active, out history);

                Ui.BeginOnMain(() =>
                {
                    _info.Text =
                        (active ? "✓ اشتراكك نشط" : "لا يوجد اشتراك نشط") + "\n" +
                        "السعر الشهري: " + price.ToString("0.00") + " ج.م\n" +
                        "الخصم: " + discount + "% على كل رحلة\n" +
                        "السنوي: " + (price * 10).ToString("0.00") + " ج.م (شهران مجانًا)";
                    _info.TextColor = active ? Theme.Green : Theme.TextPrimary;

                    for (int i = _history.ArrangedSubviews.Length - 1; i >= 0; i--)
                    {
                        var v = _history.ArrangedSubviews[i];
                        _history.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    if (history == null || history.Count == 0)
                    {
                        _history.AddArrangedSubview(MakeLabel("لا يوجد سجل اشتراكات بعد", 13f,
                            Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                        return;
                    }

                    foreach (var h in history)
                    {
                        _history.AddArrangedSubview(Controls.InfoRow(
                            (string.IsNullOrEmpty(h.Plan) ? "اشتراك" : h.Plan) + " · " + h.Price.ToString("0.00") + " ج.م",
                            h.StartAt.ToString("dd/MM/yyyy") + " ← " + h.ExpiresAt.ToString("dd/MM/yyyy") +
                            " · " + h.StatusDisplay));
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Subscribe(string plan)
        {
            var progress = Ui.ShowProgress("جارٍ الاشتراك…");
            Ui.Run(() => Api.Subscribe(plan), () =>
            {
                progress.Dismiss();
                Ui.Toast("تم الاشتراك بنجاح: " + plan);
                Refresh();
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
