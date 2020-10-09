using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoinClipper.BtcWallet.Api.Model
{
    public enum WalletStatusEnum
    {
        Closed = -1,
        Open = 0,
        FirstLoginAttempt = 1,
        SecondLoginAttempt = 2,
        ThirdLoginAttempt = 3,
        LockedOut = 4
    }
}
