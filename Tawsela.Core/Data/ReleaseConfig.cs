namespace Tawsela.Data
{
    /// <summary>
    /// Release switches for store builds.
    ///
    /// v8: customer and driver wallets are enabled.
    /// Customers can top up externally and pay for a ride from the wallet at checkout.
    /// The fare is deducted from the customer wallet when the trip is completed.
    /// Wallet-to-wallet transfers (P2P) remain disabled.
    /// </summary>
    public static class ReleaseConfig
    {
        // Customer + driver wallet screens and manual top-up requests.
        public const bool FinancialFeaturesEnabled = true;
        public const bool CustomerWalletEnabled = true;

        // Customer can choose "wallet" as the payment method at checkout.
        public const bool WalletPaymentsEnabled = true;

        // No wallet-to-wallet/P2P transfer and no manual "pay driver" button.
        // Wallet payment is settled automatically on trip completion.
        public const bool WalletTransferEnabled = false;

        // برنامج الولاء (نقاط/مستويات/هدايا/تحديات/خصم تلقائي) متوقف بالكامل.
        // إعادة تفعيله تتطلب تغيير هذا العلم + تفعيل LoyaltyEnabled على السيرفر.
        public const bool LoyaltyEnabled = false;
    }
}
