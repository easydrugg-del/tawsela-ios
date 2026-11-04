using System;
using System.Collections.Generic;

namespace Tawsela.Data
{
    /// <summary>بيانات جلسة المستخدم الحالية بعد تسجيل الدخول.</summary>
    public static class Session
    {
        public static int UserId = 0;
        public static string UserName = "";
        public static string FullName = "";
        public static string Phone = "";
        public static string Role = "عميل";   // عميل / سائق / مدير
        public static string Governorate = "";
        public static string City = "";

        public static bool IsDriver { get { return Role == "سائق"; } }
        public static bool IsAdmin { get { return Role == "مدير"; } }
        public static bool IsCustomer { get { return Role == "عميل"; } }
        public static bool IsRestaurant { get { return Role == "مطعم"; } }

        /// <summary>الحساب المميز: الوحيد الذي يرى شاشات احمدعلي (سجل الشحن/التحديث/المحذوفين).</summary>
        public static bool IsAhmedAli
        {
            get
            {
                return Normalize(UserName) == "احمدعلي" || Normalize(FullName) == "احمدعلي";
            }
        }

        /// <summary>توحيد الاسم العربي (مطابق Web2 SessionPrincipal.Normalize).</summary>
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Trim()
                .Replace(" ", "")
                .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                .Replace("ة", "ه").Replace("ى", "ي");
        }

        /// <summary>آخر موقع معروف للجهاز (للسائق).</summary>
        public static double CurrentLat = 0;
        public static double CurrentLng = 0;

        /// <summary>هل السائق متاح لاستقبال الطلبات الآن؟</summary>
        public static bool DriverOnline = false;

        public static void Reset()
        {
            UserId = 0;
            UserName = "";
            FullName = "";
            Phone = "";
            Role = "عميل";
            Governorate = "";
            City = "";
            CurrentLat = 0;
            CurrentLng = 0;
            DriverOnline = false;
        }
    }
}
