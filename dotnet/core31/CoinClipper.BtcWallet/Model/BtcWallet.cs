using System.Collections.Generic;

namespace CoinClipper.BtcWallet.Api.Model
{


    public class BtcWallet
    {
        public List<BtcAddress> Addresses { get; set; } = new List<BtcAddress>();
        public decimal ConfirmedWalletBalance { get; set; }
        public decimal UnconfirmedWalletBalance { get; set; }
        public string FileName { get; set; }
        public WalletStatusEnum Status { get; set; }
    }
}