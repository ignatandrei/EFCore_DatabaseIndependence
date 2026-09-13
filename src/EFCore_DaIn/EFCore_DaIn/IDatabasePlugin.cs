namespace EFCore_DaIn
{

    public interface IDatabasePlugin
    {
        /// <summary>Short unique name, e.g. "SqlServer", "Postgres", "Sqlite", "Mongo".</summary>
        string ProviderName { get; }

        /// <summary>Human readable description shown to operators/administrators.</summary>
        string Description { get; }

        /// <summary>Version of the plugin, e.g. "10.0.0".</summary>
        string Version { get; }
    }

}

