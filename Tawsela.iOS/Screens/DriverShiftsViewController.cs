using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/DriverShiftsActivity.cs — بدء/إنهاء الوردية وسجلها.
    /// </summary>
    public class DriverShiftsViewController : BaseViewController
    {
        private UIStackView _list;
        private UILabel _state;

        public DriverShiftsViewController() : base("نوبات العمل") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            _state = MakeLabel("", 14f, Theme.TextSecondary, UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_state);

            var start = Controls.PrimaryButton("▶ بدء نوبة");
            start.TouchUpInside += (s, e) => Run(() => Api.StartShift(), "تم بدء النوبة");
            stack.AddArrangedSubview(start);

            var end = Controls.DangerButton("⏹ إنهاء النوبة");
            end.TouchUpInside += (s, e) =>
            {
                Ui.Confirm("إنهاء النوبة", "هل تريد إنهاء النوبة الحالية؟", "إنهاء",
                    () => Run(() => Api.EndShift(), "تم إنهاء النوبة"));
            };
            stack.AddArrangedSubview(end);

            _list = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_list);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var shifts = Api.DriverShifts();
                Ui.BeginOnMain(() =>
                {
                    _state.Text = "الحالة: " + (shifts == null ? 0 : shifts.Count) + " نوبة مسجلة";
                    Render(shifts);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(List<Dictionary<string, object>> shifts)
        {
            for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _list.ArrangedSubviews[i];
                _list.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (shifts == null || shifts.Count == 0)
            {
                _list.AddArrangedSubview(MakeLabel("لا توجد نوبات بعد", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var s in shifts)
            {
                var startAt = Row.Date(s, "StartAt");
                var endAt = Row.Date(s, "EndAt");
                var rides = Row.Int(s, "Rides");
                var earnings = Row.Num(s, "Earnings");

                _list.AddArrangedSubview(Controls.InfoRow(
                    startAt + (string.IsNullOrEmpty(endAt) ? " — جارية" : " → " + endAt),
                    rides + " رحلة · " + earnings.ToString("0.00") + " ج.م"));
            }
        }

        private void Run(Action action, string success)
        {
            var progress = Ui.ShowProgress("جارٍ التنفيذ…");
            Ui.Run(action, () =>
            {
                progress.Dismiss();
                Ui.Toast(success);
                Load();
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
