# EFCore_DatabaseIndependence
Database independence with EFCore

## Azure Cosmos DB provider

`DainEF.Providers.AzureCosmosDB` implements `IEFCore_DatabasePlugin_10` using
`Microsoft.EntityFrameworkCore.Cosmos` 10.0.11 for Azure Cosmos DB for NoSQL.
Pass an account connection string (`AccountEndpoint=...;AccountKey=...`) and an
explicit, nonblank `databaseName` to `GenerateDbContextOptionsBuilder` or `UsePlugin`.
Keep account keys in application configuration or a secret store, not source code.
The provider uses gateway mode and retains normal HTTPS certificate validation.

```csharp
using DainEF.Providers.AzureCosmosDB;

var plugin = new AzureCosmosDbEfCorePlugin();
var options = plugin.GenerateDbContextOptionsBuilder<ApplicationDbContext>(
    connectionString, "ApplicationDatabase").Options;
```

`TestConnectionAsync` reads account metadata without creating resources.
Authentication/service errors return `false`; invalid arguments and cancellation
propagate as exceptions.
Models must follow Cosmos conventions: configure containers and partition keys,
provide IDs, and use embedded owned entities or scalar IDs instead of
cross-document navigation relationships. Cosmos operations must be asynchronous.

`AzureCosmosDbTest` follows the SQL Server container-test structure and inherits
the shared CRUD test using `EmpContext` directly. `EmpContext.OnModelCreating`
configures separate employee and department containers only when
`Database.IsCosmos()` is true, ignoring navigation properties and retaining their
scalar relationship IDs. Other providers retain their existing mappings.
The sample project references the Cosmos package for these mapping APIs.
Tests use Microsoft's Linux vNext preview emulator over local
HTTP. The optional `Func<HttpClient>` plugin constructor lets these tests use
Testcontainers' request-rewriting client for Docker's mapped port. The default
plugin constructor uses the SDK's normal HTTP client and certificate validation.
The vNext emulator does not enforce authentication; unauthorized/forbidden
responses are tested with a controlled HTTP handler, not the emulator.

```powershell
dotnet test --project .\src\EFCore_DaIn\EFCore_Dain_Test\EFCore_Dain_Test.csproj --filter-class EFCore_Dain_Test.AzureCosmosDbOptionsTest --filter-class EFCore_Dain_Test.AzureCosmosDbDefaultConfiguration
```

## Oracle provider

`DainEF.Providers.Oracle` implements `IEFCore_DatabasePlugin_10` using
`Oracle.EntityFrameworkCore` 10.23.26301 and EF Core 10.0.11. It requires
an Oracle database server and an existing user/schema with the necessary
permissions to create tables and access data.

```csharp
using DainEF.Providers.Oracle;
using SampleDatabase;

var plugin = new OracleEfCorePlugin();
var options = plugin.GenerateDbContextOptionsBuilder<EmpContext>(
    connectionString).Options;
using var context = new EmpContext(options);
```

Use a standard ODP.NET connection string containing `User Id`, `Password`,
and `Data Source`, supplied through your application's configuration.
The optional `databaseName` argument overrides the complete `Data Source`
(for example `localhost:1521/FREEPDB1`, a TNS alias, or a connect descriptor).
It does not change the user/schema or create an Oracle database.
`UsePlugin` configures an existing options builder with the same semantics.

`IDatabasePlugin.TestConnectionAsync` opens and disposes an Oracle connection.
Oracle connection/authentication failures return `false`; invalid arguments
and cancellation propagate as exceptions.

Oracle configuration and Docker-backed integration tests can be run with:

```powershell
dotnet test --project .\src\EFCore_DaIn\EFCore_Dain_Test\EFCore_Dain_Test.csproj --filter-class EFCore_Dain_Test.OracleOptionsTest --filter-class EFCore_Dain_Test.OracleDefaultConfiguration
```

Integration tests require Docker with Linux containers and download
`gvenzl/oracle-free:23.26.3-slim-faststart`. This pins the currently supported
Oracle Free release and uses the fast-start variant intended for automated tests.
The image is maintained by Gerald Venzl, not officially published by Oracle.
The tests use a disposable database and
test table creation, inserts, queries, updates, deletes, and connection checks.
Like SQL Server tests, `OracleTest` inherits `DatabaseContainerTest`, with
`OracleDefaultConfiguration` supplying the container. All container providers
inherit `GenerateDatabase`, which verifies creation and queries both DbSets.
Oracle supplies its existing data source and expects no increase in schema count;
other providers expect one new database.
`DatabaseContainerTest` provides the shared cross-context CRUD test for Oracle,
Azure Cosmos DB, SQL Server, PostgreSQL, and both MongoDB configurations. It assigns explicit
integer keys only when the provider's model does not generate them.
Database-count operations use `Task<int> NrDatabases` with a cancellation token;
all container tests await the providers' asynchronous database APIs.

## FileBaseContext provider

`DainEF.Providers.FileBaseContext` implements `IEFCore_DatabasePlugin_10` using
[FileBaseContext 6.0.0](https://www.nuget.org/packages/FileBaseContext/6.0.0),
compatible with EF Core 10. It stores entity records in JSON files and requires
no database server. The upstream provider is intended for development purposes.

Pass the storage directory as `connectionString`. The optional `databaseName`
selects a subdirectory; if omitted or blank, files are stored directly in the
storage directory. Relative directories are resolved against the current working
directory.

```csharp
using DainEF.Providers.FileBaseContext;
using SampleDatabase;

var plugin = new FileBaseContextEfCorePlugin();
var options = plugin.GenerateDbContextOptionsBuilder<EmpContext>(
    @"C:\Temp\DainEF", "Employees").Options;
using var context = new EmpContext(options);
```

The same arguments work with `UsePlugin` on an existing options builder.
`IDatabasePlugin.TestConnectionAsync` checks read/write access to the storage
directory, creating it if necessary and removing its temporary probe file.
Access and I/O failures return `false`; cancellation and invalid arguments
propagate as exceptions. When using `databaseName`, check the combined
directory path to test that specific database's access.
