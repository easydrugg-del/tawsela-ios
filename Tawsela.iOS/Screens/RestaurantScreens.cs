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
    /// بديل Activities/RestaurantNewDeliveryActivity.cs — طلب توصيلة من المطعم
    /// (المنطقة تحدد السعر، والعنوان يُختار على الخريطة).
    /// </summary>
    public class RestaurantNewDeliveryViewController : BaseViewController
    {
        private readonly List<Dictionary<string, object>> _areas;
        private Dictionary<string, object> _area;

        private UITextField _customerName, _customerPhone, _pickup, _delivery;
        private UILabel _areaValue, _fareValue, _coords, _pickupCoords;
        private double _pickupLat, _pickupLng, _deliveryLat, _deliveryLng;
        private readonly Dictionary<string, object> _repeat;

        /* اختيار سريع للوجهة: محافظة ← مدينة ← مناطق ← شوارع */
        private string _governorate = "", _city = "";
        private UILabel _govValue, _cityValue;
        private UIStackView _areaChipsRow, _streetChipsRow;
        private int _areaSeq, _streetSeq;

        public RestaurantNewDeliveryViewController(List<Dictionary<string, object>> areas)
            : this(areas, null) { }

        /// <summary>إن مُرِّرت توصيلة سابقة تُملأ نفس البيانات (إعادة الطلب).</summary>
        public RestaurantNewDeliveryViewController(List<Dictionary<string, object>> areas, Dictionary<string, object> repeat)
            : base("توصيلة جديدة")
        {
            _areas = areas ?? new List<Dictionary<string, object>>();
            _repeat = repeat;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadRestaurant();
            ApplyRepeat();
        }

        /// <summary>ملء بيانات توصيلة سابقة (عميل/عناوين/منطقة/موقع التوصيل).</summary>
        private void ApplyRepeat()
        {
            if (_repeat == null) return;

            _customerName.Text = Row.Str(_repeat, "CustomerName");
            _customerPhone.Text = Row.Str(_repeat, "CustomerPhone");
            _delivery.Text = Row.Str(_repeat, "DeliveryAddress");

            _deliveryLat = Row.Dbl(_repeat, "DeliveryLat");
            _deliveryLng = Row.Dbl(_repeat, "DeliveryLng");
            if (_deliveryLat != 0 || _deliveryLng != 0)
                _coords.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "الإحداثيات: {0:0.0000}, {1:0.0000}", _deliveryLat, _deliveryLng);

            var areaId = Row.Int(_repeat, "AreaId");
            for (int i = 0; i < _areas.Count; i++)
            {
                if (Row.Int(_areas[i], "Id") != areaId) continue;
                _area = _areas[i];
                _areaValue.Text = Row.Str(_area, "Name");
                _areaValue.TextColor = Theme.TextPrimary;
                _fareValue.Text = "سعر التوصيل: " + Row.Num(_area, "Price").ToString("0.00") + " ج.م";
                break;
            }
        }

        /// <summary>موقع المطعم المحفوظ (من التسجيل) يُستخدم تلقائيًا كنقطة استلام.</summary>
        private void LoadRestaurant()
        {
            Ui.Run(() =>
            {
                Dictionary<string, object> rest = null;
                try { rest = Api.MyRestaurant(); } catch { }
                Ui.BeginOnMain(() =>
                {
                    if (rest == null) return;
                    var addr = Row.Str(rest, "Address");
                    if (!string.IsNullOrWhiteSpace(addr) && string.IsNullOrWhiteSpace(_pickup.Text))
                        _pickup.Text = addr;

                    _pickupLat = Row.Dbl(rest, "Lat");
                    _pickupLng = Row.Dbl(rest, "Lng");
                    if (_pickupCoords != null)
                    {
                        _pickupCoords.Text = (_pickupLat != 0 || _pickupLng != 0)
                            ? "موقع المطعم: " + _pickupLat.ToString("0.#####") + ", " + _pickupLng.ToString("0.#####")
                            : "لم يُحدَّد موقع المطعم بعد";
                    }

                    // نبدأ من محافظة/مدينة المطعم — أغلب التوصيلات في نفس المدينة
                    var gov = Row.Str(rest, "Governorate");
                    var city = Row.Str(rest, "City");
                    if (!string.IsNullOrEmpty(gov))
                    {
                        _governorate = gov;
                        _govValue.Text = gov;
                        _govValue.TextColor = Theme.TextPrimary;
                    }
                    if (!string.IsNullOrEmpty(city))
                    {
                        _city = city;
                        _cityValue.Text = city;
                        _cityValue.TextColor = Theme.TextPrimary;
                        LoadCityPlaces(city);
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            /* ---------- المنطقة والسعر (أعلى الشاشة زي أندرويد) ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("المنطقة والسعر"));

            _areaValue = MakeLabel("اختر المنطقة", 15f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _areaValue.UserInteractionEnabled = true;
            _areaValue.AddGestureRecognizer(new UITapGestureRecognizer(() => PickArea()));
            stack.AddArrangedSubview(_areaValue);

            _fareValue = MakeLabel("—", 20f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            stack.AddArrangedSubview(_fareValue);

            /* ---------- العناوين ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("العناوين"));

            _pickup = Controls.TextField("عنوان الاستلام (المطعم)");
            stack.AddArrangedSubview(_pickup);

            _pickupCoords = MakeLabel("جارٍ تحميل موقع المطعم…", 11f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_pickupCoords);

            _delivery = Controls.TextField("عنوان التوصيل");
            stack.AddArrangedSubview(_delivery);

            _coords = MakeLabel("", 11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_coords);

            var pick = Controls.OutlineButton("تحديد موقع التوصيل على الخريطة");
            pick.TouchUpInside += (s, e) =>
            {
                Push(MapViewController.Pick(_deliveryLat, _deliveryLng, (lat, lng, address) =>
                {
                    _deliveryLat = lat; _deliveryLng = lng;
                    _coords.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "الإحداثيات: {0:0.0000}, {1:0.0000}", lat, lng);
                    if (!string.IsNullOrWhiteSpace(address)) _delivery.Text = address;
                }));
            };
            stack.AddArrangedSubview(pick);

            /* ---------- اختيار سريع للوجهة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("اختيار سريع للوجهة"));

            var govCard = MakeSelectorCard("المحافظة", out _govValue, () => PickGovernorate());
            stack.AddArrangedSubview(govCard);

            var cityCard = MakeSelectorCard("المدينة", out _cityValue, () => PickCity());
            stack.AddArrangedSubview(cityCard);

            stack.AddArrangedSubview(Controls.SectionTitle("المناطق"));
            _areaChipsRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_areaChipsRow);

            stack.AddArrangedSubview(Controls.SectionTitle("أهم الشوارع"));
            _streetChipsRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_streetChipsRow);

            /* ---------- بيانات العميل ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("بيانات العميل"));
            _customerName = Controls.TextField("اسم العميل");
            _customerPhone = Controls.TextField("رقم الهاتف");
            _customerPhone.KeyboardType = UIKeyboardType.PhonePad;
            _customerPhone.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            stack.AddArrangedSubview(_customerName);
            stack.AddArrangedSubview(_customerPhone);

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

            Ui.Choose("منطقة التوصيل", names, index => SelectArea(_areas[index]));
        }

        private void SelectArea(Dictionary<string, object> a)
        {
            _area = a;
            _areaValue.Text = Row.Str(_area, "Name");
            _areaValue.TextColor = Theme.TextPrimary;

            var price = Row.Num(_area, "Price");
            _fareValue.Text = "سعر التوصيل: " + price.ToString("0.00") + " ج.م";
        }

        /* ---------- المحافظة / المدينة / المناطق / الشوارع ---------- */

        private void PickGovernorate()
        {
            var list = EgyptPlaces.Governorates();
            if (list.Count == 0) { Ui.Toast("لا توجد محافظات"); return; }

            Ui.Choose("المحافظة", list, index =>
            {
                _governorate = list[index];
                _govValue.Text = _governorate;
                _govValue.TextColor = Theme.TextPrimary;
                _city = "";
                _cityValue.Text = "اختر المدينة";
                _cityValue.TextColor = Theme.TextSecondary;
                ClearChips(_areaChipsRow);
                ClearChips(_streetChipsRow);
            });
        }

        private void PickCity()
        {
            if (string.IsNullOrEmpty(_governorate)) { Ui.Toast("اختر المحافظة أولًا"); return; }

            var list = EgyptPlaces.CitiesOf(_governorate);
            if (list.Count == 0) { Ui.Toast("لا توجد مدن"); return; }

            Ui.Choose("المدينة", list, index =>
            {
                _city = list[index];
                _cityValue.Text = _city;
                _cityValue.TextColor = Theme.TextPrimary;
                LoadCityPlaces(_city);
            });
        }

        private void LoadCityPlaces(string city)
        {
            var seq = ++_areaSeq;
            Ui.Run(() =>
            {
                var areas = Api.Areas(city);
                if (seq != _areaSeq) return;
                Ui.BeginOnMain(() => RenderAreas(areas));
            }, null, ex => AppServices.Log("TawselaRestaurantDelivery", "areas: " + ex.Message));

            var sseq = ++_streetSeq;
            Ui.Run(() =>
            {
                var streets = Api.Streets(city);
                if (sseq != _streetSeq) return;
                Ui.BeginOnMain(() => RenderStreets(streets));
            }, null, ex => AppServices.Log("TawselaRestaurantDelivery", "streets: " + ex.Message));
        }

        private static void ClearChips(UIStackView row)
        {
            for (int i = row.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = row.ArrangedSubviews[i];
                row.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }
        }

        /// <summary>اختيار منطقة: يملأ عنوان التوصيل وإحداثياته، ويطابق منطقة التسعير تلقائيًا.</summary>
        private void ApplyDestination(string name, double lat, double lng, string cityHint = null)
        {
            _delivery.Text = name;
            _deliveryLat = lat;
            _deliveryLng = lng;
            _coords.Text = Coords(lat, lng);

            // مطابقة اسم المنطقة/المدينة مع منطقة تسعير مسجّلة
            Dictionary<string, object> match = null;
            foreach (var a in _areas)
            {
                if (string.Equals(Row.Str(a, "Name"), name, StringComparison.OrdinalIgnoreCase))
                { match = a; break; }
            }
            if (match == null && !string.IsNullOrEmpty(cityHint))
            {
                foreach (var a in _areas)
                {
                    if (string.Equals(Row.Str(a, "Name"), cityHint, StringComparison.OrdinalIgnoreCase))
                    { match = a; break; }
                }
            }
            if (match != null) SelectArea(match);
        }

        private void RenderAreas(List<Area> areas)
        {
            ClearChips(_areaChipsRow);
            if (areas == null || areas.Count == 0)
            {
                _areaChipsRow.AddArrangedSubview(MakeLabel("لا توجد مناطق مسجلة لهذه المدينة",
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            var count = Math.Min(3, areas.Count);
            for (int i = 0; i < count; i++)
            {
                var a = areas[i];
                var chip = Controls.ChipButton(a.Name, () => ApplyDestination(a.Name, a.Lat, a.Lng, a.City));
                _areaChipsRow.AddArrangedSubview(chip);
            }

            if (areas.Count > 3)
            {
                var more = Controls.ChipButton("الكل ▾", () =>
                {
                    var names = new List<string>();
                    foreach (var a in areas) names.Add(a.Name);
                    Ui.Choose("كل المناطق", names, index =>
                    {
                        var a = areas[index];
                        ApplyDestination(a.Name, a.Lat, a.Lng, a.City);
                    });
                });
                _areaChipsRow.AddArrangedSubview(more);
            }
        }

        private void RenderStreets(List<Street> streets)
        {
            ClearChips(_streetChipsRow);
            if (streets == null || streets.Count == 0)
            {
                _streetChipsRow.AddArrangedSubview(MakeLabel("لا توجد شوارع مسجلة لهذه المدينة",
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            var count = Math.Min(3, streets.Count);
            for (int i = 0; i < count; i++)
            {
                var s = streets[i];
                var chip = Controls.StreetChip(s.Name, () => ApplyDestination(s.Name, s.Lat, s.Lng, s.City));
                _streetChipsRow.AddArrangedSubview(chip);
            }

            if (streets.Count > 3)
            {
                var more = Controls.StreetChip("الكل ▾", () =>
                {
                    var names = new List<string>();
                    foreach (var s in streets) names.Add(s.Name);
                    Ui.Choose("كل الشوارع", names, index =>
                    {
                        var s = streets[index];
                        ApplyDestination(s.Name, s.Lat, s.Lng, s.City);
                    });
                });
                _streetChipsRow.AddArrangedSubview(more);
            }
        }

        private static string Coords(double lat, double lng)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "الإحداثيات: {0:0.0000}, {1:0.0000}", lat, lng);
        }

        /// <summary>بطاقة اختيار (عنوان صغير + قيمة قابلة للنقر).</summary>
        private UIView MakeSelectorCard(string title, out UILabel value, Action onTap)
        {
            var card = Controls.Card();
            var label = MakeLabel(title, 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            var v = new UILabel
            {
                Text = "—",
                Font = Theme.FontMedium(15),
                TextColor = Theme.TextSecondary,
                TextAlignment = UITextAlignment.Right,
                UserInteractionEnabled = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            v.AddGestureRecognizer(new UITapGestureRecognizer(onTap));
            card.AddGestureRecognizer(new UITapGestureRecognizer(onTap));

            card.AddSubview(label);
            card.AddSubview(v);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(v, NSLayoutAttribute.Top, NSLayoutRelation.Equal, label, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(v, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(v, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(v, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -8),
            });

            value = v;
            return card;
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
            if (_deliveryLat == 0 && _deliveryLng == 0) { Ui.Toast("حدد موقع التوصيل على الخريطة"); return; }

            var areaId = Row.Int(_area, "Id");
            var fare = Row.Num(_area, "Price");

            var progress = Ui.ShowProgress("جارٍ إرسال الطلب…");
            Ui.Run(() => Api.RequestRestaurantDelivery(areaId, name, phone, pickup,
                _pickupLat, _pickupLng, delivery, _deliveryLat, _deliveryLng, fare), () =>
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
