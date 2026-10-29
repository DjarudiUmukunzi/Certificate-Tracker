using System;
using System.Collections.Generic;

namespace CertTrack
{
    
    public class NotificationQueue
    {
        private class QueueNode
        {
            public Notification Data;
            public QueueNode Next;

            public QueueNode(Notification data)
            {
                Data = data;
            }
        }

        private QueueNode front; // next to leave
        private QueueNode back;  // last to arrive

        public int Count { get; private set; }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        // Pseudocode:
        //   node = new node holding the notification
        //   IF the queue is empty: front = node AND back = node
        //   ELSE: back.Next = node; back = node
        //   Count++
        public void Enqueue(Notification item)
        {
            QueueNode node = new QueueNode(item);

            if (back == null)
            {
                front = node;
                back = node;
            }
            else
            {
                back.Next = node;
                back = node;
            }

            Count++;
        }

        // Pseudocode:
        //   IF empty: error (nothing to remove)
        //   item  = front.Data
        //   front = front.Next
        //   IF the queue is now empty: back = null     <-- easy to forget (same trap as 'tail' in Remove)
        //   Count--
        //   RETURN item
        public Notification Dequeue()
        {
            if (front == null)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            Notification item = front.Data;
            front = front.Next;

            if (front == null)
            {
                back = null;
            }

            Count--;
            return item;
        }

        // Look at the front without removing it
        public Notification Peek()
        {
            if (front == null)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            return front.Data;
        }

        // Walk from front to back WITHOUT removing anything (used to display the waiting line)
        public IEnumerable<Notification> Items()
        {
            QueueNode current = front;
            while (current != null)
            {
                yield return current.Data;
                current = current.Next;
            }
        }

        public void Clear()
        {
            front = null;
            back = null;
            Count = 0;
        }
    }
}