using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل ActionGetContent / ActionImageCapture + FileProvider في RegisterActivity:
    /// UIImagePickerController للمعرض أو الكاميرا، ثم تصغير الصورة وإرجاع Base64.
    /// لا حاجة لصلاحية تخزين، ولا لملفات مؤقتة ولا FileProvider.
    /// </summary>
    public sealed class ImagePicker : UIImagePickerControllerDelegate, IUINavigationControllerDelegate
    {
        private Action<string> _onBase64;
        private Action<UIImage> _onImage;
        private Action<string> _onError;
        private readonly bool _camera;

        /// <summary>الحد الأقصى لطول الصورة قبل الإرسال (يقلل حجم Base64 كثيرًا).</summary>
        public static nfloat MaxDimension = 1600f;

        /// <summary>جودة JPEG (0..1).</summary>
        public static nfloat JpegQuality = 0.8f;

        private ImagePicker(bool camera)
        {
            _camera = camera;
        }

        /// <summary>فتح المعرض واختيار صورة، مع إرجاع Base64 (بدون بادئة data:).</summary>
        public static void PickFromLibrary(UIViewController presenter, Action<string> onBase64, Action<string> onError)
        {
            Present(presenter, false, onBase64, null, onError);
        }

        /// <summary>التقاط صورة بالكاميرا.</summary>
        public static void TakePhoto(UIViewController presenter, Action<string> onBase64, Action<string> onError)
        {
            Present(presenter, true, onBase64, null, onError);
        }

        /// <summary>اختيار صورة وإرجاع UIImage نفسها (لعرض معاينة).</summary>
        public static void PickImage(UIViewController presenter, bool camera, Action<UIImage> onImage, Action<string> onError)
        {
            Present(presenter, camera, null, onImage, onError);
        }

        private static void Present(UIViewController presenter, bool camera,
            Action<string> onBase64, Action<UIImage> onImage, Action<string> onError)
        {
            if (presenter == null)
            {
                if (onError != null) onError("لا توجد شاشة لعرض المعرض");
                return;
            }

            var source = camera ? UIImagePickerControllerSourceType.Camera : UIImagePickerControllerSourceType.PhotoLibrary;

            if (!UIImagePickerController.IsSourceTypeAvailable(source))
            {
                var msg = camera ? "الكاميرا غير متوفرة على هذا الجهاز" : "مكتبة الصور غير متوفرة";
                if (onError != null) onError(msg);
                return;
            }

            var picker = new ImagePicker(camera)
            {
                _onBase64 = onBase64,
                _onImage = onImage,
                _onError = onError
            };

            var controller = new UIImagePickerController
            {
                SourceType = source,
                Delegate = picker,
                AllowsEditing = false
            };

            try
            {
                presenter.PresentViewController(controller, true, null);
            }
            catch (Exception ex)
            {
                if (onError != null) onError("تعذّر فتح الصور: " + ex.Message);
            }
        }

        [Export("imagePickerController:didFinishPickingMediaWithInfo:")]
        public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            try
            {
                picker.DismissViewController(true, null);

                UIImage image = null;
                if (info != null)
                {
                    if (info.ContainsKey(new NSString("UIImagePickerControllerEditedImage")))
                        image = info.ObjectForKey(new NSString("UIImagePickerControllerEditedImage")) as UIImage;
                    if (image == null && info.ContainsKey(new NSString("UIImagePickerControllerOriginalImage")))
                        image = info.ObjectForKey(new NSString("UIImagePickerControllerOriginalImage")) as UIImage;
                }

                if (image == null)
                {
                    if (_onError != null) _onError("لم يتم اختيار صورة");
                    return;
                }

                if (_onImage != null) _onImage(image);
                if (_onBase64 != null) _onBase64(ToBase64(image));
            }
            catch (Exception ex)
            {
                if (_onError != null) _onError(ex.Message);
            }
        }

        [Export("imagePickerControllerDidCancel:")]
        public override void Canceled(UIImagePickerController picker)
        {
            try { picker.DismissViewController(true, null); } catch { }
        }

        /// <summary>تصغير الصورة (يحافظ على النسبة) ثم تحويلها لـ JPEG ثم Base64.</summary>
        public static string ToBase64(UIImage image)
        {
            if (image == null) return "";
            try
            {
                var scaled = Scale(image, MaxDimension);
                using (var data = scaled.AsJPEG(JpegQuality))
                {
                    if (data == null) return "";
                    return Convert.ToBase64String(data.ToArray());
                }
            }
            catch { return ""; }
        }

        public static UIImage Scale(UIImage image, nfloat maxDimension)
        {
            if (image == null) return null;
            var w = image.Size.Width;
            var h = image.Size.Height;
            if (w <= maxDimension && h <= maxDimension) return image;

            var ratio = Math.Min(maxDimension / w, maxDimension / h);
            var size = new CGSize(Math.Max(1, w * ratio), Math.Max(1, h * ratio));

            var renderer = new UIGraphicsImageRenderer(size);
            return renderer.CreateImage(ctx => image.Draw(new CGRect(CGPoint.Empty, size)));
        }
    }
}
