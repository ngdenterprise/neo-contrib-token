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

        // work around https://github.com/neo-project/neo-devpack-dotnet/issues/652
        public static async Task<Neo.VM.Types.ByteString[]> TokensOfAsync(this RpcClient rpcClient, UInt160 scriptHash, UInt160 owner)
        {
            var script = Neo.VM.Helper.MakeScript(scriptHash, "tokensOf", owner);
            var json = await rpcClient.InvokeScriptJsonAsync(script);
            var result = RpcInvokeResult.FromJson(json);
            result.Stack = ((JArray)json["stack"]).Select(IteratorStackItemFromJson).ToArray();

            if (result.State != Neo.VM.VMState.HALT)
            {
                var message = string.IsNullOrEmpty(result.Exception) ? $"tokensOf returned {result.State}" : result.Exception;
                throw new Exception(message);
            }
            if (result.Stack.Length > 0
                && result.Stack[0] is Neo.VM.Types.Array array)
            {
                return array.Cast<Neo.VM.Types.ByteString>().ToArray();
            }

            throw new Exception("tokensof returned unexpected results");
        }

        static async Task<JObject> InvokeScriptJsonAsync(this RpcClient rpcClient, byte[] script)
        {
            List<JObject> parameters = new List<JObject> { Convert.ToBase64String(script) };
            return await rpcClient.RpcSendAsync("invokescript", parameters.ToArray()).ConfigureAwait(false);
        }

        public static async Task<IReadOnlyDictionary<string, StackItem>> PropertiesAsync(this RpcClient rpcClient, UInt160 scriptHash, Neo.VM.Types.ByteString tokenId)
        {
            var script = Neo.VM.Helper.MakeScript(scriptHash, "properties", tokenId.GetSpan().ToArray());
            var json = await rpcClient.InvokeScriptJsonAsync(script);
            var result = RpcInvokeResult.FromJson(json);

            if (result.State != Neo.VM.VMState.HALT)
            {
                var message = string.IsNullOrEmpty(result.Exception) ? $"properties returned {result.State}" : result.Exception;
                throw new Exception(message);
            }
            if (result.Stack.Length > 0
                && result.Stack[0] is Neo.VM.Types.Map map)
            {
                return map.ToDictionary(kvp => kvp.Key.GetString(), kvp => kvp.Value);
            }

            throw new Exception("properties returned unexpected results");
        }
    }
}
