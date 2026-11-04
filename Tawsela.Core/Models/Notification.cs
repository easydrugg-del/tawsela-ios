using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>إشعار داخلي للمستخدم.</summary>
    public class Notification
    {
        public int Id;
        public string Title = "";
        public string Body = "";
        public int OrderId;
        public bool IsRead;
        public DateTime CreatedAt;

        public static Notification FromRow(Dictionary<string, object> r)
        {
            var n = new Notification
            {
                Id = Row.Int(r, "Id"),
                Title = Row.Str(r, "Title"),
                Body = Row.Str(r, "Body"),
                OrderId = Row.Int(r, "OrderId"),
                IsRead = Row.Bool(r, "IsRead")
            };
            object d;
            if (r.TryGetValue("CreatedAt", out d) && d != null && d != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(d), out dt)) n.CreatedAt = dt;
            }
            return n;
        }
    }
}
