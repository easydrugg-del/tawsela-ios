using System;
using System.Collections.Generic;
using Tawsela.Data;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantProfileActivity.cs — تعديل المطعم لبياناته
    /// (اسم/هاتف/عنوان/محافظة/مدينة/منطقة التوصيل) من حسابه،
    /// عبر POST /api/restaurant-delivery/my-restaurant.
    /// </summary>
    public class RestaurantProfileViewController : BaseViewController
    {
        private UITextField _name, _phone, _address;
        private UILabel _govValue, _cityValue, _areaValue, _message, _coords;
        private readonly List<string> _areaNames = new List<string>();
        private readonly List<int> _areaIds = new List<int>();
        private int _areaId;
        private double _lat, _lng;
        private string _governorate = "", _city = "";
        private bool _loaded;

        public RestaurantProfileViewController() : base("بيانات المطعم") { }

        public override bool ShowNavigationBar
        {
            get { return true; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(Controls.SectionTitle("بيانات المطعم"));

            _name = Controls.TextField("اسم المطعم");
            _phone = Controls.TextField("رقم الهاتف");
            _phone.KeyboardType = UIKeyboardType.PhonePad;
            _phone.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            _address = Controls.TextField("عنوان المطعم");

            stack.AddArrangedSubview(_name);
            stack.AddArrangedSubview(_phone);
            stack.AddArrangedSubview(_address);

            stack.AddArrangedSubview(Controls.SectionTitle("الموقع"));

            _govValue = MakeSelector("اختر المحافظة", PickGovernorate);
            stack.AddArrangedSubview(SelectorRow("المحافظة", _govValue));

            _cityValue = MakeSelector("اختر المدينة", PickCity);
            stack.AddArrangedSubview(SelectorRow("المدينة", _cityValue));

            stack.AddArrangedSubview(Controls.SectionTitle("منطقة التوصيل"));
            _areaValue = MakeSelector("جارٍ التحميل…", PickArea);
            stack.AddArrangedSubview(SelectorRow("منطقة التوصيل", _areaValue));

            stack.AddArrangedSubview(Controls.SectionTitle("الموقع على الخريطة"));
            var mapButton = Controls.OutlineButton("تحديد موقع المطعم على الخريطة");
            mapButton.TouchUpInside += (s, e) => PickLocation();
            stack.AddArrangedSubview(mapButton);

            _coords = MakeLabel("لم يُحدَّد الموقع بعد", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_coords);

            _message = MakeLabel("", 13f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Right);
            _message.Hidden = true;
            stack.AddArrangedSubview(_message);

            var save = Controls.PrimaryButton("حفظ التعديلات");
            save.TouchUpInside += (s, e) => Save();
            stack.AddArrangedSubview(save);

            var delete = Controls.DangerButton("حذف الحساب");
            delete.TouchUpInside += (s, e) =>
                Ui.Confirm("حذف الحساب",
                    "سيُحذف حساب المطعم نهائيًا ويُفصل عن بيانات المطعم، ولن تستطيع الدخول مرة أخرى. متابعة؟",
                    "حذف نهائي", DeleteAccount);
            stack.AddArrangedSubview(delete);

            Load();
        }

        private UILabel MakeSelector(string placeholder, Action onTap)
        {
            var value = new UILabel
            {
                Text = placeholder,
                Font = Theme.Font(15),
                TextColor = Theme.TextSecondary,
                TextAlignment = UITextAlignment.Right,
                UserInteractionEnabled = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            value.AddGestureRecognizer(new UITapGestureRecognizer(onTap));
            return value;
        }

        private UIView SelectorRow(string label, UILabel value)
        {
            var card = Controls.Card();
            var title = MakeLabel(label, 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            card.AddSubview(title);
            card.AddSubview(value);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(value, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(value, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(value, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(value, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -8),
            });
            return card;
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                List<Dictionary<string, object>> areas = null;
                Dictionary<string, object> rest = null;
                try { areas = Api.RestaurantDeliveryAreas(); } catch { }
                try { rest = Api.MyRestaurant(); } catch { }
                Ui.BeginOnMain(() => Bind(areas, rest));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Bind(List<Dictionary<string, object>> areas, Dictionary<string, object> rest)
        {
            _areaNames.Clear();
            _areaIds.Clear();
            _areaNames.Add("بدون منطقة محددة");
            _areaIds.Add(0);

            if (areas != null)
            {
                foreach (var a in areas)
                {
                    _areaNames.Add(Tawsela.Data.Row.Str(a, "Name"));
                    _areaIds.Add(Tawsela.Data.Row.Int(a, "Id"));
                }
            }

            if (rest != null)
            {
                _name.Text = Tawsela.Data.Row.Str(rest, "Name");
                _phone.Text = Tawsela.Data.Row.Str(rest, "Phone");
                _address.Text = Tawsela.Data.Row.Str(rest, "Address");
                _lat = Tawsela.Data.Row.Dbl(rest, "Lat");
                _lng = Tawsela.Data.Row.Dbl(rest, "Lng");
                ShowCoords();

                _governorate = Tawsela.Data.Row.Str(rest, "Governorate");
                _city = Tawsela.Data.Row.Str(rest, "City");
                _govValue.Text = string.IsNullOrEmpty(_governorate) ? "اختر المحافظة" : _governorate;
                _govValue.TextColor = string.IsNullOrEmpty(_governorate) ? Theme.TextSecondary : Theme.TextPrimary;
                _cityValue.Text = string.IsNullOrEmpty(_city) ? "اختر المدينة" : _city;
                _cityValue.TextColor = string.IsNullOrEmpty(_city) ? Theme.TextSecondary : Theme.TextPrimary;

                var id = Tawsela.Data.Row.Int(rest, "AreaId");
                _areaId = id;
                var idx = _areaIds.IndexOf(id);
                if (idx > 0)
                {
                    _areaValue.Text = _areaNames[idx];
                    _areaValue.TextColor = Theme.TextPrimary;
                }
                else
                {
                    _areaValue.Text = "بدون منطقة محددة";
                    _areaValue.TextColor = Theme.TextSecondary;
                }
            }
            else
            {
                _areaValue.Text = "بدون منطقة محددة";
            }

            _loaded = true;
        }

        private void PickGovernorate()
        {
            var list = new List<string>();
            foreach (var g in EgyptPlaces.Governorates()) list.Add(g);
            if (list.Count == 0) { Ui.Toast("لا توجد محافظات متاحة"); return; }

            Ui.Choose("المحافظة", list, index =>
            {
                _governorate = list[index];
                _govValue.Text = _governorate;
                _govValue.TextColor = Theme.TextPrimary;
                _city = "";
                _cityValue.Text = "اختر المدينة";
                _cityValue.TextColor = Theme.TextSecondary;
            });
        }

        private void PickCity()
        {
            if (string.IsNullOrEmpty(_governorate))
            {
                Ui.Toast("اختر المحافظة أولًا");
                return;
            }

            var list = new List<string>();
            foreach (var c in EgyptPlaces.CitiesOf(_governorate)) list.Add(c);
            if (list.Count == 0) { Ui.Toast("لا توجد مدن في هذه المحافظة"); return; }

            Ui.Choose("المدينة", list, index =>
            {
                _city = list[index];
                _cityValue.Text = _city;
                _cityValue.TextColor = Theme.TextPrimary;
            });
        }

        private void PickArea()
        {
            if (!_loaded) { Ui.Toast("جارٍ التحميل…"); return; }
            Ui.Choose("منطقة التوصيل", _areaNames, index =>
            {
                if (index < 0 || index >= _areaIds.Count) return;
                _areaId = _areaIds[index];
                _areaValue.Text = _areaNames[index];
                _areaValue.TextColor = index == 0 ? Theme.TextSecondary : Theme.TextPrimary;
            });
        }

        private void PickLocation()
        {
            Push(MapViewController.Pick(_lat, _lng, (lat, lng, picked) =>
            {
                if (lat == 0 && lng == 0) return;
                _lat = lat;
                _lng = lng;
                ShowCoords();
            }));
        }

        private void ShowCoords()
        {
            if (_coords == null) return;
            _coords.Text = (_lat != 0 || _lng != 0)
                ? "الموقع: " + _lat.ToString("0.#####") + ", " + _lng.ToString("0.#####")
                : "لم يُحدَّد الموقع بعد";
        }

        private void Save()
        {
            var name = (_name.Text ?? "").Trim();
            if (name == "")
            {
                ShowMessage("أدخل اسم المطعم");
                return;
            }
            if (string.IsNullOrEmpty(_governorate))
            {
                ShowMessage("اختر المحافظة");
                return;
            }
            if (string.IsNullOrEmpty(_city))
            {
                ShowMessage("اختر المدينة");
                return;
            }

            var phone = (_phone.Text ?? "").Trim();
            var address = (_address.Text ?? "").Trim();
            var gov = _governorate;
            var city = _city;

            var progress = Ui.ShowProgress("جارٍ الحفظ…");
            Ui.Run(
                () => Api.UpdateMyRestaurant(name, phone, address, _areaId, _lat, _lng, gov, city),
                () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم حفظ بيانات المطعم");
                    Back();
                },
                ex =>
                {
                    progress.Dismiss();
                    ShowMessage(ex == null ? "تعذّر الحفظ" : ex.Message);
                });
        }

        private void DeleteAccount()
        {
            var progress = Ui.ShowProgress("جارٍ الحذف…");
            Ui.Run(() => Api.DeleteAccount(false),
                () =>
                {
                    progress.Dismiss();
                    try { Api.ClearSession(); } catch { }
                    Router.ShowLogin();
                },
                ex =>
                {
                    progress.Dismiss();
                    ShowMessage(ex == null ? "تعذّر الحذف" : ex.Message);
                });
        }

        private void ShowMessage(string text)
        {
            _message.Text = text ?? "";
            _message.Hidden = string.IsNullOrEmpty(text);
        }
    }
}
