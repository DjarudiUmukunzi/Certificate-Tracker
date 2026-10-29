using System;
using System.Windows.Forms;

namespace CertTrack
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Pseudocode:
            //   LOOP forever
            //       show the login window; IF the user cancels / exits: end the program
            //       open the dashboard for that user and wait until it closes
            //       IF it closed because of "Log out": go round the loop (login again)
            //       ELSE (window closed): end the program
            while (true)
            {
                UserAccount user;
                using (LoginForm login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    user = login.LoggedInUser;
                }

                DashboardForm dashboard = new DashboardForm(user);
                Application.Run(dashboard);

                if (!dashboard.LoggedOut)
                {
                    return;
                }
            }
        }
    }
}