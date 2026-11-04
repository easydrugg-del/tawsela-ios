using System;
using Foundation;
using Tawsela.Data;
using Tawsela.iOS.Screens;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// التنقل المركزي — بديل StartActivity/Fini sh في أندرويد.
    /// يحدد الشاشة الجذرية حسب الدور، ويدير قفل التطبيق.
    /// </summary>
    public static class Router
    {
        private static bool _lockShowing;

        public static void SetRoot(UIViewController controller, bool animated = true)
        {
            if (controller == null) return;
            var window = Ui.KeyWindow();
            if (window == null) return;

            var nav = new UINavigationController(controller);
            var baseVc = controller as BaseViewController;
            nav.SetNavigationBarHidden(baseVc != null && !baseVc.ShowNavigationBar, false);
            nav.NavigationBar.TintColor = UIColor.White;

            if (!animated)
            {
                window.RootViewController = nav;
                return;
            }

            UIView.Transition(window, 0.3, UIViewAnimationOptions.TransitionCrossDissolve,
                () => { window.RootViewController = nav; }, null);
        }

        /// <summary>الشاشة التالية بعد الإقلاع/الدخول حسب الدور (بديل MainActivity / RestaurantMainActivity).</summary>
        public static void GoHome()
        {
            if (Session.UserId <= 0)
            {
                SetRoot(new LoginViewController());
                return;
            }

            if (AppLock.IsActive())
            {
                ShowLock(true);
                return;
            }

            if (Session.IsRestaurant)
            {
                SetRoot(new RestaurantMainViewController());
                return;
            }

            SetRoot(new MainViewController());
        }

        /// <summary>الشاشة التالية بعد السبلاش (بديل SplashActivity.GoNext).</summary>
        public static void GoAfterSplash(bool restored, bool introSeen)
        {
            if (restored) { GoHome(); return; }
            if (!introSeen) { SetRoot(new IntroViewController()); return; }
            SetRoot(new LoginViewController());
        }

        public static void ShowLogin()
        {
            SetRoot(new LoginViewController());
        }

        /// <summary>قفل التطبيق — يفتح شاشة PIN فوق أي شاشة (بديل LockActivity).</summary>
        public static void ShowLock(bool forward)
        {
            if (_lockShowing) return;
            try
            {
                var top = Ui.TopViewController();
                if (top == null) return;
                if (top is LockViewController) return;

                _lockShowing = true;
                var lockVc = new LockViewController(forward);
                lockVc.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
                top.PresentViewController(lockVc, true, null);
            }
            catch { _lockShowing = false; }
        }

        public static void LockDismissed()
        {
            _lockShowing = false;
        }

        /// <summary>يُستدعى عند رجوع التطبيق للمقدمة: يطلب PIN إن تجاوزت مدة الخلفية.</summary>
        public static void CheckAppLock()
        {
            try
            {
                if (!AppLock.IsActive()) return;
                if (Session.UserId <= 0) return;
                if (!AppLock.IsTimedOut()) return;
                ShowLock(false);
            }
            catch { }
        }

        /// <summary>توجيه لمس الإشعار إلى الشاشة المناسبة (بديل PendingIntent في FirebaseService).</summary>
        public static void OpenFromPush(UIViewController top, int orderId)
        {
            if (top == null) return;

            if (orderId > 0)
            {
                if (Session.IsDriver)
                    top.NavigationController?.PushViewController(new OrderDetailsViewController(orderId), true);
                else
                    top.NavigationController?.PushViewController(new TrackOrderViewController(orderId), true);
                return;
            }

            top.NavigationController?.PushViewController(new NotificationsViewController(), true);
        }

        public static void RegisterPushRouting()
        {
            Platform.PushService.TapHandler = orderId =>
            {
                try
                {
                    var top = Ui.TopViewController();
                    if (top != null) OpenFromPush(top, orderId);
                }
                catch { }
            };
        }
    }
}
