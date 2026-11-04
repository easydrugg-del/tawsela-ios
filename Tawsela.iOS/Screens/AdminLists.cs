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
    /// بديل Activities/AreasAdminActivity.cs — إدارة المناطق.
    /// </summary>
    public class AreasAdminViewController : ListViewController<Area>
    {
        public AreasAdminViewController() : base("إدارة المناطق") { }

        protected override string EmptyText
        {
            get { return "لا توجد مناطق مسجلة"; }
        }

        protected override List<Area> Fetch()
        {
            return Api.AllAreas();
        }

        protected override UIView BuildRow(Area a)
        {
            var row = Controls.InfoRow(a.Name + (a.IsActive ? "" : " (معطّلة)"),
                a.Governorate + " · " + a.City + "\n" +
                string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0000}, {1:0.0000}", a.Lat, a.Lng));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(a.Name, new[] { "تعديل الموقع", "حذف" }, index =>
                {
                    if (index == 0) Edit(a);
                    else Delete(a);
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
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
        }

        private void Add()
        {
            PickPlace((gov, city) =>
            {
                Ui.Prompt("منطقة جديدة", "اسم المنطقة", "", "الاسم", name =>
                {
                    if (string.IsNullOrWhiteSpace(name)) return;

                    Push(MapViewController.Pick(0, 0, (lat, lng, address) =>
                    {
                        Run(() => Api.AddArea(name, city, gov, lat, lng, true), "تمت إضافة المنطقة");
                    }));
                });
            });
        }

        private void Edit(Area a)
        {
            Push(MapViewController.Pick(a.Lat, a.Lng, (lat, lng, address) =>
            {
                Run(() => Api.UpdateArea(a.Id, a.Name, a.City, a.Governorate, lat, lng, a.IsActive), "تم تحديث المنطقة");
            }));
        }

        private void Delete(Area a)
        {
            Ui.Confirm("حذف منطقة", "حذف " + a.Name + "؟", "حذف",
                () => Run(() => Api.DeleteArea(a.Id), "تم الحذف"));
        }

        private void PickPlace(Action<string, string> done)
        {
            var govs = EgyptPlaces.Governorates();
            Ui.Choose("المحافظة", govs, gi =>
            {
                var gov = govs[gi];
                var cities = EgyptPlaces.CitiesOf(gov);
                Ui.Choose("المدينة", cities, ci => done(gov, cities[ci]));
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

    /// <summary>
    /// بديل Activities/StreetsAdminActivity.cs — إدارة الشوارع.
    /// </summary>
    public class StreetsAdminViewController : ListViewController<Street>
    {
        public StreetsAdminViewController() : base("إدارة الشوارع") { }

        protected override string EmptyText
        {
            get { return "لا توجد شوارع مسجلة"; }
        }

        protected override List<Street> Fetch()
        {
            return Api.AllStreets();
        }

        protected override UIView BuildRow(Street s)
        {
            var row = Controls.InfoRow(s.Name + (s.IsActive ? "" : " (معطّل)"),
                s.Governorate + " · " + s.City + "\n" +
                string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0000}, {1:0.0000}", s.Lat, s.Lng));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(s.Name, new[] { "تعديل الموقع", "حذف" }, index =>
                {
                    if (index == 0)
                    {
                        Push(MapViewController.Pick(s.Lat, s.Lng, (lat, lng, address) =>
                        {
                            Run(() => Api.UpdateStreet(s.Id, s.Name, s.City, s.Governorate, lat, lng, s.IsActive),
                                "تم تحديث الشارع");
                        }));
                    }
                    else
                    {
                        Ui.Confirm("حذف شارع", "حذف " + s.Name + "؟", "حذف",
                            () => Run(() => Api.DeleteStreet(s.Id), "تم الحذف"));
                    }
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("إضافة شارع");
            add.TouchUpInside += (s, e) =>
            {
                var govs = EgyptPlaces.Governorates();
                Ui.Choose("المحافظة", govs, gi =>
                {
                    var gov = govs[gi];
                    var cities = EgyptPlaces.CitiesOf(gov);
                    Ui.Choose("المدينة", cities, ci =>
                    {
                        var city = cities[ci];
                        Ui.Prompt("شارع جديد", "اسم الشارع", "", "الاسم", name =>
                        {
                            if (string.IsNullOrWhiteSpace(name)) return;
                            Push(MapViewController.Pick(0, 0, (lat, lng, address) =>
                            {
                                Run(() => Api.AddStreet(name, city, gov, lat, lng, true), "تمت إضافة الشارع");
                            }));
                        });
                    });
                });
            };
            View.AddSubview(add);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
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
    /// بديل Activities/CouponAdminActivity.cs — إدارة كوبونات الخصم.
    /// </summary>
    public class CouponsAdminViewController : ListViewController<Coupon>
    {
        public CouponsAdminViewController() : base("إدارة الكوبونات") { }

        protected override string EmptyText
        {
            get { return "لا توجد كوبونات"; }
        }

        protected override List<Coupon> Fetch()
        {
            return Api.ListCoupons();
        }

        protected override UIView BuildRow(Coupon c)
        {
            var row = Controls.InfoRow(c.Code,
                (c.Type == "percent" ? c.Value.ToString("0.#") + "%" : c.Value.ToString("0.00") + " ج.م") +
                (c.Active ? " · نشط" : " · موقوف") +
                (c.ExpiresAt == default(DateTime) ? "" : " · ينتهي " + c.ExpiresAt.ToString("dd/MM/yyyy")));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(c.Code, new[] { "تعديل", "حذف" }, index =>
                {
                    if (index == 0) { Edit(c); return; }

                    Ui.Confirm("حذف كوبون", "حذف الكوبون " + c.Code + "؟", "حذف", () =>
                    {
                        var progress = Ui.ShowProgress("جارٍ الحذف…");
                        Ui.Run(() => Api.DeleteCoupon(c.Id), () =>
                        {
                            progress.Dismiss();
                            Ui.Toast("تم الحذف");
                            Reload();
                        }, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    });
                });
            }));

            return row;
        }

        /// <summary>تعديل كوبون قائم (بديل Api.EditCoupon).</summary>
        private void Edit(Coupon c)
        {
            Ui.Prompt("تعديل الكوبون", "الكود", c.Code, "CODE", code =>
            {
                if (string.IsNullOrWhiteSpace(code)) return;

                var options = new[] { "نسبة %", "مبلغ ثابت" };
                Ui.Choose("نوع الخصم", options, typeIndex =>
                {
                    var type = typeIndex == 0 ? "percent" : "fixed";

                    Ui.Prompt("قيمة الخصم", type == "percent" ? "النسبة %" : "المبلغ",
                        c.Value.ToString("0.##"), "0", valueText =>
                        {
                            decimal value;
                            if (!decimal.TryParse(valueText, out value) || value <= 0)
                            {
                                Ui.Toast("قيمة غير صحيحة");
                                return;
                            }

                            Ui.Prompt("أقصى خصم", "0 = بدون حد", c.MaxOff.ToString("0.##"), "0", maxText =>
                            {
                                decimal maxOff;
                                if (!decimal.TryParse(maxText, out maxOff) || maxOff < 0) maxOff = 0;

                                Ui.Prompt("تاريخ الانتهاء", "dd/MM/yyyy — اتركه فارغًا لبدون انتهاء",
                                    c.ExpiresAt == default(DateTime) ? "" : c.ExpiresAt.ToString("dd/MM/yyyy"),
                                    "dd/MM/yyyy", dateText =>
                                    {
                                        DateTime? expires = null;
                                        if (!string.IsNullOrWhiteSpace(dateText))
                                        {
                                            DateTime parsed;
                                            if (!DateTime.TryParse(dateText, out parsed))
                                            {
                                                Ui.Toast("تاريخ غير صحيح");
                                                return;
                                            }
                                            expires = parsed;
                                        }

                                        var progress = Ui.ShowProgress("جارٍ الحفظ…");
                                        Ui.Run(() => Api.EditCoupon(c.Id, code.Trim(), type, value, maxOff, expires), () =>
                                        {
                                            progress.Dismiss();
                                            Ui.Toast("تم حفظ التعديلات");
                                            Reload();
                                        }, ex =>
                                        {
                                            progress.Dismiss();
                                            Ui.Toast(ex.Message);
                                        });
                                    });
                            }, UIKeyboardType.DecimalPad);
                        }, UIKeyboardType.DecimalPad);
                });
            });
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("إضافة كوبون");
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
            Ui.Prompt("كوبون جديد", "الكود", "", "CODE", code =>
            {
                if (string.IsNullOrWhiteSpace(code)) return;

                Ui.Choose("نوع الخصم", new[] { "نسبة %", "مبلغ ثابت" }, typeIndex =>
                {
                    var type = typeIndex == 0 ? "percent" : "fixed";

                    Ui.Prompt("قيمة الخصم", type == "percent" ? "النسبة %" : "المبلغ", "", "0", valueText =>
                    {
                        decimal value;
                        if (!decimal.TryParse(valueText, out value) || value <= 0)
                        {
                            Ui.Toast("قيمة غير صحيحة");
                            return;
                        }

                        var progress = Ui.ShowProgress("جارٍ الحفظ…");
                        Ui.Run(() => Api.AddCoupon(code.Trim(), type, value, 0m, null), () =>
                        {
                            progress.Dismiss();
                            Ui.Toast("تمت إضافة الكوبون");
                            Reload();
                        }, ex =>
                        {
                            progress.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    }, UIKeyboardType.DecimalPad);
                });
            });
        }
    }

    /// <summary>
    /// بديل Activities/EmergencyTypesAdminActivity.cs — إدارة أنواع الطوارئ.
    /// </summary>
    public class EmergencyTypesAdminViewController : ListViewController<Dictionary<string, object>>
    {
        public EmergencyTypesAdminViewController() : base("أنواع الطوارئ") { }

        protected override string EmptyText
        {
            get { return "لا توجد أنواع طوارئ"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.AllEmergencyTypes();
        }

        protected override UIView BuildRow(Dictionary<string, object> t)
        {
            var id = Row.Int(t, "Id");
            var title = Row.Str(t, "Title");

            var row = Controls.InfoRow(title, "");
            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Confirm("حذف نوع", "حذف " + title + "؟", "حذف", () =>
                {
                    var progress = Ui.ShowProgress("جارٍ الحذف…");
                    Ui.Run(() => Api.DeleteEmergencyType(id), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم الحذف");
                        Reload();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("إضافة نوع");
            add.TouchUpInside += (s, e) =>
            {
                Ui.Prompt("نوع طوارئ جديد", "الاسم", "", "الاسم", title =>
                {
                    if (string.IsNullOrWhiteSpace(title)) return;
                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                    Ui.Run(() => Api.AddEmergencyType(title.Trim()), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تمت الإضافة");
                        Reload();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            };
            View.AddSubview(add);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
        }
    }
}
