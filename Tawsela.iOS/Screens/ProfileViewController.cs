using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/ProfileActivity.cs + SettingsActivity.cs:
    /// بيانات الحساب، تعديل الاسم/الهاتف، تغيير كلمة المرور، قفل PIN،
    /// المستندات القانونية، تسجيل الخروج، وحذف الحساب.
    /// </summary>
    public class ProfileViewController : BaseViewController
    {
        private UISwitch _pinSwitch;
        private UILabel _pinState;
        private UILabel _loyalty;

        public ProfileViewController() : base("حسابي") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadLoyalty();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            RefreshPinState();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            stack.AddArrangedSubview(Controls.InfoRow("الاسم", Session.FullName));
            stack.AddArrangedSubview(Controls.InfoRow("اسم المستخدم", Session.UserName));
            stack.AddArrangedSubview(Controls.InfoRow("الهاتف", Session.Phone));
            stack.AddArrangedSubview(Controls.InfoRow("الدور", Session.Role));
            stack.AddArrangedSubview(Controls.InfoRow("المحافظة", Session.Governorate));
            stack.AddArrangedSubview(Controls.InfoRow("المدينة", Session.City));

            // نقاط الولاء والعضوية (بديل Api.LoyaltyPoints في ProfileActivity)
            _loyalty = MakeLabel("", 14f, Theme.Primary, UIFontWeight.Semibold, UITextAlignment.Right);
            _loyalty.Hidden = !Session.IsCustomer;
            stack.AddArrangedSubview(_loyalty);

            var edit = Controls.PrimaryButton("تعديل البيانات");
            edit.TouchUpInside += (s, e) => EditProfile();
            stack.AddArrangedSubview(edit);

            var password = Controls.OutlineButton("تغيير كلمة المرور");
            password.TouchUpInside += (s, e) => ChangePassword();
            stack.AddArrangedSubview(password);

            stack.AddArrangedSubview(Controls.SectionTitle("قفل التطبيق"));

            var pinRow = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            _pinState = MakeLabel("", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            _pinSwitch = new UISwitch { OnTintColor = Theme.Primary, TranslatesAutoresizingMaskIntoConstraints = false };
            _pinSwitch.ValueChanged += (s, e) => TogglePin(_pinSwitch.On);

            pinRow.AddSubview(_pinState);
            pinRow.AddSubview(_pinSwitch);
            pinRow.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_pinSwitch, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, pinRow, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_pinSwitch, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, pinRow, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(_pinState, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, pinRow, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(_pinState, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _pinSwitch, NSLayoutAttribute.Trailing, 1, 10),
                NSLayoutConstraint.Create(_pinState, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, pinRow, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(pinRow, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
            });
            stack.AddArrangedSubview(pinRow);

            var changePin = Controls.OutlineButton("تغيير الرقم السري");
            changePin.TouchUpInside += (s, e) => SetPin();
            stack.AddArrangedSubview(changePin);

            var biometric = Controls.OutlineButton("تسجيل البصمة / Face ID");
            biometric.TouchUpInside += (s, e) => TestBiometric();
            stack.AddArrangedSubview(biometric);

            stack.AddArrangedSubview(Controls.SectionTitle("قانوني"));

            var legal = Controls.OutlineButton("المستندات القانونية");
            legal.TouchUpInside += (s, e) => ShowLegal();
            stack.AddArrangedSubview(legal);

            var logout = Controls.DangerButton("تسجيل الخروج");
            logout.TouchUpInside += (s, e) => Logout();
            stack.AddArrangedSubview(logout);

            // الأدمن: اختصار لحسابات الإدارة (بديل btnAdminAccounts) — لا يرى «حذف الحساب»
            if (Session.IsAdmin)
            {
                var admins = Controls.OutlineButton("حسابات الإدارة");
                admins.TouchUpInside += (s, e) => Push(new AdminAccountsViewController());
                stack.AddArrangedSubview(admins);
                return;
            }

            var delete = Controls.DangerButton("حذف الحساب");
            delete.TouchUpInside += (s, e) => DeleteAccount();
            stack.AddArrangedSubview(delete);

            // رابط حذف الحساب عبر الويب (بديل txtDeleteWeb → Legal.DeleteAccountUrl)
            var deleteWeb = Controls.OutlineButton("حذف الحساب عبر الموقع");
            deleteWeb.TouchUpInside += (s, e) => Legal.Open(Legal.DeleteAccountUrl);
            stack.AddArrangedSubview(deleteWeb);
        }

        /// <summary>
        /// نقاط الولاء والمستوى ونقاط الترقية المتبقية (بديل Api.LoyaltyPoints في ProfileActivity).
        /// </summary>
        private void LoadLoyalty()
        {
            if (!Session.IsCustomer) return;

            Ui.Run(() =>
            {
                var points = Api.LoyaltyPoints();

                Ui.BeginOnMain(() =>
                {
                    string level;
                    int next;

                    if (points < 100) { level = "مبتدئ"; next = 100; }
                    else if (points < 300) { level = "فضي"; next = 300; }
                    else if (points < 600) { level = "ذهبي"; next = 600; }
                    else { level = "بلاتيني"; next = points; }

                    var remain = next == points ? 0 : next - points;

                    _loyalty.Text = "نقاطك: " + points + "  |  العضوية: " + level +
                                    (next == points ? " (أعلى مستوى)" : "\nباقي " + remain + " نقطة للترقية");
                });
            }, null, ex =>
            {
                Ui.BeginOnMain(() => _loyalty.Text = "تعذّر تحميل النقاط");
                AppServices.Log("TawselaProfile", "loyalty: " + ex.Message);
            });
        }

        private void RefreshPinState()
        {
            var active = AppLock.IsActive();
            _pinSwitch.On = active;
            _pinState.Text = active ? "مفعّل — يُطلب عند فتح التطبيق" : "غير مفعّل";
            _pinState.TextColor = active ? Theme.Green : Theme.TextSecondary;
        }

        private void TogglePin(bool enabled)
        {
            if (enabled)
            {
                if (!AppLock.HasPin())
                {
                    SetPin();
                    _pinSwitch.On = AppLock.IsActive();
                    return;
                }
                AppLock.SetEnabled(true);
                Ui.Toast("تم تفعيل قفل التطبيق");
            }
            else
            {
                AppLock.SetEnabled(false);
                Ui.Toast("تم إيقاف قفل التطبيق");
            }
            RefreshPinState();
        }

        private void SetPin()
        {
            Ui.Prompt("رقم سري جديد", "من 4 إلى 6 أرقام", "", "••••", pin =>
            {
                if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 6)
                {
                    Ui.Toast("الرقم من 4 إلى 6 أرقام");
                    return;
                }

                Ui.Prompt("تأكيد الرقم السري", "أعد إدخال الرقم", "", "••••", again =>
                {
                    if (pin != again)
                    {
                        Ui.Toast("الرقمان غير متطابقين");
                        return;
                    }

                    try
                    {
                        AppLock.SetPin(pin);
                       Ui.Toast("تم حفظ الرقم السري");
                        RefreshPinState();
                    }
                    catch (Exception ex) { Ui.Toast(ex.Message); }
                }, UIKeyboardType.NumberPad);
            }, UIKeyboardType.NumberPad);
        }

        private void TestBiometric()
        {
            string kind;
            if (!Biometrics.IsAvailable(out kind))
            {
                Ui.Toast("البصمة / Face ID غير متاح على هذا الجهاز");
                return;
            }

            Biometrics.Authenticate("لتفعيل الفتح السريع", (ok, msg) =>
            {
                Ui.Toast(ok ? "تم التحقق بنجاح — " + kind : msg);
            });
        }

        private void EditProfile()
        {
            Ui.Prompt("تعديل الاسم", "الاسم الكامل", Session.FullName, "الاسم", name =>
            {
                Ui.Prompt("تعديل الهاتف", "رقم الهاتف", Session.Phone, "01xxxxxxxxx", phone =>
                {
                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                    Ui.Run(() => Api.UpdateProfile(name, phone), () =>
                    {
                        progress.Dismiss();
                        Session.FullName = name;
                        Session.Phone = phone;
                        Ui.Toast("تم حفظ البيانات");
                        // أعد بناء الشاشة لعرض القيم الجديدة
                        NavigationController?.PopViewController(true);
                        Router.GoHome();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                }, UIKeyboardType.PhonePad);
            });
        }

        private void ChangePassword()
        {
            Ui.Prompt("كلمة المرور الحالية", "", "", "••••••", oldPwd =>
            {
                Ui.Prompt("كلمة المرور الجديدة", "6 أحرف على الأقل", "", "••••••", newPwd =>
                {
                    if ((newPwd ?? "").Length < 6)
                    {
                        Ui.Toast("كلمة المرور 6 أحرف على الأقل");
                        return;
                    }

                    var progress = Ui.ShowProgress("جارٍ التغيير…");
                    Ui.Run(() => Api.ChangePassword(oldPwd, newPwd), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم تغيير كلمة المرور");
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                }, UIKeyboardType.Default);
            }, UIKeyboardType.Default);
        }

        private void ShowLegal()
        {
            Ui.Choose("المستندات القانونية",
                new[] { "سياسة الخصوصية", "شروط الاستخدام", "حذف الحساب" },
                index =>
                {
                    var url = index == 0 ? Legal.PrivacyUrl
                            : index == 1 ? Legal.TermsUrl
                            : Legal.DeleteAccountUrl;
                    Legal.Open(url, err => Ui.Toast(err));
                });
        }

        private void Logout()
        {
            Ui.Confirm("تسجيل الخروج", "سيتم إنهاء الجلسة على هذا الجهاز.", "خروج", () =>
            {
                try { Api.ClearSession(); } catch { }
                try { AppServices.Prefs.Remove("tawsela.remember_user"); } catch { }
                try { InAppNotifications.Stop(); } catch { }
                try { AppLock.ClearBackground(); } catch { }
                Ui.Toast("تم تسجيل الخروج");
                Router.ShowLogin();
            });
        }

        private void DeleteAccount()
        {
            Ui.Confirm("حذف الحساب",
                "سيتم حذف حسابك نهائيًا. لا يمكن التراجع. هل تريد المتابعة؟",
                "حذف نهائي", () =>
                {
                    Ui.Confirm("تأكيد أخير",
                        "قد تفقد رصيد المحفظة المتبقي. متأكد؟",
                        "نعم، احذف", () =>
                        {
                            var progress = Ui.ShowProgress("جارٍ حذف الحساب…");
                            Ui.Run(() => Api.DeleteAccount(true), () =>
                            {
                                progress.Dismiss();
                                try { Api.ClearSession(); } catch { }
                                Ui.Toast("تم حذف الحساب");
                                Router.ShowLogin();
                            }, ex =>
                            {
                                progress.Dismiss();
                                Ui.Toast(ex.Message);
                            });
                        });
                });
        }
    }
}
