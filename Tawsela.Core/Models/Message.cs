using System;
using System.Collections.Generic;
using Tawsela.Data;

namespace Tawsela.Models
{
    /// <summary>رسالة دردشة بين عميل وسائق داخل طلب.</summary>
    public class Message
    {
        public int Id;
        public int OrderId;
        public int SenderId;
        public int ReceiverId;
        public string Text = "";
        public DateTime SentAt;
        public bool IsRead;

        public static Message FromRow(Dictionary<string, object> r)
        {
            if (r == null) return null;
            var m = new Message();
            m.Id = Row.Int(r, "Id");
            m.OrderId = Row.Int(r, "OrderId");
            m.SenderId = Row.Int(r, "SenderId");
            m.ReceiverId = Row.Int(r, "ReceiverId");
            m.Text = Row.Str(r, "Text");
            m.IsRead = Row.Bool(r, "IsRead");
            var s = Row.Str(r, "SentAt");
            if (s != "") DateTime.TryParse(s, out m.SentAt);
            return m;
        }
    }
}
