using System;
using System.Collections.Generic;

namespace Game.Core.Pool
{
    public interface ISimpleObjectReset
    {
        void OnReset();
        void OnDispose();
    }

    public class SimpleObjectPool<T>: IDisposable where T : class, ISimpleObjectReset, new()
    {
        private List<T> cachedObjects = new List<T>();
        public SimpleObjectPool(int initCount = 0)
        {
            if(initCount>0)
            {
                for(int i =0;i<initCount;i++)
                {
                    T item = new T();
                    cachedObjects.Add(item);
                }
            }
        }

        public void Dispose()
        {
            for(int i =0;i<cachedObjects.Count;i++)
            {
                cachedObjects[i].OnDispose();
            }
            cachedObjects.Clear();
        }

        public T GetItem()
        {
            T item = default(T);
            if(cachedObjects.Count>0)
            {
                item = cachedObjects[0];
                cachedObjects.RemoveAt(0);
            }else
            {
                item = new T();
            }
            return item;
        }

        public void PutItem(T item)
        {
            if(item !=null)
            {
                item.OnReset();
                cachedObjects.Add(item);
            }
        }
    }
}
