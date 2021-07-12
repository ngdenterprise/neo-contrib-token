using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NgdEnterprise.Samples
{
    [DisplayName("NgdEnterprise.Samples.NeoContributorToken")]
    [SupportedStandards("NEP-11")]
    [ContractPermission("*", "onNEP11Payment")]
    public class NeoContributorToken : SmartContract
    {
        public class TokenState
        {
            public UInt160 Owner;
            public string Name;
            public string Description;
            public string Image;

            public string Points;
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

        const byte Prefix_WhiteListAddress = 0x08;

        [Safe]
        public static string Symbol() => "AMZ";

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
            map["points"] = token.Points;
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

      


        public static bool Burn(ByteString tokenId)
        {
             StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
             tokenMap.Delete(tokenId);
             UpdateTotalSupply(-1);
             return true;
        }
        public static bool Transfer(UInt160 to, ByteString tokenId, object data)
        {
            if (to is null || !to.IsValid) throw new Exception("The argument \"to\" is invalid.");

            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            var tokenData = tokenMap[tokenId];
            if (tokenData == null)
            {
                Runtime.Log("Invalid tokenId");
                return false;
            }

            TokenState token = (TokenState)StdLib.Deserialize(tokenData);
            UInt160 from = token.Owner;
            if (from != UInt160.Zero && !Runtime.CheckWitness(from))
            {
                Runtime.Log("only the token owner can transfer it");
                return false;
            }

            if (from != to)
            {
                token.Owner = to;
                tokenMap[tokenId] = StdLib.Serialize(token);
                UpdateBalance(from, tokenId, -1);
                UpdateBalance(to, tokenId, +1);
            }
            PostTransfer(from, to, tokenId, data);

            bool isWhiteListedAddress = IsWhitelistedAddress(to);
            if(isWhiteListedAddress)
            {
                UpdateBalance(to, tokenId, -1);
                Burn(tokenId);
            }

            

                
            return true;
        }


        
        public static UInt256 Mint(string name, string description, string image, string points)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can mint tokens");

            // generate new token ID
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_TokenId };
            var id = (BigInteger)Storage.Get(context, key);
            Storage.Put(context, key, id + 1);

            var tokenIdString = nameof(NeoContributorToken) + id;
            var tokenId = (UInt256)CryptoLib.Sha256(tokenIdString);

            var tx = (Transaction)Runtime.ScriptContainer;
            var accountOwner = tx.Sender;

            var tokenState = new NeoContributorToken.TokenState
            {
                Owner = accountOwner,
                Name = name,
                Description = description,
                Image = image,
                Points = points
            };

            

            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            tokenMap[tokenId] = StdLib.Serialize(tokenState);
            UpdateBalance(tokenState.Owner, tokenId, +1);
            UpdateTotalSupply(+1);
            PostTransfer(null, tokenState.Owner, tokenId, null);

            AddWhitelistedStoreAddress(tx.Sender);

            return tokenId;
        }

        public static bool AddWhitelistedStoreAddress(UInt160 storeAddress)
        {
            StorageMap addressMap = new(Storage.CurrentContext, Prefix_WhiteListAddress);
            var whiteListedAddr = addressMap[storeAddress];
            if(whiteListedAddr == null)
            {
                addressMap[storeAddress] = storeAddress;
            }
           
            return true;
        }

        public static string GetAddress(ByteString addressString)
        {
            return StdLib.Base58CheckEncode(addressString);
        }

        private static bool IsWhitelistedAddress(UInt160 storeAddress)
        {
            StorageMap addressMap = new(Storage.CurrentContext, Prefix_WhiteListAddress);
            var whiteListedAddr = addressMap[storeAddress];
            return whiteListedAddr == null? false: true;
        }

        public bool Withdraw(UInt160 to)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can withdraw NEO");
            if (to == UInt160.Zero || !to.IsValid) throw new Exception("Invalid withrdrawl address");

            var balance = NEO.BalanceOf(Runtime.ExecutingScriptHash);
            if (balance <= 0) return false;

            return NEO.Transfer(Runtime.ExecutingScriptHash, to, balance);
        }

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            if (data != null)
            {
                var tokenId = (ByteString)data;
                if (Runtime.CallingScriptHash != NEO.Hash) throw new Exception("Wrong calling script hash");
                if (amount < 10) throw new Exception("Insufficient payment price");

                StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
                var tokenData = tokenMap[tokenId];
                if (tokenData == null) throw new Exception("Invalid token id"); 
                var token = (NeoContributorToken.TokenState)StdLib.Deserialize(tokenData);
                if (token.Owner != UInt160.Zero) throw new Exception("Specified token already owned");

                if (!Transfer(from, tokenId, null)) throw new Exception("Transfer Failed");
            }
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

        static void UpdateTotalSupply(BigInteger increment)
        {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_TotalSupply };
            BigInteger totalSupply = (BigInteger)Storage.Get(context, key);
            totalSupply += increment;
            Storage.Put(context, key, totalSupply);
        }

        static void UpdateBalance(UInt160 owner, ByteString tokenId, int increment)
        {
            StorageMap balanceMap = new(Storage.CurrentContext, Prefix_Balance);
            BigInteger balance = (BigInteger)balanceMap[owner];
            balance += increment;
            if (balance >= 0)
            {
                if (balance.IsZero)
                    balanceMap.Delete(owner);
                else
                    balanceMap.Put(owner, balance);
            }

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
