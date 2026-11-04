using System;
using AVFoundation;
using Foundation;
using Speech;
using UIKit;

namespace Tawsela.iOS.Platform
{
    /// <summary>
    /// بديل android.speech.SpeechRecognizer في NewOrderActivity:
    /// تعرّف صوتي عربي (ar-EG) عبر Speech + AVAudioEngine بدون أي حزمة خارجية.
    /// النتيجة جزئية ومستمرة حتى إيقاف التسجيل أو انتهاء المهلة.
    /// </summary>
    public sealed class VoiceInput : IDisposable
    {
        private SFSpeechRecognizer _recognizer;
        private SFSpeechAudioBufferRecognitionRequest _request;
        private SFSpeechRecognitionTask _task;
        private AVAudioEngine _engine;
        private NSTimer _timeout;

        private Action<string> _onText;
        private Action<string> _onError;
        private bool _running;

        public bool IsRunning { get { return _running; } }

        /// <summary>هل التعرّف الصوتي متاح على هذا الجهاز؟</summary>
        public static bool IsAvailable
        {
            get
            {
                try
                {
                    var recognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier("ar-EG"));
                    return recognizer != null && recognizer.Available;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// يبدأ التسجيل. onText يُستدعى بالنتيجة الجزئية المحدَّثة، وonError عند الفشل.
        /// </summary>
        public void Start(UIViewController presenter, Action<string> onText, Action<string> onError)
        {
            _onText = onText;
            _onError = onError;

            SFSpeechRecognizer.RequestAuthorization(status =>
            {
                Ui.BeginOnMain(() =>
                {
                    if (status != SFSpeechRecognizerAuthorizationStatus.Authorized)
                    {
                        Fail("لم يتم السماح بالتعرّف على الكلام — فعّله من إعدادات النظام");
                        return;
                    }

                    AVAudioSession.SharedInstance().RequestRecordPermission(granted =>
                    {
                        Ui.BeginOnMain(() =>
                        {
                            if (!granted)
                            {
                                Fail("لم يتم السماح بالميكروفون — فعّله من إعدادات النظام");
                                return;
                            }
                            StartRecording();
                        });
                    });
                });
            });
        }

        private void StartRecording()
        {
            try
            {
                StopInternal();

                _recognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier("ar-EG"));
                if (_recognizer == null || !_recognizer.Available)
                {
                    Fail("التعرّف الصوتي العربي غير متاح على هذا الجهاز");
                    return;
                }

                var session = AVAudioSession.SharedInstance();
                NSError sessionError;
                session.SetCategory(AVAudioSessionCategory.Record, AVAudioSessionCategoryOptions.DefaultToSpeaker, out sessionError);
                session.SetActive(true, out sessionError);

                _request = new SFSpeechAudioBufferRecognitionRequest
                {
                    ShouldReportPartialResults = true
                };

                _engine = new AVAudioEngine();
                var node = _engine.InputNode;
                var format = node.GetBusOutputFormat(0);

                node.InstallTapOnBus(0, 1024, format, (buffer, when) =>
                {
                    if (_request != null) _request.Append(buffer);
                });

                _engine.Prepare();
                NSError engineError;
                if (!_engine.StartAndReturnError(out engineError))
                {
                    Fail("تعذّر تشغيل الميكروفون");
                    return;
                }

                _running = true;

                _task = _recognizer.GetRecognitionTask(_request, (result, error) =>
                {
                    if (result != null)
                    {
                        var text = result.BestTranscription == null
                            ? ""
                            : (result.BestTranscription.FormattedString ?? "");

                        if (!string.IsNullOrWhiteSpace(text) && _onText != null)
                            Ui.BeginOnMain(() => _onText(text.Trim()));
                    }

                    if (error != null)
                    {
                        var msg = error.LocalizedDescription;
                        Ui.BeginOnMain(() => Fail("تعذّر التعرّف: " + msg));
                    }
                });

                // مهلة تلقائية 12 ثانية (مثل SpeechRecognizer في أندرويد)
                _timeout = NSTimer.CreateScheduledTimer(12.0, _ => Stop());
            }
            catch (Exception ex)
            {
                Fail("تعذّر بدء التعرّف الصوتي: " + ex.Message);
            }
        }

        /// <summary>إيقاف التسجيل وإنهاء الطلب (تُستدعى أيضًا عند إغلاق الشاشة).</summary>
        public void Stop()
        {
            if (!_running) return;
            StopInternal();
            _running = false;
        }

        private void StopInternal()
        {
            try
            {
                if (_timeout != null) { _timeout.Invalidate(); _timeout.Dispose(); _timeout = null; }
            }
            catch { }

            try
            {
                if (_engine != null)
                {
                    if (_engine.Running) _engine.Stop();
                    try { _engine.InputNode.RemoveTapOnBus(0); } catch { }
                    _engine.Dispose();
                    _engine = null;
                }
            }
            catch { }

            try { if (_request != null) { _request.EndAudio(); _request.Dispose(); _request = null; } } catch { }
            try { if (_task != null) { _task.Cancel(); _task.Dispose(); _task = null; } } catch { }

            try
            {
                var session = AVAudioSession.SharedInstance();
                session.SetActive(false, out _);
            }
            catch { }

            _running = false;
        }

        private void Fail(string message)
        {
            StopInternal();
            if (_onError != null) _onError(message);
        }

        public void Dispose()
        {
            StopInternal();
        }
    }
}
