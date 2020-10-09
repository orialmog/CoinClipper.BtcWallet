using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting; 


namespace CoinClipper.BtcWallet.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);

            var host = WebHost.CreateDefaultBuilder()
                .UseContentRoot(pathToContentRoot)
                .UseStartup<Startup>()
                .Build();

            host.Run(); 
        } 
    }
}
