# UserManager Service

A nano service that is flexible, generic, and extensible service for managing users, roles, permission, and themes. Built to support modular architecture using ASP.NET Core Dependency Injection.

---

## ✨ Features

- Manage users and retrieve user lists
- Assign roles, statuses, permissions, and themes to users
- Plug-in architecture supporting PostgreSQL, MySQL, and SQL Server
- Built-in base controller for immediate API usage
- Supports custom controller extensions with advanced logic
- Optional background context initialization and database migrations
---

## 📦 Installation

Set up the UserManager service in your project using a structured flow for build, reference, and use.
#### Step 1: Register Repository in ```repos.xml```
First, make sure your project recognizes the UserManager repository. Update your ```repos.xml``` file to include the following entry:

```bash
<Repositories>
  <Repository>
    <Name>UserManager</Name>
    <Branch>main</Branch>
  </Repository>
</Repositories>
```



#### Step 2: Build the Service
Navigate to the build directory that contains the build.ps1 script and run the following PowerShell command:
```powershell
.\build.ps1
```

This will:

Compile the UserManager service

Copy the output DLL to build\NsStore\UserManager.

#### Step 3: Reference the DLL in Your .csproj
```
<ItemGroup>
  <Reference Include="UserManager">
    <HintPath>build\NsStore\UserManager.dll</HintPath>
  </Reference>
</ItemGroup>

```

---

## Getting Started with Controllers

UserManager includes a generic RESTful controller that you can inherit and extend.

## Inheriting the Built-in Controller

To use the default logic and endpoints, simply extend the base controller:

```csharp
[Route("api/[controller]/[action]")]
[ApiController]
public class InheritUserManagerController : UserManagerControllerBase<UserProcessRole, UserProcessStatus, UserProcessPermission, UserProcessTheme>
{
    public InheritUserManagerController(
        IManageUsers<UserProcessRole, UserProcessStatus, UserProcessPermission, UserProcessTheme> userManager)
        : base(userManager)
    {
    }
}

```

## Defining Custom Endpoints

You can extend the base controller with your own logic:

```csharp
[Route("api/[controller]/[action]")]
[ApiController]
public class InheritUserManagerController : UserManagerControllerBase<UserProcessRole, UserProcessStatus, UserProcessPermission, UserProcessTheme>
{
    private readonly IManageUsers<UserProcessRole, UserProcessStatus, UserProcessPermission, UserProcessTheme> _userManager;

    public InheritUserManagerController(IManageUsers<UserProcessRole, UserProcessStatus, UserProcessPermission, UserProcessTheme> userManager)
        : base(userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomFilteredUsers()
    {
        var users = await _userManager.GetAllUsersAsync();
        var filtered = users.Where(u => u.IsActive).ToList();
        return Ok(filtered);
    }
}

```

## 🔧 Service Registration
In Program.cs, register the service using the provided extension:

```csharp
// SQL Server
builder.Services.AddUserManager<
    UserProcessRole,
    UserProcessStatus,
    UserProcessPermission,
    UserProcessTheme
>(IntiationModels._sqlModel);

// MySQL Server
builder.Services.AddUserManager<
    UserProcessRole,
    UserProcessStatus,
    UserProcessPermission,
    UserProcessTheme
>(IntiationModels._mysqlModel);

// PostgreSQL
builder.Services.AddUserManager<
    UserProcessRole,
    UserProcessStatus,
    UserProcessPermission,
    UserProcessTheme
>(IntiationModels._psqlModel);
```
***IntiationModels._psqlModel***, ***IntiationModels._sqlModel***, ***IntiationModels._mySqlModel*** must implement IDatabaseConfig and determine the correct database provider.
