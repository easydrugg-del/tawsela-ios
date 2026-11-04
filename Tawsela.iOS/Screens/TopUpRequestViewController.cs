using System;
using System.Globalization;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل نموذج طلب الشحن في Activities/WalletActivity.cs:
    /// المبلغ + رقم المُحوِّل + رقم العملية + صورة الإيصال (تُصغَّر إلى 800px/JPEG 70
    /// كما في أندرويد) ثم Api.RequestTopUp.
    /// </summary>
    public class TopUpRequestViewController : BaseViewController
    {
        private UITextField _amount, _sender, _reference;
        private UIImageView _preview;
        private UILabel _hint;
        private UILabel _payLabel;
        private UIButton _submit;

        private string _receiptBase64 = "";
        private readonly Action _onDone;

        public TopUpRequestViewController(Action onDone) : base("طلب شحن الرصيد")
        {
            _onDone = onDone;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadPayNumber();
        }

        /// <summary>رقم التحويل الرسمي من الخادم (بديل Api.PaymentPhone).</summary>
        private void LoadPayNumber()
        {
            Ui.Run(() =>
            {
                var phone = "";
                try { phone = Api.PaymentPhone(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    _payLabel.Text = string.IsNullOrWhiteSpace(phone)
                        ? "رقم التحويل غير مُهيّأ — تواصل مع الدعم"
                        : phone;

                    _payLabel.TextColor = string.IsNullOrWhiteSpace(phone) ? Theme.Red : Theme.Primary;
                });
            }, null, ex => AppServices.Log("TawselaTopUp", "payment phone: " + ex.Message));
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(MakeLabel(
                "حوّل المبلغ على محفظة الشركة ثم أدخل البيانات هنا. " +
                "مطلوب رقم العملية أو صورة الإيصال حتى تُراجع الإدارة الطلب.",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            // رقم التحويل الرسمي (بديل Api.PaymentPhone في شاشة المحفظة)
            _payLabel = MakeLabel("جارٍ تحميل رقم التحويل…", 17f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            stack.AddArrangedSubview(_payLabel);

            stack.AddArrangedSubview(Controls.SectionTitle("المبلغ"));

            _amount = Controls.TextField("المبلغ بالجنيه (1 - 10000)");
            _amount.KeyboardType = UIKeyboardType.DecimalPad;
            _amount.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            _amount.TextAlignment = UITextAlignment.Left;
            stack.AddArrangedSubview(_amount);

            stack.AddArrangedSubview(Controls.SectionTitle("بيانات التحويل"));

            _sender = Controls.TextField("رقم الهاتف الذي حوّلت منه");
            _sender.KeyboardType = UIKeyboardType.PhonePad;
            _sender.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            _sender.Text = Session.Phone ?? "";
            stack.AddArrangedSubview(_sender);

            _reference = Controls.TextField("رقم عملية التحويل (اختياري)");
            _reference.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;
            stack.AddArrangedSubview(_reference);

            // صورة الإيصال (بديل ReqReceiptImage + FileProvider في أندرويد)
            stack.AddArrangedSubview(Controls.SectionTitle("صورة الإيصال"));

            var attach = Controls.OutlineButton("إرفاق صورة الإيصال");
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

            var clear = Controls.OutlineButton("إزالة الصورة");
            clear.TouchUpInside += (s, e) =>
            {
                _receiptBase64 = "";
                _preview.Image = null;
                _preview.Hidden = true;
            };
            stack.AddArrangedSubview(clear);

            _hint = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_hint);

            _submit = Controls.PrimaryButton("إرسال طلب الشحن");
            _submit.TouchUpInside += (s, e) => Submit();
            stack.AddArrangedSubview(_submit);
        }

        /// <summary>اختيار صورة من المعرض أو الكاميرا مع معاينة فورية.</summary>
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
            _hint.Text = "تم إرفاق الإيصال (" + (_receiptBase64.Length / 1024) + " ك.ب)";
            _hint.TextColor = Theme.Green;
        }

        private void Submit()
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

            _submit.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ إرسال طلب الشحن…");

            Ui.Run(
                () => Api.RequestTopUp(amount, sender, reference, receipt),
                () =>
                {
                    progress.Dismiss();
                    _submit.Enabled = true;

                    Ui.Toast("تم إرسال طلب الشحن — سيتم مراجعته من الإدارة");

                    if (_onDone != null) _onDone();

                    if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                        NavigationController.PopViewController(true);
                },
                ex =>
                {
                    progress.Dismiss();
                    _submit.Enabled = true;
                    Ui.Toast(ex.Message);
                });
        }
    }
}
