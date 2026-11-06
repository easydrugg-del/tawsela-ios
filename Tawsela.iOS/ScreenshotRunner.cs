using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using Tawsela.Data;
using Tawsela.iOS.Screens;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// وضع التقاط الشاشات (للاختبار فقط): يُشغَّل بوسيطة --shots
    /// يفتح الشاشات واحدة واحدة ويحفظ صورًا PNG في Documents/shots
    /// ثم يكتب done.txt — بلا حاجة إلى tunnel أو نقر خارجي.
    /// </summary>
    public static class ScreenshotRunner
    {
        public static bool Enabled
        {
            get
            {
                try
                {
                    foreach (var a in NSProcessInfo.ProcessInfo.Arguments)
                        if (a == "--shots") return true;
                }
                catch { }
                return false;
            }
        }

        public static string Dir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "shots");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Start(UIWindow window)
        {
            window.RootViewController = new ScreenshotHostViewController(window);
        }
    }

    public class ScreenshotHostViewController : UIViewController
    {
        private readonly UIWindow _window;
        private bool _started;

        public ScreenshotHostViewController(UIWindow window)
        {
            _window = window;
            View.BackgroundColor = UIColor.White;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (_started) return;
            _started = true;
            _ = RunAll();
        }

        private async Task RunAll()
        {
            var dir = ScreenshotRunner.Dir;
            var ok = new List<string>();

            try
            {
                // شاشات لا تحتاج تسجيل دخول
                await Shot(dir, ok, "01-intro", () => new IntroViewController());
                await Shot(dir, ok, "02-login", () => new LoginViewController());
                await Shot(dir, ok, "03-register", () => new RegisterViewController());

                // ===== عميل =====
                await Login("customer1", "123456");
                await Shot(dir, ok, "10-home-customer", () => new MainViewController());
                await Shot(dir, ok, "11-new-order", () => new NewOrderViewController());
                await Shot(dir, ok, "12-wallet", () => new WalletViewController());
                await Shot(dir, ok, "13-coupons", () => new CouponsViewController());
                await Shot(dir, ok, "14-referral", () => new ReferralViewController());
                await Shot(dir, ok, "15-loyalty", () => new LoyaltyViewController());
                await Shot(dir, ok, "16-challenges", () => new ChallengesViewController());
                await Shot(dir, ok, "17-subscription", () => new SubscriptionViewController());
                await Shot(dir, ok, "18-support", () => new ComplaintViewController());
                await Shot(dir, ok, "19-notifications", () => new NotificationsViewController());
                await Shot(dir, ok, "20-profile", () => new ProfileViewController());
                await Shot(dir, ok, "21-settings", () => new SettingsViewController());
                await Shot(dir, ok, "22-topup-history", () => new TopUpHistoryViewController());
                await Shot(dir, ok, "23-customer-stats", () => new CustomerStatsViewController());
                await Shot(dir, ok, "24-my-orders",
                    () => new OrdersViewController("customer", "", "طلباتي"));

                // ===== سائق =====
                await Login("playdrv094621", "Tw!c5ecdb89e37f");
                await Shot(dir, ok, "30-home-driver", () => new MainViewController());
                await Shot(dir, ok, "31-driver-stats", () => new DriverStatsViewController());
                await Shot(dir, ok, "32-driver-shifts", () => new DriverShiftsViewController());
                await Shot(dir, ok, "33-driver-docs", () => new DriverDocsViewController());
                await Shot(dir, ok, "34-driver-statement", () => new DriverStatementViewController());
                await Shot(dir, ok, "35-driver-orders",
                    () => new OrdersViewController("driver", "", "توصيلاتي"));
                await Shot(dir, ok, "36-restaurant-requests", () => new RestaurantRequestsViewController());

                // ===== مطعم =====
                await Login("s", "12345678");
                await Shot(dir, ok, "40-restaurant-home", () => new RestaurantMainViewController());
                await Shot(dir, ok, "41-restaurant-profile", () => new RestaurantProfileViewController());
                await Shot(dir, ok, "42-restaurant-new",
                    () => new RestaurantNewDeliveryViewController(new List<Dictionary<string, object>>()));
                await Shot(dir, ok, "43-restaurant-requests", () => new RestaurantRequestsViewController());
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(dir, "error.txt"), ex.ToString());
            }

            SafeWrite(Path.Combine(dir, "done.txt"), string.Join("\n", ok));
        }

        private async Task Login(string user, string pass)
        {
            try
            {
                await Task.Run(() => Api.Login(user, pass));
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(ScreenshotRunner.Dir, "login-errors.txt"),
                    user + ": " + ex.Message + "\n");
            }

            await Task.Delay(600);
        }

        private async Task Shot(string dir, List<string> ok, string name, Func<UIViewController> make)
        {
            try
            {
                var vc = make();
                var nav = new UINavigationController(vc);
                nav.NavigationBar.TintColor = UIColor.White;
                _window.RootViewController = nav;
                _window.MakeKeyAndVisible();

                await Task.Delay(3200);

                var image = Snapshot(_window);
                using (var data = image.AsPNG())
                {
                    data.Save(Path.Combine(dir, name + ".png"), true);
                    ok.Add(name);
                }
            }
            catch (Exception ex)
            {
                SafeWrite(Path.Combine(dir, "shot-errors.txt"), name + ": " + ex.Message + "\n");
            }
        }

        private static UIImage Snapshot(UIView view)
        {
            var bounds = view.Bounds;
            var format = new UIGraphicsImageRendererFormat { Scale = 3f, Opaque = true };
            var renderer = new UIGraphicsImageRenderer(bounds.Size, format);
            return renderer.CreateImage(ctx => view.DrawViewHierarchy(bounds, true));
        }

        private static void SafeWrite(string path, string text)
        {
            try { File.AppendAllText(path, text + "\n"); } catch { }
        }
    }
}
