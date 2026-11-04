using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>
    /// شارع مسجّل في جدول Streets المستقل، مربوط بمدينة ومحافظة وله إحداثيات.
    /// يُدار من شاشة «الشوارع» ويظهر للعميل أسفل خانة الوجهة.
    /// </summary>
    public class Street
    {
        public int Id;
        public string Name = "";
        public string City = "";
        public string Governorate = "";
        public double Lat, Lng;
        public bool IsActive = true;

        public static Street FromRow(Dictionary<string, object> r)
        {
            return new Street
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
