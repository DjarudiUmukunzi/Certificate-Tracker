using System;
using System.Collections.Generic;

namespace CertTrack
{
    
    
    // Expiration dates are set relative to "today", so the dashboard always shows a
    // mix of green, yellow, red and expired no matter what day you run it.
    // (Phase 2 replaces this with data loaded from a CSV file.)
    public static class SampleData
    {
        public static List<Employee> Create(DateTime today)
        {
            List<Employee> employees = new List<Employee>();

            Employee alex = new Employee(1001, "Alex Rivera", "Residential", "Pat Morgan");
            alex.AddCertification(MakeCert(CertType.CRMA, today, 200));
            alex.AddCertification(MakeCert(CertType.CprFirstAidAed, today, 45));
            alex.AddCertification(MakeCert(CertType.DriverLicense, today, 12));
            employees.Add(alex);

            Employee jordan = new Employee(1002, "Jordan Lee", "Residential", "Pat Morgan");
            jordan.AddCertification(MakeCert(CertType.CRMA, today, 85));
            jordan.AddCertification(MakeCert(CertType.SafetyCare, today, -5));
            employees.Add(jordan);

            Employee sam = new Employee(1003, "Sam Patel", "Day Program", "Dana Wells");
            sam.AddCertification(MakeCert(CertType.CprFirstAidAed, today, 320));
            sam.AddCertification(MakeCert(CertType.SafetyCare, today, 58));
            sam.AddCertification(MakeCert(CertType.DriverLicense, today, 400));
            employees.Add(sam);

            Employee taylor = new Employee(1004, "Taylor Brooks", "Transportation", "Dana Wells");
            taylor.AddCertification(MakeCert(CertType.DriverLicense, today, 28));
            taylor.AddCertification(MakeCert(CertType.CprFirstAidAed, today, 90));
            employees.Add(taylor);

            Employee morgan = new Employee(1005, "Morgan Diaz", "Transportation", "Dana Wells");
            morgan.AddCertification(MakeCert(CertType.DriverLicense, today, 0));
            morgan.AddCertification(MakeCert(CertType.SafetyCare, today, 150));
            employees.Add(morgan);

            Employee casey = new Employee(1006, "Casey Nguyen", "Residential", "Pat Morgan");
            casey.AddCertification(MakeCert(CertType.CRMA, today, 31));
            casey.AddCertification(MakeCert(CertType.CprFirstAidAed, today, 260));
            employees.Add(casey);

            return employees;
        }

        // Builds a certification that expires "daysFromToday" days from today.
        // Negative = already expired. Issue date is assumed to be 2 years earlier.
        private static Certification MakeCert(CertType type, DateTime today, int daysFromToday)
        {
            DateTime expiration = today.Date.AddDays(daysFromToday);
            DateTime issued = expiration.AddYears(-2);
            return new Certification(type, issued, expiration);
        }
    }
}
