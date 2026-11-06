using System;
using System.Collections.Generic;
using System.Globalization;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/WalletActivity.cs بنفس ترتيب ونصوص أندرويد:
    /// الرصيد، الشحن الخارجي بتحويل يدوي (مبلغ/مُحوِّل/رقم عملية/صورة إيصال)،
    /// طلبات الشحن، حركة المحفظة، ثم التحويل اللحظي داخل التطبيق.
    /// </summary>
    public class WalletViewController : BaseViewController
    {
        private UILabel _balance;
        private UILabel _payNumber, _payInfo, _pendingInfo, _receiptHint;
        private UIStackView _txns, _topUps;
        private UIButton _btnSubmitTopUp;

        private UITextField _amount, _sender, _reference;
        private UIImageView _preview;
        private string _receiptBase64 = "";

        private UITextField _transferPhone, _transferAmount;

        public WalletViewController() : base("المحفظة") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadPayNumber();
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

            /* ---------- الرصيد ---------- */
            var card = Controls.Card();
            card.BackgroundColor = Theme.Primary;

            var title = MakeLabel("المحفظة / Wallet", 13f, UIColor.White.ColorWithAlpha(0.9f),
                UIFontWeight.Regular, UITextAlignment.Right);
            _balance = MakeLabel("0 ج.م", 30f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Right);

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

            /* ---------- الشحن الخارجي ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("شحن خارجي بتحويل يدوي — External top-up with manual review"));

            _payNumber = MakeLabel("جارٍ تحميل رقم التحويل…", 18f, Theme.Primary,
                UIFontWeight.Bold, UITextAlignment.Center);
            stack.AddArrangedSubview(_payNumber);

            _payInfo = MakeLabel(
                "حوّل المبلغ إلى رقم الشركة أعلاه من أي محفظة إلكترونية، ثم أدخل بيانات التحويل. لا تشارك OTP. " +
                "تتم المراجعة يدويًا قبل إضافة الرصيد.\n" +
                "Transfer to the official company number above, then submit the transaction details. " +
                "Manual review is required before the balance is credited.",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _payInfo.Lines = 0;
            stack.AddArrangedSubview(_payInfo);

            var steps = MakeLabel(
                "الخطوات / Steps:\n" +
                "1) حوّل المبلغ إلى رقم الشركة من محفظتك الإلكترونية.\n" +
                "2) احتفظ برسالة التأكيد أو الإيصال.\n" +
                "3) أدخل رقم العملية أو أرفق صورة الإيصال.\n" +
                "4) انتظر مراجعة الإدارة؛ لا يُضاف الرصيد تلقائيًا.\n" +
                "1) Transfer to the company number using your e-wallet.\n" +
                "2) Keep the confirmation message or receipt.\n" +
                "3) Enter the transaction reference or attach the receipt image.\n" +
                "4) Wait for manual approval; the balance is not added automatically.",
                11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            steps.Lines = 0;
            stack.AddArrangedSubview(steps);

            var warn = MakeLabel(
                "تنبيه: لا تشارك رمز OTP أو بيانات البطاقة مع أي شخص. / Never share OTP or card details with anyone.",
                11f, Theme.Red, UIFontWeight.Medium, UITextAlignment.Right);
            warn.Lines = 0;
            stack.AddArrangedSubview(warn);

            stack.AddArrangedSubview(MakeLabel("المبلغ (ج.م) / Amount (EGP)", 12f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));
            _amount = Controls.TextField("مثال: 100");
            _amount.KeyboardType = UIKeyboardType.DecimalPad;
            stack.AddArrangedSubview(_amount);

            stack.AddArrangedSubview(MakeLabel("رقم المحفظة المحوَّل منها / Sender wallet number", 12f,
                Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right));
            _sender = Controls.TextField("01xxxxxxxxx");
            _sender.KeyboardType = UIKeyboardType.PhonePad;
            _sender.Text = Session.Phone ?? "";
            stack.AddArrangedSubview(_sender);

            stack.AddArrangedSubview(MakeLabel(
                "رقم العملية أو صورة الإيصال (أحدهما مطلوب): رقم التحويل في رسالة التأكيد — تستخدمه الإدارة للتحقق قبل التأكيد.\n" +
                "Transaction reference or receipt image (one is required).",
                12f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right));
            _reference = Controls.TextField("رقم العملية");
            _reference.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;
            stack.AddArrangedSubview(_reference);

            var attach = Controls.OutlineButton("📷 صورة الإيصال");
            attach.TouchUpInside += (s, e) => AttachReceipt();
            stack.AddArrangedSubview(attach);

            _preview = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                BackgroundColor = Theme.White,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            _preview.Layer.CornerRadius = Theme.CornerEdit;
            _preview.Layer.BorderWidth = 1f;
            _preview.Layer.BorderColor = Theme.CardStroke.CGColor;
            _preview.Layer.MasksToBounds = true;
            _preview.HeightAnchor.ConstraintEqualTo(180).Active = true;
            stack.AddArrangedSubview(_preview);

            _receiptHint = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_receiptHint);

            _btnSubmitTopUp = Controls.PrimaryButton("إرسال طلب الشحن للمراجعة / Submit for review");
            _btnSubmitTopUp.TouchUpInside += (s, e) => SubmitTopUp();
            stack.AddArrangedSubview(_btnSubmitTopUp);

            _pendingInfo = MakeLabel("", 13f, Theme.Accent, UIFontWeight.Semibold, UITextAlignment.Right);
            _pendingInfo.Hidden = true;
            stack.AddArrangedSubview(_pendingInfo);

            /* ---------- طلبات الشحن ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("🧾 طلبات الشحن وحالتها"));
            _topUps = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_topUps);

            /* ---------- حركة المحفظة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("🧾 حركة المحفظة / Wallet activity"));
            _txns = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_txns);

            /* ---------- التحويل اللحظي ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("🔄 تحويل لحظي (دفع حق التوصيلة)"));
            stack.AddArrangedSubview(MakeLabel(
                "تحويل داخلي داخل التطبيق لحساب آخر مسجل في التطبيق — وليس تحويلاً لحساب خارجي " +
                "(فودافون كاش/بنوك)",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            stack.AddArrangedSubview(MakeLabel("رقم موبايل المستلم", 12f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));
            _transferPhone = Controls.TextField("01xxxxxxxxx");
            _transferPhone.KeyboardType = UIKeyboardType.PhonePad;
            stack.AddArrangedSubview(_transferPhone);

            stack.AddArrangedSubview(MakeLabel("المبلغ (ج.م)", 12f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));
            _transferAmount = Controls.TextField("مثال: 50");
            _transferAmount.KeyboardType = UIKeyboardType.DecimalPad;
            stack.AddArrangedSubview(_transferAmount);

            var transfer = Controls.PrimaryButton("تحويل الآن — يسمع فوراً");
            transfer.TouchUpInside += (s, e) => Transfer();
            stack.AddArrangedSubview(transfer);
        }

        /// <summary>رقم التحويل الرسمي (بديل Api.PaymentPhone).</summary>
        private void LoadPayNumber()
        {
            Ui.Run(() =>
            {
                var phone = "";
                try { phone = Api.PaymentPhone(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        _payNumber.Text = "رقم التحويل غير مُهيّأ — تواصل مع الدعم";
                        _payNumber.TextColor = Theme.Red;
                    }
                    else
                    {
                        _payNumber.Text = phone;
                        _payNumber.TextColor = Theme.Primary;
                    }
                });
            }, null, ex => AppServices.Log("TawselaWallet", "payment phone: " + ex.Message));
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var balance = Api.WalletBalance();
                var txns = Api.WalletTxns();

                List<Dictionary<string, object>> topUps = null;
                try { topUps = Api.MyTopUps(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    _balance.Text = balance.ToString("0.00") + " ج.م";
                    RenderPending(topUps);
                    RenderTopUps(topUps);
                    RenderTxns(txns);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        /// <summary>تنبيه وجود طلب شحن قيد المراجعة (بديل txtTopupStatus).</summary>
        private void RenderPending(List<Dictionary<string, object>> topUps)
        {
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

        /// <summary>اختيار صورة من المعرض أو الكاميرا مع معاينة فورية (بديل ReqReceiptImage).</summary>
        private void AttachReceipt()
        {
            Ui.Choose("صورة الإيصال", new[] { "الكاميرا", "مكتبة الصور" }, index =>
            {
                if (index == 0)
                    ImagePicker.PickImage(this, true, ShowPreview, err => Ui.Toast(err));
                else
                    ImagePicker.PickImage(this, false, ShowPreview, err => Ui.Toast(err));
            });
        }

        private void ShowPreview(UIImage image)
        {
            if (image == null) return;

            // نفس مقاس أندرويد: أقصى عرض 800px وجودة JPEG 70
            var scaled = ImagePicker.Scale(image, 800f);

            try
            {
                using (var data = scaled.AsJPEG(0.7f))
                {
                    _receiptBase64 = data == null ? "" : Convert.ToBase64String(data.ToArray());
                }
            }
            catch
            {
                _receiptBase64 = "";
            }

            if (string.IsNullOrEmpty(_receiptBase64))
            {
                Ui.Toast("تعذّر قراءة الصورة");
                return;
            }

            _preview.Image = scaled;
            _preview.Hidden = false;
            _receiptHint.Text = "تم إرفاق الإيصال (" + (_receiptBase64.Length / 1024) + " ك.ب)";
            _receiptHint.TextColor = Theme.Green;
        }

        private void SubmitTopUp()
        {
            if (!Net.IsOnline()) { Ui.Toast("لا يوجد اتصال بالإنترنت"); return; }

            decimal amount;
            if (!decimal.TryParse((_amount.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out amount))
            {
                Ui.Toast("أدخل المبلغ المطلوب");
                return;
            }

            if (amount < 1 || amount > 10000)
            {
                Ui.Toast("المبلغ يجب أن يكون بين 1 و10000 جنيه");
                return;
            }

            var sender = (_sender.Text ?? "").Trim();
            if (sender == "")
            {
                Ui.Toast("أدخل رقم الهاتف الذي حوّلت منه");
                return;
            }

            var reference = (_reference.Text ?? "").Trim();
            var receipt = _receiptBase64 ?? "";

            if (reference == "" && receipt == "")
            {
                Ui.Toast("أدخل رقم العملية أو أرفق صورة الإيصال");
                return;
            }

            _btnSubmitTopUp.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ إرسال طلب الشحن…");

            Ui.Run(
                () => Api.RequestTopUp(amount, sender, reference, receipt),
                () =>
                {
                    progress.Dismiss();
                    _btnSubmitTopUp.Enabled = true;
                    Ui.Toast("تم إرسال طلب الشحن — سيتم مراجعته من الإدارة");

                    _amount.Text = "";
                    _reference.Text = "";
                    _receiptBase64 = "";
                    _preview.Image = null;
                    _preview.Hidden = true;
                    _receiptHint.Text = "";

                    Load();
                },
                ex =>
                {
                    progress.Dismiss();
                    _btnSubmitTopUp.Enabled = true;
                    Ui.Toast(ex.Message);
                });
        }

        private void Transfer()
        {
            var phone = (_transferPhone.Text ?? "").Trim();

            decimal amount;
            if (!decimal.TryParse((_transferAmount.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                Ui.Toast("أدخل مبلغ التحويل");
                return;
            }

            if (phone.Length < 8) { Ui.Toast("أدخل رقم موبايل المستلم"); return; }

            Ui.Confirm("تحويل لحظي",
                "تحويل " + amount.ToString("0.00") + " ج.م إلى " + phone + "؟", "تحويل", () =>
            {
                var progress = Ui.ShowProgress("جارٍ التحويل…");
                Ui.Run(() => Api.WalletTransfer(phone, amount), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم التحويل بنجاح");
                    _transferPhone.Text = "";
                    _transferAmount.Text = "";
                    Load();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            });
        }
    }
}
