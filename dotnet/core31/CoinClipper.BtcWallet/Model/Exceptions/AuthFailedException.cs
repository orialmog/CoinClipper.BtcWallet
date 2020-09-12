using System;

namespace CoinClipper.BtcWallet.Api.Model.Exceptions
{
    public class AuthFailedException : Exception
    {
        public AuthFailedException(string noWalletOpened) : base(noWalletOpened)
        {
        }
    }
}