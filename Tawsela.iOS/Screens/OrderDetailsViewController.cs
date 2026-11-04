using System;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/OrderDetailsActivity.cs من جهة السائق:
    /// قبول الطلب، بدء الرحلة برمز PIN، إكمال الرحلة، والتواصل مع العميل.
    /// </summary>
    public class OrderDetailsViewController : BaseViewController
    {
        private readonly int _orderId;
        private UIStackView _stack;
        private UILabel _statusLabel;
        private Order _order;
        private NSTimer _timer;
        private string _driverNumber = "";
        private bool _isFavorite;

        public OrderDetailsViewController(int orderId) : base("تفاصيل الطلب")
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
            _timer = NSTimer.CreateRepeatingScheduledTimer(6.0, _ => Load());
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (_timer != null) { _timer.Invalidate(); _timer.Dispose(); _timer = null; }
        }

        private void BuildUi()
        {
            _stack = CreateScrollContent(12f, 16f);
            _statusLabel = MakeLabel("جارٍ التحميل…", 18f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _stack.AddArrangedSubview(_statusLabel);

            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(refresh);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var o = Api.GetOrder(_orderId);

                var driverNo = "";
                var fav = false;
                if (o != null && o.DriverId > 0)
                {
                    try { driverNo = Api.DriverNumberOf(o.DriverId); } catch { }
                    if (Session.IsCustomer)
                    {
                        try { fav = Api.IsFavorite(o.DriverId); } catch { }
                    }
                }

                Ui.BeginOnMain(() =>
                {
                    _order = o;
                    _driverNumber = driverNo;
                    _isFavorite = fav;
                    Render();
                });
            }, null, ex => AppServices.Log("TawselaOrderDetails", ex.Message));
        }

        private void Render()
        {
            var o = _order;
            if (o == null) return;

            for (int i = _stack.ArrangedSubviews.Length - 1; i >= 1; i--)
            {
                var v = _stack.ArrangedSubviews[i];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            _statusLabel.Text = o.StatusText;
            _statusLabel.TextColor = Theme.FromArgb(o.StatusColor);

            _stack.AddArrangedSubview(Controls.InfoRow("رقم الطلب", o.OrderNumber));
            _stack.AddArrangedSubview(Controls.InfoRow("العميل", o.CustomerName));
            if (!string.IsNullOrEmpty(o.CustomerPhone))
                _stack.AddArrangedSubview(Controls.InfoRow("هاتف العميل", o.CustomerPhone));
            _stack.AddArrangedSubview(Controls.InfoRow("من", o.FromAddress));
            _stack.AddArrangedSubview(Controls.InfoRow("إلى", o.ToAddress));
            if (!string.IsNullOrEmpty(o.Note))
                _stack.AddArrangedSubview(Controls.InfoRow("ملاحظة", o.Note));

            var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
            _stack.AddArrangedSubview(Controls.InfoRow("الأجرة", fare.ToString("0.00") + " ج.م"));

            if (!string.IsNullOrEmpty(o.CustomerPhone))
            {
                var call = Controls.IconButton("اتصال بالعميل", "ic_action_call", Theme.Green);
                call.TouchUpInside += (s, e) => AppServices.OpenUrl("tel:" + o.CustomerPhone.Replace(" ", ""));
                _stack.AddArrangedSubview(call);
            }

            var chat = Controls.OutlineButton("محادثة الطلب");
            chat.TouchUpInside += (s, e) => Push(new ChatViewController(o.Id));
            _stack.AddArrangedSubview(chat);

            // عرض مسار الطلب على الخريطة (بديل btnOpenOrderMap في أندرويد)
            if (o.FromLat != 0 || o.ToLat != 0)
            {
                var mapBtn = Controls.OutlineButton("عرض المسار على الخريطة");
                mapBtn.TouchUpInside += (s, e) => Push(MapViewController.ShowRoute(
                    o.FromLat, o.FromLng, o.FromAddress, o.ToLat, o.ToLng, o.ToAddress));
                _stack.AddArrangedSubview(mapBtn);
            }

            // بيانات السائق للعميل: رقم السائق + مفضلة + تتبع + حظر + إبلاغ
            if (o.DriverId > 0 && Session.IsCustomer)
            {
                if (!string.IsNullOrEmpty(_driverNumber))
                    _stack.AddArrangedSubview(Controls.InfoRow("رقم السائق", _driverNumber));

                var favBtn = Controls.OutlineButton(_isFavorite ? "★ إزالة من المفضلة" : "☆ إضافة للمفضلة");
                favBtn.TouchUpInside += (s, e) => ToggleFavorite(o.DriverId);
                _stack.AddArrangedSubview(favBtn);

                if (o.Status != "مكتمل" && o.Status != "ملغي")
                {
                    var track = Controls.OutlineButton("تتبع السائق على الخريطة");
                    var driverId = o.DriverId;
                    track.TouchUpInside += (s, e) => Push(MapViewController.TrackDriver(
                        o.FromLat, o.FromLng, o.FromAddress,
                        o.ToLat, o.ToLng, o.ToAddress,
                        () => Api.GetDriverLocation(driverId)));
                    _stack.AddArrangedSubview(track);

                    var notArrived = Controls.DangerButton("السائق لم يصل");
                    notArrived.TouchUpInside += (s, e) => ReportNotArrived(o);
                    _stack.AddArrangedSubview(notArrived);

                    var block = Controls.DangerButton("حظر هذا السائق");
                    block.TouchUpInside += (s, e) => BlockDriver(o.DriverId);
                    _stack.AddArrangedSubview(block);
                }
            }

            AddActions(o);
        }

        /// <summary>إضافة/إزالة السائق من المفضلة (بديل Api.ToggleFavorite).</summary>
        private void ToggleFavorite(int driverId)
        {
            var progress = Ui.ShowProgress("جارٍ التحديث…");
            Ui.Run(() =>
            {
                bool nowFavorite;
                try { nowFavorite = Api.ToggleFavorite(driverId); }
                catch { nowFavorite = false; }

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();
                    _isFavorite = nowFavorite;
                    Ui.Toast(nowFavorite ? "تمت الإضافة للمفضلة" : "تمت الإزالة من المفضلة");
                    Render();
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
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

        /// <summary>
        /// حظر/إلغاء حظر السائق — يعمل كمفتاح مثل أندرويد:
        /// إن كان محظورًا يُلغى الحظر مباشرة، وإلا يطلب التأكيد (بديل btnBlock).
        /// </summary>
        private void BlockDriver(int driverId)
        {
            var progress = Ui.ShowProgress("جارٍ التحقق…");

            Ui.Run(() =>
            {
                var blocked = Api.IsBlocked(driverId);

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    if (blocked)
                    {
                        Ui.Run(() => Api.UnblockDriver(driverId), () =>
                        {
                            Ui.Toast("تم إلغاء حظر السائق");
                        }, ex => Ui.Toast(ex.Message));
                        return;
                    }

                    Ui.Confirm("حظر السائق", "لن يظهر لك هذا السائق في الطلبات القادمة. تأكيد الحظر؟", "حظر", () =>
                    {
                        var p = Ui.ShowProgress("جارٍ الحظر…");
                        Ui.Run(() => Api.BlockDriver(driverId), () =>
                        {
                            p.Dismiss();
                            Ui.Toast("تم حظر السائق");
                        }, ex =>
                        {
                            p.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    });
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        /// <summary>
        /// قبول الطلب: يجلب عمولة القبول أولًا ويعرضها للتأكيد إن كانت أكبر من صفر
        /// (بديل Api.DriverAcceptFee + ShowAcceptCommission في أندرويد).
        /// </summary>
        private void AcceptOrder(Order o)
        {
            var progress = Ui.ShowProgress("جارٍ التحقق…");

            Ui.Run(() =>
            {
                decimal fee = 0m;
                try { fee = Api.DriverAcceptFee(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    if (fee > 0)
                    {
                        Ui.Confirm("عمولة قبول الطلب",
                            "سيتم خصم " + fee.ToString("0.##") + " ج.م من محفظتك عند قبول هذا الطلب.\n" +
                            "هل تريد المتابعة؟",
                            "موافق",
                            () => DoAccept(o));
                        return;
                    }

                    DoAccept(o);
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        private void DoAccept(Order o)
        {
            Ui.Run(() => Api.AssignDriver(o.Id), () =>
            {
                Ui.Toast("تم قبول الطلب");
                Load();
            }, ex => Ui.Toast(ex.Message));
        }

        private void AddActions(Order o)
        {
            // طلب بمزايدة: السائق يقدّم سعرًا بدل القبول المباشر
            if (o.BiddingEnabled && o.Status == "جديد" && o.DriverId == 0)
            {
                var bid = Controls.PrimaryButton("تقديم عرض سعر");
                bid.TouchUpInside += (s, e) =>
                {
                    Ui.Prompt("عرض سعر", "اكتب السعر الذي تطلبه (ج.م)", "", "0", text =>
                    {
                        decimal price;
                        if (!decimal.TryParse(text, out price) || price <= 0)
                        {
                            Ui.Toast("أدخل سعرًا صحيحًا");
                            return;
                        }

                        var progress = Ui.ShowProgress("جارٍ إرسال العرض…");
                        Ui.Run(() => Api.PlaceOffer(o.Id, price), () =>
                        {
                            progress.Dismiss();
                            Ui.Toast("تم إرسال عرضك");
                            Load();
                        }, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    }, UIKeyboardType.DecimalPad);
                };
                _stack.AddArrangedSubview(bid);
            }

            // قبول الطلب (إسناد السائق الحالي) — مع تنبيه عمولة القبول إن وُجدت
            if (o.Status == "جديد" && o.DriverId == 0 && !o.BiddingEnabled)
            {
                var accept = Controls.PrimaryButton("قبول الطلب");
                accept.TouchUpInside += (s, e) => AcceptOrder(o);
                _stack.AddArrangedSubview(accept);
            }

            // بدء الرحلة برمز PIN
            if (o.Status == "وصل_السائق" || o.Status == "قيد_التنفيذ" || o.Status == "جديد")
            {
                var start = Controls.PrimaryButton("بدء الرحلة (رمز PIN)");
                start.TouchUpInside += (s, e) =>
                {
                    Ui.Prompt("رمز بدء الرحلة", "أدخل الرمز الذي يظهر للعميل", "", "PIN",
                        pin => StartRide(o, pin), UIKeyboardType.NumberPad);
                };
                _stack.AddArrangedSubview(start);
            }

            // السائق يؤكد وصوله للعميل (بديل Api.ConfirmArrival)
            if (Session.IsDriver && !o.CustomerConfirmedArrival &&
                (o.Status == "قيد_التنفيذ" || o.Status == "في_الطريق"))
            {
                var arrived = Controls.PrimaryButton("وصلت للعميل");
                arrived.TouchUpInside += (s, e) =>
                {
                    Ui.Confirm("تأكيد الوصول", "وصلت إلى العميل فعلاً؟", "نعم وصلت", () =>
                    {
                        var progress = Ui.ShowProgress("جارٍ التأكيد…");
                        Ui.Run(() => Api.ConfirmArrival(o.Id), () =>
                        {
                            progress.Dismiss();
                            Ui.Toast("تم إبلاغ العميل بوصولك");
                            Load();
                        }, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    });
                };
                _stack.AddArrangedSubview(arrived);
            }

            // السائق وصل الوجهة (بديل Api.UpdateStatus)
            if (Session.IsDriver && o.Status == "أثناء_الرحله")
            {
                var reached = Controls.OutlineButton("وصلت إلى الوجهة");
                reached.TouchUpInside += (s, e) =>
                {
                    var progress = Ui.ShowProgress("جارٍ التحديث…");
                    Ui.Run(() => Api.UpdateStatus(o.Id, "وصلت_الوجهة"), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم تحديث حالة الطلب");
                        Load();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                };
                _stack.AddArrangedSubview(reached);
            }

            // السائق يبلّغ أن العميل لم يحضر
            if (Session.IsDriver && (o.Status == "وصل_السائق" || o.Status == "قيد_التنفيذ"))
            {
                var noShow = Controls.DangerButton("العميل لم يحضر");
                noShow.TouchUpInside += (s, e) =>
                {
                    Ui.Prompt("العميل لم يحضر", "اكتب ما حدث (اختياري)", "", "السبب", reason =>
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
                };
                _stack.AddArrangedSubview(noShow);
            }

            // إكمال الرحلة
            if (o.Status == "أثناء_الرحله" || o.Status == "في_الطريق" || o.Status == "وصلت_الوجهة")
            {
                var done = Controls.PrimaryButton("إكمال الرحلة");
                done.TouchUpInside += (s, e) =>
                {
                    Ui.Confirm("إكمال الرحلة", "تأكيد إكمال الرحلة وتحصيل الأجرة؟", "إكمال", () =>
                    {
                        var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
                        Ui.Run(() => Api.CompleteOrder(o.Id, fare), () =>
                        {
                            Ui.Toast("تم إكمال الرحلة");
                            Load();
                        }, ex => Ui.Toast(ex.Message));
                    });
                };
                _stack.AddArrangedSubview(done);
            }

            // مشاركة الفاتورة بعد الانتهاء (بديل btnShareReceipt → ReceiptActivity)
            if (o.Status == "مكتمل")
            {
                var receipt = Controls.OutlineButton("مشاركة الفاتورة");
                receipt.TouchUpInside += (s, e) => Push(new ReceiptViewController(o.Id));
                _stack.AddArrangedSubview(receipt);
            }

            // إعادة نفس الرحلة (بديل btnReorder للعميل)
            if (!Session.IsDriver &&
                (!string.IsNullOrWhiteSpace(o.FromAddress) || !string.IsNullOrWhiteSpace(o.ToAddress)))
            {
                var reorder = Controls.OutlineButton("إعادة الطلب");
                reorder.TouchUpInside += (s, e) => Push(new NewOrderViewController(o));
                _stack.AddArrangedSubview(reorder);
            }

            // تقييم الطرف الآخر بعد الانتهاء (بديل btnRate)
            if (o.Status == "مكتمل")
            {
                var rateeId = Session.IsDriver ? o.CustomerId : o.DriverId;
                if (rateeId > 0)
                {
                    var rate = Controls.OutlineButton(Session.IsDriver ? "تقييم العميل" : "تقييم السائق");
                    rate.TouchUpInside += (s, e) => Push(new RateViewController(rateeId, o.Id));
                    _stack.AddArrangedSubview(rate);
                }
            }

            // إلغاء الطلب بسبب (بديل btnCancel → Api.CancelOrder)
            if (o.Status != "مكتمل" && o.Status != "ملغي")
            {
                var cancel = Controls.DangerButton("إلغاء الطلب");
                cancel.TouchUpInside += (s, e) =>
                {
                    Ui.Prompt("إلغاء الطلب", "اكتب سبب الإلغاء (اختياري)", "", "السبب", reason =>
                    {
                        var progress = Ui.ShowProgress("جارٍ الإلغاء…");

                        Ui.Run(() => Api.CancelOrder(o.Id, reason ?? ""), () =>
                        {
                            progress.Dismiss();
                            Ui.Toast("تم إلغاء الطلب");
                            Load();
                        }, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    });
                };
                _stack.AddArrangedSubview(cancel);
            }
        }

        private void StartRide(Order o, string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                Ui.Toast("أدخل رمز PIN");
                return;
            }

            Ui.Run(() => Api.StartOrder(o.Id, pin), () =>
            {
                Ui.Toast("بدأت الرحلة");
                Load();
            }, ex => Ui.Toast(ex.Message));
        }
    }
}
