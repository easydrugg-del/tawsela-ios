using System;
using System.Collections.Generic;
using CoreGraphics;
using Tawsela.Core.Platform;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// تخطيط قائمة قياسي: ScrollView + StackView + سحب للتحديث + رسالة فراغ.
    /// يقابل (ScrollView + LinearLayout) المتكرر في ملفات AXML.
    /// </summary>
    public sealed class ListLayout
    {
        public readonly UIScrollView Scroll;
        public readonly UIStackView Stack;
        public readonly UIRefreshControl Refresh;
        private readonly UILabel _empty;
        private readonly UIView _host;
        private NSLayoutConstraint _scrollTop;

        public ListLayout(UIView host, float spacing = 10f, float padding = 14f)
        {
            _host = host;

            Scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AlwaysBounceVertical = true
            };
            host.AddSubview(Scroll);
            _scrollTop = NSLayoutConstraint.Create(Scroll, NSLayoutAttribute.Top, NSLayoutRelation.Equal, host.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0);
            host.AddConstraints(new[]
            {
                _scrollTop,
                NSLayoutConstraint.Create(Scroll, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, host.SafeAreaLayoutGuide, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(Scroll, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, host, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(Scroll, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, host, NSLayoutAttribute.Trailing, 1, 0),
            });

            Refresh = new UIRefreshControl();
            Scroll.RefreshControl = Refresh;

            Stack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = (nfloat)spacing,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            Scroll.AddSubview(Stack);
            Scroll.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(Stack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, Scroll, NSLayoutAttribute.Top, 1, (nfloat)padding),
                NSLayoutConstraint.Create(Stack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, Scroll, NSLayoutAttribute.Bottom, 1, -(nfloat)padding),
                NSLayoutConstraint.Create(Stack, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, Scroll, NSLayoutAttribute.Leading, 1, (nfloat)padding),
                NSLayoutConstraint.Create(Stack, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, Scroll, NSLayoutAttribute.Trailing, 1, -(nfloat)padding),
                NSLayoutConstraint.Create(Stack, NSLayoutAttribute.Width, NSLayoutRelation.Equal, Scroll, NSLayoutAttribute.Width, 1, -2 * (nfloat)padding),
            });

            _empty = new UILabel
            {
                Text = "",
                Font = Theme.Font(15),
                TextColor = Theme.TextSecondary,
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            host.AddSubview(_empty);
            host.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(_empty, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, host.SafeAreaLayoutGuide, NSLayoutAttribute.CenterY, 1, 0),
                NSLayoutConstraint.Create(_empty, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, host.SafeAreaLayoutGuide, NSLayoutAttribute.Leading, 1, 24),
                NSLayoutConstraint.Create(_empty, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, host.SafeAreaLayoutGuide, NSLayoutAttribute.Trailing, 1, -24),
            });
        }

        /// <summary>سطر توضيحي يظهر أعلى القائمة (بديل TextView الفرعي في AXML).</summary>
        public string Subtitle { get; set; }

        /// <summary>إزاحة أعلى القائمة لترك مساحة شريط بحث ثابت فوقها.</summary>
        public nfloat TopInset
        {
            set { if (_scrollTop != null) _scrollTop.Constant = value; }
        }

        /// <summary>استبدال الصفوف المعروضة (على الخيط الرئيسي).</summary>
        public void SetRows(IList<UIView> rows, string emptyText)
        {
            for (int i = Stack.ArrangedSubviews.Length - 1; i >= 0; i--)
            {
                var v = Stack.ArrangedSubviews[i];
                Stack.RemoveArrangedSubview(v);
                v.RemoveFromSuperview();
            }

            if (!string.IsNullOrEmpty(Subtitle))
            {
                var sub = new UILabel
                {
                    Text = Subtitle,
                    Font = Theme.Font(13),
                    TextColor = Theme.TextSecondary,
                    TextAlignment = UITextAlignment.Right,
                    Lines = 0,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                Stack.AddArrangedSubview(sub);
            }

            if (rows == null || rows.Count == 0)
            {
                _empty.Text = emptyText ?? "لا توجد بيانات";
                _empty.Hidden = false;
                return;
            }

            _empty.Hidden = true;
            foreach (var r in rows) Stack.AddArrangedSubview(r);
        }

        public void EndRefreshing()
        {
            try { Refresh.EndRefreshing(); } catch { }
        }
    }

    /// <summary>
    /// شاشة قائمة عامة: الجلب في الخلفية والبناء على الخيط الرئيسي
    /// (لأن إنشاء UIView خارج الخيط الرئيسي غير مسموح في iOS).
    /// </summary>
    public abstract class ListViewController<T> : BaseViewController where T : class
    {
        protected ListLayout List;

        protected ListViewController(string title) : base(title) { }

        /// <summary>نص يظهر عند عدم وجود عناصر.</summary>
        protected virtual string EmptyText
        {
            get { return "لا توجد بيانات"; }
        }

        /// <summary>سطر توضيحي أعلى القائمة (اختياري).</summary>
        protected virtual string Subtitle
        {
            get { return ""; }
        }

        /// <summary>جلب البيانات — يُنفَّذ على خيط خلفي.</summary>
        protected abstract List<T> Fetch();

        /// <summary>بناء صف واحد — يُنفَّذ على الخيط الرئيسي.</summary>
        protected abstract UIView BuildRow(T item);

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            List = new ListLayout(View);
            List.Refresh.ValueChanged += (s, e) => Reload();
            Reload();
        }

        private bool _firstAppear = true;

        /// <summary>
        /// تحديث تلقائي عند العودة للشاشة — مثل OnResume → Load في أندرويد
        /// (يمنع بقاء القائمة قديمة بعد التعديل من شاشة أخرى).
        /// </summary>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (_firstAppear)
            {
                _firstAppear = false;
                return;
            }

            Reload();
        }

        /// <summary>
        /// شريط بحث ثابت أعلى القائمة (بديل EditText أعلى RecyclerView في أندرويد).
        /// يعيد الحقل؛ التغيير يستدعي Reload تلقائيًا.
        /// </summary>
        protected UITextField AddSearchBar(string placeholder)
        {
            const float height = 60f;

            var header = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(header);
            View.AddConstraints(new[]
            {
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 8),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 16),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -16),
                NSLayoutConstraint.Create(header, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, height),
            });

            var field = Controls.TextField(placeholder);
            field.EditingChanged += (s, e) => Reload();
            header.AddArrangedSubview(field);

            List.TopInset = height + 8;
            return field;
        }

        protected void Reload()
        {            Ui.Run(
                () =>
                {
                    var data = Fetch();
                    Ui.BeginOnMain(() =>
                    {
                        var rows = new List<UIView>();
                        if (data != null)
                            foreach (var item in data)
                            {
                                if (item == null) continue;
                                try
                                {
                                    var row = BuildRow(item);
                                    if (row != null) rows.Add(row);
                                }
                                catch (Exception ex)
                                {
                                    AppServices.Log("TawselaList", ex.Message);
                                }
                            }

                        List.Subtitle = Subtitle;
                        List.SetRows(rows, EmptyText);
                        List.EndRefreshing();
                    });
                },
                null,
                ex =>
                {
                    Ui.BeginOnMain(() =>
                    {
                        List.SetRows(null, "تعذّر التحميل: " + ex.Message);
                        List.EndRefreshing();
                    });
                });
        }
    }
}
