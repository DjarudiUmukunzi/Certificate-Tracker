using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CertTrack
{
    // Saves and loads the employee data as a CSV text file.
    //
    // CSV = "comma-separated values": one record per line, fields separated by commas.
    // You can open the file in Notepad or Excel. It's the "file storage instead of a
    // database" part of the project.
    //
    // THE FILE FORMAT (one line per certification):
    //
    //   EmployeeId,Name,Department,Manager,CertType,IssueDate,ExpirationDate
    //   1001,Alex Rivera,Residential,Pat Morgan,CRMA,2024-09-30,2026-09-30
    //   1001,Alex Rivera,Residential,Pat Morgan,CprFirstAidAed,2024-12-01,2026-12-01
    //
    // An employee with 3 certifications takes 3 lines, and the employee fields repeat.
    // That's not elegant, but it keeps the file flat and easy to read in a spreadsheet.
    // An employee with NO certifications gets one line with the last three fields empty,
    // so they don't vanish from the file.
    public static class DataStore
    {
        public const string Header = "EmployeeId,Name,Department,Manager,CertType,IssueDate,ExpirationDate";

        // Always write dates the same way, whatever the computer's regional settings are.
        private const string DateFormat = "yyyy-MM-dd";

        // SAVE
        // Pseudocode:
        //   lines = a list that starts with the header line
        //   FOR each employee in the linked list
        //       IF the employee has no certifications: add ONE line with empty cert fields
        //       FOR each certification: add one line (employee fields + certification fields)
        //   write all the lines to the file (this replaces the old file)
        //   RETURN how many data lines were written (not counting the header)
        public static int Save(EmployeeLinkedList employees, string path)
        {
            List<string> lines = new List<string>();
            lines.Add(Header);

            foreach (Employee emp in employees)
            {
                if (emp.Certifications.Count == 0)
                {
                    lines.Add(BuildLine(emp, "", "", ""));
                }

                foreach (Certification cert in emp.Certifications)
                {
                    lines.Add(BuildLine(
                        emp,
                        cert.Type.ToString(), // the enum NAME ("CRMA"), so it can be turned back into an enum on load
                        cert.IssueDate.ToString(DateFormat, CultureInfo.InvariantCulture),
                        cert.ExpirationDate.ToString(DateFormat, CultureInfo.InvariantCulture)));
                }
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);

            return lines.Count - 1; // minus the header
        }

        
        
        // Pseudocode:
        //   employees = an empty linked list
        //   skipped = 0
        //   FOR each line AFTER the header
        //       IF the line is blank: ignore it
        //       IF the line can't be understood: skipped = skipped + 1
        //       ELSE the line has been added to the list
        //   RETURN employees   (and report how many lines were skipped)
        //
        // A bad line (wrong number of fields, a date that isn't a date, an unknown
        // certification type) is SKIPPED instead of crashing the whole app. That way one
        // typo in the file doesn't lock you out of everything else.
        public static EmployeeLinkedList Load(string path, out int skippedLines)
        {
            skippedLines = 0;
            EmployeeLinkedList employees = new EmployeeLinkedList();

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            for (int i = 1; i < lines.Length; i++) // start at 1 to skip the header line
            {
                if (lines[i].Trim() == "")
                {
                    continue; // blank lines are harmless, so don't count them as errors
                }

                if (!TryReadLine(lines[i], employees))
                {
                    skippedLines++;
                }
            }

            return employees;
        }

        // Turns ONE line of the file into an employee (and a certification, if the line has one).
        // Returns false if the line is not valid.
        //
        // Pseudocode:
        //   fields = split the line into its 7 fields (quote-aware)
        //   IF there aren't exactly 7 fields: RETURN false
        //   IF the employee ID isn't a whole number: RETURN false
        //   IF the certification columns are not empty
        //       check the type is real and both dates are real; RETURN false if not
        //   find the employee with this ID in the list, or create and add them
        //   IF there is a certification: add it to that employee
        //   RETURN true
        //
        // We validate the certification BEFORE touching the list, so a bad line can't
        // leave a half-added employee behind.
        private static bool TryReadLine(string line, EmployeeLinkedList employees)
        {
            List<string> fields = ParseLine(line);

            if (fields.Count != 7)
            {
                return false;
            }

            int id;
            if (!int.TryParse(fields[0], out id))
            {
                return false;
            }

            bool hasCert = fields[4] != "";
            CertType type = CertType.CRMA;
            DateTime issued = DateTime.MinValue;
            DateTime expires = DateTime.MinValue;

            if (hasCert)
            {
                // IsDefined with a STRING checks it against the enum's names (CRMA, SafetyCare...).
                // That also rejects numbers like "2", which Enum.TryParse alone would accept.
                if (!Enum.IsDefined(typeof(CertType), fields[4]))
                {
                    return false; // not one of our four certification types
                }
                type = (CertType)Enum.Parse(typeof(CertType), fields[4]);
                if (!DateTime.TryParseExact(fields[5], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out issued))
                {
                    return false;
                }
                if (!DateTime.TryParseExact(fields[6], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out expires))
                {
                    return false;
                }
            }

            
            Employee employee = employees.FindById(id);
            if (employee == null)
            {
                employee = new Employee(id, fields[1], fields[2], fields[3]);
                employees.Add(employee);
            }

            if (hasCert)
            {
                employee.AddCertification(new Certification(type, issued, expires));
            }

            return true;
        }

       
        private static List<string> ParseLine(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"'); // doubled quote = one real quote
                            i++;                 // skip the second one
                        }
                        else
                        {
                            inQuotes = false;    // closing quote
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            fields.Add(current.ToString()); // the last field has no comma after it
            return fields;
        }

        // Joins the seven fields of one line with commas.
        private static string BuildLine(Employee emp, string certType, string issueDate, string expirationDate)
        {
            string[] fields = new string[]
            {
                emp.Id.ToString(),
                Escape(emp.Name),
                Escape(emp.Department),
                Escape(emp.Manager),
                certType,
                issueDate,
                expirationDate
            };

            return string.Join(",", fields);
        }

        // PROBLEM: what if a name is "Rivera, Alex"? The comma would be read as a field
        // separator and the line would have one field too many.
        // FIX (the standard CSV rule): wrap any field that contains a comma, a double
        // quote, or a line break in double quotes, and double up any quote inside it.
        //
        //   Rivera, Alex   ->  "Rivera, Alex"
        //   The "Boss"     ->  "The ""Boss"""
        //   Alex Rivera    ->  Alex Rivera        (nothing special, left alone)
        private static string Escape(string field)
        {
            if (field == null)
            {
                return "";
            }

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}