using System;
using System.Collections.Generic;

namespace Tawsela.Data
{
    /// <summary>
    /// مساعدات قراءة صفوف JSON (Dictionary) مع تحويل آمن للأنواع.
    /// بديل خفيف للطبقة القديمة Db بدون أي اتصال بقاعدة بيانات.
    /// </summary>
    public static class Row
    {
        public static string Str(object val)
        {
            return val == null || val == DBNull.Value ? "" : Convert.ToString(val);
        }

        public static string Str(Dictionary<string, object> row, string column)
        {
            if (row == null) return "";
            object val;
            return row.TryGetValue(column, out val) ? Str(val) : "";
        }

        /// <summary>تنسيق تاريخ/وقت للعرض (يتقبل نص ISO أو DateTime).</summary>
        public static string Date(object val)
        {
            if (val == null || val == DBNull.Value) return "";
            if (val is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm");
            var s = Convert.ToString(val) ?? "";
            DateTime parsed;
            if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out parsed))
                return parsed.ToString("yyyy-MM-dd HH:mm");
            return s;
        }

        public static string Date(Dictionary<string, object> row, string column)
        {
            if (row == null) return "";
            object val;
            return row.TryGetValue(column, out val) ? Date(val) : "";
        }

        public static int Int(object val)
        {
            if (val == null || val == DBNull.Value) return 0;
            if (val is int i) return i;
            if (val is long l) return (int)l;
            if (val is short sh) return sh;
            if (val is byte by) return by;
            if (val is decimal de) return (int)de;
            if (val is double db) return (int)db;
            int parsed;
            return int.TryParse(Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        public static int Int(Dictionary<string, object> row, string column)
        {
            if (row == null) return 0;
            object val;
            return row.TryGetValue(column, out val) ? Int(val) : 0;
        }

        public static decimal Num(object val)
        {
            if (val == null || val == DBNull.Value) return 0m;
            if (val is decimal) return (decimal)val;
            decimal d;
            return decimal.TryParse(Convert.ToString(val),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d) ? d : 0m;
        }

        public static decimal Num(Dictionary<string, object> row, string column)
        {
            if (row == null) return 0m;
            object val;
            return row.TryGetValue(column, out val) ? Num(val) : 0m;
        }

        public static bool Bool(object val)
        {
            if (val == null || val == DBNull.Value) return false;
            if (val is bool) return (bool)val;
            if (val is byte) return (byte)val != 0;
            if (val is byte[]) return ((byte[])val).Length > 0 && ((byte[])val)[0] != 0;
            if (val is short) return (short)val != 0;
            if (val is int) return (int)val != 0;
            if (val is long) return (long)val != 0;
            if (val is float) return (float)val != 0;
            if (val is double) return (double)val != 0;
            if (val is decimal) return (decimal)val != 0m;
            var s = Convert.ToString(val).Trim();
            if (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (s == "" || s == "0") return false;
            decimal d;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d) && d != 0m;
        }

        public static bool Bool(Dictionary<string, object> row, string column)
        {
            if (row == null) return false;
            object val;
            return row.TryGetValue(column, out val) ? Bool(val) : false;
        }

        public static double Dbl(object val)
        {
            if (val == null || val == DBNull.Value) return 0d;
            // JSON numbers تصل من ToClr كـ decimal؛ التحويل المباشر يتجنب
            // Convert.ToString الذي يستخدم ثقافة الجهاز (فاصلة عشرية عربية) ويُفشل TryParse.
            if (val is double db) return db;
            if (val is float fl) return fl;
            if (val is decimal de) return (double)de;
            if (val is int i) return i;
            if (val is long l) return l;
            if (val is short sh) return sh;
            if (val is byte by) return by;
            double d;
            var text = Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            return double.TryParse(text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d) ? d : 0d;
        }

        public static double Dbl(Dictionary<string, object> row, string column)
        {
            if (row == null) return 0d;
            object val;
            return row.TryGetValue(column, out val) ? Dbl(val) : 0d;
        }
    }
}
