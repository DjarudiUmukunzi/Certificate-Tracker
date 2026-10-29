using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CertTrack
{
    // The "brain" of the alert system. It owns:
    //   - the queue of messages waiting to be sent
    //   - the log of messages already sent (saved to a text file so it survives restarts)
    //   - a set of keys, so the same warning is never queued twice
    //
    // The screen (AlertsForm) only shows what this class holds. All the rules live here,
    // which also means they could be tested without any window.
    public class AlertService
    {
        public const string HrName = "HR";
        public const string TrainingName = "Training Manager";

        // The 30-day escalation uses the same boundary as the red status.
        private const int EscalationDays = StatusCalculator.RedDays;

        private readonly NotificationQueue queue = new NotificationQueue();
        private readonly List<Notification> log = new List<Notification>();

        // HashSet = a collection with no duplicates and a very fast "do you contain X?" test.
        // Every key that is waiting in the queue OR already in the log lives in here.
        private readonly HashSet<string> knownKeys = new HashSet<string>();

        private readonly string logPath;

        // Set when the log file could not be written (e.g. it is open in another program)
        public string LastSaveError { get; private set; }

        public AlertService(string logPath)
        {
            this.logPath = logPath;
            LoadLog();
        }

        public int PendingCount
        {
            get { return queue.Count; }
        }

        public IEnumerable<Notification> Pending()
        {
            return queue.Items();
        }

        public List<Notification> Log
        {
            get { return log; }
        }

        // ---------------------------------------------------------------
        // 1. SCAN: look for certificates that need a message, and queue them
        // ---------------------------------------------------------------
        // Pseudocode:
        //   lines = every certificate with 90 days or fewer left (expired ones included)
        //   sort lines, most urgent first           <-- reuses the Phase 3 insertion sort
        //   FOR each line (most urgent first)
        //       wanted = [ the manager ]
        //       IF days <= 30: also HR and the Training Manager
        //       FOR each recipient in wanted
        //           build the notification and its key
        //           IF the key has not been seen before: enqueue it
        //   RETURN how many new messages were queued
        //
        // Because the lines are sorted first, the queue is filled in urgency order:
        // the person with the nearest deadline is at the FRONT and gets served first.
        public int Scan(IEnumerable<Employee> employees, DateTime today)
        {
            List<CertRow> due = new List<CertRow>();

            foreach (Employee emp in employees)
            {
                foreach (Certification cert in emp.Certifications)
                {
                    int days = StatusCalculator.DaysRemaining(cert.ExpirationDate, today);
                    if (days <= StatusCalculator.ManagerAlertDays)
                    {
                        due.Add(new CertRow(emp, cert, days));
                    }
                }
            }

            SortSearch.SortByDaysRemaining(due);

            int queued = 0;
            foreach (CertRow line in due)
            {
                // The manager is always told once we are inside the 90-day window
                queued += TryQueue(line, AlertLevel.ManagerAlert, line.Employee.Manager) ? 1 : 0;

                // Inside the 30-day window (or expired), HR and the Training Manager are told as well
                if (line.Days <= EscalationDays)
                {
                    queued += TryQueue(line, AlertLevel.Escalation, HrName) ? 1 : 0;
                    queued += TryQueue(line, AlertLevel.Escalation, TrainingName) ? 1 : 0;
                }
            }

            return queued;
        }

        private bool TryQueue(CertRow line, AlertLevel level, string recipient)
        {
            string certName = line.Certification.TypeName;
            string expires = line.Certification.ExpirationDate.ToString("yyyy-MM-dd");

            string key = line.Employee.Id + "|" + line.Certification.Type + "|" + expires
                       + "|" + level + "|" + recipient;

            // HashSet.Add returns false if the key was already there. That single call is
            // both the "have we seen it?" test and the "remember it" step.
            if (!knownKeys.Add(key))
            {
                return false;
            }

            Notification note = new Notification();
            note.Recipient = recipient;
            note.Level = level;
            note.EmployeeId = line.Employee.Id;
            note.EmployeeName = line.Employee.Name;
            note.CertName = certName;
            note.ExpirationDate = line.Certification.ExpirationDate;
            note.DaysRemaining = line.Days;
            note.Key = key;
            note.Message = BuildMessage(note, line.Employee);

            queue.Enqueue(note);
            return true;
        }

        private string BuildMessage(Notification note, Employee emp)
        {
            string when;
            if (note.DaysRemaining < 0)
            {
                when = "EXPIRED " + (-note.DaysRemaining) + " day(s) ago (" + note.ExpirationDate.ToString("yyyy-MM-dd") + ")";
            }
            else if (note.DaysRemaining == 0)
            {
                when = "expires TODAY (" + note.ExpirationDate.ToString("yyyy-MM-dd") + ")";
            }
            else
            {
                when = "expires in " + note.DaysRemaining + " day(s) (" + note.ExpirationDate.ToString("yyyy-MM-dd") + ")";
            }

            string subject = emp.Name + " (ID " + emp.Id + ", " + emp.Department + "): " + note.CertName + " " + when + ". ";

            if (note.Level == AlertLevel.ManagerAlert)
            {
                return subject + "Please make sure the renewal is scheduled.";
            }

            if (note.Recipient == HrName)
            {
                return subject + "Escalated to HR: renewal is overdue or close to the deadline.";
            }

            return subject + "Escalated to Training: please book a renewal session.";
        }

        // ---------------------------------------------------------------
        // 2. SEND: take messages off the FRONT of the queue
        // ---------------------------------------------------------------
        // In this prototype "sending" means writing the message into the log. A real system
        // would call an email service at exactly this point; nothing else would change.
        //
        // Pseudocode:
        //   IF the queue is empty: return null
        //   note = queue.Dequeue()                  <-- the oldest, most urgent message
        //   note.SentOn = the simulated today
        //   add note to the log, save the log
        //   RETURN note
        public Notification SendNext(DateTime today)
        {
            if (queue.IsEmpty)
            {
                return null;
            }

            Notification note = queue.Dequeue();
            note.SentOn = today;
            log.Add(note);
            SaveLog();
            return note;
        }

        public int SendAll(DateTime today)
        {
            int sent = 0;
            while (!queue.IsEmpty)
            {
                SendNext(today);
                sent++;
            }
            return sent;
        }

        // Wipes the queue, the log and the "already seen" keys. Handy for demos: after a reset,
        // the next scan raises every alert again.
        public void ResetAll()
        {
            queue.Clear();
            log.Clear();
            knownKeys.Clear();
            SaveLog();
        }

        // ---------------------------------------------------------------
        // 3. The log file: one line per sent message, fields separated by TAB characters
        // ---------------------------------------------------------------
        // Format:  date <TAB> level <TAB> recipient <TAB> key <TAB> message
        // (Tabs are used instead of commas because messages contain commas.)
        private static string Clean(string text)
        {
            return text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }

        private void SaveLog()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (Notification n in log)
                {
                    sb.AppendLine(n.SentOn.ToString("yyyy-MM-dd") + "\t" + n.Level + "\t"
                                + Clean(n.Recipient) + "\t" + Clean(n.Key) + "\t" + Clean(n.Message));
                }

                File.WriteAllText(logPath, sb.ToString());
                LastSaveError = null;
            }
            catch (IOException ex)
            {
                LastSaveError = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                LastSaveError = ex.Message;
            }
        }

        // Pseudocode:
        //   IF there is no file: nothing to load
        //   FOR each line: split on TAB; IF it does not have 5 parts or the date/level is bad: skip it
        //       rebuild the notification, add it to the log, remember its key
        private void LoadLog()
        {
            if (!File.Exists(logPath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(logPath))
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 5)
                {
                    continue;
                }

                DateTime sentOn;
                if (!DateTime.TryParse(parts[0], out sentOn))
                {
                    continue;
                }

                if (!Enum.IsDefined(typeof(AlertLevel), parts[1]))
                {
                    continue;
                }

                Notification note = new Notification();
                note.SentOn = sentOn;
                note.Level = (AlertLevel)Enum.Parse(typeof(AlertLevel), parts[1]);
                note.Recipient = parts[2];
                note.Key = parts[3];
                note.Message = parts[4];

                log.Add(note);
                knownKeys.Add(note.Key);
            }
        }
    }
}
