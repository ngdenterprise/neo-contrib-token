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
        static IFileSystem fs = new FileSystem();

        static async Task Main(string[] args)
        {
            // load neo express file so we can determin RPC port + protocol settings
            var chain = fs.LoadChain(@"..\default.neo-express");
            var settings = chain.GetProtocolSettings();
            var rpcClient = new RpcClient(new Uri($"http://localhost:{chain.ConsensusNodes.First().RpcPort}"), protocolSettings: settings);

            // List the deployed contracts (neo-express custom functionality) and find the NeoContributorToken script hash
            var contracts = await rpcClient.ListContractsAsync();
            var contractHash = contracts["NeoContributorToken"];

            // print the list of minted tokens
            var tokensOf = await rpcClient.TokensAsync(contractHash);
            for (int i = 0; i < tokensOf.Length; i++)
            {
                // retrieve properties of NFT
                var props = await rpcClient.PropertiesAsync(contractHash, tokensOf[i]);

                // Convert NFT properties to readable types
                var owner = new UInt160(props["owner"].GetSpan());
                var name = props["name"].GetString();
                var description = props["description"].GetString();

                // Write token info to console
                Console.WriteLine($"{i + 1}. {name} ({description})");
                if (owner == UInt160.Zero)
                    Console.WriteLine($"\tAvailable");
                else
                    Console.WriteLine($"\tOwned By {Neo.Wallets.Helper.ToAddress(owner, settings.AddressVersion)}");
                Console.WriteLine($"\tToken ID: {Convert.ToHexString(tokensOf[i].GetSpan())}");
            }
        }
    }
}
