using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;

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

            if (Debugger.IsAttached)
            {
                Debugger.Launch();
                host.Run();
            }
            else
            {
                host.RunAsService();
            }
        }

    
    }
}
