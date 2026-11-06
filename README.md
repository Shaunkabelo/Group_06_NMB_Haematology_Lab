# NMB-HLabSys | Haematology Diagnostic Laboratory System

A role-based web application for managing the day-to-day workflow of a haematology diagnostic laboratory. It brings patient registration, doctor test requests, laboratory processing, results, stock control, reporting, and administration into one system.

Built as a collaborative academic project, with a focus on realistic laboratory workflows, validated data capture, and clear role-specific user experiences.



## Highlights

- Role-based access for **Admin**, **Lab Manager**, **Lab Technician**, **Doctor**, and **Patient** users.
- Secure sign-in, forced first-password change, password reset, profile management, and account/session protection.
- Patient registration and management of allergies, medical conditions, medications, consent, and laboratory history.
- Doctor workflow for patient lookup, test requests, request cancellation, and test-result review.
- Technician workflow for receiving samples, recording results, verification, and laboratory processing.
- Lab Manager tools for staff, suppliers, consumables, stock orders, test types, technician test-type assignments, and PDF reports.
- Admin management of users and structured address lookup data (province, town/city, suburb, and area code).
- Responsive layouts, live search, filter controls, expandable data rows, and form feedback designed for desktop and mobile use.

## Key workflow

```text
Doctor requests tests
        ↓
Sample is received and assigned for processing
        ↓
Lab technician records and verifies results
        ↓
Doctor and patient can review completed results
        ↓
Lab manager monitors stock, staff capacity, and reports
```

## Technology

| Area | Tools used |
| --- | --- |
| Web application | ASP.NET Core MVC (.NET 8) |
| Language | C# |
| Data access | Dapper, Entity Framework Core, Microsoft.Data.SqlClient |
| Database | Microsoft SQL Server |
| Authentication | Cookie authentication, role-based authorization, password hashing and reset workflow |
| Reporting | QuestPDF |
| Front end | Razor Views, HTML, CSS, JavaScript, responsive UI |
| Testing | Console-based workflow and authentication checks |

## Architecture

The solution is separated into a web layer and a data layer:


Group_06_NMB_Haematology_Diagnostic_Laboratory
├── Group_06_NMB_Haematology_Diagnostic_Laboratory/   # MVC web application
├── Group_06_NMB_Haematology_Diagnostic_Lab.Data/    # Models, repositories, SQL data access
├── Database/                                        # Repeatable schema, procedure and seed scripts
└── Tests/                                           # Authentication and doctor-workflow checks


Database operations use parameterised queries and stored procedures for key multi-step workflows, including stock receipts, order cancellation, authentication, and reporting. Deployment scripts are designed to be previewed before applying changes.

## Notable implementation details

- **Structured addresses:** Addresses are normalised through Province, Town/City, Suburb, and UserAddress tables, supporting dependent drop-downs and consistent location data.
- **Inventory integrity:** Stock-order receipt and cancellation workflows use transactions and validation to prevent double receipts, invalid supplier/consumable combinations, and negative stock outcomes.
- **Staff capability assignment:** Test types can be assigned to technicians, allowing operational responsibility to be managed from both technician and test-type views.
- **Reporting:** Lab Manager reports can be previewed before download and include test activity grouped by category, date-level detail, and estimated consumable usage.
- **User feedback:** Forms provide field-level validation and user-friendly errors without exposing database implementation details.

## Running the project locally

### Prerequisites

- Visual Studio 2022 with ASP.NET and web development tools, or the .NET 8 SDK
- SQL Server / SQL Server Express

### Setup

1. Clone the repository.
2. Create or select the SQL Server application database.
3. Run the applicable scripts in the `Database` folder. Start with the schema and required stored-procedure scripts, then use the preview-first deployment or seed scripts as needed.
4. Store your local connection string and mail settings in User Secrets or environment variables. Do not commit credentials to source control.
5. Restore and run:

   ```bash
   dotnet restore
   dotnet run --project Group_06_NMB_Haematology_Diagnostic_Laboratory
   ```

6. Open the local HTTPS address shown by ASP.NET Core.

## Testing

The repository contains focused executable checks for authentication and doctor workflow behaviours:

```bash
dotnet run --project Tests/AuthenticationChecks
dotnet run --project Tests/DoctorWorkflowChecks
```

## Database scripts

The `Database` folder includes safe, repeatable scripts for:

- Structured staff addresses and address lookup seeds
- Authentication and profile support
- Inventory and stock-order workflow procedures
- Lab Manager report procedures
- Judging/demo preload data

Where a script exposes an `@Apply` setting, run it with `@Apply = 0` first to review the planned changes. Set it to `1` only when ready to commit them.

## Screenshots(Lab Manager)
<img width="1920" height="1080" alt="Screenshot (126)" src="https://github.com/user-attachments/assets/4c5a501a-94c8-4240-b60f-ce03621e2c47" />

<img width="1920" height="1080" alt="Screenshot (127)" src="https://github.com/user-attachments/assets/c60ae729-44eb-4e51-b141-9aa5cbbb3cd6" />

<img width="1920" height="1080" alt="Screenshot (128)" src="https://github.com/user-attachments/assets/e9d2f3cf-d5b8-483f-a008-5de527170114" />

<img width="1920" height="1080" alt="Screenshot (129)" src="https://github.com/user-attachments/assets/b5e5ae46-c581-4cec-a3a5-184263a04f52" />

<img width="1920" height="1080" alt="Screenshot (130)" src="https://github.com/user-attachments/assets/5d37bada-769a-4006-b66f-d6e3a2325d62" />

<img width="1920" height="1080" alt="Screenshot (131)" src="https://github.com/user-attachments/assets/4272bcbd-06e9-47f6-9037-dafec515bde1" />


## Team

Developed collaboratively for the ONP400 project module. Contributions cover the Lab Manager, Doctor, Patient, Lab Technician, Admin, database, integration, and quality-assurance areas.

## License

This project was created for academic and portfolio purposes.
