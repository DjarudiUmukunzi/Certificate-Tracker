using System.Collections.Generic;

namespace CertTrack
{
    // One employee, plus every certification / license they hold.
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }

        // The name of this employee's manager.
        public string Manager { get; set; }

        public List<Certification> Certifications { get; private set; }

        public Employee(int id, string name, string department, string manager)
        {
            Id = id;
            Name = name;
            Department = department;
            Manager = manager;
            Certifications = new List<Certification>();
        }

        public void AddCertification(Certification certification)
        {
            Certifications.Add(certification);
        }
    }
}
