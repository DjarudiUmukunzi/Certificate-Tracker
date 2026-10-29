using System;

namespace CertTrack
{
    // One certification or license that belongs to an employee.
    public class Certification
    {
        public CertType Type { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }

        public Certification(CertType type, DateTime issueDate, DateTime expirationDate)
        {
            Type = type;
            IssueDate = issueDate;
            ExpirationDate = expirationDate;
        }

        // A friendly label for screens and reports (the enum names aren't pretty).
        public string TypeName
        {
            get { return NameOf(Type); }
        }

        //  the label logic moved into a static method so the
        // "Add Certification" dialog can show friendly names in its drop-down list
        // before any Certification object exists.
        public static string NameOf(CertType type)
        {
            switch (type)
            {
                case CertType.CRMA: return "CRMA";
                case CertType.CprFirstAidAed: return "CPR / First Aid / AED";
                case CertType.SafetyCare: return "Safety Care";
                case CertType.DriverLicense: return "Driver License";
                default: return type.ToString();
            }
        }
    }
}