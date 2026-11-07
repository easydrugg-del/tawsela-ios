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

        /// <summary>شكل أندرويد: عنوان برتقالي أعلى الشاشة بدون شريط تنقل.</summary>
        protected virtual bool AndroidHeader
        {
            get { return false; }
        }

        public override bool ShowNavigationBar
        {
            get { return !AndroidHeader; }
        }

        /// <summary>صف إدارة بشكل أندرويد: أبيض بدون كروت، اسم برتقالي + تفاصيل رمادية + الحالة يسارًا.</summary>
        protected UIView AdminRow(string title, string subtitle, string status = null, UIColor statusColor = null)
        {
            var row = new UIView
            {
                BackgroundColor = Theme.White,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var t = MakeLabel(title ?? "", 15f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
            var s = MakeLabel(subtitle ?? "", 13f, Theme.TextSecondary, UIFontWeight.Regular, UITextAlignment.Right);
            s.Lines = 0;
            row.AddSubview(t);
            row.AddSubview(s);

            UILabel st = null;
            if (!string.IsNullOrEmpty(status))
            {
                st = MakeLabel(status, 12f, statusColor ?? Theme.Gray, UIFontWeight.Bold, UITextAlignment.Left);
                row.AddSubview(st);
            }

            var list = new List<NSLayoutConstraint>
            {
                NSLayoutConstraint.Create(t, NSLayoutAttribute.Top, NSLayoutRelation.Equal, row, NSLayoutAttribute.Top, 1, 12),
                NSLayoutConstraint.Create(s, NSLayoutAttribute.Top, NSLayoutRelation.Equal, t, NSLayoutAttribute.Bottom, 1, 2),
                NSLayoutConstraint.Create(s, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, row, NSLayoutAttribute.Bottom, 1, -12),
            };

            if (st != null)
            {
                list.Add(NSLayoutConstraint.Create(st, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, row, NSLayoutAttribute.CenterY, 1, 0));
                list.Add(NSLayoutConstraint.Create(st, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, row, NSLayoutAttribute.Leading, 1, 12));
                list.Add(NSLayoutConstraint.Create(t, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, st, NSLayoutAttribute.Trailing, 1, 10));
                list.Add(NSLayoutConstraint.Create(s, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, t, NSLayoutAttribute.Leading, 1, 0));
            }
            else
            {
                list.Add(NSLayoutConstraint.Create(t, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, row, NSLayoutAttribute.Leading, 1, 12));
                list.Add(NSLayoutConstraint.Create(s, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, row, NSLayoutAttribute.Leading, 1, 12));
            }

            list.Add(NSLayoutConstraint.Create(t, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, row, NSLayoutAttribute.Trailing, 1, -12));
            list.Add(NSLayoutConstraint.Create(s, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, row, NSLayoutAttribute.Trailing, 1, -12));

            row.AddConstraints(list.ToArray());
            return row;
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

            if (AndroidHeader)
            {
                // عنوان برتقالي أعلى الشاشة زي أندرويد + زر رجوع صغير
                var title = MakeLabel(Title ?? "", 20f, Theme.Primary, UIFontWeight.Bold, UITextAlignment.Right);
                View.AddSubview(title);
                View.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View.SafeAreaLayoutGuide, NSLayoutAttribute.Top, 1, 0),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 56),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1, -16),
                    NSLayoutConstraint.Create(title, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 56),
                });

                var back = new UIButton(UIButtonType.System) { TranslatesAutoresizingMaskIntoConstraints = false };
                back.SetTitle("‹", UIControlState.Normal);
                back.SetTitleColor(Theme.Primary, UIControlState.Normal);
                back.TitleLabel.Font = Theme.FontBold(30);
                back.TouchUpInside += (s, e) =>
                {
                    if (NavigationController != null) NavigationController.PopViewController(true);
                };
                View.AddSubview(back);
                View.AddConstraints(new[]
                {
                    NSLayoutConstraint.Create(back, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, title, NSLayoutAttribute.CenterY, 1, 0),
                    NSLayoutConstraint.Create(back, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, View, NSLayoutAttribute.Leading, 1, 8),
                    NSLayoutConstraint.Create(back, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
                    NSLayoutConstraint.Create(back, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1, 40),
                });

                List.TopInset = 56;
            }

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

        /// <summary>تحويل بطاقة InfoRow إلى صف أندرويد: أبيض بدون حدود، الاسم برتقالي والتفاصيل رمادية.</summary>
        private static void FlattenRow(UIView row)
        {
            row.BackgroundColor = Theme.White;
            row.Layer.CornerRadius = 0f;
            row.Layer.BorderWidth = 0f;
            row.Layer.MasksToBounds = false;

            var labels = new List<UILabel>();
            Collect(row, labels);

            if (labels.Count > 0)
            {
                labels[0].Font = Theme.FontBold(15);
                labels[0].TextColor = Theme.Primary;
            }
            if (labels.Count > 1)
            {
                labels[1].Font = Theme.Font(13);
                labels[1].TextColor = Theme.TextSecondary;
            }
        }

        private static void Collect(UIView view, List<UILabel> acc)
        {
            foreach (var sub in view.Subviews)
            {
                var l = sub as UILabel;
                if (l != null) acc.Add(l);
                else Collect(sub, acc);
            }
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
                                    if (row != null)
                                    {
                                        if (AndroidHeader) FlattenRow(row);
                                        rows.Add(row);
                                    }
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
