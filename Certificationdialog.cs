using System;
using System.Drawing;
using System.Windows.Forms;

namespace CertTrack
{
    // A pop-up that asks for one new certification for a given employee.
    // Same pattern as EmployeeDialog: show it, validate on OK, read Result.
    public class CertificationDialog : Form
    {
        private readonly ComboBox cmbType = new ComboBox();
        private readonly DateTimePicker dtpIssued = new DateTimePicker();
        private readonly DateTimePicker dtpExpires = new DateTimePicker();

        private readonly Employee employee;

        // All four CertType values, in a fixed order. The drop-down shows the friendly name
        // at position N, and types[N] is the real value behind it.
        private readonly CertType[] types = (CertType[])Enum.GetValues(typeof(CertType));

        public Certification Result { get; private set; }

        public CertificationDialog(Employee employee)
        {
            this.employee = employee;

            Text = "Add Certification";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 195);

            Label lblWho = new Label();
            lblWho.Text = "For: " + employee.Name + " (ID " + employee.Id + ")";
            lblWho.AutoSize = true;
            lblWho.Font = new Font(Font, FontStyle.Bold);
            lblWho.Location = new Point(20, 15);

            cmbType.DropDownStyle = ComboBoxStyle.DropDownList; // pick from the list, no typing
            foreach (CertType type in types)
            {
                cmbType.Items.Add(Certification.NameOf(type));
            }
            cmbType.SelectedIndex = 0;

            dtpIssued.Format = DateTimePickerFormat.Short;
            dtpIssued.Value = DateTime.Today;

            dtpExpires.Format = DateTimePickerFormat.Short;
            dtpExpires.Value = DateTime.Today.AddYears(1); // a starting guess: the user changes it

            AddRow("Certification:", cmbType, 50, 200);
            AddRow("Issue date:", dtpIssued, 85, 120);
            AddRow("Expiration date:", dtpExpires, 120, 120);

            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new Point(170, 155);
            btnOk.Width = 80;
            btnOk.Click += OnOkClicked;

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(260, 155);
            btnCancel.Width = 80;
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.Add(lblWho);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void AddRow(string labelText, Control input, int y, int width)
        {
            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Location = new Point(20, y + 3);

            input.Location = new Point(140, y);
            input.Width = width;

            Controls.Add(label);
            Controls.Add(input);
        }

        // Pseudocode:
        //   type = the certification chosen in the drop-down
        //   IF the expiration date is not after the issue date: complain and stay open
        //   IF this employee already has that certification type: complain and stay open
        //   build the Certification, set it as Result, close with OK
        private void OnOkClicked(object sender, EventArgs e)
        {
            CertType type = types[cmbType.SelectedIndex];
            DateTime issued = dtpIssued.Value.Date;
            DateTime expires = dtpExpires.Value.Date;

            if (expires <= issued)
            {
                MessageBox.Show("The expiration date must be after the issue date.", "Add Certification");
                return;
            }

            foreach (Certification existing in employee.Certifications)
            {
                if (existing.Type == type)
                {
                    MessageBox.Show(employee.Name + " already has "
                                  + Certification.NameOf(type) + " on file.\n\n"
                                  + "To record a renewal, delete the old one first, then add the new one.",
                                    "Add Certification");
                    return;
                }
            }

            Result = new Certification(type, issued, expires);
            DialogResult = DialogResult.OK;
        }
    }
}