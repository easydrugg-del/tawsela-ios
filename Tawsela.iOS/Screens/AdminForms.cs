using System;
using System.Collections.Generic;
using System.Globalization;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/BlockedDriversActivity.cs — السائقون المحظورون مع إلغاء الحظر.
    /// </summary>
    public class BlockedDriversViewController : ListViewController<Driver>
    {
        public BlockedDriversViewController() : base("السائقون المحظورون") { }

        protected override string EmptyText
        {
            get { return "لا يوجد سائقون محظورون"; }
        }

        protected override List<Driver> Fetch()
        {
            return Api.BlockedDrivers();
        }

        protected override UIView BuildRow(Driver d)
        {
            var row = Controls.InfoRow(d.FullName + " — " + d.Phone,
                (d.CarModel + " " + d.CarPlate).Trim());

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Confirm("إلغاء الحظر", "إلغاء حظر " + d.FullName + "؟", "إلغاء الحظر", () =>
                {
                    var progress = Ui.ShowProgress("جارٍ التنفيذ…");
                    Ui.Run(() => Api.UnblockDriver(d.UserId), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم إلغاء الحظر");
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
    /// بديل Activities/ServiceAdminActivity.cs — إدارة أنواع الخدمات وأسعارها
    /// (نهار/ليل/مضاعفات/عمولة) مع فترة الليل.
    /// </summary>
    public class ServiceAdminViewController : ListViewController<ServiceType>
    {
        private bool _showNight;

        public ServiceAdminViewController() : base("إدارة الخدمات") { }

        protected override string EmptyText
        {
            get { return "لا توجد خدمات معرّفة"; }
        }

        protected override List<ServiceType> Fetch()
        {
            return Api.ServiceTypes();
        }

        protected override UIView BuildRow(ServiceType s)
        {
            var baseKm = s.BaseKm > 0 ? s.BaseKm : 2m;
            var hasNight = s.NightBaseFare > 0 || s.NightExtraPerKm > 0 || s.NightMultiplier > 1;

            var modeTag = s.PricingMode == "ليل" ? " 🌙 ليل دائم"
                        : s.PricingMode == "نهار" ? " ☀️ نهار دائم" : "";

            var night = _showNight && hasNight;

            var fare = night
                ? "🌙 أول " + baseKm.ToString("0.##", CultureInfo.InvariantCulture) + " كم: " +
                  s.NightBaseFare.ToString("0", CultureInfo.InvariantCulture) + " ج.م"
                : "☀️ أول " + baseKm.ToString("0.##", CultureInfo.InvariantCulture) + " كم: " +
                  s.BaseFare.ToString("0", CultureInfo.InvariantCulture) + " ج.م";

            var desc = night
                ? "كل كم إضافي: " + s.NightExtraPerKm.ToString("0.##", CultureInfo.InvariantCulture) +
                  " ج.م (×" + s.NightMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + ")"
                : "كل كم إضافي: " + s.ExtraPerKm.ToString("0.##", CultureInfo.InvariantCulture) +
                  " ج.م (×" + s.Multiplier.ToString("0.##", CultureInfo.InvariantCulture) + ")";

            if (s.CommissionFixed > 0 || s.CommissionPercent > 0)
            {
                desc += "\nعمولة: " + s.CommissionFixed.ToString("0.##", CultureInfo.InvariantCulture) + " ج.م";
                if (s.CommissionPercent > 0)
                    desc += " + " + s.CommissionPercent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
            }

            if (!s.IsActive) desc += "\n(غير مفعّلة)";

            var row = Controls.InfoRow(s.Name + modeTag, fare + "\n" + desc);
            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
                Push(new ServiceTypeEditViewController(s, Reload))));

            return row;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var add = new UIBarButtonItem("إضافة", UIBarButtonItemStyle.Plain,
                (s, e) => Push(new ServiceTypeEditViewController(null, Reload)));

            var night = new UIBarButtonItem("🌙/☀️", UIBarButtonItemStyle.Plain, (s, e) =>
            {
                _showNight = !_showNight;
                Reload();
            });

            var window = new UIBarButtonItem("فترة الليل", UIBarButtonItemStyle.Plain,
                (s, e) => EditNightWindow());

            NavigationItem.RightBarButtonItems = new[] { add, night, window };
        }

        /// <summary>ضبط ساعة بداية ونهاية فترة الليل (بديل Spinner من/إلى).</summary>
        private void EditNightWindow()
        {
            Ui.Run(() =>
            {
                int from, to;
                Api.NightWindow(out from, out to);

                Ui.BeginOnMain(() =>
                {
                    Ui.Prompt("فترة الليل", "من الساعة (0-23)", from.ToString(), "22", fromText =>
                    {
                        int f;
                        if (!int.TryParse(fromText, out f) || f < 0 || f > 23)
                        {
                            Ui.Toast("ساعة غير صحيحة");
                            return;
                        }

                        Ui.Prompt("فترة الليل", "إلى الساعة (0-23)", to.ToString(), "6", toText =>
                        {
                            int t;
                            if (!int.TryParse(toText, out t) || t < 0 || t > 23)
                            {
                                Ui.Toast("ساعة غير صحيحة");
                                return;
                            }

                            var progress = Ui.ShowProgress("جارٍ الحفظ…");
                            Ui.Run(() => Api.AdminSaveSettings(new Dictionary<string, string>
                            {
                                { "NightFromHour", f.ToString() },
                                { "NightToHour", t.ToString() }
                            }), () =>
                            {
                                progress.Dismiss();
                                Ui.Toast("تم حفظ فترة الليل: " + f.ToString("00") + " → " + t.ToString("00"));
                                Reload();
                            }, ex =>
                            {
                                progress.Dismiss();
                                Ui.Toast("خطأ: " + ex.Message);
                            });
                        }, UIKeyboardType.NumberPad);
                    }, UIKeyboardType.NumberPad);
                });
            }, null, ex => Ui.Toast(ex.Message));
        }
    }

    /// <summary>
    /// نموذج إضافة/تعديل نوع خدمة — بديل حوار ServiceAdminActivity المليء بالحقول
    /// (نفس الحقول ونفس التحقق، لكن كشاشة قابلة للتمرير بدل AlertDialog).
    /// </summary>
    public class ServiceTypeEditViewController : BaseViewController
    {
        private readonly ServiceType _service;
        private readonly Action _onSaved;
        private readonly bool _isNew;

        private UITextField _name, _baseFare, _baseKm, _extraKm, _multiplier;
        private UITextField _nightBaseFare, _nightExtraKm, _nightMultiplier;
        private UITextField _commissionFixed, _commissionPercent;
        private UISegmentedControl _mode;
        private UISwitch _active;

        public ServiceTypeEditViewController(ServiceType service, Action onSaved)
            : base(service == null ? "خدمة جديدة" : "تعديل: " + service.Name)
        {
            _service = service;
            _onSaved = onSaved;
            _isNew = service == null;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(10f, 16f);

            var s = _service;

            stack.AddArrangedSubview(Controls.SectionTitle("اسم الخدمة"));
            _name = Controls.TextField("اسم الخدمة");
            if (s != null) _name.Text = s.Name;
            stack.AddArrangedSubview(_name);

            stack.AddArrangedSubview(Controls.SectionTitle("⚙️ وضع التسعير"));
            _mode = new UISegmentedControl(new[] { "تلقائي", "نهار", "ليل" })
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                SelectedSegment = s == null ? 0 : (s.PricingMode == "نهار" ? 1 : s.PricingMode == "ليل" ? 2 : 0)
            };
            stack.AddArrangedSubview(_mode);

            stack.AddArrangedSubview(Controls.SectionTitle("☀️ أسعار النهار"));
            _baseFare = NumberField("القيمة الأساسية", s == null ? 10m : s.BaseFare);
            _baseKm = NumberField("نطاق الكيلومترات (افتراضي 2)", s == null ? 2m : (s.BaseKm > 0 ? s.BaseKm : 2m));
            _extraKm = NumberField("قيمة كل كيلو إضافي", s == null ? 15m : s.ExtraPerKm);
            _multiplier = NumberField("المضاعف (مثلاً 1.5)", s == null ? 1m : (s.Multiplier > 0 ? s.Multiplier : 1m));

            stack.AddArrangedSubview(Labeled("القيمة الأساسية", _baseFare));
            stack.AddArrangedSubview(Labeled("نطاق الكيلومترات", _baseKm));
            stack.AddArrangedSubview(Labeled("قيمة الكيلو الإضافي", _extraKm));
            stack.AddArrangedSubview(Labeled("المضاعف", _multiplier));

            stack.AddArrangedSubview(Controls.SectionTitle("🌙 أسعار الليل"));
            _nightBaseFare = NumberField("أساسي الليل (0 = نفس النهار)", s == null ? 0m : s.NightBaseFare);
            _nightExtraKm = NumberField("كيلو الليل الإضافي (0 = نفس النهار)", s == null ? 0m : s.NightExtraPerKm);
            _nightMultiplier = NumberField("مضاعف الليل (1 = بدون)",
                s == null ? 1m : (s.NightMultiplier > 0 ? s.NightMultiplier : 1m));

            stack.AddArrangedSubview(Labeled("أساسي الليل", _nightBaseFare));
            stack.AddArrangedSubview(Labeled("كيلو الليل الإضافي", _nightExtraKm));
            stack.AddArrangedSubview(Labeled("مضاعف الليل", _nightMultiplier));

            stack.AddArrangedSubview(Controls.SectionTitle("💰 عمولة التطبيق"));
            _commissionFixed = NumberField("عمولة ثابتة على الرحلة (ج.م)",
                s == null ? 0m : s.CommissionFixed);
            _commissionPercent = NumberField("نسبة إضافية بعد النطاق (%)",
                s == null ? 0m : s.CommissionPercent);

            stack.AddArrangedSubview(Labeled("عمولة ثابتة حتى نطاق الكيلومترات", _commissionFixed));
            stack.AddArrangedSubview(Labeled("نسبة من الأجرة بعد النطاق", _commissionPercent));

            if (!_isNew)
            {
                var activeRow = new UIStackView
                {
                    Axis = UILayoutConstraintAxis.Horizontal,
                    Spacing = 10,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                _active = new UISwitch { On = s.IsActive, TranslatesAutoresizingMaskIntoConstraints = false };
                activeRow.AddArrangedSubview(_active);
                activeRow.AddArrangedSubview(MakeLabel("مفعّلة", 15f, Theme.TextPrimary,
                    UIFontWeight.Medium, UITextAlignment.Right));
                stack.AddArrangedSubview(activeRow);
            }

            var save = Controls.PrimaryButton(_isNew ? "إضافة الخدمة" : "حفظ التعديلات");
            save.TouchUpInside += (sender, e) => Save();
            stack.AddArrangedSubview(save);
        }

        private UITextField NumberField(string placeholder, decimal value)
        {
            var f = Controls.TextField(placeholder);
            f.KeyboardType = UIKeyboardType.DecimalPad;
            f.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
            f.TextAlignment = UITextAlignment.Left;
            f.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
            return f;
        }

        private UIView Labeled(string title, UIView field)
        {
            var stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 4,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(MakeLabel(title, 13f, Theme.TextSecondary,
                UIFontWeight.Medium, UITextAlignment.Right));
            stack.AddArrangedSubview(field);
            return stack;
        }

        /// <summary>قراءة رقم؛ النص الفاضي يرجع القيمة القديمة (نفس ParseOrKeep في أندرويد).</summary>
        private static decimal ParseOrKeep(UITextField field, decimal current, string name, List<string> invalid)
        {
            var text = NormalizeNumber(field == null ? "" : (field.Text ?? ""));
            if (text == "") return current;

            decimal value;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return value;

            invalid.Add(name);
            return current;
        }

        /// <summary>تطبيع الأرقام العربية/الفارسية والفاصلة العشرية العربية.</summary>
        private static string NormalizeNumber(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var sb = new System.Text.StringBuilder(text.Length);
            foreach (var raw in text.Trim())
            {
                var ch = raw;
                if (ch >= '٠' && ch <= '٩') ch = (char)('0' + (ch - '٠'));
                else if (ch >= '۰' && ch <= '۹') ch = (char)('0' + (ch - '۰'));
                else if (ch == '٫' || ch == '،' || ch == ',') ch = '.';
                else if (ch == '٬') continue;

                if (!char.IsWhiteSpace(ch)) sb.Append(ch);
            }
            return sb.ToString();
        }

        private void Save()
        {
            var invalid = new List<string>();

            var name = (_name.Text ?? "").Trim();
            if (name == "") { Ui.Toast("أدخل اسم الخدمة"); return; }

            var s = _service;
            var bf = ParseOrKeep(_baseFare, s == null ? 10m : s.BaseFare, "القيمة الأساسية", invalid);
            var bk = ParseOrKeep(_baseKm, s == null ? 2m : (s.BaseKm > 0 ? s.BaseKm : 2m), "نطاق الكيلومترات", invalid);
            var pk = ParseOrKeep(_extraKm, s == null ? 15m : s.ExtraPerKm, "قيمة الكيلو الإضافي", invalid);
            var m = ParseOrKeep(_multiplier, s == null ? 1m : (s.Multiplier > 0 ? s.Multiplier : 1m), "المضاعف", invalid);
            var nbf = ParseOrKeep(_nightBaseFare, s == null ? 0m : s.NightBaseFare, "أساسي الليل", invalid);
            var npk = ParseOrKeep(_nightExtraKm, s == null ? 0m : s.NightExtraPerKm, "كيلو الليل الإضافي", invalid);
            var nm = ParseOrKeep(_nightMultiplier, s == null ? 1m : (s.NightMultiplier > 0 ? s.NightMultiplier : 1m), "مضاعف الليل", invalid);
            var cf = ParseOrKeep(_commissionFixed, s == null ? 0m : s.CommissionFixed, "عمولة التطبيق الثابتة", invalid);
            var cp = ParseOrKeep(_commissionPercent, s == null ? 0m : s.CommissionPercent, "نسبة عمولة التطبيق", invalid);

            if (cp < 0 || cp > 100) invalid.Add("نسبة العمولة (0-100)");

            if (invalid.Count > 0)
            {
                Ui.Toast("قيم غير صالحة: " + string.Join("، ", invalid));
                return;
            }

            if (bk <= 0) bk = 2m;
            if (nm <= 0) nm = 1m;

            var mode = _mode.SelectedSegment == 1 ? "نهار" : _mode.SelectedSegment == 2 ? "ليل" : "تلقائي";
            var active = _isNew || (_active != null && _active.On);

            var progress = Ui.ShowProgress("جارٍ الحفظ…");

            if (_isNew)
            {
                Ui.Run(() => Api.AddServiceType(name, bf, bk, pk, nbf, npk, nm, m, mode, cf, cp), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تمت الإضافة");
                    if (_onSaved != null) _onSaved();
                    Back();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast("خطأ: " + ex.Message);
                });
            }
            else
            {
                var id = s.Id;
                Ui.Run(() => Api.UpdateServiceType(id, name, bf, bk, pk, nbf, npk, nm, m, mode, active, cf, cp), () =>
                {
                    progress.Dismiss();
                    Ui.Toast("تم حفظ التعديلات");
                    if (_onSaved != null) _onSaved();
                    Back();
                }, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast("خطأ: " + ex.Message);
                });
            }
        }
    }

    /// <summary>
    /// بديل Activities/RestaurantDeliveryMyOrdersActivity.cs — توصيلات المطعم التي أنشأها
    /// (Api.GetMyRestaurantDeliveries) مع متابعة التنفيذ.
    /// </summary>
    public class RestaurantDeliveryMyOrdersViewController : ListViewController<Dictionary<string, object>>
    {
        public RestaurantDeliveryMyOrdersViewController() : base("توصيلات مطعمي") { }

        protected override string EmptyText
        {
            get { return "لا توجد توصيلات سابقة"; }
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.GetMyRestaurantDeliveries();
        }

        protected override UIView BuildRow(Dictionary<string, object> o)
        {
            var id = Row.Int(o, "Id");
            var name = Row.Str(o, "CustomerName");
            var status = Row.Str(o, "Status");
            var fare = Row.Num(o, "Fare");
            var pickup = Row.Str(o, "PickupAddress");
            var delivery = Row.Str(o, "DeliveryAddress");

            var row = Controls.InfoRow(string.IsNullOrEmpty(name) ? "توصيلة #" + id : name,
                (string.IsNullOrEmpty(status) ? "—" : status) + " · " +
                fare.ToString("0.00") + " ج.م\n" +
                "من: " + pickup + "\nإلى: " + delivery);

            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
                Push(new RestaurantDeliveryExecuteViewController(id))));

            return row;
        }
    }
}
