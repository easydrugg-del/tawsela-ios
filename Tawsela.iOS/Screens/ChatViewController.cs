using System;
using System.Collections.Generic;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/ChatActivity.cs — محادثة الطلب بين العميل والسائق
    /// مع إبلاغ وحظر (متطلب UGC في المتاجر).
    /// </summary>
    public class ChatViewController : BaseViewController
    {
        private readonly int _orderId;
        private UITableView _table;
        private UITextField _input;
        private UIButton _send;
        private List<Message> _messages = new List<Message>();
        private NSTimer _timer;
        private UIView _composer;
        private nfloat _composerBottom;

        public ChatViewController(int orderId) : base("محادثة الطلب")
        {
            _orderId = orderId;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            _timer = NSTimer.CreateRepeatingScheduledTimer(4.0, _ => Load());

            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillChangeFrameNotification, OnKeyboard);
            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardHidden);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (_timer != null) { _timer.Invalidate(); _timer.Dispose(); _timer = null; }
            NSNotificationCenter.DefaultCenter.RemoveObserver(this);
        }

        private void BuildUi()
        {
            _table = new UITableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = Theme.Surface,
                SeparatorStyle = UITableViewCellSeparatorStyle.None,
                RowHeight = UITableView.AutomaticDimension,
                EstimatedRowHeight = 60
            };
            _table.DataSource = new ChatSource(this);
            View.AddSubview(_table);

            _composer = new UIView { BackgroundColor = Theme.White, TranslatesAutoresizingMaskIntoConstraints = false };
            View.AddSubview(_composer);

            _input = Controls.TextField("اكتب رسالتك...", false, 44f);
            _composer.AddSubview(_input);

            _send = Controls.PrimaryButton("إرسال", 44f);
            _send.WidthAnchor.ConstraintEqualTo(80).Active = true;
            _send.TouchUpInside += (s, e) => Send();
            _composer.AddSubview(_send);

            _composerBottom = 0;

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_composer, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_composer, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(_composer, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_composer, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 60),

                NSLayoutConstraint.Create(_input, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _composer, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_input, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _composer, NSLayoutAttribute.CenterY, 1, 0),

                NSLayoutConstraint.Create(_send, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _input, NSLayoutAttribute.Trailing, 1, 8),
                NSLayoutConstraint.Create(_send, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _composer, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_send, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _composer, NSLayoutAttribute.CenterY, 1, 0),

                NSLayoutConstraint.Create(_table, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_table, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _composer, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_table, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_table, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            NavigationItem.RightBarButtonItem = new UIBarButtonItem("إبلاغ", UIBarButtonItemStyle.Plain, (s, e) => Report());
        }

        private void OnKeyboard(NSNotification notification)
        {
            var info = notification.UserInfo;
            if (info == null) return;
            var frame = (info[UIKeyboard.FrameEndUserInfoKey] as NSValue).CGRectValue;
            var overlap = View.Bounds.Height - frame.Y;
            if (overlap <= 0) return;

            _composerBottom = overlap;
            UIView.Animate(0.25, () =>
            {
                foreach (var c in View.Constraints)
                {
                    // ارفع شريط الكتابة فوق الكيبورد
                }
                _composer.Transform = CoreGraphics.CGAffineTransform.MakeTranslation(0, -overlap);
            });
        }

        private void OnKeyboardHidden(NSNotification notification)
        {
            _composerBottom = 0;
            UIView.Animate(0.25, () => _composer.Transform = CoreGraphics.CGAffineTransform.MakeIdentity());
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var list = Api.GetMessages(_orderId);
                Ui.BeginOnMain(() =>
                {
                    _messages = list ?? new List<Message>();
                    _table.ReloadData();
                    ScrollToBottom();
                    try { Api.MarkMessagesRead(_orderId); } catch { }
                });
            }, null, ex => AppServices.Log("TawselaChat", ex.Message));
        }

        private void ScrollToBottom()
        {
            if (_messages.Count == 0) return;
            var index = NSIndexPath.FromRowSection(_messages.Count - 1, 0);
            try { _table.ScrollToRow(index, UITableViewScrollPosition.Bottom, true); } catch { }
        }

        private void Send()
        {
            var text = (_input.Text ?? "").Trim();
            if (text == "") return;
            _input.Text = "";
            _input.ResignFirstResponder();

            Ui.Run(() => Api.SendMessage(_orderId, text), () => Load(), ex => Ui.Toast(ex.Message));
        }

        private void Report()
        {
            Ui.Choose("خيارات المحادثة", new[] { "إبلاغ عن المحادثة", "حظر السائق" }, index =>
            {
                if (index == 1) { BlockDriver(); return; }

                Ui.Prompt("إبلاغ عن المحادثة", "اكتب سبب الإبلاغ", "", "السبب", reason =>
                {
                    if (string.IsNullOrWhiteSpace(reason)) return;
                    Ui.Run(() => Api.AddComplaint(_orderId, "إبلاغ عن محادثة طلب", reason), () =>
                    {
                        Ui.Toast("تم إرسال الإبلاغ");
                    }, ex => Ui.Toast(ex.Message));
                });
            });
        }

        /// <summary>حظر الطرف الآخر من داخل المحادثة (بديل Api.BlockDriver).</summary>
        private void BlockDriver()
        {
            Ui.Confirm("حظر السائق", "لن يظهر لك هذا السائق في الطلبات القادمة. تأكيد الحظر؟", "حظر", () =>
            {
                var progress = Ui.ShowProgress("جارٍ الحظر…");
                Ui.Run(() =>
                {
                    var o = Api.GetOrder(_orderId);
                    if (o == null || o.DriverId <= 0)
                    {
                        Ui.BeginOnMain(() =>
                        {
                            progress.Dismiss();
                            Ui.Toast("لا يوجد سائق مرتبط بهذا الطلب");
                        });
                        return;
                    }

                    Api.BlockDriver(o.DriverId);

                    Ui.BeginOnMain(() =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم حظر السائق");
                    });
                }, null, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            });
        }

        private sealed class ChatSource : UITableViewDataSource
        {
            private readonly ChatViewController _owner;

            public ChatSource(ChatViewController owner) { _owner = owner; }

            public override nint RowsInSection(UITableView tableView, nint section)
            {
                return _owner._messages.Count;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var m = _owner._messages[(int)indexPath.Row];
                var cell = tableView.DequeueReusableCell("chat") as ChatCell
                           ?? new ChatCell(UITableViewCellStyle.Default, "chat");
                cell.Bind(m);
                return cell;
            }
        }

        private sealed class ChatCell : UITableViewCell
        {
            private readonly UILabel _bubble = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Font = Theme.Font(15),
                Lines = 0,
                TextAlignment = UITextAlignment.Right
            };
            private readonly UILabel _meta = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Font = Theme.Font(10),
                TextColor = Theme.Gray,
                TextAlignment = UITextAlignment.Right
            };
            private NSLayoutConstraint _leading;
            private NSLayoutConstraint _trailing;

            public ChatCell(UITableViewCellStyle style, string reuse) : base(style, reuse)
            {
                BackgroundColor = UIColor.Clear;
                SelectionStyle = UITableViewCellSelectionStyle.None;

                _bubble.Layer.CornerRadius = 12f;
                _bubble.Layer.MasksToBounds = true;

                ContentView.AddSubview(_bubble);
                ContentView.AddSubview(_meta);

                _leading = NSLayoutConstraint.Create(_bubble, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, ContentView, NSLayoutAttribute.Leading, 1, 16);
                _trailing = NSLayoutConstraint.Create(_bubble, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, ContentView, NSLayoutAttribute.Trailing, 1, -16);

                ContentView.AddConstraints(new[]
                {
                    _leading, _trailing,
                    NSLayoutConstraint.Create(_bubble, NSLayoutAttribute.Top, NSLayoutRelation.Equal, ContentView, NSLayoutAttribute.Top, 1, 6),
                    NSLayoutConstraint.Create(_bubble, NSLayoutAttribute.Width, NSLayoutRelation.LessThanOrEqual, ContentView, NSLayoutAttribute.Width, 1, -80),

                    NSLayoutConstraint.Create(_meta, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _bubble, NSLayoutAttribute.Bottom, 1, 2),
                    NSLayoutConstraint.Create(_meta, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _bubble, NSLayoutAttribute.Leading, 1, 0),
                    NSLayoutConstraint.Create(_meta, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, ContentView, NSLayoutAttribute.Bottom, 1, -4),
                });
            }

            public void Bind(Message m)
            {
                var mine = m.SenderId == Session.UserId;
                _bubble.Text = "  " + (m.Text ?? "") + "  ";
                _bubble.BackgroundColor = mine ? Theme.Primary.ColorWithAlpha(0.12f) : Theme.White;
                _bubble.TextColor = Theme.TextPrimary;
                _meta.Text = m.SentAt == default(DateTime) ? "" : m.SentAt.ToString("HH:mm");

                _bubble.TextAlignment = mine ? UITextAlignment.Right : UITextAlignment.Left;
                _trailing.Active = mine;
                _leading.Active = !mine;
            }
        }
    }
}
