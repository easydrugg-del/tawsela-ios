using System;
using CoreGraphics;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// عناصر واجهة موحّدة — تقابل أنماط الأزرار والحقول في AXML
    /// (btn_*_selector، edit_rounded، tile_*، badge_*).
    /// </summary>
    public static class Controls
    {
        /* ---------- أزرار ---------- */

        /// <summary>زر أساسي برتقالي (بديل btn_accent_selector / btn_gradient_ride).</summary>
        public static UIButton PrimaryButton(string title, float height = 50f)
        {
            var b = new UIButton(UIButtonType.System);
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(UIColor.White, UIControlState.Normal);
            b.SetTitleColor(UIColor.White.ColorWithAlpha(0.6f), UIControlState.Disabled);
            b.TitleLabel.Font = Theme.FontSemiBold(16);
            b.BackgroundColor = Theme.Primary;
            b.Layer.CornerRadius = Theme.CornerEdit;
            b.Layer.MasksToBounds = true;
            b.TranslatesAutoresizingMaskIntoConstraints = false;
            b.HeightAnchor.ConstraintEqualTo((nfloat)height).Active = true;
            b.SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft;
            return b;
        }

        /// <summary>زر ثانوي بحدود برتقالية على أبيض.</summary>
        public static UIButton OutlineButton(string title, UIColor color = null, float height = 46f)
        {
            var tint = color ?? Theme.Primary;
            var b = new UIButton(UIButtonType.System);
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(tint, UIControlState.Normal);
            b.TitleLabel.Font = Theme.FontSemiBold(15);
            b.BackgroundColor = Theme.White;
            b.Layer.CornerRadius = Theme.CornerEdit;
            b.Layer.BorderWidth = 1f;
            b.Layer.BorderColor = tint.CGColor;
            b.Layer.MasksToBounds = true;
            b.TranslatesAutoresizingMaskIntoConstraints = false;
            b.HeightAnchor.ConstraintEqualTo((nfloat)height).Active = true;
            return b;
        }

        /// <summary>زر خطر (أحمر) — بديل cancel_bg.</summary>
        public static UIButton DangerButton(string title, float height = 46f)
        {
            var b = new UIButton(UIButtonType.System);
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(UIColor.White, UIControlState.Normal);
            b.TitleLabel.Font = Theme.FontSemiBold(15);
            b.BackgroundColor = Theme.Red;
            b.Layer.CornerRadius = 8f;
            b.Layer.MasksToBounds = true;
            b.TranslatesAutoresizingMaskIntoConstraints = false;
            b.HeightAnchor.ConstraintEqualTo((nfloat)height).Active = true;
            return b;
        }

        /// <summary>زر بأيقونة على اليمين ونص (بديل drawableStart + drawablePadding).</summary>
        public static UIButton IconButton(string title, string iconName, UIColor bg, UIColor fg = null)
        {
            var b = PrimaryButton(title);
            b.BackgroundColor = bg;
            b.SetTitleColor(fg ?? UIColor.White, UIControlState.Normal);
            var img = IconFactory.Image(iconName, 20f, fg ?? UIColor.White);
            if (img != null)
            {
                b.SetImage(img.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal), UIControlState.Normal);
                b.ImageEdgeInsets = new UIEdgeInsets(0, 0, 0, 10);
                b.TitleEdgeInsets = new UIEdgeInsets(0, 10, 0, 0);
            }
            return b;
        }

        /* ---------- حقول ---------- */

        /// <summary>حقل إدخال بارتفاع ثابت (بديل edit_rounded + EditText).</summary>
        public static UITextField TextField(string placeholder, bool secure = false, float height = 48f)
        {
            var f = new UITextField
            {
                Placeholder = placeholder ?? "",
                SecureTextEntry = secure,
                Font = Theme.Font(16),
                TextColor = Theme.TextPrimary,
                BackgroundColor = Theme.White,
                BorderStyle = UITextBorderStyle.None,
                TextAlignment = UITextAlignment.Right,
                TranslatesAutoresizingMaskIntoConstraints = false,
                LeftViewMode = UITextFieldViewMode.Always,
                RightViewMode = UITextFieldViewMode.Always,
                KeyboardType = UIKeyboardType.Default,
                AutocorrectionType = UITextAutocorrectionType.No,
                AutocapitalizationType = UITextAutocapitalizationType.None
            };

            // مسافة داخلية + حدود (بديل padding + edit_rounded)
            var pad = new UIView(new CGRect(0, 0, 12, (nfloat)height));
            f.LeftView = pad;
            f.RightView = new UIView(new CGRect(0, 0, 12, (nfloat)height));
            f.Layer.CornerRadius = Theme.CornerEdit;
            f.Layer.BorderWidth = 1f;
            f.Layer.BorderColor = Theme.CardStroke.CGColor;
            f.HeightAnchor.ConstraintEqualTo((nfloat)height).Active = true;
            return f;
        }

        /// <summary>حقل متعدد الأسطر (بديل EditText بأسطر).</summary>
        public static UITextView TextArea(string text, float height = 100f)
        {
            var v = new UITextView
            {
                Text = text ?? "",
                Font = Theme.Font(15),
                TextColor = Theme.TextPrimary,
                BackgroundColor = Theme.White,
                TextAlignment = UITextAlignment.Right,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            v.Layer.CornerRadius = Theme.CornerEdit;
            v.Layer.BorderWidth = 1f;
            v.Layer.BorderColor = Theme.CardStroke.CGColor;
            v.TextContainerInset = new UIEdgeInsets(10, 10, 10, 10);
            v.HeightAnchor.ConstraintEqualTo((nfloat)height).Active = true;
            return v;
        }

        /// <summary>عنوان قسم (بديل TextView عريض).</summary>
        public static UILabel SectionTitle(string text, float size = 16f)
        {
            return new UILabel
            {
                Text = text ?? "",
                Font = Theme.FontBold(size),
                TextColor = Theme.TextPrimary,
                TextAlignment = UITextAlignment.Right,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
        }

        /// <summary>بطاقة بيضاء بزوايا 16 (بديل tile_card_selector).</summary>
        public static UIView Card(float corner = Theme.CornerTile)
        {
            var v = new UIView { BackgroundColor = Theme.White, TranslatesAutoresizingMaskIntoConstraints = false };
            v.Layer.CornerRadius = (nfloat)corner;
            v.Layer.BorderWidth = 1f;
            v.Layer.BorderColor = Theme.CardStroke.CGColor;
            v.Layer.MasksToBounds = true;
            return v;
        }

        /// <summary>سطر معلومات: عنوان + قيمة، داخل بطاقة.</summary>
        public static UIView InfoRow(string label, string value, UIColor valueColor = null)
        {
            var card = Card();
            var l = new UILabel
            {
                Text = label ?? "",
                Font = Theme.Font(13),
                TextColor = Theme.TextSecondary,
                TextAlignment = UITextAlignment.Right,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            var v = new UILabel
            {
                Text = value ?? "",
                Font = Theme.FontMedium(15),
                TextColor = valueColor ?? Theme.TextPrimary,
                TextAlignment = UITextAlignment.Right,
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            card.AddSubview(l);
            card.AddSubview(v);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(l, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(l, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(l, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(v, NSLayoutAttribute.Top, NSLayoutRelation.Equal, l, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(v, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(v, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(v, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });
            return card;
        }

        /// <summary>بلاطة شاشة رئيسية: دائرة أيقونة + نص (بديل tile_* + badge_* + ImageView).</summary>
        public static UIControl Tile(string iconName, string title, UIColor light, UIColor dark, Action onTap)
        {
            var tile = new TileControl(iconName, title, light, dark);
            if (onTap != null) tile.TouchUpInside += (s, e) => onTap();
            return tile;
        }

        /// <summary>زر مربّع 48×48 بأيقونة — بديل زر الميكروفون بجوار الحقل في أندرويد.</summary>
        public static UIButton SquareIconButton(string iconName, UIColor bg, Action onTap)
        {
            var b = new UIButton(UIButtonType.System);
            b.BackgroundColor = bg;
            b.Layer.CornerRadius = Theme.CornerEdit;
            b.Layer.MasksToBounds = true;
            b.TranslatesAutoresizingMaskIntoConstraints = false;
            b.WidthAnchor.ConstraintEqualTo(48f).Active = true;
            b.HeightAnchor.ConstraintEqualTo(48f).Active = true;

            var img = IconFactory.Image(iconName, 22f, UIColor.White);
            if (img != null)
            {
                b.SetImage(img.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal), UIControlState.Normal);
            }
            if (onTap != null) b.TouchUpInside += (s, e) => onTap();
            return b;
        }

        /// <summary>سطر أفقي: حقل + زر (بديل LinearLayout horizontal في أندرويد).</summary>
        public static UIStackView FieldWithAction(UIView field, UIView action, float spacing = 8f)
        {
            var row = new UIStackView(new[] { field, action })
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = spacing,
                Alignment = UIStackViewAlignment.Center,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft
            };
            return row;
        }

        /// <summary>شريحة (chip) بنص — بديل chip_bg / MakeChip.</summary>
        public static UIButton ChipButton(string title, Action onTap)
        {
            var b = new UIButton(UIButtonType.System);
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(Theme.Primary, UIControlState.Normal);
            b.TitleLabel.Font = Theme.Font(13);
            b.BackgroundColor = Theme.White;
            b.Layer.CornerRadius = Theme.CornerChip;
            b.Layer.BorderWidth = 1f;
            b.Layer.BorderColor = Theme.CardStroke.CGColor;
            b.Layer.MasksToBounds = true;
            b.ContentEdgeInsets = new UIEdgeInsets(9, 16, 9, 16);
            b.TranslatesAutoresizingMaskIntoConstraints = false;
            if (onTap != null) b.TouchUpInside += (s, e) => onTap();
            return b;
        }

        /// <summary>شريحة شارع (حمراء) — بديل chip_red_bg / MakeStreetTile.</summary>
        public static UIButton StreetChip(string title, Action onTap)
        {
            var b = new UIButton(UIButtonType.System);
            b.SetTitle(title ?? "", UIControlState.Normal);
            b.SetTitleColor(Theme.Red, UIControlState.Normal);
            b.TitleLabel.Font = Theme.FontBold(13);
            b.TitleLabel.LineBreakMode = UILineBreakMode.TailTruncation;
            b.BackgroundColor = Theme.White;
            b.Layer.CornerRadius = Theme.CornerChipRed;
            b.Layer.BorderWidth = 1f;
            b.Layer.BorderColor = Theme.ChipRedStroke.CGColor;
            b.Layer.MasksToBounds = true;
            b.ContentEdgeInsets = new UIEdgeInsets(8, 10, 8, 10);
            b.TranslatesAutoresizingMaskIntoConstraints = false;
            if (onTap != null) b.TouchUpInside += (s, e) => onTap();
            return b;
        }
    }

    /// <summary>بلاطة بشاشة رئيسية: خلفية بيضاء + دائرة أيقونة ملوّنة + عنوان.</summary>
    public sealed class TileControl : UIControl
    {
        private readonly UIView _background;
        private readonly UIView _circle;
        private readonly UILabel _label;

        public TileControl(string iconName, string title, UIColor light, UIColor dark)
        {
            TranslatesAutoresizingMaskIntoConstraints = false;

            _background = new UIView
            {
                BackgroundColor = Theme.White,
                TranslatesAutoresizingMaskIntoConstraints = false,
                UserInteractionEnabled = false
            };
            _background.Layer.CornerRadius = Theme.CornerTile;
            _background.Layer.BorderWidth = 1f;
            _background.Layer.BorderColor = Theme.CardStroke.CGColor;
            _background.Layer.MasksToBounds = true;
            AddSubview(_background);

            _circle = Shapes.BadgeCircle(44f, light, dark);
            _circle.TranslatesAutoresizingMaskIntoConstraints = false;
            _circle.UserInteractionEnabled = false;
            _background.AddSubview(_circle);

            var icon = new UIImageView
            {
                Image = IconFactory.Image(iconName, 22f, UIColor.White),
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false,
                UserInteractionEnabled = false
            };
            _circle.AddSubview(icon);

            _label = new UILabel
            {
                Text = title ?? "",
                Font = Theme.FontMedium(13),
                TextColor = Theme.TextPrimary,
                TextAlignment = UITextAlignment.Center,
                Lines = 2,
                LineBreakMode = UILineBreakMode.WordWrap,
                TranslatesAutoresizingMaskIntoConstraints = false,
                UserInteractionEnabled = false
            };
            _background.AddSubview(_label);

            AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_background, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_background, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, this, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_background, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, this, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_background, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, this, NSLayoutAttribute.Trailing, 1, 0),
            });

            _background.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_circle, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, _background, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(_circle, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _background, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(_circle, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 44),
                NSLayoutConstraint.Create(_circle, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 44),

                NSLayoutConstraint.Create(icon, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, _circle, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(icon, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _circle, NSLayoutAttribute.CenterY, 1, 0),

                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _circle, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _background, NSLayoutAttribute.Leading, 1, 4),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _background, NSLayoutAttribute.Trailing, 1, -4),
                NSLayoutConstraint.Create(_label, NSLayoutAttribute.Bottom, NSLayoutRelation.LessThanOrEqual, _background, NSLayoutAttribute.Bottom, 1, -6),
            });
        }

        public override bool Highlighted
        {
            get { return base.Highlighted; }
            set
            {
                base.Highlighted = value;
                _background.BackgroundColor = value ? Theme.TilePressed : Theme.White;
            }
        }
    }
}
