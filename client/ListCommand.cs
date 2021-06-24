using System;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;

namespace client
{
    [Command("list")]
    class ListCommand
    {
        [Option]
        internal bool Available { get; init; } = false;

        internal async Task<int> OnExecuteAsync(ExpressChain chain, IConsole console, CancellationToken token)
        {
            try
            {
                // load neo express file so we can determin RPC port + protocol settings
                var rpcClient = chain.GetRpcClient();

                // List the deployed contracts (neo-express custom functionality) and find the NeoContributorToken script hash
                var contracts = await rpcClient.ListContractsAsync();
                var contractHash = contracts["NeoContributorToken"];

                // print the list of minted tokens
                var tokensOf = Available
                    ? await rpcClient.TokensOfAsync(contractHash, UInt160.Zero)
                    : await rpcClient.TokensAsync(contractHash);
                for (int i = 0; i < tokensOf.Length; i++)
                {
                    // retrieve properties of NFT
                    var props = await rpcClient.PropertiesAsync(contractHash, tokensOf[i]);

                    // Convert NFT properties to readable types
                    var owner = new UInt160(props["owner"].GetSpan());
                    var name = props["name"].GetString();
                    var description = props["description"].GetString();
                    var image = props["image"].GetString();

                    // Write token info to console
                    console.WriteLine($"{i + 1}. {name} ({description})");
                    console.WriteLine($"\t{image}");
                    if (owner == UInt160.Zero)
                        console.WriteLine($"\tAvailable");
                    else
                        console.WriteLine($"\tOwned By {Neo.Wallets.Helper.ToAddress(owner, chain.AddressVersion)}");
                    console.WriteLine($"\tToken ID: {Convert.ToHexString(tokensOf[i].GetSpan())}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                await console.Error.WriteLineAsync(ex.Message);
                return 1;
            }
        }
    }
}
