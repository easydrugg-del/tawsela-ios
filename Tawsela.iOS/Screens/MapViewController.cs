using System;
using CoreGraphics;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using UIKit;
using WebKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/MapActivity.cs — نفس الخريطة (Leaflet) وبنفس الوظائف:
    /// اختيار موقع بسحب العلامة، عرض نقطة واحدة، أو عرض مسار طلب.
    /// </summary>
    public class MapViewController : BaseViewController
    {
        public enum MapMode { Pick, View, Route }

        private readonly MapMode _mode;
        private readonly double _lat, _lng;
        private readonly double _fromLat, _fromLng, _toLat, _toLng;
        private readonly string _fromLabel, _toLabel, _viewLabel;
        private readonly Action<double, double, string> _onPicked;
        private readonly Func<double[]> _driverLocation;
        private NSTimer _trackTimer;

        private WKWebView _web;
        private UILabel _addressLabel;
        private UILabel _status;
        private UIButton _btnConfirm;
        private UIButton _btnGps;
        private MapScriptHandler _handler;

        private double _curLat, _curLng;
        private string _address = "";
        private bool _resolving;

        /* ---------- إنشاء الشاشات ---------- */

        private MapViewController(MapMode mode, double lat, double lng,
            Action<double, double, string> onPicked) : base("تحديد الموقع")
        {
            _mode = mode;
            _lat = _curLat = lat;
            _lng = _curLng = lng;
            _onPicked = onPicked;
        }

        private MapViewController(double fromLat, double fromLng, string fromLabel,
            double toLat, double toLng, string toLabel, Func<double[]> driverLocation = null) : base("مسار الطلب")
        {
            _mode = MapMode.Route;
            _fromLat = fromLat; _fromLng = fromLng; _fromLabel = fromLabel;
            _toLat = toLat; _toLng = toLng; _toLabel = toLabel;
            _driverLocation = driverLocation;
            _lat = _curLat = (fromLat + toLat) / 2;
            _lng = _curLng = (fromLng + toLng) / 2;
        }

        private MapViewController(double lat, double lng, string label) : base("موقع على الخريطة")
        {
            _mode = MapMode.View;
            _lat = _curLat = lat;
            _lng = _curLng = lng;
            _viewLabel = label ?? "";
        }

        /// <summary>فتح خريطة اختيار نقطة. عند التأكيد يُرجع الإحداثيات والاسم.</summary>
        public static MapViewController Pick(double lat, double lng, Action<double, double, string> onPicked)
        {
            return new MapViewController(MapMode.Pick, lat, lng, onPicked);
        }

        /// <summary>عرض نقطة واحدة بدون اختيار.</summary>
        public static MapViewController ShowPoint(double lat, double lng, string label)
        {
            return new MapViewController(lat, lng, label);
        }

        /// <summary>عرض مسار طلب (من/إلى).</summary>
        public static MapViewController ShowRoute(double fromLat, double fromLng, string fromLabel,
            double toLat, double toLng, string toLabel)
        {
            return new MapViewController(fromLat, fromLng, fromLabel, toLat, toLng, toLabel);
        }

        /// <summary>مسار الطلب مع تتبع مباشر لموقع السائق (بديل DriverMapActivity).</summary>
        public static MapViewController TrackDriver(double fromLat, double fromLng, string fromLabel,
            double toLat, double toLng, string toLabel, Func<double[]> driverLocation)
        {
            return new MapViewController(fromLat, fromLng, fromLabel, toLat, toLng, toLabel, driverLocation);
        }

        /* ---------- الواجهة ---------- */

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = Theme.White;

            var config = new WKWebViewConfiguration();
            config.UserContentController.AddScriptMessageHandler(
                _handler = new MapScriptHandler(OnPickFromMap), "pick");

            _web = new WKWebView(CGRect.Empty, config)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Opaque = false,
                BackgroundColor = Theme.White
            };
            View.AddSubview(_web);

            var bottom = new UIView { BackgroundColor = Theme.White, TranslatesAutoresizingMaskIntoConstraints = false };
            View.AddSubview(bottom);

            _addressLabel = MakeLabel("", 14f, Theme.TextPrimary, UIFontWeight.Medium, UITextAlignment.Right);
            _status = MakeLabel("", 12f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);

            bottom.AddSubview(_addressLabel);
            bottom.AddSubview(_status);

            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(_web, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Top, 1, 0),

                NSLayoutConstraint.Create(bottom, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(bottom, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, 0),
                NSLayoutConstraint.Create(bottom, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
            });

            bottom.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_addressLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(_addressLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(_addressLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Trailing, 1, -16),

                NSLayoutConstraint.Create(_status, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _addressLabel, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(_status, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(_status, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Trailing, 1, -16),
            });

            if (_mode == MapMode.Pick)
            {
                _btnGps = Controls.OutlineButton("استخدم موقعي");
                _btnGps.TouchUpInside += (s, e) => GetGps();
                bottom.AddSubview(_btnGps);

                _btnConfirm = Controls.PrimaryButton("تأكيد الموقع");
                _btnConfirm.TouchUpInside += (s, e) => Confirm();
                bottom.AddSubview(_btnConfirm);

                bottom.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(_btnGps, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _status, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(_btnGps, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Leading, 1, 16),
                    NSLayoutConstraint.Create(_btnGps, NSLayoutAttribute.Width, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Width, 1, -16 - 8),

                    NSLayoutConstraint.Create(_btnConfirm, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _btnGps, NSLayoutAttribute.Bottom, 1, 8),
                    NSLayoutConstraint.Create(_btnConfirm, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Leading, 1, 16),
                    NSLayoutConstraint.Create(_btnConfirm, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Trailing, 1, -16),
                    NSLayoutConstraint.Create(_btnConfirm, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Bottom, 1, -10),
                });
            }
            else
            {
                bottom.AddConstraint(NSLayoutConstraint.Create(_status, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, bottom, NSLayoutAttribute.Bottom, 1, -10));
            }

            LoadMap();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (_trackTimer != null) { _trackTimer.Invalidate(); _trackTimer.Dispose(); _trackTimer = null; }
            try
            {
                // منع تسريب المعالج بعد إغلاق الشاشة
                _web?.Configuration?.UserContentController?.RemoveScriptMessageHandler(new NSString("pick"));
            }
            catch { }
        }

        private void LoadMap()
        {
            string html;

            switch (_mode)
            {
                case MapMode.Route:
                    html = LeafletMap.BuildRouteHtml(_fromLat, _fromLng, _fromLabel,
                        _toLat, _toLng, _toLabel);
                    _addressLabel.Text = "من: " + _fromLabel;
                    _status.Text = "عرض مسار الطلب على الخريطة";
                    break;

                case MapMode.View:
                    html = LeafletMap.BuildViewHtml(_lat, _lng, _viewLabel);
                    _addressLabel.Text = _viewLabel;
                    _status.Text = "موقع المنطقة على الخريطة";
                    break;

                default:
                    html = LeafletMap.BuildPickerHtml(_lat, _lng);
                    _status.Text = "حرّك الخريطة أو اضغط على الموقع لتحديده";
                    break;
            }

            LeafletMap.Load(_web, html);

            if (_mode == MapMode.Pick)
            {
                // لو لم تُمرَّر إحداثيات، اطلب موقع المستخدم فورًا
                if (_lat == 0 && _lng == 0) GetGps();
                else RefreshAddress();
            }

            // تتبع مباشر لموقع السائق على نفس الخريطة
            if (_mode == MapMode.Route && _driverLocation != null)
            {
                _status.Text = "تتبع مباشر لموقع السائق — تحديث كل 5 ثوانٍ";
                _trackTimer = NSTimer.CreateRepeatingScheduledTimer(5.0, _ => RefreshDriverLocation());
                RefreshDriverLocation();
            }
        }

        /// <summary>يجلب إحداثيات السائق الحالية ويرسلها للخريطة (بديل تتبع DriverMapActivity).</summary>
        private void RefreshDriverLocation()
        {
            var getLocation = _driverLocation;
            if (getLocation == null) return;

            Ui.Run(() =>
            {
                var loc = getLocation();
                if (loc == null || loc.Length < 2 || (loc[0] == 0 && loc[1] == 0)) return;

                var lat = loc[0];
                var lng = loc[1];
                Ui.BeginOnMain(() => LeafletMap.RunJs(_web, string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "setDriver({0:0.000000},{1:0.000000});", lat, lng)));
            }, null, ex => AppServices.Log("TawselaMap", "driver: " + ex.Message));
        }

        /* ---------- الاختيار ---------- */

        private void OnPickFromMap(double lat, double lng)
        {
            _curLat = lat;
            _curLng = lng;
            Ui.BeginOnMain(() =>
            {
                _status.Text = "تم تحديد الموقع";
                RefreshAddress();
            });
        }

        private void GetGps()
        {
            LocationService.RequestPermission(granted =>
            {
                if (!granted)
                {
                    Ui.BeginOnMain(() =>
                    {
                        _status.Text = "لم يتم السماح بالوصول للموقع";
                        Ui.Confirm("صلاحية الموقع", "لتحديد موقعك على الخريطة اسمح بالوصول من الإعدادات.", "فتح الإعدادات",
                            () => LocationService.OpenSettings());
                    });
                    return;
                }

                Ui.BeginOnMain(() => _status.Text = "جارٍ تحديد موقعك…");

                LocationService.RequestOneShot((coord, accuracy) =>
                {
                    _curLat = coord.Latitude;
                    _curLng = coord.Longitude;

                    Ui.BeginOnMain(() =>
                    {
                        _status.Text = "تم تحديد موقعك عبر GPS";
                        LeafletMap.RunJs(_web, string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "setMarker({0:0.000000},{1:0.000000});", _curLat, _curLng));
                        RefreshAddress();
                    });
                }, err => Ui.BeginOnMain(() => _status.Text = err));
            });
        }

        /// <summary>يجلب اسم الشارع للإحداثيات ويعرضه بدل الأرقام (بديل RefreshAddress).</summary>
        private void RefreshAddress()
        {
            if (_mode != MapMode.Pick) return;
            if (_resolving) return;
            _resolving = true;

            var lat = _curLat;
            var lng = _curLng;
            Ui.BeginOnMain(() => _addressLabel.Text = "جارٍ تحديد اسم الشارع…");

            Ui.Run(
                () =>
                {
                    var addr = Geo.ReverseGeocode(lat, lng);
                    Ui.BeginOnMain(() =>
                    {
                        _resolving = false;
                        if (lat != _curLat || lng != _curLng) { RefreshAddress(); return; }

                        if (!string.IsNullOrWhiteSpace(addr))
                        {
                            _address = addr;
                            _addressLabel.Text = addr;
                        }
                        else
                        {
                            _address = "";
                            _addressLabel.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0:0.00000}, {1:0.00000}", lat, lng);
                        }
                    });
                },
                null,
                ex =>
                {
                    Ui.BeginOnMain(() =>
                    {
                        _resolving = false;
                        _addressLabel.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "{0:0.00000}, {1:0.00000}", lat, lng);
                    });
                });
        }

        private void Confirm()
        {
            if (_curLat == 0 && _curLng == 0)
            {
                Ui.Toast("حدد موقعًا على الخريطة أولًا");
                return;
            }

            var callback = _onPicked;
            var lat = _curLat;
            var lng = _curLng;
            var address = _address;

            if (NavigationController != null && NavigationController.ViewControllers.Length > 1)
                NavigationController.PopViewController(true);

            if (callback != null) callback(lat, lng, address);
        }
    }
}
