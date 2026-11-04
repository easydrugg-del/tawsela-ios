using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Tawsela.Core.Platform
{
    /// <summary>
    /// تخزين الإعدادات العادية (بديل SharedPreferences على أندرويد و NSUserDefaults على iOS).
    /// لا تُخزَّن هنا أي بيانات حساسة — التوكن له IAppSecure.
    /// </summary>
    public interface IAppPrefs
    {
        string Get(string key, string fallback = "");
        bool GetBool(string key, bool fallback = false);
        long GetLong(string key, long fallback = 0);
        void Set(string key, string value);
        void SetBool(string key, bool value);
        void SetLong(string key, long value);
        void Remove(string key);
        void Clear();
    }

    /// <summary>
    /// تشفير القيم الحساسة (التوكن) — Keychain على iOS و Android Keystore على أندرويد.
    /// التطبيق نفسه في SecureStore يستخدم AES-GCM بمساعدات BCL فقط.
    /// </summary>
    public interface IAppSecure
    {
        /// <summary>تشفير نص. تُرجع null عند تعذر التشفير (ليستخدم المتصل بديلًا).</summary>
        string Encrypt(string plain);

        /// <summary>فك تشفير نص. تُرجع null إذا كانت القيمة غير مشفرة أو تالفة.</summary>
        string Decrypt(string value);
    }

    /// <summary>
    /// أدوات المنصة التي لا يمكن كتابتها مرة واحدة (السجل، حالة الشبكة، فتح رابط خارجي).
    /// </summary>
    public interface IAppHost
    {
        /// <summary>حالة الشبكة الحالية (بديل ConnectivityManager على أندرويد و NWPathMonitor على iOS).</summary>
        bool IsOnline();

        /// <summary>سجل تشخيصي موحّد (بديل Android.Util.Log).</summary>
        void Log(string tag, string message);

        /// <summary>فتح رابط خارجي في المتصفح. تُرجع false إذا تعذّر.</summary>
        bool OpenUrl(string url);

        /// <summary>معرّف المنصة: "android" أو "ios" — يُستخدم عند تسجيل توكن الإشعارات على الخادم.</summary>
        string PlatformName { get; }
    }

    /// <summary>
    /// نقطة الوصول الثابتة لخدمات المنصة. يملؤها كل تطبيق عند الإقلاع:
    /// <code>
    /// AppServices.Init(prefs, secure, host);
    /// </code>
    /// </summary>
    public static class AppServices
    {
        private static IAppPrefs _prefs;
        private static IAppSecure _secure;
        private static IAppHost _host;

        public static void Init(IAppPrefs prefs, IAppSecure secure, IAppHost host)
        {
            if (prefs == null) throw new ArgumentNullException(nameof(prefs));
            if (secure == null) throw new ArgumentNullException(nameof(secure));
            if (host == null) throw new ArgumentNullException(nameof(host));
            _prefs = prefs;
            _secure = secure;
            _host = host;
        }

        public static bool IsInitialized
        {
            get { return _prefs != null && _secure != null && _host != null; }
        }

        public static IAppPrefs Prefs
        {
            get
            {
                if (_prefs == null) throw new InvalidOperationException("AppServices.Init لم تُستدعَ بعد.");
                return _prefs;
            }
        }

        public static IAppSecure Secure
        {
            get
            {
                if (_secure == null) throw new InvalidOperationException("AppServices.Init لم تُستدعَ بعد.");
                return _secure;
            }
        }

        public static IAppHost Host
        {
            get
            {
                if (_host == null) throw new InvalidOperationException("AppServices.Init لم تُستدعَ بعد.");
                return _host;
            }
        }

        public static bool IsOnline()
        {
            try { return Host.IsOnline(); } catch { return false; }
        }

        public static void Log(string tag, string message)
        {
            try { Host.Log(tag, message); }
            catch
            {
                // السجل لا يجب أن يُسقط التطبيق أبدًا
                Debug.WriteLine("[" + tag + "] " + message);
            }
        }

        public static bool OpenUrl(string url)
        {
            try { return Host.OpenUrl(url); } catch { return false; }
        }
    }

    /// <summary>
    /// تنفيذ محمول لـ IAppHost يعتمد على BCL فقط — يصلح لكل المنصات.
    /// تطبيق iOS يستخدمه مباشرة (مع تفاصيل OpenUrl) وتطبيق أندرويد يستطيع أن يرث منه.
    /// </summary>
    public class PortableHost : IAppHost
    {
        private readonly string _platformName;

        public PortableHost(string platformName)
        {
            _platformName = platformName ?? "unknown";
        }

        public string PlatformName
        {
            get { return _platformName; }
        }

        public virtual bool IsOnline()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable()) return false;
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public virtual void Log(string tag, string message)
        {
            Debug.WriteLine(string.Format("[{0}] {1}", tag, message));
            Console.WriteLine(string.Format("[{0}] {1}", tag, message));
        }

        public virtual bool OpenUrl(string url)
        {
            return false;
        }
    }

    /// <summary>تنفيذ ذاكري بسيط لـ IAppPrefs (للاختبارات وللعمل عندما لا تتوفر المنصة).</summary>
    public class MemoryPrefs : IAppPrefs
    {
        private readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public string Get(string key, string fallback = "")
        {
            string v;
            return _values.TryGetValue(key ?? "", out v) ? v : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            string v;
            if (!_values.TryGetValue(key ?? "", out v) || v == null) return fallback;
            return v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        }

        public long GetLong(string key, long fallback = 0)
        {
            string v;
            if (!_values.TryGetValue(key ?? "", out v)) return fallback;
            long l;
            return long.TryParse(v, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out l) ? l : fallback;
        }

        public void Set(string key, string value)
        {
            _values[key ?? ""] = value ?? "";
        }

        public void SetBool(string key, bool value)
        {
            _values[key ?? ""] = value ? "1" : "0";
        }

        public void SetLong(string key, long value)
        {
            _values[key ?? ""] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public void Remove(string key)
        {
            _values.Remove(key ?? "");
        }

        public void Clear()
        {
            _values.Clear();
        }
    }

    /// <summary>تنفيذ ذاكري بلا تشفير — لا يُستخدم في الإنتاج، فقط للاختبار.</summary>
    public class MemorySecure : IAppSecure
    {
        public string Encrypt(string plain) { return plain; }
        public string Decrypt(string value) { return value; }
    }
}
