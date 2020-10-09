
namespace CoinClipper.BtcWallet.Api.Model
{
    public interface IWalletService
    {
        BtcWalletStatus[] ListWallets();
        OpenWalletResult Open(string fileName, string password);
        CloseWalletResult Close(string requestToken);

        GenerateWalletResult Generate(string password);
        RecoverWalletResult Recover(string[] words, string password);
         
        BtcWallet GetBalances(string requestToken);
        BtcWalletHistory[] GetHistory(string requestToken);
        SendResult Send(string requestToken, string address, string amountBtc);
        string[] Receive(string requestToken);
    }
}