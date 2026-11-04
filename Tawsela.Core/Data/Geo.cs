using System;
using System.Collections.Generic;
using System.Net;

namespace Tawsela.Data
{
    public static class Geo
    {
        private const double EarthRadiusKm = 6371.0;

        public static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            var dLat = DegToRad(lat2 - lat1);
            var dLng = DegToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegToRad(lat1)) * Math.Cos(DegToRad(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        public static decimal EstimateFare(double km, decimal farePerKm)
        {
            if (km < 2) km = 2;
            var raw = (decimal)km * farePerKm;
            return Math.Max(1, Math.Round(raw, 0));
        }

        public static double DegToRad(double deg) { return deg * Math.PI / 180.0; }

        public static string KmText(double km)
        {
            if (km < 1) return (int)(km * 1000) + " متر";
            return km.ToString("0.0") + " كم";
        }

        /// <summary>يرجع اسم الشارع (طريق + حي + مدينة) بدل الإحداثيات قدر الإمكان.</summary>
        public static string ReverseGeocode(double lat, double lng)
        {
            try
            {
                var url = "https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=" +
                          lat.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                          "&lon=" + lng.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                          "&accept-language=ar&zoom=18";
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Tawsela/1.0");
                    var json = wc.DownloadString(url);
                    // الأولوية للعنوان بالكامل كما يرجع من الخريطة
                    var display = ExtractJson(json, "display_name");
                    if (!string.IsNullOrEmpty(display)) return display;
                    // احتياطي: تركيب مختصر من المكونات (طريق + رقم + حي + مدينة)
                    var road = ExtractJson(json, "road");
                    var house = ExtractJson(json, "house_number");
                    var suburb = ExtractJson(json, "suburb");
                    if (string.IsNullOrEmpty(suburb)) suburb = ExtractJson(json, "neighbourhood");
                    if (string.IsNullOrEmpty(suburb)) suburb = ExtractJson(json, "quarter");
                    var city = ExtractJson(json, "city");
                    if (string.IsNullOrEmpty(city)) city = ExtractJson(json, "town");
                    if (string.IsNullOrEmpty(city)) city = ExtractJson(json, "village");
                    if (string.IsNullOrEmpty(city)) city = ExtractJson(json, "state");
                    if (!string.IsNullOrEmpty(road))
                    {
                        var name = road;
                        if (!string.IsNullOrEmpty(house)) name += " " + house;
                        if (!string.IsNullOrEmpty(suburb)) name += "، " + suburb;
                        if (!string.IsNullOrEmpty(city)) name += "، " + city;
                        return name;
                    }
                }
            }
            catch { }
            return lat.ToString("0.000") + ", " + lng.ToString("0.000");
        }

        /// <summary>تحويل عنوان نصي إلى إحداثيات (للعناوين المُملاة صوتياً).</summary>
        public static bool ForwardGeocode(string address, out double lat, out double lng)
        {
            lat = 0; lng = 0;
            try
            {
                if (string.IsNullOrEmpty(address)) return false;
                var url = "https://nominatim.openstreetmap.org/search?format=json&limit=1&accept-language=ar&q=" +
                          System.Uri.EscapeDataString(address);
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Tawsela/1.0");
                    var json = wc.DownloadString(url);
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    double la, lo;
                    if (double.TryParse(ExtractJson(json, "lat"), System.Globalization.NumberStyles.Any, inv, out la) &&
                        double.TryParse(ExtractJson(json, "lon"), System.Globalization.NumberStyles.Any, inv, out lo) &&
                        la != 0 && lo != 0)
                    {
                        lat = la; lng = lo;
                        return true;
                    }
                }
            }
            catch { }
            lat = 0; lng = 0;
            return false;
        }

        private static string ExtractJson(string json, string key)
        {
            var search = "\"" + key + "\"";
            var idx = json.IndexOf(search);
            if (idx < 0) return "";
            var start = json.IndexOf("\"", idx + search.Length + 1);
            if (start < 0) return "";
            start++;
            var end = json.IndexOf("\"", start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        /// <summary>اقتراح للأثناء الكتابة: اسم مختصر + عنوان كامل + إحداثيات.</summary>
        public class PlaceSuggestion
        {
            public string Label = "";   // نص مختصر للعرض
            public string Full = "";    // العنوان الكامل (يُحفظ مع الطلب)
            public string Sub = "";     // سطر مساعد: "1.2 كم · ≈4 د" أو "آخر وجهة"
            public string City = "";    // المدينة (للترتيب/المطابقة)
            public string Area = "";    // الحي/المنطقة
            public string Kind = "";    // street/poi/house/area/city/other
            public double Lat, Lng;
            public double Score;        // ترتيب داخلي (أقل = أفضل)
        }

        /// <summary>معلومة شارع واحد قرب نقطة (لترشيح الوجهة).</summary>
        public class StreetInfo
        {
            public string Name = "";
            public string Class = "";            // تصنيف OSM: residential/primary/…
            public double CenterLat, CenterLng;  // مركز الشارع — للترتيب بالقرب فقط
            public double DistanceKm;
        }

        /// <summary>شوارع مُرشّحة + الحي/المدينة المحيطة بنقطة الانطلاق.</summary>
        public class StreetsNearResult
        {
            public string Locality = "";   // مثل: "مدينة نصر، القاهرة"
            public string City = "";       // مثل: "القاهرة"
            public List<StreetInfo> NearStreets = new List<StreetInfo>(); // ≤ 1.5 كم من الانطلاق
            public List<StreetInfo> MainStreets = new List<StreetInfo>(); // أهم شوارع المدينة (طرق رئيسية)
        }

        /// <summary>نتيجة reverse: المنطقة (حي) والمدينة المحيطة بنقطة.</summary>
        public class LocalityResult
        {
            public string Area = "";   // مثل: "مدينة نصر"
            public string City = "";   // مثل: "القاهرة"
            public string Label = "";  // مثل: "مدينة نصر، القاهرة"
        }

        /// <summary>استخراج الحي/المدينة لعن طريق reverse مع تفاصيل العنوان.</summary>
        public static LocalityResult ReversePlace(double lat, double lng)
        {
            var res = new LocalityResult();
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var url = "https://nominatim.openstreetmap.org/reverse?format=jsonv2&addressdetails=1" +
                          "&lat=" + lat.ToString("0.000000", inv) +
                          "&lon=" + lng.ToString("0.000000", inv) +
                          "&accept-language=ar&zoom=17";
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Tawsela/1.0");
                    var json = wc.DownloadString(url);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        if (!doc.RootElement.TryGetProperty("address", out var a))
                            return res;
                        res.Area = FirstOf(a, "suburb", "quarter", "neighbourhood", "city_district");
                        res.City = FirstOf(a, "city", "town", "village", "state");
                        var parts = new List<string>();
                        if (res.Area != "") parts.Add(res.Area);
                        if (res.City != "" && res.City != res.Area) parts.Add(res.City);
                        res.Label = string.Join("، ", parts);
                    }
                }
            }
            catch { }
            return res;
        }

        /// <summary>استخراج الحي/المدينة لعن طريق reverse مع تفاصيل العنوان.</summary>
        public static string LocalityOf(double lat, double lng)
        {
            return ReversePlace(lat, lng).Label;
        }

        /// <summary>المدينة المحيطة بنقطة (بديع reverse).</summary>
        public static string CityOf(double lat, double lng)
        {
            return ReversePlace(lat, lng).City;
        }

        private static Dictionary<string, double[]> _cityBBoxCache;

        /// <summary>
        /// حدود المدينة (bbox: [جنوب, شمال, غرب, شرق]) من Nominatim — بكاش.
        /// قيم مضبوطة إن لم تُستخرج.
        /// </summary>
        public static double[] CityBBox(string cityName)
        {
            var name = (cityName ?? "").Trim();
            if (name == "") return null;
            if (_cityBBoxCache == null) _cityBBoxCache = new Dictionary<string, double[]>(System.StringComparer.OrdinalIgnoreCase);
            try
            {
                lock (typeof(Geo))
                {
                    double[] cached;
                    if (_cityBBoxCache.TryGetValue(name, out cached)) return cached;
                }
                var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=eg&q=" +
                          System.Uri.EscapeDataString(name);
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Tawsela/1.0");
                    var json = wc.DownloadString(url);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            if (!el.TryGetProperty("boundingbox", out var bb)) continue;
                            if (bb.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
                            var arr = new double[4];
                            if (bb.GetArrayLength() < 4) continue;
                            if (!bb[0].TryGetDouble(out arr[0])) continue; // south
                            if (!bb[1].TryGetDouble(out arr[1])) continue; // north
                            if (!bb[2].TryGetDouble(out arr[2])) continue; // west
                            if (!bb[3].TryGetDouble(out arr[3])) continue; // east
                            lock (typeof(Geo))
                            {
                                _cityBBoxCache[name] = arr;
                            }
                            return arr;
                        }
                    }
                }
                lock (typeof(Geo)) { _cityBBoxCache[name] = null; }
            }
            catch { }
            return null;
        }

        private static string FirstOf(System.Text.Json.JsonElement obj, params string[] keys)
        {
            foreach (var k in keys)
                if (obj.TryGetProperty(k, out var v))
                {
                    var s = v.GetString() ?? "";
                    if (s != "") return s;
                }
            return "";
        }

        /// <summary>
        /// يعطّي أسماء شوارع حقيقية (OSM) حول نقطة الانطلاق عبر Overpass API:
        /// شوارع قريبة (≤ 1.5 كم من أي تصنيف) ثم أهم شوارع المدينة
        /// (طرق رئيسية primary/trunk/secondary داخل 6 كم).
        /// استعلامان خفيفان متتاليان بدل استعلام bbox كامل المدينة (يعطل Overpass).
        /// عند توفر onNearReady تُستدعى بعد اكتمال الشوارع القريبة لعرضها فورًا.
        /// </summary>
        public static StreetsNearResult StreetsNear(double lat, double lng, Action<StreetsNearResult> onPartial = null, bool includeNear = true)
        {
            var res = new StreetsNearResult();
            const double NearKm = 1.5;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                var place = ReversePlace(lat, lng);
                res.Locality = place.Label;
                res.City = place.City;
                Log("reverse ok, city=" + place.City);
            }
            catch
            {
                Log("reverse failed");
            }

            // استعلام 1: شوارع قريبة من الانطلاق (خفيف وسريع)
            if (includeNear)
            {
                try
                {
                    var json = Overpass("way[highway~\"^(residential|primary|secondary|tertiary|living_street|unclassified)$\"][name](around:1500," +
                                lat.ToString("0.000000", inv) + "," + lng.ToString("0.000000", inv) +
                                ");out tags center 40;");
                    res.NearStreets = ParseStreets(json, lat, lng, 0, NearKm, 10);
                    Log("near streets: " + res.NearStreets.Count);
                    if (res.NearStreets.Count > 0 && onPartial != null) onPartial(res);
                }
                catch (Exception ex)
                {
                    Log("near query failed: " + ex.Message);
                }
            }

            // استعلام 2: أهم شوارع المدينة — طرق رئيسية/فرعية فقط ضمن 6 كم
            try
            {
                var json = Overpass("way[highway~\"^(primary|trunk|secondary|tertiary|primary_link|secondary_link|tertiary_link|trunk_link)$\"][name](around:6000," +
                            lat.ToString("0.000000", inv) + "," + lng.ToString("0.000000", inv) +
                            ");out tags center 60;");
                res.MainStreets = ParseStreets(json, lat, lng, NearKm, double.MaxValue, 10);
                Log("main streets: " + res.MainStreets.Count);
            }
            catch (Exception ex)
            {
                Log("main query failed: " + ex.Message);
            }
            return res;
        }

        private static List<StreetInfo> ParseStreets(string json, double lat, double lng,
            double minKm, double maxKm, int max)
        {
            var seen = new Dictionary<string, StreetInfo>(System.StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    if (!doc.RootElement.TryGetProperty("elements", out var els)) return new List<StreetInfo>();
                    foreach (var el in els.EnumerateArray())
                    {
                        if (!el.TryGetProperty("tags", out var tg)) continue;
                        if (!tg.TryGetProperty("name", out var nm)) continue;
                        var name = (nm.GetString() ?? "").Trim();
                        if (name == "") continue;
                        string cls = tg.TryGetProperty("highway", out var hw) ? (hw.GetString() ?? "") : "";
                        double cLat = 0, cLng = 0;
                        if (el.TryGetProperty("center", out var c))
                        {
                            if (c.TryGetProperty("lat", out var ca)) ca.TryGetDouble(out cLat);
                            if (c.TryGetProperty("lon", out var co)) co.TryGetDouble(out cLng);
                        }
                        var d = DistanceKm(lat, lng, cLat, cLng);
                        if (d < minKm || d > maxKm) continue;
                        if (seen.TryGetValue(name, out var ex))
                        {
                            if (d < ex.DistanceKm) { ex.CenterLat = cLat; ex.CenterLng = cLng; ex.DistanceKm = d; }
                            continue;
                        }
                        seen[name] = new StreetInfo { Name = name, Class = cls, CenterLat = cLat, CenterLng = cLng, DistanceKm = d };
                    }
                }
            }
            catch (Exception ex) { Log("parse failed: " + ex.Message); }
            return TopByDistance(seen, max);
        }

        private static System.Net.Http.HttpClient _overpassHttp;
        // الخادم الأساسي + مرآة احتياطية (كيومي — كثيرًا ما تنجح حين يتباطأ الأساس)
        private static readonly string[] OverpassServers =
        {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://maps.mail.ru/osm/tools/overpass/api/interpreter"
        };

        /// <summary>استعلام Overpass مع مهلة 12 ثانية ومحاولة مرآة احتياطية عند الفشل.</summary>
        private static string Overpass(string query)
        {
            if (_overpassHttp == null)
            {
                _overpassHttp = new System.Net.Http.HttpClient();
                _overpassHttp.Timeout = TimeSpan.FromSeconds(12);
                _overpassHttp.DefaultRequestHeaders.Add("User-Agent", "Tawsela/1.0");
            }
            var q = "[out:json][timeout:10];" + query;
            var data = "?data=" + System.Uri.EscapeDataString(q);
            Exception last = null;
            foreach (var server in OverpassServers)
            {
                try
                {
                    var json = _overpassHttp.GetStringAsync(server + data).GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(json)) return json;
                }
                catch (Exception ex) { last = ex; Log("overpass " + server + " failed: " + ex.Message); }
            }
            throw last ?? new Exception("overpass unreachable");
        }

        private static void Log(string msg)
        {
            // بديل Android.Util.Log — يعمل على المنصتين عبر طبقة المنصة
            Tawsela.Core.Platform.AppServices.Log("TawselaGeo", msg);
        }

        private static List<StreetInfo> TopByDistance(Dictionary<string, StreetInfo> dict, int max)
        {
            var list = new List<StreetInfo>(dict.Values);
            list.Sort((a, b) => a.DistanceKm.CompareTo(b.DistanceKm));
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }

        private static System.Net.Http.HttpClient _searchHttp;
        private static DateTime _lastSearchAtUtc;
        private static readonly Dictionary<string, List<PlaceSuggestion>> _searchCache =
            new Dictionary<string, List<PlaceSuggestion>>();

        /// <summary>
        /// بحث عناوين/معالم مقترحة أثناء الكتابة — مجاني بالكامل:
        /// Nominatim (مع ترجيح القرب) ثم Photon كاحتياطي عند قلة النتائج، مع كاش محلي.
        /// يُعيد فارغ: لا نتائج، null: تم تجاهل الطلب (سعة الخدمة/حرف أقل من اللازم).
        /// </summary>
        public static List<PlaceSuggestion> SearchPlaces(string query, int limit = 6, double refLat = 0, double refLng = 0)
        {
            var q = (query ?? "").Trim();
            if (q.Length < 3) return null;
            var qNorm = Norm(q);
            var key = qNorm + "|" + Math.Round(refLat, 2).ToString(System.Globalization.CultureInfo.InvariantCulture) +
                      "|" + Math.Round(refLng, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
            lock (typeof(Geo))
            {
                List<PlaceSuggestion> hit;
                if (_searchCache.TryGetValue(key, out hit)) return hit;
                // Nominatim: طلب واحد كحد أقصى في الثانية
                if (_lastSearchAtUtc != default(DateTime) &&
                    (DateTime.UtcNow - _lastSearchAtUtc).TotalMilliseconds < 900)
                    return null;
                _lastSearchAtUtc = DateTime.UtcNow;
            }
            if (_searchHttp == null)
            {
                _searchHttp = new System.Net.Http.HttpClient();
                _searchHttp.Timeout = TimeSpan.FromSeconds(15);
                _searchHttp.DefaultRequestHeaders.Add("User-Agent", "Tawsela/1.0");
                _searchHttp.DefaultRequestHeaders.Add("Accept-Language", "ar");
            }

            var res = new List<PlaceSuggestion>();
            try { res = SearchNominatim(q, limit, refLat, refLng); }
            catch { }
            // احتياطي مجاني (بدون مفتاح) عند قلة/فشل نتائج Nominatim
            if (res.Count < 3)
            {
                try
                {
                    foreach (var extra in SearchPhoton(q, limit, refLat, refLng))
                    {
                        var dup = false;
                        foreach (var r in res)
                            if (string.Equals(r.Label, extra.Label, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.Full, extra.Full, StringComparison.OrdinalIgnoreCase))
                            { dup = true; break; }
                        if (!dup) res.Add(extra);
                        if (res.Count >= limit) break;
                    }
                }
                catch { }
            }
            // دمج/تنقية/ترتيب: حذف نتائج الدولة/المحافظة الخشنة، وإزالة التكرار،
            // ثم ترتيب حسب نوع النتيجة + تطابق النص + القرب من الانطلاق.
            res = CleanAndRank(res, qNorm, refLat, refLng, limit);
            ApplyDistanceSub(res, refLat, refLng);
            lock (typeof(Geo))
            {
                if (_searchCache.Count > 150) _searchCache.Clear();
                _searchCache[key] = res;
            }
            return res;
        }

        private static List<PlaceSuggestion> SearchNominatim(string q, int limit, double refLat, double refLng)
        {
            var res = new List<PlaceSuggestion>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=" + limit +
                      "&countrycodes=eg&addressdetails=1&dedupe=1&accept-language=ar&q=" +
                      System.Uri.EscapeDataString(q);
            if (refLat != 0 || refLng != 0)
            {
                // ترجيح النتائج القريبة من الانطلاق دون تقييد صارم
                var dLat = 0.35; var dLng = 0.35;
                url += "&viewbox=" + (refLng - dLng).ToString("0.0000", inv) + "," + (refLat + dLat).ToString("0.0000", inv) + "," +
                       (refLng + dLng).ToString("0.0000", inv) + "," + (refLat - dLat).ToString("0.0000", inv) + "&bounded=0";
            }
            var json = _searchHttp.GetStringAsync(url).GetAwaiter().GetResult();
            using (var doc = System.Text.Json.JsonDocument.Parse(json))
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var s = new PlaceSuggestion();
                    if (el.TryGetProperty("display_name", out var dn))
                        s.Full = dn.GetString() ?? "";
                    if (s.Full == "") continue;
                    string nm = el.TryGetProperty("name", out var nmEl) ? (nmEl.GetString() ?? "") : "";
                    var parts = s.Full.Split(new[] { "،", "," }, StringSplitOptions.RemoveEmptyEntries);
                    var first = parts.Length > 0 ? parts[0].Trim() : s.Full;
                    var second = parts.Length > 1 ? parts[1].Trim() : "";
                    s.Label = (!string.IsNullOrEmpty(nm) && nm.Length <= 40) ? nm.Trim() : first;
                    if (second != "" && !string.Equals(second, s.Label, StringComparison.OrdinalIgnoreCase))
                        s.Label += "، " + second;
                    // المدينة/الحي + نوع النتيجة لترتيب الاقتراحات لاحقًا
                    string cls = el.TryGetProperty("class", out var cEl) ? (cEl.GetString() ?? "") : "";
                    string typ = el.TryGetProperty("type", out var tEl) ? (tEl.GetString() ?? "") : "";
                    if (el.TryGetProperty("address", out var addr) &&
                        addr.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        s.City = FirstStr(addr, "city", "town", "village", "municipality", "state");
                        s.Area = FirstStr(addr, "suburb", "quarter", "neighbourhood", "city_district", "road");
                    }
                    s.Kind = KindOf(cls, typ, s.City != "" || s.Area != "");
                    string lat = el.TryGetProperty("lat", out var la) ? (la.GetString() ?? "") : "";
                    string lon = el.TryGetProperty("lon", out var lo) ? (lo.GetString() ?? "") : "";
                    double.TryParse(lat, System.Globalization.NumberStyles.Any, inv, out s.Lat);
                    double.TryParse(lon, System.Globalization.NumberStyles.Any, inv, out s.Lng);
                    if (s.Lat == 0 && s.Lng == 0) continue;
                    res.Add(s);
                }
            }
            return res;
        }

        /// <summary>Photon (komoot) — إكمال عناوين مجاني بدون مفتاح، أفضل في الأخطاء الإملائية.</summary>
        private static List<PlaceSuggestion> SearchPhoton(string q, int limit, double refLat, double refLng)
        {
            var res = new List<PlaceSuggestion>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var url = "https://photon.komoot.io/api/?q=" + System.Uri.EscapeDataString(q) + "&limit=" + Math.Max(limit, 4);
            if (refLat != 0 || refLng != 0)
                url += "&lat=" + refLat.ToString("0.000000", inv) + "&lon=" + refLng.ToString("0.000000", inv);
            var json = _searchHttp.GetStringAsync(url).GetAwaiter().GetResult();
            using (var doc = System.Text.Json.JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("features", out var feats)) return res;
                foreach (var f in feats.EnumerateArray())
                {
                    if (!f.TryGetProperty("properties", out var pr)) continue;
                    if (!f.TryGetProperty("geometry", out var geo)) continue;
                    if (!geo.TryGetProperty("coordinates", out var coords)) continue;
                    if (coords.GetArrayLength() < 2) continue;
                    double lat = 0, lng = 0;
                    coords[1].TryGetDouble(out lat);
                    coords[0].TryGetDouble(out lng);
                    if (lat == 0 && lng == 0) continue;
                    var name = GetStr(pr, "name");
                    if (name == "") name = (GetStr(pr, "street") + " " + GetStr(pr, "housenumber")).Trim();
                    if (name == "") continue;
                    var city = GetStr(pr, "city");
                    if (city == "") city = GetStr(pr, "district");
                    if (city == "") city = GetStr(pr, "state");
                    var country = GetStr(pr, "country");
                    var label = city != "" && city != name ? name + "، " + city : name;
                    var full = name;
                    if (city != "" && city != name) full += "، " + city;
                    if (country != "") full += "، " + country;
                    res.Add(new PlaceSuggestion
                    {
                        Label = label,
                        Full = full,
                        Lat = lat,
                        Lng = lng,
                        City = city,
                        Area = GetStr(pr, "locality"),
                        Kind = KindOf(GetStr(pr, "osm_key"), GetStr(pr, "osm_value"), city != "")
                    });
                }
            }
            return res;
        }

        private static string GetStr(System.Text.Json.JsonElement obj, string key)
        {
            if (obj.TryGetProperty(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String)
                return (v.GetString() ?? "").Trim();
            return "";
        }

        /// <summary>تطبيع عربي للبحث/الدمج: أ/إ/آ→ا، ة→ه، ى→ي، وإزالة التشكيل والتطويل.</summary>
        private static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch >= '\u064B' && ch <= '\u065F') continue;  // تشكيل
                if (ch == '\u0640') continue;                    // تطويل
                switch (ch)
                {
                    case 'أ': case 'إ': case 'آ': case 'ٱ': sb.Append('ا'); break;
                    case 'ة': sb.Append('ه'); break;
                    case 'ى': sb.Append('ي'); break;
                    case 'ؤ': sb.Append('و'); break;
                    case 'ئ': sb.Append('ي'); break;
                    default: sb.Append(char.ToLowerInvariant(ch)); break;
                }
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
        }

        private static string FirstStr(System.Text.Json.JsonElement obj, params string[] keys)
        {
            foreach (var k in keys)
            {
                var v = GetStr(obj, k);
                if (v != "") return v;
            }
            return "";
        }

        /// <summary>تصنيف النتيجة من وسوم OSM لتقدير أهميتها في قائمة الاقتراحات.</summary>
        private static string KindOf(string cls, string typ, bool hasCity)
        {
            cls = (cls ?? "").ToLowerInvariant();
            typ = (typ ?? "").ToLowerInvariant();
            if (cls == "highway" || typ == "road" || typ == "street" || typ == "residential" ||
                typ == "primary" || typ == "secondary" || typ == "tertiary" ||
                typ == "motorway" || typ == "trunk" || typ == "service" || typ == "footway")
                return "street";
            if (cls == "building" || typ == "house" || typ == "apartments")
                return "house";
            if (cls == "amenity" || cls == "shop" || cls == "tourism" || cls == "leisure" ||
                cls == "office" || cls == "healthcare" || cls == "craft")
                return "poi";
            if (typ == "country") return "country";
            if (cls == "place")
            {
                if (typ == "suburb" || typ == "neighbourhood" || typ == "quarter" ||
                    typ == "city_block" || typ == "borough")
                    return "area";
                if (typ == "city" || typ == "town" || typ == "village" || typ == "hamlet")
                    return "city";
            }
            if (cls == "boundary" || cls == "place")
            {
                if (typ == "state" || typ == "governorate" || typ == "region")
                    return hasCity ? "city" : "state";
                return hasCity ? "city" : "area";
            }
            if (cls == "natural" || cls == "waterway" || cls == "landuse") return "area";
            return "other";
        }

        /// <summary>إزالة التكرار والنتائج الخشنة ثم ترتيب: نوع النتيجة + تطابق النص + القرب.</summary>
        private static List<PlaceSuggestion> CleanAndRank(List<PlaceSuggestion> list, string qNorm,
            double refLat, double refLng, int limit)
        {
            var clean = new List<PlaceSuggestion>();
            foreach (var s in list)
            {
                if (s.Kind == "country" || s.Kind == "state") continue; // دولة/محافظة فقط
                if (string.IsNullOrEmpty(s.Label)) continue;
                var dup = false;
                foreach (var k in clean)
                {
                    if (Norm(k.Label) == Norm(s.Label)) { dup = true; break; }
                    if (k.Kind == s.Kind && DistanceKm(k.Lat, k.Lng, s.Lat, s.Lng) < 0.05)
                    { dup = true; break; }
                }
                if (dup) continue;
                s.Score = RankScore(s, qNorm, refLat, refLng);
                clean.Add(s);
            }
            clean.Sort((a, b) => a.Score == b.Score ? 0 : (a.Score < b.Score ? -1 : 1));
            if (limit > 0 && clean.Count > limit)
                clean.RemoveRange(limit, clean.Count - limit);
            return clean;
        }

        private static double RankScore(PlaceSuggestion s, string qNorm, double refLat, double refLng)
        {
            double score = 100;
            switch (s.Kind)
            {
                case "street": score -= 18; break;
                case "house": score -= 24; break;
                case "poi": score -= 8; break;
                case "area": score += 6; break;
                case "city": score += 18; break;
            }
            var ln = Norm(s.Label);
            if (!string.IsNullOrEmpty(qNorm))
            {
                if (ln == qNorm) score -= 30;
                else if (ln.StartsWith(qNorm)) score -= 18;
                else if (ln.Contains(qNorm)) score -= 8;
            }
            if (refLat != 0 || refLng != 0)
                score += Math.Min(DistanceKm(refLat, refLng, s.Lat, s.Lng) * 1.5, 25);
            return score;
        }

        private static void ApplyDistanceSub(List<PlaceSuggestion> list, double refLat, double refLng)
        {
            if (refLat == 0 && refLng == 0) return;
            foreach (var s in list)
            {
                var d = DistanceKm(refLat, refLng, s.Lat, s.Lng);
                s.Sub = KmText(d) + " · ≈" + EtaMinutes(d) + " د";
            }
        }

        /// <summary>أقرب معالم مشهورة حول نقطة (مسجد/مول/مستشفى/جامعة/سوق/محطة).</summary>
        public static List<PlaceSuggestion> NearbyLandmarks(double lat, double lng, int limit = 5)
        {
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var p = lat.ToString("0.000000", inv) + "," + lng.ToString("0.000000", inv);
                var q = "(" +
                        "nwr[amenity~\"^(hospital|university|place_of_worship|marketplace|bus_station|pharmacy)$\"][name](around:3000," + p + ");" +
                        "nwr[shop~\"^(mall|department_store|supermarket)$\"][name](around:3000," + p + ");" +
                        "nwr[railway=station][name](around:3000," + p + ");" +
                        ");out tags center 60;";
                var list = ParseNamedPlaces(Overpass(q), lat, lng);
                if (list.Count > limit) list.RemoveRange(limit, list.Count - limit);
                return list;
            }
            catch { return new List<PlaceSuggestion>(); }
        }

        /// <summary>
        /// أقرب أماكن من تصنيف محدد حول نقطة (مجاني عبر Overpass):
        /// mall / hospital / station / university / mosque / market / pharmacy.
        /// </summary>
        public static List<PlaceSuggestion> NearbyCategory(double lat, double lng, string category, int limit = 8)
        {
            string filter;
            switch (category)
            {
                case "mall": filter = "nwr[shop~\"^(mall|department_store)$\"][name]"; break;
                case "hospital": filter = "nwr[amenity=hospital][name]"; break;
                case "station": filter = "nwr[railway=station][name]"; break;
                case "university": filter = "nwr[amenity=university][name]"; break;
                case "mosque": filter = "nwr[amenity=place_of_worship][name]"; break;
                case "market": filter = "nwr[amenity=marketplace][name]"; break;
                case "pharmacy": filter = "nwr[amenity=pharmacy][name]"; break;
                default: return new List<PlaceSuggestion>();
            }
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var p = lat.ToString("0.000000", inv) + "," + lng.ToString("0.000000", inv);
                var q = filter + "(around:6000," + p + ");out tags center 60;";
                var list = ParseNamedPlaces(Overpass(q), lat, lng);
                if (list.Count > limit) list.RemoveRange(limit, list.Count - limit);
                return list;
            }
            catch { return new List<PlaceSuggestion>(); }
        }

        /// <summary>تحويل عناصر Overpass المسماة إلى اقتراحات مرتبة بالقرب.</summary>
        private static List<PlaceSuggestion> ParseNamedPlaces(string json, double lat, double lng)
        {
            var seen = new Dictionary<string, PlaceSuggestion>(System.StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    if (!doc.RootElement.TryGetProperty("elements", out var els))
                        return new List<PlaceSuggestion>();
                    foreach (var el in els.EnumerateArray())
                    {
                        if (!el.TryGetProperty("tags", out var tg)) continue;
                        if (!tg.TryGetProperty("name", out var nm)) continue;
                        var name = (nm.GetString() ?? "").Trim();
                        if (name == "" || seen.ContainsKey(name)) continue;
                        double cLat = 0, cLng = 0;
                        if (el.TryGetProperty("lat", out var nla)) nla.TryGetDouble(out cLat);
                        if (el.TryGetProperty("lon", out var nlo)) nlo.TryGetDouble(out cLng);
                        if (cLat == 0 && cLng == 0 && el.TryGetProperty("center", out var c))
                        {
                            if (c.TryGetProperty("lat", out var ca)) ca.TryGetDouble(out cLat);
                            if (c.TryGetProperty("lon", out var co)) co.TryGetDouble(out cLng);
                        }
                        if (cLat == 0 && cLng == 0) continue;
                        var d = DistanceKm(lat, lng, cLat, cLng);
                        seen[name] = new PlaceSuggestion
                        {
                            Label = name,
                            Full = name,
                            Sub = KmText(d) + " · ≈" + EtaMinutes(d) + " د",
                            Lat = cLat,
                            Lng = cLng
                        };
                    }
                }
            }
            catch { }
            var list = new List<PlaceSuggestion>(seen.Values);
            list.Sort((a, b) => DistanceKm(lat, lng, a.Lat, a.Lng).CompareTo(DistanceKm(lat, lng, b.Lat, b.Lng)));
            return list;
        }

        /// <summary>زمن وصول تقديري بالدقائق (سرعة وسط مدينة 20 كم/س).</summary>
        public static int EtaMinutes(double km)
        {
            if (km <= 0) return 1;
            var m = (int)Math.Ceiling(km / 20.0 * 60.0);
            return m < 1 ? 1 : m;
        }
    }
}
