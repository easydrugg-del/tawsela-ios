using System;
using System.Security.Cryptography;
using System.Text;
using Tawsela.Core.Platform;

namespace Tawsela.Data
{
    /// <summary>
    /// قفل التطبيق برقم سري (PIN 4-6 أرقام).
    /// يُخزن هاش PBKDF2-SHA256 مع Salt عشوائي (120 ألف دورة) وليس الرقم نفسه.
    ///
    /// نسخة مشتركة: التخزين عبر AppServices.Prefs
    /// (SharedPreferences على أندرويد / NSUserDefaults على iOS).
    /// منطق التشفير نفسه (PBKDF2) محمول بالكامل بدون تغيير.
    /// </summary>
    public static class AppLock
    {
        private const string Pref = "app_lock";
        private const string KeyEnabled = "enabled";
        private const string KeyPinHash = "pin_hash";
        private const string KeyBgTime = "bg_time";
        private const int Iterations = 120000;
        private const string Prefix = "v2";
        // مهلة الخلفية قبل طلب الرقم مجدداً (دقيقتان)
        public const int TimeoutMinutes = 2;
        public const int MaxAttempts = 5;

        /// <summary>مفاتيح التخزين مسبوقة باسم المجموعة (كما كان في SharedPreferences).</summary>
        private static string Key(string name)
        {
            return Pref + "." + name;
        }

        /// <summary>تجزئة الإصدار القديم (SHA256 + Pepper) — للترحيل فقط.</summary>
        private static string HashLegacy(string pin)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("Tw$ela#Pin@" + (pin ?? "")));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>تجزئة PBKDF2 بصيغة: v2$iterations$salt$hash (Base64).</summary>
        private static string HashPin(string pin)
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            using (var kdf = new Rfc2898DeriveBytes(pin ?? "", salt, Iterations, HashAlgorithmName.SHA256))
            {
                var hash = kdf.GetBytes(32);
                return Prefix + "$" + Iterations + "$" +
                       Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        private static bool VerifyPin(string pin, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            try
            {
                if (stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
                {
                    var parts = stored.Split('$');
                    if (parts.Length != 4) return false;
                    int it;
                    if (!int.TryParse(parts[1], out it) || it < 1000) return false;
                    var salt = Convert.FromBase64String(parts[2]);
                    var expected = Convert.FromBase64String(parts[3]);
                    using (var kdf = new Rfc2898DeriveBytes(pin ?? "", salt, it, HashAlgorithmName.SHA256))
                        return CryptographicOperations.FixedTimeEquals(kdf.GetBytes(expected.Length), expected);
                }

                // توافق مع الهاش القديم
                var legacy = Encoding.UTF8.GetBytes(HashLegacy(pin));
                var provided = Encoding.UTF8.GetBytes(stored);
                return legacy.Length == provided.Length &&
                       CryptographicOperations.FixedTimeEquals(legacy, provided);
            }
            catch { return false; }
        }

        public static bool IsEnabled()
        {
            return AppServices.Prefs.GetBool(Key(KeyEnabled), false);
        }

        public static bool HasPin()
        {
            return AppServices.Prefs.Get(Key(KeyPinHash), "") != "";
        }

        public static bool IsActive()
        {
            return IsEnabled() && HasPin();
        }

        public static void SetPin(string pin)
        {
            if (pin == null || pin.Length < 4 || pin.Length > 6)
                throw new Exception("الرقم السري من 4 إلى 6 أرقام");
            foreach (var ch in pin)
                if (ch < '0' || ch > '9') throw new Exception("الرقم السري أرقام فقط");
            AppServices.Prefs.Set(Key(KeyPinHash), HashPin(pin));
            AppServices.Prefs.SetBool(Key(KeyEnabled), true);
        }

        public static void SetEnabled(bool enabled)
        {
            AppServices.Prefs.SetBool(Key(KeyEnabled), enabled);
        }

        public static bool Verify(string pin)
        {
            var stored = AppServices.Prefs.Get(Key(KeyPinHash), "");
            if (!VerifyPin(pin, stored)) return false;

            // ترقية تلقائية للهاش القديم بعد أول تحقق ناجح
            if (!stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
                AppServices.Prefs.Set(Key(KeyPinHash), HashPin(pin));
            return true;
        }

        public static void MarkBackground()
        {
            AppServices.Prefs.SetLong(Key(KeyBgTime), DateTime.UtcNow.Ticks);
        }

        public static void ClearBackground()
        {
            AppServices.Prefs.SetLong(Key(KeyBgTime), 0);
        }

        /// <summary>هل تجاوزت مدة الخلفية المهلة؟</summary>
        public static bool IsTimedOut()
        {
            long t = AppServices.Prefs.GetLong(Key(KeyBgTime), 0);
            if (t <= 0) return false;
            return DateTime.UtcNow.Ticks - t > TimeSpan.FromMinutes(TimeoutMinutes).Ticks;
        }
    }
}
