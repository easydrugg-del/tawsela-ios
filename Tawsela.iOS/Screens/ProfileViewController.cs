using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/ProfileActivity.cs:
    /// بطاقة نقاط الولاء والعضوية، اسم المستخدم/الاسم الكامل/الهاتف،
    /// حفظ التغييرات، تغيير كلمة المرور، واختصار حسابات المدير.
    /// </summary>
    public class ProfileViewController : BaseViewController
    {
        private UILabel _loyalty;
        private UITextField _fullName, _phone, _oldPwd, _newPwd, _newPwd2;

        public ProfileViewController() : base("ملفي الشخصي") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            LoadLoyalty();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(12f, 16f);

            /* ---------- نقاط الولاء والعضوية ---------- */
            var card = Controls.Card();
            var title = MakeLabel("نقاط الولاء والعضوية", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            _loyalty = MakeLabel("—", 14f, Theme.Primary, UIFontWeight.Semibold, UITextAlignment.Right);
            _loyalty.Lines = 0;
            _loyalty.Hidden = !Session.IsCustomer;

            card.AddSubview(title);
            card.AddSubview(_loyalty);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_loyalty, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_loyalty, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_loyalty, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_loyalty, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });
            stack.AddArrangedSubview(card);

            /* ---------- بيانات الحساب ---------- */
            stack.AddArrangedSubview(MakeLabel("اسم المستخدم", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));
            stack.AddArrangedSubview(Controls.InfoRow("", Session.UserName));

            stack.AddArrangedSubview(MakeLabel("الاسم الكامل", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));
            _fullName = Controls.TextField("الاسم الكامل");
            _fullName.Text = Session.FullName ?? "";
            stack.AddArrangedSubview(_fullName);

            stack.AddArrangedSubview(MakeLabel("رقم الهاتف", 12f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right));
            _phone = Controls.TextField("01xxxxxxxxx");
            _phone.KeyboardType = UIKeyboardType.PhonePad;
            _phone.Text = Session.Phone ?? "";
            stack.AddArrangedSubview(_phone);

            var save = Controls.PrimaryButton("حفظ التغييرات");
            save.TouchUpInside += (s, e) => SaveProfile();
            stack.AddArrangedSubview(save);

            /* ---------- تغيير كلمة المرور ---------- */
            stack.AddArrangedSubview(Controls.SectionTitle("تغيير كلمة المرور"));

            _oldPwd = Controls.TextField("كلمة المرور الحالية");
            _oldPwd.SecureTextEntry = true;
            stack.AddArrangedSubview(_oldPwd);

            _newPwd = Controls.TextField("كلمة المرور الجديدة");
            _newPwd.SecureTextEntry = true;
            stack.AddArrangedSubview(_newPwd);

            _newPwd2 = Controls.TextField("تأكيد كلمة المرور الجديدة");
            _newPwd2.SecureTextEntry = true;
            stack.AddArrangedSubview(_newPwd2);

            var change = Controls.PrimaryButton("تغيير كلمة المرور");
            change.TouchUpInside += (s, e) => ChangePassword();
            stack.AddArrangedSubview(change);

            /* ---------- اختصار حسابات المدير (btnAdminAccounts) ---------- */
            if (Session.IsAdmin)
            {
                var admins = Controls.OutlineButton("إدارة حسابات المدير");
                admins.TouchUpInside += (s, e) => Push(new AdminAccountsViewController());
                stack.AddArrangedSubview(admins);
            }

            /* ---------- حذف الحساب (بديل btnDeleteAccount / txtDeleteWeb) ---------- */
            if (!Session.IsAdmin)
            {
                var del = Controls.OutlineButton("حذف حسابي نهائياً");
                del.SetTitleColor(Theme.Red, UIControlState.Normal);
                del.TouchUpInside += (s, e) => ConfirmDeleteAccount();
                stack.AddArrangedSubview(del);

                var web = MakeLabel("أو اطلب حذف الحساب عبر صفحة الويب", 12f,
                    Theme.Primary, UIFontWeight.Regular, UITextAlignment.Right);
                web.UserInteractionEnabled = true;
                web.AddGestureRecognizer(new UITapGestureRecognizer(
                    () => Legal.Open(Legal.DeleteAccountUrl, err => Ui.Toast(err))));
                stack.AddArrangedSubview(web);
            }
        }

        /// <summary>نفس تحذير ProfileActivity في أندرويد قبل حذف الحساب.</summary>
        private void ConfirmDeleteAccount()
        {
            Ui.Confirm("حذف حسابي نهائياً",
                "سيتم حذف بياناتك الشخصية نهائياً ولن تستطيع تسجيل الدخول مجدداً.\n" +
                "لا يمكن التراجع عن هذه العملية. هل أنت متأكد؟",
                "حذف نهائي",
                () => DeleteAccountNow(false));
        }

        /// <summary>حذف الحساب من الخادم ثم إنهاء الجلسة (مع خيار التنازل عن الرصيد).</summary>
        private void DeleteAccountNow(bool forfeitBalance)
        {
            var progress = Ui.ShowProgress("جاري حذف الحساب...");
            Ui.Run(() => Api.DeleteAccount(forfeitBalance), () =>
            {
                progress.Dismiss();
                try { Api.ClearSession(); } catch { }
                try { AppServices.Prefs.Remove("tawsela.remember_user"); } catch { }
                try { InAppNotifications.Stop(); } catch { }
                try { AppLock.ClearBackground(); } catch { }
                Ui.Toast("تم حذف الحساب");
                Router.ShowLogin();
            }, ex =>
            {
                progress.Dismiss();
                var msg = ex == null ? "تعذّر حذف الحساب" : ex.Message;

                // رصيد متبقٍ: اعرض خيار التنازل صراحةً (نفس أندرويد)
                if (!forfeitBalance && msg.Contains("رصيد"))
                {
                    Ui.Confirm("رصيد متبقٍ في المحفظة",
                        "لا يمكن حذف الحساب مع وجود رصيد. هل توافق على التنازل عن الرصيد المتبقي وحذف الحساب نهائيًا؟\n\n" +
                        "إذا أردت الاحتفاظ بالرصيد، ألغِ الحذف وتواصل مع الإدارة لتسويته أولًا.",
                        "تنازل واحذف",
                        () => DeleteAccountNow(true));
                }
                else
                {
                    Ui.Toast(msg);
                }
            });
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

        private void SaveProfile()
        {
            var name = (_fullName.Text ?? "").Trim();
            var phone = (_phone.Text ?? "").Trim();

            if (name.Length < 3) { Ui.Toast("اكتب الاسم الكامل"); return; }
            if (phone.Length < 8) { Ui.Toast("اكتب رقم هاتف صحيح"); return; }

            var progress = Ui.ShowProgress("جارٍ الحفظ…");
            Ui.Run(() => Api.UpdateProfile(name, phone), () =>
            {
                progress.Dismiss();
                Session.FullName = name;
                Session.Phone = phone;
                Ui.Toast("تم حفظ التغييرات");
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        private void ChangePassword()
        {
            var oldPwd = (_oldPwd.Text ?? "").Trim();
            var newPwd = (_newPwd.Text ?? "").Trim();
            var again = (_newPwd2.Text ?? "").Trim();

            if (oldPwd == "") { Ui.Toast("أدخل كلمة المرور الحالية"); return; }
            if (newPwd.Length < 6) { Ui.Toast("كلمة المرور 6 أحرف على الأقل"); return; }
            if (newPwd != again) { Ui.Toast("الرقمان غير متطابقين"); return; }

            var progress = Ui.ShowProgress("جارٍ التغيير…");
            Ui.Run(() => Api.ChangePassword(oldPwd, newPwd), () =>
            {
                progress.Dismiss();
                _oldPwd.Text = "";
                _newPwd.Text = "";
                _newPwd2.Text = "";
                Ui.Toast("تم تغيير كلمة المرور");
            }, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }
    }
}
