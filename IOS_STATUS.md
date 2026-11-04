# حالة نقل «توصيلة» إلى iOS

> آخر تحديث: 2026-11-04
> النتيجة: `Tawsela.Core` و `Tawsela.iOS` يُبنيان بـ **0 Errors / 0 Warnings** على Windows.
> 79 شاشة UIKit مقابل 78 Activity في أندرويد (الفارق: أدوات تطوير + شاشات مدمجة).

---

## 1. البنية المنفَّذة

```
tawsela_play -ios3/
├── Tawsela.Core/               (net10.0)  ← منطق مشترك، بلا أي اعتماد على أندرويد
│   ├── Platform/IAppPlatform.cs   IAppPrefs / IAppSecure / IAppHost / AppServices
│   ├── Data/Api.cs                194 دالة (بدون Context) + تثبيت الشهادة كما هو
│   ├── Data/SecureStore.cs        AES-GCM محمول (iv:cipher)
│   ├── Data/AppLock.cs            PBKDF2-SHA256 (120k) + ترحيل الهاش القديم
│   ├── Data/LocalBlocks.cs, Net.cs, Legal.cs, Geo.cs, Fare.cs, Loyalty.cs, ...
│   └── Models/                    19 نموذجًا
│
└── Tawsela.iOS/                (net10.0-ios 15.0+)
    ├── Tawsela.iOS.csproj
    ├── Info.plist / Entitlements.plist / PrivacyInfo.xcprivacy
    ├── Assets.xcassets/           أيقونة 1024 + كل المقاسات + LaunchLogo + LaunchBackground
    ├── Theme.cs                   ألوان colors.xml بالحرف + الخطوط + RTL
    ├── IconFactory.cs             يرسم ic_*.xml (Vector Drawable) بـ CoreGraphics
    ├── Shapes.cs                  بديل drawable/*.xml (tile/chip/badge/gradient/header)
    ├── Ui.cs                      Toast / Alert / Prompt / Progress / Choose
    ├── Controls.cs                أزرار وحقول وبلاطات موحّدة
    ├── ListLayout.cs              قائمة قياسية + ListViewController<T>
    ├── BaseViewController.cs      Safe Area + الكيبورد + RTL
    ├── Router.cs                  تنقّل مركزي + قفل التطبيق + توجيه الإشعارات
    ├── AppDelegate.cs / SceneDelegate.cs / Main.cs
    ├── Platform/
    │   ├── IosPlatform.cs         تهيئة AppServices + SecureStore
    │   ├── UserDefaultsPrefs.cs   بديل SharedPreferences
    │   ├── KeychainStore.cs       بديل Keystore (مفتاح AES في Keychain)
    │   ├── IosHost.cs             الشبكة / السجل / فتح الروابط
    │   ├── LocationService.cs     بديل LocationManager
    │   ├── Biometrics.cs          بديل BiometricPrompt (Face ID / Touch ID)
    │   ├── ImagePicker.cs         بديل ActionGetContent / ActionImageCapture
    │   ├── LeafletMap.cs          WKWebView + نفس أصول Leaflet (+ تحديث نقاط حيّ)
    │   ├── PushService.cs         UNUserNotificationCenter + APNs
    │   └── InAppNotifications.cs  بديل AppNotifications (استطلاع دوري)
    └── Screens/                   48 ملفًا / 79 شاشة
```

---

## 2. الشاشات المنقولة

### المصادقة والإقلاع
| أندرويد | iOS | الحالة |
|---|---|---|
| SplashActivity | SplashViewController | ✅ استعادة جلسة + فحص تحديث + إشعارات |
| IntroActivity | IntroViewController | ✅ |
| LoginActivity | LoginViewController | ✅ تذكر + بصمة + حظر المحاولات |
| RegisterActivity | RegisterViewController | ✅ عميل/سائق + محافظة/مدينة + مركبة + **كود المشرف (LookupSupervisor)** + شروط |
| LockActivity | LockViewController | ✅ PIN + بصمة + محاولات |

### العميل
| أندرويد | iOS | الحالة |
|---|---|---|
| MainActivity | MainViewController | ✅ بلاطات + **إعلان نشط** + **ProcessDueRecurring** |
| NewOrderActivity | NewOrderViewController | ✅ كامل: GPS/خريطة/أماكن محفوظة، وجهة، **نقطة إضافية**، مناطق/شوارع، خدمة، **دفع + رصيد المحفظة**، **كوبون**، **جدولة**، **مزايدة + حد أدنى**، **إكرامية**، **مشاركة أجرة**، **أقصى انتظار**، سائقون متاحون، تقدير ليلي |
| MapActivity / LocationPicker | MapViewController | ✅ اختيار/عرض/مسار + **تتبع مباشر للسائق** |
| TrackOrderActivity | TrackOrderViewController | ✅ تحديث 5ث، **ETA**، **رقم السائق**، PIN، **تتبع**، **تأكيد وصول**، **لم يصل**، إلغاء، تقييم (**HasRated**) |
| ChatActivity | ChatViewController | ✅ رسائل + إبلاغ |
| RateActivity | RateViewController | ✅ |
| ReceiptActivity | ReceiptViewController | ✅ + مشاركة |
| NotificationsActivity | NotificationsViewController | ✅ |
| WalletActivity | WalletViewController | ✅ رصيد + حركات + **طلبات الشحن (MyTopUps)** + شحن + سحب |
| ProfileActivity / SettingsActivity | ProfileViewController / SettingsViewController | ✅ (+ AppLock/PIN/بصمة/حذف حساب) |
| OffersActivity | CouponsViewController | ✅ + نسخ الكود |
| ReferralActivity | ReferralViewController | ✅ |
| SubscriptionActivity | SubscriptionViewController | ✅ |
| ComplaintActivity | ComplaintViewController | ✅ |
| LoyaltyActivity | LoyaltyViewController | ✅ |
| WeeklyChallengesActivity | ChallengesViewController | ✅ |
| AchievementsActivity | AchievementsViewController | ✅ |
| GiftShopActivity | GiftShopViewController | ✅ |
| TopUpHistoryActivity | TopUpHistoryViewController | ✅ (AllTopUps كما في أندرويد) |
| EmergencyListActivity | EmergencyViewController | ✅ + **تفاصيل البلاغ (SendEmergencyDetail)** |
| NearbyDriversActivity | NearbyDriversViewController | ✅ + خريطة + **مفضّلة** + **شارة مميّز** |
| RecurringActivity | RecurringViewController | ✅ |
| (الأماكن المحفوظة) | SavedPlacesViewController | ✅ |
| SplitFareActivity | SplitFareViewController | ✅ (تقسيم الأجرة) |
| OrderOffersActivity | OrderOffersViewController | ✅ (عروض السائقين + قبول) |
| ChooseServiceActivity / ChoosePaymentActivity | مدمجتان في NewOrderViewController | ✅ |

### السائق
| أندرويد | iOS | الحالة |
|---|---|---|
| DriverOrdersActivity | OrdersViewController(scope:"driver") | ✅ (فرز/بحث/تواريخ) |
| OrderDetailsActivity | OrderDetailsViewController | ✅ قبول/مزايدة/PIN/إكمال + **وصلت للعميل** + **وصلت الوجهة** + **العميل لم يحضر** + **مفضّلة/حظر/تتبع** |
| DriverStatsActivity | DriverStatsViewController | ✅ إحصائيات + توفّر + سحب + **تحديث الموقع** |
| DriverShiftsActivity | DriverShiftsViewController | ✅ |
| DriverDocActivity | DriverDocsViewController | ✅ |
| DriverStatementActivity | DriverStatementViewController | ✅ |
| DriverSubscriptionActivity | SubscriptionViewController | ✅ |
| DriverEarningsReportActivity | DriverEarningsReportViewController | ✅ |
| DriverMapActivity | DriverMovementsViewController (+ تتبع حيّ) | ✅ |
| DriverSearchActivity | DriverSearchViewController | ✅ |

### الإدارة
| أندرويد | iOS | الحالة |
|---|---|---|
| AdminActivity | AdminHomeViewController | ✅ إحصائيات + 27 قسمًا |
| AdminAnalyticsActivity | AdminAnalyticsViewController | ✅ فترة + تفصيل + **أرصدة المحافظ (AdminWallets)** |
| AdminAccountsActivity | AdminAccountsViewController | ✅ إضافة/تعديل/**كلمة المرور**/**إيقاف-تفعيل**/حذف |
| AreasAdminActivity | AreasAdminViewController | ✅ |
| StreetsAdminActivity | StreetsAdminViewController | ✅ |
| CouponAdminActivity | CouponsAdminViewController | ✅ إضافة/**تعديل**/حذف |
| EmergencyTypesAdminActivity | EmergencyTypesAdminViewController | ✅ |
| DriverAdminActivity | DriversAdminViewController | ✅ تفعيل/توفّر/**حالة**/**موقع**/حظر |
| BlockedDriversActivity | BlockedDriversViewController | ✅ |
| CustomerAdminActivity | CustomersAdminViewController | ✅ **موقع**/حذف |
| DeletedCustomersActivity | DeletedCustomersAdminViewController | ✅ |
| AdminDocsActivity | DocsAdminViewController | ✅ قبول/رفض/**قبول الكل**/عرض صورة |
| AdminComplaintsActivity | ComplaintsAdminViewController | ✅ |
| AdminSubscriptionActivity | SubscriptionsAdminViewController | ✅ **منح اشتراك** |
| TopUpAdminActivity | TopUpsAdminViewController | ✅ تأكيد/رفض |
| AdsAdminActivity | AdsAdminViewController | ✅ |
| AppUpdateAdminActivity | AppUpdateAdminViewController | ✅ |
| DriverFeeAdminActivity | DriverFeeAdminViewController | ✅ |
| ServiceAdminActivity | ServiceAdminViewController | ✅ نموذج كامل + **فترة الليل** |
| SupervisorAdminActivity | SupervisorAdminViewController | ✅ + ربط سائق |
| SupervisorEarningsActivity | SupervisorEarningsViewController | ✅ + دفعات |
| RestaurantDeliveryAdminActivity | RestaurantDeliveryAdminViewController | ✅ |
| RestaurantDeliveryMyOrdersActivity | RestaurantDeliveryMyOrdersViewController | ✅ |
| HeatmapActivity | HeatmapViewController | ✅ |
| TripReportActivity | TripReportViewController | ✅ |
| CustomerStatsActivity | CustomerStatsViewController | ✅ |
| TopUpAdminActivity (معلّقة) | TopUpsAdminViewController | ✅ |
| (المطاعم) | RestaurantsAdminViewController | ✅ إضافة/تعديل/**حساب مطعم** |

### المطعم
| أندرويد | iOS | الحالة |
|---|---|---|
| RestaurantMainActivity | RestaurantMainViewController | ✅ + **توصيلات مطعمي** |
| RestaurantNewDeliveryActivity | RestaurantNewDeliveryViewController | ✅ |
| RestaurantDeliveryRequestsActivity | RestaurantRequestsViewController | ✅ |
| RestaurantDeliveryExecuteActivity | RestaurantDeliveryExecuteViewController | ✅ |

### غير منقول (بقرار)
| أندرويد | السبب |
|---|---|
| `IconPreviewActivity` | أداة تطوير لعرض الأيقونات فقط |
| `FirebaseService` / `TawselaApplication` | بديلهما `PushService` + `AppDelegate` |
| `Ui` (Activities/Ui.cs) | بديله `Tawsela.iOS/Ui.cs` |
| `Permissions` / `AppNotifications` / `GoogleServices` | بديلها `LocationService` / `InAppNotifications` / `PushService` |

---

## 3. فروق سلوكية مقصودة (وليست نقصًا)

1. **`Api.DriverBonus`** لم يُنقل من `DriverStatsActivity`: كود أندرويد يمنح 50 ج.م كل مرة تُفتح فيها الشاشة وعدد رحلات اليوم = 10 (خطر تكرار المكافأة). الأفضل منحها من الخادم.
2. **`Api.WalletTransfer`** غير مستخدم لأن `ReleaseConfig.WalletTransferEnabled = false` في المشروع الأصلي.
3. **`Api.PendingTopUps`** مُغطّى عبر `Api.AllTopUps` في شاشة الإدارة (نفس النتيجة مع عرض كل الطلبات).
4. **`Api.DriverDocsByUser`** غير لازم: شاشة المستندات تعرض كل المستندات مع «قبول كل مستندات السائق».
5. **`Api.AdminSubscriptions`** (ملخّص) مُغطّى عبر `Api.AllSubscriptions` + `AdminSubscribe`.
6. **الجدولة** على iOS عبر شاشة `ScheduleViewController` (UIDatePicker) بدل `DatePickerDialog + TimePickerDialog`.
7. **الإشعارات** تُستطلع محليًا (`InAppNotifications`) مثل أندرويد، وAPNs اختياري عبر `EnableFirebase=true`.

---

## 4. الفروق التقنية التي يجب معرفتها

| الموضوع | أندرويد | iOS |
|---|---|---|
| تثبيت الشهادة | `HttpClientHandler` + بصمة | **يجب** `UseNativeHttpHandler=false` وإلا فشل الدخول |
| التخزين الآمن | Android Keystore | مفتاح AES-256 في Keychain (`AfterFirstUnlockThisDeviceOnly`) |
| الإشعارات | FCM مباشرة | APNs؛ وFCM على iOS يحتاج `GoogleService-Info.plist` + `EnableFirebase=true` |
| الخرائط | WebView + Leaflet | WKWebView + نفس Leaflet (نسخ الأصول إلى `Library/leaflet`) |
| الخطوط | خطوط النظام | خط النظام يعرض العربية بجودة عالية؛ Cairo اختياري عبر `Theme.CustomFontName` |
| الأيقونات | Vector Drawable | نفس ملفات XML تُرسم بـ CoreGraphics |
| الشاشة | dp / sp | pt (1:1) |
| RTL | `supportsRtl` | `ForceRightToLeft` لكل شاشة + `CFBundleDevelopmentRegion=ar` |
| Safe Area | `fitsSystemWindows` | كل الشاشات مثبتة على `SafeAreaLayoutGuide` |
| الكيبورد | `adjustResize` | `BaseViewController` يرفع `ContentInset` تلقائيًا |

---

## 5. البناء والتشغيل

> دليل التشغيل الكامل (محاكي / جهاز حقيقي / بدون Mac / أخطاء شائعة): **`IOS_RUN.md`**

### على Windows (تحقق من الكود فقط)
```powershell
cd "D:\programing\CafeManagement\tawsela_play -ios3"
dotnet build Tawsela.Core\Tawsela.Core.csproj -c Release
dotnet build Tawsela.iOS\Tawsela.iOS.csproj -c Release
```

### على macOS (لإنتاج .app)
```bash
dotnet build Tawsela.iOS/Tawsela.iOS.csproj -c Debug \
  -f net10.0-ios -r iossimulator-arm64 \
  -p:CodesignKey="" -p:CodesignProvision=""

# أو لجهاز حقيقي:
dotnet build Tawsela.iOS/Tawsela.iOS.csproj -c Release \
  -f net10.0-ios -r ios-arm64 \
  -p:CodesignKey="Apple Distribution: ..." \
  -p:CodesignProvision="Tawsela Profile"
```

### قبل الرفع إلى App Store
1. أضف `GoogleService-Info.plist` وفعّل `-p:EnableFirebase=true` إن أردت إشعارات الخادم.
2. اضبط `tawsela.app_store_id` في التفضيلات ليُفتح متجر آبل من شاشة التحديث.
3. استبدل الشهادة الذاتية بنطاق + Let's Encrypt، ثم احذف `NSAllowsArbitraryLoads`.
4. أضف `platform` إلى `Api.SetDeviceToken` على الخادم لتفادي تعارض توكن أندرويد/iOS.
5. اختبر على جهاز حقيقي: الكاميرا، الموقع، Face ID، ودفع المحفظة.

---

## 6. آخر ما أُضيف (جولة المراجعة النهائية)

| البند | أندرويد | iOS الآن |
|---|---|---|
| طلب شحن المحفظة | مبلغ + مُحوِّل + رقم عملية + **صورة إيصال** (800px/JPEG70) + حدود 1–10000 | `TopUpRequestViewController` بنفس الحقول والتحقق والمعاينة |
| رقم التحويل الرسمي | `Api.PaymentPhone` + تنبيه الطلب المعلّق في WalletActivity | يظهر في المحفظة **وفي شاشة الشحن**، مع تنبيه «طلب قيد المراجعة» |
| فلترة الطلبات | بحث نصي + «من تاريخ» + «إلى تاريخ» | `OrdersViewController`: بحث + منتقي تاريخ (`DatePickViewController`) + مسح |
| حظر السائق من المحادثة | `Api.BlockDriver` داخل ChatActivity | قائمة المحادثة: إبلاغ / حظر السائق |
| حظر/إلغاء حظر السائق | مفتاح `IsBlocked` → `UnblockDriver` أو تأكيد الحظر | `OrderDetailsViewController.BlockDriver` صار مفتاحًا بنفس المنطق |
| عمولة قبول الطلب | `Api.DriverAcceptFee` + `ShowAcceptCommission` قبل `AssignDriver` | `AcceptOrder` يعرض العمولة ويؤكد قبل الإسناد |
| رصيد السائق في الرئيسية | `txtDriverNumber` + `txtDriverBalance` + تعطيل زر الطلبات عند رصيد ≤ 0 | سطر تحت الاسم + `OpenDriverOrders` يمنع الفتح ويقترح المحفظة |
| إعادة آخر رحلة | `btnReorder` → `ReorderLast` مع `prefFrom/prefTo/prefServiceId` | بلاطة «إعادة آخر رحلة» + مُنشئ `NewOrderViewController(Order)` يملأ كل الحقول |
| الإدخال الصوتي | `android.speech` في شاشة الطلب | `VoiceInput` (Speech + AVAudioEngine، عربي ar-EG) + 3 أزرار ميكروفون |
| تفاصيل الطوارئ | `SendEmergencyDetail` | يُرسَل بعد البلاغ |

### 6.1 جولة تصحيح الأخطاء (مراجعة الحالات والنصوص)

| الخطأ | كان | صار |
|---|---|---|
| **حالات توصيل المطعم** | iOS يقارن `بانتظار_سائق` / `وصل_للمطعم` / `تم_الاستلام` — لا تطابق الخادم | الحالات الحقيقية: `قيد الانتظار` → `مخصص للسائق` → `وصل للمحل` → `استلمت الطلب` → `تم التسليم` (+ زر إغلاق) |
| **حالات السائق في الإدارة** | `متاح` / `مشغول` / `قيد_المراجعة` (قيم غير موجودة) | `نشط` / `موقوف` / `مرفوض` كما في `DriverAdminActivity` |
| **زر «إكمال الرحلة»** | يقارن `أثناء_الرحلة` (ة) والخادم يستخدم `أثناء_الرحله` (ه) → الزر لا يظهر أبدًا | صُحّح النصان (زر الإكمال + زر «وصلت للوجهة») |
| **فلتر الطلبات** | يعتمد `CreatedAt` فقط ولا يبحث في الحالة | نفس أندرويد: `CompletedAt` إن وُجد، والبحث يشمل `StatusText` |
| **قائمة «طلبات متاحة»** | تُطلب بـ `scope=driver` فتُرجع طلبات السائق نفسه | `status=جديد` بدون scope كما في أندرويد |
| **سجل الشحن للعميل** | كان يعرض **كل** طلبات الشحن في النظام (`AllTopUps`) لأي مستخدم | العميل يرى `MyTopUps` فقط، والأدمن يرى الكل من «سجل تأكيدات الدفع» |
| **مراجعة طلبات الشحن** | لا تظهر صورة الإيصال ولا رقم العملية | `TopUpDetailViewController`: البيانات + الإيصال بالحجم الكامل + تأكيد/رفض + **تعديل رقم الدفع** |
| **شاشة الإيصال** | `ReceiptViewController` موجودة لكن **غير قابلة للوصول** | زر «مشاركة الفاتورة» في تفاصيل الطلب عند الاكتمال |
| **تفاصيل الطلب** | ينقصها أزرار أندرويد | أُضيف: عرض المسار، إعادة الطلب، تقييم الطرف الآخر، إلغاء الطلب بسبب، عمولة القبول |
| **تتبع الرحلة** | لا مشاركة ولا طوارئ ولا عرض سعر للسائق | مشاركة الرحلة (UIActivity)، زر الطوارئ، «تقديم عرض سعر» |
| **الملف الشخصي** | لا يعرض نقاط الولاء | «نقاطك / العضوية / باقي N نقطة للترقية» كما في أندرويد |
| **شاشة الطلب** | لا خصم ولاء ولا وجهات سابقة | خصم الولاء حسب المستوى (5/10/15%) يُرسَل مع الطلب + آخر 5 وجهات كأزرار |
| **كشف حساب السائق** | يعرض حساب المستخدم الحالي فقط | من الإدارة: `DriverStatementPickerViewController` لاختيار أي سائق |
| **التحليلات** | للقراءة فقط | تعديل معامل الذروة، تعديل عمولة النقدي، وتصدير الطلبات CSV |

### 6.2 جولة التحقق التقني (تدقيق آلي)

| البند | النتيجة |
|---|---|
| **توقيعات `Api`** | 194 دالة في أندرويد = 194 في `Tawsela.Core`، و**نفس ترتيب وأنواع المعاملات** بعد حذف `Context` (تدقيق آلي: 0 اختلاف) |
| **المنطق المشترك** | `Fare` / `Loyalty` / `Geo` / `Row` / `Session` / `EgyptPlaces` / `MapTiles` / `ReleaseConfig` مطابقة سطرًا بسطر (0 اختلاف) |
| **`AppLock`** | نفس `PBKDF2-SHA256` و120000 تكرار و`v2$iter` ونفس الـ Pepper → أي PIN قديم يعمل |
| **الأيقونات** | كل `ic_*` مستخدمة موجودة فعليًا (كانت `ic_chart` مفقودة → استُبدلت) |
| **قفل التطبيق بعد الخلفية** | كان **لا يعمل أبدًا**: مع `UIApplicationSceneManifest` لا يُستدعى `AppDelegate.DidEnterBackground` → أُضيف `sceneDidEnterBackground` + `sceneWillEnterForeground` |
| **تحديث القوائم** | `ListViewController<T>` يعيد الجلب عند العودة للشاشة (مثل `OnResume → Load` في أندرويد) |
| **عدّاد السائقين في شاشة الطلب** | كان يستخدم نقطة نهاية إدارية للعميل → أُزيل، ورسالة محايدة عند غياب الموقع |
| **إحصائيات لوحة الإدارة** | أُضيف سطر «سائقون متاحون» من `Api.AvailableDriversCount` كما في أندرويد |
| **سجل تأكيدات الدفع** | يعرض اسم العميل + مَن اعتمد + «بانتظار البت»، والضغط يفتح التفاصيل والإيصال |
| **Info.plist / Entitlements / PrivacyInfo** | كاملة: صلاحيات عربية، ATS، Scene Manifest، APNs، ومتطلبات الخصوصية |

### 6.3 جولة الصلاحيات والتفاصيل

| البند | أندرويد | iOS الآن |
|---|---|---|
| **العملاء المحذوفون** | زر يظهر فقط لحساب «احمدعلي» + منع فتح الشاشة لغيره | نفس المنطق: الزر مخفي إلا له، والشاشة ترفض غير المصرّح |
| **خيارات العميل** | تعديل الموقع / **حظر أو فك الحظر** / حذف نهائي | أُضيف الحظر/فك الحظر (`Api.SetDriverActive`) بجانب الموقع والحذف |
| **الملف الشخصي** | الأدمن يرى «حسابات الإدارة» ولا يرى حذف الحساب؛ غير الأدمن يرى الحذف + رابط الحذف عبر الموقع | مطابق |
| **سجل تأكيدات الدفع** | اسم العميل + مَن اعتمد + «بانتظار البت» | مطابق + فتح التفاصيل والإيصال بالضغط |
| **قفل التطبيق** | `OnActivityStopped` + `onResume` | `sceneDidEnterBackground` + `sceneDidBecomeActive` (كان معطّلًا قبل الإصلاح) |
| **الأيقونات** | `ic_action_call` للاتصال | صُحّح (كانت `ic_pin`) |
| **`ic_chart`** | غير موجود في أندرويد | استُبدل بـ `ic_action_receipt` |

---

### 6.4 جولة الجاهزية للنشر (أصول + توقيع + CI)

| البند | كان | صار |
|---|---|---|
| **أيقونات التطبيق** | كلها `32bppArgb` بزوايا شفافة → **App Store يرفضها** (`ITMS-90717`: alpha channel) | أُعيد توليدها `24bppRgb` مع تعبئة الزوايا بلون الأيقونة نفسه |
| **شعار شاشة الإقلاع** | 176×123 بنسبة 1.43 بينما الأصل 1077×992 بنسبة 1.09 → الشعار مضغوط أفقيًا | أُعيد توليده من الأصل بنسبة صحيحة (134×123 / 268×246 / 402×369) |
| **أصول Leaflet** | النسخ إلى `Library/leaflet` كان غير متكرر → مجلد `images/` لا يُنسخ → **علامات الخريطة مكسورة** | نسخ متكرر مع إنشاء المجلدات الفرعية |
| **`Entitlements.plist`** | موجود لكن **غير مرتبط** بالمشروع → `aps-environment` لا يُطبَّق أصلًا | `<CodesignEntitlements>` يُفعَّل عند `EnableFirebase=true` |
| **بناء بدون Mac** | غير متاح | `.github/workflows/ios-build.yml` ينتج `Tawsela.app` للمحاكي كـ Artifact |
| **فتح المشروع** | لا Solution | `Tawsela.iOS.sln` يضم `Tawsela.Core` + `Tawsela.iOS` |
| **مشاركة على iPad** | `ReferralViewController` و`ReceiptViewController` تعرضان `UIActivityViewController` بدون Popover → **Crash على iPad** | أُضيف نفس حارس `SourceView`/`SourceRect` المستخدم في `TrackOrderViewController` |

---

## 7. ما تبقّى بصراحة (قائمة صغيرة ومقصودة)

1. **`Api.DriverBonus`** — لم يُنقل عن قصد: كود أندرويد يمنح 50 ج.م **كل مرة** تُفتح فيها شاشة الإحصائيات وعدد رحلات اليوم = 10. الأصح منحها من الخادم.
2. **`Api.WalletTransfer`** — معطّل في المشروع الأصلي (`ReleaseConfig.WalletTransferEnabled = false`).
3. **`Api.PendingTopUps` / `DriverDocsByUser` / `AdminSubscriptions` / `DriverLocations`** — مُغطّاة بوظائف مكافئة (`AllTopUps`, «قبول كل مستندات السائق», `AllSubscriptions`, `AllDrivers` مع خط العرض/الطول).
4. **`Resources/anim` (4 ملفات)** — انتقالات أندرويد؛ iOS يستخدم انتقالات UIKit الافتراضية (نفس الإحساس تقريبًا).
5. **`tuktuk.png`** — أصل غير مستخدم في أندرويد نفسه (ولا يوجد Activity يقرأه).
6. **Push عبر APNs** — الكود جاهز (`PushService` + `AppDelegate` + `Entitlements`)، لكنه يحتاج `GoogleService-Info.plist` + شهادة APNs من حساب Apple.
7. **الاختبار الفعلي** — لا يمكن على Windows: يحتاج Mac/Xcode لاختبار الصوت، الكاميرا، Face ID، الخرائط، والتوقيع.
8. **`Resources/values-en`** — ملف فارغ فعليًا (app_name فقط)؛ لا توجد نسخة إنجليزية من الواجهة أصلًا في أندرويد.
9. **إعدادات أندرويد الخاصة** — الوضع الليلي، مفتاح FCM، اللغة: لا معنى لها على iOS (التطبيق فاتح وعربي دائمًا، والإشعارات APNs).
