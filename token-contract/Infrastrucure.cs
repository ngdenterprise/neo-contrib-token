using System;
using Neo;
using Neo.SmartContract.Framework.Attributes;

namespace Neo.SmartContract.Framework
{
    // public class Address
    // {
    //     public static Address Invalid => (Address)Neo.UInt160.Zero;

    //     public extern bool IsValid
    //     {
    //         [OpCode(OpCode.DUP)]
    //         [OpCode(OpCode.ISTYPE, "0x28")] //ByteString
    //         [OpCode(OpCode.SWAP)]
    //         [OpCode(OpCode.SIZE)]
    //         [OpCode(OpCode.PUSHINT8, "14")] // 0x14 == 20 bytes expected array size
    //         [OpCode(OpCode.NUMEQUAL)]
    //         [OpCode(OpCode.BOOLAND)]
    //         get;
    //     }


    //     [OpCode(OpCode.NOP)]
    //     public static extern implicit operator ByteString(Address address);

    //     [OpCode(OpCode.NOP)]
    //     public static extern implicit operator UInt160(Address address);
    //     [OpCode(OpCode.NOP)]
    //     public static extern implicit operator Address(UInt160 address);
    // }

    public class StorageGroupAttribute : Attribute
    {
        public StorageGroupAttribute(byte prefix) { }
        public StorageGroupAttribute(byte[] prefix) { }
        public StorageGroupAttribute(string prefix) { }
    }

    public class StorageSchemaAttribute : Attribute { }

    public interface IStorageGroup<K, V>
    {
        V this[K key]
        {
            get => Get(key);
            set => Put(key, value);
        }

        V Get(K key);
        void Put(K key, V value);
        void Delete(K key);
    }

    public interface IStorageGroup<K1, K2, V>
    {
        V this[K1 key1, K2 key2]
        {
            get => Get(key1, key2);
            set => Put(key1, key2, value);
        }

        V Get(K1 seg1, K2 seg2);
        void Put(K1 seg1, K2 seg2, V value);
        void Delete(K1 seg1, K2 seg2);
    }
}