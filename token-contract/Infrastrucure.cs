using System;

namespace Neo.SmartContract.Framework
{
    public class Address : Neo.UInt160 
    {
        public static Address Invalid => (Address)Neo.UInt160.Zero;
    }

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