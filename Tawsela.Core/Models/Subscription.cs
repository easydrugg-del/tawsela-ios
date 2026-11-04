using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    public class Subscription
    {
        public int Id;
        public int UserId;
        public string FullName = "";
        public string Phone = "";
        public string Plan = "";
        public decimal Price;
        public DateTime StartAt;
        public DateTime ExpiresAt;

        public bool IsActive
        {
            get { return ExpiresAt > DateTime.Now; }
        }

        public string StatusDisplay
        {
            get
            {
                if (IsActive)
                {
                    var days = (ExpiresAt - DateTime.Now).Days;
                    if (days > 30) return "نشط (" + (days / 30) + " شهر)";
                    return "نشط (" + days + " يوم)";
                }
                return "منتهي";
            }
        }

        public int StatusColor
        {
            get
            {
                if (!IsActive) return unchecked((int)0xFF888888);
                var days = (ExpiresAt - DateTime.Now).Days;
                if (days <= 7) return unchecked((int)0xFFFFC107);
                return unchecked((int)0xFF28A745);
            }
        }

        public static Subscription FromRow(Dictionary<string, object> r)
        {
            var s = new Subscription
            {
                Id = Row.Int(r, "Id"),
                UserId = Row.Int(r, "UserId"),
                FullName = Row.Str(r, "FullName"),
                Phone = Row.Str(r, "Phone"),
                Plan = Row.Str(r, "Plan"),
                Price = (decimal)Row.Dbl(r, "Price")
            };
            object v;
            if (r.TryGetValue("StartAt", out v) && v != null && v != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(v), out dt)) s.StartAt = dt;
            }
            if (r.TryGetValue("ExpiresAt", out v) && v != null && v != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(v), out dt)) s.ExpiresAt = dt;
            }
            return s;
        }
    }
}
