using System;

namespace CertTrack
{
    // Who is being told, and how serious is it?
    public enum AlertLevel
    {
        ManagerAlert, // 90 days or fewer left: the employee's manager gets an early heads-up
        Escalation    // 30 days or fewer left (or already expired): HR and the Training Manager are told too
    }

    // One message waiting to be sent (or already sent).
    // A plain "data holder" class: it stores facts and does no work of its own.
    public class Notification
    {
        public string Recipient;      // e.g. "Pat Morgan", "HR", "Training Manager"
        public AlertLevel Level;
        public string Message;        // the sentence the recipient would read
        public int EmployeeId;
        public string EmployeeName;
        public string CertName;
        public DateTime ExpirationDate;
        public int DaysRemaining;     // measured on the simulated "today" when the scan ran
        public DateTime SentOn;       // filled in when the message leaves the queue

        // A unique fingerprint: "this recipient, about this certificate, at this level".
        // It is how we make sure the same warning is never queued twice.
        // If the certificate is renewed, the expiration date changes, so the key changes,
        // and the NEW certificate can raise fresh alerts later.
        public string Key;

        public string LevelText
        {
            get { return Level == AlertLevel.ManagerAlert ? "Manager alert (90 days)" : "ESCALATION (30 days)"; }
        }
    }
}
