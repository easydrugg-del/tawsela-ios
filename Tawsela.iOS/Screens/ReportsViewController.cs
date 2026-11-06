using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/HeatmapActivity.cs — خريطة حرارية لطلبات المدينة
    /// (Api.Heatmap) باستخدام نفس محرك Leaflet.
    /// </summary>
    public class HeatmapViewController : BaseViewController
    {
        private UILabel _summary;

        public HeatmapViewController() : base("الخريطة الحرارية للطلبات") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            _summary = MakeLabel("جاري تحميل البيانات...", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_summary);

            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Load();
            stack.AddArrangedSubview(refresh);

            // مفتاح ألوان الخريطة (بديل وسيلة إيضاح HeatmapActivity)
            stack.AddArrangedSubview(Controls.InfoRow("كثافة الطلبات",
                "عالي جداً · عالي · منخفض"));
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var rows = Api.Heatmap();
                Ui.BeginOnMain(() =>
                {
                    if (rows == null || rows.Count == 0)
                    {
                        _summary.Text = "لا توجد بيانات كافية لعرض الخريطة الحرارية";
                        return;
                    }

                    var points = new List<double[]>();
                    double sumLat = 0, sumLng = 0;
                    foreach (var r in rows)
                    {
                        var lat = Row.Dbl(r, "Lat");
                        var lng = Row.Dbl(r, "Lng");
                        if (lat == 0 && lng == 0) continue;
                        points.Add(new[] { lat, lng });
                        sumLat += lat;
                        sumLng += lng;
                    }

                    if (points.Count == 0)
                    {
                        _summary.Text = "لا توجد إحداثيات صالحة";
                        return;
                    }

                    _summary.Text = "عدد النقاط: " + points.Count;
                    Push(new PointsMapViewController("الخريطة الحرارية",
                        sumLat / points.Count, sumLng / points.Count, points, "#D32F2F"));
                });
            }, null, ex => Ui.Toast(ex.Message));
        }
    }

    /// <summary>
    /// بديل Activities/DriverSearchActivity.cs — البحث عن سائق بالاسم أو الهاتف
    /// (Api.SearchDrivers).
    /// </summary>
    public class DriverSearchViewController : BaseViewController
    {
        private UITextField _query;
        private UIStackView _results;

        public DriverSearchViewController() : base("بحث عن سائق") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(10f, 16f);

            _query = Controls.TextField("هاتف أو اسم...");
            _query.ReturnKeyType = UIReturnKeyType.Search;
            _query.EditingDidEndOnExit += (s, e) => Search();
            stack.AddArrangedSubview(MakeLabel(
                "برقم الهاتف أو باسم السائق/صاحب المركبة",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
            stack.AddArrangedSubview(_query);

            var button = Controls.PrimaryButton("بحث");
            button.TouchUpInside += (s, e) => Search();
            stack.AddArrangedSubview(button);

            _results = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_results);
        }

        private void Search()
        {
            var query = (_query.Text ?? "").Trim();
            if (query == "") { Ui.Toast("اكتب كلمة البحث"); return; }

            Ui.Run(() =>
            {
                var list = Api.SearchDrivers(query);
                Ui.BeginOnMain(() => Render(list));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render(List<Driver> list)
        {
            for (int i = _results.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _results.ArrangedSubviews[i];
                _results.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (list == null || list.Count == 0)
            {
                _results.AddArrangedSubview(MakeLabel("لا توجد نتائج", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var d in list)
            {
                var row = Controls.InfoRow(d.FullName,
                    d.Phone + " · " + d.City +
                    "\n" + (d.CarModel + " " + d.CarPlate).Trim() +
                    "\n" + d.StatusDisplay + (d.IsAvailable ? " · متاح" : ""));

                row.UserInteractionEnabled = true;
                row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
                {
                    if (d.Lat == 0 && d.Lng == 0)
                    {
                        Ui.Toast("لا تتوفر إحداثيات لهذا السائق");
                        return;
                    }
                    Push(MapViewController.ShowPoint(d.Lat, d.Lng, d.FullName));
                }));

                _results.AddArrangedSubview(row);
            }
        }
    }

    /// <summary>
    /// بديل Activities/DriverMovementsActivity.cs — مواقع السائقين وحالتهم الآن.
    /// </summary>
    public class DriverMovementsViewController : ListViewController<Driver>
    {
        private UITextField _query;

        public DriverMovementsViewController() : base("🚗 حركات سائق") { }

        protected override string EmptyText
        {
            get { return "لا يوجد سائقون"; }
        }

        protected override List<Driver> Fetch()
        {
            var list = Api.AllDrivers();
            var q = (_query == null ? "" : (_query.Text ?? "")).Trim();
            if (q.Length == 0 || list == null) return list;

            return list.FindAll(d =>
                (!string.IsNullOrEmpty(d.FullName) && d.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(d.Phone) && d.Phone.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(d.CarPlate) && d.CarPlate.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(d.DriverNumber) && d.DriverNumber.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        /// <summary>بحث فوري (بديل txtQuery / btnClearDates في أندرويد).</summary>
        private void BuildSearchBar()
        {
            const float headerHeight = 96f;

            var header = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 6,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(header);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, headerHeight),
            });

            _query = Controls.TextField("رقم الحساب / رقم السيارة / اسم السائق");
            _query.EditingChanged += (s, e) => Reload();
            header.AddArrangedSubview(_query);

            var clear = Controls.OutlineButton("🧹 الكل", null, 36f);
            clear.TouchUpInside += (s, e) => { _query.Text = ""; Reload(); };
            header.AddArrangedSubview(clear);

            List.TopInset = headerHeight + 8;
        }

        protected override UIView BuildRow(Driver d)
        {
            var row = Controls.InfoRow(d.FullName,
                d.Phone + " · " + d.StatusDisplay +
                (d.Lat == 0 && d.Lng == 0
                    ? "\nلا يوجد موقع مسجّل"
                    : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "\n{0:0.0000}, {1:0.0000}", d.Lat, d.Lng)));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                if (d.Lat == 0 && d.Lng == 0)
                {
                    Ui.Toast("لا يوجد موقع مسجّل");
                    return;
                }
                Push(MapViewController.ShowPoint(d.Lat, d.Lng, d.FullName));
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildSearchBar();

            var map = Controls.PrimaryButton("عرض الجميع على الخريطة");
            map.TouchUpInside += (s, e) => ShowAll();
            View.AddSubview(map);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(map, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(map, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(map, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
        }

        private void ShowAll()
        {
            Ui.Run(() =>
            {
                var points = DriverPoints();

                Ui.BeginOnMain(() =>
                {
                    if (points.Count == 0)
                    {
                        Ui.Toast("لا توجد مواقع مسجّلة");
                        return;
                    }

                    double sumLat = 0, sumLng = 0;
                    foreach (var p in points) { sumLat += p[0]; sumLng += p[1]; }

                    // متابعة مباشرة: نفس نقاط DriverMapActivity (تحديث تلقائي كل 15 ثانية)
                    Push(new PointsMapViewController("تحركات السائقين",
                        sumLat / points.Count, sumLng / points.Count, points, "#1976D2",
                        DriverPoints));
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        /// <summary>إحداثيات كل السائقين الذين لديهم موقع مسجّل.</summary>
        private static List<double[]> DriverPoints()
        {
            var points = new List<double[]>();
            var drivers = Api.AllDrivers();
            if (drivers == null) return points;

            foreach (var d in drivers)
            {
                if (d.Lat == 0 && d.Lng == 0) continue;
                points.Add(new[] { d.Lat, d.Lng });
            }
            return points;
        }
    }

    /// <summary>
    /// بديل Activities/DriverEarningsReportActivity.cs — تقرير أرباح نصي
    /// (Api.EarningsReport).
    /// </summary>
    public class DriverEarningsReportViewController : BaseViewController
    {
        private UILabel _report;

        public DriverEarningsReportViewController() : base("تقرير الأرباح") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load(7);
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            var share = Controls.OutlineButton("📤 مشاركة التقرير");
            share.TouchUpInside += (s, e) => ShareReport();
            stack.AddArrangedSubview(share);

            var week = Controls.OutlineButton("آخر 7 أيام");
            week.TouchUpInside += (s, e) => Load(7);
            stack.AddArrangedSubview(week);

            var month = Controls.OutlineButton("آخر 30 يومًا");
            month.TouchUpInside += (s, e) => Load(30);
            stack.AddArrangedSubview(month);

            _report = MakeLabel("جارٍ التحميل…", 13f, Theme.TextPrimary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_report);
        }

        private void Load(int days)
        {
            Ui.Run(() =>
            {
                var text = Api.EarningsReport(days);
                Ui.BeginOnMain(() => _report.Text = string.IsNullOrEmpty(text) ? "لا توجد بيانات" : text);
            }, null, ex => Ui.Toast(ex.Message));
        }

        /// <summary>مشاركة نص التقرير (بديل Intent.ActionSend في أندرويد).</summary>
        private void ShareReport()
        {
            var text = _report == null ? "" : (_report.Text ?? "");
            if (string.IsNullOrWhiteSpace(text) || text.IndexOf("جارٍ", StringComparison.Ordinal) == 0)
            {
                Ui.Toast("لا يوجد تقرير للمشاركة");
                return;
            }

            var share = new UIActivityViewController(
                new Foundation.NSObject[] { new Foundation.NSString(text) }, null);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                share.PopoverPresentationController != null)
            {
                share.PopoverPresentationController.SourceView = View;
                share.PopoverPresentationController.SourceRect =
                    new CoreGraphics.CGRect(View.Bounds.Width / 2, View.Bounds.Height / 2, 1, 1);
            }

            PresentViewController(share, true, null);
        }
    }

    /// <summary>
    /// بديل Activities/CustomerStatsActivity.cs — إحصائيات العميل واستبدال النقاط.
    /// </summary>
    public class CustomerStatsViewController : BaseViewController
    {
        private UIStackView _stack;
        private UILabel _points;

        public CustomerStatsViewController() : base("إحصائياتي") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _stack = CreateScrollContent(10f, 16f);

            _points = MakeLabel("نقاط الولاء: —", 16f, Theme.Primary,
                UIFontWeight.Bold, UITextAlignment.Right);
            _stack.AddArrangedSubview(_points);

            var redeem = Controls.OutlineButton("استبدال 100 نقطة = 10ج");
            redeem.TouchUpInside += (s, e) => Redeem();
            _stack.AddArrangedSubview(redeem);

            Load();
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var stats = Api.CustomerFullStats();
                var points = Api.LoyaltyPoints();

                Ui.BeginOnMain(() =>
                {
                    while (_stack.ArrangedSubviews.Length > 2)
                    {
                        var v = _stack.ArrangedSubviews[2];
                        _stack.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    _points.Text = "نقاط الولاء: " + points;

                    if (stats == null || stats.Count == 0)
                    {
                        _stack.AddArrangedSubview(MakeLabel("لا توجد إحصائيات بعد", 14f,
                            Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                        return;
                    }

                    foreach (var kv in stats)
                    {
                        var value = kv.Value == null ? "—" : kv.Value.ToString();
                        _stack.AddArrangedSubview(Controls.InfoRow(Translate(kv.Key), value));
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private static string Translate(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "total": return "إجمالي الرحلات";
                case "done": return "رحلات مكتملة";
                case "cancel": return "رحلات ملغاة";
                case "spent": return "إجمالي المصروف";
                case "saved": return "إجمالي الموفَّر";
                case "avg": return "متوسط الأجرة";
                default: return key;
            }
        }

        private void Redeem()
        {
            Ui.Prompt("استبدال النقاط", "عدد النقاط", "", "0", text =>
            {
                int points;
                if (!int.TryParse(text, out points) || points <= 0)
                {
                    Ui.Toast("أدخل عددًا صحيحًا");
                    return;
                }

                var progress = Ui.ShowProgress("جارٍ الاستبدال…");
                Ui.Run(() => Api.RedeemPoints(points), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم استبدال النقاط");
                    Load();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            }, UIKeyboardType.NumberPad);
        }
    }

    /// <summary>
    /// بديل Activities/TripReportActivity.cs — تقرير رحلات بين تاريخين.
    /// </summary>
    public class TripReportViewController : BaseViewController
    {
        private UIStackView _list;
        private UILabel _summary;
        private DateTime _from = DateTime.Today.AddDays(-7);
        private DateTime _to = DateTime.Today;

        public TripReportViewController() : base("تقرير الرحلات") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load(DateTime.Today.AddDays(-7), DateTime.Today);
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(10f, 16f);

            stack.AddArrangedSubview(MakeLabel("التاريخ حسب:", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));

            var week = Controls.OutlineButton("آخر 7 أيام");
            week.TouchUpInside += (s, e) => Load(DateTime.Today.AddDays(-7), DateTime.Today);
            stack.AddArrangedSubview(week);

            var month = Controls.OutlineButton("آخر 30 يومًا");
            month.TouchUpInside += (s, e) => Load(DateTime.Today.AddDays(-30), DateTime.Today);
            stack.AddArrangedSubview(month);

            var custom = Controls.OutlineButton("تحديد تاريخين");
            custom.TouchUpInside += (s, e) => PickRange();
            stack.AddArrangedSubview(custom);

            var show = Controls.PrimaryButton("عرض الرحلات");
            show.TouchUpInside += (s, e) => Load(_from, _to);
            stack.AddArrangedSubview(show);

            _summary = MakeLabel("اختر الفترة ثم اضغط عرض الرحلات", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_summary);

            _list = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_list);
        }

        private void PickRange()
        {
            Ui.Prompt("من تاريخ", "dd/MM/yyyy", DateTime.Today.AddDays(-30).ToString("dd/MM/yyyy"), "dd/MM/yyyy", fromText =>
            {
                DateTime from;
                if (!DateTime.TryParse(fromText, out from))
                {
                    Ui.Toast("تاريخ غير صحيح");
                    return;
                }

                Ui.Prompt("إلى تاريخ", "dd/MM/yyyy", DateTime.Today.ToString("dd/MM/yyyy"), "dd/MM/yyyy", toText =>
                {
                    DateTime to;
                    if (!DateTime.TryParse(toText, out to))
                    {
                        Ui.Toast("تاريخ غير صحيح");
                        return;
                    }

                    Load(from, to);
                });
            });
        }

        private void Load(DateTime from, DateTime to)
        {
            _from = from;
            _to = to;

            Ui.Run(() =>
            {
                Dictionary<int, decimal> fees;
                var orders = Api.TripReport(from, to, false, out fees);

                Ui.BeginOnMain(() =>
                {
                    for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
                    {
                        var v = _list.ArrangedSubviews[i];
                        _list.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    if (orders == null || orders.Count == 0)
                    {
                        _summary.Text = "لا توجد رحلات في هذه الفترة";
                        return;
                    }

                    decimal total = 0;
                    foreach (var o in orders) total += o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;

                    _summary.Text = "عدد الرحلات: " + orders.Count +
                                    " · الإجمالي: " + total.ToString("0.00") + " ج.م" +
                                    " · من " + from.ToString("dd/MM/yyyy") + " إلى " + to.ToString("dd/MM/yyyy");

                    foreach (var o in orders)
                    {
                        var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
                        _list.AddArrangedSubview(Controls.InfoRow(
                            o.OrderNumber + " · " + (o.CreatedAt == default(DateTime) ? "" : o.CreatedAt.ToString("dd/MM HH:mm")),
                            o.FromAddress + " → " + o.ToAddress + "\n" +
                            fare.ToString("0.00") + " ج.م · " + o.StatusText));
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }
    }
}
