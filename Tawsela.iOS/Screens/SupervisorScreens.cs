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
    /// بديل Activities/SupervisorAdminActivity.cs — إدارة المشرفين
    /// (إضافة/تعديل/تفعيل/حذف/ربط سائق بمشرف).
    /// </summary>
    public class SupervisorAdminViewController : ListViewController<Supervisor>
    {
        public SupervisorAdminViewController() : base("مشرفو التكاتك") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "مشرفو التكاتك";

        protected override string EmptyText
        {
            get { return "لا يوجد مشرفون بعد"; }
        }

        protected override List<Supervisor> Fetch()
        {
            return Api.ListSupervisors();
        }

        protected override UIView BuildRow(Supervisor s)
        {
            var row = Controls.InfoRow(s.FullName + (s.IsActive ? "" : " (موقوف)"),
                s.Phone + " · كود: " + s.Code +
                "\nعمولة " + s.CommissionPercent.ToString("0.##") + "%" +
                " · سائقون: " + s.DriversCount +
                " · رحلات: " + s.RidesAll);

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(s.FullName, new[]
                {
                    "تعديل",
                    s.IsActive ? "إيقاف" : "تفعيل",
                    "أرباح المشرف",
                    "ربط سائق",
                    "حذف"
                }, index =>
                {
                    if (index == 0) Edit(s);
                    else if (index == 1) Run(() => Api.ToggleSupervisor(s.Id, !s.IsActive), "تم التحديث");
                    else if (index == 2) Push(new SupervisorEarningsViewController(s));
                    else if (index == 3) AssignDriver(s);
                    else Ui.Confirm("حذف مشرف", "حذف " + s.FullName + "؟", "حذف",
                        () => Run(() => Api.DeleteSupervisor(s.Id), "تم الحذف"));
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("➕ إضافة مشرف");
            add.TouchUpInside += (s, e) => Add();
            View.AddSubview(add);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
        }

        private void Add()
        {
            Ui.Prompt("مشرف جديد", "الاسم الكامل", "", "الاسم", name =>
            {
                Ui.Prompt("مشرف جديد", "الهاتف", "", "01xxxxxxxxx", phone =>
                {
                    Ui.Prompt("مشرف جديد", "نسبة العمولة %", "", "0", percentText =>
                    {
                        decimal percent;
                        if (!decimal.TryParse(percentText, out percent) || percent < 0)
                        {
                            Ui.Toast("نسبة غير صحيحة");
                            return;
                        }

                        var progress = Ui.ShowProgress("جارٍ الإضافة…");
                        Ui.Run(() =>
                        {
                            var code = Api.AddSupervisor(name, phone, percent, "");
                            Ui.BeginOnMain(() =>
                            {
                                progress.Dismiss();
                                Ui.Alert("تمت الإضافة", "كود المشرف: " + (code ?? "—"));
                                Reload();
                            });
                        }, null, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    }, UIKeyboardType.DecimalPad);
                }, UIKeyboardType.PhonePad);
            });
        }

        private void Edit(Supervisor s)
        {
            Ui.Prompt("تعديل الاسم", "الاسم", s.FullName, "الاسم", name =>
            {
                Ui.Prompt("تعديل الهاتف", "الهاتف", s.Phone, "01xxxxxxxxx", phone =>
                {
                    Ui.Prompt("تعديل العمولة", "النسبة %", s.CommissionPercent.ToString("0.##"), "0", percentText =>
                    {
                        decimal percent;
                        if (!decimal.TryParse(percentText, out percent)) percent = s.CommissionPercent;
                        Run(() => Api.EditSupervisor(s.Id, name, phone, percent, s.Notes), "تم التحديث");
                    }, UIKeyboardType.DecimalPad);
                }, UIKeyboardType.PhonePad);
            });
        }

        /// <summary>ربط سائق بمشرف (بديل SetDriverSupervisor).</summary>
        private void AssignDriver(Supervisor s)
        {
            Ui.Run(() =>
            {
                var drivers = Api.AllDrivers();
                Ui.BeginOnMain(() =>
                {
                    if (drivers == null || drivers.Count == 0)
                    {
                        Ui.Toast("لا يوجد سائقون");
                        return;
                    }

                    var names = new List<string>();
                    foreach (var d in drivers) names.Add(d.FullName + " — " + d.Phone);

                    Ui.Choose("اختر السائق", names, index =>
                    {
                        var driver = drivers[index];
                        Run(() => Api.SetDriverSupervisor(driver.UserId, s.Id),
                            "تم ربط " + driver.FullName + " بالمشرف");
                    });
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Run(Action action, string success)
        {
            var progress = Ui.ShowProgress("جارٍ التنفيذ…");
            Ui.Run(action, () =>
            {
                progress.Dismiss();
                Ui.Toast(success);
                Reload();
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }

    /// <summary>
    /// بديل Activities/SupervisorEarningsActivity.cs — أرباح مشرف خلال مدة
    /// مع تسجيل الدفعات (Api.GetSupervisorEarnings / Api.AddSupervisorPayout).
    /// </summary>
    public class SupervisorEarningsViewController : BaseViewController
    {
        private readonly Supervisor _supervisor;
        private UIStackView _stack;
        private DateTime _from, _to;
        private UIButton _btnFrom, _btnTo;

        public SupervisorEarningsViewController(Supervisor supervisor)
            : base("أرباح " + (supervisor == null ? "" : supervisor.FullName))
        {
            _supervisor = supervisor;
            _from = DateTime.Today.AddDays(-30);
            _to = DateTime.Today;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _stack = CreateScrollContent(10f, 16f);

            var week = Controls.OutlineButton("آخر 7 أيام");
            week.TouchUpInside += (s, e) => { _from = DateTime.Today.AddDays(-7); _to = DateTime.Today; Load(); };
            _stack.AddArrangedSubview(week);

            var month = Controls.OutlineButton("آخر 30 يومًا");
            month.TouchUpInside += (s, e) => { _from = DateTime.Today.AddDays(-30); _to = DateTime.Today; Load(); };
            _stack.AddArrangedSubview(month);

            // من/إلى (بديل btnFrom / btnTo في أندرويد)
            _btnFrom = Controls.OutlineButton("من:", null, 38f);
            _btnFrom.TouchUpInside += (s, e) => PickDate(true);
            _stack.AddArrangedSubview(_btnFrom);

            _btnTo = Controls.OutlineButton("إلى:", null, 38f);
            _btnTo.TouchUpInside += (s, e) => PickDate(false);
            _stack.AddArrangedSubview(_btnTo);

            var show = Controls.PrimaryButton("عرض");
            show.TouchUpInside += (s, e) => Load();
            _stack.AddArrangedSubview(show);

            var payout = Controls.PrimaryButton("💸 تسجيل صرف لهذه المدة");
            payout.TouchUpInside += (s, e) => AddPayout();
            _stack.AddArrangedSubview(payout);

            Load();
        }

        private void PickDate(bool isFrom)
        {
            var initial = isFrom ? _from : _to;
            Push(new DatePickViewController(isFrom ? "من تاريخ" : "إلى تاريخ", initial, picked =>
            {
                if (isFrom)
                {
                    _from = picked;
                    _btnFrom.SetTitle("من: " + picked.ToString("dd/MM/yyyy"), UIControlState.Normal);
                }
                else
                {
                    _to = picked;
                    _btnTo.SetTitle("إلى: " + picked.ToString("dd/MM/yyyy"), UIControlState.Normal);
                }

                Load();
            }));
        }

        private void Load()
        {
            if (_supervisor == null) return;
            var from = _from;
            var to = _to;
            var id = _supervisor.Id;

            Ui.Run(() =>
            {
                var earnings = Api.GetSupervisorEarnings(id, from, to);
                Ui.BeginOnMain(() =>
                {
                    while (_stack.ArrangedSubviews.Length > 6)
                    {
                        var v = _stack.ArrangedSubviews[6];
                        _stack.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    _stack.AddArrangedSubview(Controls.InfoRow("الفترة",
                        from.ToString("dd/MM/yyyy") + " — " + to.ToString("dd/MM/yyyy")));

                    if (earnings == null)
                    {
                        _stack.AddArrangedSubview(MakeLabel("لا توجد بيانات", 14f,
                            Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                        return;
                    }

                    _stack.AddArrangedSubview(Controls.InfoRow("عدد الرحلات", earnings.Rides.ToString()));
                    _stack.AddArrangedSubview(Controls.InfoRow("إجمالي الأجرة", earnings.FareTotal.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("العمولة المستحقة", earnings.Commission.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("المدفوع", earnings.Paid.ToString("0.00") + " ج.م"));
                    _stack.AddArrangedSubview(Controls.InfoRow("المتبقي", earnings.Remaining.ToString("0.00") + " ج.م"));

                    if (earnings.Payouts != null && earnings.Payouts.Count > 0)
                    {
                        _stack.AddArrangedSubview(Controls.SectionTitle("سجل الصرف"));
                        foreach (var p in earnings.Payouts)
                        {
                            _stack.AddArrangedSubview(Controls.InfoRow(
                                p.FromDate.ToString("dd/MM") + " → " + p.ToDate.ToString("dd/MM"),
                                p.Amount.ToString("0.00") + " ج.م" +
                                (string.IsNullOrEmpty(p.Note) ? "" : "\n" + p.Note)));
                        }
                    }
                    else
                    {
                        _stack.AddArrangedSubview(Controls.SectionTitle("سجل الصرف"));
                        _stack.AddArrangedSubview(MakeLabel("لا يوجد صرف مسجّل", 13f,
                            Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void AddPayout()
        {
            if (_supervisor == null) return;

            Ui.Prompt("تسجيل دفعة", "المبلغ", "", "0", amountText =>
            {
                decimal amount;
                if (!decimal.TryParse(amountText, out amount) || amount <= 0)
                {
                    Ui.Toast("أدخل مبلغًا صحيحًا");
                    return;
                }

                Ui.Prompt("ملاحظة", "ملاحظة على الدفعة (اختياري)", "", "ملاحظة", note =>
                {
                    var from = _from;
                    var to = _to;
                    var id = _supervisor.Id;

                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                    Ui.Run(() => Api.AddSupervisorPayout(id, from, to, amount, note), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم تسجيل الدفعة");
                        Load();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            }, UIKeyboardType.DecimalPad);
        }
    }

    /// <summary>
    /// بديل Activities/RestaurantDeliveryAdminActivity.cs — مناطق توصيل المطاعم وأسعارها.
    /// </summary>
    public class RestaurantDeliveryAdminViewController : ListViewController<Dictionary<string, object>>
    {
        public RestaurantDeliveryAdminViewController() : base("🏪 توصيلات المطاعم") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "🏪 توصيلات المطاعم";

        protected override string EmptyText
        {
            get { return "لا توجد مناطق توصيل"; }
        }

        protected override string Subtitle
        {
            get { return "مناطق التوصيل وأسعارها · المطاعم والمحلات"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.AllDeliveryAreas();
        }

        protected override UIView BuildRow(Dictionary<string, object> a)
        {
            var id = Row.Int(a, "Id");
            var name = Row.Str(a, "Name");
            var city = Row.Str(a, "City");
            var price = Row.Num(a, "Price");
            var active = Row.Bool(a, "Active") || Row.Bool(a, "IsActive");
            var lat = Row.Dbl(a, "Lat");
            var lng = Row.Dbl(a, "Lng");

            var row = Controls.InfoRow(name + (active ? "" : " (موقوفة)"),
                city + " · " + price.ToString("0.00") + " ج.م");

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(name, new[] { "تعديل السعر", "تحديد الموقع", active ? "إيقاف" : "تفعيل" }, index =>
                {
                    if (index == 0)
                    {
                        Ui.Prompt("سعر التوصيل", "السعر بالجنيه", price.ToString("0.00"), "0", text =>
                        {
                            decimal newPrice;
                            if (!decimal.TryParse(text, out newPrice) || newPrice < 0)
                            {
                                Ui.Toast("سعر غير صحيح");
                                return;
                            }
                            Run(() => Api.SaveDeliveryArea(id, name, city, Row.Str(a, "Governorate"),
                                newPrice, lat, lng, active), "تم التحديث");
                        }, UIKeyboardType.DecimalPad);
                    }
                    else if (index == 1)
                    {
                        Push(MapViewController.Pick(lat, lng, (plat, plng, address) =>
                        {
                            Run(() => Api.SaveDeliveryArea(id, name, city, Row.Str(a, "Governorate"),
                                price, plat, plng, active), "تم تحديث الموقع");
                        }));
                    }
                    else
                    {
                        Run(() => Api.SaveDeliveryArea(id, name, city, Row.Str(a, "Governorate"),
                            price, lat, lng, !active), "تم التحديث");
                    }
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("إضافة منطقة");
            add.TouchUpInside += (s, e) => Add();
            View.AddSubview(add);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });

            // إدارة المطاعم والمحلات (بديل تبويب «＋ إضافة مطعم/محل»)
            var restaurants = Controls.OutlineButton("＋ إضافة مطعم/محل");
            restaurants.TouchUpInside += (s, e) => Push(new RestaurantsAdminViewController());
            View.AddSubview(restaurants);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(restaurants, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(restaurants, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(restaurants, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, add, NSLayoutAttribute.Top, 1, -8),
            });

            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 136, 0);
        }

        private void Add()
        {
            var govs = EgyptPlaces.Governorates();
            Ui.Choose("المحافظة", govs, gi =>
            {
                var gov = govs[gi];
                var cities = EgyptPlaces.CitiesOf(gov);

                Ui.Choose("المدينة", cities, ci =>
                {
                    var city = cities[ci];

                    Ui.Prompt("منطقة جديدة", "الاسم", "", "الاسم", name =>
                    {
                        Ui.Prompt("سعر التوصيل", "السعر بالجنيه", "", "0", priceText =>
                        {
                            decimal price;
                            if (!decimal.TryParse(priceText, out price) || price < 0)
                            {
                                Ui.Toast("سعر غير صحيح");
                                return;
                            }

                            Push(MapViewController.Pick(0, 0, (lat, lng, address) =>
                            {
                                Run(() => Api.SaveDeliveryArea(0, name, city, gov, price, lat, lng, true),
                                    "تمت الإضافة");
                            }));
                        }, UIKeyboardType.DecimalPad);
                    });
                });
            });
        }

        private void Run(Action action, string success)
        {
            var progress = Ui.ShowProgress("جارٍ التنفيذ…");
            Ui.Run(action, () =>
            {
                progress.Dismiss();
                Ui.Toast(success);
                Reload();
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
