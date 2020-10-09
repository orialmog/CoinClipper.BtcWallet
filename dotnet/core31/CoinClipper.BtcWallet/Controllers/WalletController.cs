using System;
using CoinClipper.BtcWallet.Api.Model;
using Microsoft.AspNetCore.Mvc;

namespace CoinClipper.BtcWallet.Api.Controllers
{
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _service;


        public WalletController(IWalletService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("/list")]
        public ActionResult<BtcWalletStatus[]> ListWallets()
        {
            return _service.ListWallets();

        }

        [HttpPost]
        [Route("/open")]
        public ActionResult<OpenWalletResult> OpenWallet([FromForm()] string walletId, [FromForm()] string password)
        {  
            try
            {
                return _service.Open(walletId, password);
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
                return _service.Close(requestToken);

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
                return _service.Recover(words, password);
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
        [Route("/generate")]
        public ActionResult<GenerateWalletResult> Generate([FromForm()] string password)
        {
            try
            {
                 return _service.Generate(password);
            }
            catch(Exception e)
            { 
                return new GenerateWalletResult
                {
                    Message = e.Message.ToString(),
                    Success = false
                };
            }
        }




        [HttpGet]
        [Route("/balances")]
        public ActionResult<GetBalancesResult> GetBalances([FromHeader] string requestToken)
        {
            var result = new GetBalancesResult
            {
                Requested = DateTime.Now
            };

            try
            {

                result.Wallet = _service.GetBalances(requestToken);
                result.Message = $"Got wallet balances";
                result.Success = true;

            }
            catch (Exception e)
            {
                result.Message = e.Message.ToString();
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
                return _service.Send(requestToken, destinationAddress, amountBtc);
            }
            catch (Exception e)
            {
                return new SendResult
                {
                    Message = e.Message.ToString(),
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

                result.Addresses = _service.Receive(requestToken);
                result.Message = "Valid receive addresses";
                result.Success = true;

            }
            catch (Exception e)
            {
                result.Message = e.Message.ToString();
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

                result.History = _service.GetHistory(requestToken);
                result.Message = $"Wallet history";
                result.Success = true;

            }
            catch (Exception e)
            {
                result.Message = e.Message.ToString();
                result.Success = false;
            }

            return result;
        }



    }
}
