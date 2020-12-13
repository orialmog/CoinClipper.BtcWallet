
namespace CoinClipper.BtcWallet.Api.Model
{
    public interface IWalletService
    {
        BtcWalletStatus[] List();
        OpenWalletResult Open(string fileName, string password);
        CloseWalletResult Close(string requestToken);

        CreateWalletResult Create(string password);
        RecoverWalletResult Recover(string[] words, string password);
         
        BtcWallet GetBalances(string requestToken);
        BtcWalletHistory[] GetHistory(string requestToken);
        SendResult Send(string requestToken, string address, string amountBtc);
        string[] Receive(string requestToken);
    }
}