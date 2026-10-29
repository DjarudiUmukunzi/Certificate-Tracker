using System;

namespace CertTrack
{
    public enum UserRole
    {
        Manager,         // sees only their own team, read-only, receives their own alerts
        HR,              // sees everyone, edits everything, runs the alert system
        TrainingManager  // sees everyone, records/removes certifications, receives training alerts
    }

    // A person who can log in. The "what may this role do?" rules live HERE, in one place,
    // so the screens just ask  user.CanEditEmployees  instead of repeating  if (role == HR).
    // If the rules change, you change one file.
    public class UserAccount
    {
        public string Username { get; private set; }
        public string Password { get; private set; }  // PROTOTYPE ONLY: see the note on Accounts below
        public string DisplayName { get; private set; }
        public UserRole Role { get; private set; }

        // For managers only: the name that appears in the Manager column of their employees
        public string ManagerName { get; private set; }

        public UserAccount(string username, string password, string displayName, UserRole role, string managerName)
        {
            Username = username;
            Password = password;
            DisplayName = displayName;
            Role = role;
            ManagerName = managerName;
        }

        // ---- permissions -------------------------------------------------
        public bool CanSeeEveryone { get { return Role != UserRole.Manager; } }
        public bool CanEditEmployees { get { return Role == UserRole.HR; } }
        public bool CanEditCerts { get { return Role == UserRole.HR || Role == UserRole.TrainingManager; } }
        public bool CanUseFiles { get { return Role == UserRole.HR; } }     // Save / Load buttons
        public bool CanRunAlerts { get { return Role == UserRole.HR; } }    // Scan / Send / Reset

        // Which alert recipient name is "mine"? null = HR sees every message.
        public string InboxName
        {
            get
            {
                if (Role == UserRole.Manager) { return ManagerName; }
                if (Role == UserRole.TrainingManager) { return AlertService.TrainingName; }
                return null;
            }
        }

        public string RoleText
        {
            get
            {
                if (Role == UserRole.TrainingManager) { return "Training Manager"; }
                return Role.ToString();
            }
        }

        // Is this employee inside this user's view?
        public bool CanSee(Employee employee)
        {
            if (CanSeeEveryone) { return true; }
            return string.Equals(employee.Manager, ManagerName, StringComparison.OrdinalIgnoreCase);
        }
    }

    // The list of accounts and the login check.
     
    public static class Accounts
    {
        private static readonly UserAccount[] all = new UserAccount[]
        {
            new UserAccount("pat",      "pat123",   "Pat Morgan",       UserRole.Manager,         "Pat Morgan"),
            new UserAccount("dana",     "dana123",  "Dana Wells",       UserRole.Manager,         "Dana Wells"),
            new UserAccount("hr",       "hr123",    "HR Administrator", UserRole.HR,              null),
            new UserAccount("training", "train123", "Training Manager", UserRole.TrainingManager, null)
        };

        // Pseudocode:
        //   FOR each account
        //       IF the username matches (ignore case) AND the password matches exactly: return it
        //   RETURN null   (login failed)
        // Note: we give the SAME failure for "wrong user" and "wrong password", so an attacker
        // cannot learn which usernames exist.
        public static UserAccount Authenticate(string username, string password)
        {
            foreach (UserAccount account in all)
            {
                if (string.Equals(account.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)
                    && account.Password == password)
                {
                    return account;
                }
            }

            return null;
        }

        
        public static string DemoHint()
        {
            string hint = "Demo accounts (username / password):\r\n";
            foreach (UserAccount account in all)
            {
                hint += "  " + account.Username + " / " + account.Password + "   (" + account.RoleText + ")\r\n";
            }
            return hint;
        }
    }
}
