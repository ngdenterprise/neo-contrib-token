using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Neo.BlockchainToolkit;

namespace client
{
    [Command("neoctb-client")]
    [Subcommand(typeof(BuyCommand), typeof(ListCommand))]
    class Program
    {
        public static Task<int> Main(string[] args)
        {
            var chain = new FileSystem().FindChain();

            var services = new ServiceCollection()
                .AddSingleton(chain)
                .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                .BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            return app.ExecuteAsync(args);
        }
    }
}
