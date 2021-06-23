using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Neo;
using Neo.BlockchainToolkit;
using Neo.Network.RPC;

namespace client
{

    class Program
    {
        readonly static UInt160 contractHash = UInt160.Parse("976642fb9eb3de35a0f29534fd3fd93628c69022");
        static IFileSystem fs = new FileSystem();

        static async Task Main(string[] args)
        {
            var chain = fs.LoadChain(@"C:\Users\harry\Source\neo\seattle\samples\neo-contrib\default.neo-express");
            var settings = chain.GetProtocolSettings();
            var rpcClient = new RpcClient(new Uri($"http://localhost:{chain.ConsensusNodes.First().RpcPort}"), protocolSettings: settings);
            var tokensOf = await rpcClient.TokensOfAsync(contractHash, UInt160.Zero);

            for (int i = 0; i < tokensOf.Length; i++)
            {
                Neo.VM.Types.ByteString tokenId = tokensOf[i];
                var props = await rpcClient.PropertiesAsync(contractHash, tokenId);
                var owner = new UInt160(props["owner"].GetSpan());
                var name = props["name"].GetString();
                var description = props["description"].GetString();

                Console.WriteLine($"{i + 1}. {name} ({description})");
                if (owner == UInt160.Zero)
                    Console.WriteLine($"\tAvailable");
                else
                    Console.WriteLine($"\tOwned By {Neo.Wallets.Helper.ToAddress(owner, settings.AddressVersion)}");
                Console.WriteLine($"\tToken ID: {Convert.ToHexString(tokenId.GetSpan())}");
            }
        }

    }
}
