using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/LoyaltyActivity.cs — نقاط الولاء ومدخلات المكافآت
    /// (الكوبونات، التحديات، متجر الهدايا، الإنجازات).
    /// </summary>
    public class LoyaltyViewController : BaseViewController
    {
        private UILabel _points;

        public LoyaltyViewController() : base("نقاط الولاء") { }

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
            var stack = CreateScrollContent(14f, 16f);

            var card = Controls.Card();
            card.BackgroundColor = Theme.Primary;

            var caption = MakeLabel("نقاطك الحالية", 12f, UIColor.White.ColorWithAlpha(0.9f),
                UIFontWeight.Regular, UITextAlignment.Center);
            _points = MakeLabel("—", 34f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);

            card.AddSubview(caption);
            card.AddSubview(_points);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(caption, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(caption, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(caption, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_points, NSLayoutAttribute.Top, NSLayoutRelation.Equal, caption, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_points, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_points, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_points, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -16),
            });
            stack.AddArrangedSubview(card);

            stack.AddArrangedSubview(MakeLabel("استبدل نقاطك بمكافآت من الأقسام التالية:",
                14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            var row1 = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 12,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            row1.AddArrangedSubview(Controls.Tile("ic_ticket", "الكوبونات", Theme.Accent, Theme.PrimaryDark,
                () => Push(new CouponsViewController())));
            row1.AddArrangedSubview(Controls.Tile("ic_trophy", "التحديات", Theme.Blue, Theme.PrimaryDark,
                () => Push(new ChallengesViewController())));
            row1.HeightAnchor.ConstraintEqualTo(96).Active = true;
            stack.AddArrangedSubview(row1);

            var row2 = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 12,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            row2.AddArrangedSubview(Controls.Tile("ic_gift", "متجر الهدايا", Theme.Green, Theme.PrimaryDark,
                () => Push(new GiftShopViewController())));
            row2.AddArrangedSubview(Controls.Tile("ic_crown", "الإنجازات", Theme.Red, Theme.PrimaryDark,
                () => Push(new AchievementsViewController())));
            row2.HeightAnchor.ConstraintEqualTo(96).Active = true;
            stack.AddArrangedSubview(row2);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var points = Api.LoyaltyPoints();
                Ui.BeginOnMain(() => _points.Text = points.ToString());
            }, null, ex => AppServices.Log("TawselaLoyalty", ex.Message));
        }
    }
}
