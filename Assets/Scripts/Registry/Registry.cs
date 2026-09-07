using UnityEngine;
using System.Collections.Generic;
using System;

namespace GameRegistry{
    
    public class Registry<T>
    {
        readonly List<T> _items = new();
        public IReadOnlyList<T> Items => _items;

        // Events for when an item is registered and deregistered
        public Action<T> OnRegister;
        public Action<T> OnDeregister;


        public void Register(T item){
            if(_items.Contains(item)) return;

            // notify any listeners when an item is registered
            _items.Add(item);
            OnRegister?.Invoke(item);
        }
        public void Deregister(T item){
            // Notify any listeners when an item is deregistered
            if(_items.Remove(item))
                OnDeregister?.Invoke(item);
        }


    }
}

