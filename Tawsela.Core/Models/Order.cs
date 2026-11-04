using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>يمثّل طلب توصيلة في قاعدة البيانات.</summary>
    public class Order
    {
        public int Id;
        public string OrderNumber = "";
        public int CustomerId;
        public string CustomerName = "";
        public string CustomerPhone = "";
        public int DriverId;
        public string DriverName = "";
        public string DriverPhone = "";
        public int ServiceTypeId;
        public string ServiceTypeName = "";
        public string PaymentMethod = "";
        public double FromLat, FromLng, ToLat, ToLng;
        public string FromAddress = "";
        public string ToAddress = "";
        public string Note = "";
        public decimal EstimatedFare;
        public decimal ActualFare;
        public string Status = "جديد";
        public DateTime CreatedAt;
        public DateTime CompletedAt;     // وقت إتمام الرحلة (MinValue = غير مكتملة)
        public DateTime ScheduledAt;     // وقت التنفيذ للطلب المجدول (MinValue = فوري)
        public string CouponCode = "";
        public decimal Discount;
        public decimal Tip;
        /// <summary>عمولة التطبيق المحسوبة على الرحلة (تُخصم من السائق عند القبول).</summary>
        public decimal AppCommission;
        public bool CommissionSettled;
        public string PinCode = "";
        public double StopLat, StopLng;
        public string StopAddress = "";
        public decimal OfferedPrice;
        public bool BiddingEnabled;
        public decimal CancelFee;
        public string CancelReason = "";
        public string FloorInfo = "";
        public bool CustomerConfirmedArrival;
        public DateTime CustomerConfirmedAt;
        public DateTime ArrivalReportedAt;

        /// <summary>تلوين حالة الطلب للعرض (ARGB صريح — بديل Resource.Color في أندرويد).</summary>
        public int StatusColor
        {
            get
            {
                switch (Status)
                {
                    case "مكتمل": return ColorGreen;
                    case "ملغي": return ColorGray;
                    case "أثناء_الرحلة": 
                    case "في_الطريق": return ColorBlue;
                    case "وصل_السائق": 
                    case "وصلت_الوجهة": return ColorAccent;
                    default: return ColorAccent; // جديد / قيد_التنفيذ
                }
            }
        }

        // نفس قيم Resources/values/colors.xml حتى يتطابق اللون بين أندرويد و iOS
        /// <summary>#D32F2F</summary>
        public const int ColorRed = unchecked((int)0xFFD32F2F);
        /// <summary>#388E3C</summary>
        public const int ColorGreen = unchecked((int)0xFF388E3C);
        /// <summary>#1976D2</summary>
        public const int ColorBlue = unchecked((int)0xFF1976D2);
        /// <summary>#9E9E9E</summary>
        public const int ColorGray = unchecked((int)0xFF9E9E9E);
        /// <summary>#FF8C1A</summary>
        public const int ColorAccent = unchecked((int)0xFFFF8C1A);

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case "مكتمل": return "مكتمل";
                    case "ملغي": return "ملغي";
                    case "قيد_التنفيذ": return "بانتظار سائق";
                    case "في_الطريق": return "السائق في الطريق إليك";
                    case "وصل_السائق": return "وصل السائق";
                    case "أثناء_الرحلة": return "أثناء الرحلة";
                    case "وصلت_الوجهة": return "وصلت إلى الوجهة";
                    default: return "جديد";
                }
            }
        }

        /// <summary>أيقونة معبرة عن حالة الطلب.</summary>
        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case "مكتمل": return "✅";
                    case "ملغي": return "❌";
                    case "قيد_التنفيذ": return "⏳";
                    case "في_الطريق": return "🚗";
                    case "وصل_السائق": return "📍";
                    case "أثناء_الرحلة": return "🛣️";
                    case "وصلت_الوجهة": return "🏁";
                    default: return "🆕";
                }
            }
        }

        public static Order FromRow(Dictionary<string, object> r)
        {
            var o = new Order
            {
                Id = Row.Int(r, "Id"),
                OrderNumber = Row.Str(r, "OrderNumber"),
                CustomerId = Row.Int(r, "CustomerId"),
                CustomerName = Row.Str(r, "CustomerName"),
                CustomerPhone = Row.Str(r, "CustomerPhone"),
                DriverId = Row.Int(r, "DriverId"),
                DriverName = Row.Str(r, "DriverName"),
                DriverPhone = Row.Str(r, "DriverPhone"),
                ServiceTypeId = Row.Int(r, "ServiceTypeId"),
                ServiceTypeName = Row.Str(r, "ServiceTypeName"),
                PaymentMethod = Row.Str(r, "PaymentMethod"),
                FromLat = Row.Dbl(r, "FromLat"),
                FromLng = Row.Dbl(r, "FromLng"),
                ToLat = Row.Dbl(r, "ToLat"),
                ToLng = Row.Dbl(r, "ToLng"),
                FromAddress = Row.Str(r, "FromAddress"),
                ToAddress = Row.Str(r, "ToAddress"),
                Note = Row.Str(r, "Note"),
                EstimatedFare = Row.Num(r, "EstimatedFare"),
                ActualFare = Row.Num(r, "ActualFare"),
                Status = Row.Str(r, "Status"),
                CouponCode = Row.Str(r, "CouponCode"),
                Discount = Row.Num(r, "Discount"),
                Tip = Row.Num(r, "Tip"),
                AppCommission = Row.Num(r, "AppCommission"),
                CommissionSettled = Row.Bool(r, "CommissionSettled"),
                PinCode = Row.Str(r, "PinCode"),
                StopLat = Row.Dbl(r, "StopLat"),
                StopLng = Row.Dbl(r, "StopLng"),
                StopAddress = Row.Str(r, "StopAddress"),
                OfferedPrice = Row.Num(r, "OfferedPrice"),
                BiddingEnabled = Row.Bool(r, "BiddingEnabled"),
                CancelFee = Row.Num(r, "CancelFee"),
                CancelReason = Row.Str(r, "CancelReason"),
                FloorInfo = Row.Str(r, "FloorInfo"),
                CustomerConfirmedArrival = Row.Bool(r, "CustomerConfirmedArrival")
            };
            object t;
            if (r.TryGetValue("CreatedAt", out t) && t != null && t != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(t), out dt)) o.CreatedAt = dt;
            }
            object comp;
            if (r.TryGetValue("CompletedAt", out comp) && comp != null && comp != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(comp), out dt)) o.CompletedAt = dt;
            }
            object s;
            if (r.TryGetValue("ScheduledAt", out s) && s != null && s != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(s), out dt)) o.ScheduledAt = dt;
            }
            object ca;
            if (r.TryGetValue("CustomerConfirmedAt", out ca) && ca != null && ca != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(ca), out dt)) o.CustomerConfirmedAt = dt;
            }
            object ra;
            if (r.TryGetValue("ArrivalReportedAt", out ra) && ra != null && ra != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(ra), out dt)) o.ArrivalReportedAt = dt;
            }
            if (o.ServiceTypeName == "")
            {
                object sv;
                if (r.TryGetValue("ServiceName", out sv)) o.ServiceTypeName = Row.Str(sv);
            }
            return o;
        }
    }
}
