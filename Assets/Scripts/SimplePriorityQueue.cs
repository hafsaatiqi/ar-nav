using System;
using System.Collections.Generic;

/// <summary>
/// Minimal binary-heap priority queue for strings with float priority.
/// Methods: Enqueue, Dequeue, Contains, UpdatePriority, Count
/// </summary>
public class SimplePriorityQueue<T> {
    private List<(T item, float pr)> heap = new List<(T, float)>();

    public int Count => heap.Count;

    private void Swap(int a, int b) {
        var t = heap[a]; heap[a] = heap[b]; heap[b] = t;
    }

    private void SiftUp(int i) {
        while (i > 0) {
            int p = (i - 1) / 2;
            if (heap[i].pr >= heap[p].pr) break;
            Swap(i, p);
            i = p;
        }
    }
    private void SiftDown(int i) {
        while (true) {
            int l = 2 * i + 1, r = l + 1;
            int smallest = i;
            if (l < heap.Count && heap[l].pr < heap[smallest].pr) smallest = l;
            if (r < heap.Count && heap[r].pr < heap[smallest].pr) smallest = r;
            if (smallest == i) break;
            Swap(i, smallest);
            i = smallest;
        }
    }

    public void Enqueue(T item, float priority) {
        heap.Add((item, priority));
        SiftUp(heap.Count - 1);
    }

    public T Dequeue() {
        if (heap.Count == 0) throw new InvalidOperationException("Empty");
        var ret = heap[0].item;
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count > 0) SiftDown(0);
        return ret;
    }

    public bool Contains(T item) {
        for (int i = 0; i < heap.Count; i++) if (EqualityComparer<T>.Default.Equals(heap[i].item, item)) return true;
        return false;
    }

    public void UpdatePriority(T item, float newPriority) {
        for (int i = 0; i < heap.Count; i++) {
            if (EqualityComparer<T>.Default.Equals(heap[i].item, item)) {
                heap[i] = (item, newPriority);
                SiftUp(i); SiftDown(i);
                return;
            }
        }
        // if not found, add it
        Enqueue(item, newPriority);
    }
}
