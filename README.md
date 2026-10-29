# CertTrack: Certificate Tracker

An employee certification and license tracker. CertTrack follows the expiration dates of CRMA, CPR/First Aid/AED, Safety Care and driver licenses, alerts managers ahead of time, and escalates to HR and the Training Manager as deadlines get close. Each role sees a color-coded dashboard showing who is compliant and who needs action.

Built in C# for the Microsoft Software & Systems Academy (MSSA) Cloud Application Development track (CCAD23). It started as a Windows desktop prototype and is being grown into a cloud web application (see the [Roadmap](#roadmap)).

> **Fake data only.** All employees in this project are made up. Do not enter real names, license numbers or personal information.

## Screenshots

| HR view | Manager view | Login |
|---|---|---|
| ![HR dashboard]("C:\Users\djaru\Downloads\hr-dashboard.png") | ![Manager view]("C:\Users\djaru\Downloads\manager-view.png") | ![Login]("C:\Users\djaru\Downloads\login.png") |

## Features

- **Color-coded dashboard:** one row per certification, colored by days left (green, yellow, red, dark red for expired), with a count of each color.
- **Simulated "today" date:** slide the date forward to watch rows change color and alerts fire, without waiting weeks.
- **Add and delete** employees and certifications, with confirmation prompts and automatic saving.
- **Undo** for the last edits or deletions.
- **Sort and search:** most-urgent-first sorting, search by name (linear) and by employee ID (binary, with the comparison count shown).
- **Alerts:** a queue of manager alerts (90 days) and HR and Training Manager escalations (30 days), with a sent-message log.
- **Role-based login:** Manager, HR and Training Manager each get their own view and permissions.

## Status rules

The dashboard color and the alert action are defined separately, from one set of constants in `StatusCalculator`.

| Days until expiration | Dashboard color | Who is alerted |
|---|---|---|
| 91 or more | Green | Nobody |
| 61 to 90 | Green | Manager (early heads-up at 90 days) |
| 31 to 60 | Yellow | Manager |
| 0 to 30 | Red | Manager, HR and Training Manager |
| Expired | Dark red, flagged Expired | Manager, HR and Training Manager |

## Roles

| Role | Sees | Can do |
|---|---|---|
| Manager | Own team only | Read-only; own alert inbox |
| HR | Everyone | Add, delete, undo, run alerts, save and load |
| Training Manager | Everyone | Add and delete certifications, undo; Training alert inbox |

### Demo accounts

| Username | Password | Role |
|---|---|---|
| `pat` | `pat123` | Manager (Pat Morgan's team) |
| `dana` | `dana123` | Manager (Dana Wells' team) |
| `hr` | `hr123` | HR |
| `training` | `train123` | Training Manager |

Passwords are stored as plain text in the code for demo purposes only. See [Known limits](#known-limits).

## Getting started

**Requirements:** Windows, Visual Studio 2022 with the ".NET desktop development" workload, and .NET 6 or later.

```bash
git clone https://github.com/DjarudiUmukunzi/Certificate-Tracker.git
```

1. Open the solution in Visual Studio.
2. Press **F5** to build and run. The login window appears first.
3. Log in with one of the demo accounts above.

The app creates two files next to the executable (in `bin\Debug\...`):

- `certtrack.csv`: all employees and certifications, saved after every change.
- `certtrack_alerts.log`: messages already sent, so alerts are not repeated after a restart.

To start from the sample data again, delete both files.

## Data structures and algorithms

The core structures and algorithms are written by hand rather than taken from the framework.

| Concept | Where | Used for |
|---|---|---|
| Singly linked list | `EmployeeLinkedList` | Employee records |
| Queue (FIFO) | `NotificationQueue` | Alerts waiting to be sent, most urgent first |
| Stack (array, doubling) | `UndoStack` | Undo history |
| HashSet | `AlertService` | Blocking duplicate alerts |
| Insertion sort (stable) | `SortSearch` | Urgency order |
| Linear search | `SortSearch` | Find by name |
| Binary search | `SortSearch` | Find by ID |
| Abstract class and polymorphism | `UndoAction` and four subclasses | Reversing each kind of change |
| Enums | `CertType`, `CertStatus`, `AlertLevel`, `UserRole` | Named choices instead of numbers |
| File I/O | `DataStore`, `AlertService` | CSV data file and alert log |

## Source files

All files use the `CertTrack` namespace. The screens are built in code, so there are no `.Designer.cs` files.

| Area | Files |
|---|---|
| Model and rules | `Employee.cs`, `Certification.cs`, `CertType.cs`, `CertStatus.cs`, `StatusCalculator.cs`, `SampleData.cs` |
| Data structures | `EmployeeNode.cs`, `EmployeeLinkedList.cs`, `NotificationQueue.cs`, `Undo.cs` |
| Algorithms | `SortSearch.cs` |
| Storage | `DataStore.cs` |
| Alerts | `Notification.cs`, `AlertService.cs`, `AlertsForm.cs` |
| Login and roles | `UserAccount.cs`, `LoginForm.cs` |
| Screens | `DashboardForm.cs`, `EmployeeDialog.cs`, `CertificationDialog.cs` |
| Entry point | `Program.cs` |

## Roadmap

CertTrack is one project in four stages. The stack was set with the course instructor: ASP.NET Core MVC with a Web API, a SQLite database, and no Blazor.

- [x] **Stage 1: Desktop prototype.** Dashboard, linked list, queue, stack, sorting and searching, CSV storage, alerts and role views.
- [ ] **Stage 2: Web foundation.** Rebuild as an ASP.NET Core MVC web app with a Web API and a SQLite database (Entity Framework Core). The rules, queue, undo and search logic is plain C# with no screen code, so most of it carries over; only the Windows Forms screens are replaced. The .NET MAUI course modules are applied as web concepts:

  | MAUI module | Web equivalent in CertTrack |
  |---|---|
  | Cross-platform app | Responsive web app for desktop and phone browsers |
  | XAML UI | Razor views and partial views |
  | XAML layout | CSS flexbox and grid |
  | Shared resources and styles | One shared stylesheet; status colors defined once |
  | Tab and flyout navigation | Navigation menu with pages per role |
  | Consume REST web services | CertTrack's own Web API (create, read, update, delete) |
  | Local data with SQLite | SQLite as the database |
  | Data binding | Data-bound dashboard grid |

- [ ] **Stage 3: AI agent and certification rollback.**
  - Every certification change is saved in a history table (who, when, before and after). HR can roll a certification back to an earlier version, and the rollback is recorded too.
  - An Azure OpenAI agent writes and sends alerts to the people the rules select, and gives suggestions to managers (who is due soon, which trainings to group) and to HR (patterns across teams, suspicious changes worth rolling back).
  - Safety rules: the rules decide statuses and recipients, never the model; a person approves every rollback; each message lists its sources and every agent action is logged; fake data only, with the API key kept in user secrets or Key Vault.
- [ ] **Stage 4: Harden, host and present.** Secure login, hosting on Azure, a testing checklist, documentation and the final demo.

**Optional extras:** an "Ask CertTrack" read-only question box; a small .NET MAUI companion app.

## Known limits

- Demo passwords are plain text in the code. A real system would store salted hashes or use a hosted login service such as Microsoft Entra ID.
- Data is stored in a CSV file, not a database.
- "Sending" an alert writes to a log. A real system would call an email or Teams service at that one point.
- Undo history is cleared when you log out or load from the CSV file, and an undone employee deletion returns the employee to the end of the list.
- Alerts that are waiting (not yet sent) are not saved on close; scanning again rebuilds them without repeating anything already sent.
- Renewing a certification means deleting the old one and adding the new one. There is no Edit button yet.
- It runs only as a Windows desktop program, for now.
- No AI features yet; they arrive in Stage 3.

## Author

Djarudi, MSSA Cloud Application Development (CCAD23). GitHub: [DjarudiUmukunzi](https://github.com/DjarudiUmukunzi)