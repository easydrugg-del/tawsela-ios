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
        private UISwitch _availability;
        private UILabel _availabilityState;
        private UILabel _notifBadge;
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

            content.AddArrangedSubview(Controls.SectionTitle(Session.IsDriver ? "الأقسام" : "القائمة الرئيسية"));

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

                // أزرار أسفل الشاشة زي أندرويد
                _grid.AddArrangedSubview(IconFullButton("الملف الشخصي", "ic_person", Theme.Primary,
                    () => Push(new ProfileViewController())));
                _grid.AddArrangedSubview(IconFullButton("الإعدادات", "ic_settings", Theme.TextPrimary,
                    () => Push(new SettingsViewController())));

                var credit = MakeLabel("Designed @ Ahmed Ali", 10f, Theme.TextSecondary,
                    UIFontWeight.Regular, UITextAlignment.Center);
                credit.Alpha = 0.6f;
                _grid.AddArrangedSubview(credit);
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
                Push(new OrdersViewController("", "جديد", "طلبات متاحة"));
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
