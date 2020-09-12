namespace CoinClipper.BtcWallet.Api.Model
{
    public class BtcWallet
    {
        public BtcAddress[] Addresses { get; set; }
        public decimal ConfirmedWalletBalance { get; set; }
        public decimal UnconfirmedWalletBalance { get; set; }
        public string FileName { get; set; } 
    }
}