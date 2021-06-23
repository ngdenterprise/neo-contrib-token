using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo;
using Neo.IO.Json;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.VM.Types;

namespace client
{
    static class Extensions
    {
        // work around https://github.com/neo-project/neo-devpack-dotnet/issues/647

        static async Task<RpcInvokeResult> InvokeIteratorScriptAsync(this RpcClient rpcClient, byte[] script, params Neo.Network.P2P.Payloads.Signer[] signers)
        {
            List<JObject> parameters = new List<JObject> { Convert.ToBase64String(script) };
            if (signers.Length > 0)
            {
                parameters.Add(signers.Select(p => p.ToJson()).ToArray());
            }
            var json = await rpcClient.RpcSendAsync("invokescript", parameters.ToArray()).ConfigureAwait(false);
            var result = RpcInvokeResult.FromJson(json);
            result.Stack = ((JArray)json["stack"]).Select(IteratorStackItemFromJson).ToArray();
            return result;

            static StackItem IteratorStackItemFromJson(JObject json)
            {
                StackItemType type = json["type"].TryGetEnum<StackItemType>();
                if (type == StackItemType.InteropInterface && json.ContainsProperty("iterator"))
                {
                    var array = new Neo.VM.Types.Array();
                    foreach (var item in (JArray)json["iterator"])
                        array.Add(IteratorStackItemFromJson(item));
                    return array;
                }
                return Neo.Network.RPC.Utility.StackItemFromJson(json);
            }
        }

        // work around https://github.com/neo-project/neo-devpack-dotnet/issues/652
        public static async Task<Neo.VM.Types.ByteString[]> TokensOfAsync(this RpcClient rpcClient, UInt160 scriptHash, UInt160 owner)
        {
            const string METHOD = "tokensOf";
            var script = Neo.VM.Helper.MakeScript(scriptHash, "tokensOf", owner);
            var result = await rpcClient.InvokeIteratorScriptAsync(script);

            if (result.State != Neo.VM.VMState.HALT)
            {
                var message = string.IsNullOrEmpty(result.Exception) ? $"{METHOD} returned {result.State}" : result.Exception;
                throw new Exception(message);
            }
            if (result.Stack.Length > 0
                && result.Stack[0] is Neo.VM.Types.Array array)
            {
                return array.Cast<Neo.VM.Types.ByteString>().ToArray();
            }

            throw new Exception($"{METHOD} returned unexpected results");
        }

        // work around https://github.com/neo-project/neo-devpack-dotnet/issues/652
        public static async Task<Neo.VM.Types.ByteString[]> TokensAsync(this RpcClient rpcClient, UInt160 scriptHash)
        {
            const string METHOD = "tokens";
            var script = Neo.VM.Helper.MakeScript(scriptHash, METHOD );
            var result = await rpcClient.InvokeIteratorScriptAsync(script);

            if (result.State != Neo.VM.VMState.HALT)
            {
                var message = string.IsNullOrEmpty(result.Exception) ? $"{METHOD} returned {result.State}" : result.Exception;
                throw new Exception(message);
            }
            if (result.Stack.Length > 0
                && result.Stack[0] is Neo.VM.Types.Array array)
            {
                return array.Cast<Neo.VM.Types.ByteString>().ToArray();
            }

            throw new Exception($"{METHOD} returned unexpected results");
        }

        // work around https://github.com/neo-project/neo-devpack-dotnet/issues/652
        public static async Task<IReadOnlyDictionary<string, StackItem>> PropertiesAsync(this RpcClient rpcClient, UInt160 scriptHash, Neo.VM.Types.ByteString tokenId)
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
    }
}
