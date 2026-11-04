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
    /// بديل Activities/NearbyDriversActivity.cs — السائقون المتاحون حول موقعك
    /// (Api.AvailableDrivers) مع عرضهم على الخريطة.
    /// </summary>
    public class NearbyDriversViewController : BaseViewController
    {
        private UIStackView _list;
        private UILabel _summary;
        private List<Driver> _drivers = new List<Driver>();
        private readonly HashSet<int> _favorites = new HashSet<int>();
        private readonly HashSet<int> _promoted = new HashSet<int>();
        private double _lat, _lng;

        public NearbyDriversViewController() : base("السائقون القريبون") { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            BuildUi();
            Locate();
        }

        private void BuildUi()
        {
            var stack = CreateScrollContent(10f, 16f);

            _summary = MakeLabel("جارٍ تحديد موقعك…", 14f, Theme.TextSecondary,
                UIFontWeight.Regular, UITextAlignment.Right);
            stack.AddArrangedSubview(_summary);

            var refresh = Controls.OutlineButton("تحديث");
            refresh.TouchUpInside += (s, e) => Locate();
            stack.AddArrangedSubview(refresh);

            var map = Controls.OutlineButton("عرض على الخريطة");
            map.TouchUpInside += (s, e) => ShowMap();
            stack.AddArrangedSubview(map);

            _list = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 10,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stack.AddArrangedSubview(_list);
        }


        private void Locate()
        {
            LocationService.RequestPermission(granted =>
            {
                if (!granted)
                {
                    Ui.BeginOnMain(() =>
                    {
                        _summary.Text = "لم يتم السماح بالوصول للموقع";
                        _summary.TextColor = Theme.Red;
                    });
                    return;
                }

                LocationService.RequestOneShot((coord, accuracy) =>
                {
                    _lat = coord.Latitude;
                    _lng = coord.Longitude;
                    Load();
                }, err => Ui.BeginOnMain(() => _summary.Text = err));
            });
        }

        private void Load()
        {
            var gov = Session.Governorate;
            var city = Session.City;

            Ui.Run(() =>
            {
                var drivers = Api.AvailableDrivers(_lat, _lng, gov, city) ?? new List<Driver>();

                // المفضّلون: قائمة واحدة بدل نداء لكل سائق
                var favs = new HashSet<int>();
                try
                {
                    var list = Api.FavoriteDrivers();
                    if (list != null) foreach (var f in list) favs.Add(f.UserId);
                }
                catch { }

                // السائقون المميّزون (بديل Api.IsPromoted)
                var promos = new HashSet<int>();
                foreach (var d in drivers)
                {
                    try { if (Api.IsPromoted(d.UserId)) promos.Add(d.UserId); } catch { }
                }

                Ui.BeginOnMain(() =>
                {
                    _drivers = drivers;
                    _favorites.Clear();
                    foreach (var id in favs) _favorites.Add(id);
                    _promoted.Clear();
                    foreach (var id in promos) _promoted.Add(id);
                    Render();
                });
            }, null, ex => Ui.Toast(ex.Message));
        }

        private void Render()
        {
            for (int i = _list.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = _list.ArrangedSubviews[i];
                _list.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            _summary.Text = _drivers.Count == 0
                ? "لا يوجد سائقون متاحون قريبًا منك الآن"
                : "يوجد " + _drivers.Count + " سائق متاح حولك";
            _summary.TextColor = _drivers.Count == 0 ? Theme.TextSecondary : Theme.Green;

            foreach (var d in _drivers)
            {
                var card = Controls.Card();

                var promoted = _promoted.Contains(d.UserId) ? " ⭐ مميّز" : "";
                var isFav = _favorites.Contains(d.UserId);

                var name = MakeLabel(d.FullName + promoted, 15f, Theme.TextPrimary, UIFontWeight.Semibold, UITextAlignment.Right);
                var car = MakeLabel(
                    (string.IsNullOrEmpty(d.CarModel) ? "" : d.CarModel) +
                    (string.IsNullOrEmpty(d.CarPlate) ? "" : " · " + d.CarPlate) +
                    (string.IsNullOrEmpty(d.CarColor) ? "" : " · " + d.CarColor),
                    12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

                var distance = MakeLabel(d.DistanceKm.ToString("0.0") + " كم", 12f, Theme.Accent,
                    UIFontWeight.Medium, UITextAlignment.Left);

                // زر المفضلة (بديل Api.ToggleFavorite)
                var fav = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
                fav.SetTitle(isFav ? "★" : "☆", UIControlState.Normal);
                fav.SetTitleColor(isFav ? Theme.Accent : Theme.Gray, UIControlState.Normal);
                fav.TitleLabel.Font = Theme.Font(20);

                var driverId = d.UserId;
                fav.TouchUpInside += (s, e) => ToggleFavorite(driverId);

                card.AddSubview(name);
                card.AddSubview(car);
                card.AddSubview(distance);
                card.AddSubview(fav);

                card.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(name, NSLayoutAttribute.Top, NSLayoutRelation.Equal, card, NSLayoutAttribute.Top, 1, 10),
                    NSLayoutConstraint.Create(name, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),

                    NSLayoutConstraint.Create(distance, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, name, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(distance, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, fav, NSLayoutAttribute.Trailing, 1, 6),

                    NSLayoutConstraint.Create(fav, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, name, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(fav, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 8),
                    NSLayoutConstraint.Create(fav, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),
                    NSLayoutConstraint.Create(fav, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 34),

                    NSLayoutConstraint.Create(car, NSLayoutAttribute.Top, NSLayoutRelation.Equal, name, NSLayoutAttribute.Bottom, 1, 4),
                    NSLayoutConstraint.Create(car, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, card, NSLayoutAttribute.Leading, 1, 12),
                    NSLayoutConstraint.Create(car, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, card, NSLayoutAttribute.Trailing, 1, -12),
                    NSLayoutConstraint.Create(car, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, card, NSLayoutAttribute.Bottom, 1, -10),
                });

                _list.AddArrangedSubview(card);
            }
        }

        /// <summary>إضافة/إزالة سائق من المفضلة ثم إعادة الرسم.</summary>
        private void ToggleFavorite(int driverId)
        {
            var progress = Ui.ShowProgress("جارٍ التحديث…");
            Ui.Run(() =>
            {
                bool nowFavorite;
                try { nowFavorite = Api.ToggleFavorite(driverId); }
                catch { nowFavorite = false; }

                Ui.BeginOnMain(() =>
                {
                    progress.Dismiss();

                    if (nowFavorite) _favorites.Add(driverId);
                    else _favorites.Remove(driverId);

                    Ui.Toast(nowFavorite ? "تمت إضافة السائق للمفضلة" : "تمت إزالة السائق من المفضلة");
                    Render();
                });
            }, null, ex =>
            {
                progress.Dismiss();
                Ui.Toast(ex.Message);
            });
        }

        private void ShowMap()
        {
            if (_drivers.Count == 0)
            {
                Ui.Toast("لا يوجد سائقون لعرضهم");
                return;
            }

            var points = new List<double[]>();
            foreach (var d in _drivers)
            {
                if (d.Lat == 0 && d.Lng == 0) continue;
                points.Add(new[] { d.Lat, d.Lng });
            }

            if (points.Count == 0)
            {
                Ui.Toast("لا تتوفر إحداثيات للسائقين");
                return;
            }

            Push(new PointsMapViewController("السائقون القريبون", _lat, _lng, points, "#1976D2"));
        }
    }
}
