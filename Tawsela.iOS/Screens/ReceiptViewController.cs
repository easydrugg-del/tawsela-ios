using System;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/ReceiptActivity.cs — إيصال الرحلة مع إمكانية مشاركته.
    /// </summary>
    public class ReceiptViewController : BaseViewController
    {
        private readonly int _orderId;
        private UIStackView _stack;
        private UILabel _number;
        private Order _order;

        public ReceiptViewController(int orderId) : base("إيصال الرحلة")
        {
            _orderId = orderId;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            _stack = CreateScrollContent(10f, 16f);

            var header = MakeLabel("توصيلة", 22f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _number = MakeLabel("", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            _stack.AddArrangedSubview(header);
            _stack.AddArrangedSubview(_number);
            _stack.AddArrangedSubview(Ui.Separator());

            var share = Controls.PrimaryButton("مشاركة الإيصال");
            share.TouchUpInside += (s, e) => Share();
            _stack.AddArrangedSubview(share);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var order = Api.GetOrder(_orderId);
                Ui.BeginOnMain(() => { _order = order; Render(); });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render()
        {
            var o = _order;
            if (o == null) return;

            // أعد بناء البطاقات مع الاحتفاظ بالعنوان والزر
            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 3; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            _number.Text = o.OrderNumber;

            _stack.AddArrangedSubview(Controls.InfoRow("التاريخ", o.CreatedAt == default(DateTime) ? "—" : o.CreatedAt.ToString("dd/MM/yyyy HH:mm")));
            _stack.AddArrangedSubview(Controls.InfoRow("العميل", o.CustomerName));
            if (!string.IsNullOrEmpty(o.DriverName))
                _stack.AddArrangedSubview(Controls.InfoRow("السائق", o.DriverName));
            _stack.AddArrangedSubview(Controls.InfoRow("من", o.FromAddress));
            _stack.AddArrangedSubview(Controls.InfoRow("إلى", o.ToAddress));

            var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
            _stack.AddArrangedSubview(Controls.InfoRow("الأجرة", fare.ToString("0.00") + " ج.م"));
            if (o.Discount > 0)
                _stack.AddArrangedSubview(Controls.InfoRow("الخصم", o.Discount.ToString("0.00") + " ج.م"));
            if (o.Tip > 0)
                _stack.AddArrangedSubview(Controls.InfoRow("البقشيش", o.Tip.ToString("0.00") + " ج.م"));
            if (!string.IsNullOrEmpty(o.PaymentMethod))
                _stack.AddArrangedSubview(Controls.InfoRow("طريقة الدفع", o.PaymentMethod));

            _stack.AddArrangedSubview(Controls.InfoRow("الحالة", o.StatusText));

            var share = Controls.PrimaryButton("مشاركة الإيصال");
            share.TouchUpInside += (s, e) => Share();
            _stack.AddArrangedSubview(share);
        }

        private void Share()
        {
            var o = _order;
            if (o == null) return;

            var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
            var text =
                "إيصال توصيلة\n" +
                "رقم الطلب: " + o.OrderNumber + "\n" +
                "التاريخ: " + (o.CreatedAt == default(DateTime) ? "" : o.CreatedAt.ToString("dd/MM/yyyy HH:mm")) + "\n" +
                "من: " + o.FromAddress + "\n" +
                "إلى: " + o.ToAddress + "\n" +
                "الأجرة: " + fare.ToString("0.00") + " ج.م\n" +
                "الحالة: " + o.StatusText;

            try
            {
                var controller = new UIActivityViewController(new NSObject[] { new NSString(text) }, null);

                // على iPad يجب تحديد مصدر الـ Popover وإلا يحدث Crash
                if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                    controller.PopoverPresentationController != null)
                {
                    controller.PopoverPresentationController.SourceView = View;
                    controller.PopoverPresentationController.SourceRect =
                        new CoreGraphics.CGRect(View.Bounds.Width / 2, View.Bounds.Height / 2, 1, 1);
                }

                PresentViewController(controller, true, null);
            }
            catch (Exception ex)
            {
                Ui.Toast(ex.Message);
            }
        }
    }
}
