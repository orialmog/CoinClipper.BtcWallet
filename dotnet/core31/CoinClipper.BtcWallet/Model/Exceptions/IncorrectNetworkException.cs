using System;

namespace CoinClipper.BtcWallet.Api.Model.Exceptions
{
    public class IncorrectNetworkException : Exception
    {  
        public IncorrectNetworkException(string msg): base(msg)
        { 
        }
    }
}