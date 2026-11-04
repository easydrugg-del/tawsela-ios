using System;
using CoreLocation;
using Foundation;
using UIKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل LocationManager في أندرويد: CoreLocation.
    /// - يطلب صلاحية "أثناء الاستخدام" فقط (لا يوجد Always في هذا التطبيق).
    /// - يوفر آخر موقع معروف + تحديثًا واحدًا (RequestLocation) + متابعة مستمرة للسائق.
    /// </summary>
    public static class LocationService
    {
        private sealed class Delegate : CLLocationManagerDelegate
        {
            public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
            {
                if (locations == null || locations.Length == 0) return;
                var loc = locations[locations.Length - 1];
                LocationService.Store(loc);
            }

            public override void Failed(CLLocationManager manager, NSError error)
            {
                LocationService.RaiseError(error == null ? "تعذّر تحديد الموقع" : error.LocalizedDescription);
            }

            public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            {
                LocationService._onAuth?.Invoke(status);
            }
        }

        private static CLLocationManager _manager;
        private static Delegate _delegate;
        private static CLLocation _last;
        private static Action<CLAuthorizationStatus> _onAuth;
        private static Action<CLLocationCoordinate2D, double> _onLocation;
        private static Action<string> _onError;
        private static readonly object Sync = new object();

        public static void Init()
        {
            lock (Sync)
            {
                if (_manager != null) return;
                _manager = new CLLocationManager();
                _delegate = new Delegate();
                _manager.Delegate = _delegate;
                _manager.DesiredAccuracy = 25; // متر — كافٍ لحساب الأجرة
                _manager.DistanceFilter = 10;
            }
        }

        public static bool IsAuthorized
        {
            get
            {
                Init();
                var status = _manager.AuthorizationStatus;
                return status == CLAuthorizationStatus.AuthorizedWhenInUse
                    || status == CLAuthorizationStatus.AuthorizedAlways;
            }
        }

        public static bool IsDenied
        {
            get
            {
                Init();
                var status = _manager.AuthorizationStatus;
                return status == CLAuthorizationStatus.Denied || status == CLAuthorizationStatus.Restricted;
            }
        }

        /// <summary>آخر موقع معروف (قد يكون قديمًا).</summary>
        public static CLLocationCoordinate2D? LastKnown
        {
            get
            {
                Init();
                var loc = _manager.Location ?? _last;
                if (loc == null) return null;
                return loc.Coordinate;
            }
        }

        /// <summary>طلب الصلاحية ثم تنفيذ callback بالنتيجة.</summary>
        public static void RequestPermission(Action<bool> done)
        {
            Init();
            if (IsAuthorized) { if (done != null) done(true); return; }
            if (IsDenied)
            {
                if (done != null) done(false);
                return;
            }

            _onAuth = status =>
            {
                var ok = status == CLAuthorizationStatus.AuthorizedWhenInUse
                         || status == CLAuthorizationStatus.AuthorizedAlways;
                if (!ok && status == CLAuthorizationStatus.Denied)
                {
                    _onAuth = null;
                    if (done != null) done(false);
                }
                else if (ok)
                {
                    _onAuth = null;
                    if (done != null) done(true);
                }
            };
            _manager.RequestWhenInUseAuthorization();
        }

        /// <summary>
        /// تحديث واحد للموقع (بديل RequestSingleUpdate).
        /// يمرر آخر موقع معروف فورًا إن وُجد، ثم النتيجة الأدق عند وصولها.
        /// </summary>
        public static void RequestOneShot(Action<CLLocationCoordinate2D, double> onLocation, Action<string> onError)
        {
            Init();
            _onLocation = onLocation;
            _onError = onError;

            if (!IsAuthorized)
            {
                if (_onError != null) _onError("صلاحية الموقع غير ممنوحة");
                return;
            }

            var known = _manager.Location;
            if (known != null && _onLocation != null)
            {
                // موقع حديث (أقل من دقيقتين) يُستخدم فورًا
                var age = Math.Abs(known.Timestamp.SecondsSinceReferenceDate -
                                   NSDate.Now.SecondsSinceReferenceDate);
                if (age < 120)
                    _onLocation(known.Coordinate, known.HorizontalAccuracy);
            }

            _manager.RequestLocation();
        }

        /// <summary>متابعة مستمرة (تتبع السائق).</summary>
        public static void StartUpdating(Action<CLLocationCoordinate2D, double> onLocation, Action<string> onError)
        {
            Init();
            _onLocation = onLocation;
            _onError = onError;
            if (!IsAuthorized) return;
            _manager.StartUpdatingLocation();
        }

        public static void StopUpdating()
        {
            Init();
            try { _manager.StopUpdatingLocation(); } catch { }
        }

        internal static void Store(CLLocation loc)
        {
            if (loc == null) return;
            _last = loc;
            var handler = _onLocation;
            if (handler != null)
            {
                try { handler(loc.Coordinate, loc.HorizontalAccuracy); }
                catch { }
            }
        }

        internal static void RaiseError(string message)
        {
            var handler = _onError;
            if (handler != null)
            {
                try { handler(message); } catch { }
            }
        }

        /// <summary>فتح إعدادات التطبيق (عندما يرفض المستخدم الصلاحية نهائيًا).</summary>
        public static void OpenSettings()
        {
            try
            {
                var url = new NSUrl(UIApplication.OpenSettingsUrlString);
                UIApplication.SharedApplication.OpenUrl(url, new NSDictionary(), null);
            }
            catch { }
        }

        /// <summary>حساب المسافة بالمتر بين نقطتين (بديل Location.DistanceTo).</summary>
        public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            var a = new CLLocation(lat1, lng1);
            var b = new CLLocation(lat2, lng2);
            if (a == null || b == null) return 0;
            return a.DistanceFrom(b);
        }
    }
}
