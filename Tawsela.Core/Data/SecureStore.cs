using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tawsela.Data
{
    /// <summary>
    /// تشفير القيم الحساسة (توكن الجلسة) بمفتاح AES-256-GCM.
    ///
    /// نسخة iOS: المفتاح نفسه محفوظ في iOS Keychain (عبر IAppSecure في المنصة)،
    /// والتشفير هنا بمساعدات BCL فقط حتى يعمل على أي منصة بدون Java/Android APIs.
    ///
    /// صيغة الناتج: base64(iv[12]) + ":" + base64(ciphertext + tag[16])
    /// وهي متوافقة مع SecureStore القديم على أندرويد (نفس الفاصل ':' ونفس حجم الـ IV).
    /// </summary>
    public static class SecureStore
    {
        private const int IvSize = 12;        // حجم الـ IV القياسي لـ GCM
        private const int TagSize = 16;       // حجم الـ authentication tag
        private const string Separator = ":";

        private static readonly object Sync = new object();

        /// <summary>يُهيّأ من طبقة المنصة عند الإقلاع. إن لم يُهيّأ يُستخدم مسار محلي.</summary>
        private static byte[] _key;

        /// <summary>
        /// مفتاح التشفير (32 بايت). تطبيق iOS يمرر مفتاحًا محفوظًا في Keychain،
        /// وإن لم يمرر شيئًا يُنشأ مفتاح عشوائي ويُحفظ في IAppPrefs المشفّرة.
        /// </summary>
        public static void Configure(byte[] key)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("مفتاح SecureStore يجب أن يكون 32 بايت", nameof(key));
            lock (Sync) { _key = key; }
        }

        private static byte[] Key()
        {
            if (_key != null) return _key;
            lock (Sync)
            {
                if (_key != null) return _key;

                // مفتاح محلي مشتق من بيانات المنصة — لا يغادر الجهاز
                var stored = Tawsela.Core.Platform.AppServices.Prefs.Get("secure_store_key", "");
                if (!string.IsNullOrEmpty(stored))
                {
                    try
                    {
                        var raw = Convert.FromBase64String(stored);
                        if (raw.Length == 32) { _key = raw; return _key; }
                    }
                    catch { }
                }

                var fresh = new byte[32];
                RandomNumberGenerator.Fill(fresh);
                Tawsela.Core.Platform.AppServices.Prefs.Set("secure_store_key", Convert.ToBase64String(fresh));
                _key = fresh;
                return _key;
            }
        }

        /// <summary>تشفير نص. تُرجع null عند تعذر التشفير (ليستخدم المتصل بديلاً).</summary>
        public static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try
            {
                var key = Key();
                var nonce = new byte[IvSize];
                RandomNumberGenerator.Fill(nonce);

                var plainBytes = Encoding.UTF8.GetBytes(plain);
                var cipher = new byte[plainBytes.Length];
                var tag = new byte[TagSize];

                using (var aes = new AesGcm(key, TagSize))
                    aes.Encrypt(nonce, plainBytes, cipher, tag);

                // ciphertext متبوعًا بالـ tag في نفس المصفوفة (كما في التخزين القديم)
                var combined = new byte[cipher.Length + tag.Length];
                Buffer.BlockCopy(cipher, 0, combined, 0, cipher.Length);
                Buffer.BlockCopy(tag, 0, combined, cipher.Length, tag.Length);

                return Convert.ToBase64String(nonce) + Separator + Convert.ToBase64String(combined);
            }
            catch { return null; }
        }

        /// <summary>فك تشفير نص. تُرجع null إذا كانت القيمة غير مشفرة أو تالفة.</summary>
        public static string Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            try
            {
                var idx = value.IndexOf(Separator, StringComparison.Ordinal);
                if (idx <= 0) return null;

                var nonce = Convert.FromBase64String(value.Substring(0, idx));
                var combined = Convert.FromBase64String(value.Substring(idx + 1));
                if (nonce.Length != IvSize || combined.Length <= TagSize) return null;

                var cipherBytes = new byte[combined.Length - TagSize];
                var tag = new byte[TagSize];
                Buffer.BlockCopy(combined, 0, cipherBytes, 0, cipherBytes.Length);
                Buffer.BlockCopy(combined, cipherBytes.Length, tag, 0, TagSize);

                var plainBytes = new byte[cipherBytes.Length];
                using (var aes = new AesGcm(Key(), TagSize))
                    aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch { return null; }
        }

        /// <summary>نسخة احتياطية: تشفير/فك تشفير عبر المنصة مباشرة (iOS Keychain / Android Keystore).</summary>
        public static string PlatformEncrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try { return Tawsela.Core.Platform.AppServices.Secure.Encrypt(plain); }
            catch { return null; }
        }

        /// <summary>نسخة احتياطية: فك تشفير عبر المنصة مباشرة.</summary>
        public static string PlatformDecrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            try { return Tawsela.Core.Platform.AppServices.Secure.Decrypt(value); }
            catch { return null; }
        }
    }
}
