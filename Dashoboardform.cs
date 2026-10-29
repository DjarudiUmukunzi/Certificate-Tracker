using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CertTrack
{
    // The main screen: a grid with one row per certification, colored by urgency.
   
    public class DashboardForm : Form
    {
        private readonly DataGridView grid = new DataGridView();
        private readonly DateTimePicker dtpToday = new DateTimePicker();
        private readonly Button btnResetToday = new Button();
        private readonly Button btnSaveCsv = new Button();
        private readonly Button btnLoadCsv = new Button();
        private readonly Button btnAddEmployee = new Button();      
        private readonly Button btnAddCert = new Button();          
        private readonly Button btnDeleteCert = new Button();       
        private readonly Button btnDeleteEmployee = new Button();  
        private readonly Label lblSummary = new Label();

        // sorting and searching controls
        private readonly CheckBox chkUrgentFirst = new CheckBox();
        private readonly TextBox txtSearch = new TextBox();
        private readonly Button btnFindName = new Button();
        private readonly Button btnFindId = new Button();

        private EmployeeLinkedList employees;

        // The alert queue and sent log live here, so they survive closing
        // and reopening the Alerts window.
        private readonly Button btnAlerts = new Button();
        private AlertService alerts;

        // who is logged in, the undo stack, and the two new buttons
        private readonly UserAccount user;
        private readonly UndoStack undo = new UndoStack();
        private readonly Button btnUndo = new Button();
        private readonly Button btnLogout = new Button();
        private readonly ToolTip tip = new ToolTip();

        // Tells Program.cs whether the window closed because of "Log out" (show the login
        // again) or because the user closed the window (end the program).
        public bool LoggedOut { get; private set; }

        //The employees copied into an array and sorted by ID, ready for
        // binary search. It is built the first time you search and thrown away (set to null)
        // whenever the data changes, so it is never out of date.
        private Employee[] sortedById;

        // lets repeated clicks on "Find Name" walk through several matches
        private string lastNameQuery = "";
        private int nameMatchIndex = 0;

        // NEW in Step 2.4
        // Each grid row remembers WHICH employee and WHICH certification it was drawn from.
        // That's how "Delete" knows what to delete when the user clicks a row.
        // It's stored in the row's Tag property, which WinForms keeps free for our own use.
        // Certification is null for an employee who has no certifications yet.
        private class RowRef
        {
            public Employee Employee;
            public Certification Certification;

            public RowRef(Employee employee, Certification certification)
            {
                Employee = employee;
                Certification = certification;
            }
        }

        public DashboardForm(UserAccount user)
        {
            this.user = user;
            Text = "CertTrack - " + user.DisplayName + " (" + user.RoleText + ")";
            Width = 1050;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            ApplyPermissions(); // NEW in Phase 5: grey out what this role may not do

            // NEW in Phase 4: load the log of already-sent alerts (if the file exists)
            alerts = new AlertService(Path.Combine(Application.StartupPath, "certtrack_alerts.log"));

            // Start from the saved file if there is one, otherwise from sample data
            if (File.Exists(CsvPath))
            {
                LoadFromCsv(false);
            }
            else
            {
                employees = new EmployeeLinkedList();
                foreach (Employee sample in SampleData.Create(DateTime.Today))
                {
                    employees.Add(sample);
                }
            }
            RefreshGrid();

            // Wire the events AFTER the first refresh so the picker doesn't trigger it twice.
            dtpToday.ValueChanged += OnTodayChanged;
            btnResetToday.Click += OnResetClicked;
            btnSaveCsv.Click += OnSaveClicked;
            btnLoadCsv.Click += OnLoadClicked;
            btnAddEmployee.Click += OnAddEmployeeClicked;       
            btnAddCert.Click += OnAddCertClicked;               
            btnDeleteCert.Click += OnDeleteCertClicked;         
            btnDeleteEmployee.Click += OnDeleteEmployeeClicked; 
            chkUrgentFirst.CheckedChanged += OnSortChanged;     
            btnFindName.Click += OnFindNameClicked;             
            btnFindId.Click += OnFindIdClicked;                 
            txtSearch.KeyDown += OnSearchKeyDown;               
            btnAlerts.Click += OnAlertsClicked;                 
            btnUndo.Click += OnUndoClicked;                     
            btnLogout.Click += OnLogoutClicked;                 
        }

        // ---------------------------------------------------------------
        // Layout
        // ---------------------------------------------------------------
        private void BuildLayout()
        {
            // Top bar, three rows:
            //   row 1: simulated date, reset, save, load
            //   row 2: add / delete buttons         
            //   row 3: color counts
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 110; // CHANGED in Step 2.4: was 80, now three rows

            Label lblToday = new Label();
            lblToday.Text = "Today (simulated):";
            lblToday.AutoSize = true;
            lblToday.Location = new Point(12, 17);

            dtpToday.Format = DateTimePickerFormat.Short;
            dtpToday.Value = DateTime.Today;
            dtpToday.Location = new Point(135, 13);
            dtpToday.Width = 110;

            btnResetToday.Text = "Reset to today";
            btnResetToday.Location = new Point(255, 11);
            btnResetToday.Width = 110;

            btnSaveCsv.Text = "Save to CSV";
            btnSaveCsv.Location = new Point(375, 11);
            btnSaveCsv.Width = 110;

            btnLoadCsv.Text = "Load from CSV";
            btnLoadCsv.Location = new Point(495, 11);
            btnLoadCsv.Width = 110;

            // NEW in Step 2.4: row 2, the edit buttons
            btnAddEmployee.Text = "Add Employee";
            btnAddEmployee.Location = new Point(12, 46);
            btnAddEmployee.Width = 120;

            btnAddCert.Text = "Add Certification";
            btnAddCert.Location = new Point(142, 46);
            btnAddCert.Width = 130;

            btnDeleteCert.Text = "Delete Certification";
            btnDeleteCert.Location = new Point(282, 46);
            btnDeleteCert.Width = 140;

            btnDeleteEmployee.Text = "Delete Employee";
            btnDeleteEmployee.Location = new Point(432, 46);
            btnDeleteEmployee.Width = 130;

            // NEW in Phase 3: sort checkbox on row 2, search box and buttons on row 1 (right side)
            chkUrgentFirst.Text = "Most urgent first";
            chkUrgentFirst.AutoSize = true;
            chkUrgentFirst.Location = new Point(590, 50);

            Label lblSearch = new Label();
            lblSearch.Text = "Search:";
            lblSearch.AutoSize = true;
            lblSearch.Location = new Point(625, 17);

            txtSearch.Location = new Point(680, 13);
            txtSearch.Width = 130;

            btnFindName.Text = "Find Name";
            btnFindName.Location = new Point(820, 11);
            btnFindName.Width = 90;

            btnFindId.Text = "Find ID";
            btnFindId.Location = new Point(920, 11);
            btnFindId.Width = 80;

            // NEW in Phase 4: opens the alerts window
            btnAlerts.Text = "Alerts...";
            btnAlerts.Location = new Point(720, 46);
            btnAlerts.Width = 100;
            topBar.Controls.Add(btnAlerts);

            // NEW in Phase 5: undo and log out
            btnUndo.Text = "Undo";
            btnUndo.Location = new Point(830, 46);
            btnUndo.Width = 90;
            btnLogout.Text = "Log out";
            btnLogout.Location = new Point(930, 46);
            btnLogout.Width = 90;
            topBar.Controls.Add(btnUndo);
            topBar.Controls.Add(btnLogout);

            topBar.Controls.Add(chkUrgentFirst);
            topBar.Controls.Add(lblSearch);
            topBar.Controls.Add(txtSearch);
            topBar.Controls.Add(btnFindName);
            topBar.Controls.Add(btnFindId);

            lblSummary.AutoSize = true;
            lblSummary.Location = new Point(12, 82); // CHANGED in Step 2.4: moved down to row 3
            lblSummary.Font = new Font(Font, FontStyle.Bold);

            topBar.Controls.Add(lblToday);
            topBar.Controls.Add(dtpToday);
            topBar.Controls.Add(btnResetToday);
            topBar.Controls.Add(btnSaveCsv);
            topBar.Controls.Add(btnLoadCsv);
            topBar.Controls.Add(btnAddEmployee);
            topBar.Controls.Add(btnAddCert);
            topBar.Controls.Add(btnDeleteCert);
            topBar.Controls.Add(btnDeleteEmployee);
            topBar.Controls.Add(lblSummary);

            // The grid
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = SystemColors.Window;

            grid.Columns.Add("colId", "Employee ID");
            grid.Columns.Add("colName", "Name");
            grid.Columns.Add("colDept", "Department");
            grid.Columns.Add("colManager", "Manager");
            grid.Columns.Add("colCert", "Certification");
            grid.Columns.Add("colExpires", "Expires");
            grid.Columns.Add("colDays", "Days Left");
            grid.Columns.Add("colStatus", "Status");
            grid.Columns["colDays"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Add the Fill control FIRST and the Top bar second: WinForms lays out docked
            // controls starting from the last one added, so the bar claims its strip first
            // and the grid fills whatever is left.
            Controls.Add(grid);
            Controls.Add(topBar);
        }

        // ---------------------------------------------------------------
        // Filling the grid
        // ---------------------------------------------------------------
        // CHANGED in Phase 3. Pseudocode:
        //   today = the date shown in the picker (NOT necessarily the real today)
        //   rows  = empty list
        //   FOR each employee, FOR each certification: add a CertRow(employee, cert, days)
        //   (an employee with no certifications gets one gray row, Days = "infinity")
        //   IF "Most urgent first" is ticked: insertion-sort rows by Days
        //   clear the grid, then add one grid row per CertRow, colored by status
        //   show the count of each color
        private void RefreshGrid()
        {
            DateTime today = dtpToday.Value.Date;
            sortedById = null; // data may have changed, so the binary-search array is stale
            grid.Rows.Clear();

            // Step 1: flatten the linked list into a list of lines
            List<CertRow> rows = new List<CertRow>();
            int visibleEmployees = 0;
            foreach (Employee emp in employees)
            {
                // NEW in Phase 5: a manager only sees their own team
                if (!user.CanSee(emp))
                {
                    continue;
                }
                visibleEmployees++;

                // Without this, an employee with no certifications would be invisible, and you
                // couldn't select them to add their first certification.
                if (emp.Certifications.Count == 0)
                {
                    rows.Add(new CertRow(emp, null, int.MaxValue));
                }

                foreach (Certification cert in emp.Certifications)
                {
                    int days = StatusCalculator.DaysRemaining(cert.ExpirationDate, today);
                    rows.Add(new CertRow(emp, cert, days));
                }
            }

            // Step 2: optional hand-written sort (see SortSearch.cs)
            if (chkUrgentFirst.Checked)
            {
                SortSearch.SortByDaysRemaining(rows);
            }

            // Step 3: draw the lines
            int green = 0, yellow = 0, red = 0, expired = 0;

            foreach (CertRow line in rows)
            {
                Employee emp = line.Employee;

                if (line.Certification == null)
                {
                    int emptyIndex = grid.Rows.Add(
                        emp.Id, emp.Name, emp.Department, emp.Manager,
                        "(none yet)", "", "", "No certifications");

                    grid.Rows[emptyIndex].Tag = new RowRef(emp, null);
                    ColorRowGray(grid.Rows[emptyIndex]);
                    continue;
                }

                CertStatus status = StatusCalculator.GetStatus(line.Days);

                int rowIndex = grid.Rows.Add(
                    emp.Id,
                    emp.Name,
                    emp.Department,
                    emp.Manager,
                    line.Certification.TypeName,
                    line.Certification.ExpirationDate.ToString("yyyy-MM-dd"),
                    line.Days,
                    StatusCalculator.StatusText(status));

                grid.Rows[rowIndex].Tag = new RowRef(emp, line.Certification);
                ColorRow(grid.Rows[rowIndex], status);

                switch (status)
                {
                    case CertStatus.Green: green++; break;
                    case CertStatus.Yellow: yellow++; break;
                    case CertStatus.Red: red++; break;
                    case CertStatus.Expired: expired++; break;
                }
            }

            lblSummary.Text = "View: " + (user.CanSeeEveryone ? "all employees" : user.ManagerName + "'s team")
                            + "   |   Green: " + green + "     Yellow: " + yellow
                            + "     Red: " + red + "     Expired: " + expired
                            + "     |     Employees shown: " + visibleEmployees + " of " + employees.Count;

            UpdateUndoButton();
        }

        private void ColorRow(DataGridViewRow row, CertStatus status)
        {
            Color background;
            switch (status)
            {
                case CertStatus.Green: background = Color.FromArgb(226, 240, 217); break;
                case CertStatus.Yellow: background = Color.FromArgb(255, 242, 204); break;
                case CertStatus.Red: background = Color.FromArgb(248, 215, 218); break;
                default: background = Color.FromArgb(240, 160, 160); break; // Expired: a stronger red
            }

            ApplyRowColors(row, background);

            if (status == CertStatus.Expired)
            {
                row.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
            }
        }

        // Gray for an employee who has no certifications yet
        private void ColorRowGray(DataGridViewRow row)
        {
            ApplyRowColors(row, Color.FromArgb(235, 235, 235));
        }

        private void ApplyRowColors(DataGridViewRow row, Color background)
        {
            row.DefaultCellStyle.BackColor = background;
            row.DefaultCellStyle.ForeColor = Color.Black;
            // Keep the status color visible even while the row is selected
            row.DefaultCellStyle.SelectionBackColor = ControlPaint.Dark(background);
            row.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        
        private RowRef GetSelectedRow()
        {
            if (grid.SelectedRows.Count == 0)
            {
                return null;
            }

            return grid.SelectedRows[0].Tag as RowRef; // "as" gives null instead of crashing
        }

        
        private void SelectRowFor(Employee employee)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                RowRef rowRef = row.Tag as RowRef;
                if (rowRef != null && rowRef.Employee == employee)
                {
                    grid.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    return;
                }
            }
        }

        
        private void OnTodayChanged(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        
        private string CsvPath
        {
            get { return Path.Combine(Application.StartupPath, "certtrack.csv"); }
        }

     
        private bool TrySave(out int rows)
        {
            rows = 0;

            try
            {
                rows = DataStore.Save(employees, CsvPath);
                return true;
            }
            catch (IOException ex)
            {
                MessageBox.Show("Could not save the file. Is it open in Excel?\n\n" + ex.Message, "CertTrack");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Could not save the file:\n\n" + ex.Message, "CertTrack");
                return false;
            }
        }

        private void SaveAfterChange()
        {
            int rows;
            TrySave(out rows);
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            int rows;
            if (TrySave(out rows))
            {
                MessageBox.Show("Saved " + rows + " rows to:\n\n" + CsvPath, "CertTrack");
            }
        }

        // Replace the list in memory with whatever is in the file.
        private void LoadFromCsv(bool showMessage)
        {
            int skipped;
            employees = DataStore.Load(CsvPath, out skipped);
            undo.Clear(); // NEW in Phase 5: the old undo actions point at employee objects that no longer exist

            if (skipped > 0)
            {
                MessageBox.Show("Loaded " + employees.Count + " employees.\n\n"
                              + skipped + " line(s) in the file could not be read and were skipped.",
                                "CertTrack");
            }
            else if (showMessage)
            {
                MessageBox.Show("Loaded " + employees.Count + " employees from:\n\n" + CsvPath, "CertTrack");
            }
        }

        private void OnLoadClicked(object sender, EventArgs e)
        {
            if (!File.Exists(CsvPath))
            {
                MessageBox.Show("There is no saved file yet. Click \"Save to CSV\" first.", "CertTrack");
                return;
            }

            LoadFromCsv(true);
            RefreshGrid();
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            dtpToday.Value = DateTime.Today; // this fires ValueChanged, which refreshes the grid
        }

        // ---------------------------------------------------------------
        // Sorting and searching 
        // ---------------------------------------------------------------
        private void OnSortChanged(object sender, EventArgs e)
        {
            RefreshGrid(); // RefreshGrid checks the checkbox itself
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // stops the "ding" sound
                OnFindNameClicked(sender, EventArgs.Empty);
            }
        }

        // LINEAR search by name. Pseudocode:
        //   matches = FindByName(all employees, text)
        //   IF none: tell the user
        //   IF the text is the same as last time: move to the NEXT match (wrap around)
        //   ELSE start again at the first match
        //   select that employee's row
        private void OnFindNameClicked(object sender, EventArgs e)
        {
            string text = txtSearch.Text.Trim();
            if (text.Length == 0)
            {
                MessageBox.Show("Type part of a name in the Search box first.", "CertTrack");
                return;
            }

            List<Employee> matches = SortSearch.FindByName(VisibleEmployees(), text);
            if (matches.Count == 0)
            {
                MessageBox.Show("No employee name contains \"" + text + "\".", "CertTrack");
                return;
            }

            if (text == lastNameQuery)
            {
                nameMatchIndex = (nameMatchIndex + 1) % matches.Count; // next match, wrapping
            }
            else
            {
                lastNameQuery = text;
                nameMatchIndex = 0;
            }

            Employee found = matches[nameMatchIndex];
            SelectRowFor(found);
            lblSummary.Text = "Match " + (nameMatchIndex + 1) + " of " + matches.Count
                            + " for \"" + text + "\": " + found.Name
                            + "  (linear search looked at " + CountVisible() + " employees)";
        }

        // BINARY search by ID. Pseudocode:
        //   read the ID as a number; IF it is not one: tell the user
        //   IF the sorted array is not built yet: build it (copy + insertion sort by ID)
        //   found = BinarySearchById(sorted array, id)
        //   select the row, and show how many comparisons it took
        private void OnFindIdClicked(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(txtSearch.Text.Trim(), out id))
            {
                MessageBox.Show("Type an employee ID (a whole number) in the Search box first.", "CertTrack");
                return;
            }

            if (sortedById == null)
            {
                sortedById = SortSearch.ToArraySortedById(employees);
            }

            int comparisons;
            Employee found = SortSearch.BinarySearchById(sortedById, id, out comparisons);

            //  ID outside this user's view must look exactly like "no such ID"
            if (found != null && !user.CanSee(found))
            {
                found = null;
            }

            if (found == null)
            {
                MessageBox.Show("No employee has ID " + id + ".\n\n(Binary search needed only "
                              + comparisons + " comparisons to be sure.)", "CertTrack");
                return;
            }

            SelectRowFor(found);
            lblSummary.Text = "Found ID " + id + ": " + found.Name
                            + "  (binary search: " + comparisons + " comparisons among "
                            + sortedById.Length + " employees)";
        }

        // ---------------------------------------------------------------
        // Roles, undo and log out
        // ---------------------------------------------------------------

        // Disable every button this role is not allowed to use.
        private void ApplyPermissions()
        {
            btnAddEmployee.Enabled = user.CanEditEmployees;
            btnDeleteEmployee.Enabled = user.CanEditEmployees;
            btnAddCert.Enabled = user.CanEditCerts;
            btnDeleteCert.Enabled = user.CanEditCerts;
            btnSaveCsv.Enabled = user.CanUseFiles;
            btnLoadCsv.Enabled = user.CanUseFiles;
            // btnUndo is handled by UpdateUndoButton (needs both permission AND something to undo)
        }

        // Only the employees this user is allowed to see (used by the search)
        private IEnumerable<Employee> VisibleEmployees()
        {
            foreach (Employee emp in employees)
            {
                if (user.CanSee(emp))
                {
                    yield return emp;
                }
            }
        }

        private int CountVisible()
        {
            int count = 0;
            foreach (Employee emp in VisibleEmployees())
            {
                count++;
            }
            return count;
        }

        
        private void UpdateUndoButton()
        {
            if (undo.Count > 0)
            {
                btnUndo.Text = "Undo (" + undo.Count + ")";
                tip.SetToolTip(btnUndo, "Undo: " + undo.Peek().Description);
            }
            else
            {
                btnUndo.Text = "Undo";
                tip.SetToolTip(btnUndo, "Nothing to undo");
            }

            btnUndo.Enabled = user.CanEditCerts && undo.Count > 0;
        }

        // Pseudocode:
        //   IF the stack is empty: stop
        //   action = undo.Pop()              <-- the MOST RECENT change comes off the top
        //   action.Undo(employees)           <-- each kind of action knows how to reverse itself
        //   save, redraw, and say what was undone
        private void OnUndoClicked(object sender, EventArgs e)
        {
            if (undo.Count == 0)
            {
                return;
            }

            UndoAction action = undo.Pop();
            action.Undo(employees);
            SaveAfterChange();
            RefreshGrid();
            lblSummary.Text = "Undid: " + action.Description;
            UpdateUndoButton();
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            LoggedOut = true;
            Close();
        }

        // NEW in Phase 4: open the alerts window. It receives the CURRENT employee list and the
        // simulated date, so sliding the date picker and re-scanning shows alerts "in the future".
        private void OnAlertsClicked(object sender, EventArgs e)
        {
            using (AlertsForm form = new AlertsForm(alerts, employees, dtpToday.Value.Date, user))
            {
                form.ShowDialog(this);
            }
        }

        // ---------------------------------------------------------------
        // Add and delete 
        // ---------------------------------------------------------------

        // Pseudocode:
        //   show the Add Employee dialog
        //   IF the user clicked OK
        //       add the new employee to the end of the linked list
        //       save to the CSV, rebuild the grid, select the new employee's row
        private void OnAddEmployeeClicked(object sender, EventArgs e)
        {
            using (EmployeeDialog dialog = new EmployeeDialog(employees))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    employees.Add(dialog.Result);
                    undo.Push(new AddEmployeeAction(dialog.Result)); // NEW in Phase 5
                    SaveAfterChange();
                    RefreshGrid();
                    SelectRowFor(dialog.Result);
                }
            }
        }

        // Pseudocode:
        //   selected = the row the user clicked; IF none: tell them and stop
        //   show the Add Certification dialog for that row's employee
        //   IF the user clicked OK: add the certification to that employee, save, refresh
        private void OnAddCertClicked(object sender, EventArgs e)
        {
            RowRef selected = GetSelectedRow();
            if (selected == null)
            {
                MessageBox.Show("Click an employee's row first, then click Add Certification.", "CertTrack");
                return;
            }

            using (CertificationDialog dialog = new CertificationDialog(selected.Employee))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    selected.Employee.AddCertification(dialog.Result);
                    undo.Push(new AddCertAction(selected.Employee, dialog.Result)); // NEW in Phase 5
                    SaveAfterChange();
                    RefreshGrid();
                    SelectRowFor(selected.Employee);
                }
            }
        }

        // Deletes ONE certification (the selected row); the employee stays.
        private void OnDeleteCertClicked(object sender, EventArgs e)
        {
            RowRef selected = GetSelectedRow();
            if (selected == null || selected.Certification == null)
            {
                MessageBox.Show("Click a certification row first, then click Delete Certification.", "CertTrack");
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Delete " + selected.Certification.TypeName + " for " + selected.Employee.Name + "?",
                "Confirm delete", MessageBoxButtons.YesNo);

            if (answer == DialogResult.Yes)
            {
                selected.Employee.Certifications.Remove(selected.Certification);
                undo.Push(new DeleteCertAction(selected.Employee, selected.Certification)); // NEW in Phase 5
                SaveAfterChange();
                RefreshGrid();
                SelectRowFor(selected.Employee);
            }
        }

        // Deletes the whole employee, including all their certifications.
        // This is where the linked list's Remove method gets used.
        private void OnDeleteEmployeeClicked(object sender, EventArgs e)
        {
            RowRef selected = GetSelectedRow();
            if (selected == null)
            {
                MessageBox.Show("Click a row for the employee you want to delete first.", "CertTrack");
                return;
            }

            int certCount = selected.Employee.Certifications.Count;

            DialogResult answer = MessageBox.Show(
                "Delete " + selected.Employee.Name + " and all " + certCount + " of their certifications?",
                "Confirm delete", MessageBoxButtons.YesNo);

            if (answer == DialogResult.Yes)
            {
                employees.Remove(selected.Employee.Id);
                undo.Push(new DeleteEmployeeAction(selected.Employee)); // NEW in Phase 5
                SaveAfterChange();
                RefreshGrid();
            }
        }
    }
}