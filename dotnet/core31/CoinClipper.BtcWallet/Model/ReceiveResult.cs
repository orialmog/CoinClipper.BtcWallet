namespace CoinClipper.BtcWallet.Api.Model
{
    public class ReceiveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string[] Addresses { get; set; }
    }
     
}