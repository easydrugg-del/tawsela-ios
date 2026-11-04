using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>نوع خدمة (توصيلة عادية / VIP / موتوسيكل) مع سعره.</summary>
    public class ServiceType
    {
        public int Id;
        public string Name = "";
        public string Description = "";
        public decimal BaseFare;
        public decimal PerKm;
        /// <summary>نطاق الكيلومترات المشمول بالمبلغ الأساسي (افتراضي 2).</summary>
        public decimal BaseKm = 2m;
        /// <summary>سعر كل كيلومتر إضافي بعد النطاق.</summary>
        public decimal ExtraPerKm;
        /// <summary>وضع التسعير: تلقائي / نهار / ليل.</summary>
        public string PricingMode = "تلقائي";
        /// <summary>بيانات التسعيرة الليلية (0/فارغ = نفس النهارية).</summary>
        public decimal NightBaseFare;
        public decimal NightExtraPerKm;
        public decimal NightMultiplier = 1m;
        public decimal Multiplier = 1m;
        /// <summary>عمولة ثابتة على الرحلة تُطبق حتى نطاق BaseKm.</summary>
        public decimal CommissionFixed;
        /// <summary>نسبة إضافية من إجمالي الأجرة إذا تجاوزت المسافة BaseKm.</summary>
        public decimal CommissionPercent;
        public bool IsActive = true;

        public static ServiceType FromRow(Dictionary<string, object> r)
        {
            var bk = Row.Num(r, "BaseKm");
            if (bk <= 0) bk = 2m;
            var epk = Row.Num(r, "ExtraPerKm");
            if (epk <= 0) epk = Row.Num(r, "PerKm");
            var nm = Row.Num(r, "NightMultiplier");
            if (nm <= 0) nm = 1m;
            var mode = Row.Str(r, "PricingMode");
            if (mode != "نهار" && mode != "ليل") mode = "تلقائي";
            return new ServiceType
            {
                Id = Row.Int(r, "Id"),
                PricingMode = mode,
                Name = Row.Str(r, "Name"),
                Description = Row.Str(r, "Description"),
                BaseFare = Row.Num(r, "BaseFare"),
                PerKm = Row.Num(r, "PerKm"),
                BaseKm = bk,
                ExtraPerKm = epk,
                NightBaseFare = Row.Num(r, "NightBaseFare"),
                NightExtraPerKm = Row.Num(r, "NightExtraPerKm"),
                NightMultiplier = nm,
                Multiplier = Row.Num(r, "Multiplier"),
                CommissionFixed = Row.Num(r, "CommissionFixed"),
                CommissionPercent = Row.Num(r, "CommissionPercent"),
                IsActive = Row.Bool(r, "IsActive")
            };
        }
    }
}
