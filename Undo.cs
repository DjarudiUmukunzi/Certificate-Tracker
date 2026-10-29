using System;

namespace CertTrack
{
    // ===================================================================
    // PART 1: the stack  (hand-built, array-based)
    // ===================================================================
    // A stack is "last in, first out" (LIFO).
    // Real-life picture: a stack of plates. You add a plate on TOP and you take a plate from the
    // TOP. The most recent action is the first one undone, exactly what an Undo button needs.
    //
    //   index:   0     1     2     3
    //          [add] [del] [add]  .....   <- 'count' = 3, the next free slot is index 3
    //                       ^ top (count - 1)
    //
    // The array is a fixed size, so when it fills up we make a new array TWICE as big and copy
    // the items across ("doubling"). Pushing is O(1) on average.
    public class UndoStack
    {
        private UndoAction[] items = new UndoAction[4];
        private int count = 0;

        public int Count
        {
            get { return count; }
        }

        // Pseudocode:
        //   IF the array is full: make a bigger array (2x) and copy everything over
        //   items[count] = action
        //   count++
        public void Push(UndoAction action)
        {
            if (count == items.Length)
            {
                UndoAction[] bigger = new UndoAction[items.Length * 2];
                for (int i = 0; i < count; i++)
                {
                    bigger[i] = items[i];
                }
                items = bigger;
            }

            items[count] = action;
            count++;
        }

        // Pseudocode:
        //   IF empty: error
        //   count--                  (the top is now one lower)
        //   action = items[count]
        //   items[count] = null      (let go of the reference so memory can be reclaimed)
        //   RETURN action
        public UndoAction Pop()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Nothing to undo.");
            }

            count--;
            UndoAction action = items[count];
            items[count] = null;
            return action;
        }

        public UndoAction Peek()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Nothing to undo.");
            }

            return items[count - 1];
        }

        public void Clear()
        {
            items = new UndoAction[4];
            count = 0;
        }
    }

    // ===================================================================
    // PART 2: the actions that can be undone  (inheritance + polymorphism)
    // ===================================================================
    // UndoAction is an ABSTRACT class: a promise that every undoable action can describe itself
    // and can Undo(). The stack only knows about UndoAction. It never needs to know WHICH kind
    // it is holding. Calling action.Undo(...) runs the right version automatically.
    // That is polymorphism: one call, many behaviours.
    public abstract class UndoAction
    {
        public string Description { get; protected set; }

        public abstract void Undo(EmployeeLinkedList employees);
    }

    // Undo "Add employee"  =  remove that employee again
    public class AddEmployeeAction : UndoAction
    {
        private readonly Employee employee;

        public AddEmployeeAction(Employee employee)
        {
            this.employee = employee;
            Description = "Added employee " + employee.Name;
        }

        public override void Undo(EmployeeLinkedList employees)
        {
            employees.Remove(employee.Id);
        }
    }

    // Undo "Delete employee"  =  put the SAME employee object back (it still holds its certifications).
    // It goes back at the END of the list, because the linked list only appends. The employee
    // is all there; only their position in the unsorted view may differ.
    public class DeleteEmployeeAction : UndoAction
    {
        private readonly Employee employee;

        public DeleteEmployeeAction(Employee employee)
        {
            this.employee = employee;
            Description = "Deleted employee " + employee.Name;
        }

        public override void Undo(EmployeeLinkedList employees)
        {
            employees.Add(employee);
        }
    }

    // Undo "Add certification"  =  remove it from that employee
    public class AddCertAction : UndoAction
    {
        private readonly Employee employee;
        private readonly Certification cert;

        public AddCertAction(Employee employee, Certification cert)
        {
            this.employee = employee;
            this.cert = cert;
            Description = "Added " + cert.TypeName + " for " + employee.Name;
        }

        public override void Undo(EmployeeLinkedList employees)
        {
            employee.Certifications.Remove(cert);
        }
    }

    // Undo "Delete certification"  =  give it back to that employee
    public class DeleteCertAction : UndoAction
    {
        private readonly Employee employee;
        private readonly Certification cert;

        public DeleteCertAction(Employee employee, Certification cert)
        {
            this.employee = employee;
            this.cert = cert;
            Description = "Deleted " + cert.TypeName + " for " + employee.Name;
        }

        public override void Undo(EmployeeLinkedList employees)
        {
            employee.AddCertification(cert);
        }
    }
}
