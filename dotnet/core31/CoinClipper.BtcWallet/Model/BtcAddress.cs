namespace CoinClipper.BtcWallet.Api.Model
{
    public class BtcAddress
    {
        public string Address { get; set; }

        public decimal ConfirmedBalance { get; set; }
        public decimal UnconfirmedBalance { get; set; }
  
        public AddressHistory[] History { get; set; }
    }
}
