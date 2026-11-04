using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>سائق متاح للعرض أمام العميل.</summary>
    public class Driver
    {
        public int UserId;
        public string FullName = "";
        public string Phone = "";
        public string CarModel = "";
        public string CarPlate = "";
        public string CarColor = "";
        public bool IsAvailable;
        public bool IsActive = true;
        public string DriverStatus = "";
        public string Governorate = "";
        public string City = "";
        /// <summary>الرقم الخاص بالسائق (مثل D-1001).</summary>
        public string DriverNumber = "";
        public double Lat;
        public double Lng;
        public double DistanceKm;
        /// <summary>المشرف المرتبط بالسائق (0 = بدون).</summary>
        public int SupervisorId;
        public string SupervisorName = "";

        public static Driver FromRow(Dictionary<string, object> r)
        {
            return new Driver
            {
                UserId = Row.Int(r, "UserId"),
                FullName = Row.Str(r, "FullName"),
                Phone = Row.Str(r, "Phone"),
                CarModel = Row.Str(r, "CarModel"),
                CarPlate = Row.Str(r, "CarPlate"),
                CarColor = Row.Str(r, "CarColor"),
                IsAvailable = Row.Bool(r, "IsAvailable"),
                IsActive = Row.Bool(r, "IsActive"),
                DriverStatus = Row.Str(r, "DriverStatus"),
                Governorate = Row.Str(r, "Governorate"),
                City = Row.Str(r, "City"),
                DriverNumber = Row.Str(r, "DriverNumber"),
                SupervisorId = Row.Int(r, "SupervisorId"),
                SupervisorName = Row.Str(r, "SupervisorName"),
                Lat = Row.Dbl(r, "CurrentLat"),
                Lng = Row.Dbl(r, "CurrentLng")
            };
        }

        public string StatusDisplay
        {
            get
            {
                if (!IsActive) return "محظور";
                switch (DriverStatus)
                {
                    case "نشط": return "نشط ✓";
                    case "قيد_المراجعة": return "⏳ قيد المراجعة";
                    case "مرفوض": return "مرفوض ✗";
                    case "موقوف": return "موقوف ⛔";
                    default: return "⏳ قيد المراجعة";
                }
            }
        }

        public int StatusColor
        {
            get
            {
                if (!IsActive) return unchecked((int)0xFF888888);
                switch (DriverStatus)
                {
                    case "نشط": return unchecked((int)0xFF28A745);
                    case "قيد_المراجعة": return unchecked((int)0xFFFD7E14);
                    case "مرفوض": return unchecked((int)0xFFDC3545);
                    case "موقوف": return unchecked((int)0xFFDC3545);
                    default: return unchecked((int)0xFFFD7E14);
                }
            }
        }
    }
}
