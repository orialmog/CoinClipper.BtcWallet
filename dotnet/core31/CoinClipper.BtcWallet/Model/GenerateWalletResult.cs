using System;
using System.Collections.Generic;

namespace CoinClipper.BtcWallet.Api.Model
{
    public class CreateWalletResult
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public string FileName { get; set; } 
        public Guid? RequestToken { get; set; }
        public List<string> Words { get; set; } = new List<string>();
    }
}
