using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;


namespace NgdEnterprise.Samples
{
    [DisplayName("NgdEnterprise.Samples.NeoContributorToken")]
    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "harrypierson@hotmail.com")]
    [ManifestExtra("Description", "This is an example contract")]
    public class NeoContributorToken : Nep11Token<NeoContributorToken.TokenState>
    {
        public class TokenState : Nep11TokenState
        {
            public string Description;
            public string Image;
        }

        const byte Prefix_ContractOwner = 0xFF;

        public override string Symbol() => "NEOCNTRB";

        public override Map<string, object> Properties(ByteString tokenId)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            Map<string, object> map = new();
            map["owner"] = token.Owner;
            map["name"] = token.Name;
            map["description"] = token.Description;
            map["image"] = token.Image;
            return map;
        }

        public ByteString Mint(string name, string description, string image)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can mint tokens");

            var tokenId = NewTokenId(nameof(NeoContributorToken));
            var tokenState = new NeoContributorToken.TokenState
            {
                Owner = UInt160.Zero,
                Name = name,
                Description = description,
                Image = image,
            };
            Mint(tokenId, tokenState);
            return tokenId;
        }

        protected static ByteString NewTokenId(ByteString data)
        {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_TokenId };
            ByteString id = Storage.Get(context, key);
            Storage.Put(context, key, (BigInteger)id + 1);
            if (id is not null) data += id;
            return CryptoLib.Sha256(data);
        }

        [DisplayName("_deploy")]
        public void Deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            var key = new byte[] { Prefix_ContractOwner };
            Storage.Put(Storage.CurrentContext, key, tx.Sender);
        }

        public void Update(ByteString nefFile, string manifest)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can update the contract");

            ContractManagement.Update(nefFile, manifest, null);
        }

        private bool ValidateContractOwner()
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);
            var tx = (Transaction)Runtime.ScriptContainer;
            return contractOwner.Equals(tx.Sender) && Runtime.CheckWitness(contractOwner);
        }

        // public void OnNEP17Payment(UInt160 from, BigInteger amount, ByteString tokenId)
        // {
        //     if (Runtime.CallingScriptHash != NEO.Hash) throw new Exception("Wrong calling script hash");

        //     // var availableToken = GetAvailableToken(tokenId);
        //     // if (availableToken == null) throw new Exception("Invalid token id");
        //     // if (amount < availableToken.Price) throw new Exception("Insufficient payment price");



        //     // NEO.Transfer(Runtime.ExecutingScriptHash, )


        //     // if (amount < 10) 

        //     // StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
        //     // var serToken = tokenMap[tokenId];
        //     // if (serToken == null) throw new 
        //     // var token = (NeoContributorToken.TokenState)StdLib.Deserialize(serToken);
        //     // if (token.Owner != UInt160.Zero) throw new Exception("Specified token already owned");

        //     // Transfer(from, tokenId, null);
        // }
    }
}
