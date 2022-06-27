using System.Collections.Generic;
using System.Numerics;
using Neo.Cryptography;
using Neo.IO;
using Neo.Persistence;
using NeoTestHarness;
using testNeoContributorToken;

namespace test
{
    static class Common
    {
        public static readonly byte[] CONTRACT_OWNER_KEY = new byte[] { 0xFF };

        public static byte[] CalculateTokenId(this DataCache snapshot, BigInteger index)
        {
            // Nep11Token class generates NFT identifiers by combining the TOKEN_ID_SALT with a token counter then takes a SHA256 hash of the result.
            // Since this checkpoint has no NFTs deployed yet, the token ID should be the SHA256 hash of the token script hash.
            var scriptHash = Neo.Utility.StrictUTF8.GetBytes(nameof(NeoContributorToken) + index.ToString());
            return scriptHash.Sha256();
        }

        public static IReadOnlyList<Neo.VM.Types.StackItem> ToList(this Neo.SmartContract.Iterators.IIterator iterator)
        {
            var refCounter = new Neo.VM.ReferenceCounter();
            var list = new List<Neo.VM.Types.StackItem>();
            while (iterator.Next())
            {
                list.Add(iterator.Value(refCounter));
            }
            return list;
        }

    }
}
