using UnityEngine;
using System.Collections;
using System;

namespace AI
{
    public interface IHeapItem<T> : IComparable<T> 
    {
        int HeapIndex { get; set; }
    }


    public class Heap<T> where T : IHeapItem<T> 
    {	
    	T[] items;
    	int currentItemCount;
    	

    	public Heap(int maxHeapSize) 
        {
    		items = new T[maxHeapSize];
    	}
    	

    	public void Add(T item) 
        {
    		item.HeapIndex = currentItemCount;
    		items[currentItemCount] = item;
    		SortUp(item);
    		currentItemCount++;
    	}


    	public T RemoveFirst() 
        {
    		T firstItem = items[0];
    		currentItemCount--;
    		items[0] = items[currentItemCount];
    		items[0].HeapIndex = 0;
    		SortDown(items[0]);
    		return firstItem;
    	}


    	public void UpdateItem(T item) 
        {
    		SortUp(item);
    	}


    	public int Count 
        {
    		get { return currentItemCount; }
    	}


    	public bool Contains(T item) 
        {
    		return Equals(items[item.HeapIndex], item);
    	}


    	private void SortDown(T item) 
        {
    		while (true) 
            {
                int childIndexLeft = ChildIndexLeft(item);
                int childIndexRight = ChildIndexRight(item);
    			int swapIndex = 0;

    			if (childIndexLeft < currentItemCount) 
                {
    				swapIndex = childIndexLeft;

    				if (childIndexRight < currentItemCount)
                    {
    					if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0) 
                        {
    						swapIndex = childIndexRight;
    					}
    				}

    				if (item.CompareTo(items[swapIndex]) < 0) 
                    {
    					Swap (item,items[swapIndex]);
    				}
    				else return;

    			}
    			else return;
    		}
    	}

    	
        private void SortUp(T item) 
        {
            int parentIndex = ParentIndex(item);
    		
    		while (true) 
            {
    			T parentItem = items[parentIndex];
    			if (item.CompareTo(parentItem) > 0) 
                {
    				Swap (item,parentItem);
    			}
    			else break;

                parentIndex = ParentIndex(item);
    		}
    	}

    	
        private void Swap(T itemA, T itemB) 
        {
    		items[itemA.HeapIndex] = itemB;
    		items[itemB.HeapIndex] = itemA;
    		int itemAIndex = itemA.HeapIndex;
    		itemA.HeapIndex = itemB.HeapIndex;
    		itemB.HeapIndex = itemAIndex;
    	}


        private static int ParentIndex(T item)
        {
            return (item.HeapIndex - 1) / 2;
        }


        private static int ChildIndexLeft(T item)
        {
            return item.HeapIndex * 2 + 1;
        }


        private static int ChildIndexRight(T item)
        {
            return item.HeapIndex * 2 + 2;
        }
    }
}
