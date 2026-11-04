using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    public class DriverDoc
    {
        public int Id;
        public int UserId;
        public string UserName = "";
        public string FullName = "";
        public string DocType = "";
        public string DocInfo = "";
        public string DocImage = "";
        public string Status = "قيد_المراجعة";
        public string CarPlate = "";
        public string CarModel = "";
        public string CarColor = "";
        public DateTime CreatedAt;

        public static DriverDoc FromRow(Dictionary<string, object> r)
        {
            var d = new DriverDoc
            {
                Id = Row.Int(r, "Id"),
                UserId = Row.Int(r, "UserId"),
                UserName = Row.Str(r, "UserName"),
                FullName = Row.Str(r, "FullName"),
                DocType = Row.Str(r, "DocType"),
                DocInfo = Row.Str(r, "DocInfo"),
                DocImage = Row.Str(r, "DocImage"),
                Status = Row.Str(r, "Status"),
                CarPlate = Row.Str(r, "CarPlate"),
                CarModel = Row.Str(r, "CarModel"),
                CarColor = Row.Str(r, "CarColor")
            };
            object v;
            if (r.TryGetValue("CreatedAt", out v) && v != null && v != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(v), out dt)) d.CreatedAt = dt;
            }
            return d;
        }
    }
}
