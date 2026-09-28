Running BlogEngine.NET using SQL Server 2008 and up:

If you wish to use SQL Server to store all your blog data, this folder has all the 
information you'll likely need.  The scripts included here are for SQL Server 2008 and up.
They could be modified to be used with earlier or later versions if needed.

Instructions for new setup:

1. Connect to your SQL Server (SQL Server Management Studio, Azure Data Studio or sqlcmd).
2. Create a new database if desired.
3. Execute the Setup script against the database you want to add the BlogEngine data to.
4. In appsettings.json set "BlogEngine:DefaultProvider" to "DbBlogProvider" and add the
   connection string, for example:

     "BlogEngine": { "DefaultProvider": "DbBlogProvider", "FileSystemProvider": "XmlBlogProvider" },
     "ConnectionStrings": { "BlogEngine": "Server=.;Database=BlogEngine;Trusted_Connection=True;TrustServerCertificate=True" }

5. Start the blog and see the welcome post.
6. Login with the username admin and password admin.  Change the password.

Upgrading from previous version

 - Use Upgrade.sql to upgrade from latest version (3.1)

Upgrading from earlier versions

- Use scripts in Archive folder to bring your DB to version 3.1, then run Upgrade.sql