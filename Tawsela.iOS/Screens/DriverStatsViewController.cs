using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/DriverStatsActivity.cs — إحصائيات السائق،
    /// الرصيد، حالة الاستقبال، والسحب.
    /// </summary>
    public class DriverStatsViewController : BaseViewController
    {
        private UIStackView _stack;
        private UILabel _availabilityState;
        private UISwitch _availability;

        public DriverStatsViewController() : base("إحصائياتي") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _stack = CreateScrollContent(12f, 16f);

            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(refresh);

            var shifts = Controls.OutlineButton("وردياتي");
            shifts.TouchUpInside += (s, e) => Push(new DriverShiftsViewController());
            _stack.AddArrangedSubview(shifts);

            var statement = Controls.OutlineButton("كشف حسابي");
            statement.TouchUpInside += (s, e) => Push(new DriverStatementViewController());
            _stack.AddArrangedSubview(statement);

            var docs = Controls.OutlineButton("مستنداتي");
            docs.TouchUpInside += (s, e) => Push(new DriverDocsViewController());
            _stack.AddArrangedSubview(docs);

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
                    while (_stack.ArrangedSubviews.Length > 4)
                    {
                        var v = _stack.ArrangedSubviews[4];
                        _stack.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    _stack.AddArrangedSubview(Controls.InfoRow("إجمالي الرحلات", stats.TotalRides.ToString()));
                    _stack.AddArrangedSubview(Controls.InfoRow("رحلات اليوم", stats.TodayRides.ToString()));
                    _stack.AddArrangedSubview(Controls.InfoRow("أرباح اليوم", stats.TodayEarnings.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("إجمالي الأرباح", stats.TotalEarnings.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("التقييم", stats.RatingText));
                    _stack.AddArrangedSubview(Controls.InfoRow("رصيد المحفظة", balance.ToString("0.00") + " ج.م"));

                    // مفتاح الاستقبال
                    var card = Controls.Card();
                    _availabilityState = MakeLabel(available ? "متاح لاستقبال الطلبات" : "غير متاح",
                        14f, available ? Theme.Green : Theme.TextSecondary, UIFontWeight.Medium, UITextAlignment.Right);
                    _availability = new UISwitch
                    {
                        OnTintColor = Theme.Green,
                        TranslatesAutoresizingMaskIntoConstraints = false,
                        On = available
                    };
                    _availability.ValueChanged += (s, e) => SetAvailability(_availability.On);

                    card.AddSubview(_availabilityState);
                    card.AddSubview(_availability);
                    card.AddConstraints(new[]
                    {
                        NSLayoutConstraint.Create(_availability, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                        NSLayoutConstraint.Create(_availability, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                        NSLayoutConstraint.Create(_availabilityState, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                        NSLayoutConstraint.Create(_availabilityState, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _availability, NSLayoutAttribute.Trailing, 1, 10),
                        NSLayoutConstraint.Create(_availabilityState, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                        NSLayoutConstraint.Create(card, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 56),
                    });
                    _stack.AddArrangedSubview(card);

                    var withdraw = Controls.PrimaryButton("سحب الأرباح");
                    withdraw.TouchUpInside += (s, e) => Withdraw();
                    _stack.AddArrangedSubview(withdraw);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void SetAvailability(bool available)
        {
            var coord = LocationService.LastKnown;
            var lat = coord.HasValue ? coord.Value.Latitude : 0;
            var lng = coord.HasValue ? coord.Value.Longitude : 0;

            Ui.Run(() => Api.SetAvailability(available, lat, lng), () =>
            {
                _availabilityState.Text = available ? "متاح لاستقبال الطلبات" : "غير متاح";
                _availabilityState.TextColor = available ? Theme.Green : Theme.TextSecondary;
                Ui.Toast(available ? "تم تفعيل استقبال الطلبات" : "تم إيقاف استقبال الطلبات");
            }, ex =>
            {
                _availability.On = !available;
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
