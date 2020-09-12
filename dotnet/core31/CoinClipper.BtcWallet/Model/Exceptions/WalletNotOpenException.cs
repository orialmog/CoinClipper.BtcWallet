using System;

namespace CoinClipper.BtcWallet.Api.Model.Exceptions
{
    public class WalletNotOpenException : Exception
    {
        public WalletNotOpenException(string noWalletOpened) : base(noWalletOpened)
        {
        }
    }
}