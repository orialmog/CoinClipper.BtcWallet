using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using CoinClipper.BtcWallet.Api.Model;
using CoinClipper.BtcWallet.Api.Model.Exceptions;
using CoinClipper.BtcWallet.Api.Services.Framework.QBitNinjaJutsus;
using HBitcoin.KeyManagement;
using NBitcoin;
using Newtonsoft.Json.Linq;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace CoinClipper.BtcWallet.Api.Services
{
    public class WalletService : IWalletService
    {
        private readonly MemoryCache _attemptCache = new MemoryCache("attempts");
        private readonly MemoryCache _safeCache = new MemoryCache("safes");
        private static readonly Dictionary<string, string> FileNameFromRequestTokens = new Dictionary<string, string>();
        private static readonly Dictionary<string, WalletStatusEnum> OpenFailedAttempts = new Dictionary<string, WalletStatusEnum>();




        private readonly CacheItemPolicy _attemptPolicy = new CacheItemPolicy()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            RemovedCallback = delegate (CacheEntryRemovedArguments e)
            {
                var toRemove = OpenFailedAttempts
                                    .Where(f => f.Key == e.CacheItem.Key)
                                    .ToArray();

                foreach (var pair in toRemove)
                {
                    OpenFailedAttempts.Remove(pair.Key);
                }
            }
        };

        private readonly CacheItemPolicy _safesPolicy = new CacheItemPolicy()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            RemovedCallback = delegate (CacheEntryRemovedArguments e)
            {
                var toRemove = FileNameFromRequestTokens
                                    .Where(f => f.Value == e.CacheItem.Key)
                                    .ToArray();

                foreach (var pair in toRemove)
                {
                    FileNameFromRequestTokens.Remove(pair.Key);
                }
            }
        };

        private string GetWalletFilePath(string path)
        {
            string walletFileName = Path.GetFileName(path);
            if (walletFileName == "") walletFileName = Config.DefaultWalletFileName;

            var walletDirName = "Wallets";
            Directory.CreateDirectory(walletDirName);
            return Path.Combine(walletDirName, walletFileName);
        }
        private void AssertWalletNotExists(string walletFilePath)
        {
            if (File.Exists(walletFilePath))
            {
                throw new WalletExistsException($"A wallet, named {walletFilePath} already exists.");
            }
        }
        private static void AssertCorrectNetwork(Network network)
        {
            if (network != Config.Network)
            {
                var msg = string.Empty;
                msg += ($"The wallet you want to load is on the {network} Bitcoin network.");
                msg += ($"But your config file specifies {Config.Network} Bitcoin network.");
                throw new IncorrectNetworkException(msg);
            }
        }

        private WalletStatusEnum DecryptWallet(string fileName, string password, out Safe safe)
        {
            safe = null;

            if (_attemptCache.Contains(fileName))
            {
                var attempt = (WalletStatusEnum)_attemptCache.Get(fileName);
                if (attempt == WalletStatusEnum.LockedOut)
                {
                    return attempt;
                }
            }

            try
            {
                safe = Safe.Load(password, GetWalletFilePath(fileName));
                AssertCorrectNetwork(safe.Network);

                return WalletStatusEnum.Open;
            }
            catch (System.Security.SecurityException)
            {
                if (_attemptCache.Contains(fileName))
                {
                    var attempt = (WalletStatusEnum)_attemptCache.Get(fileName);
                    if (attempt < WalletStatusEnum.LockedOut)
                    {
                        attempt += 1;
                        _attemptCache.Set(fileName, attempt, _attemptPolicy);
                    }
                    return attempt;
                }
                else
                {
                    _attemptCache.Add(new CacheItem(fileName, WalletStatusEnum.FirstLoginAttempt), _safesPolicy);
                    return WalletStatusEnum.FirstLoginAttempt;
                }
            }
        }

        public CoinClipper.BtcWallet.Api.Model.BtcWalletStatus[] ListWallets()
        {
            var walletDirName = "Wallets";
            var files = Directory.CreateDirectory(walletDirName)
                 .GetFiles("*.*")
                 .Select(f => f.Name)
                 .ToArray();
            var results = new List<BtcWalletStatus>();
            foreach (var item in files)
            {

                results.Add(new BtcWalletStatus()
                {
                    Status = FileNameFromRequestTokens.ContainsValue(item) ? WalletStatusEnum.Open : WalletStatusEnum.Closed,
                    FileName = item
                });
            }

            return results.ToArray();
        }



        public CloseWalletResult Close(string requestToken)
        {
            string fileName = null;

            if (FileNameFromRequestTokens.Keys.Contains(requestToken))
            {
                fileName = FileNameFromRequestTokens[requestToken];
                _safeCache.Remove(FileNameFromRequestTokens[requestToken]);
            }

            return new CloseWalletResult()
            {
                Success = true,
                Message = "Closed file " + fileName
            };
        }

        public Model.BtcWallet GetBalances(string requestToken)
        {

            if (Config.ConnectionType == ConnectionType.Http)
            {
                Safe safe = GetSafeFromCache(requestToken);

                // 0. Query all operations, grouped by addresses
                Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses =
                    QBitNinjaJutsus.QueryOperationsPerSafeAddresses(safe);

                // 1. Get all address history records
                var addressHistoryRecords = (from elem in operationsPerAddresses
                                             from op in elem.Value
                                             select new AddressHistoryRecord(elem.Key, op)).ToList();

                // 2. Calculate wallet balances
                Money confirmedWalletBalance;
                Money unconfirmedWalletBalance;
                QBitNinjaJutsus.GetBalances(addressHistoryRecords, out confirmedWalletBalance, out unconfirmedWalletBalance);


                // 3. Group all address history records by addresses
                var addressHistoryRecordsPerAddresses = new Dictionary<BitcoinAddress, HashSet<AddressHistoryRecord>>();

                var result = new Dictionary<BitcoinAddress, BtcAddress>();

                foreach (var address in operationsPerAddresses.Keys)
                {
                    var recs = new HashSet<AddressHistoryRecord>();
                    foreach (var record in addressHistoryRecords)
                    {
                        if (record.Address == address)
                            recs.Add(record);
                    }
                    addressHistoryRecordsPerAddresses.Add(address, recs);

                    result.Add(address, new BtcAddress()
                    {
                        Address = address.ToString(),
                        History = addressHistoryRecordsPerAddresses[address]?
                        .Select(h => new AddressHistory()
                        {
                            Address = address.ToString(),
                            AmountBtc = h.Amount.ToDecimal(MoneyUnit.BTC),
                            Confirmed = h.Confirmed,
                            FirstSeen = h.FirstSeen,
                            TransactionId = h.TransactionId.ToBytes(lendian: true)
                        }).ToArray()
                    });
                }

                // 4. Calculate address balances

                foreach (var elem in addressHistoryRecordsPerAddresses)
                {
                    Money confirmedBalance;
                    Money unconfirmedBalance;
                    QBitNinjaJutsus.GetBalances(elem.Value, out confirmedBalance, out unconfirmedBalance);
                    if (confirmedBalance != Money.Zero || unconfirmedBalance != Money.Zero)
                    {
                        result[elem.Key].ConfirmedBalance = confirmedBalance.ToDecimal(MoneyUnit.BTC);
                        result[elem.Key].UnconfirmedBalance = unconfirmedBalance.ToDecimal(MoneyUnit.BTC);
                    }

                }

                return new Model.BtcWallet
                {
                    FileName = FileNameFromRequestTokens[requestToken],
                    Addresses = addressHistoryRecordsPerAddresses.Select(c => result[c.Key]).ToArray(),
                    ConfirmedWalletBalance = confirmedWalletBalance.ToDecimal(MoneyUnit.BTC),
                    UnconfirmedWalletBalance = unconfirmedWalletBalance.ToDecimal(MoneyUnit.BTC)
                };
            }
            else
            {
                throw new NotImplementedException("Invalid connection type.");
            }
        }

        public BtcWalletHistory[] GetHistory(string requestToken)
        {

            var safe = GetSafeFromCache(requestToken);

            // 0. Query all operations, grouped our used safe addresses
            Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses =
                QBitNinjaJutsus.QueryOperationsPerSafeAddresses(safe);

            Dictionary<uint256, List<BalanceOperation>> operationsPerTransactions =
                QBitNinjaJutsus.GetOperationsPerTransactions(operationsPerAddresses);

            // 3. Create history records from the transactions
            // History records is arbitrary data we want to show to the user
            var txHistoryRecords = new List<Tuple<DateTimeOffset, Money, int, uint256>>();
            foreach (var elem in operationsPerTransactions)
            {
                var amount = Money.Zero;
                foreach (var op in elem.Value)
                    amount += op.Amount;
                var firstOp = elem.Value.First();

                txHistoryRecords
                    .Add(new Tuple<DateTimeOffset, Money, int, uint256>(
                        firstOp.FirstSeen,
                        amount,
                        firstOp.Confirmations,
                        elem.Key));
            }

            // 4. Order the records by confirmations and time (Simply time does not work, because of a QBitNinja bug)
            var orderedTxHistoryRecords = txHistoryRecords
                .OrderByDescending(x => x.Item3) // Confirmations
                .ThenBy(x => x.Item1); // FirstSeen 
            var history = new List<BtcWalletHistory>();
            foreach (var record in orderedTxHistoryRecords)
            {

                history.Add(new BtcWalletHistory()
                {
                    DateTime = record.Item1.DateTime,
                    AmountBtc = record.Item2.ToDecimal(MoneyUnit.BTC),// Item2 is the ConfirmedBalance
                    Confirmed = record.Item3 > 0,
                    TransactionId = record.Item4.ToBytes(lendian: true)
                });


            }
            return history.ToArray();
        }

        public OpenWalletResult Open(string fileName, string password)
        {
            var attempt = DecryptWallet(fileName, password, out Safe safe);

            if (attempt != WalletStatusEnum.Open)
            {
                return new OpenWalletResult()
                {
                    Success = false,
                    Message = attempt == WalletStatusEnum.LockedOut ?
                                "Locked out, come back in 5 mins" :
                                "Invalid password",
                    FileName = fileName,
                    RequestToken = null
                };
            }

            if (!_safeCache.Contains(fileName))
                _safeCache.Add(new CacheItem(fileName, safe), _safesPolicy);

            var msg = new StringBuilder();
            msg.AppendLine("Wallet opened.");
            msg.AppendLine($"Wallet file: {fileName}");
            var token = Guid.NewGuid();
            FileNameFromRequestTokens.Add(token.ToString(), fileName);
            return new OpenWalletResult()
            {
                Success = safe != null,
                Message = msg.ToString(),
                FileName = fileName,
                RequestToken = token
            };
        }

        public GenerateWalletResult Generate(string password)
        {
            var id = Guid.NewGuid();
            var walletFilePath = GetWalletFilePath(id.ToString());
            AssertWalletNotExists(walletFilePath);

            Mnemonic mnemonic;
            var safe = Safe.Create(out mnemonic, password, walletFilePath, Config.Network);

            _safeCache.Add(new CacheItem(id.ToString(), safe), _safesPolicy);
            // If no exception thrown the wallet is successfully created.

            var msg = new StringBuilder();
            msg.AppendLine("Wallet is successfully created.");
            msg.AppendLine($"Wallet file: {id}");
            var token = Guid.NewGuid();
            FileNameFromRequestTokens.Add(token.ToString(), id.ToString());

            msg.AppendLine("Write down the following mnemonic words.");
            msg.AppendLine("With the mnemonic words AND your password you can recover this wallet by using the recover-wallet command.");

            msg.AppendLine("-------");
            msg.AppendLine(string.Join(", ", mnemonic.Words));
            msg.AppendLine("-------");
            return new GenerateWalletResult()
            {
                Success = safe != null,
                Message = msg.ToString(),
                Words = mnemonic.Words,
                FileName = id.ToString(),
                RequestToken = token
            };
        }

        /// <param name="words">Provide your mnemonic words</param>
        /// <param name="password">
        ///  Please note the wallet cannot check if your password is correct or not.
        ///  If you provide a wrong password a wallet will be recovered with your provided mnemonic AND password pair
        /// </param>
        public RecoverWalletResult Recover(string[] words, string password)
        {
            var id = Guid.NewGuid();

            var walletFilePath = GetWalletFilePath(id.ToString());
            AssertWalletNotExists(walletFilePath);
            var mnemonicString = string.Join(" ", words);
            AssertCorrectMnemonicFormat(mnemonicString);
            var mnemonic = new Mnemonic(mnemonicString);

            var msg = new StringBuilder();
            Safe safe = null;
            Guid token = Guid.NewGuid();

            try
            {

                safe = Safe.Recover(mnemonic, password, walletFilePath, Config.Network);
                _safeCache.Add(new CacheItem(id.ToString(), safe), _safesPolicy);
                msg.AppendLine("Wallet is successfully recovered.");
                msg.AppendLine($"Wallet file: {walletFilePath}");
                FileNameFromRequestTokens.Add(id.ToString(), id.ToString());

            }
            catch (Exception e)
            {
                msg.AppendLine(e.ToString());
            }

            return new RecoverWalletResult()
            {
                Success = safe != null,
                Message = msg.ToString(),
                FileName = id.ToString(),
                RequestToken = token
            };
        }

        private static Money ParseBtcString(string value)
        {
            decimal amount;
            if (!decimal.TryParse(
                value.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out amount))
            {
                throw new InvalidOperationException("Wrong btc amount format.");
            }

            return new Money(amount, MoneyUnit.BTC);
        }

        public SendResult Send(string requestToken, string address, string btcAmount)
        {
            Safe safe = GetSafeFromCache(requestToken);

            BitcoinAddress addressToSend = null;

            var Exit = new Func<string, SendResult>(
                (m) => new SendResult()
                {
                    Success = false,
                    Message = m.ToString()
                });

            var msg = new StringBuilder();
            try
            {
                addressToSend = BitcoinAddress.Create(address, Config.Network);
            }
            catch (Exception ex)
            {
                msg.AppendLine(ex.ToString());
            }

            if (addressToSend == null)
            {
                return Exit(msg.ToString());
            }


            if (Config.ConnectionType == ConnectionType.Http)
            {

                Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses =
                    QBitNinjaJutsus.QueryOperationsPerSafeAddresses(safe);

                // 1. Gather all the not empty private keys
                msg.AppendLine("Finding not empty private keys...");
                var operationsPerNotEmptyPrivateKeys = new Dictionary<BitcoinExtKey, List<BalanceOperation>>();
                foreach (var elem in operationsPerAddresses)
                {
                    var balance = Money.Zero;
                    foreach (var op in elem.Value) balance += op.Amount;
                    if (balance > Money.Zero)
                    {
                        var secret = safe.FindPrivateKey(elem.Key);
                        operationsPerNotEmptyPrivateKeys.Add(secret, elem.Value);
                    }
                }

                // 2. Get the script pubkey of the change.
                msg.AppendLine("Select change address...");
                Script changeScriptPubKey = null;
                Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerChangeAddresses =
                    QBitNinjaJutsus.QueryOperationsPerSafeAddresses(safe, minUnusedKeys: 1, hdPathType: HdPathType.Change);
                foreach (var elem in operationsPerChangeAddresses)
                {
                    if (elem.Value.Count == 0)
                        changeScriptPubKey = safe.FindPrivateKey(elem.Key).ScriptPubKey;
                }
                if (changeScriptPubKey == null)
                    throw new ArgumentNullException();

                // 3. Gather coins can be spend
                msg.AppendLine("Gathering unspent coins...");
                Dictionary<Coin, bool> unspentCoins = QBitNinjaJutsus.GetUnspentCoins(operationsPerNotEmptyPrivateKeys.Keys);

                // 4. Get the fee
                msg.AppendLine("Calculating transaction fee...");
                Money fee;
                try
                {
                    var txSizeInBytes = 250;
                    using (var client = new HttpClient())
                    {

                        const string request = @"https://bitcoinfees.earn.com/api/v1/fees/recommended";
                        var result = client.GetAsync(request, HttpCompletionOption.ResponseContentRead).Result;
                        var json = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                        var fastestSatoshiPerByteFee = json.Value<decimal>("fastestFee");
                        fee = new Money(fastestSatoshiPerByteFee * txSizeInBytes, MoneyUnit.Satoshi);
                    }
                }
                catch
                {
                    return Exit("Couldn't calculate transaction fee, try it again later.");

                }
                msg.AppendLine($"Fee: {fee.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")}btc");

                // 5. How much money we can spend?
                Money availableAmount = Money.Zero;
                Money unconfirmedAvailableAmount = Money.Zero;
                foreach (var elem in unspentCoins)
                {
                    // If can spend unconfirmed add all
                    if (Config.CanSpendUnconfirmed)
                    {
                        availableAmount += elem.Key.Amount;
                        if (!elem.Value)
                            unconfirmedAvailableAmount += elem.Key.Amount;
                    }
                    // else only add confirmed ones
                    else
                    {
                        if (elem.Value)
                        {
                            availableAmount += elem.Key.Amount;
                        }
                    }
                }

                // 6. How much to spend?
                Money amountToSend = null;
                if (string.Equals(btcAmount, "all", StringComparison.OrdinalIgnoreCase))
                {
                    amountToSend = availableAmount;
                    amountToSend -= fee;
                }
                else
                {
                    amountToSend = ParseBtcString(btcAmount);
                }

                // 7. Do some checks
                if (amountToSend < Money.Zero || availableAmount < amountToSend + fee)
                    return Exit("Not enough coins.");

                decimal feePc =
                    Math.Round((100 * fee.ToDecimal(MoneyUnit.BTC)) / amountToSend.ToDecimal(MoneyUnit.BTC));
                if (feePc > 1)
                {
                    msg.AppendLine();
                    msg.AppendLine($"The transaction fee is {feePc.ToString("0.#")}% of your transaction amount.");
                    msg.AppendLine(
                        $"Sending:\t {amountToSend.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")}btc");
                    msg.AppendLine(
                        $"Fee:\t\t {fee.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")}btc");

                }

                var confirmedAvailableAmount = availableAmount - unconfirmedAvailableAmount;
                var totalOutAmount = amountToSend + fee;
                if (confirmedAvailableAmount < totalOutAmount)
                {
                    var unconfirmedToSend = totalOutAmount - confirmedAvailableAmount;
                    msg.AppendLine();
                    msg.AppendLine($"In order to complete this transaction " +
                                   $"you have to spend {unconfirmedToSend.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")} unconfirmed btc.");

                }

                // 8. Select coins
                msg.AppendLine("Selecting coins...");
                var coinsToSpend = new HashSet<Coin>();
                var unspentConfirmedCoins = new List<Coin>();
                var unspentUnconfirmedCoins = new List<Coin>();
                foreach (var elem in unspentCoins)
                    if (elem.Value) unspentConfirmedCoins.Add(elem.Key);
                    else unspentUnconfirmedCoins.Add(elem.Key);

                bool haveEnough = QBitNinjaJutsus.SelectCoins(ref coinsToSpend, totalOutAmount, unspentConfirmedCoins);
                if (!haveEnough)
                    haveEnough = QBitNinjaJutsus.SelectCoins(ref coinsToSpend, totalOutAmount, unspentUnconfirmedCoins);
                if (!haveEnough)
                    throw new Exception("Not enough funds.");

                // 9. Get signing keys
                var signingKeys = new HashSet<ISecret>();
                foreach (var coin in coinsToSpend)
                {
                    foreach (var elem in operationsPerNotEmptyPrivateKeys)
                    {
                        if (elem.Key.ScriptPubKey == coin.ScriptPubKey)
                            signingKeys.Add(elem.Key);
                    }
                }

                // 10. Build the transaction
                msg.AppendLine("Signing transaction...");
                var builder = new TransactionBuilder();
                var tx = builder
                    .AddCoins(coinsToSpend)
                    .AddKeys(signingKeys.ToArray())
                    .Send(addressToSend, amountToSend)
                    .SetChange(changeScriptPubKey)
                    .SendFees(fee)
                    .BuildTransaction(true);

                if (!builder.Verify(tx))
                    return Exit("Couldn't build the transaction.");

                msg.AppendLine($"Transaction Id: {tx.GetHash()}");

                var qBitClient = new QBitNinjaClient(Config.Network);

                // QBit's success response is buggy so let's check manually, too		
                BroadcastResponse broadcastResponse;
                var success = false;
                var tried = 0;
                var maxTry = 7;
                do
                {
                    tried++;
                    msg.AppendLine($"Try broadcasting transaction... ({tried})");
                    broadcastResponse = qBitClient.Broadcast(tx).Result;
                    var getTxResp = qBitClient.GetTransaction(tx.GetHash()).Result;
                    if (getTxResp == null)
                    {
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        success = true;
                        break;
                    }
                } while (tried <= maxTry);
                if (!success)
                {
                    if (broadcastResponse.Error != null)
                    {
                        msg.AppendLine($"Error code: {broadcastResponse.Error.ErrorCode} Reason: {broadcastResponse.Error.Reason}");
                    }

                    return Exit($"The transaction might not have been successfully broadcasted. Please check the Transaction ID in a block explorer.");
                }
                msg.AppendLine("Transaction is successfully propagated on the network.");
            }

            return new SendResult()
            {
                Success = true,
                Message = msg.ToString()
            };

        }

        public string[] Receive(string requestToken)
        {
            Safe safe = GetSafeFromCache(requestToken);

            var addresses = new List<string>();

            if (Config.ConnectionType == ConnectionType.Http)
            {
                Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerReceiveAddresses =
                    QBitNinjaJutsus.QueryOperationsPerSafeAddresses(safe, 7, HdPathType.Receive);

                foreach (var elem in operationsPerReceiveAddresses)
                    if (elem.Value.Count == 0)
                        addresses.Add(elem.Key.ToString());

            }
            else
            {
                throw new InvalidOperationException("Invalid connection type.");
            }

            return addresses.ToArray();
        }


        private Safe GetSafeFromCache(string requestToken)
        {
            if (!FileNameFromRequestTokens.Keys.Contains(requestToken))
                throw new WalletNotOpenException("Wallet not opened");

            var fileName = FileNameFromRequestTokens[requestToken];

            if (!_safeCache.Contains(fileName))
                throw new WalletNotOpenException("Wallet not opened");

            if (_attemptCache.Contains(fileName) &&
              ((WalletStatusEnum)_attemptCache.Get(fileName)) == WalletStatusEnum.LockedOut)
                throw new WalletNotOpenException("Wallet locked out");

            return (Safe)_safeCache[fileName];
        }

        internal static void AssertCorrectMnemonicFormat(string mnemonic)
        {
            try
            {
                if (new Mnemonic(mnemonic).IsValidChecksum)
                    return;
            }
            catch (FormatException) { }
            catch (NotSupportedException) { }

            throw new IncorrectMnumonicException("Incorrect mnemonic format.");
        }
    }
}
