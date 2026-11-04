using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>كوبون/كود خصم.</summary>
    public class Coupon
    {
        public int Id;
        public string Code = "";
        public string Type = "percent";   // percent / fixed
        public decimal Value;              // نسبة مئوية أو مبلغ ثابت
        public decimal MaxOff;             // أقصى خصم مسموح (0/null = بلا حد)
        public bool Active = true;
        public DateTime ExpiresAt;          // وقت الانتهاء (MinValue = بلا انتهاء)

        public static Coupon FromRow(Dictionary<string, object> r)
        {
            var c = new Coupon
            {
                Id = Row.Int(r, "Id"),
                Code = Row.Str(r, "Code"),
                Type = Row.Str(r, "Type"),
                Value = Row.Num(r, "Value"),
                MaxOff = Row.Num(r, "MaxOff"),
                Active = Row.Bool(r, "Active")
            };
            object ex;
            if (r.TryGetValue("ExpiresAt", out ex) && ex != null && ex != DBNull.Value)
            {
                if (ex is DateTime) c.ExpiresAt = (DateTime)ex;
                else
                {
                    DateTime dt;
                    if (DateTime.TryParse(Convert.ToString(ex), out dt)) c.ExpiresAt = dt;
                }
            }
            return c;
        }

        /// <summary>هل انتهت صلاحية الكوبون.</summary>
        public bool IsExpired
        {
            get { return ExpiresAt != default(DateTime) && ExpiresAt < DateTime.Now; }
        }

        /// <summary>يحسب مبلغ الخصم على أجرة معيّنة.</summary>
        public decimal ComputeDiscount(decimal fare)
        {
            if (!Active || fare <= 0 || IsExpired) return 0m;
            decimal d = (Type == "percent")
                ? fare * Value / 100m
                : Value;
            if (MaxOff > 0 && d > MaxOff) d = MaxOff;
            if (d > fare) d = fare;
            return System.Math.Round(d, 2);
        }
    }
}
