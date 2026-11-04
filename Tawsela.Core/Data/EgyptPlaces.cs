using System.Collections.Generic;

namespace Tawsela.Data
{
    /// <summary>
    /// نطاق خدمة «توصيلة» الحالي: محافظة الغربية (مدينة زفتى) ومحافظة الدقهلية (مدينة ميت غمر) فقط.
    /// تُستخدم هذه القائمة في التسجيل ولوحة الإدارة وشاشة الطلب.
    /// </summary>
    public static class EgyptPlaces
    {
        private static Dictionary<string, List<string>> _map;

        public static Dictionary<string, List<string>> Map()
        {
            if (_map != null) return _map;
            _map = new Dictionary<string, List<string>>();
            Add("الغربية", "زفتى");
            Add("الدقهلية", "ميت غمر");
            return _map;
        }

        private static void Add(string gov, params string[] cities)
        {
            _map[gov] = new List<string>(cities);
        }

        public static List<string> Governorates()
        {
            return new List<string>(Map().Keys);
        }

        public static List<string> CitiesOf(string gov)
        {
            List<string> list;
            return Map().TryGetValue(gov ?? "", out list) ? list : new List<string>();
        }

        /// <summary>المحافظة التابعة لها مدينة (أو "" إن لم تكن ضمن نطاق الخدمة).</summary>
        public static string GovernorateOf(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return "";
            foreach (var kv in Map())
                if (kv.Value.Contains(city)) return kv.Key;
            return "";
        }
    }
}
