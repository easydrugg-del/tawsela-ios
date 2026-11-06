using System;
using CoreGraphics;
using Tawsela.Core.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/IntroActivity.cs — نفس شكل أندرويد بالضبط:
    /// شعار في المنتصف + عنوان برتقالي + وصف، وزر «التالي» أبيض أسفل الشاشة.
    /// </summary>
    public class IntroViewController : BaseViewController
    {
        private readonly string[] _titles =
        {
            "إلى أين تريد الذهاب؟",
            "سائقون موثوقون",
            "ادفع بأمان"
        };

        private readonly string[] _bodies =
        {
            "اطلب توصيلة من أي مكان إلى أي مكان بخطوات بسيطة وراقب السائق لحظياً على الخريطة.",
            "نختار لك أقرب السائقين وأكثرهم تقييماً، مع إمكانية حفظ سائقيك المفضلين.",
            "ادفع نقداً أو إلكترونياً عبر محفظتك، واستفد من العروض والكوبونات المتاحة."
        };

        private int _page;
        private UIImageView _image;
        private UILabel _title;
        private UILabel _body;
        private UIButton _next;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.White;

            // زر التخطّي أعلى الشاشة (بديل btnSkip)
            var skip = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            skip.SetTitle("تخطّي", UIControlState.Normal);
            skip.SetTitleColor(Theme.TextSecondary, UIControlState.Normal);
            skip.TitleLabel.Font = Theme.Font(15);
            skip.ContentEdgeInsets = new UIEdgeInsets(6, 4, 6, 4);
            skip.TouchUpInside += (s, e) => Finish();
            View.AddSubview(skip);

            // البلوك الأوسط: الشعار + العنوان + الوصف (بديل weight=1 gravity=center)
            var center = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            View.AddSubview(center);

            var stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Center,
                Spacing = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            center.AddSubview(stack);

            _image = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _image.WidthAnchor.ConstraintEqualTo(130f).Active = true;
            _image.HeightAnchor.ConstraintEqualTo(130f).Active = true;
            stack.AddArrangedSubview(_image);

            _title = MakeLabel("", 24f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Center);
            _title.Lines = 0;
            stack.AddArrangedSubview(_title);
            stack.SetCustomSpacing(24f, _image);

            _body = MakeLabel("", 16f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            _body.Lines = 0;
            stack.AddArrangedSubview(_body);
            stack.SetCustomSpacing(12f, _title);

            // زر «التالي» الأبيض أسفل الشاشة (بديل btnIntroNext)
            _next = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
            _next.SetTitle("التالي", UIControlState.Normal);
            _next.SetTitleColor(Theme.TextPrimary, UIControlState.Normal);
            _next.TitleLabel.Font = Theme.FontSemiBold(17);
            _next.BackgroundColor = Theme.White;
            _next.Layer.CornerRadius = Theme.CornerTile;
            _next.Layer.BorderWidth = 1f;
            _next.Layer.BorderColor = Theme.CardStroke.CGColor;
            _next.Layer.MasksToBounds = true;
            _next.TouchUpInside += (s, e) =>
            {
                if (_page >= _titles.Length - 1) Finish();
                else { _page++; Render(); }
            };
            View.AddSubview(_next);

            center.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, center, NSLayoutAttribute.CenterX, 1, 0),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, center, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, center, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(stack, NSLayoutAttribute.Trailing, NSLayoutRelation.LessThanOrEqual, center, NSLayoutAttribute.Trailing, 1, 0),
            });

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(skip, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 6),
                NSLayoutConstraint.Create(skip, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -28),
                NSLayoutConstraint.Create(skip, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 36),

                NSLayoutConstraint.Create(_next, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -28),
                NSLayoutConstraint.Create(_next, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 28),
                NSLayoutConstraint.Create(_next, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -28),
                NSLayoutConstraint.Create(_next, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 54),

                NSLayoutConstraint.Create(center, NSLayoutAttribute.Top, NSLayoutRelation.Equal, skip, NSLayoutAttribute.Bottom, 1, 8),
                NSLayoutConstraint.Create(center, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _next, NSLayoutAttribute.Top, 1, -16),
                NSLayoutConstraint.Create(center, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 28),
                NSLayoutConstraint.Create(center, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -28),
            });

            Render();
        }

        private void Render()
        {
            if (_page < 0) _page = 0;
            if (_page >= _titles.Length) _page = _titles.Length - 1;

            _title.Text = _titles[_page];
            _body.Text = _bodies[_page];
            _next.SetTitle(_page >= _titles.Length - 1 ? "ابدأ" : "التالي", UIControlState.Normal);

            // نفس شعار أندرويد في كل الصفحات (ic_logo)
            var img = IconFactory.Image("ic_logo", 130f);
            _image.Image = img;
        }

        private void Finish()
        {
            AppServices.Prefs.SetBool("tawsela.intro_seen", true);
            Router.SetRoot(new LoginViewController());
        }
    }
}
