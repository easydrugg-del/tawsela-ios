using System;
using System.Collections.Generic;
using System.Globalization;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/NewOrderActivity.cs:
    /// تحديد الانطلاق (GPS/كتابة/أماكن محفوظة/خريطة)، الوجهة، نقطة إضافية،
    /// المحافظة/المدينة، شرائح المناطق والشوارع من الخادم، نوع الخدمة،
    /// طريقة الدفع (مع رصيد المحفظة)، كود الخصم، الجدولة، المزايدة،
    /// الإكرامية، مشاركة الأجرة، أقصى مدة انتظار، ثم إنشاء الطلب.
    /// </summary>
    public class NewOrderViewController : BaseViewController
    {
        private UITextField _fromField, _toField, _noteField;
        private UITextField _couponField, _tipField, _splitPhoneField, _maxWaitField;
        private UITextField _offeredField, _floorField;
        private UITextField _stopField;
        private double _stopLat, _stopLng;
        private UILabel _fromCoords, _toCoords;
        private UILabel _fareLabel, _distanceLabel, _couponInfo, _scheduleInfo, _loyaltyInfo;
        private UILabel _tripFareLabel, _fareDetailsLabel;
        private UILabel _nearbyLabel, _streetsTitle;
        private UILabel _govValue, _cityValue;
        private UIButton _btnService, _btnPayment, _btnCreate, _btnPickTime;
        private UISwitch _scheduledSwitch, _biddingSwitch;
        private UIStackView _areaRow, _savedRow, _biddingBox;
        private UIStackView _streetGrid, _nearbyBox;
        private UITextField _streetSearch;
        private readonly List<Street> _streets = new List<Street>();
        private bool _showAllStreets;

        private double _fromLat, _fromLng;
        private double _toLat, _toLng;

        private string _governorate = "";
        private string _city = "";
        private List<ServiceType> _services = new List<ServiceType>();
        private ServiceType _service;
        private string _payment = "نقدي";
        private decimal _discount;
        private int _tierPercent;
        private decimal _tierAmount;
        private Coupon _coupon;
        private DateTime _scheduledAt = default(DateTime);

        private int _areaSeq, _streetSeq, _driversSeq;
        private UIStackView _recentRow;
        private UIScrollView _recentScroll;
        private UILabel _recentTitle;
        private VoiceInput _voice;

        // إعادة آخر رحلة (بديل prefFrom/prefTo/prefServiceId في أندرويد)
        private Order _prefill;
        private int _prefillServiceId;
        private bool _prefillApplied;

        public NewOrderViewController() : base("طلب توصيلة جديدة") { }

        /// <summary>يفتح الشاشة معبأة بنقاط آخر رحلة ونوع خدمتها (زر «توصيلة زي آخر مرة»).</summary>
        public NewOrderViewController(Order prefill) : base("طلب توصيلة جديدة")
        {
            _prefill = prefill;
        }

        /// <summary>تعبئة رحلة سابقة بعد إنشاء الشاشة (نفس سلوك Intent extras في أندرويد).</summary>
        public void SetPrefill(Order order)
        {
            _prefill = order;
            if (IsViewLoaded && order != null) ApplyPrefill();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();

            LoadServices();
            LoadSavedPlaces();
            LoadRecentDestinations();
            LoadTier();

            // مع الإعادة لا نستبدل نقطة الانطلاق بموقع الجهاز
            if (_prefill == null) RequestLocation();
            else ApplyPrefill();

            RefreshDriversCount();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // ضمان تعبئة بيانات آخر رحلة مرة واحدة بعد ظهور الشاشة
            // (بعض العناصر تُحدَّث بعد ViewDidLoad فلا نسمح لها بمسح التعبئة).
            if (_prefill != null && !_prefillApplied) ApplyPrefill();
        }

        /// <summary>تعبئة الحقول من رحلة سابقة (بديل Intent extras في ReorderLast).</summary>
        private void ApplyPrefill()
        {
            if (_prefill == null || _fromField == null) return;
            _prefillApplied = true;

            _prefillServiceId = _prefill.ServiceTypeId;

            _fromField.Text = _prefill.FromAddress ?? "";
            _fromLat = _prefill.FromLat;
            _fromLng = _prefill.FromLng;
            _fromCoords.Text = Coords(_fromLat, _fromLng);

            _toField.Text = _prefill.ToAddress ?? "";
            _toLat = _prefill.ToLat;
            _toLng = _prefill.ToLng;
            _toCoords.Text = Coords(_toLat, _toLng);

            // نفس أندرويد: حساب الأجرة فورًا بعد التعبئة (EstimateNow)
            UpdateEstimate();
            RefreshDriversCount();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            /* ---------- موقع الانطلاق ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("موقع الانطلاق"));

            _fromField = Controls.TextField("من أين؟ (العنوان)");
            _fromField.ReturnKeyType = UIReturnKeyType.Done;
            _fromField.EditingDidEnd += (s, e) => ResolveFrom();

            // زي الأندرويد: الحقل + زر ميكروفون مربّع 48×48 في نفس السطر
            var fromMic = Controls.SquareIconButton("ic_mic", Theme.Primary, () => Voice(text =>
            {
                _fromField.Text = text;
                ResolveFrom();
            }));
            stack.AddArrangedSubview(Controls.FieldWithAction(_fromField, fromMic));

            _fromCoords = MakeLabel("", 11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_fromCoords);

            var pickFrom = Controls.IconButton("تحديد موقع الانطلاق على الخريطة", "ic_pin", Theme.Primary);
            pickFrom.TouchUpInside += (s, e) =>
            {
                var start = LocationService.LastKnown;
                var lat = _fromLat != 0 ? _fromLat : (start.HasValue ? start.Value.Latitude : 0);
                var lng = _fromLng != 0 ? _fromLng : (start.HasValue ? start.Value.Longitude : 0);

                Push(MapViewController.Pick(lat, lng, (plat, plng, address) =>
                {
                    _fromLat = plat; _fromLng = plng;
                    _fromCoords.Text = Coords(plat, plng);
                    if (!string.IsNullOrWhiteSpace(address)) _fromField.Text = address;
                    UpdateEstimate();
                    RefreshDriversCount();
                }));
            };
            stack.AddArrangedSubview(pickFrom);

            // «السائقون القريبون منك:» (بديل rowNearbyDrivers في أندرويد)
            _nearbyLabel = MakeLabel("السائقون القريبون منك:", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right);
            _nearbyLabel.Hidden = true;
            stack.AddArrangedSubview(_nearbyLabel);

            _nearbyBox = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 6,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            stack.AddArrangedSubview(_nearbyBox);

            /* ---------- الوجهة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("الوجهة"));

            _toField = Controls.TextField("إلى أين؟ (العنوان)");
            _toField.EditingDidEnd += (s, e) => ResolveTo();

            var toMic = Controls.SquareIconButton("ic_mic", Theme.Primary, () => Voice(text =>
            {
                _toField.Text = text;
                ResolveTo();
            }));
            stack.AddArrangedSubview(Controls.FieldWithAction(_toField, toMic));

            _toCoords = MakeLabel("", 11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_toCoords);

            var pickTo = Controls.IconButton("تحديد الوجهة على الخريطة", "ic_flag", Theme.Primary);
            pickTo.TouchUpInside += (s, e) =>
            {
                Push(MapViewController.Pick(_toLat, _toLng, (plat, plng, address) =>
                {
                    _toLat = plat; _toLng = plng;
                    _toCoords.Text = Coords(plat, plng);
                    if (!string.IsNullOrWhiteSpace(address)) _toField.Text = address;
                    UpdateEstimate();
                }));
            };
            stack.AddArrangedSubview(pickTo);

            /* ---------- اختيار سريع للوجهة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("اختيار سريع للوجهة"));

            var govCard = MakeSelectorCard("المحافظة", out _govValue, () => PickGovernorate());
            stack.AddArrangedSubview(govCard);

            var cityCard = MakeSelectorCard("المدينة", out _cityValue, () => PickCity());
            stack.AddArrangedSubview(cityCard);

            stack.AddArrangedSubview(MakeLabel("المناطق:", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));

            _areaRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_areaRow);

            _streetsTitle = MakeLabel("أهم شوارع المدينة:", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_streetsTitle);

            // بحث الشوارع + الإدخال الصوتي (بديل txtStreetSearch/btnVoiceStreet)
            _streetSearch = Controls.TextField("ابحث عن شارع (جزء من الاسم)");
            _streetSearch.EditingChanged += (s, e) =>
            {
                _showAllStreets = false;
                BuildStreetGrid(_streetSearch.Text);
            };

            var streetMic = Controls.SquareIconButton("ic_mic", Theme.Primary, () => Voice(text =>
            {
                _streetSearch.Text = text;
                _showAllStreets = false;
                BuildStreetGrid(text);
            }));
            streetMic.AccessibilityLabel = "بحث بالصوت";
            stack.AddArrangedSubview(Controls.FieldWithAction(_streetSearch, streetMic));

            // شبكة 3 في السطر (بديل rowCityToChips في أندرويد)
            _streetGrid = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_streetGrid);

            /* ---------- محطة وسيطة (اختياري) ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("محطة وسيطة (اختياري)"));

            _stopField = Controls.TextField("عنوان المحطة الوسطى");
            stack.AddArrangedSubview(_stopField);

            var pickStop = Controls.OutlineButton("تحديد المحطة على الخريطة");
            pickStop.TouchUpInside += (s, e) =>
            {
                Push(MapViewController.Pick(_stopLat, _stopLng, (lat, lng, address) =>
                {
                    _stopLat = lat;
                    _stopLng = lng;
                    if (!string.IsNullOrWhiteSpace(address)) _stopField.Text = address;
                }));
            };
            stack.AddArrangedSubview(pickStop);

            /* ---------- تفاصيل الطلب ---------- */
            _splitPhoneField = Controls.TextField("مشاركة الأجرة مع رقم (اختياري)");
            _splitPhoneField.KeyboardType = UIKeyboardType.PhonePad;
            stack.AddArrangedSubview(_splitPhoneField);

            _floorField = Controls.TextField("رقم الشقة/الدور (اختياري)");
            stack.AddArrangedSubview(_floorField);

            _noteField = Controls.TextField("ملاحظات (اختياري)");
            stack.AddArrangedSubview(_noteField);

            stack.AddArrangedSubview(MakeLabel("أقصي وقت للانتظار (بالدقائق)", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));

            _maxWaitField = Controls.TextField("بالدقائق (افتراضي 30)");
            _maxWaitField.KeyboardType = UIKeyboardType.NumberPad;
            _maxWaitField.Text = "30";
            stack.AddArrangedSubview(_maxWaitField);

            stack.AddArrangedSubview(MakeLabel("بعد انتهاء الوقت يُخفى الطلب من شاشة السائقين تلقائياً",
                11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            /* ---------- التقدير (txtEstimate + txtLoyaltyInfo) ---------- */
            _distanceLabel = MakeLabel("حدد الانطلاق والوجهة لحساب المسافة", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_distanceLabel);

            _fareLabel = MakeLabel("—", 15f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            _fareLabel.Lines = 0;
            stack.AddArrangedSubview(_fareLabel);

            _loyaltyInfo = MakeLabel("", 12f, Theme.Green, UIFontWeight.Medium, UITextAlignment.Right);
            _loyaltyInfo.Hidden = true;
            stack.AddArrangedSubview(_loyaltyInfo);

            /* ---------- نوع الخدمة ---------- */
            stack.AddArrangedSubview(MakeLabel("نوع الخدمة", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));

            _btnService = Controls.IconButton("توصيلة عادية", "ic_bell", Theme.Primary);
            _btnService.TouchUpInside += (s, e) => PickService();
            stack.AddArrangedSubview(_btnService);

            /* ---------- طريقة الدفع ---------- */
            stack.AddArrangedSubview(MakeLabel("طريقة الدفع", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right));

            _btnPayment = Controls.IconButton("نقدي", "ic_money", Theme.Primary);
            _btnPayment.TouchUpInside += (s, e) => PickPayment();
            stack.AddArrangedSubview(_btnPayment);

            /* ---------- أماكن مفضلة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("أماكن مفضلة"));

            _savedRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_savedRow);

            // وجهاتك السابقة: آخر 5 وجهات كأزرار سريعة
            // (بديل قائمة «آخر وجهات» في أندرويد — تظهر فقط لو فيه طلبات سابقة)
            _recentTitle = MakeLabel("وجهاتك السابقة:", 13f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right);
            _recentTitle.Hidden = true;
            stack.AddArrangedSubview(_recentTitle);

            // صف أفقي قابل للتمرير حتى لا تخرج الشرائح الطويلة عن الشاشة
            _recentScroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ShowsHorizontalScrollIndicator = false,
                DirectionalLockEnabled = true,
                Hidden = true
            };
            _recentRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _recentScroll.AddSubview(_recentRow);
            _recentScroll.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_recentRow, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _recentScroll, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_recentRow, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _recentScroll, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_recentRow, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _recentScroll, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_recentRow, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _recentScroll, NSLayoutAttribute.Trailing, 1, 0),
            });
            _recentScroll.HeightAnchor.ConstraintEqualTo(44f).Active = true;
            stack.AddArrangedSubview(_recentScroll);

            var savePlace = Controls.IconButton("حفظ الانطلاق كمكان مفضّل", "ic_save", Theme.Primary);
            savePlace.TouchUpInside += (s, e) => SaveCurrentPlace();
            stack.AddArrangedSubview(savePlace);

            /* ---------- الجدولة ---------- */
            _scheduledSwitch = new UISwitch { OnTintColor = Theme.Primary, TranslatesAutoresizingMaskIntoConstraints = false };
            _scheduledSwitch.ValueChanged += (s, e) =>
            {
                _btnPickTime.Enabled = _scheduledSwitch.On;
                if (!_scheduledSwitch.On)
                {
                    _scheduledAt = default(DateTime);
                    _scheduleInfo.Text = "";
                    UpdateEstimate();
                }
            };
            stack.AddArrangedSubview(Row("طلب مجدول (في وقت لاحق)", _scheduledSwitch));

            _btnPickTime = Controls.IconButton("اختيار موعد التنفيذ", "ic_clock", Theme.Primary);
            _btnPickTime.Enabled = false;
            _btnPickTime.TouchUpInside += (s, e) => PickScheduleTime();
            stack.AddArrangedSubview(_btnPickTime);

            _scheduleInfo = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_scheduleInfo);

            /* ---------- كود الخصم ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("كود الخصم"));

            _couponField = Controls.TextField("أدخل الكود");
            _couponField.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;

            var applyCoupon = Controls.PrimaryButton("تطبيق", 48f);
            applyCoupon.WidthAnchor.ConstraintEqualTo(96f).Active = true;
            applyCoupon.TouchUpInside += (s, e) => ApplyCoupon();
            stack.AddArrangedSubview(Controls.FieldWithAction(_couponField, applyCoupon));

            _couponInfo = MakeLabel("", 12f, Theme.Green, UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_couponInfo);

            /* ---------- حقول مخفية زي أندرويد (تفاوض/إكرامية) ---------- */
            _biddingSwitch = new UISwitch { OnTintColor = Theme.Primary, TranslatesAutoresizingMaskIntoConstraints = false };
            _biddingBox = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            _offeredField = Controls.TextField("سعرك المقترح (ج.م) - اتركه فارغاً للسعر التقديري");

            _tipField = Controls.TextField("مبلغ الإكرامية بالج.م");
            _tipField.KeyboardType = UIKeyboardType.DecimalPad;

            /* ---------- إكرامية (اختياري) ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("إكرامية (اختياري)"));
            stack.AddArrangedSubview(_tipField);

            /* ---------- تكلفة الرحلة ---------- */
            var fareCard = Controls.Card();
            var fareTitle = MakeLabel("تكلفة الرحلة", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            _tripFareLabel = MakeLabel("—", 22f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            _fareDetailsLabel = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _fareDetailsLabel.Lines = 0;

            fareCard.AddSubview(fareTitle);
            fareCard.AddSubview(_tripFareLabel);
            fareCard.AddSubview(_fareDetailsLabel);
            fareCard.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(fareTitle, NSLayoutAttribute.Top, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(fareTitle, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(fareTitle, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_tripFareLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, fareTitle, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_tripFareLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_tripFareLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_fareDetailsLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _tripFareLabel, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_fareDetailsLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_fareDetailsLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_fareDetailsLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, fareCard, NSLayoutAttribute.Bottom, 1, -12),
            });
            stack.AddArrangedSubview(fareCard);

            _btnCreate = Controls.PrimaryButton("إرسال الطلب");
            _btnCreate.TouchUpInside += (s, e) => CreateOrder();
            stack.AddArrangedSubview(_btnCreate);

            // القيم الافتراضية للمحافظة/المدينة من الجلسة
            _governorate = Session.Governorate;
            _city = Session.City;
            _govValue.Text = string.IsNullOrEmpty(_governorate) ? "اختر المحافظة" : _governorate;
            _cityValue.Text = string.IsNullOrEmpty(_city) ? "اختر المدينة" : _city;
            if (!string.IsNullOrEmpty(_city)) LoadCityPlaces(_city);
        }

        private static string Coords(double lat, double lng)
        {
            return string.Format(CultureInfo.InvariantCulture, "الإحداثيات: {0:0.0000}, {1:0.0000}", lat, lng);
        }

        /// <summary>
        /// إدخال صوتي: يبدأ التسجيل، ويستدعي apply بالنتيجة الجزئية المحدَّثة.
        /// الضغط مرة أخرى أثناء التسجيل يوقفه (بديل SpeechRecognizer في أندرويد).
        /// </summary>
        private void Voice(Action<string> apply)
        {
            if (!VoiceInput.IsAvailable)
            {
                Ui.Toast("التعرّف الصوتي غير متاح على هذا الجهاز");
                return;
            }

            if (_voice != null && _voice.IsRunning)
            {
                _voice.Stop();
                _voice = null;
                Ui.Toast("تم إيقاف التسجيل");
                return;
            }

            _voice = new VoiceInput();
            Ui.Toast("تحدّث الآن… اضغط الزر مرة أخرى للإيقاف");

            _voice.Start(this, text =>
            {
                if (apply != null) apply(text);
            }, err =>
            {
                Ui.Toast(err);
                _voice = null;
            });
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (_voice != null) { _voice.Stop(); _voice = null; }
        }

        /// <summary>صف عنوان + مفتاح (بديل CheckBox في AXML).</summary>
        private UIView Row(string title, UISwitch toggle)
        {
            var row = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            row.AddArrangedSubview(toggle);
            row.AddArrangedSubview(MakeLabel(title, 14f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right));
            return row;
        }

        private UIView MakeSelectorCard(string title, out UILabel value, Action onTap)
        {
            var card = Controls.Card();
            var label = MakeLabel(title, 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            var v = new UILabel
            {
                Text = "—",
                Font = Theme.FontMedium(15),
                TextColor = Theme.TextPrimary,
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

        /* ---------- الموقع ---------- */

        private void RequestLocation(bool showToasts = false)
        {
            LocationService.RequestPermission(granted =>
            {
                if (!granted)
                {
                    if (showToasts) Ui.Toast("لم يتم السماح بالوصول للموقع");
                    return;
                }

                LocationService.RequestOneShot((coord, accuracy) =>
                {
                    _fromLat = coord.Latitude;
                    _fromLng = coord.Longitude;
                    Ui.BeginOnMain(() =>
                    {
                        _fromCoords.Text = Coords(_fromLat, _fromLng);
                        UpdateEstimate();
                        RefreshDriversCount();

                        // املأ العنوان تلقائيًا إن كانت الخانة فاضية
                        if (string.IsNullOrWhiteSpace(_fromField.Text))
                        {
                            Ui.Run(() =>
                            {
                                var addr = Geo.ReverseGeocode(_fromLat, _fromLng);
                                if (!string.IsNullOrWhiteSpace(addr))
                                    Ui.BeginOnMain(() => _fromField.Text = addr);
                            });
                        }
                    });
                }, err =>
                {
                    if (showToasts) Ui.Toast(err);
                    if (showToasts) LocationService.OpenSettings();
                });
            });
        }

        private void ResolveFrom()
        {
            var text = (_fromField.Text ?? "").Trim();
            if (text == "") return;

            Ui.Run(() =>
            {
                double lat, lng;
                if (Geo.ForwardGeocode(text, out lat, out lng))
                {
                    _fromLat = lat; _fromLng = lng;
                    Ui.BeginOnMain(() =>
                    {
                        _fromCoords.Text = Coords(lat, lng);
                        UpdateEstimate();
                        RefreshDriversCount();
                    });
                }
                else
                {
                    Ui.BeginOnMain(() => Ui.Toast("تعذّر تحديد العنوان — جرّب كتابة أدق"));
                }
            });
        }

        private void ResolveTo()
        {
            var text = (_toField.Text ?? "").Trim();
            if (text == "") return;

            Ui.Run(() =>
            {
                double lat, lng;
                if (Geo.ForwardGeocode(text, out lat, out lng))
                {
                    _toLat = lat; _toLng = lng;
                    Ui.BeginOnMain(() =>
                    {
                        _toCoords.Text = Coords(lat, lng);
                        UpdateEstimate();
                    });
                }
                else
                {
                    Ui.BeginOnMain(() => Ui.Toast("تعذّر تحديد الوجهة — اختر منطقة أو شارعًا"));
                }
            });
        }

        /* ---------- الأماكن المحفوظة ---------- */

        private void LoadSavedPlaces()
        {
            Ui.Run(() =>
            {
                var places = Api.SavedPlaces();
                Ui.BeginOnMain(() => RenderSavedPlaces(places));
            }, null, ex => AppServices.Log("TawselaNewOrder", "saved: " + ex.Message));
        }

        private void RenderSavedPlaces(List<SavedPlace> places)
        {
            ClearChips(_savedRow);
            if (places == null || places.Count == 0) return;

            foreach (var p in places)
            {
                var place = p;
                var chip = Controls.ChipButton(string.IsNullOrEmpty(place.Label) ? place.Address : place.Label, () =>
                {
                    _fromField.Text = string.IsNullOrEmpty(place.Label)
                        ? place.Address
                        : place.Label + " - " + place.Address;
                    _fromLat = place.Lat;
                    _fromLng = place.Lng;
                    _fromCoords.Text = Coords(place.Lat, place.Lng);
                    UpdateEstimate();
                    RefreshDriversCount();
                });
                _savedRow.AddArrangedSubview(chip);
            }
        }

        private void SaveCurrentPlace()
        {
            var address = (_fromField.Text ?? "").Trim();
            if (address == "") { Ui.Toast("حدد عنوان الانطلاق أولًا"); return; }

            Ui.Prompt("حفظ المكان", "اسم المكان (مثل: البيت / العمل)", "", "البيت", label =>
            {
                var name = (label ?? "").Trim();
                if (name == "") name = "مكان محفوظ";

                var progress = Ui.ShowProgress("جارٍ الحفظ…");
                Ui.Run(() => Api.AddSavedPlace(name, address, _fromLat, _fromLng), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم حفظ المكان");
                    LoadSavedPlaces();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast("خطأ: " + ex.Message);
                });
            });
        }

        /* ---------- المحافظة / المدينة ---------- */

        private void PickGovernorate()
        {
            var list = EgyptPlaces.Governorates();
            if (list.Count == 0) { Ui.Toast("لا توجد محافظات"); return; }

            Ui.Choose("المحافظة", list, index =>
            {
                _governorate = list[index];
                _govValue.Text = _governorate;
                _city = "";
                _cityValue.Text = "اختر المدينة";
                ClearChips(_areaRow);
                _streets.Clear();
                ClearStreetGrid();
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
            }, null, ex => AppServices.Log("TawselaNewOrder", "areas: " + ex.Message));

            var sseq = ++_streetSeq;
            Ui.Run(() =>
            {
                var streets = Api.Streets(city);
                if (sseq != _streetSeq) return;
                Ui.BeginOnMain(() => RenderStreets(streets));
            }, null, ex => AppServices.Log("TawselaNewOrder", "streets: " + ex.Message));
        }

        private void ClearChips(UIStackView row)
        {
            for (int i = row.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = row.ArrangedSubviews[i];
                row.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }
        }

        private void RenderAreas(List<Area> areas)
        {
            ClearChips(_areaRow);
            if (areas == null || areas.Count == 0)
            {
                _areaRow.AddArrangedSubview(MakeLabel("لا توجد مناطق مسجلة لهذه المدينة",
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            var count = Math.Min(3, areas.Count);
            for (int i = 0; i < count; i++)
            {
                var a = areas[i];
                var chip = Controls.ChipButton(a.Name, () =>
                {
                    _toField.Text = a.Name;
                    _toLat = a.Lat;
                    _toLng = a.Lng;
                    _toCoords.Text = Coords(a.Lat, a.Lng);
                    UpdateEstimate();
                });
                _areaRow.AddArrangedSubview(chip);
            }

            // «الكل» عند وجود أكثر من 3
            if (areas.Count > 3)
            {
                var more = Controls.ChipButton("الكل ▾", () =>
                {
                    var names = new List<string>();
                    foreach (var a in areas) names.Add(a.Name);
                    Ui.Choose("كل المناطق", names, index =>
                    {
                        var a = areas[index];
                        _toField.Text = a.Name;
                        _toLat = a.Lat;
                        _toLng = a.Lng;
                        _toCoords.Text = Coords(a.Lat, a.Lng);
                        UpdateEstimate();
                    });
                });
                _areaRow.AddArrangedSubview(more);
            }
        }

        private void RenderStreets(List<Street> streets)
        {
            _streets.Clear();
            if (streets != null) _streets.AddRange(streets);

            _showAllStreets = false;
            if (_streetsTitle != null)
                _streetsTitle.Text = string.IsNullOrEmpty(_city)
                    ? "أهم شوارع المدينة:"
                    : "أهم شوارع " + _city + ":";

            BuildStreetGrid(_streetSearch != null ? _streetSearch.Text : null);
        }

        /// <summary>اختيار شارع كوجهة (مشترك بين الشبكة والبحث).</summary>
        private void ApplyStreet(Street s)
        {
            _toField.Text = s.Name;
            _toLat = s.Lat;
            _toLng = s.Lng;
            _toCoords.Text = Coords(s.Lat, s.Lng);
            UpdateEstimate();
        }

        /// <summary>شبكة الشوارع: 3 في السطر + فلترة فورية + «عرض كل الشوارع» (بديل BuildStreetGrid في أندرويد).</summary>
        private void BuildStreetGrid(string filter)
        {
            if (_streetGrid == null) return;
            ClearStreetGrid();

            var q = (filter ?? "").Trim();
            var list = new List<Street>();
            foreach (var s in _streets)
            {
                if (string.IsNullOrWhiteSpace(s.Name)) continue;
                if (q.Length > 0 && s.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                list.Add(s);
            }

            if (list.Count == 0)
            {
                _streetGrid.AddArrangedSubview(MakeLabel(
                    q.Length > 0 ? "لا يوجد شارع بهذا الاسم" : "لا توجد شوارع مسجلة لهذه المدينة",
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            var max = _showAllStreets ? int.MaxValue : 18;
            var shown = list.Count > max ? list.GetRange(0, max) : list;

            UIStackView row = null;
            var inRow = 0;
            foreach (var s in shown)
            {
                if (row == null)
                {
                    row = new UIStackView
                    {
                        Axis = UILayoutConstraintAxis.Horizontal,
                        Distribution = UIStackViewDistribution.FillEqually,
                        Spacing = 8,
                        TranslatesAutoresizingMaskIntoConstraints = false
                    };
                    _streetGrid.AddArrangedSubview(row);
                    inRow = 0;
                }

                var st = s;
                var chip = Controls.StreetChip(st.Name, () => ApplyStreet(st));
                chip.TitleLabel.LineBreakMode = UILineBreakMode.WordWrap;
                chip.TitleLabel.Lines = 2;
                chip.TitleLabel.TextAlignment = UITextAlignment.Center;
                chip.TitleLabel.Font = Theme.FontBold(11);
                chip.HeightAnchor.ConstraintEqualTo(46f).Active = true;
                row.AddArrangedSubview(chip);

                inRow++;
                if (inRow >= 3) row = null;
            }

            if (shown.Count < list.Count)
            {
                var more = Controls.OutlineButton("عرض كل الشوارع (" + list.Count + ")", Theme.Red, 40f);
                more.TouchUpInside += (s, e) =>
                {
                    _showAllStreets = true;
                    BuildStreetGrid(_streetSearch != null ? _streetSearch.Text : null);
                };
                _streetGrid.AddArrangedSubview(more);
            }
        }

        private void ClearStreetGrid()
        {
            if (_streetGrid == null) return;
            for (int i = _streetGrid.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _streetGrid.ArrangedSubviews[i];
                _streetGrid.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }
        }

        /* ---------- الخدمة والدفع ---------- */

        private void LoadServices()
        {
            Ui.Run(() =>
            {
                var list = Api.ServiceTypes();
                Ui.BeginOnMain(() =>
                {
                    _services = list ?? new List<ServiceType>();
                    if (_services.Count > 0)
                    {
                        // عند الإعادة نُفضّل نفس نوع الخدمة السابق
                        _service = _services[0];
                        if (_prefillServiceId > 0)
                        {
                            foreach (var s in _services)
                            {
                                if (s.Id == _prefillServiceId) { _service = s; break; }
                            }
                        }

                        _btnService.SetTitle("الخدمة: " + _service.Name, UIControlState.Normal);
                        UpdateEstimate();
                    }
                });
            }, null, ex => AppServices.Log("TawselaNewOrder", "services: " + ex.Message));
        }

        private void PickService()
        {
            if (_services == null || _services.Count == 0) { Ui.Toast("لا توجد خدمات متاحة"); return; }

            var names = new List<string>();
            foreach (var s in _services) names.Add(s.Name);

            Ui.Choose("اختار نوع التوصيلة", names, index =>
            {
                _service = _services[index];
                _btnService.SetTitle(_service.Name, UIControlState.Normal);
                UpdateEstimate();
            });
        }

        /// <summary>اختيار الدفع مع رصيد المحفظة (بديل ChoosePaymentActivity).</summary>
        private void PickPayment()
        {
            Ui.Run(() =>
            {
                decimal balance = 0m;
                try { balance = Api.WalletBalance(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    var options = new List<string> { "نقدي" };
                    if (ReleaseConfig.WalletPaymentsEnabled)
                        options.Add("محفظة (" + balance.ToString("0.00") + " ج.م)");

                    Ui.Choose("طريقة الدفع", options, index =>
                    {
                        _payment = index == 0 ? "نقدي" : "محفظة";
                        _btnPayment.SetTitle(index == 0
                            ? "نقدي"
                            : "محفظة — رصيد " + balance.ToString("0.00") + " ج.م", UIControlState.Normal);

                        if (index == 1 && balance <= 0)
                            Ui.Toast("رصيد المحفظة صفر — اشحن المحفظة أولًا أو ادفع نقدًا");
                    });
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        /* ---------- كود الخصم ---------- */

        private void ApplyCoupon()
        {
            var code = (_couponField.Text ?? "").Trim();

            if (code == "")
            {
                _coupon = null;
                _discount = 0;
                _couponInfo.Text = "";
                UpdateEstimate();
                return;
            }

            var progress = Ui.ShowProgress("جارٍ التحقق من الكود…");
            Ui.Run(() =>
            {
                var coupon = Api.ValidateCoupon(code);
                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    if (coupon == null)
                    {
                        _coupon = null;
                        _discount = 0;
                        _couponInfo.Text = "";
                        Ui.Toast("كود غير صالح");
                        UpdateEstimate();
                        return;
                    }

                    _coupon = coupon;
                    var gross = CurrentEstimate();
                    _discount = coupon.ComputeDiscount(gross);
                    _couponInfo.Text = "خصم " + _discount.ToString("0.00") + " ج.م (" + coupon.Code + ")";
                    UpdateEstimate();
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        /* ---------- الجدولة ---------- */

        private void PickScheduleTime()
        {
            var initial = _scheduledAt == default(DateTime) ? DateTime.Now.AddHours(1) : _scheduledAt;
            Push(new ScheduleViewController(initial, picked =>
            {
                _scheduledAt = picked;
                _scheduleInfo.Text = "موعد الطلب: " + picked.ToString("dd/MM/yyyy HH:mm");
                UpdateEstimate();
            }));
        }

        /* ---------- السائقون المتاحون ---------- */

        private void RefreshDriversCount()
        {
            var seq = ++_driversSeq;
            var lat = _fromLat;
            var lng = _fromLng;

            Ui.Run(() =>
            {
                // بدون إحداثيات تُخفى القائمة بالكامل (نفس أندرويد)
                if (lat == 0 && lng == 0)
                {
                    Ui.BeginOnMain(() =>
                    {
                        if (seq != _driversSeq) return;
                        if (_nearbyLabel != null) _nearbyLabel.Hidden = true;
                        if (_nearbyBox != null) _nearbyBox.Hidden = true;
                    });
                    return;
                }

                List<Driver> drivers;
                try
                {
                    drivers = Api.AvailableDrivers(lat, lng, _governorate, _city) ?? new List<Driver>();
                }
                catch { drivers = null; }

                if (seq != _driversSeq) return;
                Ui.BeginOnMain(() =>
                {
                    if (seq != _driversSeq) return;
                    if (drivers == null) { ShowNearby(null, "تعذر تحميل السائقين حاليًا"); return; }
                    if (drivers.Count == 0) { ShowNearby(null, "لا يوجد سائقون متاحون قريبون حاليًا"); return; }
                    ShowNearby(drivers, null);
                });
            }, null, ex => AppServices.Log("TawselaNewOrder", "drivers: " + ex.Message));
        }

        /// <summary>أول 3 سائقين قريبين تحت خانة الانطلاق (بديل BuildNearbyRow في أندرويد).</summary>
        private void ShowNearby(List<Driver> drivers, string hint)
        {
            if (_nearbyBox == null) return;

            while (_nearbyBox.ArrangedSubviews.Length > 0)
            {
                var v = _nearbyBox.ArrangedSubviews[0];
                _nearbyBox.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (_nearbyLabel != null) _nearbyLabel.Hidden = false;
            _nearbyBox.Hidden = false;

            if (hint != null)
            {
                _nearbyBox.AddArrangedSubview(MakeLabel(hint, 12f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            var shown = 0;
            foreach (var d in drivers)
            {
                if (shown >= 3) break;

                var km = Geo.DistanceKm(_fromLat, _fromLng, d.Lat, d.Lng);
                var name = string.IsNullOrWhiteSpace(d.FullName) ? "سائق" : d.FullName;

                // نص السيارة: الموديل + اللون + اللوحة (نفس CarText في أندرويد)
                var car = "";
                if (!string.IsNullOrWhiteSpace(d.CarModel)) car = d.CarModel;
                if (!string.IsNullOrWhiteSpace(d.CarColor)) car += (car == "" ? "" : " · ") + d.CarColor;
                if (!string.IsNullOrWhiteSpace(d.CarPlate)) car += (car == "" ? "" : " · ") + "لوحة " + d.CarPlate;

                var text = name + (car == "" ? "" : " · " + car) + " · " +
                           Geo.KmText(km) + " · " + Geo.EtaMinutes(km) + " دقيقة";

                _nearbyBox.AddArrangedSubview(MakeLabel(text, 12f, Theme.TextPrimary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                shown++;
            }
        }

        /* ---------- التقدير ---------- */

        private double DistanceKm()
        {
            if (_fromLat == 0 && _fromLng == 0) return 0;
            if (_toLat == 0 && _toLng == 0) return 0;
            return Geo.DistanceKm(_fromLat, _fromLng, _toLat, _toLng);
        }

        /// <summary>
        /// آخر 5 وجهات من طلبات العميل السابقة كأزرار سريعة
        /// (بديل اقتراحات Api.GetOrders في NewOrderActivity).
        /// </summary>
        private void LoadRecentDestinations()
        {
            if (!Session.IsCustomer || _recentRow == null) return;

            Ui.Run(() =>
            {
                var orders = Api.GetOrders("", "customer");

                Ui.BeginOnMain(() =>
                {
                    ClearChips(_recentRow);

                    var seen = new HashSet<string>();

                    foreach (var o in orders)
                    {
                        if (seen.Count >= 5) break;
                        if (string.IsNullOrWhiteSpace(o.ToAddress)) continue;
                        if (!seen.Add(o.ToAddress)) continue;

                        var address = o.ToAddress;
                        var lat = o.ToLat;
                        var lng = o.ToLng;

                        var chip = Controls.ChipButton("↺ " + ShortAddress(address), () =>
                        {
                            _toField.Text = address;
                            _toLat = lat;
                            _toLng = lng;
                            _toCoords.Text = Coords(lat, lng);
                            UpdateEstimate();
                        });
                        _recentRow.AddArrangedSubview(chip);
                    }

                    var empty = _recentRow.ArrangedSubviews.Length == 0;
                    if (_recentScroll != null) _recentScroll.Hidden = empty;
                    if (_recentTitle != null) _recentTitle.Hidden = empty;
                });
            }, null, ex => AppServices.Log("TawselaNewOrder", "recent: " + ex.Message));
        }

        /// <summary>اختصار عنوان طويل للعرض في الشريحة: أول جزئين وبحد أقصى 40 حرفًا (نفس ShortAddr).</summary>
        private static string ShortAddress(string full)
        {
            var parts = (full ?? "").Split(new[] { "،", "," }, StringSplitOptions.RemoveEmptyEntries);
            var text = parts.Length > 0 ? parts[0].Trim() : (full ?? "");
            if (parts.Length > 1) text += "، " + parts[1].Trim();
            if (text.Length > 40) text = text.Substring(0, 40) + "…";
            return text;
        }

        /// <summary>
        /// خصم الولاء حسب مستوى العميل (بديل LoadTier + ComputeTierAmount في أندرويد).
        /// </summary>
        private void LoadTier()
        {
            if (!ReleaseConfig.LoyaltyEnabled || !Session.IsCustomer)
            {
                _tierPercent = 0;
                _tierAmount = 0;
                if (_loyaltyInfo != null) _loyaltyInfo.Hidden = true;
                return;
            }

            Ui.Run(() =>
            {
                var points = Api.LoyaltyPoints();
                string name;
                int percent, next;
                Loyalty.Tier(points, out name, out percent, out next);

                Ui.BeginOnMain(() =>
                {
                    _tierPercent = percent;

                    _loyaltyInfo.Text = percent > 0
                        ? "خصم الولاء (" + name + "): " + percent + "%"
                        : "العضوية: " + name + " — أكمل رحلاتك للحصول على خصم";

                    _loyaltyInfo.Hidden = false;
                    UpdateEstimate();
                });
            }, null, ex => AppServices.Log("TawselaNewOrder", "loyalty: " + ex.Message));
        }

        /// <summary>قيمة خصم الولاء من الأجرة الإجمالية.</summary>
        private decimal ComputeTierAmount(decimal gross)
        {
            if (!ReleaseConfig.LoyaltyEnabled || _tierPercent <= 0) return 0m;
            return Math.Round(gross * _tierPercent / 100m, 0);
        }

        /// <summary>الأجرة قبل الخصم (مع أسعار الليل حسب وقت الطلب/الجدولة).</summary>
        private decimal CurrentEstimate()
        {
            if (_service == null) return 0m;

            var km = DistanceKm();
            if (km <= 0) return 0m;

            int nightFrom, nightTo;
            Api.NightWindow(out nightFrom, out nightTo);

            var at = _scheduledAt == default(DateTime) ? DateTime.Now : _scheduledAt;
            return Fare.Estimate(_service, km, at, nightFrom, nightTo);
        }

        private void UpdateEstimate()
        {
            var km = DistanceKm();
            if (km <= 0)
            {
                _distanceLabel.Text = "حدد الانطلاق والوجهة لحساب المسافة";
                _fareLabel.Text = "—";
                _tripFareLabel.Text = "—";
                _fareDetailsLabel.Text = "";
                _loyaltyInfo.Hidden = true;
                return;
            }

            _distanceLabel.Text = "المسافة التقديرية: " + Geo.KmText(km) +
                                  " · الزمن: " + Geo.EtaMinutes(km) + " دقيقة";

            if (_service == null)
            {
                _fareLabel.Text = "—";
                _tripFareLabel.Text = "—";
                _fareDetailsLabel.Text = "";
                return;
            }

            int nightFrom, nightTo;
            Api.NightWindow(out nightFrom, out nightTo);

            var at = _scheduledAt == default(DateTime) ? DateTime.Now : _scheduledAt;
            var fare = Fare.Estimate(_service, km, at, nightFrom, nightTo);
            var night = Fare.IsNightAt(at, nightFrom, nightTo) ||
                        _service.PricingMode == "ليل" ||
                        (_service.PricingMode != "نهار" && Fare.IsNightEffective(_service, nightFrom, nightTo));

            // خصم الولاء يُحسب من الأجرة الإجمالية (بديل ComputeTierAmount)
            _tierAmount = ComputeTierAmount(fare);

            var net = fare - _discount - _tierAmount;
            if (net < 0) net = 0;

            var text = "التكلفة التقديرية: " + fare.ToString("0.00") + " ج.م" +
                       (_service == null ? "" : " (" + _service.Name + (night ? " · ليل 🌙" : "") + ")");

            if (_discount > 0) text += "\nالخصم: " + _discount.ToString("0.00") + " ج.م";
            if (_tierAmount > 0)
                text += "\nخصم الولاء (" + _tierPercent + "%): " + _tierAmount.ToString("0.00") + " ج.م";
            if (net < fare) text += "\nالصافي: " + net.ToString("0.00") + " ج.م";

            _fareLabel.Text = text;

            // بطاقة «تكلفة الرحلة» أسفل الشاشة (بديل cardFare في أندرويد)
            _tripFareLabel.Text = net.ToString("0.00") + " ج.م";

            var details = _service.Name + " · " + Geo.KmText(km) + (night ? " · ليل 🌙" : "");
            if (_discount > 0) details += "\nخصم الكوبون: " + _discount.ToString("0.00") + " ج.م";
            if (_tierAmount > 0)
                details += "\nخصم الولاء (" + _tierPercent + "%): " + _tierAmount.ToString("0.00") + " ج.م";
            _fareDetailsLabel.Text = details;

            _loyaltyInfo.Hidden = _tierAmount <= 0;
            if (_tierAmount > 0)
                _loyaltyInfo.Text = "خصم الولاء (" + _tierPercent + "%): " + _tierAmount.ToString("0.00") + " ج.م";
        }

        /* ---------- الإنشاء ---------- */

        private void CreateOrder()
        {
            if (!Net.IsOnline()) { Ui.Toast("لا يوجد اتصال بالإنترنت"); return; }

            var fromAddr = (_fromField.Text ?? "").Trim();
            var toAddr = (_toField.Text ?? "").Trim();

            if (fromAddr == "") { Ui.Toast("حدد نقطة الانطلاق"); return; }
            if (toAddr == "") { Ui.Toast("حدد الوجهة"); return; }
            if (_fromLat == 0 && _fromLng == 0) { Ui.Toast("حدد موقع الانطلاق على الخريطة أولًا"); return; }
            if (_toLat == 0 && _toLng == 0) { Ui.Toast("اختر منطقة أو شارعًا محددًا للوجهة"); return; }
            if (_service == null) { Ui.Toast("اختر نوع الخدمة"); return; }

            var bidding = _biddingSwitch != null && _biddingSwitch.On;
            var offered = ParseDecimal(_offeredField);
            if (bidding && offered <= 0) { Ui.Toast("أدخل السعر الذي تعرضه للمزايدة"); return; }

            var tip = ParseDecimal(_tipField);
            var maxWait = 30;
            int parsedWait;
            if (int.TryParse((_maxWaitField.Text ?? "").Trim(), out parsedWait) && parsedWait > 0)
                maxWait = parsedWait;

            var km = DistanceKm();
            int nightFrom, nightTo;
            Api.NightWindow(out nightFrom, out nightTo);
            var at = _scheduledAt == default(DateTime) ? DateTime.Now : _scheduledAt;
            var fare = Fare.Estimate(_service, km, at, nightFrom, nightTo);

            // نفس أندرويد: الخصم المُرسَل = خصم الكوبون + خصم الولاء
            _tierAmount = ComputeTierAmount(fare);

            var note = (_noteField.Text ?? "").Trim();

            // محطة وسيطة (بديل txtStopAddress / btnPickStop)
            var stopAddr = (_stopField == null ? "" : (_stopField.Text ?? "").Trim());

            var couponCode = _coupon != null ? _coupon.Code : "";
            var discount = _discount + _tierAmount;
            var floor = (_floorField.Text ?? "").Trim();
            var splitPhone = (_splitPhoneField.Text ?? "").Trim();

            var serviceId = _service.Id;
            var payment = _payment;
            var scheduled = _scheduledAt;

            _btnCreate.Enabled = false;
            var progress = Ui.ShowProgress("جارٍ إنشاء الطلب…");

            Ui.Run(
                () =>
                {
                    var orderId = Api.CreateOrder(
                        _fromLat, _fromLng, fromAddr,
                        _toLat, _toLng, toAddr,
                        note, fare, serviceId, payment,
                        scheduled, couponCode, discount, tip,
                        _stopLat, _stopLng, stopAddr,
                        offered, bidding, floor, maxWait);

                    // مشاركة الأجرة: يُسجَّل رقم الطرف الثاني بعد إنشاء الطلب
                    if (orderId > 0 && splitPhone != "")
                    {
                        try { Api.SetSplitPhone(orderId, splitPhone); } catch { }
                    }
                },
                () =>
                {
                    progress.Dismiss();
                    _btnCreate.Enabled = true;
                    Ui.Toast(bidding
                        ? "تم إنشاء الطلب — سيتلقى السائقون عروضهم"
                        : "تم إنشاء الطلب، جارٍ البحث عن سائق");
                    Router.GoHome();
                },
                ex =>
                {
                    progress.Dismiss();
                    _btnCreate.Enabled = true;
                    Ui.Toast(ex.Message);
                });
        }

        private static decimal ParseDecimal(UITextField field)
        {
            if (field == null) return 0m;

            var text = (field.Text ?? "").Trim();
            if (text == "") return 0m;

            decimal value;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value) && value > 0)
                return value;
            return 0m;
        }
    }

    /// <summary>
    /// اختيار موعد الطلب (بديل DatePickerDialog + TimePickerDialog في أندرويد).
    /// </summary>
    public class ScheduleViewController : BaseViewController
    {
        private readonly Action<DateTime> _onPicked;
        private UIDatePicker _picker;

        public ScheduleViewController(DateTime initial, Action<DateTime> onPicked) : base("موعد الطلب")
        {
            _onPicked = onPicked;
            _picker = new UIDatePicker
            {
                Mode = UIDatePickerMode.DateAndTime,
                PreferredDatePickerStyle = UIDatePickerStyle.Wheels,
                MinimumDate = (NSDate)DateTime.Now,
                Date = (NSDate)initial,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var stack = CreateScrollContent(12f, 16f);
            stack.AddArrangedSubview(_picker);

            var confirm = Controls.PrimaryButton("تأكيد الموعد");
            confirm.TouchUpInside += (s, e) =>
            {
                var picked = (DateTime)_picker.Date;
                var callback = _onPicked;

                if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                    NavigationController.PopViewController(true);

                if (callback != null) callback(picked);
            };
            stack.AddArrangedSubview(confirm);
        }
    }
}
