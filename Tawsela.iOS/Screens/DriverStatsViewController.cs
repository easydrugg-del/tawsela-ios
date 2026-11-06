using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/DriverStatsActivity.cs بنفس ترتيب ونصوص أندرويد:
    /// لوحة السائق، أرباح اليوم/الأسبوع، الرحلات، التقييم، الاستقبال،
    /// التوصيلات المكتملة، الرصيد، السحب، واشتراك VIP.
    /// </summary>
    public class DriverStatsViewController : BaseViewController
    {
        private UIStackView _stack;
        private UIButton _btnAvailability;
        private bool _available;
        private UITextField _dest;

        public DriverStatsViewController() : base("لوحة السائق") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _stack = CreateScrollContent(12f, 16f);

            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(refresh);

            var shifts = Controls.OutlineButton("نوبات العمل");
            shifts.TouchUpInside += (s, e) => Push(new DriverShiftsViewController());
            _stack.AddArrangedSubview(shifts);

            var statement = Controls.OutlineButton("كشف الحساب");
            statement.TouchUpInside += (s, e) => Push(new DriverStatementViewController());
            _stack.AddArrangedSubview(statement);

            var docs = Controls.OutlineButton("إعادة إرسال الوثائق");
            docs.TouchUpInside += (s, e) => Push(new DriverDocsViewController());
            _stack.AddArrangedSubview(docs);

            // وجهة السائق (بديل txtDest / btnDest في activity_driver_stats)
            _stack.AddArrangedSubview(MakeLabel("وجهتي اليومية", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));

            _dest = Controls.TextField("وجهتي (مثال: المعادي)");
            _dest.Text = AppServices.Prefs.Get("tawsela.driver_dest", "");
            _stack.AddArrangedSubview(_dest);

            var saveDest = Controls.PrimaryButton("حفظ");
            saveDest.TouchUpInside += (s, e) =>
            {
                AppServices.Prefs.Set("tawsela.driver_dest", (_dest.Text ?? "").Trim());
                Ui.Toast("تم حفظ وجهتك");
            };
            _stack.AddArrangedSubview(saveDest);

            Load();
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var stats = Api.DriverStats();
                var balance = Api.WalletBalance();

                string status;
                bool available;
                Api.GetDriverStatus(out status, out available);

                // تحديث موقع السائق الحالي لخدمة التتبع (بديل Api.UpdateDriverLocation)
                var coord = LocationService.LastKnown;
                if (coord.HasValue)
                {
                    try { Api.UpdateDriverLocation(coord.Value.Latitude, coord.Value.Longitude); } catch { }
                }

                Ui.BeginOnMain(() =>
                {
                    while (_stack.ArrangedSubviews.Length > 7)
                    {
                        var v = _stack.ArrangedSubviews[7];
                        _stack.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    _available = available;

                    _stack.AddArrangedSubview(Controls.SectionTitle("لوحة السائق"));

                    _stack.AddArrangedSubview(Controls.InfoRow("أرباح اليوم",
                        stats.TodayEarnings.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("أرباح الأسبوع",
                        stats.TotalEarnings.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("إجمالي الرحلات",
                        stats.TotalRides.ToString()));
                    _stack.AddArrangedSubview(Controls.InfoRow("متوسط التقييم", stats.RatingText));
                    _stack.AddArrangedSubview(Controls.InfoRow("مكتملة", stats.TotalRides.ToString()));

                    // زر الاستقبال (بديل btnDriverAvailability)
                    _btnAvailability = Controls.PrimaryButton(AvailabilityTitle());
                    _btnAvailability.BackgroundColor = available ? Theme.Green : Theme.Gray;
                    _btnAvailability.TouchUpInside += (s, e) => SetAvailability(!_available);
                    _stack.AddArrangedSubview(_btnAvailability);

                    // التوصيلات المكتملة (بديل btnDriverOrders)
                    var completed = Controls.OutlineButton("التوصيلات المكتملة");
                    completed.TouchUpInside += (s, e) =>
                        Push(new OrdersViewController("driver", "مكتمل", "التوصيلات المكتملة"));
                    _stack.AddArrangedSubview(completed);

                    _stack.AddArrangedSubview(Controls.InfoRow("الرصيد المتاح للسحب:",
                        balance.ToString("0.00") + " ج.م"));

                    var withdraw = Controls.PrimaryButton("سحب الأرباح");
                    withdraw.TouchUpInside += (s, e) => Withdraw();
                    _stack.AddArrangedSubview(withdraw);

                    // اشتراك VIP (بديل btnDriverSubscription)
                    var sub = Controls.OutlineButton("اشتراك VIP");
                    sub.TouchUpInside += (s, e) => Push(new SubscriptionViewController());
                    _stack.AddArrangedSubview(sub);

                    // بونص اليوم (نفس منطق أندرويد)
                    if (stats.TodayRides >= 10)
                    {
                        _stack.AddArrangedSubview(MakeLabel(
                            "🎉 بونص اليوم: أكملت " + stats.TodayRides + "/10 - بونص 50ج مستحق!",
                            13f, Theme.Green, UIFontWeight.Semibold, UITextAlignment.Right));
                    }
                    else if (stats.TodayRides >= 5)
                    {
                        _stack.AddArrangedSubview(MakeLabel(
                            "متبقي " + (10 - stats.TodayRides) + " رحلات للبونص 50ج",
                            13f, Theme.Accent, UIFontWeight.Medium, UITextAlignment.Right));
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private string AvailabilityTitle()
        {
            return _available ? "متاح لاستقبال التوصيلات ✓" : "متاح لاستقبال التوصيلات";
        }

        private void SetAvailability(bool available)
        {
            var coord = LocationService.LastKnown;
            var lat = coord.HasValue ? coord.Value.Latitude : 0;
            var lng = coord.HasValue ? coord.Value.Longitude : 0;

            if (available && lat == 0 && lng == 0)
            {
                LocationService.RequestPermission(granted =>
                {
                    if (!granted)
                    {
                        Ui.Toast("لازم تسمح بالوصول للموقع لتستقبل الطلبات");
                        return;
                    }
                    DoSetAvailability(true);
                });
                return;
            }

            DoSetAvailability(available);
        }

        private void DoSetAvailability(bool available)
        {
            var coord = LocationService.LastKnown;
            var lat = coord.HasValue ? coord.Value.Latitude : 0;
            var lng = coord.HasValue ? coord.Value.Longitude : 0;

            Ui.Run(() => Api.SetAvailability(available, lat, lng), () =>
            {
                _available = available;
                if (_btnAvailability != null)
                {
                    _btnAvailability.SetTitle(AvailabilityTitle(), UIControlState.Normal);
                    _btnAvailability.BackgroundColor = available ? Theme.Green : Theme.Gray;
                }
                Ui.Toast(available ? "أصبحت متاحاً لاستقبال التوصيلات" : "أصبحت غير متصل");
            }, ex =>
            {
                Ui.Toast(ex.Message);
            });
        }

        private void Withdraw()
        {
            Ui.Prompt("سحب الأرباح", "المبلغ المراد سحبه", "", "المبلغ", text =>
            {
                decimal amount;
                if (!decimal.TryParse(text, out amount) || amount <= 0)
                {
                    Ui.Toast("أدخل مبلغًا صحيحًا");
                    return;
                }

                var progress = Ui.ShowProgress("جارٍ تنفيذ السحب…");
                Ui.Run(() => Api.Withdraw(amount), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم إرسال طلب السحب");
                    Load();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            }, UIKeyboardType.DecimalPad);
        }
    }
}
