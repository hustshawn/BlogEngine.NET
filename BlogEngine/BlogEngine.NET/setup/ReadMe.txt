BlogEngine.NET supports a variety of data storage options.  By default, it stores all
data in XML files in the App_Data folder.  To store blog data in a database, configure
the provider in appsettings.json (or environment variables) instead of web.config:

  "BlogEngine": {
    "DefaultProvider": "DbBlogProvider",     // blog data, membership and roles
    "FileSystemProvider": "XmlBlogProvider"  // uploaded files: XmlBlogProvider or DbBlogProvider
  },
  "ConnectionStrings": {
    "BlogEngine": "<connection string>",
    "BlogEngine_ProviderName": "Microsoft.Data.SqlClient"  // optional, this is the default
  }

The same settings as environment variables: BlogEngine__DefaultProvider,
BlogEngine__FileSystemProvider, ConnectionStrings__BlogEngine.

- SQLServer: supported and tested on .NET 10 (SQL Server 2022). See SQLServer/ReadMe.txt.
- MySQL, SQLite: the database scripts are kept, but only Microsoft.Data.SqlClient is registered
  as an ADO.NET provider. Using them requires adding a .NET provider package and registering its
  DbProviderFactory under BlogEngine_ProviderName; this combination has not been tested.
- SQL CE is not available on .NET Core and is no longer supported.

Additional information can be found on our site at https://blogengine.io/
