using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RegisterActivity.cs (الجزء الأساسي للنموذج):
    /// عميل أو سائق، بيانات الحساب، المحافظة/المدينة، بيانات السيارة للسائق،
    /// الموافقة على الشروط، ثم Api.Register.
    /// </summary>
    public class RegisterViewController : BaseViewController
    {
        private UISegmentedControl _role;
        private UITextField _fullName, _phone, _user, _pass, _referral, _carModel, _carPlate, _carColor;
        private UITextField _supervisorCode, _restaurantName, _restaurantAddress;
        private UILabel _govValue, _cityValue, _message, _vehicleLabel, _supervisorName, _restCoords;
        private double _restLat, _restLng;
        private UISwitch _terms;
        private UIView _driverSection, _restaurantSection;
        private UIButton _btnRegister;

        private string _governorate = "";
        private string _city = "";

        public RegisterViewController() : base("إنشاء حساب") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            _role = new UISegmentedControl(new[] { "عميل", "سائق", "مطعم" });
            _role.SelectedSegment = 0;
            _role.SelectedSegmentTintColor = Theme.Primary;
            _role.SetTitleTextAttributes(new UIStringAttributes { ForegroundColor = UIColor.White, Font = Theme.FontMedium(14) },
                UIControlState.Selected);
            _role.SetTitleTextAttributes(new UIStringAttributes { ForegroundColor = Theme.TextPrimary, Font = Theme.FontMedium(14) },
                UIControlState.Normal);
            _role.TranslatesAutoresizingMaskIntoConstraints = false;
            _role.ValueChanged += (s, e) => UpdateRole();
            stack.AddArrangedSubview(_role);

            _fullName = Controls.TextField("الاسم الكامل");
            _phone = Controls.TextField("رقم الهاتف");
            _phone.KeyboardType = UIKeyboardType.PhonePad;
            _phone.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;

            _user = Controls.TextField("اسم المستخدم (بالإنجليزية)");
            _pass = Controls.TextField("كلمة المرور", true);

            stack.AddArrangedSubview(_fullName);
            stack.AddArrangedSubview(_phone);
            stack.AddArrangedSubview(_user);
            stack.AddArrangedSubview(_pass);

            // المحافظة ثم المدينة (بديل spinnerGovernorate / spinnerCity)
            _govValue = MakeSelector("المحافظة", "اختر المحافظة", () => PickGovernorate());
            stack.AddArrangedSubview(SelectorRow("المحافظة", _govValue));

            _cityValue = MakeSelector("المدينة", "اختر المدينة", () => PickCity());
            stack.AddArrangedSubview(SelectorRow("المدينة", _cityValue));

            _referral = Controls.TextField("كود الإحالة (اختياري)");
            stack.AddArrangedSubview(_referral);

            // --- قسم السائق ---
            _vehicleLabel = Controls.SectionTitle("بيانات المركبة");
            _carModel = Controls.TextField("موديل السيارة / التوك توك");
            _carPlate = Controls.TextField("رقم اللوحة");
            _carColor = Controls.TextField("لون المركبة");

            // كود المشرف (اختياري) مع التحقق الفوري (بديل Api.LookupSupervisor)
            _supervisorCode = Controls.TextField("كود المشرف (اختياري)");
            _supervisorCode.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;
            _supervisorCode.EditingDidEnd += (s, e) => CheckSupervisor();

            _supervisorName = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Medium, UITextAlignment.Right);
            _supervisorName.Hidden = true;

            _driverSection = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 12,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            ((UIStackView)_driverSection).AddArrangedSubview(_vehicleLabel);
            ((UIStackView)_driverSection).AddArrangedSubview(_carModel);
            ((UIStackView)_driverSection).AddArrangedSubview(_carPlate);
            ((UIStackView)_driverSection).AddArrangedSubview(_carColor);
            ((UIStackView)_driverSection).AddArrangedSubview(_supervisorCode);
            ((UIStackView)_driverSection).AddArrangedSubview(_supervisorName);
            stack.AddArrangedSubview(_driverSection);

            // --- قسم المطعم ---
            var restLabel = Controls.SectionTitle("بيانات المطعم");
            _restaurantName = Controls.TextField("اسم المطعم");
            _restaurantAddress = Controls.TextField("عنوان المطعم");

            _restaurantSection = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 12,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            ((UIStackView)_restaurantSection).AddArrangedSubview(restLabel);
            ((UIStackView)_restaurantSection).AddArrangedSubview(_restaurantName);
            ((UIStackView)_restaurantSection).AddArrangedSubview(_restaurantAddress);

            // موقع المطعم يُحدَّد مرة واحدة عند التسجيل ويُستخدم تلقائيًا في طلبات التوصيل
            var pickRestLocation = Controls.OutlineButton("تحديد موقع المطعم على الخريطة");
            pickRestLocation.TouchUpInside += (s, e) =>
            {
                Push(MapViewController.Pick(_restLat, _restLng, (lat, lng, address) =>
                {
                    _restLat = lat;
                    _restLng = lng;
                    UpdateRestCoords();
                }));
            };
            _restCoords = MakeLabel("لم يُحدَّد الموقع بعد — لا بد من تحديده مرة واحدة عند التسجيل",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            ((UIStackView)_restaurantSection).AddArrangedSubview(pickRestLocation);
            ((UIStackView)_restaurantSection).AddArrangedSubview(_restCoords);

            stack.AddArrangedSubview(_restaurantSection);

            // الشروط (إلزامية — بديل chkTerms)
            var termsRow = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            var termsLabel = MakeLabel("أوافق على شروط الاستخدام وسياسة الخصوصية", 13f,
                Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            termsLabel.UserInteractionEnabled = true;
            termsLabel.AddGestureRecognizer(new UITapGestureRecognizer(() => ShowTerms()));

            _terms = new UISwitch { OnTintColor = Theme.Primary, TranslatesAutoresizingMaskIntoConstraints = false };
            termsRow.AddSubview(termsLabel);
            termsRow.AddSubview(_terms);
            termsRow.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_terms, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, termsRow, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_terms, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, termsRow, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(termsLabel, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, termsRow, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(termsLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _terms, NSLayoutAttribute.Trailing, 1, 10),
                NSLayoutConstraint.Create(termsLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, termsRow, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(termsRow, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
            });
            stack.AddArrangedSubview(termsRow);

            _message = MakeLabel("", 13f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Right);
            _message.Hidden = true;
            stack.AddArrangedSubview(_message);

            _btnRegister = Controls.PrimaryButton("إنشاء الحساب");
            _btnRegister.TouchUpInside += (s, e) => DoRegister();
            stack.AddArrangedSubview(_btnRegister);

            UpdateRole();
        }

        private UILabel MakeSelector(string label, string placeholder, Action onTap)
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

        private void UpdateRole()
        {
            var isDriver = _role.SelectedSegment == 1;
            var isRestaurant = _role.SelectedSegment == 2;
            _driverSection.Hidden = !isDriver;
            if (_restaurantSection != null) _restaurantSection.Hidden = !isRestaurant;
            if (_referral != null) _referral.Hidden = isRestaurant;
        }

        private void PickGovernorate()
        {
            var list = new List<string>();
            foreach (var gov in EgyptPlaces.Governorates()) list.Add(gov);
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
            foreach (var city in EgyptPlaces.CitiesOf(_governorate)) list.Add(city);
            if (list.Count == 0) { Ui.Toast("لا توجد مدن في هذه المحافظة"); return; }

            Ui.Choose("المدينة", list, index =>
            {
                _city = list[index];
                _cityValue.Text = _city;
                _cityValue.TextColor = Theme.TextPrimary;
            });
        }

        private void ShowTerms()
        {
            Ui.Choose("المستندات القانونية",
                new[] { "سياسة الخصوصية", "شروط الاستخدام" },
                index => Legal.Open(index == 0 ? Legal.PrivacyUrl : Legal.TermsUrl, err => Ui.Toast(err)));
        }

        /// <summary>التحقق من كود المشرف وعرض اسمه (بديل Api.LookupSupervisor في RegisterActivity).</summary>
        private void CheckSupervisor()
        {
            var code = (_supervisorCode.Text ?? "").Trim().Replace("-", "").Replace(" ", "");
            if (code.Length < 2)
            {
                _supervisorName.Hidden = true;
                return;
            }

            Ui.Run(() =>
            {
                string name;
                try { name = Api.LookupSupervisor(code); }
                catch { name = null; }

                Ui.BeginOnMain(() =>
                {
                    _supervisorName.Hidden = false;

                    if (!string.IsNullOrEmpty(name))
                    {
                        _supervisorName.Text = "المشرف: " + name;
                        _supervisorName.TextColor = Theme.Green;
                    }
                    else
                    {
                        _supervisorName.Text = "كود المشرف غير صحيح";
                        _supervisorName.TextColor = Theme.Red;
                    }
                });
            }, null, ex => AppServices.Log("TawselaRegister", "supervisor: " + ex.Message));
        }

        private void DoRegister()
        {
            if (!Net.IsOnline()) { ShowMessage("لا يوجد اتصال بالإنترنت"); return; }

            var isDriver = _role.SelectedSegment == 1;
            var isRestaurant = _role.SelectedSegment == 2;
            var role = isRestaurant ? "مطعم" : isDriver ? "سائق" : "عميل";

            var fullName = (_fullName.Text ?? "").Trim();
            var phone = (_phone.Text ?? "").Trim();
            var user = (_user.Text ?? "").Trim();
            var pass = _pass.Text ?? "";

            if (fullName == "") { ShowMessage("أدخل الاسم الكامل"); return; }
            if (phone == "") { ShowMessage("أدخل رقم الهاتف"); return; }
            if (user == "") { ShowMessage("أدخل اسم المستخدم"); return; }
            if (pass.Length < 6) { ShowMessage("كلمة المرور 6 أحرف على الأقل"); return; }
            if (string.IsNullOrEmpty(_governorate) || string.IsNullOrEmpty(_city))
            {
                ShowMessage("اختر المحافظة والمدينة");
                return;
            }
            if (!_terms.On) { ShowMessage("يجب الموافقة على الشروط"); return; }

            var carModel = isDriver ? (_carModel.Text ?? "").Trim() : "";
            var carPlate = isDriver ? (_carPlate.Text ?? "").Trim() : "";
            var carColor = isDriver ? (_carColor.Text ?? "").Trim() : "";

            if (isDriver && (carModel == "" || carPlate == ""))
            {
                ShowMessage("أدخل موديل السيارة ورقم اللوحة");
                return;
            }

            var restaurantName = isRestaurant ? (_restaurantName.Text ?? "").Trim() : "";
            var restaurantAddress = isRestaurant ? (_restaurantAddress.Text ?? "").Trim() : "";
            if (isRestaurant && restaurantName == "") { ShowMessage("أدخل اسم المطعم"); return; }
            if (isRestaurant && restaurantAddress == "") { ShowMessage("أدخل عنوان المطعم"); return; }
            if (isRestaurant && _restLat == 0 && _restLng == 0) { ShowMessage("حدد موقع المطعم على الخريطة"); return; }

            var restLat = isRestaurant ? _restLat : 0;
            var restLng = isRestaurant ? _restLng : 0;

            var referral = (_referral.Text ?? "").Trim();
            var supervisorCode = isDriver ? (_supervisorCode.Text ?? "").Trim() : "";

            _btnRegister.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ إنشاء الحساب…");

            Ui.Run(
                () => Api.Register(user, pass, fullName, phone, role, carModel, carPlate, carColor,
                    referral, _governorate, _city, supervisorCode,
                    restaurantName, 0, restaurantAddress, restLat, restLng),
                () =>
                {
                    progress.Dismiss();
                    _btnRegister.Enabled = true;
                    try { Platform.PushService.RegisterCurrentToken(); } catch { }
                    Ui.Toast("تم إنشاء الحساب بنجاح");
                    Router.GoHome();
                },
                ex =>
                {
                    progress.Dismiss();
                    _btnRegister.Enabled = true;
                    ShowMessage(ex == null ? "تعذّر إنشاء الحساب" : ex.Message);
                });
        }

        private void UpdateRestCoords()
        {
            if (_restCoords == null) return;
            _restCoords.Text = (_restLat != 0 || _restLng != 0)
                ? "موقع المطعم: " + _restLat.ToString("0.#####") + ", " + _restLng.ToString("0.#####")
                : "لم يُحدَّد الموقع بعد — لا بد من تحديده مرة واحدة عند التسجيل";
        }

        private void ShowMessage(string text)
        {
            _message.Text = text ?? "";
            _message.Hidden = string.IsNullOrEmpty(text);
        }
    }
}
