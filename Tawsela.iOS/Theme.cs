using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// الهوية البصرية — نفس قيم Resources/values/colors.xml بالحرف حتى تتطابق المنصتان.
    /// المقاسات: 1dp في أندرويد = 1pt في iOS (كثافة مستقلة).
    /// </summary>
    public static class Theme
    {
        /* ---------- الألوان (مطابقة colors.xml) ---------- */

        public static readonly UIColor Primary = Hex(0xFF6A00);

        public static readonly UIColor PrimaryDark = Hex(0xC44E00);

        public static readonly UIColor Accent = Hex(0xFF8C1A);

        public static readonly UIColor AccentDark = Hex(0xC44E00);

        public static readonly UIColor White = UIColor.White;

        public static readonly UIColor Black = UIColor.Black;

        public static readonly UIColor GrayLight = Hex(0xF7F8FA);

        public static readonly UIColor Gray = Hex(0x9E9E9E);

        public static readonly UIColor Red = Hex(0xD32F2F);

        public static readonly UIColor Green = Hex(0x388E3C);

        public static readonly UIColor Blue = Hex(0x1976D2);

        public static readonly UIColor Surface = Hex(0xEAEDED);

        public static readonly UIColor CardStroke = Hex(0xD5D9D9);

        public static readonly UIColor TextPrimary = Hex(0x1C1C1C);

        public static readonly UIColor TextSecondary = Hex(0x565959);

        /* ---------- ألوان مشتقة من drawable ---------- */

        /// <summary>#FFF1E3 — خلفية الضغط في tile_*.xml</summary>
        public static readonly UIColor TilePressed = Hex(0xFFF1E3);

        /// <summary>#FFF4EC — خلفية الضغط في chip_bg.xml</summary>
        public static readonly UIColor ChipPressed = Hex(0xFFF4EC);

        /// <summary>#FDECEC — خلفية الضغط في chip_red_bg.xml</summary>
        public static readonly UIColor ChipRedPressed = Hex(0xFDECEC);

        /// <summary>#E57373 — حدود chip_red_bg.xml</summary>
        public static readonly UIColor ChipRedStroke = Hex(0xE57373);

        /// <summary>#FFD2AE — حدود area_tile_bg.xml</summary>
        public static readonly UIColor AreaTileStroke = Hex(0xFFD2AE);

        /// <summary>#F44336 — btn_badge_bg.xml</summary>
        public static readonly UIColor BadgeRed = Hex(0xF44336);

        /* ---------- المقاسات ---------- */

        public const float CornerTile = 16f;      // tile_*_selector
        public const float CornerChip = 18f;      // chip_bg
        public const float CornerChipRed = 12f;   // chip_red_bg
        public const float CornerArea = 10f;      // area_tile_bg
        public const float CornerEdit = 12f;      // edit_rounded
        public const float CornerRipple = 14f;    // card_ripple
        public const float CornerSearch = 24f;    // bg_search

        /// <summary>اسم خط عربي مخصص إن وُجد داخل الحزمة (مثال: Cairo).</summary>
        public static string CustomFontName = null;

        /* ---------- الخطوط ---------- */

        /// <summary>خط النظام مع وزن. يعرض العربية بجودة عالية على iOS.</summary>
        public static UIFont Font(float size, UIFontWeight weight = UIFontWeight.Regular)
        {
            if (!string.IsNullOrEmpty(CustomFontName))
            {
                var custom = UIFont.FromName(CustomFontName, size);
                if (custom != null) return custom;
            }
            return UIFont.SystemFontOfSize(size, weight);
        }

        public static UIFont FontBold(float size) { return Font(size, UIFontWeight.Bold); }

        public static UIFont FontSemiBold(float size) { return Font(size, UIFontWeight.Semibold); }

        public static UIFont FontMedium(float size) { return Font(size, UIFontWeight.Medium); }

        /* ---------- التنسيق العام ---------- */

        /// <summary>
        /// يُطبَّق مرة واحدة عند الإقلاع: يثبّت الاتجاه من اليمين لليسار لكل التطبيق
        /// حتى لا يختلف الشكل بين جهاز عربي وجهاز إنجليزي (التطبيق عربي 100%).
        /// </summary>
        public static void ApplyGlobal()
        {

            var nav = UINavigationBar.Appearance;
            nav.BarTintColor = Primary;
            nav.TintColor = UIColor.White;
            nav.Translucent = false;
            nav.BarStyle = UIBarStyle.Black;
            nav.TitleTextAttributes = new UIStringAttributes
            {
                ForegroundColor = UIColor.White,
                Font = FontBold(17)
            };
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var appearance = new UINavigationBarAppearance();
                appearance.ConfigureWithOpaqueBackground();
                appearance.BackgroundColor = Primary;
                appearance.TitleTextAttributes = new UIStringAttributes
                {
                    ForegroundColor = UIColor.White,
                    Font = FontBold(17)
                };
                nav.StandardAppearance = appearance;
                nav.ScrollEdgeAppearance = appearance;
            }

            // شريط الحالة أبيض على الخلفية البرتقالية
            UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.LightContent, false);
        }

        /* ---------- أدوات ---------- */

        public static UIColor Hex(uint rgb, float alpha = 1f)
        {
            return UIColor.FromRGBA(
                (byte)((rgb >> 16) & 0xFF),
                (byte)((rgb >> 8) & 0xFF),
                (byte)(rgb & 0xFF),
                (byte)(Math.Max(0f, Math.Min(1f, alpha)) * 255f));
        }

        /// <summary>تحويل لون ARGB int (كما في Order.StatusColor) إلى UIColor.</summary>
        public static UIColor FromArgb(int argb)
        {
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);
            return UIColor.FromRGBA(r, g, b, a);
        }

        /// <summary>ظل خفيف للبطاقات (يقارب elevation في أندرويد).</summary>
        public static void ApplyCardShadow(UIView view, float radius = 8f, float opacity = 0.08f)
        {
            if (view == null) return;
            view.Layer.ShadowColor = UIColor.Black.CGColor;
            view.Layer.ShadowOpacity = opacity;
            view.Layer.ShadowRadius = radius;
            view.Layer.ShadowOffset = new CGSize(0, 2);
            view.Layer.MasksToBounds = false;
        }
    }
}
