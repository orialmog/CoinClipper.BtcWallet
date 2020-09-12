using System;

namespace CoinClipper.BtcWallet.Api.Model
{

    public class BtcWalletHistory
    {
        public DateTime DateTime { get; set; }
        public decimal AmountBtc { get; set; }
        public bool Confirmed { get; set; }
        public byte[] TransactionId { get; set; }
    }
}