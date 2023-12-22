/****************************************************
文件：SerializableDictionary
作者：haitao.li
日期：2023/07/04 12:06:03
功能：ScriptableObject的序列化不支持Dictionary类型，所以需要自己实现一个
*****************************************************/

using System.Collections.Generic;

namespace ResourceTools.Editor
{
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey> Keys = new List<TKey>();
        public List<TValue> Values = new List<TValue>();
        
        public TValue this[TKey key]
        {
            get
            {
                int index = Keys.IndexOf(key);
                if (index != -1)
                    return Values[index];
                else
                    return default(TValue);
            }
            set
            {
                int index = Keys.IndexOf(key);
                if (index != -1)
                    Values[index] = value;
                else
                {
                    Keys.Add(key);
                    Values.Add(value);
                }
            }
        }
        
        // 添加一个键值对
        public void Add(TKey key, TValue value)
        {
            Keys.Add(key);
            Values.Add(value);
        }
        
        // Remove方法
        public void Remove(TKey key)
        {
            int index = Keys.IndexOf(key);
            if (index >= 0)
            {
                Keys.RemoveAt(index);
                Values.RemoveAt(index);
            }
        }

        // 尝试获取值
        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = Keys.IndexOf(key);
            if (index >= 0)
            {
                value = Values[index];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        
        
    }
}