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
    /// بديل Activities/NotificationsActivity.cs — قائمة إشعارات المستخدم مع
    /// تعليم الكل كمقروء، وسحب للتحديث.
    /// </summary>
    public class NotificationsViewController : BaseViewController
    {
        private UIScrollView _scroll;
        private UIStackView _stack;
        private UILabel _empty;
        private UIRefreshControl _refresh;

        public NotificationsViewController() : base("الإشعارات") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load(readAll: true);
        }

        private void BuildUi()
        {
            _scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AlwaysBounceVertical = true
            };
            View.AddSubview(_scroll);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            _refresh = new UIRefreshControl();
            _refresh.ValueChanged += (s, e) => Load(false);
            _scroll.RefreshControl = _refresh;

            _stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _scroll.AddSubview(_stack);
            _scroll.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Bottom, 1, -14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Leading, 1, 14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Trailing, 1, -14),
                NSLayoutConstraint.Create(_stack, NSLayoutAttribute.Width, NSLayoutRelation.Equal, _scroll, NSLayoutAttribute.Width, 1, -28),
            });

            _empty = MakeLabel("لا توجد إشعارات", 15f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            _empty.Hidden = true;
            _stack.AddArrangedSubview(_empty);

            // تعليم الكل كمقروء
            var markAll = Controls.OutlineButton("تعليم الكل كمقروء");
            markAll.TouchUpInside += (s, e) =>
            {
                Ui.Run(() => Api.MarkAllRead(), () =>
                {
                    Ui.Toast("تم تعليم الكل كمقروء");
                    Load(false);
                }, ex => Ui.Toast(ex.Message));
            };
            _stack.AddArrangedSubview(markAll);
        }

        private void Load(bool readAll)
        {
            Ui.Run(() =>
            {
                var list = Api.UserNotifications();
                Ui.BeginOnMain(() => Render(list));
            }, () =>
            {
                _refresh.EndRefreshing();
                if (readAll)
                {
                    try { Api.MarkAllRead(); } catch { }
                    try { PushService.ClearBadge(); } catch { }
                }
            }, ex =>
            {
                _refresh.EndRefreshing();
                Ui.Toast(ex.Message);
            });
        }

        private void Render(List<Notification> list)
        {
            // احذف كل الصفوف القديمة (استثناء زر "تعليم الكل" والفراغ)
            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 2; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (list == null || list.Count == 0)
            {
                _empty.Hidden = false;
                return;
            }
            _empty.Hidden = true;

            foreach (var n in list)
            {
                _stack.AddArrangedSubview(Row(n));
            }
        }

        private UIView Row(Notification n)
        {
            var card = Controls.Card();
            card.BackgroundColor = n.IsRead ? Theme.White : Theme.Hex(0xFFF7EF);

            var icon = new UIImageView
            {
                Image = IconFactory.Image("ic_bell", 20f, Theme.Primary),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var title = MakeLabel(n.Title, 15f, Theme.TextPrimary,
                n.IsRead ? UIFontWeight.Regular : UIFontWeight.Semibold, UITextAlignment.Right);
            var body = MakeLabel(n.Body, 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            var time = MakeLabel(n.CreatedAt == default(DateTime) ? "" : n.CreatedAt.ToString("dd/MM HH:mm"),
                11f, Theme.Gray, UIFontWeight.Regular, UITextAlignment.Left);

            card.AddSubview(icon);
            card.AddSubview(title);
            card.AddSubview(body);
            card.AddSubview(time);

            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(icon, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(icon, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -10),
                NSLayoutConstraint.Create(icon, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 20),
                NSLayoutConstraint.Create(icon, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 20),

                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, icon, NSLayoutAttribute.Leading, 1, -8),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                NSLayoutConstraint.Create(body, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(body, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(body, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(time, NSLayoutAttribute.Top, NSLayoutRelation.Equal, body, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(time, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(time, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(time, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -8),
            });

            return card;
        }
    }
}
