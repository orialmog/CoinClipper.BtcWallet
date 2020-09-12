using System;

namespace CoinClipper.BtcWallet.Api.Model
{
    public class OpenWalletResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public Guid? RequestToken { get; set; }
    }
}