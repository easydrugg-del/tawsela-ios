using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/SplitFareActivity.cs — تقسيم أجرة الرحلة بين شخصين
    /// (Api.SplitFare + Api.SetSplitPhone).
    /// </summary>
    public class SplitFareViewController : BaseViewController
    {
        private readonly int _orderId;
        private readonly decimal _fare;

        private UITextField _phone1, _phone2, _amount1, _amount2;
        private UILabel _hint;

        public SplitFareViewController(int orderId, decimal fare) : base("تقسيم الفاتورة")
        {
            _orderId = orderId;
            _fare = fare;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            _hint = MakeLabel("الأجرة: " + _fare.ToString("0.00") + " ج.م",
                16f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            stack.AddArrangedSubview(_hint);

            stack.AddArrangedSubview(Controls.SectionTitle("الشخص الأول:"));
            _phone1 = Controls.TextField("رقم الهاتف الأول");
            _phone1.KeyboardType = UIKeyboardType.PhonePad;
            _phone1.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            stack.AddArrangedSubview(_phone1);

            _amount1 = Controls.TextField("المبلغ (ج.م)");
            _amount1.KeyboardType = UIKeyboardType.DecimalPad;
            stack.AddArrangedSubview(_amount1);

            stack.AddArrangedSubview(Controls.SectionTitle("الشخص الثاني:"));
            _phone2 = Controls.TextField("رقم الهاتف الثاني");
            _phone2.KeyboardType = UIKeyboardType.PhonePad;
            _phone2.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            stack.AddArrangedSubview(_phone2);

            _amount2 = Controls.TextField("المبلغ (ج.م)");
            _amount2.KeyboardType = UIKeyboardType.DecimalPad;
            stack.AddArrangedSubview(_amount2);

            var submit = Controls.PrimaryButton("💰 تقسيم الفاتورة");
            submit.TouchUpInside += (s, e) => Submit();
            stack.AddArrangedSubview(submit);
        }

        private void Submit()
        {
            var phone1 = (_phone1.Text ?? "").Trim();
            var phone2 = (_phone2.Text ?? "").Trim();

            if (phone1 == "" || phone2 == "")
            {
                Ui.Toast("أدخل رقمَي الهاتف");
                return;
            }

            decimal a1, a2;
            if (!decimal.TryParse(_amount1.Text, out a1) || a1 < 0)
            {
                Ui.Toast("أدخل مبلغًا صحيحًا للطرف الأول");
                return;
            }
            if (!decimal.TryParse(_amount2.Text, out a2) || a2 < 0)
            {
                Ui.Toast("أدخل مبلغًا صحيحًا للطرف الثاني");
                return;
            }

            if (Math.Abs(a1 + a2 - _fare) > 0.01m)
            {
                Ui.Toast("مجموع المبلغين يجب أن يساوي " + _fare.ToString("0.00") + " ج.م");
                return;
            }

            var progress = Ui.ShowProgress("جارٍ حفظ التقسيم…");

            Ui.Run(() =>
            {
                Api.SetSplitPhone(_orderId, phone1);
                Api.SetSplitPhone(_orderId, phone2);
                Api.SplitFare(_orderId, phone1, phone2, a1, a2);
            }, () =>
            {
                progress.Dismiss();
                Ui.Toast("تم حفظ تقسيم الأجرة");
                NavigationController?.PopViewController(true);
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
