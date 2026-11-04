using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    public class Complaint
    {
        public int Id;
        public int OrderId;
        public int UserId;
        public int TargetId;
        public string Subject = "";
        public string Body = "";
        public string Status = "مفتوحة";
        public string Reply = "";
        public string UserName = "";
        public DateTime CreatedAt;

        public static Complaint FromRow(Dictionary<string, object> r)
        {
            var c = new Complaint
            {
                Id = Row.Int(r, "Id"),
                OrderId = Row.Int(r, "OrderId"),
                UserId = Row.Int(r, "UserId"),
                TargetId = Row.Int(r, "TargetId"),
                Subject = Row.Str(r, "Subject"),
                Body = Row.Str(r, "Body"),
                Status = Row.Str(r, "Status"),
                Reply = Row.Str(r, "Reply"),
                UserName = Row.Str(r, "UserName")
            };
            object v;
            if (r.TryGetValue("CreatedAt", out v) && v != null && v != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(v), out dt)) c.CreatedAt = dt;
            }
            return c;
        }
    }
}
