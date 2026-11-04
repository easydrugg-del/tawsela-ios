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
    /// بديل Activities/AchievementsActivity.cs — إنجازات المستخدم (Api.MyAchievements).
    /// </summary>
    public class AchievementsViewController : ListViewController<Dictionary<string, object>>
    {
        public AchievementsViewController() : base("إنجازاتي") { }

        protected override string EmptyText
        {
            get { return "لا توجد إنجازات بعد — أكمل رحلاتك لتفتحها"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.MyAchievements();
        }

        protected override UIView BuildRow(Dictionary<string, object> item)
        {
            var card = Controls.Card();

            var title = MakeLabel(Row.Str(item, "Title"), 15f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            var desc = MakeLabel(Row.Str(item, "Description"), 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            var done = Row.Bool(item, "IsCompleted") || Row.Bool(item, "Completed");
            var progress = Row.Int(item, "Progress");
            var target = Row.Int(item, "Target");

            var badge = MakeLabel(done ? "مكتمل" : (target > 0 ? progress + "/" + target : "قيد التقدم"),
                11f, done ? Theme.Green : Theme.Accent, UIFontWeight.Bold, UITextAlignment.Left);

            card.AddSubview(title);
            card.AddSubview(desc);
            card.AddSubview(badge);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(badge, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(badge, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(desc, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(desc, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(desc, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(desc, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            return card;
        }
    }
}
