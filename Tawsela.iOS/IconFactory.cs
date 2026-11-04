using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Tawsela.iOS
{
    /// <summary>
    /// يرسم أيقونات أندرويد Vector Drawable (Resources/drawable/ic_*.xml) كما هي
    /// على iOS باستخدام CoreGraphics — فلا فرق بصريًا بين المنصتين.
    ///
    /// يدعم أوامر SVG المستخدمة في أيقونات Material:
    /// M m L l H h V v C c S s Q q T t A a Z z
    /// </summary>
    public static class IconFactory
    {
        private sealed class IconDef
        {
            public float ViewportW = 24f;
            public float ViewportH = 24f;
            public List<KeyValuePair<CGPath, UIColor>> Paths =
                new List<KeyValuePair<CGPath, UIColor>>();
            /// <summary>مصدر PNG بديل (مثل tuktuk.png) عندما لا توجد أيقونة Vector.</summary>
            public UIImage PngFallback;
        }

        private static readonly Dictionary<string, IconDef> Cache =
            new Dictionary<string, IconDef>(StringComparer.OrdinalIgnoreCase);

        private static readonly object Sync = new object();

        /// <summary>هل الأيقونة موجودة داخل الحزمة؟</summary>
        public static bool Exists(string name)
        {
            return NSBundle.MainBundle.PathForResource(name, "xml", "icons") != null
                || NSBundle.MainBundle.PathForResource("icons/" + name, "xml") != null;
        }

        /// <summary>أيقونة بحجم معين. tint = null يعني استخدام ألوان المصدر (أبيض غالبًا).</summary>
        public static UIImage Render(string name, nfloat size, UIColor tint = null)
        {
            return Render(name, new CGSize(size, size), tint);
        }

        public static UIImage Render(string name, CGSize size, UIColor tint = null)
        {
            var def = Load(name);
            if (def == null) return null;

            var renderer = new UIGraphicsImageRenderer(size);
            return renderer.CreateImage(context =>
            {
                var cg = context.CGContext;
                cg.SetShouldAntialias(true);
                cg.ScaleCTM(size.Width / def.ViewportW, size.Height / def.ViewportH);
                foreach (var pair in def.Paths)
                {
                    var color = tint ?? pair.Value;
                    if (color == null) continue;
                    cg.AddPath(pair.Key);
                    cg.SetFillColor(color.CGColor);
                    cg.FillPath();
                }
            });
        }

        private static IconDef Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            lock (Sync)
            {
                IconDef cached;
                if (Cache.TryGetValue(name, out cached)) return cached;

                var path = NSBundle.MainBundle.PathForResource(name, "xml", "icons")
                           ?? NSBundle.MainBundle.PathForResource("icons/" + name, "xml");
                if (path == null)
                {
                    // بعض الأيقونات قد تكون PNG (tuktuk.png مثلًا)
                    var png = UIImage.FromBundle(name);
                    if (png != null)
                    {
                        var def = new IconDef { ViewportW = (float)png.Size.Width, ViewportH = (float)png.Size.Height };
                        var p = new CGPath();
                        p.AddRect(new CGRect(0, 0, png.Size.Width, png.Size.Height));
                        def.Paths.Add(new KeyValuePair<CGPath, UIColor>(p, null));
                        // نستخدم الصورة الأصلية مباشرة بدل مسار
                        def.PngFallback = png;
                        Cache[name] = def;
                        return def;
                    }
                    Cache[name] = null;
                    return null;
                }

                XDocument doc;
                try { doc = XDocument.Load(path); }
                catch { Cache[name] = null; return null; }

                var result = new IconDef();
                foreach (var el in doc.Descendants())
                {
                    var local = el.Name.LocalName;
                    if (local != "vector" && local != "path") continue;

                    if (local == "vector")
                    {
                        var vw = el.Attribute(XName.Get("viewportWidth", "http://schemas.android.com/apk/res/android"));
                        var vh = el.Attribute(XName.Get("viewportHeight", "http://schemas.android.com/apk/res/android"));
                        float f;
                        if (vw != null && float.TryParse(vw.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) result.ViewportW = f;
                        if (vh != null && float.TryParse(vh.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) result.ViewportH = f;
                    }
                    else
                    {
                        var dataAttr = el.Attribute(XName.Get("pathData", "http://schemas.android.com/apk/res/android"))
                                       ?? el.Attribute("pathData");
                        if (dataAttr == null) continue;

                        var fillAttr = el.Attribute(XName.Get("fillColor", "http://schemas.android.com/apk/res/android"))
                                       ?? el.Attribute("fillColor");

                        var cgPath = new CGPath();
                        try { PathParser.Build(dataAttr.Value, cgPath); }
                        catch { continue; }

                        result.Paths.Add(new KeyValuePair<CGPath, UIColor>(
                            cgPath, ParseAndroidColor(fillAttr == null ? null : fillAttr.Value)));
                    }
                }

                Cache[name] = result;
                return result;
            }
        }

        /// <summary>PNG fallback (مثل tuktuk.png) — غير مستخدم في الرسم المساري.</summary>
        private static UIImage PngOf(string name)
        {
            var def = Load(name);
            return def == null ? null : def.PngFallback;
        }

        /// <summary>أيقونة جاهزة: Vector إن وُجد، وإلا PNG من الحزمة.</summary>
        public static UIImage Image(string name, nfloat size, UIColor tint = null)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var def = Load(name);
            if (def == null) return null;
            if (def.PngFallback != null) return def.PngFallback;
            return Render(name, size, tint);
        }

        private static UIColor ParseAndroidColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Trim();
            if (!value.StartsWith("#", StringComparison.Ordinal)) return null;
            var hex = value.Substring(1);
            try
            {
                switch (hex.Length)
                {
                    case 3:
                        {
                            var r = Convert.ToByte(new string(hex[0], 2), 16);
                            var g = Convert.ToByte(new string(hex[1], 2), 16);
                            var b = Convert.ToByte(new string(hex[2], 2), 16);
                            return UIColor.FromRGBA(r, g, b, (byte)255);
                        }
                    case 4:
                        {
                            var a = Convert.ToByte(new string(hex[0], 2), 16);
                            var r = Convert.ToByte(new string(hex[1], 2), 16);
                            var g = Convert.ToByte(new string(hex[2], 2), 16);
                            var b = Convert.ToByte(new string(hex[3], 2), 16);
                            return UIColor.FromRGBA(r, g, b, a);
                        }
                    case 6:
                        {
                            var r = Convert.ToByte(hex.Substring(0, 2), 16);
                            var g = Convert.ToByte(hex.Substring(2, 2), 16);
                            var b = Convert.ToByte(hex.Substring(4, 2), 16);
                            return UIColor.FromRGBA(r, g, b, (byte)255);
                        }
                    case 8:
                        {
                            var a = Convert.ToByte(hex.Substring(0, 2), 16);
                            var r = Convert.ToByte(hex.Substring(2, 2), 16);
                            var g = Convert.ToByte(hex.Substring(4, 2), 16);
                            var b = Convert.ToByte(hex.Substring(6, 2), 16);
                            return UIColor.FromRGBA(r, g, b, a);
                        }
                }
            }
            catch { }
            return null;
        }
    }

    /// <summary>مفسّر pathData الخاص بـ SVG / Android VectorDrawable.</summary>
    internal static class PathParser
    {
        public static void Build(string data, CGPath path)
        {
            if (string.IsNullOrEmpty(data)) return;
            var t = new Tokenizer(data);
            char cmd = '\0';
            double cx = 0, cy = 0;      // النقطة الحالية
            double sx = 0, sy = 0;      // بداية المسار الفرعي
            double lastCtrlX = 0, lastCtrlY = 0;
            char lastCmd = '\0';

            while (!t.End)
            {
                if (t.PeekIsCommand())
                {
                    cmd = t.ReadCommand();
                }
                else
                {
                    if (cmd == '\0') break;

                    // تكرار ضمني: بعد M/m يُعامَل الزوج التالي كـ L/l
                    if (cmd == 'M') cmd = 'L';
                    else if (cmd == 'm') cmd = 'l';
                }

                bool rel = char.IsLower(cmd);
                char up = char.ToUpperInvariant(cmd);

                switch (up)
                {
                    case 'M':
                        {
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            if (rel) { x += cx; y += cy; }
                            path.MoveToPoint(new CGPoint(x, y));
                            cx = x; cy = y; sx = x; sy = y;
                            break;
                        }
                    case 'L':
                        {
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            if (rel) { x += cx; y += cy; }
                            path.AddLineToPoint(new CGPoint(x, y));
                            cx = x; cy = y;
                            break;
                        }
                    case 'H':
                        {
                            double x = t.ReadNumber();
                            if (rel) x += cx;
                            path.AddLineToPoint(new CGPoint(x, cy));
                            cx = x;
                            break;
                        }
                    case 'V':
                        {
                            double y = t.ReadNumber();
                            if (rel) y += cy;
                            path.AddLineToPoint(new CGPoint(cx, y));
                            cy = y;
                            break;
                        }
                    case 'C':
                        {
                            double x1 = t.ReadNumber(), y1 = t.ReadNumber();
                            double x2 = t.ReadNumber(), y2 = t.ReadNumber();
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            if (rel)
                            {
                                x1 += cx; y1 += cy; x2 += cx; y2 += cy; x += cx; y += cy;
                            }
                            path.AddCurveToPoint(new CGPoint(x1, y1), new CGPoint(x2, y2), new CGPoint(x, y));
                            lastCtrlX = x2; lastCtrlY = y2;
                            cx = x; cy = y;
                            break;
                        }
                    case 'S':
                        {
                            double x2 = t.ReadNumber(), y2 = t.ReadNumber();
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            double x1, y1;
                            if (lastCmd == 'C' || lastCmd == 'c' || lastCmd == 'S' || lastCmd == 's')
                            {
                                x1 = 2 * cx - lastCtrlX; y1 = 2 * cy - lastCtrlY;
                            }
                            else { x1 = cx; y1 = cy; }
                            if (rel)
                            {
                                x2 += cx; y2 += cy; x += cx; y += cy;
                            }
                            path.AddCurveToPoint(new CGPoint(x1, y1), new CGPoint(x2, y2), new CGPoint(x, y));
                            lastCtrlX = x2; lastCtrlY = y2;
                            cx = x; cy = y;
                            break;
                        }
                    case 'Q':
                        {
                            double x1 = t.ReadNumber(), y1 = t.ReadNumber();
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            if (rel)
                            {
                                x1 += cx; y1 += cy; x += cx; y += cy;
                            }
                            path.AddQuadCurveToPoint((nfloat)x1, (nfloat)y1, (nfloat)x, (nfloat)y);
                            lastCtrlX = x1; lastCtrlY = y1;
                            cx = x; cy = y;
                            break;
                        }
                    case 'T':
                        {
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            double x1, y1;
                            if (lastCmd == 'Q' || lastCmd == 'q' || lastCmd == 'T' || lastCmd == 't')
                            {
                                x1 = 2 * cx - lastCtrlX; y1 = 2 * cy - lastCtrlY;
                            }
                            else { x1 = cx; y1 = cy; }
                            if (rel) { x += cx; y += cy; }
                            path.AddQuadCurveToPoint((nfloat)x1, (nfloat)y1, (nfloat)x, (nfloat)y);
                            lastCtrlX = x1; lastCtrlY = y1;
                            cx = x; cy = y;
                            break;
                        }
                    case 'A':
                        {
                            double rx = t.ReadNumber(), ry = t.ReadNumber();
                            double rot = t.ReadNumber();
                            bool large = t.ReadFlag();
                            bool sweep = t.ReadFlag();
                            double x = t.ReadNumber(), y = t.ReadNumber();
                            if (rel) { x += cx; y += cy; }
                            ArcTo(path, cx, cy, rx, ry, rot, large, sweep, x, y);
                            cx = x; cy = y;
                            break;
                        }
                    case 'Z':
                        {
                            path.CloseSubpath();
                            cx = sx; cy = sy;
                            break;
                        }
                    default:
                        // أمر غير معروف — نتوقف بدل الدوران بلا نهاية
                        return;
                }

                // نحفظ الأمر الذي رسم فعلاً (يُستخدم في انعكاس S/T)
                lastCmd = up == 'Z' ? '\0' : cmd;
            }
        }

        /// <summary>تحويل قوس SVG (endpoint) إلى منحنيات بيزيه.</summary>
        private static void ArcTo(CGPath path, double x1, double y1,
            double rx, double ry, double phiDeg, bool largeArc, bool sweep, double x2, double y2)
        {
            if (rx == 0 || ry == 0) { path.AddLineToPoint(new CGPoint(x2, y2)); return; }
            rx = Math.Abs(rx); ry = Math.Abs(ry);

            double phi = phiDeg * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);

            double dx2 = (x1 - x2) / 2.0;
            double dy2 = (y1 - y2) / 2.0;
            double x1p = cosPhi * dx2 + sinPhi * dy2;
            double y1p = -sinPhi * dx2 + cosPhi * dy2;

            double lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
            if (lambda > 1)
            {
                var s = Math.Sqrt(lambda);
                rx *= s; ry *= s;
            }

            double sign = (largeArc != sweep) ? 1 : -1;
            double num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
            double den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
            double coef = den == 0 ? 0 : sign * Math.Sqrt(Math.Max(0, num / den));

            double cxp = coef * (rx * y1p / ry);
            double cyp = coef * -(ry * x1p / rx);

            double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2.0;

            double Angle(double ux, double uy, double vx, double vy)
            {
                double dot = ux * vx + uy * vy;
                double len = Math.Sqrt(ux * ux + uy * uy) * Math.Sqrt(vx * vx + vy * vy);
                if (len == 0) return 0;
                double a = Math.Acos(Math.Max(-1, Math.Min(1, dot / len)));
                return (ux * vy - uy * vx < 0) ? -a : a;
            }

            double theta1 = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
            double dtheta = Angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
            if (!sweep && dtheta > 0) dtheta -= 2 * Math.PI;
            if (sweep && dtheta < 0) dtheta += 2 * Math.PI;

            int segments = (int)Math.Ceiling(Math.Abs(dtheta) / (Math.PI / 2));
            if (segments < 1) segments = 1;
            double delta = dtheta / segments;
            double t = 8.0 / 3.0 * Math.Sin(delta / 4) * Math.Sin(delta / 4) / Math.Sin(delta / 2);

            double angle = theta1;
            double px = x1, py = y1;

            for (int i = 0; i < segments; i++)
            {
                double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
                double cosB = Math.Cos(angle + delta), sinB = Math.Sin(angle + delta);

                double ex = cosPhi * (rx * cosB) - sinPhi * (ry * sinB) + cx;
                double ey = sinPhi * (rx * cosB) + cosPhi * (ry * sinB) + cy;

                double dx1 = -rx * sinA * t, dy1 = ry * cosA * t;
                double dxe = rx * sinB * t, dye = -ry * cosB * t;

                double c1x = px + cosPhi * dx1 - sinPhi * dy1;
                double c1y = py + sinPhi * dx1 + cosPhi * dy1;
                double c2x = ex + cosPhi * dxe - sinPhi * dye;
                double c2y = ey + sinPhi * dxe + cosPhi * dye;

                path.AddCurveToPoint(new CGPoint(c1x, c1y), new CGPoint(c2x, c2y), new CGPoint(ex, ey));

                px = ex; py = ey;
                angle += delta;
            }
        }

        private sealed class Tokenizer
        {
            private readonly string _s;
            private int _i;

            public Tokenizer(string s) { _s = s; _i = 0; }

            public bool End
            {
                get
                {
                    SkipSeparators();
                    return _i >= _s.Length;
                }
            }

            private void SkipSeparators()
            {
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c == ',' || c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                    else break;
                }
            }

            public bool PeekIsCommand()
            {
                SkipSeparators();
                if (_i >= _s.Length) return false;
                return char.IsLetter(_s[_i]) && _s[_i] != 'e' && _s[_i] != 'E';
            }

            public char ReadCommand()
            {
                SkipSeparators();
                return _s[_i++];
            }

            public double ReadNumber()
            {
                SkipSeparators();
                int start = _i;
                if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                bool dot = false;
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c >= '0' && c <= '9') { _i++; continue; }
                    if (c == '.' && !dot) { dot = true; _i++; continue; }
                    if ((c == 'e' || c == 'E') && _i > start)
                    {
                        _i++;
                        if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                        continue;
                    }
                    break;
                }
                var token = _s.Substring(start, _i - start);
                double v;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) v = 0;
                return v;
            }

            /// <summary>علم القوس في SVG قد يُكتب "01" بدون فاصل.</summary>
            public bool ReadFlag()
            {
                SkipSeparators();
                if (_i < _s.Length && (_s[_i] == '0' || _s[_i] == '1'))
                {
                    var v = _s[_i] == '1';
                    _i++;
                    return v;
                }
                // احتياط: قد يأتي كرقم عادي
                var n = ReadNumber();
                return Math.Abs(n) > 0.5;
            }
        }
    }
}
