using System;
using System.Security.Cryptography;
using System.Text;
using Foundation;
using Security;
using Tawsela.Core.Platform;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل Android Keystore على iOS: مفتاح AES-256 محفوظ في Keychain
    /// (ThisDeviceOnly — لا يُنقل مع النسخ الاحتياطي ولا يخرج من الجهاز).
    /// التشفير نفسه بـ AES-GCM عبر BCL، وهو نفس أسلوب SecureStore في أندرويد.
    /// </summary>
    public sealed class KeychainStore : IAppSecure
    {
        private const string Service = "com.tawsela.app.secure";
        private const string Account = "secure_store_key";
        private const int KeySize = 32;
        private const int IvSize = 12;
        private const int TagSize = 16;

        private static byte[] _key;
        private static readonly object Sync = new object();

        /// <summary>مفتاح التشفير (يُنشأ مرة واحدة ويُحفظ في Keychain).</summary>
        public static byte[] GetOrCreateKey()
        {
            if (_key != null) return _key;
            lock (Sync)
            {
                if (_key != null) return _key;

                try
                {
                    var existing = ReadKey();
                    if (existing != null && existing.Length == KeySize)
                    {
                        _key = existing;
                        return _key;
                    }

                    var fresh = new byte[KeySize];
                    RandomNumberGenerator.Fill(fresh);
                    if (WriteKey(fresh))
                    {
                        _key = fresh;
                        return _key;
                    }
                }
                catch { }

                // احتياط أخير: مفتاح للجلسة الحالية فقط (يفقد التوكن عند إعادة التشغيل)
                var fallback = new byte[KeySize];
                RandomNumberGenerator.Fill(fallback);
                _key = fallback;
                return _key;
            }
        }

        private static byte[] ReadKey()
        {
            var query = new SecRecord(SecKind.GenericPassword)
            {
                Service = Service,
                Account = Account
            };
            SecStatusCode status;
            var match = SecKeyChain.QueryAsRecord(query, out status);
            if (status != SecStatusCode.Success || match == null || match.ValueData == null) return null;
            return match.ValueData.ToArray();
        }

        private static bool WriteKey(byte[] key)
        {
            // احذف القديم أولًا حتى لا يفشل الإضافة بـ DuplicateItem
            try
            {
                var del = new SecRecord(SecKind.GenericPassword) { Service = Service, Account = Account };
                SecKeyChain.Remove(del);
            }
            catch { }

            var record = new SecRecord(SecKind.GenericPassword)
            {
                Service = Service,
                Account = Account,
                ValueData = NSData.FromArray(key),
                // متاح بعد أول فتح للجهاز، ولا يُنقل لجهاز آخر أو نسخة احتياطية
                Accessible = SecAccessible.AfterFirstUnlockThisDeviceOnly
            };
            var status = SecKeyChain.Add(record);
            if (status == SecStatusCode.Success) return true;

            // بعض الحالات ترجع DuplicateItem رغم الحذف — نعتبرها نجاحًا لو يمكن القراءة
            var check = ReadKey();
            return check != null && check.Length == key.Length;
        }

        public string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try
            {
                var key = GetOrCreateKey();
                var nonce = new byte[IvSize];
                RandomNumberGenerator.Fill(nonce);

                var plainBytes = Encoding.UTF8.GetBytes(plain);
                var cipher = new byte[plainBytes.Length];
                var tag = new byte[TagSize];

                using (var aes = new AesGcm(key, TagSize))
                    aes.Encrypt(nonce, plainBytes, cipher, tag);

                var combined = new byte[cipher.Length + tag.Length];
                Buffer.BlockCopy(cipher, 0, combined, 0, cipher.Length);
                Buffer.BlockCopy(tag, 0, combined, cipher.Length, tag.Length);

                return Convert.ToBase64String(nonce) + ":" + Convert.ToBase64String(combined);
            }
            catch { return null; }
        }

        public string Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            try
            {
                var idx = value.IndexOf(':', StringComparison.Ordinal);
                if (idx <= 0) return null;

                var nonce = Convert.FromBase64String(value.Substring(0, idx));
                var combined = Convert.FromBase64String(value.Substring(idx + 1));
                if (nonce.Length != IvSize || combined.Length <= TagSize) return null;

                var cipher = new byte[combined.Length - TagSize];
                var tag = new byte[TagSize];
                Buffer.BlockCopy(combined, 0, cipher, 0, cipher.Length);
                Buffer.BlockCopy(combined, cipher.Length, tag, 0, TagSize);

                var plain = new byte[cipher.Length];
                using (var aes = new AesGcm(GetOrCreateKey(), TagSize))
                    aes.Decrypt(nonce, cipher, tag, plain);

                return Encoding.UTF8.GetString(plain);
            }
            catch { return null; }
        }

        /// <summary>مسح مفتاح التشفير (تسجيل خروج كامل / إعادة تهيئة).</summary>
        public static void ResetKey()
        {
            lock (Sync)
            {
                _key = null;
                try
                {
                    var del = new SecRecord(SecKind.GenericPassword) { Service = Service, Account = Account };
                    SecKeyChain.Remove(del);
                }
                catch { }
            }
        }
    }
}
