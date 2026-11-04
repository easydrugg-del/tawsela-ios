using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>إعلان/برومو يظهر لعملاء أو سائقين حسب النطاق الجغرافي.</summary>
    public class Ad
    {
        public int Id;
        public string Title = "";
        public string Body = "";
        public string ImageData = "";
        public string LinkUrl = "";
        public bool IsActive = true;
        /// <summary>الكل / المحافظة / المدينة</summary>
        public string TargetScope = "الكل";
        public string TargetGov = "";
        public string TargetCities = "";

        public static Ad FromRow(Dictionary<string, object> r)
        {
            var scope = Row.Str(r, "TargetScope");
            if (scope == "") scope = "الكل";
            return new Ad
            {
                Id = Row.Int(r, "Id"),
                Title = Row.Str(r, "Title"),
                Body = Row.Str(r, "Body"),
                ImageData = Row.Str(r, "ImageData"),
                LinkUrl = Row.Str(r, "LinkUrl"),
                IsActive = Row.Bool(r, "IsActive"),
                TargetScope = scope,
                TargetGov = Row.Str(r, "TargetGov"),
                TargetCities = Row.Str(r, "TargetCities")
            };
        }

        /// <summary>وصف مختصر للنطاق الجغرافي.</summary>
        public string TargetSummary()
        {
            if (TargetScope == "المحافظة")
                return string.IsNullOrEmpty(TargetGov) ? "[الكل]" : "[المحافظة: " + TargetGov + "]";
            if (TargetScope == "المدينة")
                return string.IsNullOrEmpty(TargetCities) ? "[الكل]" : "[المدن: " + TargetCities + "]";
            return "[الكل]";
        }
    }
}
