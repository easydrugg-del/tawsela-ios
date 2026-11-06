using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/EmergencyListActivity.cs — سجل بلاغات الطوارئ
    /// وإرسال بلاغ جديد (Api.ReportEmergency / Api.AllEmergencies).
    /// </summary>
    public class EmergencyViewController : BaseViewController
    {
        private UIStackView _list;
        private UILabel _empty;

        public EmergencyViewController() : base("الطوارئ") { }

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
                "استخدم زر الطوارئ فقط عند وجود خطر حقيقي. سيتم إبلاغ الإدارة بموقعك.",
                13f, Theme.Red, UIFontWeight.Regular, UITextAlignment.Right));

            var report = Controls.DangerButton("إرسال بلاغ طوارئ");
            report.TouchUpInside += (s, e) => Report();
            stack.AddArrangedSubview(report);

            stack.AddArrangedSubview(Controls.SectionTitle("بلاغات السائقين (الأحدث أولاً)"));
            stack.AddArrangedSubview(Controls.SectionTitle("بلاغاتي السابقة"));

            _empty = MakeLabel("لا توجد بلاغات", 14f, Theme.TextSecondary,
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
                var items = Api.AllEmergencies();
                Ui.BeginOnMain(() => Render(items));
            }, null, ex => AppServices.Log("TawselaEmergency", ex.Message));
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

            foreach (var e in items)
            {
                _list.AddArrangedSubview(Controls.InfoRow(
                    Row.Str(e, "Type"),
                    Row.Str(e, "Address") + "\n" + Row.Date(e, "CreatedAt")));
            }
        }

        private void Report()
        {
            Ui.Run(() =>
            {
                var types = Api.EmergencyTypes();
                Ui.BeginOnMain(() =>
                {
                    if (types == null || types.Count == 0)
                    {
                        Ui.Toast("لا توجد أنواع طوارئ معرّفة");
                        return;
                    }

                    Ui.Choose("نوع الطوارئ", types, index =>
                    {
                        var type = types[index];

                        // نستخدم آخر موقع معروف كعنوان للبلاغ
                        var coord = LocationService.LastKnown;
                        var address = "";
                        if (coord.HasValue)
                        {
                            try { address = Geo.ReverseGeocode(coord.Value.Latitude, coord.Value.Longitude); }
                            catch { }
                        }

                        Ui.Confirm("تأكيد البلاغ",
                            "سيتم إرسال بلاغ: " + type + (string.IsNullOrEmpty(address) ? "" : "\nالموقع: " + address),
                            "إرسال", () =>
                            {
                                Ui.Prompt("تفاصيل البلاغ", "اكتب تفاصيل إضافية (اختياري)", "", "التفاصيل", details =>
                                {
                                    var progress = Ui.ShowProgress("جارٍ إرسال البلاغ…");
                                    Ui.Run(() =>
                                    {
                                        Api.ReportEmergency(type, address);

                                        // تفاصيل إضافية للإدارة (بديل Api.SendEmergencyDetail)
                                        if (!string.IsNullOrWhiteSpace(details))
                                        {
                                            try { Api.SendEmergencyDetail(details.Trim()); } catch { }
                                        }
                                    }, () =>
                                    {
                                        progress.Dismiss();
                                        Ui.Toast("تم إرسال البلاغ — الإدارة في الطريق");
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
            }, null, ex => Ui.Toast(ex.Message));
        }
    }
}
