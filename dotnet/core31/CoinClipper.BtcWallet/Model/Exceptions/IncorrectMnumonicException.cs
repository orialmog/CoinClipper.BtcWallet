using System;

namespace CoinClipper.BtcWallet.Api.Model.Exceptions
{
    public class IncorrectMnumonicException : Exception
    {
        public IncorrectMnumonicException(string incorrectMnemonicFormat) : base(incorrectMnemonicFormat)
        {
             
        }
    }
}