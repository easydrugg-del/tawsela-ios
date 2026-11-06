using System;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/ReferralActivity.cs — كود الإحالة وعدد المُحالين والمكافأة،
    /// مع مشاركة الكود (بديل Intent.ActionSend).
    /// </summary>
    public class ReferralViewController : BaseViewController
    {
        private UILabel _codeLabel;
        private UILabel _countLabel;
        private UILabel _rewardLabel;

        public ReferralViewController() : base("نظام الإحالة") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Load();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(14f, 18f);

            stack.AddArrangedSubview(MakeLabel("شارك كودك مع أصدقائك واحصل على مكافأة عند أول رحلة لكل صديق.",
                14f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Center));

            var card = Controls.Card();
            card.BackgroundColor = Theme.Primary;

            var caption = MakeLabel("كود الإحالة الخاص بك", 12f,
                UIColor.White.ColorWithAlpha(0.9f), UIFontWeight.Regular, UITextAlignment.Center);
            _codeLabel = MakeLabel("—", 28f, UIColor.White, UIFontWeight.Bold, UITextAlignment.Center);

            card.AddSubview(caption);
            card.AddSubview(_codeLabel);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(caption, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 14),
                NSLayoutConstraint.Create(caption, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(caption, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(_codeLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, caption, NSLayoutAttribute.Bottom, 1, 6),
                NSLayoutConstraint.Create(_codeLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(_codeLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(_codeLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -16),
            });
            stack.AddArrangedSubview(card);

            _countLabel = MakeLabel("", 15f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
            _rewardLabel = MakeLabel("", 15f, Theme.Green, UIFontWeight.Medium, UITextAlignment.Right);

            stack.AddArrangedSubview(_countLabel);
            stack.AddArrangedSubview(_rewardLabel);

            var copy = Controls.OutlineButton("نسخ الكود");
            copy.TouchUpInside += (s, e) => CopyCode();
            stack.AddArrangedSubview(copy);

            var share = Controls.PrimaryButton("مشاركة كود الإحالة");
            share.TouchUpInside += (s, e) => ShareCode();
            stack.AddArrangedSubview(share);
        }

        private void Load()
        {
            Ui.Run(() =>
            {
                string code;
                int count;
                decimal reward;
                Api.GetReferral(out code, out count, out reward);

                Ui.BeginOnMain(() =>
                {
                    _codeLabel.Text = string.IsNullOrEmpty(code) ? "—" : code;
                    _countLabel.Text = "الأصدقاء المسجلون: " + count;
                    _rewardLabel.Text = "إجمالي المكافآت: " + reward.ToString("0.00") + " ج.م";
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void CopyCode()
        {
            try
            {
                UIPasteboard.General.String = _codeLabel.Text ?? "";
                Ui.Toast("تم نسخ الكود");
            }
            catch { }
        }

        private void ShareCode()
        {
            var text = "انضم إلى توصيلة باستخدام كود الإحالة: " + (_codeLabel.Text ?? "") +
                       "\n" + Legal.BaseUrl;

            try
            {
                var controller = new UIActivityViewController(
                    new NSObject[] { new NSString(text) }, null);

                // على iPad يجب تحديد مصدر الـ Popover وإلا يحدث Crash
                if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                    controller.PopoverPresentationController != null)
                {
                    controller.PopoverPresentationController.SourceView = View;
                    controller.PopoverPresentationController.SourceRect =
                        new CoreGraphics.CGRect(View.Bounds.Width / 2, View.Bounds.Height / 2, 1, 1);
                }

                PresentViewController(controller, true, null);
            }
            catch (Exception ex)
            {
                Ui.Toast(ex.Message);
            }
        }
    }
}
