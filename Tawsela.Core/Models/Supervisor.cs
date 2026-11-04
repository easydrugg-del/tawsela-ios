using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>مشرف تكاتك — عمولته هي العمولة النقدية المخصومة فعلياً على رحلات سائقيه.</summary>
    public class Supervisor
    {
        public int Id;
        public string FullName = "";
        public string Phone = "";
        public string Code = "";
        public decimal CommissionPercent;
        public bool IsActive = true;
        public string Notes = "";
        public int DriversCount;
        public int RidesAll;
        public decimal CommissionAll;

        public static Supervisor FromRow(Dictionary<string, object> r)
        {
            return new Supervisor
            {
                Id = Row.Int(r, "Id"),
                FullName = Row.Str(r, "FullName"),
                Phone = Row.Str(r, "Phone"),
                Code = Row.Str(r, "Code"),
                CommissionPercent = Row.Num(r, "CommissionPercent"),
                IsActive = Row.Bool(r, "IsActive"),
                Notes = Row.Str(r, "Notes"),
                DriversCount = Row.Int(r, "DriversCount"),
                RidesAll = Row.Int(r, "RidesAll"),
                CommissionAll = Row.Num(r, "CommissionAll")
            };
        }

        public string StatusDisplay { get { return IsActive ? "نشط ✓" : "معطّل ✗"; } }
    }

    /// <summary>صرف مسجّل لمشرف عن مدة من وإلى.</summary>
    public class SupervisorPayout
    {
        public int Id;
        public DateTime FromDate;
        public DateTime ToDate;
        public decimal Amount;
        public string Note = "";
        public DateTime CreatedAt;

        public static SupervisorPayout FromRow(Dictionary<string, object> r)
        {
            return new SupervisorPayout
            {
                Id = Row.Int(r, "Id"),
                FromDate = DateOf(r, "FromDate"),
                ToDate = DateOf(r, "ToDate"),
                Amount = Row.Num(r, "Amount"),
                Note = Row.Str(r, "Note"),
                CreatedAt = DateOf(r, "CreatedAt")
            };
        }

        private static DateTime DateOf(Dictionary<string, object> r, string col)
        {
            object v;
            if (!r.TryGetValue(col, out v) || v == null || v == DBNull.Value) return default(DateTime);
            if (v is DateTime) return (DateTime)v;
            DateTime dt;
            if (DateTime.TryParse(Convert.ToString(v), out dt)) return dt;
            return default(DateTime);
        }

        public string PeriodDisplay
        {
            get { return FromDate.ToString("yyyy-MM-dd") + " ← " + ToDate.ToString("yyyy-MM-dd"); }
        }
    }

    /// <summary>أرباح مشرف خلال مدة (من/إلى) من الـ API.</summary>
    public class SupervisorEarnings
    {
        public int Rides;
        public decimal FareTotal;
        public decimal Commission;
        public decimal Paid;
        public List<SupervisorPayout> Payouts = new List<SupervisorPayout>();

        public decimal Remaining { get { return Commission - Paid; } }
    }
}
