using System;
using System.Collections.Generic;
using Tawsela.Core.Platform;

namespace Tawsela.Data
{
    /// <summary>
    /// حظر محلي (على جهاز السائق) للعميل. يمنع فتح المحادثة وإرسال الرسائل.
    /// الحظر من جهة العميل للسائق يظل عبر الخادم (Api.BlockDriver).
    ///
    /// نسخة مشتركة: تعمل على أندرويد و iOS عبر AppServices.Prefs
    /// (SharedPreferences على أندرويد / NSUserDefaults على iOS).
    /// </summary>
    public static class LocalBlocks
    {
        private const string PrefName = "tawsela";

        private static string Key()
        {
            return PrefName + ".blocked_customers_" + Session.UserId;
        }

        public static bool IsBlocked(int userId)
        {
            if (userId <= 0) return false;
            try
            {
                var raw = AppServices.Prefs.Get(Key(), "") ?? "";
                foreach (var part in raw.Split(','))
                {
                    int id;
                    if (int.TryParse(part, out id) && id == userId) return true;
                }
            }
            catch { }
            return false;
        }

        public static void Block(int userId)
        {
            if (userId <= 0) return;
            try
            {
                var raw = AppServices.Prefs.Get(Key(), "") ?? "";
                var ids = new List<string>();
                foreach (var part in raw.Split(','))
                    if (!string.IsNullOrWhiteSpace(part)) ids.Add(part.Trim());
                if (!ids.Contains(userId.ToString()))
                    ids.Add(userId.ToString());
                AppServices.Prefs.Set(Key(), string.Join(",", ids));
            }
            catch { }
        }

        public static void Unblock(int userId)
        {
            if (userId <= 0) return;
            try
            {
                var raw = AppServices.Prefs.Get(Key(), "") ?? "";
                var ids = new List<string>();
                foreach (var part in raw.Split(','))
                    if (!string.IsNullOrWhiteSpace(part) && part.Trim() != userId.ToString())
                        ids.Add(part.Trim());
                AppServices.Prefs.Set(Key(), string.Join(",", ids));
            }
            catch { }
        }
    }
}
