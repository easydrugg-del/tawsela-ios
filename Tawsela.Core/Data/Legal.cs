using System;
using Tawsela.Core.Platform;

namespace Tawsela.Data
{
    /// <summary>
    /// روابط الصفحات القانونية. يجب نشر الملفات المرفقة في المشروع على هذه المسارات
    /// قبل رفع التطبيق إلى Google Play / App Store (خصوصاً سياسة الخصوصية).
    ///
    /// نسخة مشتركة: الفتح يتم عبر IAppHost.OpenUrl
    /// (Intent على أندرويد / UIApplication.OpenUrl على iOS).
    /// </summary>
    public static class Legal
    {
        // مؤقتاً على Blogger (متاح في مصر) إلى أن يتم ربط الدومين الرسمي tawsela-app.com.
        public const string BaseUrl = "https://tawsela-app.blogspot.com";
        public const string PrivacyUrl = BaseUrl + "/p/privacy-policy.html";
        public const string TermsUrl = BaseUrl + "/p/terms-of-service.html";
        public const string DeleteAccountUrl = BaseUrl + "/p/account-deletion.html";

        /// <summary>فتح رابط خارجي. تُرجع false إذا تعذّر الفتح.</summary>
        public static bool Open(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return AppServices.OpenUrl(url);
        }

        /// <summary>فتح رابط مع رسالة خطأ عند الفشل عبر متصل مخصص (لأن Core لا يعرف UI).</summary>
        public static bool Open(string url, Action<string> onError)
        {
            var ok = Open(url);
            if (!ok && onError != null)
            {
                try { onError("تعذّر فتح الرابط"); } catch { }
            }
            return ok;
        }
    }
}
