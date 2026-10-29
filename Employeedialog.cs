using System;
using System.Drawing;
using System.Windows.Forms;

namespace CertTrack
{
    // A small pop-up window that asks for a new employee's details.
    // Built in code, like the dashboard (no designer file).
    //
    // How a dialog works:
    //   1. The dashboard creates it and calls ShowDialog(). That pauses the dashboard until
    //      this window closes.
    //   2. If the user clicks OK and the input is valid, we set DialogResult = OK, which
    //      closes the window.
    //   3. The dashboard checks for OK and reads our Result property.
    public class EmployeeDialog : Form
    {
        private readonly TextBox txtId = new TextBox();
        private readonly TextBox txtName = new TextBox();
        private readonly TextBox txtDepartment = new TextBox();
        private readonly TextBox txtManager = new TextBox();

        // We keep the list so we can reject a duplicate ID BEFORE the window closes.
        private readonly EmployeeLinkedList existing;

        // The new employee, filled in only when the user clicked OK with valid input.
        public Employee Result { get; private set; }

        public EmployeeDialog(EmployeeLinkedList existing)
        {
            this.existing = existing;

            Text = "Add Employee";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(340, 205);

            AddRow("Employee ID:", txtId, 20);
            AddRow("Name:", txtName, 55);
            AddRow("Department:", txtDepartment, 90);
            AddRow("Manager:", txtManager, 125);

            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new Point(150, 165);
            btnOk.Width = 80;
            btnOk.Click += OnOkClicked;

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(240, 165);
            btnCancel.Width = 80;
            btnCancel.DialogResult = DialogResult.Cancel; // a Cancel button closes the window by itself

            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;     // pressing Enter clicks OK
            CancelButton = btnCancel; // pressing Esc clicks Cancel
        }

        // Puts a label and a text box side by side at the given height.
        private void AddRow(string labelText, TextBox box, int y)
        {
            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Location = new Point(20, y + 3);

            box.Location = new Point(120, y);
            box.Width = 200;

            Controls.Add(label);
            Controls.Add(box);
        }

        // Validate EVERYTHING before closing. Each check shows a message and returns,
        // leaving the window open so the user can fix it.
        private void OnOkClicked(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(txtId.Text.Trim(), out id) || id <= 0)
            {
                MessageBox.Show("Employee ID must be a positive whole number.", "Add Employee");
                txtId.Focus();
                return;
            }

            if (existing.FindById(id) != null)
            {
                MessageBox.Show("An employee with ID " + id + " already exists.", "Add Employee");
                txtId.Focus();
                return;
            }

            string name = txtName.Text.Trim();
            string department = txtDepartment.Text.Trim();
            string manager = txtManager.Text.Trim();

            if (name == "" || department == "" || manager == "")
            {
                MessageBox.Show("Please fill in the name, department, and manager.", "Add Employee");
                return;
            }

            Result = new Employee(id, name, department, manager);
            DialogResult = DialogResult.OK; // closes the window
        }
    }
}
