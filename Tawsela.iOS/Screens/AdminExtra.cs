using System;
using System.Collections.Generic;
using System.Globalization;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/AdminAccountsActivity.cs — حسابات الإدارة.
    /// </summary>
    public class AdminAccountsViewController : ListViewController<Dictionary<string, object>>
    {
        public AdminAccountsViewController() : base("إدارة حسابات المدير") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "إدارة حسابات المدير";

        protected override string EmptyText
        {
            get { return "لا يوجد حسابات مديرين"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.AllAdmins();
        }

        protected override UIView BuildRow(Dictionary<string, object> a)
        {
            var id = Row.Int(a, "Id");
            var name = Row.Str(a, "FullName");
            if (string.IsNullOrEmpty(name)) name = Row.Str(a, "UserName");
            var phone = Row.Str(a, "Phone");
            var user = Row.Str(a, "UserName");

            var row = Controls.InfoRow(name, user + " · " + phone);
            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                var active = Row.Bool(a, "IsActive") || !a.ContainsKey("IsActive");

                Ui.Choose(name, new[]
                {
                    "تعديل",
                    "تغيير كلمة المرور",
                    active ? "إيقاف الحساب" : "تفعيل الحساب",
                    "حذف"
                }, index =>
                {
                    if (index == 0)
                    {
                        Ui.Prompt("تعديل الاسم", "الاسم", name, "الاسم", newName =>
                        {
                            Ui.Prompt("تعديل الهاتف", "الهاتف", phone, "01xxxxxxxxx", newPhone =>
                            {
                                Run(() => Api.UpdateAdmin(id, newName, newPhone), "تم التحديث");
                            }, UIKeyboardType.PhonePad);
                        });
                    }
                    else if (index == 1)
                    {
                        Ui.Prompt("كلمة مرور جديدة", "كلمة المرور", "", "••••••", pass =>
                        {
                            if ((pass ?? "").Length < 6)
                            {
                                Ui.Toast("كلمة المرور 6 أحرف على الأقل");
                                return;
                            }
                            Run(() => Api.ResetAdminPassword(id, pass), "تم تغيير كلمة المرور");
                        });
                    }
                    else if (index == 2)
                    {
                        Run(() => Api.ToggleAdminActive(id, !active),
                            active ? "تم إيقاف الحساب" : "تم تفعيل الحساب");
                    }
                    else
                    {
                        Ui.Confirm("حذف حساب", "حذف " + name + "؟", "حذف",
                            () => Run(() => Api.DeleteAdmin(id), "تم الحذف"));
                    }
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("+ إضافة مدير جديد");
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
            Ui.Prompt("حساب جديد", "اسم المستخدم", "", "user", user =>
            {
                Ui.Prompt("حساب جديد", "كلمة المرور", "", "••••••", pass =>
                {
                    Ui.Prompt("حساب جديد", "الاسم الكامل", "", "الاسم", name =>
                    {
                        Ui.Prompt("حساب جديد", "الهاتف", "", "01xxxxxxxxx", phone =>
                        {
                            Run(() => Api.AddAdmin(user, pass, name, phone), "تمت الإضافة");
                        }, UIKeyboardType.PhonePad);
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

    /// <summary>
    /// بديل Activities/AdminAnalyticsActivity.cs — تحليلات فترة زمنية
    /// (Api.AdminRange + Api.AdminRangeList).
    /// </summary>
    public class AdminAnalyticsViewController : BaseViewController
    {
        private UIStackView _stack;
        private DateTime _from, _to;
        private int _fixedCount = 3;

        public AdminAnalyticsViewController() : base("تحليلات متقدمة") { }

        public override bool ShowNavigationBar => false;

        protected override bool FlatHeader => true;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _from = DateTime.Today.AddDays(-7);
            _to = DateTime.Today;

            _stack = CreateScrollContent(10f, 16f);

            var week = Controls.OutlineButton("آخر 7 أيام");
            week.TouchUpInside += (s, e) => { _from = DateTime.Today.AddDays(-7); _to = DateTime.Today; Load(); };
            _stack.AddArrangedSubview(week);

            var month = Controls.OutlineButton("آخر 30 يومًا");
            month.TouchUpInside += (s, e) => { _from = DateTime.Today.AddDays(-30); _to = DateTime.Today; Load(); };
            _stack.AddArrangedSubview(month);

            var breakdown = Controls.OutlineButton("التوزيع (الحالة / الخدمة / الدفع)");
            breakdown.TouchUpInside += (s, e) => ShowBreakdown();
            _stack.AddArrangedSubview(breakdown);

            // أفضل السائقين والعملاء (بديل txtTopDrivers / txtTopCustomers)
            var topDrivers = Controls.OutlineButton("أفضل السائقين");
            topDrivers.TouchUpInside += (s, e) => ShowTop("topDrivers", "أفضل السائقين");
            _stack.AddArrangedSubview(topDrivers);

            var topCustomers = Controls.OutlineButton("أفضل العملاء");
            topCustomers.TouchUpInside += (s, e) => ShowTop("topCustomers", "أفضل العملاء");
            _stack.AddArrangedSubview(topCustomers);

            // ضبط معامل الذروة (بديل btnPeak في أندرويد)
            var peak = Controls.OutlineButton("تسعير الذروة");
            peak.TouchUpInside += (s, e) =>
            {
                Ui.Prompt("مضاعف الذروة (1.0 = عادي)", "مثال: 1.5", "", "1.0", text =>
                {
                    decimal m;
                    if (!decimal.TryParse((text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out m))
                    {
                        Ui.Toast("قيمة غير صحيحة");
                        return;
                    }

                    SaveSettings(new Dictionary<string, string>
                    {
                        { "PeakMultiplier", m.ToString(CultureInfo.InvariantCulture) }
                    }, "تم ضبط معامل الذروة " + m.ToString(CultureInfo.InvariantCulture));
                }, UIKeyboardType.DecimalPad);
            };
            _stack.AddArrangedSubview(peak);

            // ضبط عمولة النقدي: نسبة + مبلغ ثابت (بديل btnComm)
            var commission = Controls.OutlineButton("عمولة الدفع النقدي للسائق");
            commission.TouchUpInside += (s, e) =>
            {
                Ui.Prompt("نسبة % (مثلاً 15)", "مثال: 15", "", "15", pText =>
                {
                    Ui.Prompt("مبلغ ثابت (0=نسبة)", "اكتب 0 إن لم توجد", "", "0", fText =>
                    {
                        decimal p, f;
                        decimal.TryParse((pText ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out p);
                        decimal.TryParse((fText ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out f);

                        SaveSettings(new Dictionary<string, string>
                        {
                            { "CashCommissionPercent", p.ToString(CultureInfo.InvariantCulture) },
                            { "CashCommissionFixed", f.ToString(CultureInfo.InvariantCulture) }
                        }, f > 0 ? "تم ضبط العمولة: " + f.ToString("0.##") + " ج.م" : "تم ضبط العمولة: " + p.ToString("0.##") + "%");
                    }, UIKeyboardType.DecimalPad);
                }, UIKeyboardType.DecimalPad);
            };
            _stack.AddArrangedSubview(commission);

            // تصدير الطلبات CSV (بديل btnExport)
            var export = Controls.OutlineButton("تصدير الطلبات (مشاركة نص)");
            export.TouchUpInside += (s, e) => ExportCsv();
            _stack.AddArrangedSubview(export);

            // عدد العناصر الثابتة أعلى الشاشة (تُستثنى عند إعادة رسم النتائج)
            _fixedCount = _stack.ArrangedSubviews.Length;

            Load();
        }

        /// <summary>حفظ إعدادات الإدارة (بديل Api.AdminSaveSettings).</summary>
        private void SaveSettings(Dictionary<string, string> values, string success)
        {
            var progress = Ui.ShowProgress("جارٍ الحفظ…");

            Ui.Run(() => Api.AdminSaveSettings(values), () =>
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

        /// <summary>تصدير الطلبات نصيًا ومشاركتها (بديل Intent.ActionSend في أندرويد).</summary>
        private void ExportCsv()
        {
            var progress = Ui.ShowProgress("جارٍ تجهيز الملف…");

            Ui.Run(() =>
            {
                var orders = Api.GetOrders("", "");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("OrderNumber,Customer,Driver,Status,Fare,Date");

                foreach (var o in orders)
                {
                    var fare = o.ActualFare > 0 ? o.ActualFare : o.EstimatedFare;
                    sb.AppendLine(o.OrderNumber + "," + o.CustomerName + "," + o.DriverName + "," +
                                  o.Status + "," + fare.ToString("0.##", CultureInfo.InvariantCulture) + "," +
                                  o.CreatedAt.ToString("yyyy-MM-dd"));
                }

                var text = sb.ToString();

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    var share = new UIActivityViewController(
                        new NSObject[] { new Foundation.NSString(text) }, null);

                    // على iPad يجب تحديد مصدر الـ Popover وإلا يحدث Crash
                    if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                        share.PopoverPresentationController != null)
                    {
                        share.PopoverPresentationController.SourceView = View;
                        share.PopoverPresentationController.SourceRect =
                            new CoreGraphics.CGRect(View.Bounds.Width / 2, View.Bounds.Height / 2, 1, 1);
                    }

                    PresentViewController(share, true, null);
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        private void Load()
        {
            var from = _from;
            var to = _to;

            Ui.Run(() =>
            {
                var range = Api.AdminRange(from, to);

                Dictionary<string, object> wallets = null;
                try { wallets = Api.AdminWallets(); } catch { }

                Ui.BeginOnMain(() =>
                {
                    while (_stack.ArrangedSubviews.Length > _fixedCount)
                    {
                        var v = _stack.ArrangedSubviews[_fixedCount];
                        _stack.RemoveArrangedSubview(v);
                        v.RemoveFromSuperview();
                    }

                    _stack.AddArrangedSubview(Controls.InfoRow("الفترة",
                        from.ToString("dd/MM/yyyy") + " — " + to.ToString("dd/MM/yyyy")));

                    if (range == null || range.Count == 0)
                    {
                        _stack.AddArrangedSubview(MakeLabel("لا توجد بيانات في هذه الفترة",
                            14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
                    }
                    else
                    {
                        foreach (var kv in range)
                        {
                            var value = kv.Value == null ? "—" : kv.Value.ToString();
                            _stack.AddArrangedSubview(Controls.InfoRow(Translate(kv.Key), value));
                        }
                    }

                    // أرصدة المحافظ (بديل Api.AdminWallets)
                    if (wallets != null && wallets.Count > 0)
                    {
                        _stack.AddArrangedSubview(Controls.SectionTitle("المحافظ"));

                        foreach (var kv in wallets)
                        {
                            var value = kv.Value == null ? "—" : kv.Value.ToString();
                            _stack.AddArrangedSubview(Controls.InfoRow(TranslateWallet(kv.Key), value));
                        }
                    }
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private static string TranslateWallet(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "customers": return "أرصدة العملاء";
                case "customerstotal": return "إجمالي أرصدة العملاء";
                case "drivers": return "أرصدة السائقين";
                case "driverstotal": return "إجمالي أرصدة السائقين";
                case "total": return "الإجمالي";
                case "pending": return "طلبات شحن معلّقة";
                case "pendingcount": return "عدد طلبات الشحن المعلّقة";
                case "cash": return "التحصيل النقدي";
                default: return key;
            }
        }

        private void ShowBreakdown()
        {
            var options = new[] { "حسب الحالة", "حسب الخدمة", "حسب الدفع", "ساعات الذروة" };
            var keys = new[] { "byStatus", "byService", "byPayment", "peak" };

            Ui.Choose("التوزيع (الحالة / الخدمة / الدفع)", options, index =>
            {
                var from = _from;
                var to = _to;
                var key = keys[index];

                Ui.Run(() =>
                {
                    var rows = Api.AdminRangeList(from, to, key);
                    Ui.BeginOnMain(() => RenderBreakdown(options[index], rows));
                }, null, ex => Ui.Toast(ex.Message));
            });
        }

        private void ShowTop(string key, string title)
        {
            var from = _from;
            var to = _to;

            Ui.Run(() =>
            {
                var rows = Api.AdminRangeList(from, to, key);
                Ui.BeginOnMain(() => RenderBreakdown(title, rows));
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void RenderBreakdown(string title, List<Dictionary<string, object>> rows)
        {
            while (_stack.ArrangedSubviews.Length > _fixedCount)
            {
                var v = _stack.ArrangedSubviews[_fixedCount];
                _stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            _stack.AddArrangedSubview(Controls.SectionTitle(title));

            if (rows == null || rows.Count == 0)
            {
                _stack.AddArrangedSubview(MakeLabel("لا توجد بيانات", 14f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Right));
                return;
            }

            foreach (var r in rows)
            {
                var label = Row.Str(r, "Label");
                if (string.IsNullOrEmpty(label)) label = Row.Str(r, "Name");

                var count = Row.Int(r, "Cnt");
                var total = Row.Num(r, "Total");

                _stack.AddArrangedSubview(Controls.InfoRow(label,
                    count > 0 ? count + " طلب · " + total.ToString("0.00") + " ج.م"
                              : total.ToString("0.00") + " ج.م"));
            }
        }

        private static string Translate(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "orders": return "عدد الطلبات";
                case "avgfare": return "متوسط الأجرة";
                case "driversdebt": return "مديونية السائقين";
                case "opencomplaints": return "شكاوى مفتوحة";
                case "cashcommission": return "عمولة النقدي";
                case "cashcommissionfixed": return "عمولة ثابتة (نقدي)";
                case "cashcommissionpercent": return "نسبة العمولة (نقدي)";
                case "commfixed": return "عمولة ثابتة";
                case "commpercent": return "نسبة العمولة";
                case "peakmultiplier": return "معامل الذروة";
                default: return key;
            }
        }
    }

    /// <summary>
    /// بديل Activities/AdsAdminActivity.cs — إدارة الإعلانات.
    /// </summary>
    public class AdsAdminViewController : ListViewController<Dictionary<string, object>>
    {
        public AdsAdminViewController() : base("إدارة الإعلانات") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "إدارة الإعلانات";

        protected override string EmptyText
        {
            get { return "لا توجد إعلانات بعد"; }
        }

        protected override string Subtitle
        {
            get { return "الإعلانات المفعّلة تظهر للعميل والسائق في الشاشة الرئيسية"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.AllAds();
        }

        protected override UIView BuildRow(Dictionary<string, object> a)
        {
            var id = Row.Int(a, "Id");
            var title = Row.Str(a, "Title");
            var scope = Row.Str(a, "TargetScope");
            var active = Row.Bool(a, "IsActive");

            var row = Controls.InfoRow(title + (active ? "" : " (موقوف)"),
                Row.Str(a, "Body") + (string.IsNullOrEmpty(scope) ? "" : "\n" + scope));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(title, new[] { active ? "إيقاف" : "تفعيل", "حذف" }, index =>
                {
                    if (index == 0)
                        Run(() => Api.SetAdActive(id, !active), "تم التحديث");
                    else
                        Ui.Confirm("حذف إعلان", "حذف " + title + "؟", "حذف",
                            () => Run(() => Api.DeleteAd(id), "تم الحذف"));
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = Controls.PrimaryButton("إعلان جديد (صورة و/أو نص)");
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
            const string form = "إعلان جديد (صورة و/أو نص)";

            Ui.Prompt(form, "عنوان الإعلان", "", "العنوان", title =>
            {
                Ui.Prompt(form, "نص الإعلان (اختياري)", "", "النص", body =>
                {
                    Ui.Prompt(form, "رابط عند الضغط (اختياري)", "", "https://", link =>
                    {
                        Ui.Choose("يظهر الإعلان لـ:", new[] { "الكل", "محافظة", "مدينة" }, scopeIndex =>
                        {
                            var scope = scopeIndex == 0 ? "الكل" : scopeIndex == 1 ? "محافظة" : "مدينة";
                            var gov = scope == "محافظة" ? Session.Governorate : "";
                            var city = "";

                            Action publishStep = () =>
                            {
                                Ui.Choose("اختيار صورة", new[] { "بدون صورة", "المعرض", "الكاميرا" }, imageIndex =>
                                {
                                    Action<string> publish = image =>
                                        Ui.Confirm("نشر الإعلان", "نشر الإعلان الآن؟", "نشر", () =>
                                            Run(() => Api.AddAd(title, body, image, link, scope, gov, city), "تمت الإضافة"));

                                    if (imageIndex == 0)
                                    {
                                        publish("");
                                        return;
                                    }

                                    ImagePicker.PickImage(this, imageIndex == 2, img =>
                                    {
                                        if (img == null) { publish(""); return; }

                                        try
                                        {
                                            using (var data = ImagePicker.Scale(img, 800f).AsJPEG(0.7f))
                                                publish(data == null ? "" : Convert.ToBase64String(data.ToArray()));
                                        }
                                        catch
                                        {
                                            publish("");
                                        }
                                    }, err => Ui.Toast(err));
                                });
                            };

                            if (scope != "مدينة")
                            {
                                publishStep();
                                return;
                            }

                            Ui.Prompt("المدن المختارة (تضاف من القائمة)",
                                "اسم المدينة — اكتب «مسح المدن» للمسح", "", Session.City, cityText =>
                            {
                                var text = (cityText ?? "").Trim();
                                if (text == "مسح المدن") city = "";
                                else if (text.Length > 0) city = text;

                                publishStep();
                            });
                        });
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

    /// <summary>
    /// بديل Activities/AppUpdateAdminActivity.cs — ضبط إصدار التطبيق ورابط التحديث.
    /// </summary>
    public class AppUpdateAdminViewController : BaseViewController
    {
        private UILabel _current;
        private string _version = "";
        private string _url = "";

        public AppUpdateAdminViewController() : base("تحديث الاصدار") { }

        public override bool ShowNavigationBar => false;

        protected override bool FlatHeader => true;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(MakeLabel(
                "النسخة المسجلة هنا هي ما سيُقارن بها التطبيق عند فتحه",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            _current = MakeLabel("جارٍ التحميل…", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_current);

            var save = Controls.PrimaryButton("حفظ");
            save.TouchUpInside += (s, e) => Edit();
            stack.AddArrangedSubview(save);

            stack.AddArrangedSubview(MakeLabel(
                "ملاحظة: على iOS يُفتح رابط App Store فقط. ضع رابط التطبيق من App Store Connect " +
                "أو اتركه فارغًا وحدّث رقم التطبيق في الإعدادات لاحقًا.",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var settings = Api.AdminSettings();
                Ui.BeginOnMain(() =>
                {
                    _version = Pick(settings, "LatestAppVersion", "LatestVersion", "appVersion");
                    _url = Pick(settings, "LatestAppUrl", "LatestUrl", "appUrl");

                    _current.Text = "الإصدار المعلن: " + (string.IsNullOrEmpty(_version) ? "—" : _version) +
                                    "\nالرابط: " + (string.IsNullOrEmpty(_url) ? "—" : _url);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Edit()
        {
            Ui.Prompt("رقم الاصدار الجديد (مثال: 1.3.0)", "مثال: 1.3.0", _version, "1.3.0", version =>
            {
                Ui.Prompt("رابط التحديث الاخير", "رابط App Store", _url, "https://apps.apple.com/...", url =>
                {
                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                    Ui.Run(() => Api.AdminSaveSettings(new Dictionary<string, string>
                    {
                        { "LatestAppVersion", version },
                        { "LatestAppUrl", url }
                    }), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم حفظ بيانات الإصدار");
                        Load();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                });
            });
        }

        private static string Pick(Dictionary<string, string> settings, params string[] keys)
        {
            if (settings == null) return "";
            foreach (var k in keys)
            {
                string v;
                if (settings.TryGetValue(k, out v) && !string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            return "";
        }
    }

    /// <summary>
    /// بديل Activities/DriverFeeAdminActivity.cs — عمولة/رسم قبول الطلب للسائق.
    /// </summary>
    public class DriverFeeAdminViewController : BaseViewController
    {
        private UILabel _current;
        private string _value = "";

        public DriverFeeAdminViewController() : base("قيمة الخصم من السائق") { }

        public override bool ShowNavigationBar => false;

        protected override bool FlatHeader => true;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            _current = MakeLabel("جارٍ التحميل…", 15f, Theme.TextPrimary,
                UIFontWeight.Medium, UITextAlignment.Right);
            stack.AddArrangedSubview(_current);

            stack.AddArrangedSubview(MakeLabel(
                "تُخصم من محفظة السائق عند قبوله أي طلب (0 = بدون خصم)",
                12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right));

            var edit = Controls.PrimaryButton("حفظ");
            edit.TouchUpInside += (s, e) => Edit();
            stack.AddArrangedSubview(edit);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                var settings = Api.AdminSettings();
                Ui.BeginOnMain(() =>
                {
                    string v = "";
                    if (settings != null)
                    {
                        if (!settings.TryGetValue("DriverAcceptFee", out v) || string.IsNullOrWhiteSpace(v))
                            settings.TryGetValue("driverFee", out v);
                    }

                    _value = v ?? "";
                    _current.Text = "القيمة الحالية: " + (string.IsNullOrEmpty(_value) ? "0.00" : _value) + " ج.م";
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Edit()
        {
            Ui.Prompt("قيمة الخصم من السائق", "المبلغ بالجنيه", _value, "مثال: 5", text =>
            {
                decimal value;
                if (!decimal.TryParse(text, out value) || value < 0)
                {
                    Ui.Toast("أدخل قيمة صحيحة");
                    return;
                }

                var progress = Ui.ShowProgress("جارٍ الحفظ…");
                Ui.Run(() => Api.AdminSaveSettings(new Dictionary<string, string>
                {
                    { "DriverAcceptFee", value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) }
                }), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم حفظ القيمة");
                    Load();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
            }, UIKeyboardType.DecimalPad);
        }
    }

    /// <summary>
    /// بديل Activities/RestaurantMainActivity.cs من جهة الإدارة —
    /// إدارة المطاعم وحساباتها (Api.AllRestaurants / SaveRestaurant / CreateRestaurantAccount).
    /// </summary>
    public class RestaurantsAdminViewController : ListViewController<Dictionary<string, object>>
    {
        public RestaurantsAdminViewController() : base("المطاعم والمحلات") { }

        protected override bool AndroidHeader => true;

        protected override string PageTitle => "المطاعم والمحلات";

        protected override string EmptyText
        {
            get { return "لا توجد مطاعم مسجّلة"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.AllRestaurants();
        }

        protected override UIView BuildRow(Dictionary<string, object> r)
        {
            var id = Row.Int(r, "Id");
            var name = Row.Str(r, "Name");
            var phone = Row.Str(r, "Phone");
            var address = Row.Str(r, "Address");
            var active = Row.Bool(r, "IsActive") || !r.ContainsKey("IsActive");
            var owner = Row.Int(r, "OwnerUserId");

            var row = Controls.InfoRow(name + (active ? "" : " (موقوف)"),
                (string.IsNullOrEmpty(phone) ? "" : phone + " · ") +
                (owner > 0 ? "له حساب · " : "بدون حساب · ") +
                (string.IsNullOrEmpty(address) ? "" : "\n" + address));

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(name, new[]
                {
                    "تعديل البيانات",
                    "إنشاء حساب للمطعم",
                    active ? "إيقاف" : "تفعيل"
                }, index =>
                {
                    if (index == 0)
                    {
                        Edit(r, id, name, phone, address, active);
                    }
                    else if (index == 1)
                    {
                        CreateAccount(id, name, phone);
                    }
                    else
                    {
                        Run(() => Api.SaveRestaurant(id, name, owner, phone, address,
                            Row.Int(r, "AreaId"), Row.Dbl(r, "Lat"), Row.Dbl(r, "Lng"), !active),
                            "تم التحديث");
                    }
                });
            }));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavigationItem.RightBarButtonItem = new UIBarButtonItem("طلبات التفعيل",
                UIBarButtonItemStyle.Plain, (s, e) => Push(new RestaurantApprovalsViewController()));

            var add = Controls.PrimaryButton("إضافة مطعم");
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
            Ui.Prompt("مطعم جديد", "اسم المطعم", "", "اسم المطعم", name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;

                Ui.Prompt("مطعم جديد", "رقم الهاتف", "", "01xxxxxxxxx", phone =>
                {
                    Ui.Prompt("مطعم جديد", "العنوان", "", "العنوان", address =>
                    {
                        Push(MapViewController.Pick(0, 0, (lat, lng, picked) =>
                        {
                            var addr = string.IsNullOrWhiteSpace(address) ? picked : address;
                            Run(() => Api.SaveRestaurant(0, name, 0, phone, addr, 0, lat, lng, true),
                                "تمت إضافة المطعم");
                        }));
                    });
                }, UIKeyboardType.PhonePad);
            });
        }

        private void Edit(Dictionary<string, object> r, int id, string name, string phone, string address, bool active)
        {
            Ui.Prompt("اسم المطعم", "الاسم", name, "الاسم", newName =>
            {
                Ui.Prompt("هاتف المطعم", "الهاتف", phone, "01xxxxxxxxx", newPhone =>
                {
                    Ui.Prompt("عنوان المطعم", "العنوان", address, "العنوان", newAddress =>
                    {
                        Run(() => Api.SaveRestaurant(id, newName, Row.Int(r, "OwnerUserId"),
                            newPhone, newAddress, Row.Int(r, "AreaId"),
                            Row.Dbl(r, "Lat"), Row.Dbl(r, "Lng"), active), "تم التحديث");
                    });
                }, UIKeyboardType.PhonePad);
            });
        }

        /// <summary>إنشاء حساب دخول لصاحب المطعم (بديل Api.CreateRestaurantAccount).</summary>
        private void CreateAccount(int restaurantId, string name, string phone)
        {
            Ui.Prompt("حساب المطعم", "اسم المستخدم", "", "restaurant", user =>
            {
                if (string.IsNullOrWhiteSpace(user)) return;

                Ui.Prompt("حساب المطعم", "كلمة المرور", "", "••••••", pass =>
                {
                    if ((pass ?? "").Length < 6)
                    {
                        Ui.Toast("كلمة المرور 6 أحرف على الأقل");
                        return;
                    }

                    var progress = Ui.ShowProgress("جارٍ الإنشاء…");
                    Ui.Run(() => Api.CreateRestaurantAccount(restaurantId, user, pass, name, phone), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم إنشاء حساب المطعم");
                        Reload();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
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
