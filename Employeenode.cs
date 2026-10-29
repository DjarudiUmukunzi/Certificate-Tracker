namespace CertTrack
{
    // ONE link in the chain.
    //
    // A node holds two things:
    //   Data : the employee stored in this link
    //   Next : a reference to the node that comes after it (null = "I'm the last one")
    //
    // Picture a treasure hunt: each clue (node) contains a prize (Data) and tells
    // you where to find the next clue (Next).
    public class EmployeeNode
    {
        public Employee Data;
        public EmployeeNode Next;

        public EmployeeNode(Employee data)
        {
            Data = data;
            Next = null; // a brand-new node isn't connected to anything yet
        }
    }
}
