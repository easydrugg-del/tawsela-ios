using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>عنوان مفضّل محفوظ للعميل (المنزل/العمل...).</summary>
    public class SavedPlace
    {
        public int Id;
        public int UserId;
        public string Label = "";
        public string Address = "";
        public double Lat;
        public double Lng;

        public static SavedPlace FromRow(Dictionary<string, object> r)
        {
            return new SavedPlace
            {
                Id = Row.Int(r, "Id"),
                UserId = Row.Int(r, "UserId"),
                Label = Row.Str(r, "Label"),
                Address = Row.Str(r, "Address"),
                Lat = Row.Dbl(r, "Lat"),
                Lng = Row.Dbl(r, "Lng")
            };
        }
    }
}
