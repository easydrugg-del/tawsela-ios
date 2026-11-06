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
    /// بديل Activities/RegisterActivity.cs (الجزء الأساسي للنموذج):
    /// عميل أو سائق، بيانات الحساب، المحافظة/المدينة، بيانات السيارة للسائق،
    /// الموافقة على الشروط، ثم Api.Register.
    /// </summary>
    public class RegisterViewController : BaseViewController
    {
        private UISegmentedControl _role;
        private UITextField _fullName, _phone, _user, _pass, _referral, _carModel, _carPlate;
        private UITextField _supervisorCode, _restaurantName, _restaurantAddress;
        private UILabel _govValue, _cityValue, _message, _vehicleLabel, _vehicleValue, _supervisorName, _restCoords;
        private string _vehicleType = "";
        private double _restLat, _restLng;
        private UISwitch _terms;
        private UIView _driverSection, _restaurantSection;
        private UIButton _btnRegister;

        /* ---------- وثائق السائق (بديل linerDriverDocs في RegisterActivity) ---------- */

        private UITextField _opLicense;
        private readonly Dictionary<string, string> _docs = new Dictionary<string, string>();

        // نفس مفاتيح الخادم العربية المستخدمة في أندرويد
        private static readonly string[] DocKeys =
        {
            "فاتورة", "ترخيص_مرور", "بطاقة_سائق", "بطاقة_مالك", "اصل_فيش"
        };

        // نفس نصوص أزرار activity_register.xml
        private static readonly string[] DocLabels =
        {
            "📷 صورة الفاتورة *",
            "📷 ترخيص المرور (اختياري)",
            "📷 بطاقة السائق *",
            "📷 بطاقة المالك *",
            "📷 أصل الفيش *"
        };

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

            stack.AddArrangedSubview(MakeLabel("أنا:", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));

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

            _user = Controls.TextField("اسم المستخدم (للدخول)");
            _pass = Controls.TextField("كلمة المرور", true);

            stack.AddArrangedSubview(_fullName);
            stack.AddArrangedSubview(_phone);
            stack.AddArrangedSubview(_user);
            stack.AddArrangedSubview(_pass);

            // موقعك: المحافظة ثم المدينة (بديل spinnerGovernorate / spinnerCity)
            stack.AddArrangedSubview(Controls.SectionTitle("موقعك"));

            _govValue = MakeSelector("المحافظة", "اختر المحافظة", () => PickGovernorate());
            stack.AddArrangedSubview(SelectorRow("المحافظة", _govValue));

            _cityValue = MakeSelector("المدينة", "اختر المدينة", () => PickCity());
            stack.AddArrangedSubview(SelectorRow("المدينة", _cityValue));

            _referral = Controls.TextField("كود الإحالة (اختياري)");
            stack.AddArrangedSubview(_referral);

            // --- قسم السائق ---
            // نفس ترتيب أندرويد: نوع المركبة (عنوان) ← موديل ← لوحة ← نوع المركبة (spinner) ← كود المشرف
            _vehicleLabel = Controls.SectionTitle("بيانات المركبة");
            _carModel = Controls.TextField("موديل السيارة");
            _carPlate = Controls.TextField("رقم اللوحة");
            _vehicleValue = MakeSelector("نوع المركبة", "اختر نوع المركبة", () => PickVehicleType());

            // كود المشرف (اختياري) مع التحقق الفوري (بديل Api.LookupSupervisor)
            _supervisorCode = Controls.TextField("كود المشرف (رقمين - اختياري)");
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
            ((UIStackView)_driverSection).AddArrangedSubview(SelectorRow("نوع المركبة", _vehicleValue));
            ((UIStackView)_driverSection).AddArrangedSubview(_supervisorCode);
            ((UIStackView)_driverSection).AddArrangedSubview(_supervisorName);

            // وثائق السائق (مطلوبة) — رقم التشغيل + 5 صور مع حالة كل صورة
            var docsBox = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            docsBox.AddArrangedSubview(Controls.SectionTitle("وثائق السائق (مطلوبة)"));

            _opLicense = Controls.TextField("رقم التشغيل *");
            docsBox.AddArrangedSubview(_opLicense);

            for (int i = 0; i < DocKeys.Length; i++)
            {
                var key = DocKeys[i];
                var status = MakeLabel("لم تُرفع", 12f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right);

                var pick = Controls.OutlineButton(DocLabels[i]);
                pick.TouchUpInside += (s, e) => PickDoc(key, status);

                docsBox.AddArrangedSubview(pick);
                docsBox.AddArrangedSubview(status);
            }

            ((UIStackView)_driverSection).AddArrangedSubview(docsBox);

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
            // كود الإحالة للعميل فقط (أندرويد: linerReferral مخفي للسائق والمطعم)
            if (_referral != null) _referral.Hidden = isDriver || isRestaurant;
        }

        /// <summary>
        /// اختيار صورة مستند (معرض/كاميرا) وترميزها Base64 بحد 800px وجودة JPEG 70
        /// — نفس ShowImageSourceDialog + DecodeBitmap في أندرويد.
        /// </summary>
        private void PickDoc(string key, UILabel status)
        {
            Ui.Choose("اختر مصدر الصورة", new[] { "المعرض", "الكاميرا" }, index =>
            {
                Action<UIImage> apply = image =>
                {
                    if (image == null) return;

                    var scaled = ImagePicker.Scale(image, 800f);
                    string base64;
                    try
                    {
                        using (var data = scaled.AsJPEG(0.7f))
                            base64 = data == null ? "" : Convert.ToBase64String(data.ToArray());
                    }
                    catch
                    {
                        base64 = "";
                    }

                    if (string.IsNullOrEmpty(base64))
                    {
                        Ui.Toast("تعذّر قراءة الصورة");
                        return;
                    }

                    _docs[key] = base64;
                    status.Text = "✓ تم الرفع";
                    status.TextColor = Theme.Green;
                };

                if (index == 0)
                    ImagePicker.PickImage(this, false, apply, err => Ui.Toast(err));
                else
                    ImagePicker.PickImage(this, true, apply, err => Ui.Toast(err));
            });
        }

        /// <summary>صورة واحدة على الأقل من الفاتورة أو بطاقة السائق أو بطاقة المالك أو أصل الفيش.</summary>
        private bool HasRequiredDoc()
        {
            return _docs.ContainsKey("فاتورة") || _docs.ContainsKey("بطاقة_سائق") ||
                   _docs.ContainsKey("بطاقة_مالك") || _docs.ContainsKey("اصل_فيش");
        }

        /// <summary>نوع المركبة (بديل spinnerVehicleType في أندرويد — قيمته تُرسل في حقل carColor).</summary>
        private void PickVehicleType()
        {
            var types = new List<string> { "سيارة", "توكتوك", "سيارة كيوتة" };
            Ui.Choose("نوع المركبة", types, index =>
            {
                _vehicleType = types[index];
                _vehicleValue.Text = _vehicleType;
                _vehicleValue.TextColor = Theme.TextPrimary;
            });
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
        {            if (string.IsNullOrEmpty(_governorate))
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

            // نفس نصوص وترتيب تحقق أندرويد (RegisterActivity)
            if (fullName == "" || phone == "" || user == "" || pass == "")
            {
                ShowMessage("أكمل جميع الحقول الأساسية");
                return;
            }
            if (pass.Length < 6) { ShowMessage("كلمة المرور يجب أن تكون 6 أحرف على الأقل"); return; }
            if (!_terms.On)
            {
                ShowMessage("يجب الموافقة على شروط الاستخدام وسياسة الخصوصية");
                return;
            }

            var carModel = isDriver ? (_carModel.Text ?? "").Trim() : "";
            var carPlate = isDriver ? (_carPlate.Text ?? "").Trim() : "";
            var carColor = isDriver ? _vehicleType : "";

            if (string.IsNullOrEmpty(_governorate)) { ShowMessage("اختر المحافظة"); return; }
            if (string.IsNullOrEmpty(_city)) { ShowMessage("اختر المدينة"); return; }

            if (isDriver && (carModel == "" || carPlate == ""))
            {
                ShowMessage("أدخل بيانات السيارة");
                return;
            }

            var opLicense = isDriver ? (_opLicense.Text ?? "").Trim() : "";
            if (isDriver && opLicense == "") { ShowMessage("رقم التشغيل مطلوب"); return; }
            if (isDriver && !HasRequiredDoc())
            {
                ShowMessage("ارفع صورة واحدة على الأقل (الفاتورة أو بطاقة السائق أو بطاقة المالك أو أصل الفيش)");
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

            // نسخة من الصور بخمس مفاتيح دائمًا (أندرويد يرسل المفاتيح الخمسة ولو فاضية)
            var images = new Dictionary<string, string>();
            if (isDriver)
            {
                foreach (var key in DocKeys)
                {
                    string value;
                    images[key] = _docs.TryGetValue(key, out value) ? value : "";
                }
            }

            _btnRegister.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ إنشاء الحساب…");

            Ui.Run(
                () =>
                {
                    Api.Register(user, pass, fullName, phone, role, carModel, carPlate, carColor,
                        referral, _governorate, _city, supervisorCode,
                        restaurantName, 0, restaurantAddress, restLat, restLng);

                    // رفع وثائق السائق بعد إنشاء الحساب (بديل Api.AddDriverDocs في أندرويد)
                    if (isDriver) Api.AddDriverDocs(images, opLicense);
                },
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
