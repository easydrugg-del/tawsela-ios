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
    /// بديل Activities/ComplaintActivity.cs — إرسال شكوى ومتابعة الردود
    /// (Api.AddComplaint / Api.MyComplaints).
    /// </summary>
    public class ComplaintViewController : BaseViewController
    {
        private UITextField _subject;
        private UITextView _body;
        private UITextField _orderId;
        private UIButton _send;
        private UIStackView _list;

        public ComplaintViewController() : base("الشكاوى والدعم") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(Controls.SectionTitle("شكوى جديدة"));

            _subject = Controls.TextField("موضوع الشكوى");
            stack.AddArrangedSubview(_subject);

            _orderId = Controls.TextField("رقم الطلب (اختياري)");
            _orderId.KeyboardType = UIKeyboardType.NumberPad;
            _orderId.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            stack.AddArrangedSubview(_orderId);

            _body = Controls.TextArea("", 120f);
            stack.AddArrangedSubview(Controls.SectionTitle("التفاصيل"));
            stack.AddArrangedSubview(_body);

            _send = Controls.PrimaryButton("إرسال الشكوى");
            _send.TouchUpInside += (s, e) => Send();
            stack.AddArrangedSubview(_send);

            stack.AddArrangedSubview(Controls.SectionTitle("شكاواي السابقة"));

            _list = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_list);
        }

        private void Send()
        {
            if (!Net.IsOnline()) { Ui.Toast("لا يوجد اتصال بالإنترنت"); return; }

            var subject = (_subject.Text ?? "").Trim();
            var body = _body.Text ?? "";

            if (subject == "") { Ui.Toast("اكتب موضوع الشكوى"); return; }
            if (body.Trim() == "") { Ui.Toast("اكتب تفاصيل الشكوى"); return; }

            int orderId;
            if (!int.TryParse((_orderId.Text ?? "").Trim(), out orderId)) orderId = 0;

            _send.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ الإرسال…");

            Ui.Run(() => Api.AddComplaint(orderId, subject, body), () =>
            {
                progress.Dismiss();
                _send.Enabled = true;
                _subject.Text = "";
                _body.Text = "";
                _orderId.Text = "";
                Ui.Toast("تم إرسال الشكوى وسيتم الرد قريبًا");
                Load();
            }, ex =>
            {
                progress.Dismiss();
                _send.Enabled = true;
                Ui.Toast(ex.Message);
            });
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var list = Api.MyComplaints();
                Ui.BeginOnMain(() => Render(list));
            }, null, ex => AppServices.Log("TawselaComplaint", ex.Message));
        }

        private void Render(List<Complaint> list)
        {
            for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _list.ArrangedSubviews[i];
                _list.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (list == null || list.Count == 0)
            {
                _list.AddArrangedSubview(MakeLabel("لا توجد شكاوى سابقة", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var c in list)
            {
                var card = Controls.Card();

                var subject = MakeLabel(c.Subject, 15f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
                var status = MakeLabel(string.IsNullOrEmpty(c.Status) ? "قيد المراجعة" : c.Status,
                    12f, Theme.Accent, UIFontWeight.Medium, UITextAlignment.Left);
                var body = MakeLabel(c.Body, 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

                card.AddSubview(subject);
                card.AddSubview(status);
                card.AddSubview(body);

                var lastAnchor = body;

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(subject, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(subject, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(status, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, subject, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(status, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                    NSLayoutConstraint.Create(body, NSLayoutAttribute.Top, NSLayoutRelation.Equal, subject, NSLayoutAttribute.Bottom, 1, 4),
                    NSLayoutConstraint.Create(body, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(body, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                });

                if (!string.IsNullOrEmpty(c.Reply))
                {
                    var reply = MakeLabel("الرد: " + c.Reply, 13f, Theme.Green, UIFontWeight.Regular, UITextAlignment.Right);
                    card.AddSubview(reply);
                    card.AddConstraints(new[]
                    {
                        NSLayoutConstraint.Create(reply, NSLayoutAttribute.Top, NSLayoutRelation.Equal, body, NSLayoutAttribute.Bottom, 1, 6),
                        NSLayoutConstraint.Create(reply, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                        NSLayoutConstraint.Create(reply, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    });
                    lastAnchor = reply;
                }

                card.AddConstraint(NSLayoutConstraint.Create(lastAnchor, NSLayoutAttribute.Bottom,
                    NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10));

                _list.AddArrangedSubview(card);
            }
        }
    }
}
