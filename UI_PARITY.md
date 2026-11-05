# مطابقة واجهة iOS مع أندرويد — ملاحظات

مرجع التصميم: ملفات `Resources/layout/*.xml` في مشروع أندرويد.

---

## 1. شاشة «طلب توصيلة جديدة»

المصدر: `Resources/layout/activity_new_order.xml`

| العنصر | أندرويد | iOS قبل | iOS الآن |
|---|---|---|---|
| الميكروفون | زر 48×48 برتقالي **جوه سطر الحقل** (`btnVoiceFrom`) | زر بعرض الشاشة | `Controls.SquareIconButton` + `FieldWithAction` |
| اختيار من الخريطة | `tile_orange_selector` — **برتقالي ممتلئ** + أيقونة | إطار برتقالي فاضي | `Controls.IconButton` |
| نوع الخدمة / الدفع | برتقالي ممتلئ + أيقونة | إطار فاضي | `Controls.IconButton` |
| حفظ المكان | `ic_save` برتقالي | إطار فاضي | `Controls.IconButton` |
| كود الخصم | حقل + زر في سطر واحد | حقل ثم زر منفصل | `FieldWithAction` |

### قواعد ثابتة

- **`drawableStart` في RTL = يمين النص** ← في iOS: `ImageEdgeInsets = (0, 10, 0, 0)` و`TitleEdgeInsets = (0, 0, 0, 10)`.
- **أزرار الخريطة/الخدمة/الدفع ممتلئة برتقالية بنص أبيض**، مش إطار.
- الميكروفون **مربّع 48×48**، مش صف كامل.

---

## 2. النص العربي المشوّش (mojibake)

### العَرَض
حروف مثل «ط § ¨ „ ٹ ھ» بدل «البيت».

### السبب
بيانات قديمة (28 أغسطس 2026) اتخزّنت بعد ما **بايتات UTF-8 اتقرأت كـ Windows-1256**.
السيرفر الحالي سليم — تم التحقق بكتابة نص جديد ورجوعه صحيحًا.

### الحل
`Tawsela.Core/Data/TextFix.cs` — بيكشف التوقيع المشوّش، يعكس التحويل (CP1256 → UTF-8)،
ويقبل الناتج فقط لو كان UTF-8 صالحًا ويحتوي حروفًا عربية. غير كده يرجّع النص الأصلي.

- الجدول (128 حرفًا) **مكتوب يدويًا** — لا يعتمد على `CodePagesEncodingProvider` لأنه قد يُحذف مع الـ trimming.
- التطبيق: `SavedPlace.FromRow` → `TextFix.Repair(Label/Address)`.

---

## 3. شريط عنوان الرئيسية

`MainViewController.BuildHeader()` — العنوان `_greeting` كان مربوطًا بـ:

```csharp
_greeting.Trailing == _bell.Leading - 14   // ❌ الجرس نفسه على الحافة اليسرى → العنوان بره الشاشة
```

الصح:

```csharp
_greeting.Trailing == header.Trailing - 18                    // يمين
_greeting.Leading  >= _bell.Trailing + 14                     // ما يتلاقاش مع الجرس
```

---

## 4. مفاتيح شاشة الدخول بلا نص

`LoginViewController.MakeSwitchRow` كانت تبني القيود يدويًا:

```csharp
label.Trailing == toggle.Leading - 10
toggle.Leading  == row.Leading          // ❌ في LTR = يسار، فالليبل بيتزقّ بره
```

الصح: `UIStackView` أفقي مع `SemanticContentAttribute = ForceRightToLeft`
(النص يمين، المفتاح يسار).

---

## 5. قواعد عامة للفحص

- أي `NSLayoutConstraint` بيربط `Trailing` بعنصر مربوط بالـ `Leading` — **راجع اتجاهه**.
- استخدم `UIStackView` بدل القيود اليدوية في الصفوف (حقل + زر، نص + مفتاح).
- كل زر فيه أيقونة: الأيقونة على **يمين** النص.
