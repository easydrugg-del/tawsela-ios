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

---

## 6. شاشة توصيلة المطعم/المحل — إضافة (اتعملت في أندرويد كمان)

`Screens/RestaurantScreens.cs` → `RestaurantNewDeliveryViewController`
أندرويد: `Activities/RestaurantNewDeliveryActivity.cs` + `Resources/layout/activity_restaurant_new_delivery.xml`

الاختيار السريع تحت «عنوان التسليم (العميل)» مباشرة:

| العنصر | السلوك |
|---|---|
| المحافظة | `EgyptPlaces.Governorates()` |
| المدينة | `EgyptPlaces.CitiesOf(gov)` — بتُملأ تلقائيًا من محافظة/مدينة المطعم |
| المناطق | `Api.Areas(city)` → «مناطق {المدينة}:» + شرائح برتقالية (`chip_bg`) + «الكل ▾» |
| أهم الشوارع | `Api.Streets(city)` → «أهم شوارع {المدينة}:» + **شبكة 3 بلاطات في السطر** (`chip_red_bg` بحدود حمراء) + بحث نصي/صوتي + «عرض كل الشوارع (N)» |

- أندرويد: الشبكة بتتبني في `BuildStreetGrid` (6 سطور = 18 بلاطة، والباقي وراء «عرض كل الشوارع»).
- البحث: `EditText` + زر ميكروفون (`RecognizerIntent`، ar-EG) — الفلترة فورية مع كل حرف.
- iOS: شرائح أفقية + زر «🔍 بحث» (نفس نمط شاشة العميل في iOS).

- الضغط على شريحة/بلاطة → يملأ **عنوان التوصيل + الإحداثيات** (`ApplyDestination`).
- لو اسم المنطقة/المدينة مطابق لمنطقة تسعير → **تختارها تلقائيًا** ويظهر السعر.
- المنطقتان مختلفتان: `_areas` = مناطق التسعير (بسعر)، `Api.Areas` = مناطق جغرافية (بإحداثيات).

---

## 7. لوحة المطعم — شكل الزراير زي زراير قسم الإدارة

- أندرويد: `Resources/layout/activity_restaurant_main.xml` → بلاطات بيضاء (`tile_card_selector`) بارتفاع 96dp،
  إيموجي 30sp + عنوان 12sp، صفّين 2×2 + بلاطة «🚪 تسجيل الخروج» (نص أحمر) وفراغ لموازنة الصف.
- زر «＋ طلب توصيل جديد» يفضل برتقالي عريض في الأعلى (الإجراء الأساسي الوحيد).
- iOS: `RestaurantMainViewController` بيستخدم `Controls.Tile` — نفس بلاطات لوحة الإدارة في iOS
  (كارت أبيض + أيقونة دائرية ملوّنة + عنوان)، فاللوحتين متسقتين.
- الكود لازم يقرأ البلاطات كـ `LinearLayout` مش `Button` (`FindViewById<LinearLayout>`).

---

## 8. توجيه حساب المطعم بعد إعادة التشغيل (باج اتصلّح)

- أندرويد: `SplashActivity.GoNext` كان بيروح لـ `MainActivity` (شاشة العميل) لأي جلسة محفوظة.
- اتصلّح: `Session.IsRestaurant ? typeof(RestaurantMainActivity) : typeof(MainActivity)`.

## 9. بانر «كود خصم اليوم»

- اتشال من `MainActivity` (أندرويد) — بقى `ViewStates.Gone` ومفيش أي كود بيجيبه من `Api.CouponsList`.
- iOS ما كانش فيه من الأساس.

## 10. «توصيلة زي آخر مرة» (إعادة آخر رحلة)

- أندرويد: `MainActivity.ReorderLast` بيجيب آخر طلب ويبعت Intent extras
  (`prefFrom/prefTo/prefServiceId`) لـ `NewOrderActivity`، اللي بيملأ العنوانين
  ونوع الخدمة ويحسب الأجرة فورًا (`EstimateNow`).
- iOS كان بيفتح الشاشة **فاضية** لأن التعبئة كانت بتحصل قبل اكتمال بناء الشاشة
  ومفيش إعادة تطبيق بعد الظهور. اتصلّح في `NewOrderViewController`:
  - `_prefill` مش readonly + `SetPrefill(order)` عشان البيانات توصل مضمونة.
  - `ApplyPrefill()` اتنقل لآخر `ViewDidLoad` (بعد تحميل الخدمات/الأماكن).
  - إعادة تطبيق مرة واحدة في `ViewDidAppear` لو حصل أي مسح للحقول.
  - بعد التعبئة: `UpdateEstimate()` + `RefreshDriversCount()` زي أندرويد.
- نص البلاطة اتوحّد: «توصيلة زي آخر مرة» في المنصتين.

## 11. صفحة العميل (البلاطات) — مطابقة أندرويد بالضبط

- الهيدر: «أهلًا {الاسم}» يمين + شارة «الرصيد: N ج.م» + زر «خروج» أحمر شمال
  (بديل `txtDriverBalance` + `btnLogout`). شارة الدور اتشالت زي أندرويد.
- بطاقة «رايح فين؟» + زر غامق «اضغط لطلب توصيلة جديدة» (`cardWhereTo`).
- عنوان القسم: «القائمة الرئيسية».
- بطاقات بنفس ترتيب وتسميات أندرويد:
  - «🚕 توصيلاتي»: توصيلاتي | توصيلة زي آخر مرة.
  - «💰 المحفظة والمفضلة والإشعارات»: محفظتي | المفضلون، العروض والكوبونات | الإشعارات.
  - «⭐ المكافآت والدعم»: دعوة الأصدقاء | نقاط الولاء، اشتراك VIP (جديد) | الدعم،
    تحديات الأسبوع (جديد).
- أزرار أسفل الشاشة: «الملف الشخصي» (برتقالي) + «الإعدادات» (غامق) + «Designed @ Ahmed Ali».
- شارات الأيقونات بنفس تدرّجات `badge_orange/green/purple/deep_orange`،
  وشارة عدد الإشعارات على بلاطة الإشعارات (بديل `txtNotifBadge`).
- «المفضلون» بيفتح قائمة السائقين المفضلين مع الاتصال (بديل `ShowFavorites`).


