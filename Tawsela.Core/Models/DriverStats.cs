using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>إحصائيات سائق (اليوم/الأسبوع/الكلي).</summary>
    public class DriverStats
    {
        public int TotalRides = 0;
        public decimal TotalEarnings = 0m;   // إجمالي الأجرة
        public decimal TodayEarnings = 0m;
        public int TodayRides = 0;
        public decimal AvgRating = 0m;       // متوسط تقييم السائق
        public int RatingCount = 0;

        public string RatingText
        {
            get { return RatingCount > 0 ? AvgRating.ToString("0.0") + " ★ (" + RatingCount + ")" : "لا تقييمات"; }
        }
    }
}
