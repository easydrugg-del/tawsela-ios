using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>
    /// منطقة/قرية مربوطة بمدينة ولها إحداثيات على الخريطة.
    /// تُدار من لوحة الإدارة وتظهر للعميل أسفل خانة الوجهة حسب مدينته.
    /// </summary>
    public class Area
    {
        public int Id;
        public string Name = "";
        public string City = "";
        public string Governorate = "";
        public double Lat, Lng;
        public bool IsActive = true;

        public static Area FromRow(Dictionary<string, object> r)
        {
            return new Area
            {
                Id = Row.Int(r, "Id"),
                Name = Row.Str(r, "Name"),
                City = Row.Str(r, "City"),
                Governorate = Row.Str(r, "Governorate"),
                Lat = Row.Dbl(r, "Lat"),
                Lng = Row.Dbl(r, "Lng"),
                IsActive = Row.Bool(r, "IsActive")
            };
        }
    }
}
