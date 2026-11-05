using System;
using System.Collections.Generic;
using Tawsela.Data;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantOrderDetailActivity.cs — تفاصيل توصيلة من جهة المطعم
    /// مع إمكانية الإلغاء قبل أن يستلمها السائق.
    /// </summary>
    public class RestaurantOrderDetailViewController : BaseViewController
    {
        private readonly int _id;
        private UILabel _status, _fare, _area, _pickup, _delivery, _driver, _message;
        private UIButton _cancel;

        public RestaurantOrderDetailViewController(int id) : base("تفاصيل التوصيلة")
        {
            _id = id;
        }

        public override bool ShowNavigationBar
        {
            get { return true; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var stack = CreateScrollContent(10f, 16f);

            _status = MakeLabel("جارٍ التحميل…", 16f, Theme.Primary, UIFontWeight.Semibold, UITextAlignment.Right);
            stack.AddArrangedSubview(_status);

            _fare = MakeLabel("", 15f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_fare);

            _area = MakeLabel("", 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_area);

            _pickup = MakeLabel("", 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _pickup.Lines = 0;
            stack.AddArrangedSubview(_pickup);

            _delivery = MakeLabel("", 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _delivery.Lines = 0;
            stack.AddArrangedSubview(_delivery);

            _driver = MakeLabel("", 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _driver.Lines = 0;
            stack.AddArrangedSubview(_driver);

            _message = MakeLabel("", 13f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Right);
            _message.Hidden = true;
            stack.AddArrangedSubview(_message);

            var repeat = Controls.OutlineButton("إعادة نفس الطلب");
            repeat.TouchUpInside += (s, e) => Repeat();
            stack.AddArrangedSubview(repeat);

            _cancel = Controls.DangerButton("إلغاء الطلب");
            _cancel.Hidden = true;
            _cancel.TouchUpInside += (s, e) =>
                Ui.Confirm("إلغاء الطلب", "سيُلغى طلب التوصيل ولا يستطيع السائق استلامه. متابعة؟", "إلغاء الطلب", Cancel);
            stack.AddArrangedSubview(_cancel);

            Load();
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                Dictionary<string, object> o = null;
                try { o = Api.GetRestaurantDelivery(_id); } catch { }
                Ui.BeginOnMain(() => Bind(o));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Bind(Dictionary<string, object> o)
        {
            if (o == null)
            {
                _status.Text = "التوصيل غير موجود";
                return;
            }

            var status = Tawsela.Data.Row.Str(o, "Status");
            var customer = Tawsela.Data.Row.Str(o, "CustomerName");
            var customerPhone = Tawsela.Data.Row.Str(o, "CustomerPhone");
            var fare = Tawsela.Data.Row.Num(o, "Fare");
            var area = Tawsela.Data.Row.Str(o, "AreaName");
            var pickup = Tawsela.Data.Row.Str(o, "PickupAddress");
            var delivery = Tawsela.Data.Row.Str(o, "DeliveryAddress");
            var driver = Tawsela.Data.Row.Str(o, "DriverName");
            var driverPhone = Tawsela.Data.Row.Str(o, "DriverPhone");

            Title = string.IsNullOrEmpty(customer) ? "توصيلة #" + _id : customer;
            _status.Text = "الحالة: " + (string.IsNullOrEmpty(status) ? "—" : status);
            _fare.Text = "سعر التوصيل: " + fare.ToString("0.##") + " ج.م";
            _area.Text = string.IsNullOrEmpty(area) ? "" : "المنطقة: " + area;
            _pickup.Text = "الاستلام: " + (string.IsNullOrEmpty(pickup) ? "—" : pickup);
            _delivery.Text = "التوصيل: " + (string.IsNullOrEmpty(delivery) ? "—" : delivery);

            var driverLine = "السائق: " + (string.IsNullOrEmpty(driver) ? "لم يُسند بعد" : driver);
            if (!string.IsNullOrEmpty(driverPhone)) driverLine += "  (" + driverPhone + ")";
            if (!string.IsNullOrEmpty(customerPhone)) driverLine += "\nهاتف العميل: " + customerPhone;
            _driver.Text = driverLine;

            var canCancel = status == "قيد الانتظار" || status == "مخصص للسائق";
            _cancel.Hidden = !canCancel;
        }

        /// <summary>فتح شاشة توصيلة جديدة بنفس بيانات هذه التوصيلة.</summary>
        private void Repeat()
        {
            var progress = Ui.ShowProgress("جارٍ التحميل…");
            Ui.Run(() =>
            {
                List<Dictionary<string, object>> areas = null;
                Dictionary<string, object> order = null;
                try { areas = Api.RestaurantDeliveryAreas(); } catch { }
                try { order = Api.GetRestaurantDelivery(_id); } catch { }
                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();
                    Push(new RestaurantNewDeliveryViewController(areas, order));
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        private void Cancel()
        {
            var progress = Ui.ShowProgress("جارٍ الإلغاء…");
            Ui.Run(() => Api.CancelRestaurantDelivery(_id),
                () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم إلغاء الطلب");
                    Back();
                },
                ex =>
                {
                    progress.Dismiss();
                    _message.Text = ex == null ? "تعذّر الإلغاء" : ex.Message;
                    _message.Hidden = false;
                });
        }
    }
}
