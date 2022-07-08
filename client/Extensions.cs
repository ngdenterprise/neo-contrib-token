using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.IO.Json;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.VM.Types;

using static Neo.Network.RPC.Utility;

using ByteString = Neo.VM.Types.ByteString;
using InteropInterface = Neo.VM.Types.InteropInterface;
using StackItem = Neo.VM.Types.StackItem;

namespace client
{
    static class Extensions
    {
        public static RpcClient GetRpcClient(this ExpressChain chain)
        {
            var settings = chain.GetProtocolSettings();
            return new RpcClient(new Uri($"http://localhost:{chain.ConsensusNodes.First().RpcPort}"), protocolSettings: settings);
        }

        // work around https://github.com/neo-project/neo-modules/issues/739
        public static IAsyncEnumerable<ByteString> TokensOfAsync(this RpcClient rpcClient, UInt160 scriptHash, UInt160 owner)
        {
            const string METHOD = "tokensOf";
            var script = Neo.VM.Helper.MakeScript(scriptHash, "tokensOf", owner);
            return rpcClient.ParseTokenResultsAsync(script, METHOD);
        }

        // work around https://github.com/neo-project/neo-modules/issues/739
        public static IAsyncEnumerable<ByteString> TokensAsync(this RpcClient rpcClient, UInt160 scriptHash)
        {
            const string METHOD = "tokens";
            var script = Neo.VM.Helper.MakeScript(scriptHash, METHOD);
            return rpcClient.ParseTokenResultsAsync(script, METHOD);
        }

        static async IAsyncEnumerable<ByteString> ParseTokenResultsAsync(this RpcClient rpcClient, byte[] script, string method = null)
        {
            method ??= "InvokeScript";
            var result = await rpcClient.InvokeScriptAsync(script).ConfigureAwait(false);
            await using var _ = new SessionDisposer(rpcClient, result);

            if (result.State != Neo.VM.VMState.HALT)
            {
                var message = string.IsNullOrEmpty(result.Exception) ? $"{method} returned {result.State}" : result.Exception;
                throw new Exception(message);
            }
            if (!string.IsNullOrEmpty(result.Session)
                && result.Stack.Length > 0
                && TryGetIteratorId(result.Stack[0], out var iteratorId))
            {
                await foreach (var json in rpcClient.TraverseIteratorAsync(result.Session, iteratorId))
                {
                    yield return (ByteString)StackItemFromJson(json);
                }
            }

            static bool TryGetIteratorId(StackItem item, out string iteratorId)
            {
                if (item is InteropInterface interop)
                {
                    var @object = interop.GetInterface<object>();
                    if (@object is JObject json)
                    {
                        iteratorId = json["id"]?.AsString() ?? "";
                        if (json["interface"]?.AsString() == "IIterator"
                            && !string.IsNullOrEmpty(iteratorId))
                        {
                            return true;
                        }
                    }
                }

                iteratorId = string.Empty;
                return false;
            }
        }

        // work around https://github.com/neo-project/neo-modules/issues/739
        public static async Task<IReadOnlyDictionary<string, StackItem>> PropertiesAsync(this RpcClient rpcClient, UInt160 scriptHash, ByteString tokenId)
        {
            const string METHOD = "properties";
            var script = Neo.VM.Helper.MakeScript(scriptHash, METHOD, tokenId.GetSpan().ToArray());
            var result = await rpcClient.InvokeScriptAsync(script);

            if (result.State != Neo.VM.VMState.HALT)
            {
                var message = string.IsNullOrEmpty(result.Exception) ? $"{METHOD} returned {result.State}" : result.Exception;
                throw new Exception(message);
            }
            if (result.Stack.Length > 0
                && result.Stack[0] is Neo.VM.Types.Map map)
            {
                return map.ToDictionary(kvp => kvp.Key.GetString(), kvp => kvp.Value);
            }

            throw new Exception($"{METHOD} returned unexpected results");
        }

        public static async Task<IReadOnlyDictionary<string, UInt160>> ListContractsAsync(this RpcClient rpcClient)
        {
            const string METHOD = "expresslistcontracts";
            var json = await rpcClient.RpcSendAsync(METHOD).ConfigureAwait(false);
            if (json is JArray array)
            {
                Dictionary<string, UInt160> contracts = new();
                foreach (var jsonContract in array)
                {
                    var hash = UInt160.Parse(jsonContract["hash"].AsString());
                    var manifest = Neo.SmartContract.Manifest.ContractManifest.FromJson(jsonContract["manifest"]);
                    contracts.Add(manifest.Name, hash);
                }
                return contracts;
            }
            throw new Exception($"{METHOD} returned unexpected results");
        }

        class SessionDisposer : IAsyncDisposable
        {
            readonly RpcClient rpcClient;
            readonly RpcInvokeResult result;
            int disposed = 0;

            public SessionDisposer(RpcClient rpcClient, RpcInvokeResult result)
            {
                this.rpcClient = rpcClient;
                this.result = result;
            }

            public async ValueTask DisposeAsync()
            {
                if (!string.IsNullOrEmpty(result.Session)
                    && Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
                {
                    await rpcClient.TerminateSessionAsync(result.Session);
                }
            }
        }
    }
}
