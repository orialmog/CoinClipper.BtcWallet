using System;

namespace CoinClipper.BtcWallet.Api.Model.Exceptions
{
    public class WalletExistsException : Exception
    {
        public WalletExistsException(string walletFilePath) : base(walletFilePath)
        {
           
        }
    }
}