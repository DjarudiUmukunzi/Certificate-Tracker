using System;
using System.Drawing;
using System.Windows.Forms;

namespace CertTrack
{
    // The login window. Shown before the dashboard. Built in code (no designer file).
    // On success: LoggedInUser holds the account and DialogResult is OK.
    public class LoginForm : Form
    {
        private readonly TextBox txtUser = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly Button btnLogin = new Button();
        private readonly Button btnCancel = new Button();
        private readonly Label lblError = new Label();
        private int failedAttempts = 0;

        public UserAccount LoggedInUser { get; private set; }

        public LoginForm()
        {
            Text = "CertTrack - Login";
            ClientSize = new Size(420, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label title = new Label();
            title.Text = "CertTrack";
            title.Font = new Font(Font.FontFamily, 16, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(20, 14);

            Label lblUser = new Label();
            lblUser.Text = "Username";
            lblUser.AutoSize = true;
            lblUser.Location = new Point(20, 62);
            txtUser.Location = new Point(110, 58);
            txtUser.Width = 200;

            Label lblPass = new Label();
            lblPass.Text = "Password";
            lblPass.AutoSize = true;
            lblPass.Location = new Point(20, 96);
            txtPassword.Location = new Point(110, 92);
            txtPassword.Width = 200;
            txtPassword.UseSystemPasswordChar = true; // shows dots instead of the letters

            btnLogin.Text = "Log in";
            btnLogin.Location = new Point(110, 128);
            btnLogin.Width = 90;

            btnCancel.Text = "Exit";
            btnCancel.Location = new Point(210, 128);
            btnCancel.Width = 90;
            btnCancel.DialogResult = DialogResult.Cancel;

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = true;
            lblError.Location = new Point(110, 162);

            TextBox hint = new TextBox();
            hint.Multiline = true;
            hint.ReadOnly = true;
            hint.Text = Accounts.DemoHint();
            hint.Location = new Point(20, 190);
            hint.Size = new Size(380, 90);
            hint.BackColor = SystemColors.Control;
            hint.BorderStyle = BorderStyle.None;

            Controls.Add(title);
            Controls.Add(lblUser);
            Controls.Add(txtUser);
            Controls.Add(lblPass);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
            Controls.Add(btnCancel);
            Controls.Add(lblError);
            Controls.Add(hint);

            AcceptButton = btnLogin; // Enter key = click "Log in"
            CancelButton = btnCancel; // Esc key = click "Exit"

            btnLogin.Click += OnLoginClicked;
        }

        // Pseudocode:
        //   IF username or password is empty: say so and stop
        //   account = Accounts.Authenticate(username, password)
        //   IF account is null: count the failure, clear the password, show an error
        //   ELSE: remember the account, close with OK
        private void OnLoginClicked(object sender, EventArgs e)
        {
            if (txtUser.Text.Trim().Length == 0 || txtPassword.Text.Length == 0)
            {
                lblError.Text = "Enter a username and a password.";
                return;
            }

            UserAccount account = Accounts.Authenticate(txtUser.Text, txtPassword.Text);

            if (account == null)
            {
                failedAttempts++;
                txtPassword.Clear();
                txtPassword.Focus();
                lblError.Text = "Wrong username or password (attempt " + failedAttempts + " of 3).";

                if (failedAttempts >= 3)
                {
                    MessageBox.Show("Too many failed attempts. The program will close.", "CertTrack");
                    DialogResult = DialogResult.Cancel;
                }
                return;
            }

            LoggedInUser = account;
            DialogResult = DialogResult.OK;
        }
    }
}