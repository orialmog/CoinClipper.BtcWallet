using System;
using CoinClipper.BtcWallet.Api.Model;
using Microsoft.AspNetCore.Mvc;

namespace CoinClipper.BtcWallet.Api.Controllers
{
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;


        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet]
        [Route("/list")]
        public ActionResult<BtcWalletStatus[]> List()
        {
            return _walletService.List();

        }

        [HttpPost]
        [Route("/open")]
        public ActionResult<OpenWalletResult> Open([FromForm()] string fileName, [FromForm()] string password)
        {  
            try
            {
                return _walletService.Open(fileName, password);
            }
            catch (Exception e)
            { 
                return new OpenWalletResult
                {
                    Message = e.Message.ToString(),
                    Success = false
                };
            }

        }

        [HttpGet]
        [Route("/close")]
        public ActionResult<CloseWalletResult> Close([FromHeader] string requestToken)
        {
            try
            {
                return _walletService.Close(requestToken);

            }
            catch (Exception e)
            {
                return new CloseWalletResult
                {
                    Message = e.Message.ToString(),
                    Success = false
                }; 
            }
        }


        [HttpPost()]
        [Route("/recover")]
        public ActionResult<RecoverWalletResult> Recover([FromForm()] string[] words, [FromForm()] string password)
        { 
            try
            {
                return _walletService.Recover(words, password);
            }
            catch (Exception e)
            {
                return new RecoverWalletResult
                {
                    Message = e.Message.ToString(),
                    Success = false
                }; 
            }

        }


        [HttpPost]
        [Route("/create")]
        public ActionResult<CreateWalletResult> Create([FromForm()] string password)
        {
            try
            {
                 return _walletService.Create(password);
            }
            catch(Exception e)
            { 
                return new CreateWalletResult
                {
                    Message = e.Message,
                    Success = false
                };
            }
        }




        [HttpGet]
        [Route("/balances")]
        public ActionResult<UpdateBalanceResult> GetBalances([FromHeader] string requestToken)
        {
            var result = new UpdateBalanceResult
            {
                Requested = DateTime.Now
            };

            try
            {

                result.Wallet = _walletService.GetBalances(requestToken);
                result.Message = $"Got wallet balances";
                result.Success = true;

            }
            catch (Exception e)
            {
                result.Message = e.Message;
                result.Success = false;
            }

            return result;
        }


        [HttpPost()]
        [Route("/send")]
        public ActionResult<SendResult> Send([FromHeader] string requestToken, [FromForm()] string destinationAddress, [FromForm()] string amountBtc)
        {
            try
            {
                return _walletService.Send(requestToken, destinationAddress, amountBtc);
            }
            catch (Exception e)
            {
                return new SendResult
                {
                    Message = e.Message,
                    Success = false
                };
            }

        }


        [HttpGet()]
        [Route("/receive")]
        public ActionResult<ReceiveResult> Receive([FromHeader] string requestToken)
        {
            var result = new ReceiveResult();

            try
            {

                result.Addresses = _walletService.Receive(requestToken);
                result.Message = "Valid receive addresses";
                result.Success = true;

            }
            catch (Exception e)
            {
                result.Message = e.Message;
                result.Success = false;
            }

            return result;
        }

        [HttpGet()]
        [Route("/history")]
        public ActionResult<BtcWalletHistoryResult> GetHistory([FromHeader] string requestToken)
        {
            var result = new BtcWalletHistoryResult();
            try
            {

                result.History = _walletService.GetHistory(requestToken);
                result.Message = "Wallet history";
                result.Success = true;

            }
            catch (Exception e)
            {
                result.Message = e.Message;
                result.Success = false;
            }

            return result;
        }



    }
}
