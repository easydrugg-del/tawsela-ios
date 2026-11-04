using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    public class OrderOffer
    {
        public int Id;
        public int OrderId;
        public int DriverId;
        public string DriverName = "";
        public string DriverPhone = "";
        public string CarInfo = "";
        public decimal OfferPrice;
        public string Status = "معلق";
        public DateTime CreatedAt;

        public static OrderOffer FromRow(Dictionary<string, object> r)
        {
            var o = new OrderOffer
            {
                Id = Row.Int(r, "Id"),
                OrderId = Row.Int(r, "OrderId"),
                DriverId = Row.Int(r, "DriverId"),
                DriverName = Row.Str(r, "DriverName"),
                DriverPhone = Row.Str(r, "DriverPhone"),
                CarInfo = Row.Str(r, "CarInfo"),
                OfferPrice = Row.Num(r, "OfferPrice"),
                Status = Row.Str(r, "Status")
            };
            object v;
            if (r.TryGetValue("CreatedAt", out v) && v != null && v != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(v), out dt)) o.CreatedAt = dt;
            }
            return o;
        }
    }
}
