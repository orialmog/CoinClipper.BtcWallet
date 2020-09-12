using System;

namespace CoinClipper.BtcWallet.Api.Model
{
    public class GenerateWalletResult
    {
        public string[] Words { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public string FileName { get; set; }
        public Guid RequestToken { get; set; }
    }
}
