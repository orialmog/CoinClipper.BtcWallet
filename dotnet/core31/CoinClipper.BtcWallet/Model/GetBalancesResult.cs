using System;

namespace CoinClipper.BtcWallet.Api.Model
{
    public class UpdateBalanceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } 

        public BtcWallet Wallet { get; set; }
        public DateTime Requested { get; set; }
    }
}
