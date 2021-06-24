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

        [Option]
        internal bool Wait { get; init; } = false;

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

                // retrieve the private key for the specified account from neo express file
                var wallet = chain.Wallets.SingleOrDefault(w => w.Name.Equals(Account, StringComparison.OrdinalIgnoreCase));
                var keyPair = new KeyPair(Convert.FromHexString(wallet.DefaultAccount.PrivateKey));

                // Transfer 10 neo to the NeoContributorToken, passing in the desired tokenId as the data parameter
                var tokenId = Convert.FromHexString(TokenId);
                var tx = await walletApi.TransferAsync(NativeContract.NEO.Hash, keyPair, contractHash, 10, tokenId);

                if (Wait)
                {
                    // wait for the tx to execute
                    var rpcTx = await walletApi.WaitTransactionAsync(tx);
                    var appLog = await rpcClient.GetApplicationLogAsync(tx.Hash.ToString());

                    var exec = appLog.Executions.Single();
                    var result = exec.VMState == Neo.VM.VMState.HALT && exec.Stack.Single().GetBoolean() 
                        ? "succeeded" 
                        : "failed";

                    console.WriteLine($"Transfer of token {TokenId} {result}");
                    console.WriteLine($"Gas Consumed: {exec.GasConsumed}");
                }
                else
                {
                    console.WriteLine($"Transfer Transaction Hash {tx.Hash}");
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
