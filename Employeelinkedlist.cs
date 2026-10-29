using System.Collections;
using System.Collections.Generic;

namespace CertTrack
{

    public class EmployeeLinkedList : IEnumerable<Employee>
    {
        private EmployeeNode head; // first node (null while the list is empty)
        private EmployeeNode tail; // last node  (null while the list is empty)

        // How many employees are in the list. Anyone can read it; only this class changes it.
        public int Count { get; private set; }

        // ADD: attach a new node at the END of the chain.
        // Returns false (and adds nothing) if that employee ID is already in the list.
        //
        // Pseudocode:
        //   IF FindById(employee.Id) is not null: RETURN false     // IDs must be unique
        //   newNode = a node holding the employee
        //   IF the list is empty
        //       head = newNode; tail = newNode
        //   ELSE
        //       tail.Next = newNode        // the old last node points at the new one
        //       tail = newNode             // the new one is now the last node
        //   Count = Count + 1
        //   RETURN true
        //
        // Keeping a tail reference means we never have to walk the whole chain just to
        // find the end: Add is O(1).
        public bool Add(Employee employee)
        {
            if (FindById(employee.Id) != null)
            {
                return false;
            }

            EmployeeNode newNode = new EmployeeNode(employee);

            if (head == null)
            {
                head = newNode;
                tail = newNode;
            }
            else
            {
                tail.Next = newNode;
                tail = newNode;
            }

            Count++;
            return true;
        }

        // FIND BY ID: start at the head and follow Next arrows until the ID matches
        // or we fall off the end (null). There's no way to "jump" into the middle of a
        // linked list, so this is O(n) in the worst case.
        //
        // Pseudocode:
        //   current = head
        //   WHILE current is not null
        //       IF current.Data.Id == id: RETURN current.Data
        //       current = current.Next
        //   RETURN null
        public Employee FindById(int id)
        {
            EmployeeNode current = head;

            while (current != null)
            {
                if (current.Data.Id == id)
                {
                    return current.Data;
                }
                current = current.Next; // step to the next node
            }

            return null; // reached the end without a match
        }

        
        
        // Pseudocode:
        //   previous = null, current = head
        //   WHILE current is not null AND current.Id != id
        //       previous = current; current = current.Next
        //   IF current is null: RETURN false                 // never found it
        //   IF previous is null: head = current.Next         // removing the FIRST node
        //   ELSE previous.Next = current.Next                // skip over the removed node
        //   IF current is the tail: tail = previous          // removed the LAST node: tail steps back
        //   Count = Count - 1
        //   RETURN true
        //
        // Three cases to think about: removing the first node, a middle node, the last node
        // (or the only node). The "tail = previous" line is the easy one to forget: without
        // it, tail would still point at a node that is no longer in the list, and the next
        // Add would attach new employees to a ghost.
        public bool Remove(int id)
        {
            EmployeeNode previous = null;
            EmployeeNode current = head;

            while (current != null && current.Data.Id != id)
            {
                previous = current;
                current = current.Next;
            }

            if (current == null)
            {
                return false; // no employee with that ID
            }

            if (previous == null)
            {
                head = current.Next;      // we're removing the first node
            }
            else
            {
                previous.Next = current.Next; // the node before now skips over this one
            }

            if (current == tail)
            {
                tail = previous;          // null if that was the only node
            }

            Count--;
            return true;
        }

        // ENUMERATOR: walks head to tail and hands back one employee at a time.
        // "yield return" pauses here, gives one value to the foreach loop, then resumes
        // from this exact spot when the loop asks for the next one.
        public IEnumerator<Employee> GetEnumerator()
        {
            EmployeeNode current = head;

            while (current != null)
            {
                yield return current.Data;
                current = current.Next;
            }
        }

        // Required by the older non-generic IEnumerable; it just forwards to the one above.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}