using Tawsela.Core.Platform;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tawsela.Models;

namespace Tawsela.Data
{
    /// <summary>
    /// عميل الـ API الوسيط — التطبيق لا يعرف سوى رابط الـ API والتوكن.
    /// لا توجد بيانات قاعدة (سيرفر/sa) في التطبيق إطلاقاً عبر هذا المسار.
    /// </summary>
    public static class Api
    {
        private const string Pref = "api_settings";
        private const string KeyUrl = "base_url";
        private const string KeyToken = "token";
        private const string KeyTokenEncrypted = "token_encrypted";
        private const string KeyUser = "user_json";

        private static readonly object ClientLock = new object();
        private static HttpClient _sharedClient;
        private static string _sharedClientBaseUrl;

        /// <summary>رابط الـ API الثابت (قابل للتجاوز من الإعدادات إن لزم).</summary>
        public static string DefaultUrl = "https://116.202.66.142:5001";

        /// <summary>بصمة شهادة السيرفر الذاتية (تثبيت الشهادة — تمنع انتحالها).</summary>
        public static string PinnedThumbprint = "999B9BFAFE39D9F59C0ACB25BCF8F58D183D2C98";

        public static string BaseUrl()
        {
            var s = AppServices.Prefs.Get(Pref + "." + KeyUrl, "");
            return string.IsNullOrEmpty(s) ? DefaultUrl : s.Trim().TrimEnd('/');
        }
        public static void SetBaseUrl(string url)
        {
            AppServices.Prefs.Set(Pref + "." + KeyUrl, (url ?? "").Trim().TrimEnd('/'));
        }
        public static void SaveToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                AppServices.Prefs.Remove(Pref + "." + KeyToken);
                AppServices.Prefs.Remove(Pref + "." + KeyTokenEncrypted);
                return;
            }

            // تشفير التوكن بمفتاح الجهاز (iOS Keychain / Android Keystore عبر IAppSecure)
            var encrypted = SecureStore.Encrypt(token);
            if (!string.IsNullOrEmpty(encrypted))
            {
                AppServices.Prefs.Set(Pref + "." + KeyToken, encrypted);
                AppServices.Prefs.SetBool(Pref + "." + KeyTokenEncrypted, true);
            }
            else
            {
                // بديل نصي فقط إن تعذّر التشفير (نادرًا) — يُشفّر في الحفظ التالي
                AppServices.Prefs.Set(Pref + "." + KeyToken, token);
                AppServices.Prefs.SetBool(Pref + "." + KeyTokenEncrypted, false);
            }
        }
        public static string LoadToken()
        {
            var raw = AppServices.Prefs.Get(Pref + "." + KeyToken, "");
            if (string.IsNullOrEmpty(raw)) return "";

            if (!AppServices.Prefs.GetBool(Pref + "." + KeyTokenEncrypted, false))
                return raw; // توكن قديم مخزّن كنص — سيُشفّر عند الحفظ التالي

            var decrypted = SecureStore.Decrypt(raw);
            if (!string.IsNullOrEmpty(decrypted)) return decrypted;

            // تعذر فك التشفير (مفتاح مفقود/تغيّر) — نطلب تسجيل الدخول من جديد
            AppServices.Prefs.Remove(Pref + "." + KeyToken);
            AppServices.Prefs.Remove(Pref + "." + KeyTokenEncrypted);
            AppServices.Prefs.Remove(Pref + "." + KeyUser);
            return "";
        }
        public static void ClearToken()
        {
            AppServices.Prefs.Remove(Pref + "." + KeyToken);
            AppServices.Prefs.Remove(Pref + "." + KeyTokenEncrypted);
        }
        /// <summary>حذف التوكن وبيانات المستخدم المحفوظة (تسجيل خروج كامل).</summary>
        public static void ClearSession()
        {
            AppServices.Prefs.Remove(Pref + "." + KeyToken);
            AppServices.Prefs.Remove(Pref + "." + KeyTokenEncrypted);
            AppServices.Prefs.Remove(Pref + "." + KeyUser);
            Session.Reset();
        }
        /// <summary>HttpClient واحد مشترك (إعادة استخدام الاتصال/TLS بدل عميل لكل طلب).</summary>
        private static HttpClient Client()
        {
            var baseUrl = BaseUrl();
            lock (ClientLock)
            {
                if (_sharedClient != null && _sharedClientBaseUrl == baseUrl)
                    return _sharedClient;

                _sharedClient?.Dispose();
                var handler = new HttpClientHandler();
                // قبول الشهادة الذاتية فقط إذا طابقت البصمة المثبتة (حماية من التنصت والانتحال)
                handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None) return true;
                    if (cert == null) return false;
                    try { return string.Equals(cert.Thumbprint, PinnedThumbprint, StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                };
                _sharedClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
                _sharedClientBaseUrl = baseUrl;
                return _sharedClient;
            }
        }

        private static string Send(System.Net.Http.HttpRequestMessage req)
        {
            var token = LoadToken();
            if (token != "")
                req.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using (var resp = Client().SendAsync(req).GetAwaiter().GetResult())
            {
                var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode) return text;

                if ((int)resp.StatusCode == 401)
                {
                    ClearSession();
                    throw new Exception("انتهت صلاحية الجلسة، يرجى تسجيل الدخول من جديد");
                }
                if ((int)resp.StatusCode == 403)
                    throw new Exception("لا تملك صلاحية تنفيذ هذا الإجراء");
                throw new Exception(ReadError(text, (int)resp.StatusCode));
            }
        }

        private static string Post(string path, object body)
        {
            var json = JsonSerializer.Serialize(body);
            using (var req = new System.Net.Http.HttpRequestMessage(
                       System.Net.Http.HttpMethod.Post, BaseUrl() + path))
            {
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return Send(req);
            }
        }

        private static string Put(string path, object body)
        {
            var json = JsonSerializer.Serialize(body);
            using (var req = new System.Net.Http.HttpRequestMessage(
                       System.Net.Http.HttpMethod.Put, BaseUrl() + path))
            {
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return Send(req);
            }
        }

        private static string Delete(string path)
        {
            using (var req = new System.Net.Http.HttpRequestMessage(
                       System.Net.Http.HttpMethod.Delete, BaseUrl() + path))
                return Send(req);
        }

        public static string Get(string path)
        {
            using (var req = new System.Net.Http.HttpRequestMessage(
                       System.Net.Http.HttpMethod.Get, BaseUrl() + path))
                return Send(req);
        }

        private static string ReadError(string json, int statusCode = 0)
        {
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("error", out var e))
                        return e.GetString() ?? "خطأ غير معروف";
                }
            }
            catch { }
            if (statusCode >= 500)
                return "الخادم غير متاح حاليًا، حاول بعد قليل";
            if (statusCode > 0)
                return "تعذر تنفيذ الطلب (رمز " + statusCode + ")";
            return "خطأ في الاتصال بالخادم";
        }

        /// <summary>دخول عبر الـ API — يملأ Session ويحفظ التوكن.</summary>
        public static void Login(string userName, string password)
        {
            var text = Post("/api/auth/login", new { UserName = userName ?? "", Password = password ?? "" });
            using (var doc = JsonDocument.Parse(text))
            {
                var root = doc.RootElement;
                SaveToken(root.GetProperty("token").GetString() ?? "");
                ApplyUser(root.GetProperty("user"));
            }
        }

        /// <summary>ملء الجلسة من استجابة المستخدم وحفظها لاستعادة الجلسة لاحقًا.</summary>
        private static void ApplyUser(JsonElement u)
        {
            Session.UserId = u.TryGetProperty("id", out var id) ? id.GetInt32() : 0;
            Session.UserName = u.TryGetProperty("userName", out var un) ? (un.GetString() ?? "") : "";
            Session.FullName = u.TryGetProperty("fullName", out var fn) ? (fn.GetString() ?? "") : "";
            Session.Phone = u.TryGetProperty("phone", out var ph) ? (ph.GetString() ?? "") : "";
            Session.Role = u.TryGetProperty("role", out var rl) ? (rl.GetString() ?? "") : "";
            if (Session.Role == "") Session.Role = "عميل";
            Session.Governorate = u.TryGetProperty("governorate", out var g) ? (g.GetString() ?? "") : "";
            Session.City = u.TryGetProperty("city", out var c) ? (c.GetString() ?? "") : "";

            AppServices.Prefs.Set(Pref + "." + KeyUser, u.GetRawText());
        }

        /// <summary>هل انتهت صلاحية التوكن (JWT)؟ true عند أي خطأ في القراءة.</summary>
        public static bool TokenExpired(string token)
        {
            try
            {
                var parts = (token ?? "").Split('.');
                if (parts.Length < 2) return true;
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using (var doc = JsonDocument.Parse(json))
                {
                    if (!doc.RootElement.TryGetProperty("exp", out var exp)) return false;
                    return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow;
                }
            }
            catch { return true; }
        }

        /// <summary>هل يوجد توكن محفوظ وما زال صالحًا؟ (دون ملء الجلسة)</summary>
        public static bool HasValidSession()
        {
            var token = LoadToken();
            return !string.IsNullOrEmpty(token) && !TokenExpired(token);
        }

        /// <summary>استعادة الجلسة من التوكن المحفوظ عند تشغيل التطبيق. تُرجع false إذا انتهت الصلاحية.</summary>
        public static bool RestoreSession()
        {
            var token = LoadToken();
            if (string.IsNullOrEmpty(token) || TokenExpired(token))
            {
                ClearSession();
                return false;
            }

            var raw = AppServices.Prefs.Get(Pref + "." + KeyUser, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(raw))
                        ApplyUser(doc.RootElement);
                }
                catch { }
            }

            // توكن قديم محفوظ قبل إضافة user_json: استخراج البيانات من claims
            if (Session.UserId <= 0)
            {
                var claims = TokenClaims(token);
                if (claims != null)
                {
                    Session.UserId = ParseInt(claims, "uid");
                    Session.UserName = Claim(claims, "unique_name") ?? Claim(claims, "/name") ?? "";
                    Session.FullName = Claim(claims, "fullName") ?? "";
                    Session.Role = Claim(claims, "role") ?? "عميل";
                    if (Session.Role == "") Session.Role = "عميل";
                    if (Session.UserId > 0)
                    {
                        var json = JsonSerializer.Serialize(new
                        {
                            id = Session.UserId,
                            userName = Session.UserName,
                            fullName = Session.FullName,
                            phone = "",
                            role = Session.Role,
                            governorate = "",
                            city = ""
                        });
                        AppServices.Prefs.Set(Pref + "." + KeyUser, json);
                    }
                }
            }
            return Session.UserId > 0;
        }

        private static string Claim(Dictionary<string, string> claims, string name)
        {
            if (claims == null) return null;
            string v;
            if (claims.TryGetValue(name, out v)) return v;
            foreach (var kv in claims)
                if (kv.Key.EndsWith(name, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            return null;
        }

        private static int ParseInt(Dictionary<string, string> claims, string name)
        {
            var v = Claim(claims, name);
            int i;
            return int.TryParse(v, out i) ? i : 0;
        }

        /// <summary>قراءة claims من حمولة JWT (بدون تحقق توقيع — التحقق على الخادم).</summary>
        private static Dictionary<string, string> TokenClaims(string token)
        {
            try
            {
                var parts = (token ?? "").Split('.');
                if (parts.Length < 2) return null;
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var result = new Dictionary<string, string>();
                using (var doc = JsonDocument.Parse(json))
                    foreach (var p in doc.RootElement.EnumerateObject())
                        result[p.Name] = p.Value.ValueKind == JsonValueKind.String
                            ? (p.Value.GetString() ?? "")
                            : p.Value.ToString();
                return result;
            }
            catch { return null; }
        }

        /* ---------- أدوات JSON ---------- */

        private static object? ToClr(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.String: return e.GetString();
                case JsonValueKind.Number:
                    if (e.TryGetDecimal(out var d)) return d;
                    if (e.TryGetDouble(out var db)) return db;
                    return e.GetRawText();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                default: return null;
            }
        }

        private static Dictionary<string, object> Row(JsonElement el)
        {
            var d = new Dictionary<string, object>();
            foreach (var p in el.EnumerateObject())
                d[p.Name] = ToClr(p.Value);
            return d;
        }

        private static List<Dictionary<string, object>> Rows(string json)
        {
            var list = new List<Dictionary<string, object>>();
            using (var doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var el in doc.RootElement.EnumerateArray())
                        list.Add(Row(el));
            }
            return list;
        }

        private static decimal Dec(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
                decimal.TryParse(v.ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var x);
                return x;
            }
            return 0m;
        }

        private static string Qs(Dictionary<string, string> q)
        {
            var sb = new StringBuilder("?");
            foreach (var kv in q)
            {
                if (sb.Length > 1) sb.Append("&");
                sb.Append(Uri.EscapeDataString(kv.Key)).Append("=").Append(Uri.EscapeDataString(kv.Value ?? ""));
            }
            return sb.ToString();
        }

        /* ---------- الطلبات ---------- */

        public static List<Order> GetOrders(string status = "", string scope = "")
        {
            var list = new List<Order>();
            foreach (var r in Rows(Get("/api/orders" + Qs(new Dictionary<string, string> { { "status", status }, { "scope", scope } }))))
                list.Add(Order.FromRow(r));
            return list;
        }

        public static Order GetOrder(int orderId)
        {
            var text = Get("/api/orders/" + orderId);
            using (var doc = JsonDocument.Parse(text))
                return Order.FromRow(Row(doc.RootElement));
        }

        public static int CreateOrder(double fromLat, double fromLng, string fromAddr,
            double toLat, double toLng, string toAddr, string note, decimal estFare,
            int serviceTypeId, string paymentMethod, DateTime scheduledAt,
            string couponCode, decimal discount, decimal tip,
            double stopLat, double stopLng, string stopAddr,
            decimal offeredPrice, bool bidding, string floorInfo, int maxWaitMinutes = 30)
        {
            var text = Post("/api/orders", new
            {
                FromLat = fromLat, FromLng = fromLng, FromAddress = fromAddr ?? "",
                ToLat = toLat, ToLng = toLng, ToAddress = toAddr ?? "",
                Note = note ?? "", EstimatedFare = estFare, ServiceTypeId = serviceTypeId,
                PaymentMethod = paymentMethod ?? "",
                ScheduledAt = scheduledAt == default(DateTime) ? null : (DateTime?)scheduledAt,
                CouponCode = couponCode ?? "",
                Discount = discount, Tip = tip,
                StopLat = stopLat, StopLng = stopLng, StopAddress = stopAddr ?? "",
                OfferedPrice = offeredPrice,
                Bidding = bidding, FloorInfo = floorInfo ?? "",
                MaxWaitMinutes = maxWaitMinutes <= 0 ? 30 : maxWaitMinutes
            });
            using (var doc = JsonDocument.Parse(text))
                return doc.RootElement.GetProperty("id").GetInt32();
        }

        public static void UpdateStatus(int orderId, string status)
        {
            Post("/api/orders/" + orderId + "/status", new { Status = status ?? "" });
        }

        /// <summary>بدء الرحلة بعد التحقق من رمز PIN على السيرفر (السائق فقط).</summary>
        public static void StartOrder(int orderId, string pin)
        {
            Post("/api/orders/" + orderId + "/start", new { Pin = pin ?? "" });
        }

        /// <summary>العميل يؤكد وصول السائق (مع تحقق الموقع على السيرفر).</summary>
        public static void ConfirmArrival(int orderId)
        {
            Post("/api/orders/" + orderId + "/confirm-arrival", new { });
        }

        /// <summary>العميل يبلّغ أن السائق لم يصل فعلاً.</summary>
        public static void ReportNotArrived(int orderId, string reason)
        {
            Post("/api/orders/" + orderId + "/report-not-arrived", new { Reason = reason ?? "" });
        }

        public static void AssignDriver(int orderId)
        {
            Post("/api/orders/" + orderId + "/assign", new { });
        }

        public static void CompleteOrder(int orderId, decimal fare)
        {
            Post("/api/orders/" + orderId + "/complete", new { Fare = fare });
        }

        public static void CancelOrder(int orderId, string reason)
        {
            Post("/api/orders/" + orderId + "/cancel", new { Reason = reason ?? "" });
        }

        /* ---------- توصيلات المطاعم ---------- */

        public static List<Dictionary<string, object>> GetAvailableRestaurantDeliveries(double? lat, double? lng)
        {
            var qs = "";
            if (lat.HasValue && lng.HasValue)
                qs = "?lat=" + lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                     "&lng=" + lng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var list = new List<Dictionary<string, object>>();
            foreach (var r in Rows(Get("/api/restaurant-delivery/available" + qs)))
                list.Add(r);
            return list;
        }

        public static string AcceptRestaurantDelivery(int id)
        {
            return Post("/api/restaurant-delivery/" + id + "/accept", new { });
        }

        public static string ArrivedAtRestaurant(int id)
        {
            return Post("/api/restaurant-delivery/" + id + "/arrived", new { });
        }

        /// <summary>تفاصيل توصيل مطعم معين (سائق مسند / مطعم صاحب الطلب / إدارة).</summary>
        public static Dictionary<string, object>? GetRestaurantDelivery(int id)
        {
            var json = Get("/api/restaurant-delivery/" + id);
            using (var doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    return Row(doc.RootElement);
            }
            return null;
        }

        public static string PickedUpRestaurantDelivery(int id)
        {
            return Post("/api/restaurant-delivery/" + id + "/picked-up", new { });
        }

        public static string DeliveredRestaurantDelivery(int id)
        {
            return Post("/api/restaurant-delivery/" + id + "/delivered", new { });
        }

        public static List<Dictionary<string, object>> GetMyRestaurantDeliveries()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var r in Rows(Get("/api/restaurant-delivery/my-orders")))
                list.Add(r);
            return list;
        }

        public static List<Dictionary<string, object>> RestaurantDeliveryAreas()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var r in Rows(Get("/api/restaurant-delivery/areas")))
                list.Add(r);
            return list;
        }

        public static Dictionary<string, object>? MyRestaurant()
        {
            using (var doc = JsonDocument.Parse(Get("/api/restaurant-delivery/my-restaurant")))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    return Row(doc.RootElement);
            }
            return null;
        }

        public static string RequestRestaurantDelivery(int areaId, string customerName, string customerPhone,
            string pickupAddress, double pickupLat, double pickupLng,
            string deliveryAddress, double deliveryLat, double deliveryLng, decimal fare)
        {
            return Post("/api/restaurant-delivery/request", new
            {
                AreaId = areaId,
                AreaName = "",
                CustomerName = customerName ?? "",
                CustomerPhone = customerPhone ?? "",
                PickupAddress = pickupAddress ?? "",
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DeliveryAddress = deliveryAddress ?? "",
                DeliveryLat = deliveryLat,
                DeliveryLng = deliveryLng,
                Fare = fare
            });
        }

        public static List<Dictionary<string, object>> AllDeliveryAreas()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var r in Rows(Get("/api/admin/delivery-areas"))) list.Add(r);
            return list;
        }

        public static string SaveDeliveryArea(int id, string name, string city, string governorate,
            decimal price, double lat, double lng, bool active)
        {
            return Post("/api/admin/delivery-areas", new
            {
                Id = id, Name = name ?? "", City = city ?? "", Governorate = governorate ?? "",
                Price = price, Lat = lat, Lng = lng, RadiusMeters = 0, IsActive = active
            });
        }

        public static List<Dictionary<string, object>> AllRestaurants()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var r in Rows(Get("/api/admin/restaurants"))) list.Add(r);
            return list;
        }

        public static string SaveRestaurant(int id, string name, int ownerUserId, string phone,
            string address, int areaId, double lat, double lng, bool active)
        {
            return Post("/api/admin/restaurants", new
            {
                Id = id, Name = name ?? "", OwnerUserId = ownerUserId > 0 ? ownerUserId : (int?)null,
                Phone = phone ?? "", Address = address ?? "", AreaId = areaId > 0 ? areaId : (int?)null,
                Lat = lat, Lng = lng, IsActive = active
            });
        }

        public static string CreateRestaurantAccount(int restaurantId, string userName, string password,
            string fullName, string phone)
        {
            return Post("/api/admin/restaurants/" + restaurantId + "/account", new
            {
                UserId = 0,
                UserName = userName ?? "",
                Password = password ?? "",
                FullName = fullName ?? "",
                Phone = phone ?? ""
            });
        }

        /* ---------- المحفظة ---------- */

        public static decimal WalletBalance()
        {
            using (var doc = JsonDocument.Parse(Get("/api/wallet/balance")))
                return Dec(doc.RootElement, "balance");
        }

        public static List<WalletTxn> WalletTxns()
        {
            var list = new List<WalletTxn>();
            foreach (var r in Rows(Get("/api/wallet/txns")))
                list.Add(WalletTxn.FromRow(r));
            return list;
        }

        /// <summary>حركات محفظة سائق معين (للإدارة) — يتطلب endpoint الخادم الجديد.</summary>
        public static List<WalletTxn> DriverTxns(int userId)
        {
            var list = new List<WalletTxn>();
            foreach (var r in Rows(Get("/api/admin/wallets/txns" + Qs(new Dictionary<string, string> { { "userId", userId.ToString() } }))))
                list.Add(WalletTxn.FromRow(r));
            return list;
        }

        public static void RequestTopUp(decimal amount, string senderPhone, string receiptRef, string receiptImage = "")
        {
            Post("/api/wallet/request-topup", new { Amount = amount, SenderPhone = senderPhone ?? "", ReceiptRef = receiptRef ?? "", ReceiptImage = receiptImage ?? "" });
        }

        public static List<Dictionary<string, object>> MyTopUps()
        {
            return Rows(Get("/api/wallet/my-topups"));
        }

        public static void WalletTransfer(string toPhone, decimal amount)
        {
            Post("/api/wallet/transfer", new { ToPhone = toPhone ?? "", Amount = amount });
        }

        /* ---------- إشعارات / إعدادات / إعلانات / طوارئ ---------- */

        public static List<Notification> UserNotifications()
        {
            var list = new List<Notification>();
            foreach (var r in Rows(Get("/api/notifications")))
                list.Add(Notification.FromRow(r));
            return list;
        }

        public static void MarkAllRead()
        {
            Post("/api/notifications/read-all", new { });
        }

        public static string PaymentPhone()
        {
            using (var doc = JsonDocument.Parse(Get("/api/settings/payment-phone")))
                return doc.RootElement.TryGetProperty("phone", out var v) ? (v.GetString() ?? "") : "";
        }

        public static decimal DriverAcceptFee()
        {
            using (var doc = JsonDocument.Parse(Get("/api/settings/driver-fee")))
                return Dec(doc.RootElement, "fee");
        }

        public static void GetAppUpdate(out string version, out string url)
        {
            version = ""; url = "";
            using (var doc = JsonDocument.Parse(Get("/api/settings/app-update")))
            {
                if (doc.RootElement.TryGetProperty("version", out var v)) version = v.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("url", out var u)) url = u.GetString() ?? "";
            }
        }

        public static List<Dictionary<string, object>> ActiveAds(string gov, string city)
        {
            return Rows(Get("/api/ads/active" + Qs(new Dictionary<string, string> { { "gov", gov ?? "" }, { "city", city ?? "" } })));
        }

        public static List<string> EmergencyTypes()
        {
            var list = new List<string>();
            using (var doc = JsonDocument.Parse(Get("/api/emergency/types")))
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var el in doc.RootElement.EnumerateArray())
                        list.Add(el.GetString() ?? "");
            return list;
        }

        public static void ReportEmergency(string type, string address)
        {
            Post("/api/emergency/report", new { Type = type ?? "", Address = address ?? "" });
        }

        public static void SendEmergencyDetail(string detail)
        {
            Post("/api/emergency/notify", new { Detail = detail ?? "" });
        }

        /* ---------- خدمات / كوبونات / أماكن ---------- */

        public static List<ServiceType> ServiceTypes()
        {
            var list = new List<ServiceType>();
            foreach (var r in Rows(Get("/api/services")))
                list.Add(ServiceType.FromRow(r));
            return list;
        }

        public static Coupon ValidateCoupon(string code)
        {
            var text = Post("/api/coupons/validate", new { Code = code ?? "" });
            using (var doc = JsonDocument.Parse(text))
                return Coupon.FromRow(Row(doc.RootElement));
        }

        public static List<SavedPlace> SavedPlaces()
        {
            var list = new List<SavedPlace>();
            foreach (var r in Rows(Get("/api/places")))
                list.Add(SavedPlace.FromRow(r));
            return list;
        }

        public static void AddSavedPlace(string label, string address, double lat, double lng)
        {
            Post("/api/places", new { Label = label ?? "", Address = address ?? "", Lat = lat, Lng = lng });
        }

        /* ---------- رسائل / تقييم / عروض ---------- */

        public static List<Message> GetMessages(int orderId)
        {
            var list = new List<Message>();
            foreach (var r in Rows(Get("/api/orders/" + orderId + "/messages")))
                list.Add(Message.FromRow(r));
            return list;
        }

        public static void SendMessage(int orderId, string text)
        {
            Post("/api/orders/" + orderId + "/messages", new { Text = text ?? "" });
        }

        public static void MarkMessagesRead(int orderId)
        {
            Post("/api/orders/" + orderId + "/messages/read", new { });
        }

        public static void AddRating(int orderId, int rateeId, int rating, string comment, string photoPath)
        {
            Post("/api/ratings", new { OrderId = orderId, RateeId = rateeId, Rating = rating, Comment = comment ?? "", PhotoPath = photoPath ?? "" });
        }

        public static bool HasRated(int orderId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/ratings/has?orderId=" + orderId)))
                return doc.RootElement.TryGetProperty("has", out var v) && v.GetBoolean();
        }

        public static List<OrderOffer> GetOffers(int orderId)
        {
            var list = new List<OrderOffer>();
            foreach (var r in Rows(Get("/api/orders/" + orderId + "/offers")))
                list.Add(OrderOffer.FromRow(r));
            return list;
        }

        public static void PlaceOffer(int orderId, decimal price)
        {
            Post("/api/orders/" + orderId + "/offers", new { Price = price });
        }

        public static void AcceptOffer(int offerId)
        {
            Post("/api/offers/" + offerId + "/accept", new { });
        }

        /* ---------- السائق ---------- */

        public static List<Driver> AvailableDrivers(double lat, double lng, string gov, string city)
        {
            var list = new List<Driver>();
            foreach (var r in Rows(Get("/api/drivers/available" + Qs(new Dictionary<string, string> {
                { "lat", lat.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "lng", lng.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "gov", gov ?? "" }, { "city", city ?? "" } }))))
            {
                var d = Driver.FromRow(r);
                d.DistanceKm = Geo.DistanceKm(lat, lng, d.Lat, d.Lng);
                list.Add(d);
            }
            list.Sort((a, b) => a.DistanceKm.CompareTo(b.DistanceKm));
            return list;
        }

        public static void GetDriverStatus(out string status, out bool available)
        {
            status = ""; available = false;
            using (var doc = JsonDocument.Parse(Get("/api/drivers/status")))
            {
                if (doc.RootElement.TryGetProperty("status", out var s)) status = s.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("available", out var a)) available = a.GetBoolean();
            }
        }

        public static void SetAvailability(bool available, double lat, double lng)
        {
            Post("/api/drivers/availability", new { Available = available, Lat = lat, Lng = lng });
        }

        public static DriverStats DriverStats()
        {
            var s = new DriverStats();
            using (var doc = JsonDocument.Parse(Get("/api/drivers/stats")))
            {
                var r = doc.RootElement;
                s.TotalRides = r.TryGetProperty("totalRides", out var a) ? a.GetInt32() : 0;
                s.TotalEarnings = Dec(r, "totalEarnings");
                s.TodayRides = r.TryGetProperty("todayRides", out var b) ? b.GetInt32() : 0;
                s.TodayEarnings = Dec(r, "todayEarnings");
                s.RatingCount = r.TryGetProperty("ratingCount", out var c) ? c.GetInt32() : 0;
                s.AvgRating = Dec(r, "avgRating");
            }
            return s;
        }

        public static void Withdraw(decimal amount)
        {
            Post("/api/drivers/withdraw", new { Amount = amount });
        }

        public static string DriverNumber()
        {
            using (var doc = JsonDocument.Parse(Get("/api/drivers/number")))
                return doc.RootElement.TryGetProperty("number", out var v) ? (v.GetString() ?? "") : "";
        }

        public static string DriverNumberOf(int driverId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/drivers/" + driverId + "/number")))
                return doc.RootElement.TryGetProperty("number", out var v) ? (v.GetString() ?? "") : "";
        }

        /* ---------- اشتراك / شكاوى / مفضلة / حظر / ولاء ---------- */

        public static void SubscriptionInfo(out decimal price, out bool active)
        {
            SubscriptionInfoFull(out price, out _, out active, out _);
        }

        public static void SubscriptionInfoFull(out decimal price, out int discount, out bool active, out List<Subscription> history)
        {
            price = 0; discount = 0; active = false;
            history = new List<Subscription>();
            using (var doc = JsonDocument.Parse(Get("/api/subscription/info")))
            {
                var r = doc.RootElement;
                price = Dec(r, "price");
                if (r.TryGetProperty("discount", out var d)) discount = d.GetInt32();
                if (r.TryGetProperty("active", out var a)) active = a.GetBoolean();
                foreach (var el in SubRows(r, "history"))
                    history.Add(Subscription.FromRow(el));
            }
        }

        public static void Subscribe(string plan)
        {
            Post("/api/subscription/subscribe", new { Plan = plan ?? "" });
        }

        public static void AddComplaint(int orderId, string subject, string body)
        {
            Post("/api/complaints", new { OrderId = orderId, Subject = subject ?? "", Body = body ?? "" });
        }

        public static List<Complaint> MyComplaints()
        {
            var list = new List<Complaint>();
            foreach (var r in Rows(Get("/api/complaints/mine")))
                list.Add(Complaint.FromRow(r));
            return list;
        }

        public static bool ToggleFavorite(int driverId)
        {
            var text = Post("/api/favorites/toggle", new { DriverId = driverId });
            using (var doc = JsonDocument.Parse(text))
                return doc.RootElement.TryGetProperty("favorite", out var v) && v.GetBoolean();
        }

        public static List<Driver> FavoriteDrivers()
        {
            var list = new List<Driver>();
            foreach (var r in Rows(Get("/api/favorites")))
                list.Add(Driver.FromRow(r));
            return list;
        }

        public static void BlockDriver(int driverId)
        {
            Post("/api/blocks/block", new { DriverId = driverId });
        }

        public static void UnblockDriver(int driverId)
        {
            Post("/api/blocks/unblock", new { DriverId = driverId });
        }

        public static int LoyaltyPoints()
        {
            using (var doc = JsonDocument.Parse(Get("/api/loyalty/points")))
                return doc.RootElement.TryGetProperty("points", out var v) ? v.GetInt32() : 0;
        }

        public static List<Order> ScheduledDue()
        {
            var list = new List<Order>();
            foreach (var r in Rows(Get("/api/orders/scheduled-due")))
                list.Add(Order.FromRow(r));
            return list;
        }

        public static void MarkReminded(int orderId)
        {
            Post("/api/orders/" + orderId + "/reminded", new { });
        }

        public static void SetSplitPhone(int orderId, string phone)
        {
            Post("/api/orders/" + orderId + "/split-phone", new { Phone = phone });
        }

        /* ---------- إدارة ---------- */

        private static void NeedAdmin()
        {
            if (Session.Role != "مدير") throw new Exception("غير مصرح");
        }

        public static Dictionary<string, object> AdminStats()
        {
            return Row(JsonDocument.Parse(Get("/api/admin/stats")).RootElement);
        }

        public static int AvailableDriversCount()
        {
            using (var doc = JsonDocument.Parse(Get("/api/admin/available-drivers-count")))
                return doc.RootElement.TryGetProperty("count", out var v) ? v.GetInt32() : 0;
        }

        public static Dictionary<string, object> AdminRange(DateTime from, DateTime to)
        {
            var q = Qs(new Dictionary<string, string> {
                { "from", from.ToString("yyyy-MM-dd") }, { "to", to.ToString("yyyy-MM-dd") } });
            using (var doc = JsonDocument.Parse(Get("/api/admin/range" + q)))
                return Row(doc.RootElement);
        }

        private static List<Dictionary<string, object>> SubRows(JsonElement root, string name)
        {
            var list = new List<Dictionary<string, object>>();
            if (root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var el in arr.EnumerateArray())
                    list.Add(Row(el));
            return list;
        }

        public static List<Dictionary<string, object>> AdminRangeList(DateTime from, DateTime to, string name)
        {
            var q = Qs(new Dictionary<string, string> {
                { "from", from.ToString("yyyy-MM-dd") }, { "to", to.ToString("yyyy-MM-dd") } });
            using (var doc = JsonDocument.Parse(Get("/api/admin/range" + q)))
                return SubRows(doc.RootElement, name);
        }

        public static Dictionary<string, object> AdminWallets()
        {
            using (var doc = JsonDocument.Parse(Get("/api/admin/wallets")))
                return Row(doc.RootElement);
        }

        public static Dictionary<string, string> AdminSettings()
        {
            var d = new Dictionary<string, string>();
            using (var doc = JsonDocument.Parse(Get("/api/admin/settings")))
                foreach (var p in doc.RootElement.EnumerateObject())
                    d[p.Name] = p.Value.GetString() ?? "";
            return d;
        }

        public static void AdminSaveSettings(Dictionary<string, string> values)
        {
            Post("/api/admin/settings", values);
        }

        public static List<Dictionary<string, object>> AllAdmins()
        {
            return Rows(Get("/api/admin/admins"));
        }

        public static void AddAdmin(string user, string pass, string name, string phone)
        {
            Post("/api/admin/admins", new { UserName = user, Password = pass, FullName = name, Phone = phone });
        }

        public static void UpdateAdmin(int id, string name, string phone)
        {
            Put("/api/admin/admins/" + id, new { FullName = name, Phone = phone });
        }

        public static void ResetAdminPassword(int id, string pass)
        {
            Post("/api/admin/admins/" + id + "/reset-password", new { Password = pass });
        }

        public static void ToggleAdminActive(int id, bool active)
        {
            Post("/api/admin/admins/" + id + "/toggle", new { Active = active });
        }

        public static void DeleteAdmin(int id)
        {
            Delete("/api/admin/admins/" + id);
        }

        public static List<Driver> AllDrivers()
        {
            var list = new List<Driver>();
            foreach (var r in Rows(Get("/api/admin/drivers")))
                list.Add(Driver.FromRow(r));
            return list;
        }

        public static List<Driver> SearchDrivers(string query)
        {
            var list = new List<Driver>();
            foreach (var r in Rows(Get("/api/admin/drivers/search" + Qs(new Dictionary<string, string> { { "q", query ?? "" } }))))
                list.Add(Driver.FromRow(r));
            return list;
        }

        public static void SetDriverActive(int userId, bool active)
        {
            Post("/api/admin/drivers/" + userId + "/active", new { Active = active });
        }

        /// <summary>تعديل محافظة/مدينة مستخدم (سائق أو عميل) من لوحة الإدارة.</summary>
        public static void SetUserLocation(int userId, string governorate, string city)
        {
            Put("/api/admin/users/" + userId + "/location",
                new { Governorate = governorate, City = city });
        }

        public static void SetDriverStatus(int userId, string status)
        {
            Post("/api/admin/drivers/" + userId + "/status", new { Status = status });
        }

        public static void SetDriverAvailAdmin(int userId, bool available)
        {
            Post("/api/admin/drivers/" + userId + "/availability", new { Active = available });
        }

        public static List<Customer> AllCustomers()
        {
            var list = new List<Customer>();
            foreach (var r in Rows(Get("/api/admin/customers")))
                list.Add(Customer.FromRow(r));
            return list;
        }

        /// <summary>حذف عميل نهائيًا (يؤرشف في DeletedCustomers على الخادم).</summary>
        public static void DeleteCustomer(int userId)
        {
            Delete("/api/admin/customers/" + userId);
        }

        /// <summary>قائمة العملاء المحذوفين — مقيدة لحساب احمدعلي على الخادم.</summary>
        public static List<DeletedCustomer> AllDeletedCustomers()
        {
            var list = new List<DeletedCustomer>();
            foreach (var r in Rows(Get("/api/admin/deleted-customers")))
                list.Add(DeletedCustomer.FromRow(r));
            return list;
        }

        public static List<Coupon> ListCoupons()
        {
            var list = new List<Coupon>();
            foreach (var r in Rows(Get("/api/admin/coupons")))
                list.Add(Coupon.FromRow(r));
            return list;
        }

        public static void AddCoupon(string code, string type, decimal value, decimal maxOff, DateTime? exp)
        {
            Post("/api/admin/coupons", new { Code = code, Type = type, Value = value, MaxOff = maxOff, ExpiresAt = exp });
        }

        public static void EditCoupon(int id, string code, string type, decimal value, decimal maxOff, DateTime? exp)
        {
            Put("/api/admin/coupons/" + id, new { Code = code, Type = type, Value = value, MaxOff = maxOff, ExpiresAt = exp });
        }

        public static void DeleteCoupon(int id)
        {
            Delete("/api/admin/coupons/" + id);
        }

        /* ---------- المناطق (Areas) ---------- */

        /// <summary>المناطق النشطة لمدينة العميل (أو كل المناطق عند عدم تحديد مدينة).</summary>
        public static List<Area> Areas(string city)
        {
            var list = new List<Area>();
            var path = "/api/areas";
            if (!string.IsNullOrWhiteSpace(city))
                path += Qs(new Dictionary<string, string> { { "city", city } });
            foreach (var r in Rows(Get(path)))
                list.Add(Area.FromRow(r));
            return list;
        }

        /// <summary>أهم شوارع مدينة من جدول Streets المستقل.</summary>
        public static List<Street> Streets(string city)
        {
            var list = new List<Street>();
            var path = "/api/streets";
            if (!string.IsNullOrWhiteSpace(city))
                path += Qs(new Dictionary<string, string> { { "city", city } });
            foreach (var r in Rows(Get(path)))
                list.Add(Street.FromRow(r));
            return list;
        }

        /// <summary>المدن التي تحتوي مناطق نشطة.</summary>
        public static List<string> AreaCities()
        {
            var list = new List<string>();
            foreach (var r in Rows(Get("/api/areas/cities")))
            {
                var c = Tawsela.Data.Row.Str(r, "City");
                if (c != "") list.Add(c);
            }
            return list;
        }

        public static List<Area> AllAreas()
        {
            var list = new List<Area>();
            foreach (var r in Rows(Get("/api/admin/areas")))
                list.Add(Area.FromRow(r));
            return list;
        }

        public static void AddArea(string name, string city, string governorate,
            double lat, double lng, bool active)
        {
            Post("/api/admin/areas", new
            {
                Name = name, City = city, Governorate = governorate,
                Lat = lat, Lng = lng, Active = active
            });
        }

        public static void UpdateArea(int id, string name, string city, string governorate,
            double lat, double lng, bool active)
        {
            Put("/api/admin/areas/" + id, new
            {
                Name = name, City = city, Governorate = governorate,
                Lat = lat, Lng = lng, Active = active
            });
        }

        public static void DeleteArea(int id)
        {
            Delete("/api/admin/areas/" + id);
        }

        /* ---------- الشوارع (Streets) ---------- */

        public static List<Street> AllStreets()
        {
            var list = new List<Street>();
            foreach (var r in Rows(Get("/api/admin/streets")))
                list.Add(Street.FromRow(r));
            return list;
        }

        public static void AddStreet(string name, string city, string governorate,
            double lat, double lng, bool active)
        {
            Post("/api/admin/streets", new
            {
                Name = name, City = city, Governorate = governorate,
                Lat = lat, Lng = lng, Active = active
            });
        }

        public static void UpdateStreet(int id, string name, string city, string governorate,
            double lat, double lng, bool active)
        {
            Put("/api/admin/streets/" + id, new
            {
                Name = name, City = city, Governorate = governorate,
                Lat = lat, Lng = lng, Active = active
            });
        }

        public static void DeleteStreet(int id)
        {
            Delete("/api/admin/streets/" + id);
        }

        /* ---------- مشرفو التكاتك ---------- */

        public static List<Supervisor> ListSupervisors()
        {
            var list = new List<Supervisor>();
            foreach (var r in Rows(Get("/api/admin/supervisors")))
                list.Add(Supervisor.FromRow(r));
            return list;
        }

        public static string AddSupervisor(string fullName, string phone, decimal commissionPercent, string notes)
        {
            var text = Post("/api/admin/supervisors", new { FullName = fullName, Phone = phone, CommissionPercent = commissionPercent, Notes = notes });
            using (var doc = JsonDocument.Parse(text))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("code", out var c)) return c.GetString() ?? "";
            }
            return "";
        }

        /// <summary>التحقق من كود المشرف أثناء تسجيل السائق (يعيد الاسم أو فراغ).</summary>
        public static string LookupSupervisor(string code)
        {
            var text = Get("/api/supervisors/lookup?code=" + Uri.EscapeDataString(code ?? ""));
            using (var doc = JsonDocument.Parse(text))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("found", out var f) && f.GetBoolean() &&
                    root.TryGetProperty("name", out var n))
                    return n.GetString() ?? "";
            }
            return "";
        }

        public static void EditSupervisor(int id, string fullName, string phone, decimal commissionPercent, string notes)
        {
            Put("/api/admin/supervisors/" + id, new { FullName = fullName, Phone = phone, CommissionPercent = commissionPercent, Notes = notes });
        }

        public static void ToggleSupervisor(int id, bool active)
        {
            Post("/api/admin/supervisors/" + id + "/toggle", new { Active = active });
        }

        public static void DeleteSupervisor(int id)
        {
            Delete("/api/admin/supervisors/" + id);
        }

        /// <summary>أرباح مشرف خلال مدة (من/إلى) + الصرف المسجل.</summary>
        public static SupervisorEarnings GetSupervisorEarnings(int id, DateTime from, DateTime to)
        {
            var json = Get("/api/admin/supervisors/" + id + "/earnings" + Qs(new Dictionary<string, string>
            {
                { "from", from.ToString("yyyy-MM-dd") },
                { "to", to.ToString("yyyy-MM-dd") }
            }));
            var e = new SupervisorEarnings();
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                e.Rides = (int)Dec(root, "rides");
                e.FareTotal = Dec(root, "fareTotal");
                e.Commission = Dec(root, "commission");
                e.Paid = Dec(root, "paid");
                if (root.TryGetProperty("payouts", out var ps) && ps.ValueKind == JsonValueKind.Array)
                    foreach (var el in ps.EnumerateArray())
                        e.Payouts.Add(SupervisorPayout.FromRow(Row(el)));
            }
            return e;
        }

        /// <summary>تسجيل صرف لمشرف عن مدة من وإلى.</summary>
        public static void AddSupervisorPayout(int id, DateTime from, DateTime to, decimal amount, string note)
        {
            Post("/api/admin/supervisors/" + id + "/payouts", new { From = from, To = to, Amount = amount, Note = note });
        }

        /// <summary>ربط سائق بمشرف (0 = بدون).</summary>
        public static void SetDriverSupervisor(int driverUserId, int supervisorId)
        {
            Post("/api/admin/drivers/" + driverUserId + "/supervisor", new { SupervisorId = supervisorId });
        }

        private static int _nightFrom = -2;
        private static int _nightTo = -2;
        private static long _nightAt;

        /// <summary>فترة الليل بالساعة (كاش 30 دقيقة).</summary>
        public static void NightWindow(out int fromHour, out int toHour)
        {
            fromHour = 22; toHour = 6;
            try
            {
                long now = System.DateTime.UtcNow.Ticks;
                if (_nightFrom != -2 && now - _nightAt < System.TimeSpan.FromMinutes(30).Ticks)
                {
                    fromHour = _nightFrom; toHour = _nightTo;
                    return;
                }
                using (var doc = JsonDocument.Parse(Get("/api/settings/night")))
                {
                    if (doc.RootElement.TryGetProperty("from", out var f)) fromHour = f.GetInt32();
                    if (doc.RootElement.TryGetProperty("to", out var t)) toHour = t.GetInt32();
                }
                _nightFrom = fromHour; _nightTo = toHour; _nightAt = now;
            }
            catch { }
        }

        public static void AddServiceType(string name, decimal bf, decimal baseKm, decimal extraPk, decimal nightBf, decimal nightEpk, decimal nightM, decimal m, string mode, decimal commissionFixed, decimal commissionPercent)
        {
            Post("/api/admin/services", new { Name = name, BaseFare = bf, PerKm = extraPk, Multiplier = m, Active = true, BaseKm = baseKm, ExtraPerKm = extraPk, NightBaseFare = nightBf, NightExtraPerKm = nightEpk, NightMultiplier = nightM, PricingMode = mode, CommissionFixed = commissionFixed, CommissionPercent = commissionPercent });
        }

        public static void UpdateServiceType(int id, string name, decimal bf, decimal baseKm, decimal extraPk, decimal nightBf, decimal nightEpk, decimal nightM, decimal m, string mode, bool active, decimal commissionFixed, decimal commissionPercent)
        {
            Put("/api/admin/services/" + id, new { Name = name, BaseFare = bf, PerKm = extraPk, Multiplier = m, Active = active, BaseKm = baseKm, ExtraPerKm = extraPk, NightBaseFare = nightBf, NightExtraPerKm = nightEpk, NightMultiplier = nightM, PricingMode = mode, CommissionFixed = commissionFixed, CommissionPercent = commissionPercent });
        }

        public static List<Complaint> AllComplaints()
        {
            var list = new List<Complaint>();
            foreach (var r in Rows(Get("/api/admin/complaints")))
                list.Add(Complaint.FromRow(r));
            return list;
        }

        public static void ReplyComplaint(int id, string reply, string status)
        {
            Post("/api/admin/complaints/" + id + "/reply", new { Reply = reply, Status = status });
        }

        public static List<DriverDoc> AllDocuments()
        {
            var list = new List<DriverDoc>();
            foreach (var r in Rows(Get("/api/admin/documents")))
                list.Add(DriverDoc.FromRow(r));
            return list;
        }

        public static void SetDocStatus(int id, string status)
        {
            Post("/api/admin/documents/" + id + "/status", new { Status = status });
        }

        public static List<DriverDoc> DriverDocsByUser(int userId)
        {
            // تُجلب ضمن الكل وتُفلتر محلياً (حجمها صغير)
            var list = new List<DriverDoc>();
            foreach (var d in AllDocuments())
                if (d.UserId == userId) list.Add(d);
            return list;
        }

        public static void SetAllDriverDocsStatus(int userId, string status)
        {
            foreach (var d in DriverDocsByUser(userId))
                SetDocStatus(d.Id, status);
        }

        public static string MyDocStatus()
        {
            using (var doc = JsonDocument.Parse(Get("/api/drivers/docs-status")))
                return doc.RootElement.TryGetProperty("status", out var v) ? (v.GetString() ?? "") : "";
        }

        public static void AddDocument(string type, string info)
        {
            Post("/api/drivers/docs", new { Type = type, Info = info });
        }

        public static void AddDriverDocs(Dictionary<string, string> images, string opLicense)
        {
            Post("/api/drivers/docs/full", new { OperatingLicense = opLicense, Images = images });
        }

        public static Dictionary<string, object> AdminSubscriptions()
        {
            using (var doc = JsonDocument.Parse(Get("/api/admin/subscriptions")))
                return Row(doc.RootElement);
        }

        public static List<Subscription> AllSubscriptions()
        {
            var list = new List<Subscription>();
            using (var doc = JsonDocument.Parse(Get("/api/admin/subscriptions")))
                foreach (var el in SubRows(doc.RootElement, "list"))
                    list.Add(Subscription.FromRow(el));
            return list;
        }

        public static void AdminSubscribe(int userId, string plan, decimal price)
        {
            Post("/api/admin/subscriptions", new { UserId = userId, Plan = plan, Price = price });
        }

        public static void CancelSubscription(int userId)
        {
            Delete("/api/admin/subscriptions/" + userId);
        }

        public static List<Dictionary<string, object>> PendingTopUps()
        {
            return Rows(Get("/api/admin/topups"));
        }

        public static List<Dictionary<string, object>> AllTopUps()
        {
            return Rows(Get("/api/admin/topups/all"));
        }

        public static void ConfirmTopUp(int id)
        {
            Post("/api/admin/topups/" + id + "/confirm", new { });
        }

        public static void RejectTopUp(int id, string reason)
        {
            Post("/api/admin/topups/" + id + "/reject", new { Reply = reason });
        }

        public static List<Dictionary<string, object>> AllAds()
        {
            return Rows(Get("/api/admin/ads"));
        }

        public static List<Ad> ActiveAdsList(string gov, string city)
        {
            var list = new List<Ad>();
            foreach (var r in ActiveAds(gov, city))
                list.Add(Ad.FromRow(r));
            return list;
        }

        public static void AddAd(string title, string body, string img, string link, string scope, string gov, string cities)
        {
            Post("/api/admin/ads", new { Title = title, Body = body, ImageData = img, LinkUrl = link, TargetScope = scope, TargetGov = gov, TargetCities = cities });
        }

        public static void SetAdActive(int id, bool active)
        {
            Post("/api/admin/ads/" + id + "/active", new { Active = active });
        }

        public static void DeleteAd(int id)
        {
            Delete("/api/admin/ads/" + id);
        }

        public static List<Dictionary<string, object>> AllEmergencyTypes()
        {
            return Rows(Get("/api/admin/emergency-types"));
        }

        public static void AddEmergencyType(string title)
        {
            Post("/api/admin/emergency-types", new { Title = title });
        }

        public static void DeleteEmergencyType(int id)
        {
            Delete("/api/admin/emergency-types/" + id);
        }

        public static List<Dictionary<string, object>> AllEmergencies()
        {
            return Rows(Get("/api/admin/emergencies"));
        }

        public static List<Order> TripReport(DateTime from, DateTime to, bool byCompleted, out Dictionary<int, decimal> fees)
        {
            fees = new Dictionary<int, decimal>();
            var list = new List<Order>();
            var q = Qs(new Dictionary<string, string> {
                { "from", from.ToString("yyyy-MM-dd") }, { "to", to.ToString("yyyy-MM-dd") },
                { "byCompleted", byCompleted ? "true" : "false" } });
            using (var doc = JsonDocument.Parse(Get("/api/admin/trip-report" + q)))
            {
                foreach (var el in SubRows(doc.RootElement, "trips"))
                    list.Add(Order.FromRow(el));
                if (doc.RootElement.TryGetProperty("fees", out var f) && f.ValueKind == JsonValueKind.Object)
                    foreach (var p in f.EnumerateObject())
                        if (int.TryParse(p.Name, out var oid))
                            fees[oid] = ToClr(p.Value) is decimal dd ? dd : 0m;
            }
            return list;
        }

        public static List<Dictionary<string, object>> Heatmap()
        {
            return Rows(Get("/api/admin/heatmap"));
        }

        public static List<Driver> DriverLocations()
        {
            var list = new List<Driver>();
            foreach (var r in Rows(Get("/api/drivers/locations")))
                list.Add(Driver.FromRow(r));
            return list;
        }

        public static void SetDeviceToken(string token)
        {
            Post("/api/device-token", new { Token = token });
        }

        public static void GetReferral(out string code, out int count, out decimal reward)
        {
            code = ""; count = 0; reward = 0;
            using (var doc = JsonDocument.Parse(Get("/api/referral")))
            {
                var r = doc.RootElement;
                if (r.TryGetProperty("code", out var c)) code = c.GetString() ?? "";
                if (r.TryGetProperty("count", out var n)) count = n.GetInt32();
                reward = Dec(r, "reward");
            }
        }

        public static void GetReferral(out string code, out int count)
        {
            GetReferral(out code, out count, out _);
        }

        public static void RedeemPoints(int points)
        {
            Post("/api/loyalty/redeem", new { Points = points });
        }

        public static void GiftsInfo(out int points, out List<Dictionary<string, object>> gifts)
        {
            points = 0;
            gifts = new List<Dictionary<string, object>>();
            using (var doc = JsonDocument.Parse(Get("/api/gifts")))
            {
                if (doc.RootElement.TryGetProperty("points", out var p)) points = p.GetInt32();
                gifts = SubRows(doc.RootElement, "gifts");
            }
        }

        public static void RedeemGift(int giftId)
        {
            Post("/api/gifts/redeem", new { GiftId = giftId });
        }

        public static Dictionary<string, object> CustomerFullStats()
        {
            using (var doc = JsonDocument.Parse(Get("/api/customers/me/full-stats")))
                return Row(doc.RootElement);
        }

        public static List<Dictionary<string, object>> MyRecurring()
        {
            return Rows(Get("/api/recurring"));
        }

        public static void AddRecurring(double fromLat, double fromLng, string fromAddr, double toLat, double toLng, string toAddr, string note, int serviceTypeId, string pattern, string timeOfDay)
        {
            Post("/api/recurring", new { FromLat = fromLat, FromLng = fromLng, FromAddress = fromAddr, ToLat = toLat, ToLng = toLng, ToAddress = toAddr, Note = note, ServiceTypeId = serviceTypeId, Pattern = pattern, TimeOfDay = timeOfDay });
        }

        public static void ToggleRecurring(int id, bool active)
        {
            Post("/api/recurring/" + id + "/toggle", new { Active = active });
        }

        public static void DeleteRecurring(int id)
        {
            Delete("/api/recurring/" + id);
        }

        public static void StartShift()
        {
            Post("/api/shifts/start", new { });
        }

        public static void EndShift()
        {
            Post("/api/shifts/end", new { });
        }

        public static List<Dictionary<string, object>> DriverShifts()
        {
            return Rows(Get("/api/shifts"));
        }

        public static string EarningsReport(int days)
        {
            using (var doc = JsonDocument.Parse(Get("/api/drivers/earnings-report?days=" + days)))
                return doc.RootElement.TryGetProperty("report", out var v) ? (v.GetString() ?? "") : "";
        }

        public static void SplitFare(int orderId, string phone1, string phone2, decimal a1, decimal a2)
        {
            Post("/api/split-fare", new { OrderId = orderId, Phone1 = phone1, Phone2 = phone2, Amount1 = a1, Amount2 = a2 });
        }

        public static List<Driver> BlockedDrivers()
        {
            var list = new List<Driver>();
            foreach (var r in Rows(Get("/api/blocks")))
                list.Add(Driver.FromRow(r));
            return list;
        }

        public static bool IsFavorite(int driverId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/favorites/is?driverId=" + driverId)))
                return doc.RootElement.TryGetProperty("favorite", out var v) && v.GetBoolean();
        }

        public static bool IsBlocked(int driverId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/blocks/is?driverId=" + driverId)))
                return doc.RootElement.TryGetProperty("blocked", out var v) && v.GetBoolean();
        }

        public static void UpdateProfile(string fullName, string phone)
        {
            Put("/api/profile", new { FullName = fullName, Phone = phone });
        }

        public static void ChangePassword(string oldPwd, string newPwd)
        {
            Post("/api/profile/password", new { OldPassword = oldPwd, NewPassword = newPwd });
        }

        public static void DeleteAccount(bool forfeitBalance = false)
        {
            Post("/api/account/delete", new { ForfeitBalance = forfeitBalance });
        }

        public static void UpdateDriverLocation(double lat, double lng)
        {
            Post("/api/drivers/location", new { Lat = lat, Lng = lng });
        }

        public static double[] GetDriverLocation(int driverId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/drivers/" + driverId + "/location")))
            {
                if (doc.RootElement.TryGetProperty("has", out var h) && !h.GetBoolean()) return null;
                double la = 0, lo = 0;
                if (doc.RootElement.TryGetProperty("lat", out var a)) a.TryGetDouble(out la);
                if (doc.RootElement.TryGetProperty("lng", out var o)) o.TryGetDouble(out lo);
                return new double[] { la, lo };
            }
        }

        public static int OrderEta(int orderId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/orders/" + orderId + "/eta")))
                return doc.RootElement.TryGetProperty("minutes", out var v) ? v.GetInt32() : 0;
        }

        public static void CheckLongRide(int orderId)
        {
            Post("/api/orders/" + orderId + "/long-ride-check", new { });
        }

        public static List<Dictionary<string, object>> MyAchievements()
        {
            return Rows(Get("/api/achievements"));
        }

        public static void ChallengesInfo(out List<Dictionary<string, object>> challenges, out int completedRides)
        {
            challenges = new List<Dictionary<string, object>>();
            completedRides = 0;
            using (var doc = JsonDocument.Parse(Get("/api/challenges")))
            {
                challenges = SubRows(doc.RootElement, "challenges");
                if (doc.RootElement.TryGetProperty("completedRides", out var v)) completedRides = v.GetInt32();
            }
        }

        public static List<Coupon> CouponsList()
        {
            var list = new List<Coupon>();
            foreach (var r in Rows(Get("/api/coupons")))
                list.Add(Coupon.FromRow(r));
            return list;
        }

        public static int ProcessDueRecurring()
        {
            using (var doc = JsonDocument.Parse(Post("/api/recurring/process-due", new { })))
                return doc.RootElement.TryGetProperty("created", out var v) ? v.GetInt32() : 0;
        }

        public static bool IsPromoted(int driverId)
        {
            using (var doc = JsonDocument.Parse(Get("/api/drivers/" + driverId + "/promoted")))
                return doc.RootElement.TryGetProperty("promoted", out var v) && v.GetBoolean();
        }

        public static void DriverBonus(decimal amount, string note)
        {
            Post("/api/drivers/bonus", new { Amount = amount, Note = note });
        }

        public static List<Subscription> UserSubscriptions()
        {
            var list = new List<Subscription>();
            foreach (var r in Rows(Get("/api/drivers/subscriptions")))
                list.Add(Subscription.FromRow(r));
            return list;
        }

        /// <summary>تسجيل حساب عبر الـ API — يملأ Session ويحفظ التوكن.</summary>
        public static void Register(string userName, string password, string fullName,
            string phone, string role, string carModel, string carPlate, string carColor,
            string referralCode, string governorate, string city, string supervisorCode = "")
        {
            var text = Post("/api/auth/register", new
            {
                UserName = userName ?? "", Password = password ?? "", FullName = fullName ?? "",
                Phone = phone ?? "", Role = role ?? "", CarModel = carModel ?? "",
                CarPlate = carPlate ?? "", CarColor = carColor ?? "",
                ReferralCode = referralCode ?? "", Governorate = governorate ?? "", City = city ?? "",
                SupervisorCode = supervisorCode ?? ""
            });
            using (var doc = JsonDocument.Parse(text))
            {
                var root = doc.RootElement;
                SaveToken(root.GetProperty("token").GetString() ?? "");
                ApplyUser(root.GetProperty("user"));
                if (Session.Governorate == "") Session.Governorate = governorate ?? "";
                if (Session.City == "") Session.City = city ?? "";
            }
        }
    }
}
