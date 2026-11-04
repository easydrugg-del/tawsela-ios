using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RecurringActivity.cs — الرحلات المتكررة:
    /// عرض، تفعيل/إيقاف، حذف، وإضافة رحلة متكررة جديدة.
    /// </summary>
    public class RecurringViewController : BaseViewController
    {
        private UIStackView _list;
        private UILabel _empty;

        public RecurringViewController() : base("الرحلات المتكررة") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(10f, 16f);

            stack.AddArrangedSubview(MakeLabel(
                "احفظ رحلتك المتكررة (مثل الذهاب للعمل) ليتم تنبيهك بها يوميًا.",
                13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            var add = Controls.PrimaryButton("إضافة رحلة متكررة");
            add.TouchUpInside += (s, e) => AddNew();
            stack.AddArrangedSubview(add);

            _empty = MakeLabel("لا توجد رحلات متكررة", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);

            _list = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_list);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var items = Api.MyRecurring();
                Ui.BeginOnMain(() => Render(items));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(List<Dictionary<string, object>> items)
        {
            for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _list.ArrangedSubviews[i];
                _list.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (items == null || items.Count == 0)
            {
                _list.AddArrangedSubview(_empty);
                return;
            }

            foreach (var r in items)
            {
                var id = Row.Int(r, "Id");
                var from = Row.Str(r, "FromAddr");
                var to = Row.Str(r, "ToAddr");
                var pattern = Row.Str(r, "Pattern");
                var time = Row.Str(r, "TimeOfDay");
                var active = Row.Bool(r, "IsActive");

                var card = Controls.Card();

                var title = MakeLabel(from + " → " + to, 14f, Theme.TextPrimary,
                    UIFontWeight.Medium, UITextAlignment.Right);
                var details = MakeLabel(
                    (string.IsNullOrEmpty(pattern) ? "يوميًا" : pattern) +
                    (string.IsNullOrEmpty(time) ? "" : " · " + time),
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

                var toggle = new UISwitch
                {
                    OnTintColor = Theme.Primary,
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    On = active
                };
                toggle.ValueChanged += (s, e) =>
                {
                    var value = toggle.On;
                    Ui.Run(() => Api.ToggleRecurring(id, value),
                        () => Ui.Toast(value ? "تم التفعيل" : "تم الإيقاف"),
                        ex => { toggle.On = !value; Ui.Toast(ex.Message); });
                };

                var delete = Controls.DangerButton("حذف", 40f);
                delete.TouchUpInside += (s, e) =>
                {
                    Ui.Confirm("حذف الرحلة المتكررة", "هل تريد حذفها؟", "حذف", () =>
                    {
                        Ui.Run(() => Api.DeleteRecurring(id), () =>
                        {
                            Ui.Toast("تم الحذف");
                            Load();
                        }, ex => Ui.Toast(ex.Message));
                    });
                };

                card.AddSubview(title);
                card.AddSubview(details);
                card.AddSubview(toggle);
                card.AddSubview(delete);

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(toggle, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(toggle, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),

                    NSLayoutConstraint.Create(details, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 4),
                    NSLayoutConstraint.Create(details, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(details, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(delete, NSLayoutAttribute.Top, NSLayoutRelation.Equal, details, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(delete, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(delete, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(delete, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                });

                _list.AddArrangedSubview(card);
            }
        }

        /// <summary>إضافة رحلة متكررة: من/إلى + النمط + الوقت.</summary>
        private void AddNew()
        {
            Ui.Prompt("رحلة متكررة", "عنوان الانطلاق", "", "من", fromAddr =>
            {
                if (string.IsNullOrWhiteSpace(fromAddr)) return;

                Ui.Prompt("رحلة متكررة", "عنوان الوجهة", "", "إلى", toAddr =>
                {
                    if (string.IsNullOrWhiteSpace(toAddr)) return;

                    Ui.Choose("التكرار", new[] { "يوميًا", "أسبوعيًا" }, patternIndex =>
                    {
                        var pattern = patternIndex == 0 ? "يومي" : "أسبوعي";

                        Ui.Prompt("وقت الرحلة", "اكتب الوقت مثل 08:00", "08:00", "HH:mm", time =>
                        {
                            if (string.IsNullOrWhiteSpace(time)) return;

                            var progress = Ui.ShowProgress("جارٍ الحفظ…");

                            Ui.Run(() =>
                            {
                                double fLat, fLng, tLat, tLng;
                                if (!Geo.ForwardGeocode(fromAddr, out fLat, out fLng))
                                    throw new Exception("تعذّر تحديد موقع الانطلاق");
                                if (!Geo.ForwardGeocode(toAddr, out tLat, out tLng))
                                    throw new Exception("تعذّر تحديد موقع الوجهة");

                                var service = 0;
                                try
                                {
                                    var list = Api.ServiceTypes();
                                    if (list != null && list.Count > 0) service = list[0].Id;
                                }
                                catch { }

                                Api.AddRecurring(fLat, fLng, fromAddr, tLat, tLng, toAddr,
                                    "", service, pattern, time.Trim());
                            }, () =>
                            {
                                progress.Dismiss();
                                Ui.Toast("تم حفظ الرحلة المتكررة");
                                Load();
                            }, ex =>
                            {
                                progress.Dismiss();
                                Ui.Toast(ex.Message);
                            });
                        });
                    });
                });
            });
        }
    }
}
