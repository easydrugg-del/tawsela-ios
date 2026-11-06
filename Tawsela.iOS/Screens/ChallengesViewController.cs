using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/WeeklyChallengesActivity.cs — تحديات الأسبوع وتقدّمها.
    /// </summary>
    public class ChallengesViewController : BaseViewController
    {
        private UILabel _header;
        private UIStackView _list;

        public ChallengesViewController() : base("تحديات الأسبوع") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(MakeLabel("🏅 أكمل التحديات واكسب نقاط إضافية",
                13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            _header = MakeLabel("جارٍ التحميل…", 15f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_header);

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
                List<Dictionary<string, object>> challenges;
                int completed;
                Api.ChallengesInfo(out challenges, out completed);

                Ui.BeginOnMain(() =>
                {
                    _header.Text = "رحلاتك المكتملة هذا الأسبوع: " + completed;
                    Render(challenges);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(List<Dictionary<string, object>> challenges)
        {
            for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _list.ArrangedSubviews[i];
                _list.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (challenges == null || challenges.Count == 0)
            {
                _list.AddArrangedSubview(MakeLabel("لا توجد تحديات حاليًا", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var c in challenges)
            {
                var card = Controls.Card();

                var title = MakeLabel(Row.Str(c, "Title"), 15f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
                var target = Row.Int(c, "Target");
                var progress = Row.Int(c, "Progress");
                var reward = Row.Num(c, "Reward");
                var done = Row.Bool(c, "IsCompleted") || (target > 0 && progress >= target);

                var progressLabel = MakeLabel(target > 0 ? progress + " / " + target : "—",
                    12f, done ? Theme.Green : Theme.Accent, UIFontWeight.Medium, UITextAlignment.Left);

                var bar = new UIProgressView(UIProgressViewStyle.Default)
                {
                    ProgressTintColor = done ? Theme.Green : Theme.Primary,
                    TrackTintColor = Theme.CardStroke,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                if (target > 0) bar.Progress = Math.Min(1f, (float)progress / target);

                var rewardLabel = MakeLabel("المكافأة: " + reward.ToString("0.00") + " ج.م", 12f,
                    Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

                card.AddSubview(title);
                card.AddSubview(progressLabel);
                card.AddSubview(bar);
                card.AddSubview(rewardLabel);

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(progressLabel, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(progressLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                    NSLayoutConstraint.Create(bar, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(bar, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(bar, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(rewardLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, bar, NSLayoutAttribute.Bottom, 1, 6),
                    NSLayoutConstraint.Create(rewardLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(rewardLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(rewardLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                });

                _list.AddArrangedSubview(card);
            }
        }
    }
}
