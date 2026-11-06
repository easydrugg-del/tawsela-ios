using System;
using CoreGraphics;
using Tawsela.Core.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/IntroActivity.cs — شاشة تعريف سريعة تُعرض مرة واحدة،
    /// ثم تُسجّل intro_seen وتنتقل لتسجيل الدخول.
    /// </summary>
    public class IntroViewController : BaseViewController
    {
        private readonly string[] _titles =
        {
            "اطلب توصيلتك في ثوانٍ",
            "تابع السائق لحظة بلحظة",
            "ادفع بالطريقة التي تناسبك"
        };

        private readonly string[] _bodies =
        {
            "حدد نقطة الانطلاق والوجهة، واختر نوع الخدمة، وسيصلك أقرب سائق متاح.",
            "شاهد موقع السائق على الخريطة، وتواصل معه مباشرة، واعرف وقت الوصول.",
            "محفظتك داخل التطبيق، وكوبونات الخصم، والدفع النقدي — كلها متاحة."
        };

        private readonly string[] _icons = { "ic_taxi", "ic_action_map", "ic_wallet" };

        private int _page;
        private UIImageView _image;
        private UILabel _title;
        private UILabel _body;
        private UIPageControl _dots;
        private UIButton _next;

        public override bool ShowNavigationBar
        {
            get { return false; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.White;

            var stack = CreateScrollContent(16f, 24f);
            stack.Alignment = UIStackViewAlignment.Fill;

            var skip = new UIButton(UIButtonType.System);
            skip.SetTitle("تخطّي", UIControlState.Normal);
            skip.SetTitleColor(Theme.TextSecondary, UIControlState.Normal);
            skip.TitleLabel.Font = Theme.Font(14);
            skip.TranslatesAutoresizingMaskIntoConstraints = false;
            skip.TouchUpInside += (s, e) => Finish();

            var skipRow = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            skipRow.AddSubview(skip);
            skipRow.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(skip, NSLayoutAttribute.Top, NSLayoutRelation.Equal, skipRow, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(skip, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, skipRow, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(skip, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, skipRow, NSLayoutAttribute.Leading, 1, 0),
            });
            stack.AddArrangedSubview(skipRow);
            stack.SetCustomSpacing(10f, skipRow);

            _image = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _image.HeightAnchor.ConstraintEqualTo(160).Active = true;
            stack.AddArrangedSubview(_image);

            _title = MakeLabel("", 24f, Theme.TextPrimary, UIFontWeight.Bold, UITextAlignment.Center);
            stack.AddArrangedSubview(_title);

            _body = MakeLabel("", 15f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center);
            stack.AddArrangedSubview(_body);

            _dots = new UIPageControl
            {
                Pages = _titles.Length,
                CurrentPage = 0,
                PageIndicatorTintColor = Theme.CardStroke,
                CurrentPageIndicatorTintColor = Theme.Primary,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _dots.ValueChanged += (s, e) => { _page = (int)_dots.CurrentPage; Render(); };
            stack.AddArrangedSubview(_dots);
            _dots.HeightAnchor.ConstraintEqualTo(24).Active = true;

            _next = Controls.PrimaryButton("التالي");
            _next.TouchUpInside += (s, e) =>
            {
                if (_page >= _titles.Length - 1) Finish();
                else { _page++; Render(); }
            };
            stack.AddArrangedSubview(_next);

            Render();
        }

        private void Render()
        {
            if (_page < 0) _page = 0;
            if (_page >= _titles.Length) _page = _titles.Length - 1;

            _title.Text = _titles[_page];
            _body.Text = _bodies[_page];
            _dots.CurrentPage = _page;
            _next.SetTitle(_page >= _titles.Length - 1 ? "ابدأ الآن" : "التالي", UIControlState.Normal);

            var icon = _icons[_page];
            var img = IconFactory.Image(icon, 120f, Theme.Primary);
            if (img != null)
            {
                _image.Image = img;
            }
            else
            {
                // بديل: دائرة بلون العلامة عند غياب الأيقونة
                var circle = Shapes.Circle(140f, Theme.Primary.ColorWithAlpha(0.12f));
                var renderer = new UIGraphicsImageRenderer(new CGSize(140, 140));
                _image.Image = renderer.CreateImage(ctx =>
                {
                    var path = UIBezierPath.FromOval(new CGRect(0, 0, 140, 140));
                    Theme.Primary.ColorWithAlpha(0.12f).SetFill();
                    path.Fill();
                });
            }
        }

        private void Finish()
        {
            AppServices.Prefs.SetBool("tawsela.intro_seen", true);
            Router.SetRoot(new LoginViewController());
        }
    }
}
