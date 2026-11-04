using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantNewDeliveryActivity.cs — طلب توصيلة من المطعم
    /// (المنطقة تحدد السعر، والعنوان يُختار على الخريطة).
    /// </summary>
    public class RestaurantNewDeliveryViewController : BaseViewController
    {
        private readonly List<Dictionary<string, object>> _areas;
        private Dictionary<string, object> _area;

        private UITextField _customerName, _customerPhone, _pickup, _delivery;
        private UILabel _areaValue, _fareValue, _coords;
        private double _lat, _lng;

        public RestaurantNewDeliveryViewController(List<Dictionary<string, object>> areas)
            : base("توصيلة جديدة")
        {
            _areas = areas ?? new List<Dictionary<string, object>>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(Controls.SectionTitle("بيانات العميل"));
            _customerName = Controls.TextField("اسم العميل");
            _customerPhone = Controls.TextField("رقم الهاتف");
            _customerPhone.KeyboardType = UIKeyboardType.PhonePad;
            _customerPhone.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            stack.AddArrangedSubview(_customerName);
            stack.AddArrangedSubview(_customerPhone);

            stack.AddArrangedSubview(Controls.SectionTitle("العناوين"));

            _pickup = Controls.TextField("عنوان الاستلام (المطعم)");
            var myRestaurant = Session.City;
            stack.AddArrangedSubview(_pickup);

            _delivery = Controls.TextField("عنوان التوصيل");
            stack.AddArrangedSubview(_delivery);

            _coords = MakeLabel("", 11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_coords);

            var pick = Controls.OutlineButton("تحديد موقع التوصيل على الخريطة");
            pick.TouchUpInside += (s, e) =>
            {
                Push(MapViewController.Pick(_lat, _lng, (lat, lng, address) =>
                {
                    _lat = lat; _lng = lng;
                    _coords.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "الإحداثيات: {0:0.0000}, {1:0.0000}", lat, lng);
                    if (!string.IsNullOrWhiteSpace(address)) _delivery.Text = address;
                }));
            };
            stack.AddArrangedSubview(pick);

            stack.AddArrangedSubview(Controls.SectionTitle("المنطقة والسعر"));

            _areaValue = MakeLabel("اختر المنطقة", 15f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _areaValue.UserInteractionEnabled = true;
            _areaValue.AddGestureRecognizer(new UITapGestureRecognizer(() => PickArea()));
            stack.AddArrangedSubview(_areaValue);

            _fareValue = MakeLabel("—", 20f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            stack.AddArrangedSubview(_fareValue);

            var submit = Controls.PrimaryButton("إرسال طلب التوصيل");
            submit.TouchUpInside += (s, e) => Submit();
            stack.AddArrangedSubview(submit);
        }

        private void PickArea()
        {
            if (_areas.Count == 0)
            {
                Ui.Toast("لا توجد مناطق توصيل معرّفة");
                return;
            }

            var names = new List<string>();
            foreach (var a in _areas) names.Add(Row.Str(a, "Name"));

            Ui.Choose("منطقة التوصيل", names, index =>
            {
                _area = _areas[index];
                _areaValue.Text = Row.Str(_area, "Name");
                _areaValue.TextColor = Theme.TextPrimary;

                var price = Row.Num(_area, "Price");
                _fareValue.Text = "سعر التوصيل: " + price.ToString("0.00") + " ج.م";
            });
        }

        private void Submit()
        {
            if (_area == null) { Ui.Toast("اختر منطقة التوصيل"); return; }

            var name = (_customerName.Text ?? "").Trim();
            var phone = (_customerPhone.Text ?? "").Trim();
            var pickup = (_pickup.Text ?? "").Trim();
            var delivery = (_delivery.Text ?? "").Trim();

            if (name == "") { Ui.Toast("أدخل اسم العميل"); return; }
            if (phone == "") { Ui.Toast("أدخل رقم هاتف العميل"); return; }
            if (delivery == "") { Ui.Toast("أدخل عنوان التوصيل"); return; }
            if (_lat == 0 && _lng == 0) { Ui.Toast("حدد موقع التوصيل على الخريطة"); return; }

            var areaId = Row.Int(_area, "Id");
            var fare = Row.Num(_area, "Price");

            var progress = Ui.ShowProgress("جارٍ إرسال الطلب…");
            Ui.Run(() => Api.RequestRestaurantDelivery(areaId, name, phone, pickup,
                _lat, _lng, delivery, _lat, _lng, fare), () =>
            {
                progress.Dismiss();
                Ui.Toast("تم إرسال طلب التوصيل");
                NavigationController?.PopViewController(true);
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }

    /// <summary>
    /// بديل Activities/RestaurantDeliveryRequestsActivity.cs — التوصيلات المتاحة للسائق.
    /// </summary>
    public class RestaurantRequestsViewController : ListViewController<Dictionary<string, object>>
    {
        public RestaurantRequestsViewController() : base("توصيلات متاحة") { }

        protected override string EmptyText
        {
            get { return "لا توجد توصيلات متاحة الآن"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            var coord = LocationService.LastKnown;
            double? lat = coord.HasValue ? (double?)coord.Value.Latitude : null;
            double? lng = coord.HasValue ? (double?)coord.Value.Longitude : null;
            return Api.GetAvailableRestaurantDeliveries(lat, lng);
        }

        protected override UIView BuildRow(Dictionary<string, object> o)
        {
            var id = Row.Int(o, "Id");
            var pickup = Row.Str(o, "PickupAddress");
            var delivery = Row.Str(o, "DeliveryAddress");
            var fare = Row.Num(o, "Fare");
            var name = Row.Str(o, "CustomerName");

            var row = Controls.InfoRow(name,
                "من: " + pickup + "\nإلى: " + delivery + "\n" + fare.ToString("0.00") + " ج.م");

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Confirm("قبول التوصيلة", "قبول توصيلة " + name + "؟", "قبول", () =>
                {
                    var progress = Ui.ShowProgress("جارٍ القبول…");
                    Ui.Run(() => Api.AcceptRestaurantDelivery(id), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم قبول التوصيلة");
                        Reload();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            }));

            return row;
        }
    }

    /// <summary>
    /// بديل Activities/RestaurantDeliveryExecuteActivity.cs — تنفيذ توصيلة المطعم.
    /// </summary>
    public class RestaurantDeliveryExecuteViewController : BaseViewController
    {
        private readonly int _id;
        private UIStackView _stack;
        private UILabel _status;

        public RestaurantDeliveryExecuteViewController(int id) : base("تنفيذ التوصيلة")
        {
            _id = id;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _stack = CreateScrollContent(12f, 16f);
            _status = MakeLabel("جارٍ التحميل…", 16f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            _stack.AddArrangedSubview(_status);
            Load();
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var o = Api.GetRestaurantDelivery(_id);
                Ui.BeginOnMain(() => Render(o));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(Dictionary<string, object> o)
        {
            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 1; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (o == null)
            {
                _status.Text = "تعذّر تحميل التوصيلة";
                return;
            }

            var status = Row.Str(o, "Status");
            _status.Text = string.IsNullOrEmpty(status) ? "توصيلة مطعم" : status;

            _stack.AddArrangedSubview(Controls.InfoRow("العميل", Row.Str(o, "CustomerName")));
            _stack.AddArrangedSubview(Controls.InfoRow("الهاتف", Row.Str(o, "CustomerPhone")));
            _stack.AddArrangedSubview(Controls.InfoRow("الاستلام", Row.Str(o, "PickupAddress")));
            _stack.AddArrangedSubview(Controls.InfoRow("التوصيل", Row.Str(o, "DeliveryAddress")));
            _stack.AddArrangedSubview(Controls.InfoRow("السعر", Row.Num(o, "Fare").ToString("0.00") + " ج.م"));

            var phone = Row.Str(o, "CustomerPhone");
            if (!string.IsNullOrEmpty(phone))
            {
                var call = Controls.OutlineButton("اتصال بالعميل");
                call.TouchUpInside += (s, e) => AppServices.OpenUrl("tel:" + phone.Replace(" ", ""));
                _stack.AddArrangedSubview(call);
            }

            if (string.IsNullOrEmpty(status) || status == "قيد الانتظار")
                Action("قبول التوصيلة", () => Api.AcceptRestaurantDelivery(_id));
            else if (status == "مخصص للسائق")
                Action("وصلت للمحل", () => Api.ArrivedAtRestaurant(_id));
            else if (status == "وصل للمحل")
                Action("تم استلام الطلب", () => Api.PickedUpRestaurantDelivery(_id));
            else if (status == "استلمت الطلب")
                Action("تم التسليم للعميل", () => Api.DeliveredRestaurantDelivery(_id));
            else if (status == "تم التسليم")
            {
                // بديل btnComplete → Finish في أندرويد
                var close = Controls.OutlineButton("إغلاق");
                close.TouchUpInside += (s, e) => Back();
                _stack.AddArrangedSubview(close);
            }
        }

        private void Action(string title, Func<string> action)
        {
            var button = Controls.PrimaryButton(title);
            button.TouchUpInside += (s, e) =>
            {
                var progress = Ui.ShowProgress("جارٍ التنفيذ…");
                Ui.Run(() => { action(); }, () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم");
                    Load();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            };
            _stack.AddArrangedSubview(button);
        }
    }
}
