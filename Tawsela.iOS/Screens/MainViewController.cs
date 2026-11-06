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
        private UILabel _balanceBadge;
        private UIButton _logoutBtn;
        private decimal _driverBalance = -1m;
        private UIButton _btnOnline;
        private bool _online;
        private UILabel _notifBadge;
        private UIStackView _grid;
        private UIView _header;
        private UIView _statusBanner;
        private UILabel _statusLabel;
        private NSLayoutConstraint _statusHeight;
        private UILabel _numberPill;

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
            _header = header;
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

            // شعار الرصيد (بديل txtDriverBalance في أندرويد)
            _balanceBadge = MakeLabel("", 12f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);
            _balanceBadge.BackgroundColor = Theme.BadgeRed;
            _balanceBadge.Layer.CornerRadius = 11f;
            _balanceBadge.Layer.MasksToBounds = true;
            _balanceBadge.TextAlignment = UITextAlignment.Center;
            _balanceBadge.Hidden = true;
            _balanceBadge.Text = "  الرصيد: —  ";

            // زر الخروج الأحمر في الهيدر (بديل btnLogout في أندرويد)
            _logoutBtn = new UIButton(UIButtonType.System)
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _logoutBtn.SetTitle("خروج", UIControlState.Normal);
            _logoutBtn.SetTitleColor(UIColor.White, UIControlState.Normal);
            _logoutBtn.TitleLabel.Font = Theme.FontSemiBold(14);
            _logoutBtn.BackgroundColor = Theme.Red;
            _logoutBtn.Layer.CornerRadius = 16f;
            _logoutBtn.Layer.MasksToBounds = true;
            _logoutBtn.ContentEdgeInsets = new UIEdgeInsets(8, 18, 8, 18);
            _logoutBtn.TouchUpInside += (s, e) => Logout();

            header.AddSubview(_logoutBtn);
            header.AddSubview(_balanceBadge);

            header.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_greeting, NSLayoutAttribute.Top, NSLayoutRelation.Equal, header.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 16),
                // العنوان على اليمين وزر الخروج على الشمال (زي الأندرويد)
                NSLayoutConstraint.Create(_greeting, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, header, NSLayoutAttribute.Trailing, 1, -18),
                NSLayoutConstraint.Create(_greeting, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, _balanceBadge, NSLayoutAttribute.Trailing, 1, 10),

                NSLayoutConstraint.Create(_balanceBadge, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(_balanceBadge, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, _logoutBtn, NSLayoutAttribute.Trailing, 1, 8),
                NSLayoutConstraint.Create(_balanceBadge, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 22),

                NSLayoutConstraint.Create(_roleBadge, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(_roleBadge, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(_roleBadge, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, header, NSLayoutAttribute.Leading, 1, 16),

                NSLayoutConstraint.Create(_driverInfo, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _roleBadge, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(_driverInfo, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _roleBadge, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(_driverInfo, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, header, NSLayoutAttribute.Leading, 1, 16),

                NSLayoutConstraint.Create(_logoutBtn, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _greeting, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(_logoutBtn, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, header, NSLayoutAttribute.Leading, 1, 18),
                NSLayoutConstraint.Create(_logoutBtn, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 36),
            });

            // شارة الدور مخفية زي أندرويد (الهيدر: الاسم + الرصيد + خروج)
            _roleBadge.Hidden = true;
        }

        private void BuildContent()
        {
            // شريط حالة الحساب (بديل txtAccountStatus في أندرويد) — يظهر للسائق فقط
            if (Session.IsDriver)
            {
                _statusBanner = new UIView
                {
                    BackgroundColor = Theme.Accent,
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    Hidden = true
                };
                _statusLabel = MakeLabel("", 13.5f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);
                _statusLabel.Lines = 0;
                _statusBanner.AddSubview(_statusLabel);

                View.AddSubview(_statusBanner);
                _statusHeight = _statusBanner.HeightAnchor.ConstraintEqualTo(0f);
                View.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(_statusBanner, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _header, NSLayoutAttribute.Bottom, 1, 0),
                    NSLayoutConstraint.Create(_statusBanner, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                    NSLayoutConstraint.Create(_statusBanner, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                    _statusHeight,
                    NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _statusBanner, NSLayoutAttribute.Top, 1, 8),
                    NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _statusBanner, NSLayoutAttribute.Bottom, 1, -8),
                    NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _statusBanner, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(_statusLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _statusBanner, NSLayoutAttribute.Trailing, 1, -12),
                });
            }

            var scroll = new UIScrollView { TranslatesAutoresizingMaskIntoConstraints = false, AlwaysBounceVertical = true };
            View.AddSubview(scroll);
            View.AddConstraints(new[]
            {
                Session.IsDriver
                    ? NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _statusBanner, NSLayoutAttribute.Bottom, 1, 0)
                    : NSLayoutConstraint.Create(scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _header, NSLayoutAttribute.Bottom, 1, 0),
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
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Top, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Top, 1, 12),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Bottom, 1, -20),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(content, NSLayoutAttribute.Width, NSLayoutRelation.Equal, scroll, NSLayoutAttribute.Width, 1, -32),
            });

            // بطاقة «رايح فين؟» (بديل cardWhereTo في أندرويد)
            if (!Session.IsDriver)
            {
                var whereCard = Controls.Card();
                var whereTitle = MakeLabel("رايح فين؟", 22f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);

                var go = new UIControl { TranslatesAutoresizingMaskIntoConstraints = false };
                go.Layer.CornerRadius = 16f;
                go.Layer.MasksToBounds = true;

                var goBg = new GradientView
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    Colors = new[] { Theme.Hex(0x424242), Theme.Hex(0x616161) },
                    Start = new CGPoint(0, 0.5),
                    End = new CGPoint(1, 0.5)
                };
                var goText = MakeLabel("اضغط لطلب توصيلة جديدة", 16f, UIColor.White,
                    UIFontWeight.Bold, UITextAlignment.Center);

                go.AddSubview(goBg);
                go.AddSubview(goText);
                go.TouchUpInside += (s, e) => Push(new NewOrderViewController());

                whereCard.AddSubview(whereTitle);
                whereCard.AddSubview(go);
                whereCard.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(whereTitle, NSLayoutAttribute.Top, NSLayoutRelation.Equal, whereCard, NSLayoutAttribute.Top, 1, 18),
                    NSLayoutConstraint.Create(whereTitle, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, whereCard, NSLayoutAttribute.Leading, 1, 18),
                    NSLayoutConstraint.Create(whereTitle, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, whereCard, NSLayoutAttribute.Trailing, 1, -18),

                    NSLayoutConstraint.Create(go, NSLayoutAttribute.Top, NSLayoutRelation.Equal, whereTitle, NSLayoutAttribute.Bottom, 1, 12),
                    NSLayoutConstraint.Create(go, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, whereCard, NSLayoutAttribute.Leading, 1, 18),
                    NSLayoutConstraint.Create(go, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, whereCard, NSLayoutAttribute.Trailing, 1, -18),
                    NSLayoutConstraint.Create(go, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, whereCard, NSLayoutAttribute.Bottom, 1, -18),
                    NSLayoutConstraint.Create(go, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 52),

                    NSLayoutConstraint.Create(goBg, NSLayoutAttribute.Top, NSLayoutRelation.Equal, go, NSLayoutAttribute.Top, 1, 0),
                    NSLayoutConstraint.Create(goBg, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, go, NSLayoutAttribute.Bottom, 1, 0),
                    NSLayoutConstraint.Create(goBg, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, go, NSLayoutAttribute.Leading, 1, 0),
                    NSLayoutConstraint.Create(goBg, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, go, NSLayoutAttribute.Trailing, 1, 0),

                    NSLayoutConstraint.Create(goText, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, go, NSLayoutAttribute.CenterX, 1, 0),
                    NSLayoutConstraint.Create(goText, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, go, NSLayoutAttribute.CenterY, 1, 0),
                });

                content.AddArrangedSubview(whereCard);
            }

            // إعلان نشط للمحافظة/المدينة (بديل Api.ActiveAdsList في MainActivity)
            LoadAd(content);

            // حالة الاستقبال أصبحت بطاقة داخل «لوحة الكابتن» (نفس أندرويد)
            if (Session.IsDriver)
            {
                LoadAvailability();
                LoadDriverStatus();
            }

            if (!Session.IsDriver)
                content.AddArrangedSubview(Controls.SectionTitle("القائمة الرئيسية"));

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
                // ===== لوحة الكابتن: نفس بطاقات وأزرار أندرويد =====

                // العنوان + رقم السائق (بديل txtDriverNumber)
                var head = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
                var capTitle = MakeLabel("لوحة الكابتن", 17f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);

                _numberPill = MakeLabel("  رقم: —  ", 13f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);
                _numberPill.BackgroundColor = Theme.BadgeRed;
                _numberPill.Layer.CornerRadius = 13f;
                _numberPill.Layer.MasksToBounds = true;
                _numberPill.Hidden = true;

                head.AddSubview(capTitle);
                head.AddSubview(_numberPill);
                head.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(capTitle, NSLayoutAttribute.Top, NSLayoutRelation.Equal, head, NSLayoutAttribute.Top, 1, 2),
                    NSLayoutConstraint.Create(capTitle, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, head, NSLayoutAttribute.Leading, 1, 4),
                    NSLayoutConstraint.Create(capTitle, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, head, NSLayoutAttribute.Trailing, 1, -4),

                    NSLayoutConstraint.Create(_numberPill, NSLayoutAttribute.Top, NSLayoutRelation.Equal, capTitle, NSLayoutAttribute.Bottom, 1, 6),
                    NSLayoutConstraint.Create(_numberPill, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, head, NSLayoutAttribute.Trailing, 1, -4),
                    NSLayoutConstraint.Create(_numberPill, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, head, NSLayoutAttribute.Bottom, 1, 0),
                    NSLayoutConstraint.Create(_numberPill, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 26),
                });
                _grid.AddArrangedSubview(head);

                // 🟢 حالة الاستقبال
                _btnOnline = Controls.PrimaryButton("تفعيل الاستقبال (غير متصل)", 60f);
                _btnOnline.TouchUpInside += (s, e) => SetAvailability(!_online);
                _grid.AddArrangedSubview(CardSection("🟢 حالة الاستقبال", 14f, _btnOnline));

                // 📥 التوصيلات
                _grid.AddArrangedSubview(CardSection("📥 التوصيلات", 12f, TileRow(
                    Tile("ic_taxi", "توصيلاتي (المنتهية)", BadgeOrangeLight, BadgeOrangeDark,
                        () => Push(new OrdersViewController("driver", "", "توصيلاتي"))),
                    Tile("ic_inbox", "الواردة (الجديدة)", TealLight, TealDark, OpenDriverOrders))));

                // 🏪 توصيلات المطاعم
                _grid.AddArrangedSubview(CardSection("🏪 توصيلات المطاعم", 12f,
                    RowTile("ic_home", "توصيلات المطاعم", BadgePurpleLight, BadgePurpleDark,
                        () => Push(new RestaurantRequestsViewController()))));

                // 🗺️ الشغل والأرباح
                _grid.AddArrangedSubview(CardSection("🗺️ الشغل والأرباح", 12f, TileRow(
                    Tile("ic_money", "الأرباح", TealLight, TealDark,
                        () => Push(new DriverStatsViewController())))));

                // 👤 حسابي
                _grid.AddArrangedSubview(CardSection("👤 حسابي", 12f,
                    RowTile("ic_wallet", "المحفظة / شحن الرصيد", BadgeGreenLight, BadgeGreenDark,
                        () => Push(new WalletViewController())),
                    RowTile("ic_bell", "الإشعارات", BadgePurpleLight, BadgePurpleDark,
                        () => Push(new NotificationsViewController())),
                    RowTile("ic_doc", "إعادة إرسال الوثائق", IndigoLight, IndigoDark,
                        () => Push(new DriverDocsViewController())),
                    RowTile("ic_clock", "النوبات", IndigoLight, IndigoDark,
                        () => Push(new DriverShiftsViewController()))));

                // زر الطوارئ SOS
                var sos = Controls.IconButton("زر الطوارئ SOS", "ic_action_warning", Theme.Red);
                sos.HeightAnchor.ConstraintEqualTo(58f).Active = true;
                sos.TouchUpInside += (s, e) => Push(new EmergencyViewController());
                _grid.AddArrangedSubview(sos);
            }
            else
            {
                // ===== نفس ترتيب وزراير صفحة العميل في أندرويد بالضبط =====

                // 🚕 توصيلاتي
                _grid.AddArrangedSubview(SectionCard("🚕 توصيلاتي", new[]
                {
                    new[]
                    {
                        Tile("ic_taxi", "توصيلاتي", BadgeOrangeLight, BadgeOrangeDark,
                            () => Push(new OrdersViewController("customer", "", "طلباتي"))),
                        Tile("ic_action_reorder", "توصيلة زي آخر مرة", BadgeOrangeLight, BadgeOrangeDark,
                            ReorderLast),
                    }
                }));

                // 💰 المحفظة والمفضلة والإشعارات
                _grid.AddArrangedSubview(SectionCard("💰 المحفظة والمفضلة والإشعارات", new[]
                {
                    new[]
                    {
                        Tile("ic_wallet", "محفظتي", BadgeGreenLight, BadgeGreenDark,
                            () => Push(new WalletViewController())),
                        Tile("ic_star", "المفضلون", BadgeGreenLight, BadgeGreenDark, ShowFavorites),
                    },
                    new[]
                    {
                        Tile("ic_gift", "العروض والكوبونات", BadgePurpleLight, BadgePurpleDark,
                            () => Push(new CouponsViewController())),
                        NotificationsTile(),
                    }
                }));

                // ⭐ المكافآت والدعم
                _grid.AddArrangedSubview(SectionCard("⭐ المكافآت والدعم", new[]
                {
                    new[]
                    {
                        Tile("ic_group", "دعوة الأصدقاء", BadgeDeepLight, BadgeDeepDark,
                            () => Push(new ReferralViewController())),
                        Tile("ic_trophy", "نقاط الولاء", BadgeDeepLight, BadgeDeepDark,
                            () => Push(new LoyaltyViewController())),
                    },
                    new[]
                    {
                        NewBadge(Tile("ic_crown", "اشتراك VIP", BadgeDeepLight, BadgeDeepDark,
                            () => Push(new SubscriptionViewController()))),
                        Tile("ic_support", "الدعم", BadgeDeepLight, BadgeDeepDark,
                            () => Push(new ComplaintViewController())),
                    },
                    new[]
                    {
                        NewBadge(Tile("ic_flag", "تحديات الأسبوع", BadgeDeepLight, BadgeDeepDark,
                            () => Push(new ChallengesViewController()))),
                    }
                }));

            }

            // أزرار أسفل الشاشة زي أندرويد (لكل الأدوار)
            _grid.AddArrangedSubview(IconFullButton("ملفي الشخصي", "ic_person", Theme.Primary,
                () => Push(new ProfileViewController())));
            _grid.AddArrangedSubview(IconFullButton("الإعدادات", "ic_settings", Theme.TextPrimary,
                () => Push(new SettingsViewController())));

            var credit = MakeLabel("Designed @ Ahmed Ali", 10f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Center);
            credit.Alpha = 0.6f;
            _grid.AddArrangedSubview(credit);

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

        /* ---------- بلاطات صفحة العميل (نفس شكل أندرويد) ---------- */

        // ألوان شارات أيقونات أندرويد (drawable/badge_*)
        private static readonly UIColor BadgeOrangeLight = Theme.Hex(0xFF9A3C);
        private static readonly UIColor BadgeOrangeDark = Theme.Hex(0xE85300);
        private static readonly UIColor BadgeGreenLight = Theme.Hex(0x81C784);
        private static readonly UIColor BadgeGreenDark = Theme.Hex(0x2E7D32);
        private static readonly UIColor BadgePurpleLight = Theme.Hex(0xCE93D8);
        private static readonly UIColor BadgePurpleDark = Theme.Hex(0x6A1B9A);
        private static readonly UIColor BadgeDeepLight = Theme.Hex(0xFF8A65);
        private static readonly UIColor BadgeDeepDark = Theme.Hex(0xBF360C);
        private static readonly UIColor TealLight = Theme.Hex(0x80CBC4);
        private static readonly UIColor TealDark = Theme.Hex(0x00897B);
        private static readonly UIColor IndigoLight = Theme.Hex(0x9FA8DA);
        private static readonly UIColor IndigoDark = Theme.Hex(0x3949AB);

        /// <summary>بطاقة قسم زي أندرويد: عنوان + صفوف بلاطات (2 في السطر).</summary>
        private UIView SectionCard(string title, UIView[][] rows)
        {
            var card = Controls.Card();
            var stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            card.AddSubview(stack);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 12),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -12),
            });

            stack.AddArrangedSubview(MakeLabel(title, 15f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right));

            foreach (var tiles in rows)
            {
                var row = new UIStackView
                {
                    Axis = UILayoutConstraintAxis.Horizontal,
                    Spacing = 8,
                    Distribution = UIStackViewDistribution.FillEqually,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };

                foreach (var t in tiles)
                {
                    t.HeightAnchor.ConstraintEqualTo(88f).Active = true;
                    row.AddArrangedSubview(t);
                }

                stack.AddArrangedSubview(row);
            }

            return card;
        }

        /// <summary>بطاقة بعنوان + أي عناصر (ارتفاع كل عنصر تحدده هي) — نفس بطاقات أندرويد.</summary>
        private UIView CardSection(string title, float padding, params UIView[] items)
        {
            var card = Controls.Card();
            var stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            card.AddSubview(stack);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -padding),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -padding),
            });

            stack.AddArrangedSubview(MakeLabel(title, 15f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right));
            foreach (var item in items) stack.AddArrangedSubview(item);
            return card;
        }

        /// <summary>صف بلاطات بارتفاع 88 (بديل LinearLayout horizontal + weightSum في أندرويد).</summary>
        private UIView TileRow(params UIView[] tiles)
        {
            var row = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            foreach (var t in tiles)
            {
                t.HeightAnchor.ConstraintEqualTo(88f).Active = true;
                row.AddArrangedSubview(t);
            }
            return row;
        }

        /// <summary>سطر داخل بطاقة: أيقونة دائرية على اليمين + نص (بديل btnWalletDriver...).</summary>
        private UIControl RowTile(string icon, string title, UIColor light, UIColor dark, Action onTap)
        {
            var row = new UIControl { TranslatesAutoresizingMaskIntoConstraints = false };
            row.BackgroundColor = Theme.White;
            row.Layer.CornerRadius = Theme.CornerTile;
            row.Layer.BorderWidth = 1f;
            row.Layer.BorderColor = Theme.CardStroke.CGColor;
            row.Layer.MasksToBounds = true;

            var circle = Shapes.BadgeCircle(40f, light, dark);
            circle.TranslatesAutoresizingMaskIntoConstraints = false;
            circle.UserInteractionEnabled = false;

            var img = new UIImageView
            {
                Image = IconFactory.Image(icon, 20f, UIColor.White),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false,
                UserInteractionEnabled = false
            };
            circle.AddSubview(img);

            var label = MakeLabel(title, 15f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Right);
            label.UserInteractionEnabled = false;

            row.AddSubview(circle);
            row.AddSubview(label);
            row.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(row, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 64),

                NSLayoutConstraint.Create(circle, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(circle, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, row, NSLayoutAttribute.Trailing, 1, -14),
                NSLayoutConstraint.Create(circle, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
                NSLayoutConstraint.Create(circle, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),

                NSLayoutConstraint.Create(img, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, circle, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(img, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, circle, NSLayoutAttribute.CenterY, 1, 0),

                NSLayoutConstraint.Create(label, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, circle, NSLayoutAttribute.Leading, 1, -10),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, row, NSLayoutAttribute.Leading, 1, 14),
            });

            if (onTap != null) row.TouchUpInside += (s, e) => onTap();
            return row;
        }

        /// <summary>يغلّف بلاطة ويضع عليها شارة صغيرة (بديل badge_new / txtNotifBadge).</summary>
        private UIView BadgeWrap(UIControl tile, UILabel badge, float height, float minWidth)
        {
            var wrap = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            tile.TranslatesAutoresizingMaskIntoConstraints = false;
            badge.TranslatesAutoresizingMaskIntoConstraints = false;
            badge.TextAlignment = UITextAlignment.Center;
            badge.TextColor = UIColor.White;
            badge.Layer.MasksToBounds = true;

            wrap.AddSubview(tile);
            wrap.AddSubview(badge);

            wrap.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(tile, NSLayoutAttribute.Top, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(tile, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(tile, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(tile, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Trailing, 1, 0),

                NSLayoutConstraint.Create(badge, NSLayoutAttribute.Top, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Top, 1, 4),
                NSLayoutConstraint.Create(badge, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, wrap, NSLayoutAttribute.Leading, 1, 4),
                NSLayoutConstraint.Create(badge, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, height),
                NSLayoutConstraint.Create(badge, NSLayoutAttribute.Width, NSLayoutRelation.GreaterThanOrEqual, null, NSLayoutAttribute.NoAttribute, 1, minWidth),
            });

            return wrap;
        }

        /// <summary>شارة «جديد» زي أندرويد على اشتراك VIP وتحديات الأسبوع.</summary>
        private UIView NewBadge(UIControl tile)
        {
            var badge = new UILabel
            {
                Text = "جديد",
                Font = Theme.Font(10),
                BackgroundColor = Theme.TextPrimary,
                LineBreakMode = UILineBreakMode.Clip
            };
            badge.Layer.CornerRadius = 8f;
            return BadgeWrap(tile, badge, 16f, 28f);
        }

        /// <summary>بلاطة الإشعارات مع شارة عدد غير المقروء (بديل txtNotifBadge).</summary>
        private UIView NotificationsTile()
        {
            var tile = Tile("ic_bell", "الإشعارات", BadgePurpleLight, BadgePurpleDark,
                () => Push(new NotificationsViewController()));

            _notifBadge = new UILabel
            {
                Text = "",
                Font = Theme.Font(11),
                BackgroundColor = Theme.BadgeRed,
                Hidden = true
            };
            _notifBadge.Layer.CornerRadius = 10f;
            return BadgeWrap(tile, _notifBadge, 20f, 20f);
        }

        /// <summary>زر عريض بأيقونة أسفل الشاشة (بديل btnProfile/btnSettings).</summary>
        private UIControl IconFullButton(string title, string icon, UIColor bg, Action onTap)
        {
            return Controls.IconButton(title, icon, bg);
        }

        /// <summary>سائقون مفضلون مع الاتصال (بديل ShowFavorites في أندرويد).</summary>
        private void ShowFavorites()
        {
            var progress = Ui.ShowProgress("جارٍ التحميل…");

            Ui.Run(() =>
            {
                var favs = Api.FavoriteDrivers() ?? new List<Driver>();
                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    if (favs.Count == 0)
                    {
                        Ui.Toast("لا يوجد سائقون مفضلون بعد");
                        return;
                    }

                    var items = new List<string>();
                    foreach (var d in favs)
                        items.Add(d.FullName + " — " + d.Phone + "\n" + d.CarModel + " (" + d.CarPlate + ")");

                    Ui.Choose("سائقيّ المفضلون", items, index =>
                    {
                        var phone = favs[index].Phone;
                        if (!string.IsNullOrEmpty(phone))
                            AppServices.OpenUrl("tel:" + phone);
                    });
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast("خطأ: " + ex.Message);
            });
        }

        /// <summary>تسجيل الخروج من الهيدر (بديل btnLogout).</summary>
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
                Push(new OrdersViewController("", "جديد", "التوصيلات الواردة", "لا توجد توصيلات حالياً"));
                return;
            }

            Ui.Confirm("الرصيد صفر",
                "لا يمكن استقبال الطلبات قبل شحن المحفظة. هل تريد فتح المحفظة الآن؟",
                "فتح المحفظة",
                () => Push(new WalletViewController()));
        }

        /// <summary>
        /// توصيلة زي آخر مرة: يجلب آخر طلبات العميل ويفتح شاشة الطلب معبأة
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

                    // نمرّر آخر رحلة بالطريقتين لضمان وصول البيانات للشاشة
                    // (نفس Intent extras prefFrom/prefTo في أندرويد).
                    var order = orders[0];
                    var screen = new NewOrderViewController(order);
                    screen.SetPrefill(order);
                    Push(screen);
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
                _greeting.Text = "أهلاً " + name;
            }

            if (_roleBadge != null)
            {
                var role = Session.Role;
                if (Session.IsAdmin) role = "إدارة";
                if (Session.IsDriver) role = "سائق";
                if (Session.IsCustomer) role = "عميل";
                _roleBadge.Text = role;
            }

            // شعار الإشعارات غير المقروءة (على بلاطة الإشعارات زي أندرويد)
            Ui.Run(() =>
            {
                try
                {
                    var list = Api.UserNotifications();
                    var unread = 0;
                    if (list != null) foreach (var n in list) if (!n.IsRead) unread++;
                    Ui.BeginOnMain(() =>
                    {
                        if (_notifBadge == null) return;
                        _notifBadge.Hidden = unread == 0;
                        _notifBadge.Text = unread > 9 ? "9+" : unread.ToString();
                    });
                }
                catch { }
            });

            // رصيد المحفظة في شارة الهيدر (بديل LoadBalanceBadge/LoadDriverBalance)
            if (ReleaseConfig.CustomerWalletEnabled || Session.IsDriver)
            {
                Ui.Run(() =>
                {
                    decimal bal = 0m;
                    try { bal = Api.WalletBalance(); } catch { return; }
                    Ui.BeginOnMain(() =>
                    {
                        if (_balanceBadge == null) return;
                        _balanceBadge.Text = "  الرصيد: " + bal.ToString("0") + " ج.م  ";
                        _balanceBadge.Hidden = false;
                    });
                }, null, ex => AppServices.Log("TawselaMain", "balance: " + ex.Message));
            }

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
                        _driverInfo.Text = "رصيدك: " + balance.ToString("0.00") + " ج.م";
                        _driverInfo.TextColor = balance > 0
                            ? UIColor.White.ColorWithAlpha(0.9f)
                            : Theme.Hex(0xFFD2AE);

                        // رقم السائق تحت «لوحة الكابتن» (بديل txtDriverNumber)
                        if (_numberPill != null && !string.IsNullOrEmpty(number))
                        {
                            _numberPill.Text = "  رقم: " + number + "  ";
                            _numberPill.Hidden = false;
                        }
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
                    _online = available;
                    ApplyOnlineButton();
                });
            }, null, ex => AppServices.Log("TawselaMain", "status: " + ex.Message));
        }

        /// <summary>شريط حالة حساب السائق (بديل txtAccountStatus في أندرويد).</summary>
        private void LoadDriverStatus()
        {
            Ui.Run(() =>
            {
                string status;
                bool available;
                try { Api.GetDriverStatus(out status, out available); }
                catch { return; }

                Ui.BeginOnMain(() =>
                {
                    if (_statusBanner == null || _statusLabel == null) return;

                    string text;
                    UIColor bg;
                    if (status == "نشط")
                    {
                        text = "✅ حسابك نشط — جاهز لاستقبال التوصيلات";
                        bg = Theme.Hex(0x28A745);
                    }
                    else if (status == "قيد_المراجعة" || string.IsNullOrEmpty(status))
                    {
                        text = "⏳ حسابك قيد المراجعة — لا يمكنك استقبال التوصيلات حتى موافقة الإدارة";
                        bg = Theme.Hex(0xDC3545);
                    }
                    else if (status == "مرفوض")
                    {
                        text = "❌ حسابك مرفوض — تواصل مع الإدارة";
                        bg = Theme.Hex(0xDC3545);
                    }
                    else if (status == "موقوف")
                    {
                        text = "⛔ حسابك موقوف — تواصل مع الإدارة";
                        bg = Theme.Hex(0xDC3545);
                    }
                    else
                    {
                        text = "⚠️ حالة الحساب: " + status;
                        bg = Theme.Hex(0xDC3545);
                    }

                    _statusLabel.Text = text;
                    _statusBanner.BackgroundColor = bg;
                    _statusBanner.Hidden = false;
                    if (_statusHeight != null) _statusHeight.Active = false;
                    View.SetNeedsLayout();
                });
            }, null, ex => AppServices.Log("TawselaMain", "account status: " + ex.Message));
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
                        ApplyOnlineButton();
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
                _online = available;
                ApplyOnlineButton();
                Ui.Toast(available ? "أصبحت متاحاً لاستقبال التوصيلات" : "أصبحت غير متصل");
            }, ex =>
            {
                ApplyOnlineButton();
                Ui.Toast(ex.Message);
            });
        }

        /// <summary>نص ولون زر الاستقبال (بديل UpdateOnlineButton في أندرويد).</summary>
        private void ApplyOnlineButton()
        {
            if (_btnOnline == null) return;
            _btnOnline.SetTitle(_online ? "إيقاف الاستقبال (متصل)" : "تفعيل الاستقبال (غير متصل)",
                UIControlState.Normal);
            _btnOnline.BackgroundColor = _online ? Theme.Green : Theme.Primary;
        }
    }
}
