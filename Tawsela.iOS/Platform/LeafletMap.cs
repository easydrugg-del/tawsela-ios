using System;
using System.Globalization;
using Foundation;
using Tawsela.Core.Platform;
using Tawsela.Data;
using UIKit;
using WebKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل WebView + Assets/leaflet في أندرويد.
    ///
    /// على iOS لا يمكن قراءة ملفات الحزمة مباشرة من WKWebView،
    /// لذلك ننسخ مجلد leaflet مرة واحدة إلى Library ثم نكتب map.html هناك
    /// ونفتحه بـ LoadFileUrl مع صلاحية قراءة المجلد.
    /// </summary>
    public static class LeafletMap
    {
        private static string _folder;
        private static readonly object Sync = new object();

        /// <summary>مجلد العمل القابل للكتابة الذي يحتوي أصول leaflet.</summary>
        public static string Folder
        {
            get
            {
                if (_folder != null) return _folder;
                lock (Sync)
                {
                    if (_folder != null) return _folder;

                    var fm = NSFileManager.DefaultManager;
                    var library = fm.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User);
                    var baseDir = library != null && library.Length > 0
                        ? library[0].Path
                        : System.IO.Path.GetTempPath();

                    var target = System.IO.Path.Combine(baseDir, "leaflet");
                    try
                    {
                        if (!System.IO.Directory.Exists(target))
                            System.IO.Directory.CreateDirectory(target);

                        // انسخ الأصول من الحزمة (أول مرة فقط، وأيضًا بعد تحديث التطبيق)
                        var bundleDir = NSBundle.MainBundle.ResourcePath;
                        var src = System.IO.Path.Combine(bundleDir, "leaflet");
                        if (System.IO.Directory.Exists(src))
                        {
                            // نسخ متكرر: images/ ضرورية لأيقونات العلامات في Leaflet،
                            // والنسخ غير المتكرر كان يترك الخريطة بعلامات مكسورة.
                            foreach (var file in System.IO.Directory.GetFiles(src, "*", System.IO.SearchOption.AllDirectories))
                            {
                                var rel = file.Substring(src.Length).TrimStart('\\', '/');
                                var dest = System.IO.Path.Combine(target, rel);
                                try
                                {
                                    var dir = System.IO.Path.GetDirectoryName(dest);
                                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                                        System.IO.Directory.CreateDirectory(dir);
                                    System.IO.File.Copy(file, dest, true);
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }

                    _folder = target;
                    return _folder;
                }
            }
        }

        /// <summary>يكتب HTML في مجلد leaflet ويفتحه داخل WKWebView.</summary>
        public static void Load(WKWebView web, string html)
        {
            if (web == null) return;
            try
            {
                var folder = Folder;
                var file = System.IO.Path.Combine(folder, "map.html");
                System.IO.File.WriteAllText(file, html ?? "");

                var url = NSUrl.FromFilename(file);
                var readAccess = NSUrl.FromFilename(folder);
                web.LoadFileUrl(url, readAccess);
            }
            catch (Exception ex)
            {
                AppServices.Log("TawselaMap", "load failed: " + ex.Message);
            }
        }

        /// <summary>تنفيذ JavaScript داخل الخريطة (بديل LoadUrl("javascript:...")).</summary>
        public static void RunJs(WKWebView web, string js)
        {
            if (web == null || string.IsNullOrEmpty(js)) return;
            web.EvaluateJavaScript(js, null);
        }

        /* ---------- بناء صفحات الخريطة (نفس منطق MapActivity) ---------- */

        private static string JsStr(string s)
        {
            return "'" + (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'") + "'";
        }

        private static string Num(double v)
        {
            return v.ToString("0.000000", CultureInfo.InvariantCulture);
        }

        private const string Head =
            "<html><head><meta charset='utf-8'/>" +
            "<meta name='viewport' content='initial-scale=1.0,maximum-scale=1.0,user-scalable=no'/>" +
            "<link rel='stylesheet' href='leaflet.css'/>" +
            "<script src='leaflet.js'></script>" +
            "<style>html,body{height:100%;margin:0}#map{width:100%;height:100%}" +
            ".leaflet-control-attribution{font-size:9px}</style>" +
            "</head><body><div id='map'></div><script>";

        /// <summary>خريطة اختيار نقطة واحدة (سحب العلامة أو الضغط على الخريطة).</summary>
        public static string BuildPickerHtml(double lat, double lng)
        {
            return Head +
                "var map=L.map('map').setView([" + Num(lat) + "," + Num(lng) + "],15);" +
                MapTiles.TileLayerJs("map") +
                "var marker=L.marker([" + Num(lat) + "," + Num(lng) + "],{draggable:true}).addTo(map);" +
                "var myLoc=L.circleMarker([" + Num(lat) + "," + Num(lng) + "]," +
                "{radius:8,color:'#4285F4',fillColor:'#4285F4',fillOpacity:0.9,weight:2}).addTo(map);" +
                "function notify(lat,lng){window.webkit.messageHandlers.pick.postMessage({lat:lat,lng:lng});}" +
                "marker.on('dragend',function(e){var p=e.target.getLatLng();notify(p.lat,p.lng);});" +
                "map.on('click',function(e){marker.setLatLng(e.latlng);notify(e.latlng.lat,e.latlng.lng);});" +
                "function setMarker(a,b){marker.setLatLng([a,b]);map.setView([a,b],16);}" +
                "function setMyLoc(a,b){myLoc.setLatLng([a,b]);}" +
                "</script></body></html>";
        }

        /// <summary>عرض نقطة واحدة (موقع منطقة) بعلامة واسم، بدون اختيار.</summary>
        public static string BuildViewHtml(double lat, double lng, string label)
        {
            var html = Head +
                "var map=L.map('map').setView([" + Num(lat) + "," + Num(lng) + "],16);" +
                MapTiles.TileLayerJs("map") +
                "L.marker([" + Num(lat) + "," + Num(lng) + "]).addTo(map)";

            if (!string.IsNullOrEmpty(label))
                html += ".bindPopup(" + JsStr(label) + ").openPopup()";

            return html + ";</script></body></html>";
        }

        /// <summary>عرض مسار طلب: نقطتان + خط بينهما.</summary>
        public static string BuildRouteHtml(double fromLat, double fromLng, string fromLabel,
            double toLat, double toLng, string toLabel, double driverLat = 0, double driverLng = 0)
        {
            var html = Head +
                "var map=L.map('map').setView([" + Num((fromLat + toLat) / 2) + "," + Num((fromLng + toLng) / 2) + "],13);" +
                MapTiles.TileLayerJs("map") +
                "var m1=L.marker([" + Num(fromLat) + "," + Num(fromLng) + "]).addTo(map)" +
                ".bindPopup(" + JsStr("من: " + fromLabel) + ").openPopup();" +
                "var m2=L.marker([" + Num(toLat) + "," + Num(toLng) + "]).addTo(map)" +
                ".bindPopup(" + JsStr("إلى: " + toLabel) + ");" +
                "var line=L.polyline([[" + Num(fromLat) + "," + Num(fromLng) + "],[" +
                Num(toLat) + "," + Num(toLng) + "]],{color:'#FF6F00',weight:4}).addTo(map);" +
                "map.fitBounds(line.getBounds(),{padding:[40,40]});" +
                // علامة السائق تُنشأ عند أول تحديث ثم تتحرك (تتبع مباشر)
                "var driver=null;" +
                "function setDriver(a,b){if(!driver){driver=L.circleMarker([a,b]," +
                "{radius:10,color:'#1976D2',fillColor:'#1976D2',fillOpacity:0.9,weight:2})" +
                ".addTo(map).bindPopup('السائق');}else{driver.setLatLng([a,b]);}}";

            if (driverLat != 0 || driverLng != 0)
            {
                html += "setDriver(" + Num(driverLat) + "," + Num(driverLng) + ");";
            }

            return html + "</script></body></html>";
        }

        /// <summary>خريطة حرارية/نقاط متعددة (بديل HeatmapActivity و NearbyDriversActivity).</summary>
        public static string BuildPointsHtml(string title, double centerLat, double centerLng,
            System.Collections.Generic.List<double[]> points, string color = "#FF6F00")
        {
            var html = Head +
                "var map=L.map('map').setView([" + Num(centerLat) + "," + Num(centerLng) + "],13);" +
                MapTiles.TileLayerJs("map") +
                // طبقة قابلة للاستبدال حتى يمكن تحديث النقاط دون إعادة تحميل الصفحة
                "var grp=L.layerGroup().addTo(map);" +
                "var markerColor='" + color + "';" +
                "function addPoint(a,b){L.circleMarker([a,b],{radius:7,color:markerColor," +
                "fillColor:markerColor,fillOpacity:0.85,weight:2}).addTo(grp);}" +
                "function setPoints(arr){grp.clearLayers();if(!arr)return;" +
                "for(var i=0;i<arr.length;i++){addPoint(arr[i][0],arr[i][1]);}}" +
                "function setCenter(a,b,z){map.setView([a,b],z||map.getZoom());}";

            if (points != null)
            {
                foreach (var p in points)
                {
                    if (p == null || p.Length < 2) continue;
                    html += "addPoint(" + Num(p[0]) + "," + Num(p[1]) + ");";
                }
            }

            return html + "</script></body></html>";
        }
    }

    /// <summary>مستقبل رسائل JavaScript من الخريطة (بديل MapClient.shouldOverrideUrlLoading).</summary>
    public sealed class MapScriptHandler : NSObject, IWKScriptMessageHandler
    {
        private readonly Action<double, double> _onPick;

        public MapScriptHandler(Action<double, double> onPick)
        {
            _onPick = onPick;
        }

        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            try
            {
                var dict = message.Body as NSDictionary;
                if (dict == null) return;

                var latObj = dict.ObjectForKey(new NSString("lat"));
                var lngObj = dict.ObjectForKey(new NSString("lng"));
                if (latObj == null || lngObj == null) return;

                var lat = (latObj as NSNumber)?.DoubleValue ?? 0;
                var lng = (lngObj as NSNumber)?.DoubleValue ?? 0;
                if (_onPick != null) _onPick(lat, lng);
            }
            catch { }
        }
    }
}
