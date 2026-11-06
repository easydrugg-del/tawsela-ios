using System;
using System.Globalization;
using CoreGraphics;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/TrackOrderActivity.cs — نفس شكل أندرويد بالضبط:
    /// هيدر برتقالي، بطاقة الحالة + رمز الرحلة، خريطة، شريط تتبع عمودي،
    /// تفاصيل الطلب، أزرار التواصل والطوارئ.
    /// </summary>
    public class TrackOrderViewController : BaseViewController
    {
        private static readonly string[] Stages =
        {
            "طلب جديد", "قبول السائق", "السائق بالطريق",
            "وصل السائق", "أثناء الرحلة", "مكتمل"
        };

        private static readonly string[] StageDesc =
        {
            "تم إنشاء الطلب بنجاح", "السائق يقبل الرحلة", "السائق في طريقه إليك",
            "السائق وصل لنقطة الانطلاق", "أنت الآن في الطريق", "وصلت إلى وجهتك بنجاح"
        };

        private readonly int _orderId;
        private readonly Order _preview;
        private Order _order;
        private NSTimer _timer;
        private bool _rated;
        private bool _ratingPrompted;
        private int _longRideChecks;

        private UILabel _topSub;
        private UILabel _status;
        private UILabel _detail;
        private UILabel _pin;
        private UIView _arrivalRow;
        private UIButton _confirmArrival;
        private UIButton _notArrived;

        private UILabel _mapStatus;
        private UIButton _mapAction;

        private UIStackView _steps;
        private UILabel _info;
        private UILabel _eta;
        private UILabel _contact;
        private UIView _contactRow;
        private UIButton _offers;
        private UIButton _place;
        private UIButton _pay;

        private bool _busy;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public TrackOrderViewController(int orderId) : base()
        {
            _orderId = orderId;
        }

        /// <summary>للالتقاط/المعاينة فقط: عرض طلب جاهز بدون شبكة.</summary>
        public TrackOrderViewController(Order preview) : base()
        {
            _preview = preview;
            _orderId = preview != null ? preview.Id : 0;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.Surface;
            BuildUi();

            if (_preview != null) Render(_preview, "", 0);
            else Load();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            _timer = NSTimer.CreateRepeatingScheduledTimer(5.0, _ => Load());
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (_timer != null) { _timer.Invalidate(); _timer.Dispose(); _timer = null; }
        }

        /* ---------- البناء ---------- */

        private void BuildUi()
        {
            // الهيدر البرتقالي (بديل bg_header + txtOrderIdTop)
            var header = Shapes.Header(120f);
            header.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(header);

            var title = MakeLabel("تتبع الطلب", 20f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Right);
            _topSub = MakeLabel("طلب رقم " + _orderId, 13f, Theme.Hex(0xB0B5CC),
                UIFontWeight.Regular, UITextAlignment.Right);
            header.AddSubview(title);
            header.AddSubview(_topSub);

            var scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AlwaysBounceVertical = true
            };
            View.AddSubview(scroll);

            var content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            scroll.AddSubview(content);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 120),

                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, header, NSLayoutAttribute.Trailing, 1, -16),

                NSLayoutConstraint.Create(_topSub, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(_topSub, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, title, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_topSub, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, title, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(content, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, 0),
            });

            // ===== بطاقة الحالة =====
            _status = MakeLabel("جاري البحث عن سائق...", 20f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _status.Lines = 0;
            _detail = MakeLabel("", 14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            _detail.Lines = 0;

            _pin = MakeLabel("", 20f, Theme.Accent, UIFontWeight.Bold, UITextAlignment.Center);
            _pin.Lines = 0;
            _pin.BackgroundColor = Theme.White;
            _pin.Layer.CornerRadius = 12f;
            _pin.Layer.BorderWidth = 1f;
            _pin.Layer.BorderColor = Theme.CardStroke.CGColor;
            _pin.Layer.MasksToBounds = true;
            _pin.Hidden = true;

            _confirmArrival = SolidButton("السائق وصل ✅", Theme.Green, 48f, 14f);
            _notArrived = SolidButton("لم يصل ⚠️", Theme.Red, 48f, 14f);
            _confirmArrival.TouchUpInside += (s, e) => ConfirmArrival();
            _notArrived.TouchUpInside += (s, e) => NotArrived();

            _arrivalRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 6,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            ((UIStackView)_arrivalRow).AddArrangedSubview(_confirmArrival);
            ((UIStackView)_arrivalRow).AddArrangedSubview(_notArrived);

            var statusCard = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 6,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            statusCard.AddArrangedSubview(_status);
            statusCard.AddArrangedSubview(_detail);
            statusCard.AddArrangedSubview(_pin);
            statusCard.AddArrangedSubview(_arrivalRow);
            statusCard.SetCustomSpacing(10f, _detail);

            var statusBox = new UIView { BackgroundColor = Theme.White, TranslatesAutoresizingMaskIntoConstraints = false };
            statusBox.AddSubview(statusCard);
            statusBox.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(statusCard, NSLayoutAttribute.Top, NSLayoutRelation.Equal, statusBox, NSLayoutAttribute.Top, 1, 16),
                NSLayoutConstraint.Create(statusCard, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, statusBox, NSLayoutAttribute.Bottom, 1, -16),
                NSLayoutConstraint.Create(statusCard, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, statusBox, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(statusCard, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, statusBox, NSLayoutAttribute.Trailing, 1, -16),
            });
            content.AddArrangedSubview(Pad(statusBox, 12, 12, 12, 0));

            // ===== منطقة الخريطة =====
            _mapStatus = MakeLabel("جاري تحميل الخريطة...", 13f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Center);
            _mapStatus.Lines = 0;

            var spinner = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Medium)
            {
                Color = Theme.Primary,
                HidesWhenStopped = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            spinner.StopAnimating();

            _mapAction = SolidButton("إعادة المحاولة", Theme.Green, 42f, 13f);
            _mapAction.TouchUpInside += (s, e) => OnMapAction();

            var mapInner = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                Alignment = UIStackViewAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            mapInner.AddArrangedSubview(spinner);
            mapInner.AddArrangedSubview(_mapStatus);
            mapInner.AddArrangedSubview(_mapAction);

            var mapBox = new UIView { BackgroundColor = Theme.Hex(0xF1F3F5), TranslatesAutoresizingMaskIntoConstraints = false };
            mapBox.AddSubview(mapInner);
            mapBox.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(mapBox, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 220),

                NSLayoutConstraint.Create(mapInner, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, mapBox, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(mapInner, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, mapBox, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(mapInner, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, mapBox, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(mapInner, NSLayoutAttribute.Trailing, NSLayoutRelation.LessThanOrEqual, mapBox, NSLayoutAttribute.Trailing, 1, -16),
            });
            content.AddArrangedSubview(Pad(mapBox, 0, 12, 12, 0));

            // ===== مسار الرحلة (شريط التتبع) =====
            var stepsTitle = MakeLabel("مسار الرحلة", 14f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Right);
            _steps = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var stepsCard = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 12,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stepsCard.AddArrangedSubview(stepsTitle);
            stepsCard.AddArrangedSubview(_steps);

            var stepsBox = new UIView { BackgroundColor = Theme.White, TranslatesAutoresizingMaskIntoConstraints = false };
            stepsBox.AddSubview(stepsCard);
            stepsBox.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(stepsCard, NSLayoutAttribute.Top, NSLayoutRelation.Equal, stepsBox, NSLayoutAttribute.Top, 1, 16),
                NSLayoutConstraint.Create(stepsCard, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, stepsBox, NSLayoutAttribute.Bottom, 1, -16),
                NSLayoutConstraint.Create(stepsCard, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, stepsBox, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(stepsCard, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, stepsBox, NSLayoutAttribute.Trailing, 1, -16),
            });
            content.AddArrangedSubview(Pad(stepsBox, 8, 12, 12, 0));

            // ===== تفاصيل الطلب =====
            _info = MakeLabel("", 15f, Theme.Primary, UIFontWeight.Regular, UITextAlignment.Right);
            _info.Lines = 0;

            var infoBox = new UIView { BackgroundColor = Theme.White, TranslatesAutoresizingMaskIntoConstraints = false };
            infoBox.AddSubview(_info);
            infoBox.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Top, NSLayoutRelation.Equal, infoBox, NSLayoutAttribute.Top, 1, 16),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, infoBox, NSLayoutAttribute.Bottom, 1, -16),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, infoBox, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(_info, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, infoBox, NSLayoutAttribute.Trailing, 1, -16),
            });
            content.AddArrangedSubview(infoBox);

            // الوقت المتوقع + جهة التواصل
            _eta = MakeLabel("", 15f, Theme.Accent, UIFontWeight.Bold, UITextAlignment.Center);
            _eta.Lines = 0;
            content.AddArrangedSubview(Pad(_eta, 8, 16, 16, 0));

            _contact = MakeLabel("", 15f, Theme.Primary, UIFontWeight.Regular, UITextAlignment.Center);
            _contact.Lines = 0;
            content.AddArrangedSubview(Pad(_contact, 10, 16, 16, 0));

            var call = CardButton("اتصال", 48f);
            var sms = CardButton("رسالة", 48f);
            call.TouchUpInside += (s, e) => Call();
            sms.TouchUpInside += (s, e) => Sms();

            var contactStack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            contactStack.AddArrangedSubview(call);
            contactStack.AddArrangedSubview(sms);
            _contactRow = contactStack;
            content.AddArrangedSubview(Pad(_contactRow, 8, 16, 16, 0));

            var share = CardButton("مشاركة الرحلة", 48f);
            share.TouchUpInside += (s, e) => ShareTrip();
            content.AddArrangedSubview(Pad(share, 10, 16, 16, 0));

            var emergency = SolidButton("زر الطوارئ", Theme.Red, 48f, 15f, bold: true);
            emergency.TouchUpInside += (s, e) => SendEmergency();
            content.AddArrangedSubview(Pad(emergency, 8, 16, 16, 0));

            _offers = CardButton("عروض السائقين", 48f);
            _offers.Hidden = true;
            _offers.TouchUpInside += (s, e) => Push(new OrderOffersViewController(_orderId));
            content.AddArrangedSubview(Pad(_offers, 8, 16, 16, 0));

            _place = CardButton("قدّم عرض سعر", 48f);
            _place.Hidden = true;
            _place.TouchUpInside += (s, e) => PlaceOffer();
            content.AddArrangedSubview(Pad(_place, 8, 16, 16, 0));

            _pay = SolidButton("💳 دفع حق التوصيلة لمحفظة السائق", Theme.Primary, 48f, 15f);
            _pay.Hidden = true;
            _pay.TouchUpInside += (s, e) => Push(new WalletViewController());
            content.AddArrangedSubview(Pad(_pay, 8, 16, 16, 0));

            var refresh = CardButton("تحديث الحالة", 48f);
            refresh.TouchUpInside += (s, e) => Load();
            content.AddArrangedSubview(Pad(refresh, 16, 16, 16, 0));
        }

        private UIView Pad(UIView child, float top, float left, float right, float bottom)
        {
            var box = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            child.TranslatesAutoresizingMaskIntoConstraints = false;
            box.AddSubview(child);
            box.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Top, NSLayoutRelation.Equal, box, NSLayoutAttribute.Top, 1, top),
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, box, NSLayoutAttribute.Bottom, 1, -bottom),
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, box, NSLayoutAttribute.Leading, 1, left),
                NSLayoutConstraint.Create(child, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, box, NSLayoutAttribute.Trailing, 1, -right),
            });
            return box;
        }

        private UIButton CardButton(string title, float height)
        {
            var b = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(Theme.TextPrimary, UIControlState.Normal);
            b.TitleLabel.Font = Theme.FontMedium(15);
            b.BackgroundColor = Theme.White;
            b.Layer.CornerRadius = Theme.CornerTile;
            b.Layer.BorderWidth = 1f;
            b.Layer.BorderColor = Theme.CardStroke.CGColor;
            b.Layer.MasksToBounds = true;
            b.HeightAnchor.ConstraintEqualTo(height).Active = true;
            b.SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft;
            return b;
        }

        private UIButton SolidButton(string title, UIColor bg, float height, float size, bool bold = false)
        {
            var b = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(UIColor.White, UIControlState.Normal);
            b.SetTitleColor(UIColor.White.ColorWithAlpha(0.6f), UIControlState.Disabled);
            b.TitleLabel.Font = bold ? Theme.FontBold(size) : Theme.FontSemiBold(size);
            b.BackgroundColor = bg;
            b.Layer.CornerRadius = Theme.CornerTile;
            b.Layer.MasksToBounds = true;
            b.HeightAnchor.ConstraintEqualTo(height).Active = true;
            b.SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft;
            return b;
        }

        /* ---------- البيانات ---------- */

        private void Load()
        {
            Ui.Run(() =>
            {
                var order = Api.GetOrder(_orderId);

                var rated = false;
                try { rated = Api.HasRated(_orderId); } catch { }

                if (order != null && order.Status == "أثناء_الرحلة" && _longRideChecks < 3)
                {
                    _longRideChecks++;
                    try { Api.CheckLongRide(_orderId); } catch { }
                }

                var eta = 0;
                try { eta = Api.OrderEta(_orderId); } catch { }

                var driverNo = "";
                if (order != null && order.DriverId > 0)
                {
                    try { driverNo = Api.DriverNumberOf(order.DriverId); } catch { }
                }

                Ui.BeginOnMain(() =>
                {
                    _order = order;
                    _rated = rated;
                    Render(order, driverNo, eta);
                });
            }, null, ex => AppServices.Log("TawselaTrack", ex.Message));
        }

        private void Render(Order o, string driverNo, int eta)
        {
            if (o == null)
            {
                _status.Text = "لم يتم العثور على الطلب";
                return;
            }

            if (o.Id > 0) _topSub.Text = "طلب رقم " + o.Id;

            _status.Text = o.StatusText;
            _status.TextColor = Theme.FromArgb(o.StatusColor);

            // الخدمة والدفع (بديل txtStatusDetail)
            _detail.Text = "الخدمة: " + (string.IsNullOrEmpty(o.ServiceTypeName) ? "عادية" : o.ServiceTypeName) +
                           "   |   الدفع: " + (string.IsNullOrEmpty(o.PaymentMethod) ? "غير محدد" : o.PaymentMethod);

            // رمز الرحلة
            if (Session.IsCustomer && o.CustomerConfirmedArrival)
            {
                _pin.Text = "✅ تم تأكيد وصول السائق";
                _pin.Hidden = false;
            }
            else if (Session.IsCustomer && !string.IsNullOrEmpty(o.PinCode))
            {
                _pin.Text = "🔑 رمز الرحلة: " + o.PinCode;
                _pin.Hidden = false;
            }
            else
            {
                _pin.Hidden = true;
            }

            // أزرار تأكيد الوصول
            var showArrival = Session.IsCustomer && o.Status == "وصل_السائق" && !o.CustomerConfirmedArrival;
            _arrivalRow.Hidden = !showArrival;

            var reported = o.ArrivalReportedAt != default(DateTime);
            _notArrived.Enabled = !reported;
            _notArrived.SetTitle(reported ? "تم الإبلاغ" : "لم يصل ⚠️", UIControlState.Normal);

            // تفاصيل الطلب
            var info = "من: " + o.FromAddress + "\n" +
                       "إلى: " + o.ToAddress + "\n" +
                       (string.IsNullOrEmpty(o.DriverName)
                           ? ""
                           : "السائق: " + o.DriverName +
                             (string.IsNullOrEmpty(driverNo) ? "" : " (رقم " + driverNo + ")") + "\n") +
                       "الأجرة: " + (o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare).ToString("0.##") + " ج.م";

            if (!string.IsNullOrEmpty(o.StopAddress)) info += "\nمحطة وسيطة: " + o.StopAddress;
            if (!string.IsNullOrEmpty(o.FloorInfo)) info += "\nتفاصيل: " + o.FloorInfo;
            if (o.OfferedPrice > 0) info += "\nسعر العميل المقترح: " + o.OfferedPrice.ToString("0.##") + " ج.م";
            if (o.CancelFee > 0) info += "\nغرامة إلغاء: " + o.CancelFee.ToString("0.##") + " ج.م";
            _info.Text = info;

            _eta.Text = eta > 0 ? "الوقت المتوقع ≈ " + eta + " دقيقة" : "";

            // جهة التواصل
            var otherName = Session.IsCustomer ? o.DriverName : o.CustomerName;
            var otherPhone = Session.IsCustomer ? o.DriverPhone : o.CustomerPhone;
            if (!string.IsNullOrEmpty(otherPhone))
            {
                _contact.Text = "التواصل مع " + (Session.IsCustomer ? "السائق" : "العميل") +
                                ": " + otherName + " (" + otherPhone + ")";
                _contactRow.Hidden = false;
            }
            else
            {
                _contact.Text = "";
                _contactRow.Hidden = true;
            }

            // الأزرار الشرطية
            if (o.BiddingEnabled)
            {
                _offers.Hidden = !(Session.IsCustomer && o.Status == "جديد");
                _place.Hidden = !(Session.IsDriver && o.Status == "جديد");
            }
            else
            {
                _offers.Hidden = true;
                _place.Hidden = true;
            }

            _pay.Hidden = !(ReleaseConfig.WalletTransferEnabled && Session.IsCustomer &&
                            !string.IsNullOrEmpty(o.DriverPhone) &&
                            o.Status != "مكتمل" && o.Status != "ملغي" && o.Status != "جديد");

            // مسار الرحلة
            BuildSteps(o.Status);

            // الخريطة
            UpdateMap(o);

            // تقييم بعد الاكتمال (مرة واحدة)
            if (o.Status == "مكتمل" && !_rated && Session.IsCustomer && !_ratingPrompted)
            {
                _ratingPrompted = true;
                Ui.Confirm("شكراً على رحلتك!", "هل تود تقييم السائق؟", "نعم",
                    () => Push(new RateViewController(o.DriverId, o.Id)), "لاحقاً");
            }
        }

        private void UpdateMap(Order o)
        {
            var hasDriver = o.DriverId > 0 && o.Status != "مكتمل" && o.Status != "ملغي";
            if (hasDriver)
            {
                _mapStatus.Text = "خريطة تتبع السائق جاهزة — اضغط للعرض";
                _mapAction.SetTitle("عرض الخريطة", UIControlState.Normal);
            }
            else
            {
                _mapStatus.Text = "ستظهر خريطة تتبع السائق بعد قبول الطلب.";
                _mapAction.SetTitle("إعادة المحاولة", UIControlState.Normal);
            }
        }

        private void OnMapAction()
        {
            var o = _order;
            if (o == null) return;

            var hasDriver = o.DriverId > 0 && o.Status != "مكتمل" && o.Status != "ملغي";
            if (!hasDriver || (o.FromLat == 0 && o.FromLng == 0))
            {
                Load();
                return;
            }

            Push(MapViewController.TrackDriver(
                o.FromLat, o.FromLng, o.FromAddress,
                o.ToLat, o.ToLng, o.ToAddress,
                () => Api.GetDriverLocation(o.DriverId)));
        }

        /* ---------- شريط التتبع ---------- */

        private static int StageIndex(string status)
        {
            switch (status)
            {
                case "جديد": return 0;
                case "قيد_التنفيذ": return 1;
                case "في_الطريق": return 2;
                case "وصل_السائق": return 3;
                case "أثناء_الرحلة": return 4;
                case "مكتمل": return 5;
                default: return -1;
            }
        }

        private void BuildSteps(string status)
        {
            foreach (var v in _steps.ArrangedSubviews)
            {
                _steps.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (status == "ملغي")
            {
                var card = new UIView
                {
                    BackgroundColor = Theme.Red,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                card.Layer.CornerRadius = 12f;
                var lbl = MakeLabel("✕\nتم إلغاء الطلب", 16f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);
                lbl.Lines = 2;
                card.AddSubview(lbl);
                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(lbl, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterX, 1, 0),
                    NSLayoutConstraint.Create(lbl, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(card, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 96),
                });
                _steps.AddArrangedSubview(card);
                return;
            }

            int cur = StageIndex(status);

            for (int i = 0; i < Stages.Length; i++)
            {
                bool isDone = i < cur;
                bool isCurrent = i == cur;

                var row = new UIStackView
                {
                    Axis = UILayoutConstraintAxis.Horizontal,
                    Spacing = 8,
                    Alignment = UIStackViewAlignment.Fill,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };

                // العمود الأيمن: الدائرة + الخط
                var col = new UIStackView
                {
                    Axis = UILayoutConstraintAxis.Vertical,
                    Alignment = UIStackViewAlignment.Center,
                    Spacing = 0,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                col.WidthAnchor.ConstraintEqualTo(40f).Active = true;

                var circle = new UIView
                {
                    BackgroundColor = isDone ? Theme.Green : (isCurrent ? Theme.Primary : Theme.Hex(0xF7F8FA)),
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                circle.Layer.CornerRadius = 14f;
                circle.Layer.BorderWidth = (isDone || isCurrent) ? 0f : 2f;
                circle.Layer.BorderColor = Theme.Gray.CGColor;
                circle.WidthAnchor.ConstraintEqualTo(28f).Active = true;
                circle.HeightAnchor.ConstraintEqualTo(28f).Active = true;

                if (isCurrent)
                {
                    circle.Layer.BorderWidth = 2f;
                    circle.Layer.BorderColor = Theme.Accent.CGColor;
                }

                var num = MakeLabel(isDone ? "✓" : (i + 1).ToString(),
                    isDone ? 13f : 12f,
                    (isDone || isCurrent) ? UIColor.White : Theme.Gray,
                    UIFontWeight.Bold, UITextAlignment.Center);
                circle.AddSubview(num);
                circle.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(num, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, circle, NSLayoutAttribute.CenterX, 1, 0),
                    NSLayoutConstraint.Create(num, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, circle, NSLayoutAttribute.CenterY, 1, 0),
                });

                col.AddArrangedSubview(circle);

                if (i < Stages.Length - 1)
                {
                    var line = new UIView
                    {
                        BackgroundColor = isDone ? Theme.Green : Theme.CardStroke,
                        TranslatesAutoresizingMaskIntoConstraints = false
                    };
                    line.Layer.CornerRadius = 1.5f;
                    line.WidthAnchor.ConstraintEqualTo(3f).Active = true;
                    line.HeightAnchor.ConstraintEqualTo(isDone ? 20f : 12f).Active = true;
                    col.AddArrangedSubview(line);
                }

                // العمود الأيسر: العنوان + الوصف
                var colText = new UIStackView
                {
                    Axis = UILayoutConstraintAxis.Vertical,
                    Spacing = 2,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };

                var stageTitle = MakeLabel(Stages[i], 15f,
                    isDone ? Theme.Green : (isCurrent ? Theme.Primary : Theme.Gray),
                    isCurrent ? UIFontWeight.Bold : UIFontWeight.Regular,
                    UITextAlignment.Right);

                if (isDone)
                {
                    stageTitle.AttributedText = new NSAttributedString(Stages[i], new UIStringAttributes
                    {
                        Font = Theme.Font(15),
                        ForegroundColor = Theme.Green,
                        StrikethroughStyle = NSUnderlineStyle.Single
                    });
                }

                colText.AddArrangedSubview(stageTitle);

                if (isCurrent || isDone)
                {
                    var desc = MakeLabel(StageDesc[i], isDone ? 11f : 12f,
                        isDone ? Theme.Gray : Theme.TextSecondary,
                        UIFontWeight.Regular, UITextAlignment.Right);
                    desc.Lines = 0;
                    colText.AddArrangedSubview(desc);
                }

                row.AddArrangedSubview(col);
                row.AddArrangedSubview(colText);
                colText.SetContentHuggingPriority(1f, UILayoutConstraintAxis.Horizontal);

                _steps.AddArrangedSubview(row);
            }
        }

        /* ---------- أفعال ---------- */

        private void ConfirmArrival()
        {
            if (_busy) return;
            _busy = true;
            var progress = Ui.ShowProgress("جارٍ تأكيد وصول السائق…");

            Ui.Run(() => Api.ConfirmArrival(_orderId), () =>
            {
                _busy = false;
                progress.Dismiss();
                Ui.Toast("تم تأكيد وصول السائق ✅");
                Load();
            }, ex =>
            {
                _busy = false;
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        private void NotArrived()
        {
            Ui.Prompt("السائق لم يصل", "اكتب ما حدث (اختياري)", "", "السبب", reason =>
            {
                var progress = Ui.ShowProgress("جارٍ الإبلاغ…");
                Ui.Run(() => Api.ReportNotArrived(_orderId, reason), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم إبلاغ الإدارة");
                    Load();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            });
        }

        private void PlaceOffer()
        {
            Ui.Prompt("قدّم عرضاً", "سعرك المقترح بالج.م", "", "0", text =>
            {
                decimal price;
                if (!decimal.TryParse((text ?? "").Trim(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out price) || price <= 0)
                {
                    Ui.Toast("أدخل سعراً");
                    return;
                }

                var p = Ui.ShowProgress("جاري الإرسال…");
                Ui.Run(() => Api.PlaceOffer(_orderId, price), () =>
                {
                    p.Dismiss();
                    Ui.Toast("تم إرسال العرض");
                }, ex =>
                {
                    p.Dismiss();
                    Ui.Toast(ex.Message);
                });
            }, UIKeyboardType.DecimalPad);
        }

        private void SendEmergency()
        {
            Ui.Confirm("بلاغ طوارئ", "إرسال بلاغ طوارئ للإدارة مع بيانات هذه الرحلة؟", "إرسال", () =>
            {
                var p = Ui.ShowProgress("إرسال إشعار الطوارئ…");
                var detail = "بلاغ من المسافر " + Session.FullName + " (طلب " + _orderId + ")";

                Ui.Run(() => Api.SendEmergencyDetail(detail), () =>
                {
                    p.Dismiss();
                    Ui.Toast("تم إرسال بلاغ الطوارئ للإدارة");
                }, ex =>
                {
                    p.Dismiss();
                    Ui.Toast("خطأ: " + ex.Message);
                });
            });
        }

        private void Call()
        {
            var o = _order;
            if (o == null) return;
            var phone = Session.IsCustomer ? o.DriverPhone : o.CustomerPhone;
            var clean = (phone ?? "").Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(clean)) return;
            AppServices.OpenUrl("tel:" + clean);
        }

        private void Sms()
        {
            var o = _order;
            if (o == null) return;
            var phone = Session.IsCustomer ? o.DriverPhone : o.CustomerPhone;
            var clean = (phone ?? "").Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(clean)) return;
            AppServices.OpenUrl("sms:" + clean);
        }

        private void ShareTrip()
        {
            var o = _order;
            if (o == null) return;

            var text = "أنا في رحلة توصيلة رقم " + o.Id +
                       "\n\n📍 من: " + o.FromAddress +
                       "\n\n🎯 إلى: " + o.ToAddress +
                       "\n\nالسائق: " + o.DriverName +
                       (Session.IsCustomer && !string.IsNullOrEmpty(o.PinCode)
                           ? "\n\nرمز الرحلة: " + o.PinCode
                           : "");

            var share = new UIActivityViewController(new NSObject[] { new NSString(text) }, null);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                share.PopoverPresentationController != null)
            {
                share.PopoverPresentationController.SourceView = View;
                share.PopoverPresentationController.SourceRect =
                    new CGRect(View.Bounds.Width / 2, View.Bounds.Height / 2, 1, 1);
            }

            PresentViewController(share, true, null);
        }
    }
}
