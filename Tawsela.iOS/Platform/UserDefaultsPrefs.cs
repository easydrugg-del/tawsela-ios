using System;
using Foundation;
using Tawsela.Core.Platform;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل SharedPreferences على iOS: NSUserDefaults.
    /// المفاتيح تُخزَّن كما هي؛ الأسماء تأتي من طبقة Core مسبوقة بمجموعتها.
    /// </summary>
    public sealed class UserDefaultsPrefs : IAppPrefs
    {
        private readonly NSUserDefaults _defaults;

        public UserDefaultsPrefs()
        {
            _defaults = NSUserDefaults.StandardUserDefaults;
        }

        public string Get(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            var v = _defaults.StringForKey(key);
            return v ?? fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            // NSUserDefaults لا يميز بين "غير موجود" و"false" — نتحقق من الوجود أولًا
            if (_defaults[key] == null) return fallback;
            return _defaults.BoolForKey(key);
        }

        public long GetLong(string key, long fallback = 0)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            if (_defaults[key] == null) return fallback;
            return (long)_defaults.IntForKey(key);
        }

        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _defaults.SetString(value ?? "", key);
        }

        public void SetBool(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _defaults.SetBool(value, key);
        }

        public void SetLong(string key, long value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _defaults.SetInt((nint)value, key);
        }

        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _defaults.RemoveObject(key);
        }

        public void Clear()
        {
            try
            {
                var domain = NSBundle.MainBundle.BundleIdentifier;
                if (!string.IsNullOrEmpty(domain)) _defaults.RemovePersistentDomain(domain);
                _defaults.Synchronize();
            }
            catch { }
        }

        public void Synchronize()
        {
            try { _defaults.Synchronize(); } catch { }
        }
    }
}
