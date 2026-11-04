using System;
using Tawsela.Core.Platform;
using Tawsela.Data;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// تهيئة طبقة المنصة مرة واحدة عند الإقلاع.
    /// يجب استدعاؤها قبل أي استخدام لـ Api أو AppLock أو SecureStore.
    /// </summary>
    public static class IosPlatform
    {
        private static bool _initialized;
        private static readonly object Sync = new object();

        public static UserDefaultsPrefs Prefs { get; private set; }

        public static KeychainStore Secure { get; private set; }

        public static IosHost Host { get; private set; }

        public static void Init()
        {
            if (_initialized) return;
            lock (Sync)
            {
                if (_initialized) return;

                Prefs = new UserDefaultsPrefs();
                Secure = new KeychainStore();
                Host = new IosHost();

                AppServices.Init(Prefs, Secure, Host);

                // مفتاح SecureStore المشترك = مفتاح Keychain نفسه،
                // حتى تعمل نفس بنية "iv:cipher" المشفّرة مع التوكن على المنصتين.
                try { SecureStore.Configure(KeychainStore.GetOrCreateKey()); }
                catch { }

                _initialized = true;
            }
        }
    }
}
