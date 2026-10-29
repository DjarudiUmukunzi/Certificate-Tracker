using System;

namespace CertTrack
{
    // All of the date and status rules live in ONE place, so every screen
    // (dashboard, alerts, reports) agrees on what "red" means.
    public static class StatusCalculator
    {
        // The thresholds from the project proposal
        public const int ManagerAlertDays = 90;  // manager gets an early heads-up (used in Phase 4)
        public const int YellowDays = 60;        // 60 days or fewer: yellow
        public const int RedDays = 30;           // 30 days or fewer: red, escalate to HR / Training

        // Whole days from "today" until the expiration date.
        // Negative means it already expired.
        // .Date strips the time of day, so 11:59 PM and 12:01 AM don't change the answer.
        public static int DaysRemaining(DateTime expirationDate, DateTime today)
        {
            return (expirationDate.Date - today.Date).Days;
        }

        // Pseudocode:
        //   IF days < 0:           Expired
        //   ELSE IF days <= 30:    Red
        //   ELSE IF days <= 60:    Yellow
        //   ELSE:                  Green
        //
        // Order matters: check the most urgent case first, so a value like 10 stops at
        // Red and never reaches the Yellow test.
        // Day 0 (expires today) counts as Red, not Expired: it's still valid today.
        public static CertStatus GetStatus(int daysRemaining)
        {
            if (daysRemaining < 0)
            {
                return CertStatus.Expired;
            }
            if (daysRemaining <= RedDays)
            {
                return CertStatus.Red;
            }
            if (daysRemaining <= YellowDays)
            {
                return CertStatus.Yellow;
            }
            return CertStatus.Green;
        }

        public static string StatusText(CertStatus status)
        {
            switch (status)
            {
                case CertStatus.Green: return "OK";
                case CertStatus.Yellow: return "Due soon";
                case CertStatus.Red: return "Urgent";
                case CertStatus.Expired: return "EXPIRED";
                default: return status.ToString();
            }
        }
    }
}
