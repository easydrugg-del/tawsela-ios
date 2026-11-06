using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/DriverDocActivity.cs — رفع مستندات السائق
    /// (نفس مفاتيح الخادم العربية) مع عرض حالة المستندات.
    /// </summary>
    public class DriverDocsViewController : BaseViewController
    {
        private readonly string[] _keys =
        {
            "فاتورة",
            "ترخيص_مرور",
            "بطاقة_سائق",
            "بطاقة_مالك",
            "اصل_فيش"
        };

        private readonly string[] _labels =
        {
            "فاتورة (إيصال)",
            "ترخيص المرور",
            "بطاقة السائق",
            "بطاقة المالك",
            "أصل الفيش"
        };

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>();
        private UITextField _opLicense;
        private UILabel _status;
        private UIButton _submit;
        private readonly List<UILabel> _imageStates = new List<UILabel>();

        public DriverDocsViewController() : base("إعادة إرسال الوثائق") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadStatus();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(10f, 16f);

            _status = MakeLabel("جارٍ التحميل…", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_status);

            _opLicense = Controls.TextField("رقم التشغيل");
            stack.AddArrangedSubview(_opLicense);

            stack.AddArrangedSubview(MakeLabel("ارفع صورة واضحة لكل مستند (يمكنك رفع بعضها فقط):",
                13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            for (int i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];
                var label = _labels[i];

                var card = Controls.Card();
                var title = MakeLabel(label, 14f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
                var state = MakeLabel("لم تُرفع", 12f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Right);
                _imageStates.Add(state);

                var pick = Controls.OutlineButton("اختيار صورة");
                pick.TouchUpInside += (s, e) =>
                {
                    ImagePicker.PickFromLibrary(this, base64 =>
                    {
                        if (string.IsNullOrEmpty(base64))
                        {
                            Ui.Toast("تعذّر قراءة الصورة");
                            return;
                        }

                        _images[key] = base64;
                        state.Text = "تم الرفع ✓";
                        state.TextColor = Theme.Green;
                    }, err => Ui.Toast(err));
                };

                card.AddSubview(title);
                card.AddSubview(state);
                card.AddSubview(pick);

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(state, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(state, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                    NSLayoutConstraint.Create(pick, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(pick, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(pick, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(pick, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                });

                stack.AddArrangedSubview(card);
            }

            _submit = Controls.PrimaryButton("إرسال للمراجعة");
            _submit.TouchUpInside += (s, e) => Submit();
            stack.AddArrangedSubview(_submit);
        }

        private void LoadStatus()
        {
            Ui.Run(() =>
            {
                var status = Api.MyDocStatus();
                Ui.BeginOnMain(() => _status.Text = string.IsNullOrEmpty(status) ? "لا توجد مستندات مرفوعة بعد" : status);
            }, null, ex => AppServices.Log("TawselaDocs", ex.Message));
        }

        private void Submit()
        {
            if (_images.Count == 0)
            {
                Ui.Toast("ارفع صورة واحدة على الأقل (الفاتورة أو بطاقة السائق أو بطاقة المالك أو أصل الفيش)");
                return;
            }

            var images = new Dictionary<string, string>(_images);
            var opLicense = (_opLicense.Text ?? "").Trim();

            _submit.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ إرسال المستندات…");

            Ui.Run(() => Api.AddDriverDocs(images, opLicense), () =>
            {
                progress.Dismiss();
                _submit.Enabled = true;
                Ui.Toast("تم إرسال المستندات للمراجعة");
                LoadStatus();
            }, ex =>
            {
                progress.Dismiss();
                _submit.Enabled = true;
                Ui.Toast(ex.Message);
            });
        }
    }
}
