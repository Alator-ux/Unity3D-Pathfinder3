using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PriorityQueue<KeyType, ValueType>
{
    private SortedDictionary<KeyType, Queue<ValueType>> _elements = new SortedDictionary<KeyType, Queue<ValueType>>();

    public int Count { get; private set; }

    public void Enqueue(ValueType item, KeyType priority)
    {
        if (!_elements.ContainsKey(priority))
        {
            _elements[priority] = new Queue<ValueType>();
        }
        _elements[priority].Enqueue(item);
        Count++;
    }

    public (KeyType, ValueType) Dequeue()
    {
        if (Count == 0)
            throw new InvalidOperationException("The queue is empty.");

        var firstPair = _elements.First();
        var item = firstPair.Value.Dequeue();
        if (firstPair.Value.Count == 0)
        {
            _elements.Remove(firstPair.Key);
        }
        Count--;
        return (firstPair.Key, item);
    }

    public (KeyType, ValueType) Peek()
    {
        if (Count == 0)
            throw new InvalidOperationException("The queue is empty.");

        var firstPair = _elements.First();
        return (firstPair.Key, firstPair.Value.Peek());
    }

    public bool IsEmpty()
    {
        return Count == 0;
    }
}
