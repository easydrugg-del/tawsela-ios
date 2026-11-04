using System;
using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/MainActivity.cs — الشاشة الرئيسية حسب الدور
    /// (عميل / سائق / إدارة) بنفس ألوان وترتيب أندرويد.
    /// </summary>
    public class MainViewController : BaseViewController
    {
        private UILabel _greeting;
        private UILabel _roleBadge;
        private UILabel _driverInfo;
        private decimal _driverBalance = -1m;
        private UISwitch _availability;
        private UILabel _availabilityState;
        private UIControl _bell;
        private UILabel _bellBadge;
        private UIStackView _grid;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public MainViewController() : base() { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.Surface;
            BuildHeader();
            BuildContent();
            ProcessRecurring();
        }

        /// <summary>معالجة الرحلات المتكررة المستحقة عند فتح التطبيق (بديل Api.ProcessDueRecurring).</summary>
        private void ProcessRecurring()
        {
            Ui.Run(() =>
            {
                int created;
                try { created = Api.ProcessDueRecurring(); }
                catch { return; }

                if (created > 0)
                    Ui.BeginOnMain(() => Ui.Toast("تم إنشاء " + created + " رحلة متكررة تلقائيًا"));
            }, null, ex => AppServices.Log("TawselaMain", "recurring: " + ex.Message));
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            RefreshHeader();
        }

        private void BuildHeader()
        {
            var header = Shapes.Header(150f);
            header.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(header);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 150),
            });

            _greeting = MakeLabel("أهلًا بك", 20f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Right);
            _roleBadge = MakeLabel("", 12f, UIColor.White.ColorWithAlpha(0.9f), UIFontWeight.Regular, UITextAlignment.Right);

            // سطر السائق: رقمه + رصيد محفظته (بديل txtDriverNumber/txtDriverBalance)
            _driverInfo = MakeLabel("", 11f, UIColor.White.ColorWithAlpha(0.85f), UIFontWeight.Regular, UITextAlignment.Right);
            _driverInfo.Hidden = !Session.IsDriver;

            header.AddSubview(_greeting);
            header.AddSubview(_roleBadge);
            header.AddSubview(_driverInfo);

            // جرس الإشعارات
            _bell = new UIControl { TranslatesAutoresizingMaskIntoConstraints = false };
            var bellIcon = new UIImageView
            {
                Image = IconFactory.Image("ic_bell", 24f, UIColor.White),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false,
                UserInteractionEnabled = false
            };
            _bellBadge = MakeLabel("", 10f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);
            _bellBadge.BackgroundColor = Theme.BadgeRed;
            _bellBadge.Layer.CornerRadius = 9f;
            _bellBadge.Layer.MasksToBounds = true;
            _bellBadge.TextAlignment = UITextAlignment.Center;
            _bellBadge.Hidden = true;

            _bell.AddSubview(bellIcon);
            _bell.AddSubview(_bellBadge);
            _bell.TouchUpInside += (s, e) => Push(new NotificationsViewController());
            header.AddSubview(_bell);

            var bellTap = new UITapGestureRecognizer(() => Push(new NotificationsViewController()));
            _bell.AddGestureRecognizer(bellTap);

            header.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_greeting, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 16),
                NSLayoutConstraint.Create(_greeting, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _bell, NSLayoutAttribute.Leading, 1, -14),

                NSLayoutConstraint.Create(_roleBadge, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_roleBadge, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(_roleBadge, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, header, NSLayoutAttribute.Leading, 1, 16),

                NSLayoutConstraint.Create(_driverInfo, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _roleBadge, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(_driverInfo, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _roleBadge, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(_driverInfo, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, header, NSLayoutAttribute.Leading, 1, 16),

                NSLayoutConstraint.Create(_bell, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.CenterY, 1, 6),
                NSLayoutConstraint.Create(_bell, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 18),
                NSLayoutConstraint.Create(_bell, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),
                NSLayoutConstraint.Create(_bell, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),

                NSLayoutConstraint.Create(bellIcon, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, _bell, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(bellIcon, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _bell, NSLayoutAttribute.CenterY, 1, 0),

                NSLayoutConstraint.Create(_bellBadge, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _bell, NSLayoutAttribute.Top, 1, -2),
                NSLayoutConstraint.Create(_bellBadge, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _bell, NSLayoutAttribute.Trailing, 1, 2),
                NSLayoutConstraint.Create(_bellBadge, NSLayoutAttribute.Width, NSLayoutRelation.GreaterThanOrEqual, null, NSLayoutAttribute.NoAttribute, 1, 18),
                NSLayoutConstraint.Create(_bellBadge, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 18),
            });
        }

        private void BuildContent()
        {
            var scroll = new UIScrollView { TranslatesAutoresizingMaskIntoConstraints = false, AlwaysBounceVertical = true };
            View.AddSubview(scroll);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 120),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            var content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 14,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            scroll.AddSubview(content);
            scroll.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, 50),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -20),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, -32),
            });

            // زر الطلب السريع (عميل فقط)
            if (!Session.IsDriver)
            {
                var quick = Controls.Tile("ic_taxi", "اطلب توصيلة الآن", Theme.Accent, Theme.PrimaryDark,
                    () => Push(new NewOrderViewController()));
                quick.BackgroundColor = UIColor.Clear;
                content.AddArrangedSubview(quick);
                quick.HeightAnchor.ConstraintEqualTo(92).Active = true;
            }

            // إعلان نشط للمحافظة/المدينة (بديل Api.ActiveAdsList في MainActivity)
            LoadAd(content);

            // مفتاح الاستقبال للسائق
            if (Session.IsDriver)
            {
                var card = Controls.Card();
                _availabilityState = MakeLabel("غير متاح", 14f, Theme.TextSecondary, UIFontWeight.Medium, UITextAlignment.Right);
                _availability = new UISwitch { OnTintColor = Theme.Green, TranslatesAutoresizingMaskIntoConstraints = false };
                _availability.ValueChanged += (s, e) => SetAvailability(_availability.On);

                card.AddSubview(_availabilityState);
                card.AddSubview(_availability);
                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(_availability, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(_availability, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(_availabilityState, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, card, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(_availabilityState, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _availability, NSLayoutAttribute.Trailing, 1, 10),
                    NSLayoutConstraint.Create(_availabilityState, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(card, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 56),
                });
                content.AddArrangedSubview(card);
                LoadAvailability();
            }

            content.AddArrangedSubview(Controls.SectionTitle("الأقسام"));

            _grid = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 12,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            content.AddArrangedSubview(_grid);

            BuildTiles();
        }

        /// <summary>أول إعلان نشط للمحافظة/المدينة (بديل Api.ActiveAdsList).</summary>
        private void LoadAd(UIStackView content)
        {
            var gov = Session.Governorate;
            var city = Session.City;

            Ui.Run(() =>
            {
                List<Ad> ads = null;
                try { ads = Api.ActiveAdsList(gov, city); } catch { }
                if (ads == null || ads.Count == 0) return;

                var ad = ads[0];
                Ui.BeginOnMain(() =>
                {
                    var card = Controls.Card();
                    card.BackgroundColor = Theme.Hex(0xFFF4EC);

                    var title = MakeLabel(string.IsNullOrEmpty(ad.Title) ? "إعلان" : ad.Title,
                        15f, Theme.PrimaryDark, UIFontWeight.Bold, UITextAlignment.Right);
                    var body = MakeLabel(ad.Body, 13f, Theme.TextPrimary, UIFontWeight.Regular, UITextAlignment.Right);

                    card.AddSubview(title);
                    card.AddSubview(body);
                    card.AddConstraints(new[]
                    {
                        NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                        NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                        NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                        NSLayoutConstraint.Create(body, NSLayoutAttribute.Top, NSLayoutRelation.Equal, title, NSLayoutAttribute.Bottom, 1, 4),
                        NSLayoutConstraint.Create(body, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                        NSLayoutConstraint.Create(body, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                        NSLayoutConstraint.Create(body, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                    });

                    if (!string.IsNullOrWhiteSpace(ad.LinkUrl))
                    {
                        var link = ad.LinkUrl;
                        card.UserInteractionEnabled = true;
                        card.AddGestureRecognizer(new UITapGestureRecognizer(() => AppServices.OpenUrl(link)));
                    }

                    content.InsertArrangedSubview(card, 0);
                });
            }, null, ex => AppServices.Log("TawselaMain", "ads: " + ex.Message));
        }

        private void BuildTiles()
        {
            foreach (var v in _grid.ArrangedSubviews)
            {
                _grid.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (Session.IsDriver)
            {
                AddRow(
                    Tile("ic_taxi", "طلبات متاحة", Theme.Blue, Theme.PrimaryDark, OpenDriverOrders),
                    Tile("ic_inbox", "طلباتي", Theme.Accent, Theme.PrimaryDark,
                        () => Push(new OrdersViewController("driver", "", "طلباتي"))));

                AddRow(
                    Tile("ic_wallet", "المحفظة", Theme.Green, Theme.PrimaryDark,
                        () => Push(new WalletViewController())),
                    Tile("ic_bell", "الإشعارات", Theme.Hex(0xEF6C00), Theme.PrimaryDark,
                        () => Push(new NotificationsViewController())));

                AddRow(
                    Tile("ic_person", "حسابي", Theme.Hex(0x8E24AA), Theme.PrimaryDark,
                        () => Push(new ProfileViewController())),
                    Tile("ic_action_receipt", "تقرير الأرباح", Theme.Hex(0x00897B), Theme.PrimaryDark,
                        () => Push(new DriverEarningsReportViewController())));

                _grid.AddArrangedSubview(Controls.SectionTitle("عملي"));

                AddRow(
                    Tile("ic_action_receipt", "إحصائياتي", Theme.Blue, Theme.PrimaryDark,
                        () => Push(new DriverStatsViewController())),
                    Tile("ic_clock", "الورديات", Theme.Accent, Theme.PrimaryDark,
                        () => Push(new DriverShiftsViewController())));

                AddRow(
                    Tile("ic_doc", "مستنداتي", Theme.Hex(0x8E24AA), Theme.PrimaryDark,
                        () => Push(new DriverDocsViewController())),
                    Tile("ic_money", "كشف الحساب", Theme.Green, Theme.PrimaryDark,
                        () => Push(new DriverStatementViewController())));

                AddRow(
                    Tile("ic_work", "الاشتراك", Theme.Hex(0x00897B), Theme.PrimaryDark,
                        () => Push(new SubscriptionViewController())),
                    Tile("ic_action_warning", "الطوارئ", Theme.Red, Theme.PrimaryDark,
                        () => Push(new EmergencyViewController())));
            }
            else
            {
                AddRow(
                    Tile("ic_inbox", "طلباتي", Theme.Accent, Theme.PrimaryDark,
                        () => Push(new OrdersViewController("customer", "", "طلباتي"))),
                    Tile("ic_wallet", "المحفظة", Theme.Green, Theme.PrimaryDark,
                        () => Push(new WalletViewController())));

                AddRow(
                    Tile("ic_bell", "الإشعارات", Theme.Hex(0xEF6C00), Theme.PrimaryDark,
                        () => Push(new NotificationsViewController())),
                    Tile("ic_person", "حسابي", Theme.Hex(0x8E24AA), Theme.PrimaryDark,
                        () => Push(new ProfileViewController())));

                AddRow(
                    Tile("ic_taxi", "توصيلة جديدة", Theme.Hex(0x00897B), Theme.PrimaryDark,
                        () => Push(new NewOrderViewController())),
                    Tile("ic_action_receipt", "إحصائياتي", Theme.Red, Theme.PrimaryDark,
                        () => Push(new CustomerStatsViewController())));

                // إعادة آخر رحلة بضغطة واحدة (بديل btnReorder في MainActivity)
                AddRow(
                    Tile("ic_action_reorder", "إعادة آخر رحلة", Theme.Blue, Theme.PrimaryDark, ReorderLast),
                    Tile("ic_action_map", "سائقون قريبون", Theme.Hex(0x00897B), Theme.PrimaryDark,
                        () => Push(new NearbyDriversViewController())));

                // ---- المكافآت والعروض ----
                _grid.AddArrangedSubview(Controls.SectionTitle("المكافآت والعروض"));

                AddRow(
                    Tile("ic_ticket", "العروض", Theme.Accent, Theme.PrimaryDark,
                        () => Push(new CouponsViewController())),
                    Tile("ic_crown", "نقاط الولاء", Theme.Hex(0xF9A825), Theme.PrimaryDark,
                        () => Push(new LoyaltyViewController())));

                AddRow(
                    Tile("ic_gift", "دعوة الأصدقاء", Theme.Green, Theme.PrimaryDark,
                        () => Push(new ReferralViewController())),
                    Tile("ic_trophy", "التحديات", Theme.Blue, Theme.PrimaryDark,
                        () => Push(new ChallengesViewController())));

                AddRow(
                    Tile("ic_work", "الاشتراك", Theme.Hex(0x8E24AA), Theme.PrimaryDark,
                        () => Push(new SubscriptionViewController())),
                    Tile("ic_action_receipt", "سجل الشحن", Theme.Hex(0x00897B), Theme.PrimaryDark,
                        () => Push(new TopUpHistoryViewController(false))));

                // ---- أخرى ----
                _grid.AddArrangedSubview(Controls.SectionTitle("أخرى"));

                AddRow(
                    Tile("ic_support", "الشكاوى", Theme.Hex(0xEF6C00), Theme.PrimaryDark,
                        () => Push(new ComplaintViewController())),
                    Tile("ic_settings", "الإعدادات", Theme.Gray, Theme.PrimaryDark,
                        () => Push(new SettingsViewController())));

                _grid.AddArrangedSubview(Controls.SectionTitle("خدمات إضافية"));

                AddRow(
                    Tile("ic_pin", "أماكني المحفوظة", Theme.Blue, Theme.PrimaryDark,
                        () => Push(new SavedPlacesViewController())),
                    Tile("ic_clock", "رحلات متكررة", Theme.Accent, Theme.PrimaryDark,
                        () => Push(new RecurringViewController())));

                AddRow(
                    Tile("ic_action_map", "سائقون قريبون", Theme.Hex(0x00897B), Theme.PrimaryDark,
                        () => Push(new NearbyDriversViewController())),
                    Tile("ic_action_warning", "الطوارئ", Theme.Red, Theme.PrimaryDark,
                        () => Push(new EmergencyViewController())));
            }

            // الإدارة
            if (Session.IsAdmin)
            {
                AddRow(
                    Tile("ic_group", "لوحة الإدارة", Theme.Blue, Theme.PrimaryDark,
                        () => Push(new AdminHomeViewController())),
                    Tile("ic_doc", "التقارير", Theme.Accent, Theme.PrimaryDark,
                        () => Push(new AdminHomeViewController())));
            }
        }

        private UIControl Tile(string icon, string title, UIColor light, UIColor dark, Action onTap)
        {
            return Controls.Tile(icon, title, light, dark, onTap);
        }

        private void AddRow(UIControl left, UIControl right)
        {
            var row = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 12,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            row.AddArrangedSubview(left);
            row.AddArrangedSubview(right);
            row.HeightAnchor.ConstraintEqualTo(96).Active = true;
            _grid.AddArrangedSubview(row);
        }

        /// <summary>
        /// فتح الطلبات المتاحة للسائق — ممنوع إذا كان الرصيد صفرًا
        /// (بديل تعطيل btnDriverOrders عند balance &lt;= 0 في MainActivity).
        /// </summary>
        private void OpenDriverOrders()
        {
            // الرصيد لم يُحمّل بعد: نجلبه الآن بدل رفض خاطئ
            if (_driverBalance < 0)
            {
                var progress = Ui.ShowProgress("جارٍ التحقق من الرصيد…");

                Ui.Run(() =>
                {
                    decimal balance = 0m;
                    try { balance = Api.WalletBalance(); } catch { }

                    Ui.BeginOnMain(() =>
                    {
                        progress.Dismiss();
                        _driverBalance = balance;
                        OpenDriverOrders();
                    });
                }, null, ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
                return;
            }

            if (_driverBalance > 0)
            {
                Push(new OrdersViewController("", "جديد", "طلبات متاحة"));
                return;
            }

            Ui.Confirm("الرصيد صفر",
                "لا يمكن استقبال الطلبات قبل شحن المحفظة. هل تريد فتح المحفظة الآن؟",
                "فتح المحفظة",
                () => Push(new WalletViewController()));
        }

        /// <summary>
        /// إعادة آخر رحلة: يجلب آخر طلبات العميل ويفتح شاشة الطلب معبأة
        /// (بديل ReorderLast في MainActivity).
        /// </summary>
        private void ReorderLast()
        {
            var progress = Ui.ShowProgress("جارٍ التحضير…");

            Ui.Run(() =>
            {
                var orders = Api.GetOrders("", "customer");

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    if (orders == null || orders.Count == 0)
                    {
                        Ui.Toast("لا توجد رحلات سابقة");
                        return;
                    }

                    Push(new NewOrderViewController(orders[0]));
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast("خطأ: " + ex.Message);
            });
        }

        private void RefreshHeader()
        {
            if (_greeting != null)
            {
                var name = string.IsNullOrEmpty(Session.FullName) ? Session.UserName : Session.FullName;
                _greeting.Text = "أهلًا " + name;
            }

            if (_roleBadge != null)
            {
                var role = Session.Role;
                if (Session.IsAdmin) role = "إدارة";
                if (Session.IsDriver) role = "سائق";
                if (Session.IsCustomer) role = "عميل";
                _roleBadge.Text = role;
            }

            // شعار الإشعارات غير المقروءة
            Ui.Run(() =>
            {
                try
                {
                    var list = Api.UserNotifications();
                    var unread = 0;
                    if (list != null) foreach (var n in list) if (!n.IsRead) unread++;
                    Ui.BeginOnMain(() =>
                    {
                        _bellBadge.Hidden = unread == 0;
                        _bellBadge.Text = unread > 9 ? "9+" : unread.ToString();
                    });
                }
                catch { }
            });

            // بيانات السائق: الرقم + الرصيد (بديل txtDriverNumber/txtDriverBalance)
            if (Session.IsDriver && _driverInfo != null)
            {
                Ui.Run(() =>
                {
                    decimal balance = 0m;
                    var number = "";
                    try { balance = Api.WalletBalance(); } catch { }
                    try { number = Api.DriverNumber(); } catch { }

                    Ui.BeginOnMain(() =>
                    {
                        _driverBalance = balance;
                        _driverInfo.Text = (string.IsNullOrEmpty(number) ? "" : "رقم السائق: " + number + " · ") +
                                           "رصيدك: " + balance.ToString("0.00") + " ج.م";
                        _driverInfo.TextColor = balance > 0
                            ? UIColor.White.ColorWithAlpha(0.9f)
                            : Theme.Hex(0xFFD2AE);
                    });
                }, null, ex => AppServices.Log("TawselaMain", "driver info: " + ex.Message));
            }
        }

        /* ---------- حالة السائق ---------- */

        private void LoadAvailability()
        {
            Ui.Run(() =>
            {
                string status;
                bool available;
                Api.GetDriverStatus(out status, out available);
                Ui.BeginOnMain(() =>
                {
                    _availability.On = available;
                    _availabilityState.Text = available ? "متاح لاستقبال الطلبات" : "غير متاح";
                    _availabilityState.TextColor = available ? Theme.Green : Theme.TextSecondary;
                });
            }, null, ex => AppServices.Log("TawselaMain", "status: " + ex.Message));
        }

        private void SetAvailability(bool available)
        {
            var coord = LocationService.LastKnown;
            var lat = coord.HasValue ? coord.Value.Latitude : 0;
            var lng = coord.HasValue ? coord.Value.Longitude : 0;

            if (available && (lat == 0 && lng == 0))
            {
                LocationService.RequestPermission(granted =>
                {
                    if (!granted)
                    {
                        Ui.Toast("لازم تسمح بالوصول للموقع لتستقبل الطلبات");
                        _availability.On = false;
                        return;
                    }
                    DoSetAvailability(true);
                });
                return;
            }

            DoSetAvailability(available);
        }

        private void DoSetAvailability(bool available)
        {
            var coord = LocationService.LastKnown;
            var lat = coord.HasValue ? coord.Value.Latitude : 0;
            var lng = coord.HasValue ? coord.Value.Longitude : 0;

            Ui.Run(() => Api.SetAvailability(available, lat, lng), () =>
            {
                _availabilityState.Text = available ? "متاح لاستقبال الطلبات" : "غير متاح";
                _availabilityState.TextColor = available ? Theme.Green : Theme.TextSecondary;
                Ui.Toast(available ? "تم تفعيل استقبال الطلبات" : "تم إيقاف استقبال الطلبات");
            }, ex =>
            {
                _availability.On = !available;
                Ui.Toast(ex.Message);
            });
        }
    }
}
