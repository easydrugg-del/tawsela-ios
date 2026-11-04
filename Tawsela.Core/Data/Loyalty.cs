namespace Tawsela.Data
{
    /// <summary>مستويات الولاء ونسبة الخصم المصاحبة لكل مستوى.</summary>
    public static class Loyalty
    {
        public static void Tier(int points, out string name, out int percent, out int next)
        {
            if (!ReleaseConfig.LoyaltyEnabled) { name = "موقوف"; percent = 0; next = 0; return; }
            if (points < 100) { name = "مبتدئ"; percent = 0; next = 100; }
            else if (points < 300) { name = "فضي"; percent = 5; next = 300; }
            else if (points < 600) { name = "ذهبي"; percent = 10; next = 600; }
            else { name = "بلاتيني"; percent = 15; next = points; }
        }
    }
}
