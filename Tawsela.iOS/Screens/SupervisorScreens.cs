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

        public override bool ShowNavigationBar => false;

        protected override bool FlatHeader => true;

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
    /// بديل Activities/RestaurantDeliveryAdminActivity.cs — نفس شاشة أندرويد:
    /// «＋ إضافة منطقة تسعير» + «مناطق التوصيل وأسعارها» (الاسم — السعر ج.م + تعديل)
    /// ثم «＋ إضافة مطعم/محل» + «المطاعم والمحلات» (الاسم — المنطقة (حساب/بدون حساب) + حساب + تعديل).
    /// هذه التسعيرة خاصة بتوصيل المطاعم والمحلات، منفصلة تمامًا عن مناطق العملاء (Api.Areas).
    /// </summary>
    public class RestaurantDeliveryAdminViewController : BaseViewController
    {
        private readonly List<Dictionary<string, object>> _areas = new List<Dictionary<string, object>>();
        private readonly List<Dictionary<string, object>> _restaurants = new List<Dictionary<string, object>>();

        private UIStackView _areasBox;
        private UIStackView _restaurantsBox;

        public RestaurantDeliveryAdminViewController() : base("🏪 توصيلات المطاعم") { }

        /// <summary>الهيدر مسطح مثل باقي شاشات الإدارة (بدون شريط تنقل).</summary>
        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        /// <summary>شكل أندرويد: شريط عنوان مسطح فوق المحتوى.</summary>
        protected override bool FlatHeader
        {
            get { return true; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        /* ---------- بناء الشاشة ---------- */

        private void BuildUi()
        {
            var scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AlwaysBounceVertical = true
            };
            View.AddSubview(scroll);

            var content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            scroll.AddSubview(content);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(content, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -20),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, 14),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, -14),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, -28),
            });

            // ＋ إضافة منطقة تسعير
            var addArea = Controls.PrimaryButton("＋ إضافة منطقة تسعير", 46f);
            addArea.TouchUpInside += (s, e) => AddArea();
            content.AddArrangedSubview(addArea);

            content.AddArrangedSubview(Controls.SectionTitle("مناطق التوصيل وأسعارها"));

            _areasBox = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            content.AddArrangedSubview(_areasBox);

            // ＋ إضافة مطعم/محل
            var addRestaurant = Controls.OutlineButton("＋ إضافة مطعم/محل", null, 44f);
            addRestaurant.TouchUpInside += (s, e) => AddRestaurant();
            content.AddArrangedSubview(addRestaurant);

            content.AddArrangedSubview(Controls.SectionTitle("المطاعم والمحلات"));

            _restaurantsBox = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            content.AddArrangedSubview(_restaurantsBox);
        }

        /* ---------- تحميل ---------- */

        private void Load()
        {
            var progress = Ui.ShowProgress("جارٍ التحميل…");
            Ui.Run(() =>
            {
                var areas = Api.AllDeliveryAreas();
                var restaurants = Api.AllRestaurants();
                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();
                    _areas.Clear();
                    _areas.AddRange(areas);
                    _restaurants.Clear();
                    _restaurants.AddRange(restaurants);
                    RenderAreas();
                    RenderRestaurants();
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast("تعذر التحميل: " + ex.Message);
            });
        }

        /* ---------- مناطق التوصيل وأسعارها ---------- */

        private void RenderAreas()
        {
            Clear(_areasBox);

            if (_areas.Count == 0)
            {
                _areasBox.AddArrangedSubview(MakeLabel("لا توجد مناطق بعد", 13f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var a in _areas)
            {
                var name = Row.Str(a, "Name");
                var price = Row.Num(a, "Price");

                var card = Controls.Card();
                var label = MakeLabel(name + " — " + price.ToString("0.##") + " ج.م", 15f,
                    Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Right);
                label.Lines = 0;

                var edit = SmallButton("تعديل", () => EditArea(a));

                card.AddSubview(label);
                card.AddSubview(edit);
                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, edit, NSLayoutAttribute.Leading, 1, -8),

                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -10),
                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 72),
                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),
                });

                _areasBox.AddArrangedSubview(card);
            }
        }

        /// <summary>إضافة منطقة تسعير (نفس حقول أندرويد: الاسم + المدينة + السعر الثابت).</summary>
        private void AddArea()
        {
            Ui.Prompt("إضافة منطقة", "اسم المنطقة", "", "اسم المنطقة", name =>
            {
                if (string.IsNullOrWhiteSpace(name)) { Ui.Toast("أدخل اسم المنطقة"); return; }

                Ui.Prompt("إضافة منطقة", "المدينة", "", "المدينة", city =>
                {
                    Ui.Prompt("إضافة منطقة", "السعر الثابت", "", "0", priceText =>
                    {
                        decimal price;
                        if (!decimal.TryParse(priceText, out price) || price < 0)
                        {
                            Ui.Toast("أدخل سعراً صحيحاً");
                            return;
                        }

                        Save(() => Api.SaveDeliveryArea(0, name.Trim(), (city ?? "").Trim(), "", price, 0, 0, true),
                            "تم حفظ المنطقة");
                    }, UIKeyboardType.DecimalPad);
                });
            });
        }

        /// <summary>تعديل منطقة: الاسم + المدينة + السعر (مع الحفاظ على الموقع الحالي).</summary>
        private void EditArea(Dictionary<string, object> a)
        {
            var id = Row.Int(a, "Id");
            var name = Row.Str(a, "Name");
            var city = Row.Str(a, "City");
            var governorate = Row.Str(a, "Governorate");
            var price = Row.Num(a, "Price");
            var lat = Row.Dbl(a, "Lat");
            var lng = Row.Dbl(a, "Lng");
            var active = Row.Bool(a, "Active") || Row.Bool(a, "IsActive");

            Ui.Prompt("تعديل منطقة", "اسم المنطقة", name, "اسم المنطقة", newName =>
            {
                if (string.IsNullOrWhiteSpace(newName)) { Ui.Toast("أدخل اسم المنطقة"); return; }

                Ui.Prompt("تعديل منطقة", "المدينة", city, "المدينة", newCity =>
                {
                    Ui.Prompt("تعديل منطقة", "السعر الثابت", price.ToString("0.##"), "0", priceText =>
                    {
                        decimal newPrice;
                        if (!decimal.TryParse(priceText, out newPrice) || newPrice < 0)
                        {
                            Ui.Toast("أدخل سعراً صحيحاً");
                            return;
                        }

                        Save(() => Api.SaveDeliveryArea(id, newName.Trim(), (newCity ?? "").Trim(),
                            governorate, newPrice, lat, lng, active), "تم حفظ المنطقة");
                    }, UIKeyboardType.DecimalPad);
                });
            });
        }

        /* ---------- المطاعم والمحلات ---------- */

        private void RenderRestaurants()
        {
            Clear(_restaurantsBox);

            if (_restaurants.Count == 0)
            {
                _restaurantsBox.AddArrangedSubview(MakeLabel("لا توجد مطاعم بعد", 13f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var r in _restaurants)
            {
                var name = Row.Str(r, "Name");
                var areaName = Row.Str(r, "AreaName");
                var owner = Row.Int(r, "OwnerUserId");

                var text = name + " — " + (string.IsNullOrEmpty(areaName) ? "بدون منطقة" : areaName) +
                           (owner > 0 ? "  (حساب: " + owner + ")" : "  (بدون حساب)");

                var card = Controls.Card();
                var label = MakeLabel(text, 14f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Right);
                label.Lines = 0;

                var account = SmallButton("حساب", () => ShowAccountDialog(r));
                var edit = SmallButton("تعديل", () => EditRestaurant(r));

                card.AddSubview(label);
                card.AddSubview(account);
                card.AddSubview(edit);
                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, account, NSLayoutAttribute.Leading, 1, -8),

                    NSLayoutConstraint.Create(account, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(account, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 66),
                    NSLayoutConstraint.Create(account, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),
                    NSLayoutConstraint.Create(account, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, edit, NSLayoutAttribute.Leading, 1, -6),

                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 66),
                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),
                    NSLayoutConstraint.Create(edit, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -10),
                });

                _restaurantsBox.AddArrangedSubview(card);
            }
        }

        private void AddRestaurant()
        {
            Ui.Prompt("إضافة مطعم", "اسم المطعم/المحل", "", "الاسم", name =>
            {
                if (string.IsNullOrWhiteSpace(name)) { Ui.Toast("أدخل اسم المطعم"); return; }

                Ui.Prompt("إضافة مطعم", "رقم الهاتف", "", "01xxxxxxxxx", phone =>
                {
                    Ui.Prompt("إضافة مطعم", "العنوان", "", "العنوان", address =>
                    {
                        Ui.Prompt("إضافة مطعم", "رقم حساب المطعم (OwnerUserId)", "", "0", ownerText =>
                        {
                            int owner;
                            int.TryParse((ownerText ?? "").Trim(), out owner);

                            PickArea(0, areaId =>
                            {
                                Save(() => Api.SaveRestaurant(0, name.Trim(), owner, (phone ?? "").Trim(),
                                    (address ?? "").Trim(), areaId, 0, 0, true), "تم حفظ المطعم");
                            });
                        }, UIKeyboardType.NumberPad);
                    });
                }, UIKeyboardType.PhonePad);
            });
        }

        private void EditRestaurant(Dictionary<string, object> r)
        {
            var id = Row.Int(r, "Id");
            var name = Row.Str(r, "Name");
            var phone = Row.Str(r, "Phone");
            var address = Row.Str(r, "Address");
            var owner = Row.Int(r, "OwnerUserId");
            var areaId = Row.Int(r, "AreaId");
            var lat = Row.Dbl(r, "Lat");
            var lng = Row.Dbl(r, "Lng");
            var active = Row.Bool(r, "IsActive") || !r.ContainsKey("IsActive");

            Ui.Prompt("تعديل مطعم", "اسم المطعم/المحل", name, "الاسم", newName =>
            {
                Ui.Prompt("تعديل مطعم", "رقم الهاتف", phone, "01xxxxxxxxx", newPhone =>
                {
                    Ui.Prompt("تعديل مطعم", "العنوان", address, "العنوان", newAddress =>
                    {
                        Ui.Prompt("تعديل مطعم", "رقم حساب المطعم (OwnerUserId)",
                            owner > 0 ? owner.ToString() : "", "0", ownerText =>
                        {
                            int newOwner;
                            int.TryParse((ownerText ?? "").Trim(), out newOwner);

                            PickArea(areaId, newAreaId =>
                            {
                                Save(() => Api.SaveRestaurant(id, newName.Trim(), newOwner,
                                    (newPhone ?? "").Trim(), (newAddress ?? "").Trim(),
                                    newAreaId, lat, lng, active), "تم حفظ المطعم");
                            });
                        }, UIKeyboardType.NumberPad);
                    });
                }, UIKeyboardType.PhonePad);
            });
        }

        /// <summary>اختيار منطقة تسعير للمطعم (أول عنصر: بدون منطقة) — بديل Spinner في أندرويد.</summary>
        private void PickArea(int currentId, Action<int> onPick)
        {
            var names = new List<string> { currentId == 0 ? "✓ بدون منطقة" : "بدون منطقة" };
            var ids = new List<int> { 0 };

            for (int i = 0; i < _areas.Count; i++)
            {
                var id = Row.Int(_areas[i], "Id");
                names.Add(Row.Str(_areas[i], "Name") + " (" +
                          Row.Num(_areas[i], "Price").ToString("0.##") + " ج.م)" +
                          (id == currentId ? " ✓" : ""));
                ids.Add(id);
            }

            Ui.Choose("منطقة التوصيل", names, index =>
            {
                if (onPick != null) onPick(ids[index]);
            });
        }

        /// <summary>إنشاء حساب دخول للمطعم (بديل ShowAccountDialog في أندرويد).</summary>
        private void ShowAccountDialog(Dictionary<string, object> r)
        {
            var id = Row.Int(r, "Id");
            var name = Row.Str(r, "Name");
            var phone = Row.Str(r, "Phone");

            Ui.Prompt("حساب دخول المطعم", "اسم المستخدم", "", "restaurant", userName =>
            {
                if (string.IsNullOrWhiteSpace(userName)) { Ui.Toast("أكمل جميع الحقول"); return; }

                Ui.Prompt("حساب دخول المطعم", "كلمة المرور", "", "••••••", password =>
                {
                    if ((password ?? "").Length < 6) { Ui.Toast("كلمة المرور 6 أحرف على الأقل"); return; }

                    Ui.Prompt("حساب دخول المطعم", "اسم المطعم الظاهر", name, "الاسم", fullName =>
                    {
                        Ui.Prompt("حساب دخول المطعم", "رقم الهاتف", phone, "01xxxxxxxxx", accountPhone =>
                        {
                            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(accountPhone))
                            {
                                Ui.Toast("أكمل جميع الحقول");
                                return;
                            }

                            Save(() => Api.CreateRestaurantAccount(id, userName.Trim(), password,
                                fullName.Trim(), accountPhone.Trim()), "تم إنشاء حساب المطعم");
                        }, UIKeyboardType.PhonePad);
                    });
                });
            });
        }

        /* ---------- أدوات ---------- */

        private UIButton SmallButton(string title, Action onTap)
        {
            var b = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            b.SetTitle(title, UIControlState.Normal);
            b.SetTitleColor(Theme.Primary, UIControlState.Normal);
            b.TitleLabel.Font = Theme.FontBold(14);
            // منع كسر العنوان على سطرين داخل الأزرار الصغيرة (تعديل/حساب)
            b.TitleLabel.Lines = 1;
            b.TitleLabel.LineBreakMode = UILineBreakMode.Clip;
            b.TitleLabel.AdjustsFontSizeToFitWidth = true;
            b.TitleLabel.MinimumScaleFactor = 0.75f;
            b.ContentEdgeInsets = new UIEdgeInsets(0, 6, 0, 6);
            b.BackgroundColor = Theme.Surface;
            b.Layer.CornerRadius = 8f;
            b.Layer.BorderWidth = 1f;
            b.Layer.BorderColor = Theme.CardStroke.CGColor;
            b.TouchUpInside += (s, e) => { if (onTap != null) onTap(); };
            return b;
        }

        private void Save(Action action, string success)
        {
            var progress = Ui.ShowProgress("جاري الحفظ...");
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

        private static void Clear(UIStackView box)
        {
            if (box == null) return;
            foreach (var v in box.ArrangedSubviews)
            {
                box.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }
        }
    }
}
