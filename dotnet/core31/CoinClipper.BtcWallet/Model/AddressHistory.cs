using System;

namespace CoinClipper.BtcWallet.Api.Model
{
    public class AddressHistory
    {
        public string Address { get; set; }
        public decimal AmountBtc { get; set; }
        public DateTimeOffset FirstSeen { get; set; }
        public bool Confirmed { get; set; }
        public string TransactionId { get; set; }
    }
}
