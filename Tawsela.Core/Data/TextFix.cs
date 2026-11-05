using System.Collections.Generic;
using System.Text;

namespace Tawsela.Data
{
    /// <summary>
    /// إصلاح نصوص عربية قديمة اتكتبت بترميز خاطئ:
    /// بايتات UTF-8 اتقرأت كـ Windows-1256، فتظهر حروف مثل «ط § ¨ „ ٹ ھ»
    /// بدل «البيت». البيانات الجديدة سليمة — ده للصفوف القديمة فقط.
    /// الجدول مكتوب يدويًا حتى لا نعتمد على CodePages (قد تُحذف عند الـ trimming).
    /// </summary>
    public static class TextFix
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>حروف CP1256 المقابلة للبايتات 0x80..0xBF — ما بتظهرش في عربي سليم.</summary>
        private const string Signature = "\u20AC\u067E\u201A\u0192\u201E\u2026\u2020\u2021\u02C6\u2030\u0679\u2039\u0152\u0686\u0698\u0688\u06AF\u2018\u2019\u201C\u201D\u2022\u2013\u2014\u06A9\u2122\u0691\u203A\u0153\u200C\u200D\u06BA\u00A0\u060C\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u06BE\u00AB\u00AC\u00AD\u00AE\u00AF\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u061B\u00BB\u00BC\u00BD\u00BE\u061F";

        private static readonly Dictionary<char, byte> To1256 = new Dictionary<char, byte>
        {
            { '\u20AC', 0x80 },
            { '\u067E', 0x81 },
            { '\u201A', 0x82 },
            { '\u0192', 0x83 },
            { '\u201E', 0x84 },
            { '\u2026', 0x85 },
            { '\u2020', 0x86 },
            { '\u2021', 0x87 },
            { '\u02C6', 0x88 },
            { '\u2030', 0x89 },
            { '\u0679', 0x8A },
            { '\u2039', 0x8B },
            { '\u0152', 0x8C },
            { '\u0686', 0x8D },
            { '\u0698', 0x8E },
            { '\u0688', 0x8F },
            { '\u06AF', 0x90 },
            { '\u2018', 0x91 },
            { '\u2019', 0x92 },
            { '\u201C', 0x93 },
            { '\u201D', 0x94 },
            { '\u2022', 0x95 },
            { '\u2013', 0x96 },
            { '\u2014', 0x97 },
            { '\u06A9', 0x98 },
            { '\u2122', 0x99 },
            { '\u0691', 0x9A },
            { '\u203A', 0x9B },
            { '\u0153', 0x9C },
            { '\u200C', 0x9D },
            { '\u200D', 0x9E },
            { '\u06BA', 0x9F },
            { '\u00A0', 0xA0 },
            { '\u060C', 0xA1 },
            { '\u00A2', 0xA2 },
            { '\u00A3', 0xA3 },
            { '\u00A4', 0xA4 },
            { '\u00A5', 0xA5 },
            { '\u00A6', 0xA6 },
            { '\u00A7', 0xA7 },
            { '\u00A8', 0xA8 },
            { '\u00A9', 0xA9 },
            { '\u06BE', 0xAA },
            { '\u00AB', 0xAB },
            { '\u00AC', 0xAC },
            { '\u00AD', 0xAD },
            { '\u00AE', 0xAE },
            { '\u00AF', 0xAF },
            { '\u00B0', 0xB0 },
            { '\u00B1', 0xB1 },
            { '\u00B2', 0xB2 },
            { '\u00B3', 0xB3 },
            { '\u00B4', 0xB4 },
            { '\u00B5', 0xB5 },
            { '\u00B6', 0xB6 },
            { '\u00B7', 0xB7 },
            { '\u00B8', 0xB8 },
            { '\u00B9', 0xB9 },
            { '\u061B', 0xBA },
            { '\u00BB', 0xBB },
            { '\u00BC', 0xBC },
            { '\u00BD', 0xBD },
            { '\u00BE', 0xBE },
            { '\u061F', 0xBF },
            { '\u06C1', 0xC0 },
            { '\u0621', 0xC1 },
            { '\u0622', 0xC2 },
            { '\u0623', 0xC3 },
            { '\u0624', 0xC4 },
            { '\u0625', 0xC5 },
            { '\u0626', 0xC6 },
            { '\u0627', 0xC7 },
            { '\u0628', 0xC8 },
            { '\u0629', 0xC9 },
            { '\u062A', 0xCA },
            { '\u062B', 0xCB },
            { '\u062C', 0xCC },
            { '\u062D', 0xCD },
            { '\u062E', 0xCE },
            { '\u062F', 0xCF },
            { '\u0630', 0xD0 },
            { '\u0631', 0xD1 },
            { '\u0632', 0xD2 },
            { '\u0633', 0xD3 },
            { '\u0634', 0xD4 },
            { '\u0635', 0xD5 },
            { '\u0636', 0xD6 },
            { '\u00D7', 0xD7 },
            { '\u0637', 0xD8 },
            { '\u0638', 0xD9 },
            { '\u0639', 0xDA },
            { '\u063A', 0xDB },
            { '\u0640', 0xDC },
            { '\u0641', 0xDD },
            { '\u0642', 0xDE },
            { '\u0643', 0xDF },
            { '\u00E0', 0xE0 },
            { '\u0644', 0xE1 },
            { '\u00E2', 0xE2 },
            { '\u0645', 0xE3 },
            { '\u0646', 0xE4 },
            { '\u0647', 0xE5 },
            { '\u0648', 0xE6 },
            { '\u00E7', 0xE7 },
            { '\u00E8', 0xE8 },
            { '\u00E9', 0xE9 },
            { '\u00EA', 0xEA },
            { '\u00EB', 0xEB },
            { '\u0649', 0xEC },
            { '\u064A', 0xED },
            { '\u00EE', 0xEE },
            { '\u00EF', 0xEF },
            { '\u064B', 0xF0 },
            { '\u064C', 0xF1 },
            { '\u064D', 0xF2 },
            { '\u064E', 0xF3 },
            { '\u00F4', 0xF4 },
            { '\u064F', 0xF5 },
            { '\u0650', 0xF6 },
            { '\u00F7', 0xF7 },
            { '\u0651', 0xF8 },
            { '\u00F9', 0xF9 },
            { '\u0652', 0xFA },
            { '\u00FB', 0xFB },
            { '\u00FC', 0xFC },
            { '\u200E', 0xFD },
            { '\u200F', 0xFE },
            { '\u06D2', 0xFF },
        };

        /// <summary>يرجّع النص بعد إصلاحه، أو النص الأصلي لو مش مشوّش.</summary>
        public static string Repair(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            if (text.IndexOfAny(Signature.ToCharArray()) < 0) return text;

            var bytes = new byte[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                // ASCII يتحوّل كما هو (مسافة، أرقام، NSURLSession...)
                if (ch < 0x80)
                {
                    bytes[i] = (byte)ch;
                    continue;
                }
                byte b;
                if (!To1256.TryGetValue(ch, out b)) return text;
                bytes[i] = b;
            }

            try
            {
                var fixedText = StrictUtf8.GetString(bytes);
                if (fixedText.IndexOf('\uFFFD') >= 0) return text;

                bool hasArabic = false;
                foreach (var c in fixedText)
                {
                    if (c >= '\u0600' && c <= '\u06FF') { hasArabic = true; break; }
                }
                return hasArabic ? fixedText : text;
            }
            catch
            {
                return text;
            }
        }
    }
}
