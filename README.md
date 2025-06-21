# TaskSchedulingApp

**TaskSchedulingApp** is an ASP.NET Core Web API designed to streamline task scheduling and management for teams. It enables users to create, assign, and track tasks, set personalized alarms, and receive real-time notifications. The application supports advanced authorization with roles and custom policies for secure operations and is built with a focus on scalability and maintainability, tailored for team collaboration in a professional environment.

---

## Features

### Task Management
- Create, read, update, and delete tasks with details like title, description, due date, and status.
- Assign tasks to multiple users and manage assignees dynamically.

### Assignee-Specific Alarms
- Allows assignees to set personal alarm dates for tasks they are assigned to.
- Validates alarm dates to ensure they fall between the current time and the task’s due date using custom attributes.

### Real-Time Notifications
- Sends notifications for task due dates and assignee-specific alarms via SignalR.
- Targets notifications to specific users for personalized alerts.

### JWT Authentication
- Secures API endpoints using JSON Web Tokens (JWT) with ASP.NET Core Identity.

### Authorization
- Implements role-based access with two roles:
  - `TeamLead`: Can create, update, assign, and delete tasks.
  - `Developer`: Can view assigned tasks and set personal alarms.

- Uses custom authorization policies:
  - `TaskCreator`: Restricts task creation, updating, and deletion to the user who created the task.
  - `TeamMember`: Allows users assigned to a task to perform actions like setting personal alarms or rejecting task assignments.

- Policies are enforced via a custom authorization handler for fine-grained access control.

### Swagger Documentation
- Provides API documentation via Swagger UI.

### Entity Framework Core
- Manages database operations with SQL Server.

### Logging
- Integrates Serilog for structured logging to files and console for debugging.

---

## Technology Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity (for JWT authentication)
- SignalR (for real-time notifications)
- Serilog (for logging)
- Swagger/OpenAPI (for API documentation)
