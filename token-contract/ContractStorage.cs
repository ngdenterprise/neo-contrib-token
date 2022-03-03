using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

#nullable enable

namespace NgdEnterprise.Samples
{
    [StorageSchema]
    static class ContractStorage
    {
        const byte Prefix_TotalSupply = 0x00;
        const byte Prefix_Balance = 0x01;
        const byte Prefix_TokenId = 0x02;
        const byte Prefix_Token = 0x03;
        const byte Prefix_AccountToken = 0x04;
        const byte Prefix_ContractOwner = 0xFF;

        public class BalanceStorageGroup // : IStorageGroup<Address, BigInteger>
        {
            StorageMap map = new StorageMap(Storage.CurrentContext, Prefix_Balance);
            public BigInteger Get(Address key)
            {
                var value = map.Get(key);
                return value == null ? BigInteger.Zero : (BigInteger)value;
            }

            public void Put(Address key, BigInteger value) => map.Put(key, value);
            public void Delete(Address key) => map.Delete(key);
        }

        public class TokenStorageGroup //: IStorageGroup<UInt256, TokenState>
        {
            StorageMap map = new StorageMap(Storage.CurrentContext, Prefix_Token);

            public TokenState Get(UInt256 key)
            {
                var value = map.Get(key);
                return value == null ? null! : (TokenState)StdLib.Deserialize(value);
            }
            // TODO: generic iterator class support
            public Iterator Find(FindOptions options = FindOptions.KeysOnly | FindOptions.RemovePrefix)
                => map.Find(FindOptions.KeysOnly | FindOptions.RemovePrefix);
            public void Put(UInt256 key, TokenState value) => map.Put(key, StdLib.Serialize(value));
            public void Delete(UInt256 key) => map.Delete(key);
        }

        public class AccountTokenStorageGroup //: IStorageGroup<Address, UInt256, BigInteger>
        {
            StorageMap map = new StorageMap(Storage.CurrentContext, Prefix_AccountToken);

            public BigInteger Get(Address owner, UInt256 tokenId)
            {
                var value = map.Get(owner + tokenId);
                return value == null ? BigInteger.Zero : (BigInteger)value;
            }

            // TODO: generic iterator class support
            public Iterator Find(Address owner, FindOptions options = FindOptions.KeysOnly | FindOptions.RemovePrefix)
                => map.Find(owner, options);
            public void Put(Address owner, UInt256 tokenId, BigInteger value) => map.Put(owner + tokenId, value);
            public void Delete(Address owner, UInt256 tokenId) => map.Delete(owner + tokenId);
        }

        [StorageGroup(Prefix_TotalSupply)]
        public static BigInteger TotalSupply
        {
            get => (BigInteger)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_TotalSupply });
            set => Storage.Put(Storage.CurrentContext, new byte[] { Prefix_TotalSupply }, value);
        }

        [StorageGroup(Prefix_Balance)]
        public static BalanceStorageGroup Balances => new BalanceStorageGroup();

        [StorageGroup(Prefix_TokenId)]
        public static BigInteger TokenId
        {
            get => (BigInteger)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_TokenId });
            set => Storage.Put(Storage.CurrentContext, new byte[] { Prefix_TokenId }, value);
        }

        [StorageGroup(Prefix_AccountToken)]
        public static AccountTokenStorageGroup AccountToken => new AccountTokenStorageGroup();

        [StorageGroup(Prefix_Token)]
        public static TokenStorageGroup Tokens => new TokenStorageGroup();

        [StorageGroup(Prefix_ContractOwner)]
        public static Address ContractOwner
        {
            get => (Address)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_ContractOwner });
            set => Storage.Put(Storage.CurrentContext, new byte[] { Prefix_ContractOwner }, value);
        }
    }
}
