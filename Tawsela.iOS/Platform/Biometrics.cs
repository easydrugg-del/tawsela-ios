using System;
using Foundation;
using LocalAuthentication;
using UIKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل Xamarin.AndroidX.Biometric: LocalAuthentication (Face ID / Touch ID).
    /// يعمل بدون أي حزمة NuGet إضافية.
    /// </summary>
    public static class Biometrics
    {
        /// <summary>هل الجهاز يدعم البصمة/الوجه وهل مسجّل مستخدم؟</summary>
        public static bool IsAvailable(out string kind)
        {
            kind = "";
            try
            {
                using (var context = new LAContext())
                {
                    NSError error;
                    if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out error))
                        return false;

                    var type = context.BiometryType;
                    if (type == LABiometryType.FaceId) kind = "Face ID";
                    else if (type == LABiometryType.TouchId) kind = "Touch ID";
                    else if (type == LABiometryType.OpticId) kind = "Optic ID";
                    else kind = "البصمة";
                    return true;
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// طلب التحقق. على الـ main thread يُعرض حوار النظام.
        /// done(success, message) — message = سبب الفشل بالعربية.
        /// </summary>
        public static void Authenticate(string reason, Action<bool, string> done)
        {
            var result = done ?? ((ok, msg) => { });
            try
            {
                using (var context = new LAContext())
                {
                    NSError error;
                    if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out error))
                    {
                        Ui.BeginOnMain(() => result(false, CanNotUseMessage(error)));
                        return;
                    }

                    // fallbackTitle فارغ لأن التطبيق يستخدم PIN خاصًا به عند الفشل
                    context.LocalizedFallbackTitle = "";
                    context.EvaluatePolicy(
                        LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
                        string.IsNullOrEmpty(reason) ? "افتح تطبيق توصيلة" : reason,
                        (success, evalError) =>
                        {
                            var message = success ? "" : FailureMessage(evalError);
                            Ui.BeginOnMain(() => result(success, message));
                        });
                }
            }
            catch (Exception ex)
            {
                Ui.BeginOnMain(() => result(false, ex.Message));
            }
        }

        private static string CanNotUseMessage(NSError error)
        {
            if (error == null) return "البصمة/الوجه غير متاح على هذا الجهاز";
            if (error.Code == (long)LAStatus.BiometryNotEnrolled) return "لم يتم تسجيل بصمة أو وجه على الجهاز";
            if (error.Code == (long)LAStatus.BiometryNotAvailable) return "البصمة/الوجه غير متاح الآن";
            if (error.Code == (long)LAStatus.PasscodeNotSet) return "لا يوجد رمز دخول للجهاز";
            return "البصمة/الوجه غير متاح";
        }

        private static string FailureMessage(NSError error)
        {
            if (error == null) return "فشل التحقق";
            var code = error.Code;

            // ملاحظة: LAStatus فيه أسماء مرادفة بنفس القيمة الرقمية،
            // لذلك نستخدم if بدل switch لتفادي أخطاء التكرار.
            if (code == (long)LAStatus.UserCancel ||
                code == (long)LAStatus.SystemCancel ||
                code == (long)LAStatus.AppCancel ||
                code == (long)LAStatus.UserFallback)
                return "تم الإلغاء";

            if (code == (long)LAStatus.AuthenticationFailed)
                return "لم يتم التعرف على البصمة/الوجه";

            if (code == (long)LAStatus.BiometryLockout)
                return "محاولات كثيرة — استخدم الرقم السري للجهاز";

            if (code == (long)LAStatus.BiometryNotEnrolled)
                return "لم يتم تسجيل بصمة أو وجه على الجهاز";

            if (code == (long)LAStatus.BiometryNotAvailable)
                return "البصمة/الوجه غير متاح";

            if (code == (long)LAStatus.PasscodeNotSet)
                return "لا يوجد رمز دخول للجهاز";

            return "فشل التحقق";
        }
    }
}
