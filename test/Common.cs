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

        public static byte[] CalculateTokenId(this DataCache snapshot, uint index = 0)
        {
            // Nep11Token class generates NFT identifiers by combining the TOKEN_ID_SALT with a token counter then takes a SHA256 hash of the result.
            // Since this checkpoint has no NFTs deployed yet, the token ID should be the SHA256 hash of the token script hash.
            var scriptHash = Neo.Utility.StrictUTF8.GetBytes(nameof(NeoContributorToken));
            if (index != 0)
            {
                scriptHash = (new BigInteger(scriptHash) + index).ToByteArray();
            }
            return scriptHash.Sha256();
        }
    }
}
