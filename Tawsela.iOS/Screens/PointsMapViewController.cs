using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CoreGraphics;
using Foundation;
using Tawsela.iOS.Platform;
using UIKit;
using WebKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// خريطة نقاط متعددة (بديل HeatmapActivity / NearbyDriversActivity / DriverMapActivity).
    /// تدعم التحديث التلقائي لمتابعة تحركات السائقين (بديل المؤقّت في DriverMapActivity).
    /// </summary>
    public class PointsMapViewController : BaseViewController
    {
        private readonly double _lat, _lng;
        private readonly List<double[]> _points;
        private readonly string _color;
        private readonly Func<List<double[]>> _refresh;
        private WKWebView _web;
        private NSTimer _timer;

        public PointsMapViewController(string title, double centerLat, double centerLng,
            List<double[]> points, string color = "#FF6F00", Func<List<double[]>> refresh = null) : base(title)
        {
            _lat = centerLat;
            _lng = centerLng;
            _points = points;
            _color = color;
            _refresh = refresh;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.White;

            _web = new WKWebView(CGRect.Empty, new WKWebViewConfiguration())
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Opaque = false,
                BackgroundColor = Theme.White
            };
            View.AddSubview(_web);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
            });

            LeafletMap.Load(_web, LeafletMap.BuildPointsHtml(Title, _lat, _lng, _points, _color));

            // متابعة مباشرة: تحديث كل 15 ثانية + زر تحديث يدوي
            if (_refresh != null)
            {
                NavigationItem.RightBarButtonItem = new UIBarButtonItem("تحديث", UIBarButtonItemStyle.Plain,
                    (s, e) => RefreshNow());

                _timer = NSTimer.CreateRepeatingScheduledTimer(15.0, _ => RefreshNow());
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (_timer != null) { _timer.Invalidate(); _timer.Dispose(); _timer = null; }
        }

        /// <summary>يجلب النقاط الجديدة ويرسلها للخريطة دون إعادة تحميل الصفحة.</summary>
        private void RefreshNow()
        {
            var refresh = _refresh;
            if (refresh == null) return;

            Ui.Run(() =>
            {
                var list = refresh();
                if (list == null) return;

                var sb = new StringBuilder("[");
                var first = true;
                foreach (var p in list)
                {
                    if (p == null || p.Length < 2) continue;
                    if (!first) sb.Append(',');
                    sb.Append('[')
                      .Append(p[0].ToString("0.######", CultureInfo.InvariantCulture))
                      .Append(',')
                      .Append(p[1].ToString("0.######", CultureInfo.InvariantCulture))
                      .Append(']');
                    first = false;
                }
                sb.Append(']');

                var json = sb.ToString();
                Ui.BeginOnMain(() => LeafletMap.RunJs(_web, "setPoints(" + json + ");"));
            }, null, ex => Ui.Toast(ex.Message));
        }
    }
}
