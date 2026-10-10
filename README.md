# EFCore_DatabaseIndependence
Database independence with EFCore

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
