using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.Models;
using Tawsela.iOS.Platform;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل قائمة الأماكن المحفوظة (Api.SavedPlaces / Api.AddSavedPlace):
    /// بيت، عمل، أو أي عنوان متكرر مع إحداثياته.
    /// </summary>
    public class SavedPlacesViewController : ListViewController<SavedPlace>
    {
        public SavedPlacesViewController() : base("الأماكن المحفوظة") { }

        protected override string EmptyText
        {
            get { return "لا توجد أماكن محفوظة — أضف عنوانك المتكرر"; }
        }

        protected override List<SavedPlace> Fetch()
        {
            return Api.SavedPlaces();
        }

        protected override UIView BuildRow(SavedPlace p)
        {
            var card = Controls.Card();

            var label = MakeLabel(p.Label, 15f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
            var address = MakeLabel(p.Address, 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            card.AddSubview(label);
            card.AddSubview(address);
            card.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                NSLayoutConstraint.Create(address, NSLayoutAttribute.Top, NSLayoutRelation.Equal, label, NSLayoutAttribute.Bottom, 1, 4),
                NSLayoutConstraint.Create(address, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                NSLayoutConstraint.Create(address, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                NSLayoutConstraint.Create(address, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
            });

            // عرض الموقع على الخريطة
            card.UserInteractionEnabled = true;
            card.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                if (p.Lat == 0 && p.Lng == 0) return;
                Push(MapViewController.ShowPoint(p.Lat, p.Lng, p.Label + " — " + p.Address));
            }));

            return card;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            List.Scroll.ContentInset = new UIEdgeInsets(0, 0, 84, 0);

            // زر إضافة مكان محفوظ (يُثبّت أعلى القائمة)
            var add = Controls.PrimaryButton("إضافة مكان محفوظ");
            add.TouchUpInside += (s, e) => AddNew();
            View.AddSubview(add);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(add, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, -12),
            });
        }

        private void AddNew()
        {
            Ui.Prompt("مكان محفوظ", "الاسم (بيت / عمل / ...)", "", "الاسم", label =>
            {
                if (string.IsNullOrWhiteSpace(label)) return;

                Push(MapViewController.Pick(0, 0, (lat, lng, address) =>
                {
                    var finalAddress = string.IsNullOrWhiteSpace(address)
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00000}, {1:0.00000}", lat, lng)
                        : address;

                    var progress = Ui.ShowProgress("جارٍ الحفظ…");
                    Ui.Run(() => Api.AddSavedPlace(label, finalAddress, lat, lng), () =>
                    {
                        progress.Dismiss();
                        Ui.Toast("تم حفظ المكان");
                        Reload();
                    }, ex =>
                    {
                        progress.Dismiss();
                        Ui.Toast(ex.Message);
                    });
                }));
            });
        }
    }
}
