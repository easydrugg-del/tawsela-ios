using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>حركة واحدة في المحفظة (إيداع/شحن/سداد).</summary>
    public class WalletTxn
    {
        public int Id;
        public decimal Amount;
        public string Type = "";
        public string Note = "";
        public DateTime CreatedAt;

        public string AmountText
        {
            get { return (Amount >= 0 ? "+" : "-") + System.Math.Abs(Amount).ToString("0.00") + " ج.م"; }
        }

        public static WalletTxn FromRow(Dictionary<string, object> r)
        {
            var t = new WalletTxn
            {
                Id = Row.Int(r, "Id"),
                Amount = Row.Num(r, "Amount"),
                Type = Row.Str(r, "Type"),
                Note = Row.Str(r, "Note")
            };
            object d;
            if (r.TryGetValue("CreatedAt", out d) && d != null && d != DBNull.Value)
            {
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(d), out dt)) t.CreatedAt = dt;
            }
            return t;
        }
    }
}
