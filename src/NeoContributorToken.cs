using System;
using System.ComponentModel;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;


namespace NGDEnterprise.Samples
{
    public class NeoContributorToken : Nep11Token<NeoContributorToken.TokenState>
    {
        public class TokenState : Nep11TokenState
        {
            public string Description;
            public string Image;
        }

        const byte Prefix_Contract_Owner = 0xFF;

        public override string Symbol() => "NEOCNTRB";

        public ByteString Mint(string name, string description, string image)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can mint tokens");

            var tokenId = NewTokenId();
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

        [DisplayName("_deploy")]
        public void Deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            var key = new byte[] { Prefix_Contract_Owner };
            Storage.Put(Storage.CurrentContext, key, tx.Sender);
        }

        public void Update(ByteString nefFile, string manifest)
        {
            if (!ValidateContractOwner())
            {
                throw new Exception("Only the contract owner can update the contract");
            }
            ContractManagement.Update(nefFile, manifest, null);
        }

        private bool ValidateContractOwner()
        {
            var key = new byte[] { Prefix_Contract_Owner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);
            var tx = (Transaction)Runtime.ScriptContainer;
            return contractOwner.Equals(tx.Sender) && Runtime.CheckWitness(contractOwner);
        }
    }
}
