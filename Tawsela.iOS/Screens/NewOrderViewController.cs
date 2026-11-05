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
        private UITextField _fromField, _toField, _stopField, _noteField;
        private UITextField _couponField, _tipField, _splitPhoneField, _maxWaitField;
        private UITextField _offeredField, _floorField;
        private UILabel _fromCoords, _toCoords, _stopCoords;
        private UILabel _fareLabel, _distanceLabel, _couponInfo, _scheduleInfo, _driversInfo;
        private UILabel _govValue, _cityValue;
        private UIButton _btnService, _btnPayment, _btnCreate, _btnPickTime;
        private UISwitch _scheduledSwitch, _biddingSwitch;
        private UIStackView _areaRow, _streetRow, _savedRow, _biddingBox;

        private double _fromLat, _fromLng;
        private double _toLat, _toLng;
        private double _stopLat, _stopLng;

        private string _governorate = "";
        private string _city = "";
        private List<ServiceType> _services = new List<ServiceType>();
        private ServiceType _service;
        private string _payment = "نقدي";
        private decimal _discount;
        private int _tierPercent;
        private decimal _tierAmount;
        private UILabel _loyaltyInfo;
        private Coupon _coupon;
        private DateTime _scheduledAt = default(DateTime);

        private int _areaSeq, _streetSeq, _driversSeq;
        private UIStackView _recentRow;
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

            /* ---------- الانطلاق ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("نقطة الانطلاق"));

            _fromField = Controls.TextField("اكتب عنوان الانطلاق");
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

            var useGps = Controls.OutlineButton("استخدم موقعي الحالي");
            useGps.TouchUpInside += (s, e) => RequestLocation(true);
            stack.AddArrangedSubview(useGps);

            var pickFrom = Controls.IconButton("اختيار الانطلاق من الخريطة", "ic_pin", Theme.Primary);
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

            // أماكن محفوظة (بديل Spinner الأماكن المحفوظة)
            _savedRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_savedRow);

            var savePlace = Controls.IconButton("حفظ الانطلاق كمكان مفضّل", "ic_save", Theme.Primary);
            savePlace.TouchUpInside += (s, e) => SaveCurrentPlace();
            stack.AddArrangedSubview(savePlace);

            /* ---------- الوجهة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("الوجهة"));

            _toField = Controls.TextField("اكتب عنوان الوجهة");
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

            // وجهات سابقة (بديل اقتراحات Api.GetOrders في أندرويد)
            _recentRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 6,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _recentRow.Hidden = true;
            stack.AddArrangedSubview(_recentRow);

            /* ---------- نقطة إضافية ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("نقطة إضافية (اختياري)"));

            _stopField = Controls.TextField("عنوان التوقف في الطريق");

            var stopMic = Controls.SquareIconButton("ic_mic", Theme.Primary, () => Voice(text => { _stopField.Text = text; }));
            stack.AddArrangedSubview(Controls.FieldWithAction(_stopField, stopMic));

            _stopCoords = MakeLabel("", 11f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_stopCoords);

            var pickStop = Controls.IconButton("تحديد المحطة على الخريطة", "ic_pin", Theme.Primary);
            pickStop.TouchUpInside += (s, e) =>
            {
                Push(MapViewController.Pick(_stopLat, _stopLng, (plat, plng, address) =>
                {
                    _stopLat = plat; _stopLng = plng;
                    _stopCoords.Text = Coords(plat, plng);
                    if (!string.IsNullOrWhiteSpace(address)) _stopField.Text = address;
                }));
            };
            stack.AddArrangedSubview(pickStop);

            /* ---------- المحافظة / المدينة ---------- */
            var govCard = MakeSelectorCard("المحافظة", out _govValue, () => PickGovernorate());
            stack.AddArrangedSubview(govCard);

            var cityCard = MakeSelectorCard("المدينة", out _cityValue, () => PickCity());
            stack.AddArrangedSubview(cityCard);

            /* ---------- المناطق والشوارع ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("المناطق"));
            _areaRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_areaRow);

            stack.AddArrangedSubview(Controls.SectionTitle("أهم الشوارع"));
            _streetRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_streetRow);

            /* ---------- الخدمة والدفع ---------- */
            _btnService = Controls.IconButton("اختر نوع الخدمة", "ic_bell", Theme.Primary);
            _btnService.TouchUpInside += (s, e) => PickService();
            stack.AddArrangedSubview(_btnService);

            _btnPayment = Controls.IconButton("الدفع: نقدي", "ic_money", Theme.Primary);
            _btnPayment.TouchUpInside += (s, e) => PickPayment();
            stack.AddArrangedSubview(_btnPayment);

            /* ---------- كود الخصم ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("كود الخصم"));

            _couponField = Controls.TextField("أدخل كود الخصم");
            _couponField.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;

            var applyCoupon = Controls.PrimaryButton("تطبيق", 48f);
            applyCoupon.WidthAnchor.ConstraintEqualTo(96f).Active = true;
            applyCoupon.TouchUpInside += (s, e) => ApplyCoupon();
            stack.AddArrangedSubview(Controls.FieldWithAction(_couponField, applyCoupon));

            _couponInfo = MakeLabel("", 12f, Theme.Green, UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_couponInfo);

            /* ---------- الجدولة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("جدولة الطلب"));

            _scheduledSwitch = new UISwitch { OnTintColor = Theme.Primary, TranslatesAutoresizingMaskIntoConstraints = false };
            _scheduledSwitch.ValueChanged += (s, e) =>
            {
                _btnPickTime.Enabled = _scheduledSwitch.On;
                if (!_scheduledSwitch.On)
                {
                    _scheduledAt = default(DateTime);
                    _scheduleInfo.Text = "الطلب الآن";
                    UpdateEstimate();
                }
            };
            stack.AddArrangedSubview(Row("طلب مجدول لاحقًا", _scheduledSwitch));

            _btnPickTime = Controls.IconButton("اختيار موعد التنفيذ", "ic_clock", Theme.Primary);
            _btnPickTime.Enabled = false;
            _btnPickTime.TouchUpInside += (s, e) => PickScheduleTime();
            stack.AddArrangedSubview(_btnPickTime);

            _scheduleInfo = MakeLabel("الطلب الآن", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_scheduleInfo);

            /* ---------- المزايدة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("المزايدة على السعر"));

            _biddingSwitch = new UISwitch { OnTintColor = Theme.Primary, TranslatesAutoresizingMaskIntoConstraints = false };
            _biddingSwitch.ValueChanged += (s, e) =>
            {
                _biddingBox.Hidden = !_biddingSwitch.On;
            };
            stack.AddArrangedSubview(Row("اطلب بمزايدة (السائقون يقدّمون عروضهم)", _biddingSwitch));

            _biddingBox = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            _offeredField = Controls.TextField("السعر الذي تعرضه (ج.م)");
            _offeredField.KeyboardType = UIKeyboardType.DecimalPad;
            _floorField = Controls.TextField("أقل سعر تقبله (اختياري)");
            _floorField.KeyboardType = UIKeyboardType.DecimalPad;
            _biddingBox.AddArrangedSubview(_offeredField);
            _biddingBox.AddArrangedSubview(_floorField);
            stack.AddArrangedSubview(_biddingBox);

            /* ---------- الإكرامية والملاحظة ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("تفاصيل إضافية"));

            _tipField = Controls.TextField("إكرامية للسائق (اختياري)");
            _tipField.KeyboardType = UIKeyboardType.DecimalPad;
            stack.AddArrangedSubview(_tipField);

            _noteField = Controls.TextField("ملاحظة للسائق (اختياري)");
            stack.AddArrangedSubview(_noteField);

            _splitPhoneField = Controls.TextField("رقم مشاركة الأجرة (اختياري)");
            _splitPhoneField.KeyboardType = UIKeyboardType.PhonePad;
            stack.AddArrangedSubview(_splitPhoneField);

            _maxWaitField = Controls.TextField("أقصى مدة انتظار للسائق (دقيقة)");
            _maxWaitField.KeyboardType = UIKeyboardType.NumberPad;
            _maxWaitField.Text = "30";
            stack.AddArrangedSubview(_maxWaitField);

            /* ---------- السائقون المتاحون ---------- */
            _driversInfo = MakeLabel("جارٍ البحث عن سائقين قريبين…", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_driversInfo);

            /* ---------- التقدير ---------- */
            var estimateCard = Controls.Card();
            _distanceLabel = MakeLabel("—", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _fareLabel = MakeLabel("—", 20f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);

            // سطر خصم الولاء حسب المستوى (بديل txtLoyaltyInfo)
            _loyaltyInfo = MakeLabel("", 12f, Theme.Green, UIFontWeight.Medium, UITextAlignment.Right);
            _loyaltyInfo.Hidden = true;

            estimateCard.AddSubview(_distanceLabel);
            estimateCard.AddSubview(_fareLabel);
            estimateCard.AddSubview(_loyaltyInfo);
            estimateCard.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_distanceLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(_distanceLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_distanceLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_fareLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _distanceLabel, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_fareLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_fareLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_loyaltyInfo, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _fareLabel, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_loyaltyInfo, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_loyaltyInfo, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_loyaltyInfo, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, estimateCard, NSLayoutAttribute.Bottom, 1, -12),
            });
            stack.AddArrangedSubview(estimateCard);

            _btnCreate = Controls.PrimaryButton("اطلب التوصيلة");
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
                ClearChips(_streetRow);
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
            ClearChips(_streetRow);
            if (streets == null || streets.Count == 0)
            {
                _streetRow.AddArrangedSubview(MakeLabel("لا توجد شوارع مسجلة لهذه المدينة",
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            var count = Math.Min(3, streets.Count);
            for (int i = 0; i < count; i++)
            {
                var s = streets[i];
                var chip = Controls.StreetChip(s.Name, () =>
                {
                    _toField.Text = s.Name;
                    _toLat = s.Lat;
                    _toLng = s.Lng;
                    _toCoords.Text = Coords(s.Lat, s.Lng);
                    UpdateEstimate();
                });
                _streetRow.AddArrangedSubview(chip);
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
                        _toField.Text = s.Name;
                        _toLat = s.Lat;
                        _toLng = s.Lng;
                        _toCoords.Text = Coords(s.Lat, s.Lng);
                        UpdateEstimate();
                    });
                });
                _streetRow.AddArrangedSubview(more);
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

            Ui.Choose("نوع الخدمة", names, index =>
            {
                _service = _services[index];
                _btnService.SetTitle("الخدمة: " + _service.Name, UIControlState.Normal);
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
                            ? "الدفع: نقدي"
                            : "الدفع: محفظة — رصيد " + balance.ToString("0.00") + " ج.م", UIControlState.Normal);

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
                // بدون إحداثيات لا يمكن الاستعلام عن القريبين (بديل AvailableDrivers في أندرويد)
                if (lat == 0 && lng == 0)
                {
                    Ui.BeginOnMain(() =>
                    {
                        if (seq != _driversSeq) return;
                        _driversInfo.Text = "حدد نقطة الانطلاق لعرض السائقين القريبين";
                        _driversInfo.TextColor = Theme.TextSecondary;
                    });
                    return;
                }

                int count;
                try
                {
                    count = (Api.AvailableDrivers(lat, lng, _governorate, _city) ?? new List<Driver>()).Count;
                }
                catch { count = 0; }

                if (seq != _driversSeq) return;
                Ui.BeginOnMain(() =>
                {
                    _driversInfo.Text = count > 0
                        ? "سائقون متاحون الآن: " + count
                        : "لا يوجد سائقون متاحون حاليًا — يمكنك الطلب وسيُبلَّغ السائقون";
                    _driversInfo.TextColor = count > 0 ? Theme.Green : Theme.TextSecondary;
                });
            }, null, ex => AppServices.Log("TawselaNewOrder", "drivers: " + ex.Message));
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
                    while (_recentRow.ArrangedSubviews.Length > 0)
                    {
                        var v = _recentRow.ArrangedSubviews[0];
                        _recentRow.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    var seen = new HashSet<string>();

                    foreach (var o in orders)
                    {
                        if (seen.Count >= 5) break;
                        if (string.IsNullOrWhiteSpace(o.ToAddress)) continue;
                        if (!seen.Add(o.ToAddress)) continue;

                        var address = o.ToAddress;
                        var lat = o.ToLat;
                        var lng = o.ToLng;

                        var chip = Controls.OutlineButton("↺ " + address, null, 38f);
                        chip.ContentEdgeInsets = new UIEdgeInsets(0, 12, 0, 12);
                        chip.TouchUpInside += (s, e) =>
                        {
                            _toField.Text = address;
                            _toLat = lat;
                            _toLng = lng;
                            _toCoords.Text = Coords(lat, lng);
                            UpdateEstimate();
                        };
                        _recentRow.AddArrangedSubview(chip);
                    }

                    _recentRow.Hidden = _recentRow.ArrangedSubviews.Length == 0;
                });
            }, null, ex => AppServices.Log("TawselaNewOrder", "recent: " + ex.Message));
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
                return;
            }

            _distanceLabel.Text = "المسافة التقديرية: " + Geo.KmText(km) +
                                  " · الزمن: " + Geo.EtaMinutes(km) + " دقيقة";

            if (_service == null)
            {
                _fareLabel.Text = "—";
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
        }

        /* ---------- الإنشاء ---------- */

        private void CreateOrder()
        {
            if (!Net.IsOnline()) { Ui.Toast("لا يوجد اتصال بالإنترنت"); return; }

            var fromAddr = (_fromField.Text ?? "").Trim();
            var toAddr = (_toField.Text ?? "").Trim();

            if (fromAddr == "") { Ui.Toast("حدد نقطة الانطلاق"); return; }
            if (toAddr == "") { Ui.Toast("حدد الوجهة"); return; }
            if (_fromLat == 0 && _fromLng == 0) { Ui.Toast("اضغط «استخدم موقعي الحالي» أولًا"); return; }
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
            var stopAddr = (_stopField.Text ?? "").Trim();
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
