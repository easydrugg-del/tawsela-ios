using System;
using System.Collections.Generic;
using Tawsela.Data;
using UIKit;

namespace Tawsela.iOS.Screens
{
    /// <summary>
    /// بديل Activities/RestaurantApprovalActivity.cs — الإدارة تعتمد أو ترفض
    /// المطاعم التي سجّلت نفسها (Restaurants.IsActive = 0).
    /// </summary>
    public class RestaurantApprovalsViewController : ListViewController<Dictionary<string, object>>
    {
        public RestaurantApprovalsViewController() : base("طلبات تفعيل المطاعم") { }

        protected override string EmptyText
        {
            get { return "لا توجد طلبات تفعيل حاليًا"; }
        }

        protected override string Subtitle
        {
            get { return "المطاعم التي سجّلت نفسها ولم تُعتمد بعد"; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // زر «تحديث» (بديل btnRefresh في أندرويد)
            NavigationItem.RightBarButtonItem = new UIBarButtonItem("تحديث",
                UIBarButtonItemStyle.Plain, (s, e) => Reload());
        }

        protected override List<Dictionary<string, object>> Fetch()
        {
            return Api.PendingRestaurants();
        }

        protected override UIView BuildRow(Dictionary<string, object> r)
        {
            var id = Tawsela.Data.Row.Int(r, "Id");
            var name = Tawsela.Data.Row.Str(r, "Name");
            var owner = Tawsela.Data.Row.Str(r, "OwnerUserName");
            var phone = Tawsela.Data.Row.Str(r, "Phone");
            var gov = Tawsela.Data.Row.Str(r, "Governorate");
            var city = Tawsela.Data.Row.Str(r, "City");

            var line = (string.IsNullOrEmpty(owner) ? "—" : owner) + " · " +
                       (string.IsNullOrEmpty(phone) ? "—" : phone);
            var place = (string.IsNullOrEmpty(gov) ? "" : gov) +
                        (string.IsNullOrEmpty(city) ? "" : " - " + city);
            if (!string.IsNullOrEmpty(place)) line += "\n" + place;

            var row = Controls.InfoRow(string.IsNullOrEmpty(name) ? "مطعم" : name, line);
            row.UserInteractionEnabled = true;
            row.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                Ui.Choose(name, new[] { "اعتماد", "رفض" }, index =>
                {
                    if (index == 0)
                        Run(() => Api.ApproveRestaurant(id), "تم اعتماد المطعم");
                    else
                        Run(() => Api.RejectRestaurant(id), "تم رفض المطعم");
                });
            }));

            return row;
        }

        private void Run(Func<string> action, string success)
        {
            Ui.Run(() => { action(); },
                () =>
                {
                    Ui.Toast(success);
                    Reload();
                },
                ex => Ui.Toast(ex == null ? "تعذّر التنفيذ" : ex.Message));
        }
    }
}
