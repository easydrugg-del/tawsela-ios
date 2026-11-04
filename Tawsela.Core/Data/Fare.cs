using System;
using Tawsela.Models;

namespace Tawsela.Data
{
    /// <summary>
    /// حسابات الأجرة والتسعيرة الليلية — منطق عرض فقط.
    /// المرجع النهائي للسعر هو الخادم عند إنشاء الطلب.
    /// </summary>
    public static class Fare
    {
        public static bool IsNightAt(DateTime time, int fromHour, int toHour)
        {
            int h = time.Hour;
            if (fromHour == toHour) return false;
            if (fromHour < toHour) return h >= fromHour && h < toHour;
            return h >= fromHour || h < toHour;
        }

        public static bool IsNightNow(int fromHour, int toHour)
        {
            return IsNightAt(DateTime.Now, fromHour, toHour);
        }

        /// <summary>هل التسعيرة الليلية مطبقة على نوع الخدمة الآن؟</summary>
        public static bool IsNightEffective(ServiceType st, int nightFrom, int nightTo)
        {
            if (st == null) return false;
            if (st.PricingMode == "ليل") return true;
            if (st.PricingMode == "نهار") return false;
            return nightFrom >= 0 && IsNightNow(nightFrom, nightTo);
        }

        public static decimal Estimate(ServiceType st, double km)
        {
            return Estimate(st, km, -1, -1);
        }

        public static decimal Estimate(ServiceType st, double km, int nightFrom, int nightTo)
        {
            return Estimate(st, km, DateTime.Now, nightFrom, nightTo);
        }

        /// <summary>تقدير الأجرة حسب المسافة والوقت (قيمة تقريبية للعرض).</summary>
        public static decimal Estimate(ServiceType st, double km, DateTime orderTime, int nightFrom, int nightTo)
        {
            // احتياطي فقط عند تعذّر تحميل الخدمات: نطابق تسعيرة "توصيلة عادية"
            // المسجلة في الخادم (16 ج.م لأول 2 كم + 5 ج.م لكل كم إضافي)
            // بدل المعادلة القديمة 2 كم × 15 = 30 ج.م لأي مسافة أقل من 2 كم.
            if (st == null) return EstimateFallback(km);

            bool night = st.PricingMode == "ليل" ? true
                : st.PricingMode == "نهار" ? false
                : (nightFrom >= 0 && IsNightAt(orderTime, nightFrom, nightTo));

            decimal baseFare = (night && st.NightBaseFare > 0) ? st.NightBaseFare : st.BaseFare;
            decimal perExtra = (night && st.NightExtraPerKm > 0) ? st.NightExtraPerKm : st.ExtraPerKm;
            if (perExtra <= 0) perExtra = st.PerKm;
            decimal mult = (night && st.NightMultiplier > 0) ? st.NightMultiplier : st.Multiplier;
            if (mult <= 0) mult = 1m;
            decimal baseKm = st.BaseKm > 0 ? st.BaseKm : 2m;
            // الحساب بالمتر: المسافة تُحوَّل لمتر، وسعر المتر = سعر الكيلو ÷ 1000،
            // فتُحسب الأمتار الزائدة عن النطاق المشمول بدقة (بدون تقريب للكيلو).
            decimal meters = (decimal)km * 1000m;
            decimal includedMeters = baseKm * 1000m;
            decimal perMeter = perExtra / 1000m;
            decimal subtotal = meters <= includedMeters
                ? baseFare
                : baseFare + (meters - includedMeters) * perMeter;
            var total = subtotal * mult;
            return Math.Max(1, Math.Round(total, 0, MidpointRounding.AwayFromZero));
        }

        private static decimal EstimateFallback(double km)
        {
            decimal baseFare = 16m;
            decimal baseKm = 2m;
            decimal extraPerKm = 5m;
            decimal d = (decimal)km;
            decimal meters = d * 1000m;
            decimal includedMeters = baseKm * 1000m;
            decimal perMeter = extraPerKm / 1000m;
            decimal subtotal = meters <= includedMeters ? baseFare : baseFare + (meters - includedMeters) * perMeter;
            return Math.Max(1, Math.Round(subtotal, 0, MidpointRounding.AwayFromZero));
        }
    }
}
