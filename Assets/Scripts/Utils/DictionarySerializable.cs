using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    [Serializable]
    public class DictionarySerializable<TKey, TValue>
    {
        public List<KeyValuePairSerializable> pairElements;

        public DictionarySerializable(string[] dictionary) => pairElements = new List<KeyValuePairSerializable>();

        public DictionarySerializable(TKey[] keyList, TValue[] values) =>
            pairElements = keyList.Select((key, i) => new KeyValuePairSerializable { key = key, value = values[i] })
                .ToList();

        public DictionarySerializable(KeyValuePairSerializable[] elements) => pairElements = elements.ToList();

        // Transform Dictionary => List (Serializable Dictionary)
        public DictionarySerializable(Dictionary<TKey, TValue> dictionary) =>
            pairElements = dictionary.Select(
                pair => new KeyValuePairSerializable { key = pair.Key, value = pair.Value }
            ).ToList();

        public TValue GetValue(TKey key)
        {
            var element = pairElements.Find(
                pair => EqualityComparer<TKey>.Default.Equals(pair.key, key)
            );

            if (element == null)
                throw new KeyNotFoundException("Key not found: " + key);

            return element.value;
        }

        public void SetValue(TKey key, TValue value)
        {
            var element = pairElements.Find(pair => EqualityComparer<TKey>.Default.Equals(pair.key, key));
            if (element == null)
                pairElements.Add(new KeyValuePairSerializable { key = key, value = value });
            else
                element.value = value;
        }

        [Serializable]
        public class KeyValuePairSerializable
        {
            public TKey key;
            public TValue value;
        }
    }
}