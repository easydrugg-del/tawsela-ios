using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// بديل ملفات Resources/drawable/*.xml (shape / selector / layer-list) على iOS.
    /// كل دالة هنا تقابل ملف XML بنفس الاسم والقيم.
    /// </summary>
    public static class Shapes
    {
        /* ---------- tile_*_selector + card_ripple (خلفيات قابلة للضغط) ---------- */

        /// <summary>
        /// خلفية قابلة للضغط بحدود وزوايا. تقابل tile_card_selector.xml و card_ripple.xml.
        /// </summary>
        public static UIView Pressable(UIView container, UIColor normal, UIColor pressed,
            UIColor stroke, nfloat strokeWidth, nfloat corner)
        {
            container.BackgroundColor = normal;
            container.Layer.CornerRadius = corner;
            container.Layer.BorderWidth = strokeWidth;
            container.Layer.BorderColor = stroke.CGColor;
            container.Layer.MasksToBounds = true;
            container.Tag = 0;

            // بديل state_pressed في selector: نغيّر الخلفية أثناء اللمس فقط،
            // ولا نعطّل لمس العناصر الداخلية (أزرار/شرائح) ولا التمرير.
            var recognizer = new UILongPressGestureRecognizer(state =>
            {
                switch (state.State)
                {
                    case UIGestureRecognizerState.Began:
                        container.BackgroundColor = pressed;
                        break;
                    case UIGestureRecognizerState.Ended:
                    case UIGestureRecognizerState.Cancelled:
                    case UIGestureRecognizerState.Failed:
                        container.BackgroundColor = normal;
                        break;
                }
            })
            {
                MinimumPressDuration = 0,
                CancelsTouchesInView = false,
                DelaysTouchesBegan = false,
                DelaysTouchesEnded = false
            };

            container.UserInteractionEnabled = true;
            container.AddGestureRecognizer(recognizer);
            return container;
        }

        /* ---------- تعريفات مطابقة للملفات ---------- */

        /// <summary>tile_card_selector.xml — بطاقة بيضاء، زوايا 16، حدود card_stroke أو primary</summary>
        public static UIView TileCard(UIView v)
        {
            return Pressable(v, Theme.White, Theme.TilePressed, Theme.Primary, 1f, Theme.CornerTile);
        }

        /// <summary>tile_red_selector.xml — نفس البطاقة بحدود حمراء</summary>
        public static UIView TileRed(UIView v)
        {
            return Pressable(v, Theme.White, Theme.ChipRedPressed, Theme.Red, 1f, Theme.CornerTile);
        }

        /// <summary>chip_bg.xml — زوايا 18</summary>
        public static UIView Chip(UIView v)
        {
            return Pressable(v, Theme.White, Theme.ChipPressed, Theme.CardStroke, 1f, Theme.CornerChip);
        }

        /// <summary>chip_red_bg.xml — زوايا 12، حدود #E57373 وضغط #FDECEC</summary>
        public static UIView ChipRed(UIView v)
        {
            return Pressable(v, Theme.White, Theme.ChipRedPressed, Theme.ChipRedStroke, 1f, Theme.CornerChipRed);
        }

        /// <summary>area_tile_bg.xml — زوايا 10، حدود #FFD2AE</summary>
        public static UIView AreaTile(UIView v)
        {
            return Pressable(v, Theme.White, Theme.ChipPressed, Theme.AreaTileStroke, 1f, Theme.CornerArea);
        }

        /// <summary>edit_rounded.xml — حقل إدخال: أبيض، زوايا 12، حدود card_stroke</summary>
        public static void EditRounded(UIView v, float corner = Theme.CornerEdit)
        {
            v.BackgroundColor = Theme.White;
            v.Layer.CornerRadius = corner;
            v.Layer.BorderWidth = 1f;
            v.Layer.BorderColor = Theme.CardStroke.CGColor;
            v.Layer.MasksToBounds = true;
        }

        /// <summary>card_ripple.xml — زوايا 14</summary>
        public static UIView CardRipple(UIView v)
        {
            return Pressable(v, Theme.White, Theme.ChipPressed, Theme.CardStroke, 1f, Theme.CornerRipple);
        }

        /// <summary>bg_search.xml — حقل بحث بزوايا 24</summary>
        public static void SearchField(UIView v)
        {
            v.BackgroundColor = Theme.White;
            v.Layer.CornerRadius = Theme.CornerSearch;
            v.Layer.BorderWidth = 1f;
            v.Layer.BorderColor = Theme.CardStroke.CGColor;
            v.Layer.MasksToBounds = true;
        }

        /// <summary>btn_badge_bg.xml — دائرة حمراء 24dp لنقطة الإشعارات</summary>
        public static UIView Badge(float size = 24f)
        {
            var v = new UIView { BackgroundColor = Theme.BadgeRed };
            v.Layer.CornerRadius = size / 2f;
            v.Layer.MasksToBounds = true;
            return v;
        }

        /// <summary>cancel_bg.xml — زر أحمر بزوايا 8</summary>
        public static void CancelButton(UIView v)
        {
            v.BackgroundColor = Theme.Red;
            v.Layer.CornerRadius = 8f;
            v.Layer.MasksToBounds = true;
        }

        /// <summary>circle_*.xml — دائرة بلون</summary>
        public static UIView Circle(nfloat size, UIColor color)
        {
            var v = new UIView { BackgroundColor = color };
            v.Layer.CornerRadius = size / 2f;
            v.Layer.MasksToBounds = true;
            return v;
        }

        /// <summary>line_done / line_pending — خط بزوايا 2</summary>
        public static UIView Line(UIColor color, float height = 3f)
        {
            var v = new UIView { BackgroundColor = color };
            v.Layer.CornerRadius = 2f;
            v.Layer.MasksToBounds = true;
            return v;
        }

        /* ---------- badge_* (تدرّج + ظل) ---------- */

        /// <summary>
        /// بديل badge_orange / badge_blue / badge_green ... :
        /// دائرة بتدرّج قطري + هالة خارجية + دائرة داخلية داكنة قليلاً + لمعة.
        /// </summary>
        public static UIView BadgeCircle(nfloat size, UIColor light, UIColor dark)
        {
            var host = new UIView();
            host.BackgroundColor = UIColor.Clear;
            host.Frame = new CGRect(0, 0, size, size);

            // الهالة الخارجية (أخفّ وأكبر قليلاً)
            var glow = new UIView
            {
                Frame = new CGRect(-4, -4, size + 8, size + 8),
                BackgroundColor = light.ColorWithAlpha(0.18f)
            };
            glow.Layer.CornerRadius = (size + 8) / 2f;
            glow.Layer.MasksToBounds = true;
            host.AddSubview(glow);

            // الدائرة الداكنة خلف التدرّج (تعطي عمقًا كالطبقة الوسطى)
            var baseCircle = new UIView
            {
                Frame = new CGRect(1, 3, size - 2, size - 4),
                BackgroundColor = dark.ColorWithAlpha(0.80f)
            };
            baseCircle.Layer.CornerRadius = (size - 4) / 2f;
            baseCircle.Layer.MasksToBounds = true;
            host.AddSubview(baseCircle);

            // التدرّج الرئيسي
            var grad = new GradientView
            {
                Frame = new CGRect(0, 0, size - 2, size - 3),
                Colors = new[] { light, dark },
                Start = new CGPoint(0.15, 0.15),
                End = new CGPoint(0.85, 0.85)
            };
            grad.Layer.CornerRadius = (size - 3) / 2f;
            grad.Layer.MasksToBounds = true;
            host.AddSubview(grad);

            // اللمعة العلوية
            var shine = new UIView
            {
                Frame = new CGRect(7, 3, size - 14, Math.Max(2, size * 0.16f)),
                BackgroundColor = UIColor.White.ColorWithAlpha(0.27f)
            };
            shine.Layer.CornerRadius = shine.Frame.Height / 2f;
            shine.Layer.MasksToBounds = true;
            host.AddSubview(shine);

            return host;
        }

        /* ---------- bg_header (تدرّج رأسي) ---------- */

        /// <summary>bg_header.xml — تدرّج من primary إلى primary_dark بزاوية 270 (لأسفل)</summary>
        public static UIView Header(nfloat height)
        {
            return new GradientView
            {
                Frame = new CGRect(0, 0, 1, height),
                Colors = new[] { Theme.Primary, Theme.PrimaryDark },
                Start = new CGPoint(0, 0),
                End = new CGPoint(0, 1)
            };
        }
    }

    /// <summary>عرض بتدرّج ألوان (بديل gradient في أندرويد).</summary>
    public sealed class GradientView : UIView
    {
        private readonly CAGradientLayer _gradient = new CAGradientLayer();

        public UIColor[] Colors
        {
            get { return null; }
            set
            {
                if (value == null) return;
                var arr = new CGColor[value.Length];
                for (int i = 0; i < value.Length; i++) arr[i] = value[i].CGColor;
                _gradient.Colors = arr;
            }
        }

        public CGPoint Start
        {
            get { return _gradient.StartPoint; }
            set { _gradient.StartPoint = value; }
        }

        public CGPoint End
        {
            get { return _gradient.EndPoint; }
            set { _gradient.EndPoint = value; }
        }

        public GradientView()
        {
            Layer.AddSublayer(_gradient);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            _gradient.Frame = Bounds;
        }
    }
}
