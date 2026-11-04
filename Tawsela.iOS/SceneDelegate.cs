using System;
using Foundation;
using Tawsela.Data;
using Tawsela.iOS.Platform;
using Tawsela.iOS.Screens;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// إنشاء النافذة والجذر — مطلوب مع UIApplicationSceneManifest (iOS 13+)
    /// وهو إلزامي عند البناء بـ SDK 26.
    /// </summary>
    [Register("SceneDelegate")]
    public class SceneDelegate : UIResponder, IUIWindowSceneDelegate
    {
        public UIWindow Window { get; set; }

        [Export("scene:willConnectToSession:options:")]
        public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
        {
            var windowScene = scene as UIWindowScene;
            if (windowScene == null) return;

            Window = new UIWindow(windowScene);

            var splash = new SplashViewController();
            var nav = new UINavigationController(splash);
            nav.SetNavigationBarHidden(true, false);
            nav.NavigationBar.TintColor = UIColor.White;

            Window.RootViewController = nav;
            Window.MakeKeyAndVisible();
        }

        [Export("sceneDidBecomeActive:")]
        public void DidBecomeActive(UIScene scene)
        {
            try
            {
                // إعادة قفل التطبيق إذا تجاوز مدة الخلفية (بديل LockActivity في onResume)
                Router.CheckAppLock();
            }
            catch { }
        }

        /// <summary>
        /// مع UIApplicationSceneManifest لا يُستدعى AppDelegate.DidEnterBackground،
        /// لذلك نحفظ وقت الخلفية هنا وإلا لن يُقفل التطبيق بعد المهلة أبدًا.
        /// </summary>
        [Export("sceneDidEnterBackground:")]
        public void DidEnterBackground(UIScene scene)
        {
            try { InAppNotifications.Stop(); } catch { }
            try { if (AppLock.IsActive()) AppLock.MarkBackground(); } catch { }
        }

        [Export("sceneWillEnterForeground:")]
        public void WillEnterForeground(UIScene scene)
        {
            try { InAppNotifications.Start(); } catch { }
        }
    }
}
