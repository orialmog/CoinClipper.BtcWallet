using CoinClipper.BtcWallet.Api.Model;

namespace CoinClipper.BtcWallet.Api.Controllers
{
    public class BtcWalletHistoryResult
    {
        public BtcWalletHistory[] History { get; internal set; }
        public string Message { get; internal set; }
        public bool Success { get; internal set; }
    }
}