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
    /// بديل Activities/TrackOrderActivity.cs (من جهة العميل):
    /// متابعة حالة الطلب كل 5 ثوانٍ، بيانات السائق، رمز PIN، الإلغاء،
    /// تأكيد الوصول، والتقييم بعد الاكتمال.
    /// </summary>
    public class TrackOrderViewController : BaseViewController
    {
        private readonly int _orderId;
        private UIStackView _stack;
        private Order _order;
        private NSTimer _timer;
        private UILabel _statusLabel;
        private bool _rated;
        private int _longRideChecks;
        private int _eta;
        private string _driverNumber = "";

        public TrackOrderViewController(int orderId) : base("تتبع الطلب")
        {
            _orderId = orderId;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
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

        private void BuildUi()
        {
            _stack = CreateScrollContent(12f, 16f);

            _statusLabel = MakeLabel("جاري البحث عن سائق...", 18f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _stack.AddArrangedSubview(_statusLabel);

            var refresh = Controls.OutlineButton("تحديث الحالة");
            refresh.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(refresh);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var order = Api.GetOrder(_orderId);

                var rated = false;
                try { rated = Api.HasRated(_orderId); } catch { }

                var driverNo = "";
                if (order != null && order.DriverId > 0)
                {
                    try { driverNo = Api.DriverNumberOf(order.DriverId); } catch { }
                }

                // فحص الرحلات الطويلة: 3 محاولات فقط كما في أندرويد
                if (order != null && order.Status == "في_الطريق" && _longRideChecks < 3)
                {
                    _longRideChecks++;
                    try { Api.CheckLongRide(_orderId); } catch { }
                }

                var eta = 0;
                try { eta = Api.OrderEta(_orderId); } catch { }

                Ui.BeginOnMain(() =>
                {
                    _order = order;
                    _rated = rated;
                    _driverNumber = driverNo;
                    _eta = eta;
                    Render();
                });
            }, null, ex =>
            {
                // فشل الشبكة خلال الاستطلاع لا يجب أن يزعج المستخدم كل 5 ثوانٍ
                AppServices.Log("TawselaTrack", ex.Message);
            });
        }

        private void Render()
        {
            var o = _order;
            if (o == null) return;

            // احتفظ بالعنوان الأول ثم أعد بناء البطاقات
            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 1; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            _statusLabel.Text = o.StatusText;
            _statusLabel.TextColor = Theme.FromArgb(o.StatusColor);

            _stack.AddArrangedSubview(Controls.InfoRow("رقم الطلب", o.OrderNumber));

            if (!string.IsNullOrEmpty(o.DriverName))
            {
                _stack.AddArrangedSubview(Controls.InfoRow("السائق",
                    o.DriverName + (string.IsNullOrEmpty(_driverNumber) ? "" : " (رقم " + _driverNumber + ")")));
                if (!string.IsNullOrEmpty(o.DriverPhone))
                    _stack.AddArrangedSubview(Controls.InfoRow("هاتف السائق", o.DriverPhone));
            }

            // الوقت المتوقع للوصول (بديل Api.OrderEta في أندرويد)
            if (_eta > 0)
                _stack.AddArrangedSubview(Controls.InfoRow("الوقت المتوقع", "السائق على بعد " + _eta + " دقيقة"));

            _stack.AddArrangedSubview(Controls.InfoRow("من", o.FromAddress));
            _stack.AddArrangedSubview(Controls.InfoRow("إلى", o.ToAddress));

            if (!string.IsNullOrEmpty(o.Note))
                _stack.AddArrangedSubview(Controls.InfoRow("ملاحظة", o.Note));

            var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
            _stack.AddArrangedSubview(Controls.InfoRow("الأجرة", fare.ToString("0.00") + " ج.م"));

            if (!string.IsNullOrEmpty(o.PinCode))
                _stack.AddArrangedSubview(Controls.InfoRow("رمز بدء الرحلة", o.PinCode));

            // أزرار التواصل مع السائق
            if (!string.IsNullOrEmpty(o.DriverPhone) && o.Status != "مكتمل" && o.Status != "ملغي")
            {
                var call = Controls.IconButton("اتصال", "ic_action_call", Theme.Green);
                call.TouchUpInside += (s, e) => Call(o.DriverPhone);
                _stack.AddArrangedSubview(call);
            }

            if (o.Id > 0)
            {
                var chat = Controls.OutlineButton("رسالة");
                chat.TouchUpInside += (s, e) => Push(new ChatViewController(o.Id));
                _stack.AddArrangedSubview(chat);
            }

            // عرض المسار على الخريطة (بديل فتح MapActivity بوضع showFrom/showTo)
            if ((o.FromLat != 0 || o.FromLng != 0) && (o.ToLat != 0 || o.ToLng != 0))
            {
                var driverId = o.DriverId;
                var trackable = driverId > 0 && o.Status != "مكتمل" && o.Status != "ملغي";

                var map = Controls.OutlineButton(trackable ? "موقع السائق" : "مسار الرحلة");
                map.TouchUpInside += (s, e) =>
                {
                    if (trackable)
                    {
                        Push(MapViewController.TrackDriver(
                            o.FromLat, o.FromLng, o.FromAddress,
                            o.ToLat, o.ToLng, o.ToAddress,
                            () => Api.GetDriverLocation(driverId)));
                    }
                    else
                    {
                        Push(MapViewController.ShowRoute(
                            o.FromLat, o.FromLng, o.FromAddress, o.ToLat, o.ToLng, o.ToAddress));
                    }
                };
                _stack.AddArrangedSubview(map);
            }

            // السائق لم يصل — إبلاغ الإدارة (بديل Api.ReportNotArrived)
            if (o.DriverId > 0 && (o.Status == "في_الطريق" || o.Status == "وصل_السائق"))
            {
                var notArrived = Controls.DangerButton("لم يصل ⚠️");
                notArrived.TouchUpInside += (s, e) => ReportNotArrived(o);
                _stack.AddArrangedSubview(notArrived);
            }

            // طلب بمزايدة: اعرض عروض السائقين حتى يتم القبول
            if (o.BiddingEnabled && o.DriverId == 0 && (o.Status == "جديد" || o.Status == "قيد_التنفيذ"))
            {
                var offers = Controls.PrimaryButton("عروض السائقين");
                offers.TouchUpInside += (s, e) => Push(new OrderOffersViewController(o.Id));
                _stack.AddArrangedSubview(offers);
            }

            // السائق يقدّم عرضه على الطلب (بديل btnPlaceOffer)
            if (Session.IsDriver && o.BiddingEnabled && o.Status == "جديد" && o.DriverId == 0)
            {
                var place = Controls.PrimaryButton("تقديم عرض سعر");
                place.TouchUpInside += (s, e) =>
                {
                    Ui.Prompt("عرض سعر", "اكتب السعر الذي تطلبه (ج.م)", "", "0", text =>
                    {
                        decimal price;
                        if (!decimal.TryParse((text ?? "").Trim(), NumberStyles.Any,
                                CultureInfo.InvariantCulture, out price) || price <= 0)
                        {
                            Ui.Toast("سعر غير صحيح");
                            return;
                        }

                        var p = Ui.ShowProgress("جارٍ إرسال العرض…");
                        Ui.Run(() => Api.PlaceOffer(o.Id, price), () =>
                        {
                            p.Dismiss();
                            Ui.Toast("تم تقديم العرض");
                        }, ex =>
                        {
                            p.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    }, UIKeyboardType.DecimalPad);
                };
                _stack.AddArrangedSubview(place);
            }

            // مشاركة تفاصيل الرحلة (بديل btnShareTrip)
            var share = Controls.OutlineButton("مشاركة الرحلة");
            share.TouchUpInside += (s, e) => ShareTrip(o);
            _stack.AddArrangedSubview(share);

            // زر الطوارئ من داخل الرحلة (بديل btnEmergency → SendEmergencyDetail)
            var emergency = Controls.DangerButton("زر الطوارئ");
            emergency.TouchUpInside += (s, e) =>
            {
                Ui.Confirm("بلاغ طوارئ", "إرسال بلاغ طوارئ للإدارة مع بيانات هذه الرحلة؟", "إرسال", () =>
                {
                    var p = Ui.ShowProgress("جارٍ إرسال البلاغ…");
                    var detail = "بلاغ من المسافر " + Session.FullName + " (طلب " + o.Id + ")";

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
            };
            _stack.AddArrangedSubview(emergency);

            // تقسيم الأجرة مع شخص آخر (بعد قبول سائق)
            if (o.DriverId > 0 && o.Status != "مكتمل" && o.Status != "ملغي")
            {
                var fareForSplit = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
                var split = Controls.OutlineButton("تقسيم الفاتورة");
                split.TouchUpInside += (s, e) => Push(new SplitFareViewController(o.Id, fareForSplit));
                _stack.AddArrangedSubview(split);
            }

            // تأكيد وصول السائق (إن لم يؤكد)
            if (!o.CustomerConfirmedArrival && (o.Status == "وصل_السائق" || o.Status == "في_الطريق"))
            {
                var confirm = Controls.PrimaryButton("السائق وصل ✅");
                confirm.TouchUpInside += (s, e) =>
                {
                    Ui.Confirm("تأكيد الوصول", "هل وصل السائق فعلاً؟", "نعم وصل", () =>
                    {
                        Ui.Run(() => Api.ConfirmArrival(o.Id), () =>
                        {
                            Ui.Toast("تم تأكيد الوصول");
                            Load();
                        }, ex => Ui.Toast(ex.Message));
                    });
                };
                _stack.AddArrangedSubview(confirm);
            }

            // التقييم بعد الاكتمال (مرة واحدة فقط — بديل Api.HasRated)
            if (o.Status == "مكتمل")
            {
                if (!_rated)
                {
                    var rate = Controls.PrimaryButton("تقييم الرحلة");
                    rate.TouchUpInside += (s, e) => Push(new RateViewController(o.DriverId, o.Id));
                    _stack.AddArrangedSubview(rate);
                }
                else
                {
                    _stack.AddArrangedSubview(MakeLabel("تم تقييم هذه الرحلة — شكرًا لك",
                        13f, Theme.Green, UIFontWeight.Medium, UITextAlignment.Right));
                }
            }

            // الإلغاء
            if (o.Status == "جديد" || o.Status == "قيد_التنفيذ" || o.Status == "في_الطريق")
            {
                var cancel = Controls.DangerButton("إلغاء الطلب");
                cancel.TouchUpInside += (s, e) => Cancel(o);
                _stack.AddArrangedSubview(cancel);
            }
        }

        /// <summary>مشاركة تفاصيل الرحلة نصيًا (بديل Intent.ActionSend).</summary>
        private void ShareTrip(Order o)
        {
            if (o == null) return;

            var text = "أنا في رحلة توصيلة رقم " + o.Id +
                       "\n\n📍 من: " + o.FromAddress +
                       "\n\n🎯 إلى: " + o.ToAddress +
                       "\n\nالسائق: " + o.DriverName +
                       (Session.IsCustomer && !string.IsNullOrEmpty(o.PinCode)
                           ? "\n\nرمز الرحلة: " + o.PinCode
                           : "");

            var share = new UIActivityViewController(
                new NSObject[] { new NSString(text) }, null);

            // على iPad يجب تحديد مصدر الـ Popover وإلا يحدث Crash
            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                share.PopoverPresentationController != null)
            {
                share.PopoverPresentationController.SourceView = View;
                share.PopoverPresentationController.SourceRect =
                    new CoreGraphics.CGRect(View.Bounds.Width / 2, View.Bounds.Height / 2, 1, 1);
            }

            PresentViewController(share, true, null);
        }

        private void Cancel(Order o)
        {
            Ui.Prompt("إلغاء الطلب", "اكتب سبب الإلغاء", "", "السبب", reason =>
            {
                Ui.Run(() => Api.CancelOrder(o.Id, reason), () =>
                {
                    Ui.Toast("تم إلغاء الطلب");
                    Load();
                }, ex => Ui.Toast(ex.Message));
            });
        }

        /// <summary>إبلاغ الإدارة أن السائق لم يصل (بديل Api.ReportNotArrived).</summary>
        private void ReportNotArrived(Order o)
        {
            Ui.Prompt("السائق لم يصل", "اكتب ما حدث (اختياري)", "", "السبب", reason =>
            {
                var progress = Ui.ShowProgress("جارٍ الإبلاغ…");
                Ui.Run(() => Api.ReportNotArrived(o.Id, reason), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم إبلاغ الإدارة");
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            });
        }

        private void Call(string phone)
        {
            var clean = (phone ?? "").Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(clean)) return;
            AppServices.OpenUrl("tel:" + clean);
        }
    }
}
