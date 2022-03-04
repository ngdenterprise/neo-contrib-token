using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

#nullable enable

namespace NgdEnterprise.Samples
{
    public class TokenState : Nep11TokenState
    {
        public string Description = string.Empty;
        public string Image = string.Empty;
    }

    [DisplayName("NgdEnterprise.Samples.NeoContributorToken")]
    [SupportedStandards("NEP-11")]
    [ContractPermission("*", "onNEP11Payment")]
    public class NeoContributorToken : SmartContract
    {

        public delegate void OnTransferDelegate(UInt160? from, UInt160 to, BigInteger amount, UInt256 tokenId);

        [DisplayName("Transfer")]
        public static event OnTransferDelegate OnTransfer = default!;

        [Safe]
        public string Symbol() => "NEOCNTRB";

        [Safe]
        public byte Decimals() => 0;

        [Safe]
        public static BigInteger TotalSupply() => ContractStorage.TotalSupply;

        [Safe]
        public static BigInteger BalanceOf(UInt160 owner)
        {
            if (owner is null || !owner.IsValid)
                throw new Exception("The argument \"owner\" is invalid.");
            return ContractStorage.Balances.Get(owner);
        }

        [Safe]
        public static UInt160 OwnerOf(UInt256 tokenId)
        {
            var token = ContractStorage.Tokens.Get(tokenId);
            if (token is null) throw new Exception("Invalid token id");
            return token.Owner;
        }

        [Safe]
        public static Map<string, object> Properties(UInt256 tokenId)
        {
            var token = ContractStorage.Tokens.Get(tokenId);
            if (token is null) throw new Exception("Invalid token id");

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
            return ContractStorage.Tokens.Find();
        }

        [Safe]
        public static Iterator TokensOf(UInt160 owner)
        {
            if (owner is null || !owner.IsValid)
                throw new Exception("The argument \"owner\" is invalid");

            return ContractStorage.AccountToken.Find(owner);
        }

        public static bool Transfer(UInt160 to, UInt256 tokenId, object? data)
        {
            if (to is null || !to.IsValid) throw new Exception("The argument \"to\" is invalid.");

            var tokenStorage = ContractStorage.Tokens;
            var token = tokenStorage.Get(tokenId);
            if (token is null)
            {
                Runtime.Log("Invalid tokenId");
                return false;
            }

            UInt160 from = token.Owner;
            if (from != UInt160.Zero && !Runtime.CheckWitness(from))
            {
                Runtime.Log("only the token owner can transfer it");
                return false;
            }

            if (from != to)
            {
                token.Owner = to;
                tokenStorage.Put(tokenId, token);
                UpdateBalance(from, tokenId, -1);
                UpdateBalance(to, tokenId, +1);
            }
            PostTransfer(from, to, tokenId, data);
            return true;
        }

        public static UInt256 Mint(string name, string description, string image)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can mint tokens");

            // generate new token ID
            var id = ContractStorage.TokenId;
            ContractStorage.TokenId = id + 1;

            var tokenIdString = nameof(NeoContributorToken) + id;
            var tokenId = (UInt256)CryptoLib.Sha256(tokenIdString);

            var token = new TokenState
            {
                Owner = UInt160.Zero,
                Name = name,
                Description = description,
                Image = image,
            };

            ContractStorage.Tokens.Put(tokenId, token);
            UpdateBalance(token.Owner, tokenId, +1);
            UpdateTotalSupply(+1);
            PostTransfer(null, token.Owner, tokenId, null);

            return tokenId;
        }

        public bool Withdraw(UInt160 to)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can withdraw NEO");
            if (to == UInt160.Zero || !to.IsValid) throw new Exception("Invalid withdrawal address");

            var balance = NEO.BalanceOf(Runtime.ExecutingScriptHash);
            if (balance <= 0) return false;

            return NEO.Transfer(Runtime.ExecutingScriptHash, to, balance);
        }

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            if (data != null)
            {
                var tokenId = (UInt256)data;
                if (Runtime.CallingScriptHash != NEO.Hash) throw new Exception("Wrong calling script hash");
                if (amount < 10) throw new Exception("Insufficient payment price");

                var token = ContractStorage.Tokens.Get(tokenId);
                if (token.Owner != UInt160.Zero) throw new Exception("Specified token already owned");

                if (!Transfer(from, tokenId, null)) throw new Exception("Transfer Failed");
            }
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            ContractStorage.ContractOwner = (UInt160)tx.Sender;
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can update the contract");

            ContractManagement.Update(nefFile, manifest, null);
        }

        static void UpdateTotalSupply(BigInteger increment)
        {
            var totalSupply = ContractStorage.TotalSupply;
            ContractStorage.TotalSupply = totalSupply + increment;
        }

        static void UpdateBalance(UInt160 owner, UInt256 tokenId, int increment)
        {
            var balancesGroup = ContractStorage.Balances;
            var balance = balancesGroup.Get(owner);

            balance += increment;
            if (balance >= 0)
            {
                if (balance.IsZero)
                    balancesGroup.Delete(owner);
                else
                    balancesGroup.Put(owner, balance);
            }

            if (increment > 0)
                ContractStorage.AccountToken.Put(owner, tokenId, 0);
            else
                ContractStorage.AccountToken.Delete(owner, tokenId);
        }

        static void PostTransfer(UInt160? from, UInt160 to, UInt256 tokenId, object? data)
        {
            OnTransfer(from, to, 1, tokenId);
            if (to is not null && ContractManagement.GetContract(to) is not null)
                Contract.Call(to, "onNEP11Payment", CallFlags.All, from, 1, tokenId, data);
        }

        static bool ValidateContractOwner()
        {
            var contractOwner = ContractStorage.ContractOwner;
            var tx = (Transaction)Runtime.ScriptContainer;
            return contractOwner.Equals(tx.Sender) && Runtime.CheckWitness(contractOwner);
        }
    }
}
