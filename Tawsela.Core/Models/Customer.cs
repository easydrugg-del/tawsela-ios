using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>حساب عميل (مستخدم بدور عميل).</summary>
    public class Customer
    {
        public int UserId;
        public string FullName = "";
        public string Phone = "";
        public string Governorate = "";
        public string City = "";
        public bool IsActive;

        public static Customer FromRow(Dictionary<string, object> r)
        {
            if (r == null) return null;
            var c = new Customer();
            c.UserId = Row.Int(r, "Id");
            c.FullName = Row.Str(r, "FullName");
            c.Phone = Row.Str(r, "Phone");
            c.Governorate = Row.Str(r, "Governorate");
            c.City = Row.Str(r, "City");
            c.IsActive = Row.Bool(r, "IsActive");
            return c;
        }
    }
}
