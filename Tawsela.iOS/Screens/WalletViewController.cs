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
    /// بديل Activities/WalletActivity.cs — الرصيد، الحركات، طلب شحن،
    /// وتحويل للسائق (سحب الأرباح).
    /// </summary>
    public class WalletViewController : BaseViewController
    {
        private UILabel _balance;
        private UILabel _payNumber, _payInfo, _pendingInfo;
        private UIStackView _txns;
        private UIStackView _topUps;
        private UIButton _btnTopUp;

        public WalletViewController() : base("المحفظة") { }

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

            var title = MakeLabel("رصيد المحفظة", 13f, UIColor.White.ColorWithAlpha(0.9f),
                UIFontWeight.Regular, UITextAlignment.Right);
            _balance = MakeLabel("—", 30f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Right);

            card.AddSubview(title);
            card.AddSubview(_balance);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -16),

                NSLayoutConstraint.Create(_balance, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_balance, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(_balance, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(_balance, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -16),
            });
            stack.AddArrangedSubview(card);

            _btnTopUp = Controls.PrimaryButton("طلب شحن الرصيد");
            _btnTopUp.TouchUpInside += (s, e) => RequestTopUp();
            stack.AddArrangedSubview(_btnTopUp);

            // رقم التحويل الرسمي + تنبيه الطلب المعلّق (بديل txtPayNumber/txtPayPhoneInfo/txtTopupStatus)
            _payNumber = MakeLabel("", 18f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _payNumber.Hidden = true;
            stack.AddArrangedSubview(_payNumber);

            _payInfo = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _payInfo.Hidden = true;
            stack.AddArrangedSubview(_payInfo);

            _pendingInfo = MakeLabel("", 13f, Theme.Accent, UIFontWeight.Semibold, UITextAlignment.Right);
            _pendingInfo.Hidden = true;
            stack.AddArrangedSubview(_pendingInfo);

            if (Session.IsDriver)
            {
                var withdraw = Controls.OutlineButton("سحب الأرباح");
                withdraw.TouchUpInside += (s, e) => Withdraw();
                stack.AddArrangedSubview(withdraw);
            }

            stack.AddArrangedSubview(Controls.SectionTitle("طلبات الشحن"));

            _topUps = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_topUps);

            stack.AddArrangedSubview(Controls.SectionTitle("آخر الحركات"));

            _txns = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_txns);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var balance = Api.WalletBalance();
                var txns = Api.WalletTxns();

                List<Dictionary<string, object>> topUps = null;
                try { topUps = Api.MyTopUps(); } catch { }

                // رقم التحويل الرسمي (بديل Api.PaymentPhone)
                var payPhone = "";
                try { payPhone = Api.PaymentPhone(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    _balance.Text = balance.ToString("0.00") + " ج.م";
                    RenderPayInfo(payPhone, topUps);
                    RenderTopUps(topUps);
                    RenderTxns(txns);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        /// <summary>
        /// عرض رقم التحويل الرسمي وتنبيه الطلب المعلّق
        /// (بديل txtPayNumber + txtPayPhoneInfo + txtTopupStatus).
        /// </summary>
        private void RenderPayInfo(string payPhone, List<Dictionary<string, object>> topUps)
        {
            if (string.IsNullOrWhiteSpace(payPhone))
            {
                _payNumber.Hidden = true;
                _payInfo.Text = "رقم الدفع غير مُهيّأ. تواصل مع الدعم قبل التحويل.";
                _payInfo.Hidden = false;
            }
            else
            {
                _payNumber.Text = payPhone;
                _payNumber.Hidden = false;

                _payInfo.Text = "حوّل المبلغ على رقم الشركة أعلاه ثم أرسل بيانات العملية " +
                                "أو صورة الإيصال من زر «طلب شحن الرصيد». " +
                                "المراجعة يدوية قبل إضافة الرصيد.";
                _payInfo.Hidden = false;
            }

            // هل هناك طلب قيد المراجعة؟
            var pending = "";
            if (topUps != null)
            {
                foreach (var r in topUps)
                {
                    if (Row.Str(r, "Status") == "معلق")
                    {
                        pending = "لديك طلب قيد المراجعة: " + Row.Num(r, "Amount").ToString("0.00") +
                                  " ج.م — بانتظار الموافقة";
                        break;
                    }
                }
            }

            _pendingInfo.Text = pending;
            _pendingInfo.Hidden = string.IsNullOrEmpty(pending);
        }

        /// <summary>طلبات شحن الرصيد الخاصة بالمستخدم وحالتها (بديل Api.MyTopUps).</summary>
        private void RenderTopUps(List<Dictionary<string, object>> list)
        {
            for (int i = _topUps.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _topUps.ArrangedSubviews[i];
                _topUps.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (list == null || list.Count == 0)
            {
                _topUps.AddArrangedSubview(MakeLabel("لا توجد طلبات شحن", 13f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var r in list)
            {
                var amount = Row.Num(r, "Amount");
                var status = Row.Str(r, "Status");
                var sender = Row.Str(r, "SenderPhone");
                var date = Row.Date(r, "CreatedAt");

                var color = status == "مقبول" ? Theme.Green
                          : status == "مرفوض" ? Theme.Red
                          : Theme.Accent;

                _topUps.AddArrangedSubview(Controls.InfoRow(
                    date,
                    amount.ToString("0.00") + " ج.م · " + status +
                    (string.IsNullOrEmpty(sender) ? "" : "\nمن: " + sender),
                    color));
            }
        }

        private void RenderTxns(List<WalletTxn> list)
        {
            for (int i = _txns.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _txns.ArrangedSubviews[i];
                _txns.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (list == null || list.Count == 0)
            {
                _txns.AddArrangedSubview(MakeLabel("لا توجد حركات بعد", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Center));
                return;
            }

            foreach (var t in list)
            {
                var card = Controls.Card();
                var amount = MakeLabel(t.AmountText, 15f, t.Amount >= 0 ? Theme.Green : Theme.Red,
                    UIFontWeight.Bold, UITextAlignment.Left);
                var type = MakeLabel(string.IsNullOrEmpty(t.Type) ? "حركة" : t.Type, 13f, Theme.TextPrimary,
                    UIFontWeight.Medium, UITextAlignment.Right);
                var note = MakeLabel(t.Note, 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
                var date = MakeLabel(t.CreatedAt == default(DateTime) ? "" : t.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    11f, Theme.Gray, UIFontWeight.Regular, UITextAlignment.Right);

                card.AddSubview(amount);
                card.AddSubview(type);
                card.AddSubview(note);
                card.AddSubview(date);

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(type, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(type, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(amount, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, type, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(amount, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                    NSLayoutConstraint.Create(note, NSLayoutAttribute.Top, NSLayoutRelation.Equal, type, NSLayoutAttribute.Bottom, 1, 4),
                    NSLayoutConstraint.Create(note, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(note, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(date, NSLayoutAttribute.Top, NSLayoutRelation.Equal, note, NSLayoutAttribute.Bottom, 1, 4),
                    NSLayoutConstraint.Create(date, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(date, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(date, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -8),
                });

                _txns.AddArrangedSubview(card);
            }
        }

        private void RequestTopUp()
        {
            Push(new TopUpRequestViewController(Load));
        }

        private void Withdraw()
        {
            Ui.Prompt("سحب الأرباح", "المبلغ المراد سحبه", "", "المبلغ", text =>
            {
                decimal amount;
                if (!decimal.TryParse(text, out amount) || amount <= 0)
                {
                    Ui.Toast("أدخل مبلغًا صحيحًا");
                    return;
                }

                Ui.Confirm("تأكيد السحب", "سيتم خصم " + amount.ToString("0.00") + " ج.م من المحفظة.", "تأكيد", () =>
                {
                    var progress = Ui.ShowProgress("جارٍ تنفيذ السحب…");
                    Ui.Run(() => Api.Withdraw(amount), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم إرسال طلب السحب");
                        Load();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            }, UIKeyboardType.DecimalPad);
        }
    }
}
