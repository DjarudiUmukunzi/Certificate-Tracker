using System;
using System.Collections.Generic;

namespace CertTrack
{
    // One flat line of the dashboard: "this employee, this certification, this many days left".
    // We flatten the linked list into a List of these so we can SORT the lines.
    // (Sorting a linked list in place is awkward; sorting a list of lines is easy.)
    public class CertRow
    {
        public Employee Employee;
        public Certification Certification; // null = employee with no certifications yet
        public int Days;                    // days remaining; int.MaxValue when there is no certification

        public CertRow(Employee employee, Certification certification, int days)
        {
            Employee = employee;
            Certification = certification;
            Days = days;
        }
    }

    // Hand-written sorting and searching. No List.Sort, no LINQ, no Array.BinarySearch:
    // the point of this class is that YOU can see every comparison.
    public static class SortSearch
    {
        // ---------------------------------------------------------------
        // SORT 1: insertion sort by days remaining (most urgent first)
        // ---------------------------------------------------------------
        //
        //
        // Pseudocode:
        //   FOR i = 1 TO last index
        //       key = rows[i]                      (the card we just picked up)
        //       j   = i - 1
        //       WHILE j >= 0 AND rows[j].Days > key.Days
        //           rows[j + 1] = rows[j]          (shift the bigger card one place right)
        //           j = j - 1
        //       rows[j + 1] = key                  (drop the card into the gap)
        
        public static void SortByDaysRemaining(List<CertRow> rows)
        {
            for (int i = 1; i < rows.Count; i++)
            {
                CertRow key = rows[i];
                int j = i - 1;

                while (j >= 0 && rows[j].Days > key.Days)
                {
                    rows[j + 1] = rows[j];
                    j--;
                }

                rows[j + 1] = key;
            }
        }

        // ---------------------------------------------------------------
        // SORT 2: insertion sort of employees by ID (needed for binary search)
        // ---------------------------------------------------------------
        // Same algorithm as above, on an array of employees, comparing Id.
        public static Employee[] ToArraySortedById(EmployeeLinkedList employees)
        {
            // Step 1: copy the linked list into an array (an array allows jumping to the middle)
            Employee[] array = new Employee[employees.Count];
            int index = 0;
            foreach (Employee emp in employees)
            {
                array[index] = emp;
                index++;
            }

            // Step 2: insertion sort on Id
            for (int i = 1; i < array.Length; i++)
            {
                Employee key = array[i];
                int j = i - 1;

                while (j >= 0 && array[j].Id > key.Id)
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = key;
            }

            return array;
        }

        // ---------------------------------------------------------------
        // SEARCH 1: linear search by name (works on UNSORTED data)
        // ---------------------------------------------------------------
        // Real-life picture: reading a paper list from the top, line by line, until the end.
        //
        // Pseudocode:
        //   matches = empty list
        //   FOR each employee, from first to last
        //       IF the employee's name contains the text (ignoring upper/lower case)
        //           add the employee to matches
        //   RETURN matches
        //
        // Cost: O(n). It must look at every employee, because a name could be anywhere,
        // and we want ALL matches ("Sam" might match two people).
        public static List<Employee> FindByName(IEnumerable<Employee> employees, string text)
        {
            List<Employee> matches = new List<Employee>();
            string wanted = text.Trim().ToLower();

            if (wanted.Length == 0)
            {
                return matches;
            }

            foreach (Employee emp in employees)
            {
                if (emp.Name.ToLower().Contains(wanted))
                {
                    matches.Add(emp);
                }
            }

            return matches;
        }

        // ---------------------------------------------------------------
        // SEARCH 2: binary search by employee ID (needs data SORTED by Id)
        // ---------------------------------------------------------------
        // Real-life picture: guessing a number from 1 to 100. "Is it 50?" "Higher."
        // Each guess throws away half of what is left.
        //
        // Pseudocode:
        //   low = 0, high = last index
        //   WHILE low <= high
        //       mid = the middle index between low and high
        //       IF sorted[mid].Id == id  -> FOUND, return mid
        //       IF sorted[mid].Id <  id  -> the ID must be in the right half: low  = mid + 1
        //       ELSE                     -> the ID must be in the left half:  high = mid - 1
        //   RETURN -1   (not found)
        //
        // Cost: O(log n). 1,000,000 employees need at most about 20 comparisons.
        // "comparisons" is an out parameter so the screen can SHOW how few steps it took.
        public static Employee BinarySearchById(Employee[] sortedById, int id, out int comparisons)
        {
            comparisons = 0;
            int low = 0;
            int high = sortedById.Length - 1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2; // not (low+high)/2: that can overflow on huge arrays
                comparisons++;

                if (sortedById[mid].Id == id)
                {
                    return sortedById[mid];
                }

                if (sortedById[mid].Id < id)
                {
                    low = mid + 1;   // throw away the left half
                }
                else
                {
                    high = mid - 1;  // throw away the right half
                }
            }

            return null; // not found
        }
    }
}
