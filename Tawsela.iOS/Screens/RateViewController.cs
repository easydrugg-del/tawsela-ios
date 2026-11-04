using System;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RateActivity.cs — تقييم الرحلة بعد الاكتمال
    /// (نجوم + تعليق + صورة اختيارية عبر Api.AddRating).
    /// </summary>
    public class RateViewController : BaseViewController
    {
        private readonly int _rateeId;
        private readonly int _orderId;
        private int _rating;
        private UIImageView[] _stars;
        private UITextView _comment;
        private UIImage _photo;
        private UILabel _photoLabel;

        public RateViewController(int rateeId, int orderId) : base("تقييم الرحلة")
        {
            _rateeId = rateeId;
            _orderId = orderId;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(14f, 18f);

            stack.AddArrangedSubview(MakeLabel("كيف كانت رحلتك؟", 20f, Theme.TextPrimary,
                UIFontWeight.Bold, UITextAlignment.Center));

            // النجوم
            var starsRow = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Spacing = 8,
                Distribution = UIStackViewDistribution.FillEqually,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _stars = new UIImageView[5];
            for (int i = 0; i < 5; i++)
            {
                var index = i + 1;
                var star = new UIImageView
                {
                    Image = IconFactory.Image("ic_star", 36f, Theme.CardStroke),
                    ContentMode = UIViewContentMode.ScaleAspectFit,
                    UserInteractionEnabled = true,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                star.HeightAnchor.ConstraintEqualTo(44).Active = true;
                star.AddGestureRecognizer(new UITapGestureRecognizer(() => SetRating(index)));
                _stars[i] = star;
                starsRow.AddArrangedSubview(star);
            }
            stack.AddArrangedSubview(starsRow);
            SetRating(5);

            _comment = Controls.TextArea("", 110f);
            ((UITextView)_comment).Text = "";
            stack.AddArrangedSubview(Controls.SectionTitle("تعليق (اختياري)"));
            stack.AddArrangedSubview(_comment);

            var photoBtn = Controls.OutlineButton("إضافة صورة (اختياري)");
            photoBtn.TouchUpInside += (s, e) =>
            {
                ImagePicker.PickImage(this, false, img =>
                {
                    _photo = img;
                    _photoLabel.Text = "تم اختيار صورة";
                    _photoLabel.TextColor = Theme.Green;
                }, err => Ui.Toast(err));
            };
            stack.AddArrangedSubview(photoBtn);

            _photoLabel = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_photoLabel);

            var submit = Controls.PrimaryButton("إرسال التقييم");
            submit.TouchUpInside += (s, e) => Submit();
            stack.AddArrangedSubview(submit);
        }

        private void SetRating(int value)
        {
            _rating = value;
            for (int i = 0; i < _stars.Length; i++)
            {
                var filled = i < value;
                _stars[i].Image = IconFactory.Image("ic_star", 36f, filled ? Theme.Accent : Theme.CardStroke);
            }
        }

        private void Submit()
        {
            if (_rating <= 0)
            {
                Ui.Toast("اختر عدد النجوم");
                return;
            }

            var comment = _comment.Text ?? "";
            var photoPath = _photo == null ? "" : ImagePicker.ToBase64(_photo);

            var progress = Ui.ShowProgress("جارٍ إرسال التقييم…");

            Ui.Run(
                () => Api.AddRating(_orderId, _rateeId, _rating, comment, photoPath),
                () =>
                {
                    progress.Dismiss();
                    Ui.Toast("شكرًا لتقييمك");
                    NavigationController?.PopViewController(true);
                },
                ex =>
                {
                    progress.Dismiss();
                    Ui.Toast(ex.Message);
                });
        }
    }
}
