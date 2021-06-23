using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit.Models;
using Neo.Network.RPC;
using Neo.SmartContract.Native;
using Neo.Wallets;

namespace client
{
    [Command("buy")]
    class BuyCommand
    {
        [Argument(0)]
        internal string TokenId { get; init; } = string.Empty;

        [Argument(1)]
        internal string Account { get; init; } = string.Empty;

        internal async Task<int> OnExecuteAsync(ExpressChain chain, IConsole console, CancellationToken token)
        {
            try
            {
                // load neo express file so we can determin RPC port + protocol settings
                var rpcClient = chain.GetRpcClient();
                var walletApi = new WalletAPI(rpcClient);

                // List the deployed contracts (neo-express custom functionality) and find the NeoContributorToken script hash
                var contracts = await rpcClient.ListContractsAsync();
                var contractHash = contracts["NeoContributorToken"];

                var tokenId = Convert.FromHexString(TokenId);
                var wallet = chain.Wallets.SingleOrDefault(w => w.Name.Equals(Account, StringComparison.OrdinalIgnoreCase));
                var keyPair = new KeyPair(Convert.FromHexString(wallet.DefaultAccount.PrivateKey));

                var tx = await walletApi.TransferAsync(NativeContract.NEO.Hash, keyPair, contractHash, 10, tokenId);
                var rpcTx = await walletApi.WaitTransactionAsync(tx);


                // // print the list of minted tokens
                // var tokensOf = await rpcClient.TokensAsync(contractHash);
                // for (int i = 0; i < tokensOf.Length; i++)
                // {
                //     // retrieve properties of NFT
                //     var props = await rpcClient.PropertiesAsync(contractHash, tokensOf[i]);

                //     // Convert NFT properties to readable types
                //     var owner = new UInt160(props["owner"].GetSpan());
                //     var name = props["name"].GetString();
                //     var description = props["description"].GetString();
                //     var image = props["image"].GetString();

                //     // Write token info to console
                //     console.WriteLine($"{i + 1}. {name} ({description})");
                //     console.WriteLine($"\t{image}");
                //     if (owner == UInt160.Zero)
                //         console.WriteLine($"\tAvailable");
                //     else
                //         console.WriteLine($"\tOwned By {Neo.Wallets.Helper.ToAddress(owner, chain.AddressVersion)}");
                //     console.WriteLine($"\tToken ID: {Convert.ToHexString(tokensOf[i].GetSpan())}");
                // }

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
