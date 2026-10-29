using System;
using System.Drawing;
using System.Windows.Forms;

namespace CertTrack
{
    // The alerts screen. It shows the QUEUE (waiting messages, front = next to be sent)
    // and the LOG (messages already sent). It contains no rules; AlertService does the work.
    // Built in code, like the other screens: no designer file.
    public class AlertsForm : Form
    {
        private readonly AlertService alerts;
        private readonly EmployeeLinkedList employees;
        private readonly DateTime today; // the simulated date chosen on the dashboard
        private readonly UserAccount user; // NEW in Phase 5: who is looking decides what they see and may do

        private readonly Button btnScan = new Button();
        private readonly Button btnSendNext = new Button();
        private readonly Button btnSendAll = new Button();
        private readonly Button btnReset = new Button();
        private readonly Label lblStatus = new Label();
        private readonly DataGridView pendingGrid = new DataGridView();
        private readonly DataGridView logGrid = new DataGridView();
        private readonly Label lblPending = new Label();

        public AlertsForm(AlertService alerts, EmployeeLinkedList employees, DateTime today, UserAccount user)
        {
            this.user = user;
            this.alerts = alerts;
            this.employees = employees;
            this.today = today;

            Text = "CertTrack - Alerts and Escalations";
            Width = 1000;
            Height = 660;
            StartPosition = FormStartPosition.CenterParent;

            BuildLayout();
            ApplyRole();
            RefreshAll();

            btnScan.Click += OnScanClicked;
            btnSendNext.Click += OnSendNextClicked;
            btnSendAll.Click += OnSendAllClicked;
            btnReset.Click += OnResetClicked;
        }

        private void BuildLayout()
        {
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 80;

            Label lblToday = new Label();
            lblToday.Text = "Simulated today: " + today.ToString("yyyy-MM-dd")
                          + "     Manager alert at " + StatusCalculator.ManagerAlertDays
                          + " days, HR + Training escalation at " + StatusCalculator.RedDays + " days";
            lblToday.AutoSize = true;
            lblToday.Location = new Point(12, 12);
            lblToday.Font = new Font(Font, FontStyle.Bold);

            btnScan.Text = "1. Scan for alerts";
            btnScan.Location = new Point(12, 40);
            btnScan.Width = 130;

            btnSendNext.Text = "2. Send next";
            btnSendNext.Location = new Point(152, 40);
            btnSendNext.Width = 110;

            btnSendAll.Text = "Send all";
            btnSendAll.Location = new Point(272, 40);
            btnSendAll.Width = 90;

            btnReset.Text = "Reset history";
            btnReset.Location = new Point(372, 40);
            btnReset.Width = 110;

            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(500, 45);

            topBar.Controls.Add(lblToday);
            topBar.Controls.Add(btnScan);
            topBar.Controls.Add(btnSendNext);
            topBar.Controls.Add(btnSendAll);
            topBar.Controls.Add(btnReset);
            topBar.Controls.Add(lblStatus);

            lblPending.AutoSize = false;
            lblPending.Dock = DockStyle.Top;
            lblPending.Height = 24;
            lblPending.Padding = new Padding(12, 5, 0, 0);
            lblPending.Font = new Font(Font, FontStyle.Bold);

            ConfigureGrid(pendingGrid);
            pendingGrid.Dock = DockStyle.Top;
            pendingGrid.Height = 230;
            pendingGrid.Columns.Add("pPos", "Queue position");
            pendingGrid.Columns.Add("pTo", "To");
            pendingGrid.Columns.Add("pLevel", "Level");
            pendingGrid.Columns.Add("pMsg", "Message");
            pendingGrid.Columns["pPos"].FillWeight = 12;
            pendingGrid.Columns["pTo"].FillWeight = 14;
            pendingGrid.Columns["pLevel"].FillWeight = 17;
            pendingGrid.Columns["pMsg"].FillWeight = 57;

            Label lblLog = new Label();
            lblLog.Text = user.CanRunAlerts
                ? "Sent log (saved to certtrack_alerts.log)"
                : "Your inbox: messages sent to " + user.InboxName;
            lblLog.AutoSize = false;
            lblLog.Dock = DockStyle.Top;
            lblLog.Height = 24;
            lblLog.Padding = new Padding(12, 5, 0, 0);
            lblLog.Font = new Font(Font, FontStyle.Bold);

            ConfigureGrid(logGrid);
            logGrid.Dock = DockStyle.Fill;
            logGrid.Columns.Add("lDate", "Sent on");
            logGrid.Columns.Add("lTo", "To");
            logGrid.Columns.Add("lLevel", "Level");
            logGrid.Columns.Add("lMsg", "Message");
            logGrid.Columns["lDate"].FillWeight = 12;
            logGrid.Columns["lTo"].FillWeight = 14;
            logGrid.Columns["lLevel"].FillWeight = 17;
            logGrid.Columns["lMsg"].FillWeight = 57;

            // Fill first, then the Top controls in REVERSE of how they should appear (last added = highest).
            // Result from the top down: topBar, lblPending, pendingGrid, lblLog, logGrid
            Controls.Add(logGrid);
            Controls.Add(lblLog);
            Controls.Add(pendingGrid);
            Controls.Add(lblPending);
            Controls.Add(topBar);
        }

        private void ConfigureGrid(DataGridView g)
        {
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.BackgroundColor = SystemColors.Window;
            g.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        // NEW in Phase 5
        // HR (InboxName == null) sees every message. A manager or the Training Manager sees
        // only the messages addressed to them.
        private bool IsMine(Notification n)
        {
            string inbox = user.InboxName;
            return inbox == null || string.Equals(n.Recipient, inbox, StringComparison.OrdinalIgnoreCase);
        }

        // NEW in Phase 5
        // Only HR operates the alert system. Everyone else just reads their inbox, so the
        // queue and the action buttons are hidden for them.
        private void ApplyRole()
        {
            bool canRun = user.CanRunAlerts;
            btnScan.Visible = canRun;
            btnSendNext.Visible = canRun;
            btnSendAll.Visible = canRun;
            btnReset.Visible = canRun;
            lblStatus.Visible = canRun;
            lblPending.Visible = canRun;
            pendingGrid.Visible = canRun;
        }

        // Redraw both grids from the service's current data
        private void RefreshAll()
        {
            pendingGrid.Rows.Clear();
            int position = 1;
            foreach (Notification n in alerts.Pending())
            {
                if (IsMine(n))
                {
                    int i = pendingGrid.Rows.Add(position, n.Recipient, n.LevelText, n.Message);
                    ColorByLevel(pendingGrid.Rows[i], n.Level);
                }
                position++; // the real queue position, even if some rows are hidden
            }

            logGrid.Rows.Clear();
            // Newest first: walk the list backwards
            for (int k = alerts.Log.Count - 1; k >= 0; k--)
            {
                Notification n = alerts.Log[k];
                if (!IsMine(n))
                {
                    continue;
                }

                int i = logGrid.Rows.Add(n.SentOn.ToString("yyyy-MM-dd"), n.Recipient, n.LevelText, n.Message);
                ColorByLevel(logGrid.Rows[i], n.Level);
            }

            lblPending.Text = "Waiting in the queue: " + alerts.PendingCount
                            + "   (row 1 is the FRONT: it is sent first)";
            btnSendNext.Enabled = user.CanRunAlerts && alerts.PendingCount > 0;
            btnSendAll.Enabled = user.CanRunAlerts && alerts.PendingCount > 0;
        }

        private void ColorByLevel(DataGridViewRow row, AlertLevel level)
        {
            Color background = level == AlertLevel.Escalation
                ? Color.FromArgb(248, 215, 218)   // red tint
                : Color.FromArgb(255, 242, 204);  // yellow tint

            row.DefaultCellStyle.BackColor = background;
            row.DefaultCellStyle.ForeColor = Color.Black;
            row.DefaultCellStyle.SelectionBackColor = ControlPaint.Dark(background);
            row.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void WarnIfSaveFailed()
        {
            if (alerts.LastSaveError != null)
            {
                MessageBox.Show("The log file could not be saved:\n\n" + alerts.LastSaveError, "CertTrack");
            }
        }

        private void OnScanClicked(object sender, EventArgs e)
        {
            int queued = alerts.Scan(employees, today);
            RefreshAll();
            lblStatus.Text = queued == 0
                ? "Nothing new to queue (everything due was already queued or sent)."
                : queued + " new message(s) added to the back of the queue.";
        }

        private void OnSendNextClicked(object sender, EventArgs e)
        {
            Notification sent = alerts.SendNext(today);
            RefreshAll();
            WarnIfSaveFailed();
            lblStatus.Text = sent == null ? "The queue is empty." : "Sent to " + sent.Recipient + ".";
        }

        private void OnSendAllClicked(object sender, EventArgs e)
        {
            int sent = alerts.SendAll(today);
            RefreshAll();
            WarnIfSaveFailed();
            lblStatus.Text = sent + " message(s) sent.";
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                "Clear the queue and the sent log? The next scan will raise every alert again.",
                "Confirm reset", MessageBoxButtons.YesNo);

            if (answer == DialogResult.Yes)
            {
                alerts.ResetAll();
                RefreshAll();
                WarnIfSaveFailed();
                lblStatus.Text = "History cleared.";
            }
        }
    }
}