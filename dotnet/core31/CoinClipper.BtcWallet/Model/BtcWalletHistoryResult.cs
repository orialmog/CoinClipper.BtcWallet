namespace CoinClipper.BtcWallet.Api.Model
{
    public class BtcWalletHistoryResult
    {
        public BtcWalletHistory[] History { get; internal set; }
        public string Message { get; internal set; }
        public bool Success { get; internal set; }
    }
}