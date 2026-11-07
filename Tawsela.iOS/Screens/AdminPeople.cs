using System;
using System.Collections.Generic;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/DriverAdminActivity.cs + BlockedDriversActivity:
    /// إدارة السائقين (تفعيل/إيقاف/حظر/توفّر).
    /// </summary>
    public class DriversAdminViewController : ListViewController<Driver>
    {
        private UITextField _search;

        public DriversAdminViewController() : base("إدارة السائقين") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "إدارة السائقين";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _search = AddSearchBar("بحث بالاسم أو رقم السيارة...");
        }

        protected override string EmptyText
        {
            get { return "لا يوجد سائقون"; }
        }

        protected override List<Driver> Fetch()
        {
            var list = Api.AllDrivers();

            var q = (_search == null ? "" : (_search.Text ?? "")).Trim();
            if (q.Length == 0 || list == null) return list;

            return list.FindAll(d =>
                (!string.IsNullOrEmpty(d.FullName) && d.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(d.Phone) && d.Phone.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(d.CarPlate) && d.CarPlate.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        protected override UIView BuildRow(Driver d)
        {
            var row = Controls.InfoRow(d.FullName,
                d.Phone + " · " + d.City +
                "\n" + (d.CarModel + " " + d.CarPlate).Trim() +
                "\n" + (d.IsActive ? "نشط" : "موقوف") + (d.IsAvailable ? " · متاح" : ""));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(d.FullName, new[]
                {
                    d.IsActive ? "إيقاف الحساب" : "تفعيل الحساب",
                    d.IsAvailable ? "إلغاء التوفّر" : "تعيين متاح",
                    "تغيير الحالة",
                    "تغيير المحافظة/المدينة",
                    "حظر",
                    "إلغاء الحظر"
                }, index =>
                {
                    if (index == 0)
                        Run(() => Api.SetDriverActive(d.UserId, !d.IsActive), "تم التحديث");
                    else if (index == 1)
                        Run(() => Api.SetDriverAvailAdmin(d.UserId, !d.IsAvailable), "تم التحديث");
                    else if (index == 2)
                        ChangeStatus(d);
                    else if (index == 3)
                        ChangeLocation(d.UserId);
                    else if (index == 4)
                        Ui.Confirm("حظر السائق", "حظر " + d.FullName + "؟", "حظر",
                            () => Run(() => Api.BlockDriver(d.UserId), "تم الحظر"));
                    else
                        Run(() => Api.UnblockDriver(d.UserId), "تم إلغاء الحظر");
                });
            }));

            return row;
        }

        /// <summary>تغيير حالة السائق يدويًا (بديل Api.SetDriverStatus).</summary>
        private void ChangeStatus(Driver d)
        {
            // نفس قيم أندرويد: نشط / موقوف / مرفوض (DriverAdminActivity)
            var options = new[] { "نشط", "موقوف", "مرفوض" };
            Ui.Choose("حالة السائق", options, index =>
            {
                var status = options[index];
                Run(() => Api.SetDriverStatus(d.UserId, status), "تم تغيير الحالة إلى " + status);
            });
        }

        /// <summary>تغيير محافظة/مدينة المستخدم (بديل Api.SetUserLocation).</summary>
        private void ChangeLocation(int userId)
        {
            var govs = EgyptPlaces.Governorates();
            Ui.Choose("المحافظة", govs, gi =>
            {
                var gov = govs[gi];
                var cities = EgyptPlaces.CitiesOf(gov);

                Ui.Choose("المدينة", cities, ci =>
                {
                    Run(() => Api.SetUserLocation(userId, gov, cities[ci]), "تم تحديث الموقع");
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

    /// <summary>
    /// بديل Activities/CustomerAdminActivity.cs — إدارة العملاء.
    /// </summary>
    public class CustomersAdminViewController : ListViewController<Customer>
    {
        public CustomersAdminViewController() : base("إدارة العملاء") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "إدارة العملاء";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // زر «العملاء المحذوفون» — للأدمن المميّز فقط (بديل Session.IsAhmedAli)
            if (!Session.IsAhmedAli) return;

            var deleted = Controls.OutlineButton("العملاء المحذوفون");
            deleted.TouchUpInside += (s, e) => Push(new DeletedCustomersAdminViewController());
            View.AddSubview(deleted);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(deleted, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(deleted, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(deleted, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });

            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
        }

        protected override string EmptyText
        {
            get { return "لا يوجد عملاء"; }
        }

        protected override List<Customer> Fetch()
        {
            return Api.AllCustomers();
        }

        protected override UIView BuildRow(Customer c)
        {
            var row = Controls.InfoRow(c.FullName,
                c.Phone + " · " + c.City + (c.IsActive ? "" : " · موقوف"));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                // نفس خيارات أندرويد: الموقع / الحظر / الحذف النهائي
                Ui.Choose(c.FullName + " — " + c.Phone,
                    new[]
                    {
                        "تعديل المحافظة والمدينة",
                        c.IsActive ? "حظر العميل" : "فك الحظر",
                        "حذف نهائي"
                    },
                    index =>
                    {
                        if (index == 0)
                        {
                            var govs = EgyptPlaces.Governorates();
                            Ui.Choose("المحافظة", govs, gi =>
                            {
                                var gov = govs[gi];
                                var cities = EgyptPlaces.CitiesOf(gov);

                                Ui.Choose("المدينة", cities, ci =>
                                {
                                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                                    Ui.Run(() => Api.SetUserLocation(c.UserId, gov, cities[ci]), () =>
                                    {
                                        progress.Dismiss();
                                        Ui.Toast("تم تحديث الموقع");
                                        Reload();
                                    }, ex =>
                                    {
                                        progress.Dismiss();
                                        Ui.Toast(ex.Message);
                                    });
                                });
                            });
                            return;
                        }

                        if (index == 1)
                        {
                            var progress = Ui.ShowProgress("جارٍ التحديث…");
                            Ui.Run(() => Api.SetDriverActive(c.UserId, !c.IsActive), () =>
                            {
                                progress.Dismiss();
                                Ui.Toast("تم التحديث");
                                Reload();
                            }, ex =>
                            {
                                progress.Dismiss();
                                Ui.Toast(ex.Message);
                            });
                            return;
                        }

                        Ui.Confirm("حذف عميل", "حذف حساب " + c.FullName + "؟", "حذف", () =>
                        {
                            var progress = Ui.ShowProgress("جارٍ الحذف…");
                            Ui.Run(() => Api.DeleteCustomer(c.UserId), () =>
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
    }

    /// <summary>
    /// بديل Activities/DeletedCustomersActivity.cs — سجل العملاء المحذوفين.
    /// </summary>
    public class DeletedCustomersAdminViewController : ListViewController<DeletedCustomer>
    {
        public DeletedCustomersAdminViewController() : base("العملاء المحذوفون") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "العملاء المحذوفون";

        public override void ViewDidLoad()
        {
            // نفس حماية أندرويد: الشاشة للأدمن المميّز فقط
            if (!Session.IsAhmedAli)
            {
                Ui.Toast("ليس لديك صلاحية لهذه الشاشة");
                Ui.BeginOnMain(Back);
                return;
            }

            base.ViewDidLoad();
        }

        protected override string EmptyText
        {
            get { return "لا يوجد محذوفون بعد"; }
        }

        protected override string Subtitle
        {
            get { return "أرشيف بيانات العملاء المحذوفين ومن حذفها ومتى"; }
        }

        protected override List<DeletedCustomer> Fetch()
        {
            return Api.AllDeletedCustomers();
        }

        protected override UIView BuildRow(DeletedCustomer c)
        {
            return Controls.InfoRow(c.FullName,
                c.Phone + " · " + c.City +
                (string.IsNullOrEmpty(c.DeletedAt) ? "" : "\nحُذف: " + c.DeletedAt) +
                (string.IsNullOrEmpty(c.DeletedBy) ? "" : " بواسطة " + c.DeletedBy));
        }
    }

    /// <summary>
    /// بديل Activities/AdminDocsActivity.cs — مراجعة مستندات السائقين.
    /// </summary>
    public class DocsAdminViewController : ListViewController<DriverDoc>
    {
        private UITextField _search;

        public DocsAdminViewController() : base("مراجعة وثائق السائقين") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "مراجعة وثائق السائقين";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _search = AddSearchBar("بحث بالاسم أو رقم السيارة...");
        }

        protected override string EmptyText
        {
            get { return "لا توجد مستندات للمراجعة"; }
        }

        protected override List<DriverDoc> Fetch()
        {
            var list = Api.AllDocuments();

            var q = (_search == null ? "" : (_search.Text ?? "")).Trim();
            if (q.Length == 0 || list == null) return list;

            return list.FindAll(d =>
                (!string.IsNullOrEmpty(d.FullName) && d.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(d.CarPlate) && d.CarPlate.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        protected override UIView BuildRow(DriverDoc d)
        {
            var row = Controls.InfoRow(d.FullName + " — " + d.DocType,
                d.DocInfo + "\n" + d.Status +
                (string.IsNullOrEmpty(d.CarPlate) ? "" : "\n" + d.CarModel + " " + d.CarPlate));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(d.FullName + " — " + d.DocType,
                    new[] { "قبول", "رفض", "قبول كل مستندات السائق", "عرض الصورة" }, index =>
                    {
                        if (index == 0)
                            SetStatus(d.Id, "مقبول");
                        else if (index == 1)
                            SetStatus(d.Id, "مرفوض");
                        else if (index == 2)
                            ApproveAll(d);
                        else
                            ShowImage(d);
                    });
            }));

            return row;
        }

        private void SetStatus(int id, string status)
        {
            var progress = Ui.ShowProgress("جارٍ التحديث…");
            Ui.Run(() => Api.SetDocStatus(id, status), () =>
            {
                progress.Dismiss();
                Ui.Toast("تم التحديث: " + status);
                Reload();
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        /// <summary>قبول كل مستندات سائق واحد (بديل Api.SetAllDriverDocsStatus).</summary>
        private void ApproveAll(DriverDoc d)
        {
            Ui.Confirm("قبول كل المستندات", "قبول كل مستندات " + d.FullName + "؟", "قبول الكل", () =>
            {
                var progress = Ui.ShowProgress("جارٍ التحديث…");
                Ui.Run(() => Api.SetAllDriverDocsStatus(d.UserId, "مقبول"), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم قبول كل مستندات السائق");
                    Reload();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            });
        }

        private void ShowImage(DriverDoc d)
        {
            var data = d.DocImage ?? "";
            if (string.IsNullOrEmpty(data))
            {
                Ui.Toast("لا توجد صورة");
                return;
            }

            try
            {
                if (data.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    Legal.Open(data, err => Ui.Toast(err));
                    return;
                }

                var bytes = Convert.FromBase64String(data);
                var image = UIImage.LoadFromData(Foundation.NSData.FromArray(bytes));
                if (image == null)
                {
                    Ui.Toast("تعذّر عرض الصورة");
                    return;
                }

                var controller = new ImageViewController(image);
                Push(controller);
            }
            catch
            {
                Ui.Toast("تعذّر عرض الصورة");
            }
        }
    }

    /// <summary>عرض صورة مستند بملء الشاشة مع إمكانية الإغلاق.</summary>
    public class ImageViewController : BaseViewController
    {
        private readonly UIImage _image;

        public ImageViewController(UIImage image) : base("المستند")
        {
            _image = image;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = UIColor.Black;

            var view = new UIImageView(_image)
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(view);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });
        }
    }

    /// <summary>
    /// بديل Activities/AdminComplaintsActivity.cs — الرد على الشكاوى.
    /// </summary>
    public class ComplaintsAdminViewController : ListViewController<Complaint>
    {
        public ComplaintsAdminViewController() : base("إدارة الشكاوى") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "إدارة الشكاوى";

        protected override string EmptyText
        {
            get { return "لا توجد شكاوى"; }
        }

        protected override List<Complaint> Fetch()
        {
            return Api.AllComplaints();
        }

        protected override UIView BuildRow(Complaint c)
        {
            var row = Controls.InfoRow(c.Subject,
                (string.IsNullOrEmpty(c.UserName) ? "" : c.UserName + " · ") + c.Status +
                "\n" + c.Body +
                (string.IsNullOrEmpty(c.Reply) ? "" : "\nالرد: " + c.Reply));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Prompt("الرد على الشكوى", c.Subject, c.Reply, "الرد", reply =>
                {
                    if (string.IsNullOrWhiteSpace(reply)) return;

                    var progress = Ui.ShowProgress("جارٍ الإرسال…");
                    Ui.Run(() => Api.ReplyComplaint(c.Id, reply, "تم_الرد"), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم إرسال الرد");
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
    }

    /// <summary>
    /// بديل Activities/AdminSubscriptionActivity.cs — إدارة اشتراكات السائقين.
    /// </summary>
    public class SubscriptionsAdminViewController : ListViewController<Subscription>
    {
        private UITextField _search;
        private List<Subscription> _rows;

        public SubscriptionsAdminViewController() : base("إدارة الاشتراكات") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "إدارة الاشتراكات";

        protected override string EmptyText
        {
            get { return "لا توجد اشتراكات"; }
        }

        protected override string Subtitle
        {
            get
            {
                if (_rows == null) return "إجمالي الإيراد: —";

                decimal total = 0;
                var active = 0;
                foreach (var s in _rows)
                {
                    total += s.Price;
                    if (s.IsActive) active++;
                }
                return "إجمالي الإيراد: " + total.ToString("0.00") + " ج.م · اشتراك نشط: " + active;
            }
        }

        protected override List<Subscription> Fetch()
        {
            _rows = Api.AllSubscriptions();

            var q = (_search == null ? "" : (_search.Text ?? "")).Trim();
            if (q.Length == 0 || _rows == null) return _rows;

            return _rows.FindAll(s =>
                (!string.IsNullOrEmpty(s.FullName) && s.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(s.Phone) && s.Phone.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        protected override UIView BuildRow(Subscription s)
        {
            return Controls.InfoRow(s.FullName + " — " + s.Plan,
                s.Phone + " · " + s.Price.ToString("0.00") + " ج.م" +
                "\n" + s.StatusDisplay +
                (s.ExpiresAt == default(DateTime) ? "" : " · ينتهي " + s.ExpiresAt.ToString("dd/MM/yyyy")));
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // بحث باسم العميل (بديل txtSearch في أندرويد)
            const float headerHeight = 60f;
            var header = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
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

            _search = Controls.TextField("بحث باسم العميل...");
            _search.EditingChanged += (s, e) => Reload();
            header.AddArrangedSubview(_search);

            List.TopInset = headerHeight + 8;

            var edit = Controls.OutlineButton("تعديل الأسعار والخصومات");
            edit.TouchUpInside += (s, e) => EditSettings();
            View.AddSubview(edit);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(edit, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(edit, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(edit, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -74),
            });

            var grant = Controls.PrimaryButton("منح اشتراك لعميل");
            grant.TouchUpInside += (s, e) => Grant();
            View.AddSubview(grant);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(grant, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(grant, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(grant, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 148, 0);
        }

        /// <summary>تعديل سعر الاشتراك ونسبة الخصم (بديل ShowSettingsDialog في أندرويد).</summary>
        private void EditSettings()
        {
            Ui.Run(() =>
            {
                var settings = Api.AdminSettings();
                var price = settings.ContainsKey("subPrice") ? Row.Num(settings["subPrice"]) : 99m;
                var disc = settings.ContainsKey("subDiscount") ? Row.Int(settings["subDiscount"]) : 15;

                Ui.BeginOnMain(() =>
                {
                    Ui.Prompt("السعر الشهري (ج.م)", "السعر الشهري (ج.م)",
                        price.ToString("0.##"), "99", priceText =>
                    {
                        Ui.Prompt("نسبة الخصم (%)", "نسبة الخصم (%)",
                            disc.ToString(), "15", discText =>
                        {
                            decimal p;
                            int d;
                            if (!decimal.TryParse((priceText ?? "").Trim(), out p)) p = 99;
                            if (!int.TryParse((discText ?? "").Trim(), out d)) d = 15;

                            var progress = Ui.ShowProgress("جارٍ الحفظ…");
                            Ui.Run(() => Api.AdminSaveSettings(new Dictionary<string, string>
                            {
                                { "SubscriptionPrice", p.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                                { "SubscriptionDiscount", d.ToString() }
                            }), () =>
                            {
                                progress.Dismiss();
                                Ui.Toast("تم الحفظ");
                                Reload();
                            }, ex =>
                            {
                                progress.Dismiss();
                                Ui.Toast(ex.Message);
                            });
                        }, UIKeyboardType.NumberPad);
                    }, UIKeyboardType.DecimalPad);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        /// <summary>منح اشتراك يدوي لعميل (بديل Api.AdminSubscribe).</summary>
        private void Grant()
        {
            Ui.Run(() =>
            {
                var customers = Api.AllCustomers() ?? new List<Customer>();
                Ui.BeginOnMain(() =>
                {
                    if (customers.Count == 0)
                    {
                        Ui.Toast("لا يوجد عملاء");
                        return;
                    }

                    var names = new List<string>();
                    foreach (var c in customers) names.Add(c.FullName + " — " + c.Phone);

                    Ui.Choose("اختر العميل", names, index =>
                    {
                        var userId = customers[index].UserId;

                        Ui.Prompt("الخطة", "اسم الخطة (شهري/سنوي…)", "شهري", "شهري", plan =>
                        {
                            Ui.Prompt("السعر", "سعر الاشتراك (ج.م)", "", "0", priceText =>
                            {
                                decimal price;
                                if (!decimal.TryParse(priceText, out price) || price < 0)
                                {
                                    Ui.Toast("سعر غير صحيح");
                                    return;
                                }

                                var progress = Ui.ShowProgress("جارٍ الحفظ…");
                                Ui.Run(() => Api.AdminSubscribe(userId, plan, price), () =>
                                {
                                    progress.Dismiss();
                                    Ui.Toast("تم منح الاشتراك");
                                    Reload();
                                }, ex =>
                                {
                                    progress.Dismiss();
                                    Ui.Toast(ex.Message);
                                });
                            }, UIKeyboardType.DecimalPad);
                        });
                    });
                });
            }, null, ex => Ui.Toast(ex.Message));
        }
    }

    /// <summary>
    /// بديل Activities/TopUpAdminActivity.cs — مراجعة طلبات شحن المحفظة.
    /// </summary>
    public class TopUpsAdminViewController : ListViewController<Dictionary<string, object>>
    {
        public TopUpsAdminViewController() : base("طلبات شحن العملاء") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "طلبات شحن العملاء";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // تعديل رقم الدفع الرسمي (بديل AdminSaveSettings { PaymentPhone })
            var editPhone = Controls.OutlineButton("رقم هاتف الدفع (يحوّل عليه العملاء)");
            editPhone.TouchUpInside += (s, e) => EditPaymentPhone();
            View.AddSubview(editPhone);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(editPhone, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(editPhone, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(editPhone, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });

            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);
        }

        /// <summary>قراءة/حفظ PaymentPhone في إعدادات الإدارة (بديل TopUpAdminActivity).</summary>
        private void EditPaymentPhone()
        {
            var progress = Ui.ShowProgress("جارٍ التحميل…");

            Ui.Run(() =>
            {
                var settings = Api.AdminSettings();
                var current = settings != null && settings.ContainsKey("PaymentPhone")
                    ? settings["PaymentPhone"]
                    : "";

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    Ui.Prompt("رقم هاتف الدفع (يحوّل عليه العملاء)", "الرقم الذي يحوّل عليه العملاء", current, "01xxxxxxxxx", phone =>
                    {
                        var saving = Ui.ShowProgress("جارٍ الحفظ…");

                        Ui.Run(() => Api.AdminSaveSettings(new Dictionary<string, string>
                        {
                            { "PaymentPhone", (phone ?? "").Trim() }
                        }), () =>
                        {
                            saving.Dismiss();
                            Ui.Toast("تم حفظ رقم الدفع");
                        }, ex =>
                        {
                            saving.Dismiss();
                            Ui.Toast(ex.Message);
                        });
                    }, UIKeyboardType.PhonePad);
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        protected override string EmptyText
        {
            get { return "لا توجد طلبات معلقة"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.AllTopUps();
        }

        protected override UIView BuildRow(Dictionary<string, object> t)
        {
            var id = Row.Int(t, "Id");
            var amount = Row.Num(t, "Amount");
            var status = Row.Str(t, "Status");
            var sender = Row.Str(t, "SenderPhone");
            var reference = Row.Str(t, "ReceiptRef");
            var hasReceipt = !string.IsNullOrWhiteSpace(Row.Str(t, "ReceiptImage"));

            // نفس صف أندرويد: المبلغ + المُحوِّل + رقم العملية + علامة وجود إيصال
            var subtitle = "من: " + sender +
                           (string.IsNullOrEmpty(reference) ? "" : " | عملية: " + reference) +
                           (hasReceipt ? " | 📎 إيصال" : "") +
                           "\n" + (string.IsNullOrEmpty(status) ? "قيد المراجعة" : status);

            var row = Controls.InfoRow(amount.ToString("0.00") + " ج.م", subtitle);

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                // شاشة تفاصيل تعرض صورة الإيصال (بديل ImageView في TopUpAdminActivity)
                Push(new TopUpDetailViewController(t, Reload));
            }));

            return row;
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
    /// تفاصيل طلب شحن للمراجعة: البيانات + صورة الإيصال + تأكيد/رفض
    /// (بديل TopUpAdminActivity الذي يعرض ImageView للإيصال).
    /// </summary>
    public class TopUpDetailViewController : BaseViewController
    {
        private readonly Dictionary<string, object> _row;
        private readonly Action _onChanged;

        public TopUpDetailViewController(Dictionary<string, object> row, Action onChanged)
            : base("تفاصيل طلب الشحن")
        {
            _row = row ?? new Dictionary<string, object>();
            _onChanged = onChanged;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var stack = CreateScrollContent(12f, 16f);

            var amount = Row.Num(_row, "Amount");
            var status = Row.Str(_row, "Status");

            stack.AddArrangedSubview(Controls.SectionTitle("المبلغ"));
            stack.AddArrangedSubview(MakeLabel(amount.ToString("0.00") + " ج.م",
                26f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right));

            stack.AddArrangedSubview(Controls.SectionTitle("بيانات التحويل"));
            stack.AddArrangedSubview(InfoRow("المُحوِّل", Row.Str(_row, "SenderPhone")));
            stack.AddArrangedSubview(InfoRow("رقم العملية", Row.Str(_row, "ReceiptRef")));
            stack.AddArrangedSubview(InfoRow("الحالة", string.IsNullOrEmpty(status) ? "قيد المراجعة" : status));

            stack.AddArrangedSubview(InfoRow("التاريخ", Row.Date(_row, "CreatedAt")));

            /* ---------- صورة الإيصال ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("صورة الإيصال"));

            var image = Decode(Row.Str(_row, "ReceiptImage"));
            if (image == null)
            {
                stack.AddArrangedSubview(MakeLabel("لا توجد صورة مرفقة", 13f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
            }
            else
            {
                var preview = new UIImageView(image)
                {
                    ContentMode = UIViewContentMode.ScaleAspectFit,
                    BackgroundColor = Theme.White,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                preview.Layer.CornerRadius = Theme.CornerEdit;
                preview.Layer.BorderWidth = 1f;
                preview.Layer.BorderColor = Theme.CardStroke.CGColor;
                preview.Layer.MasksToBounds = true;
                preview.HeightAnchor.ConstraintEqualTo(280).Active = true;
                stack.AddArrangedSubview(preview);

                var open = Controls.OutlineButton("عرض الصورة بالحجم الكامل");
                open.TouchUpInside += (s, e) => ShowFull(image);
                stack.AddArrangedSubview(open);
            }

            /* ---------- المراجعة ---------- */
            if (string.IsNullOrEmpty(status) || status == "معلق")
            {
                var confirm = Controls.PrimaryButton("تأكيد إضافة الرصيد");
                confirm.TouchUpInside += (s, e) => Confirm(amount);
                stack.AddArrangedSubview(confirm);

                var reject = Controls.DangerButton("رفض الطلب");
                reject.TouchUpInside += (s, e) => Reject();
                stack.AddArrangedSubview(reject);
            }
        }

        private UIView InfoRow(string label, string value)
        {
            return MakeLabel(label + ": " + (string.IsNullOrWhiteSpace(value) ? "—" : value),
                14f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right);
        }

        private void Confirm(decimal amount)
        {
            Ui.Confirm("تأكيد الشحن",
                "إضافة " + amount.ToString("0.00") + " ج.م إلى محفظة العميل؟", "تأكيد", () =>
                {
                    Run(() => Api.ConfirmTopUp(Row.Int(_row, "Id")), "تم تأكيد الشحن");
                });
        }

        private void Reject()
        {
            Ui.Prompt("سبب الرفض", "اكتب السبب", "", "السبب", reason =>
            {
                Run(() => Api.RejectTopUp(Row.Int(_row, "Id"), reason), "تم الرفض");
            });
        }

        private void Run(Action action, string success)
        {
            var progress = Ui.ShowProgress("جارٍ التنفيذ…");

            Ui.Run(action, () =>
            {
                progress.Dismiss();
                Ui.Toast(success);

                if (_onChanged != null) _onChanged();

                if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                    NavigationController.PopViewController(true);
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        /// <summary>عرض الإيصال بالحجم الكامل — اضغط في أي مكان للإغلاق.</summary>
        private void ShowFull(UIImage image)
        {
            var vc = new UIViewController { ModalPresentationStyle = UIModalPresentationStyle.FullScreen };
            vc.View.BackgroundColor = UIColor.Black;

            var iv = new UIImageView(image)
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            vc.View.AddSubview(iv);

            var hint = new UILabel
            {
                Text = "اضغط للإغلاق",
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(13),
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            vc.View.AddSubview(hint);

            vc.View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(iv, NSLayoutAttribute.Top, NSLayoutRelation.Equal, vc.View, NSLayoutAttribute.Top, 1, 40),
                NSLayoutConstraint.Create(iv, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, hint, NSLayoutAttribute.Top, 1, -12),
                NSLayoutConstraint.Create(iv, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, vc.View, NSLayoutAttribute.Leading, 1, 8),
                NSLayoutConstraint.Create(iv, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, vc.View, NSLayoutAttribute.Trailing, 1, -8),

                NSLayoutConstraint.Create(hint, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, vc.View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -16),
                NSLayoutConstraint.Create(hint, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, vc.View, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(hint, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, vc.View, NSLayoutAttribute.Trailing, 1, -16),
            });

            vc.View.UserInteractionEnabled = true;
            vc.View.AddGestureRecognizer(new UITapGestureRecognizer(() => vc.DismissViewController(true, null)));

            PresentViewController(vc, true, null);
        }

        /// <summary>فك Base64 إلى صورة (بديل Convert.FromBase64String + BitmapFactory).</summary>
        private static UIImage Decode(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                using (var data = NSData.FromArray(bytes))
                {
                    return data == null ? null : UIImage.LoadFromData(data);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
