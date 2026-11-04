using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>لقطة عميل محذوف من أرشيف المحذوفين.</summary>
    public class DeletedCustomer
    {
        public int Id;
        public int UserId;
        public string UserName = "";
        public string FullName = "";
        public string Phone = "";
        public string Governorate = "";
        public string City = "";
        public bool WasActive = true;
        public string DeletedBy = "";
        public string DeletedAt = "";

        public static DeletedCustomer FromRow(Dictionary<string, object> r)
        {
            if (r == null) return null;
            var c = new DeletedCustomer();
            c.Id = Row.Int(r, "Id");
            c.UserId = Row.Int(r, "UserId");
            c.UserName = Row.Str(r, "UserName");
            c.FullName = Row.Str(r, "FullName");
            c.Phone = Row.Str(r, "Phone");
            c.Governorate = Row.Str(r, "Governorate");
            c.City = Row.Str(r, "City");
            c.WasActive = Row.Bool(r, "WasActive");
            c.DeletedBy = Row.Str(r, "DeletedBy");
            object d;
            if (r.TryGetValue("DeletedAt", out d) && d != null && d != System.DBNull.Value)
                c.DeletedAt = System.Convert.ToString(d);
            return c;
        }
    }
}
