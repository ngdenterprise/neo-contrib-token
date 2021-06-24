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
    public class NeoContributorToken : SmartContract
    {
        public class TokenState
        {
            public UInt160 Owner;
            public string Name;
            public string Description;
            public string Image;
        }

        public delegate void OnTransferDelegate(UInt160 from, UInt160 to, BigInteger amount, ByteString tokenId);

        [DisplayName("Transfer")]
        public static event OnTransferDelegate OnTransfer;

        const byte Prefix_TotalSupply = 0x00;
        const byte Prefix_Balance = 0x01;
        const byte Prefix_TokenId = 0x02;
        const byte Prefix_Token = 0x03;
        const byte Prefix_AccountToken = 0x04;
        const byte Prefix_ContractOwner = 0xFF;

        [Safe]
        public static string Symbol() => "NEOCNTRB";

        [Safe]
        public static byte Decimals() => 0;

        [Safe]
        public static BigInteger TotalSupply() => (BigInteger)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_TotalSupply });

        [Safe]
        public static BigInteger BalanceOf(UInt160 owner)
        {
            if (owner is null || !owner.IsValid)
                throw new Exception("The argument \"owner\" is invalid.");
            StorageMap balanceMap = new(Storage.CurrentContext, Prefix_Balance);
            return (BigInteger)balanceMap[owner];
        }

        [Safe]
        public static UInt160 OwnerOf(ByteString tokenId)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            return token.Owner;
        }

        [Safe]
        public static Map<string, object> Properties(ByteString tokenId)
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

        [Safe]
        public static Iterator Tokens()
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            return tokenMap.Find(FindOptions.KeysOnly | FindOptions.RemovePrefix);
        }

        [Safe]
        public static Iterator TokensOf(UInt160 owner)
        {
            if (owner is null || !owner.IsValid)
                throw new Exception("The argument \"owner\" is invalid");
            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
            return accountMap.Find(owner, FindOptions.KeysOnly | FindOptions.RemovePrefix);
        }

        public static bool Transfer(UInt160 to, ByteString tokenId, object data)
        {
            if (to is null || !to.IsValid)
                throw new Exception("The argument \"to\" is invalid.");
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            UInt160 from = token.Owner;
            if (from != UInt160.Zero && !Runtime.CheckWitness(from)) return false;
            if (from != to)
            {
                token.Owner = to;
                tokenMap[tokenId] = StdLib.Serialize(token);
                UpdateBalance(from, tokenId, -1);
                UpdateBalance(to, tokenId, +1);
            }
            PostTransfer(from, to, tokenId, data);
            return true;
        }

        public static ByteString Mint(string name, string description, string image)
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

        public bool Withdraw(UInt160 to)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can withdraw NEO");
            if (to == UInt160.Zero || !to.IsValid) throw new Exception("Invalid withrdrawl address");

            var balance = NEO.BalanceOf(Runtime.ExecutingScriptHash);
            if (balance <= 0) return false;

            return NEO.Transfer(Runtime.ExecutingScriptHash, to, balance);
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            var key = new byte[] { Prefix_ContractOwner };
            Storage.Put(Storage.CurrentContext, key, tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can update the contract");

            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, ByteString tokenId)
        {
            if (tokenId != null)
            {
                if (Runtime.CallingScriptHash != NEO.Hash) throw new Exception("Wrong calling script hash");
                if (amount < 10) throw new Exception("Insufficient payment price");

                StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
                var serToken = tokenMap[tokenId];
                if (serToken == null) throw new Exception("Invalid token id"); 
                var token = (NeoContributorToken.TokenState)StdLib.Deserialize(serToken);
                if (token.Owner != UInt160.Zero) throw new Exception("Specified token already owned");

                if (!Transfer(from, tokenId, null)) throw new Exception("Transfer Failed");
            }
        }

        static void UpdateTotalSupply(BigInteger increment)
        {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_TotalSupply };
            BigInteger totalSupply = (BigInteger)Storage.Get(context, key);
            totalSupply += increment;
            Storage.Put(context, key, totalSupply);
        }

        static bool UpdateBalance(UInt160 owner, BigInteger increment)
        {
            StorageMap balanceMap = new(Storage.CurrentContext, Prefix_Balance);
            BigInteger balance = (BigInteger)balanceMap[owner];
            balance += increment;
            if (balance < 0) return false;
            if (balance.IsZero)
                balanceMap.Delete(owner);
            else
                balanceMap.Put(owner, balance);
            return true;
        }

        static ByteString NewTokenId(ByteString data)
        {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_TokenId };
            ByteString id = Storage.Get(context, key);
            Storage.Put(context, key, (BigInteger)id + 1);
            if (id is not null) data += id;
            return CryptoLib.Sha256(data);
        }

        static void Mint(ByteString tokenId, TokenState token)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            tokenMap[tokenId] = StdLib.Serialize(token);
            UpdateBalance(token.Owner, tokenId, +1);
            UpdateTotalSupply(+1);
            PostTransfer(null, token.Owner, tokenId, null);
        }

        static void Burn(ByteString tokenId)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            tokenMap.Delete(tokenId);
            UpdateBalance(token.Owner, tokenId, -1);
            UpdateTotalSupply(-1);
            PostTransfer(token.Owner, null, tokenId, null);
        }

        static void UpdateBalance(UInt160 owner, ByteString tokenId, int increment)
        {
            UpdateBalance(owner, increment);
            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
            ByteString key = owner + tokenId;
            if (increment > 0)
                accountMap.Put(key, 0);
            else
                accountMap.Delete(key);
        }

        static void PostTransfer(UInt160 from, UInt160 to, ByteString tokenId, object data)
        {
            OnTransfer(from, to, 1, tokenId);
            if (to is not null && ContractManagement.GetContract(to) is not null)
                Contract.Call(to, "onNEP11Payment", CallFlags.All, from, 1, tokenId, data);
        }

        static bool ValidateContractOwner()
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);
            var tx = (Transaction)Runtime.ScriptContainer;
            return contractOwner.Equals(tx.Sender) && Runtime.CheckWitness(contractOwner);
        }
    }
}
