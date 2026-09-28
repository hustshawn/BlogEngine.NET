using System;
using System.Data;
using System.Data.Common;

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// Helper class for working with DbConnections.
    /// </summary>
    /// <remarks>
    /// This class handles the creation of DbConnection, setting its connection string, and opening the connection if possible.
    /// 
    /// Usage:
    /// using(var helper = new DbConnectionHelper(providerName, connectionString)) {
    ///     if (helper.HasConnection) {
    ///         // do stuff
    ///     }
    /// }
    /// </remarks>
    public sealed class DbConnectionHelper : IDisposable
    {

        #region "Constructors"

        /// <summary>
        /// Creates a new DbConnectionHelper instance from the given provider name and database connection string.
        /// </summary>
        /// <param name="providerName">The ADO.NET provider name (e.g. "Microsoft.Data.SqlClient").</param>
        /// <param name="connectionString">The database connection string.</param>
        public DbConnectionHelper(string providerName, string connectionString)
        {
            this._dbProvFactory = DbProviderFactories.GetFactory(providerName);
            this._connection = this._dbProvFactory.CreateConnection();

            this._hasConnection = (this._connection != null);
            if (this._hasConnection)
            {
                this._connection.ConnectionString = connectionString;
                this._connection.Open();
            }
        }

        #endregion

        #region "Properties"

        /// <summary>
        /// Returns the DbConnection of this instance.
        /// </summary>
        public DbConnection Connection
        {
            get { return this._connection; }
        }
        private DbConnection _connection;

        /// <summary>
        /// Gets whether the Connection of this ConnectionHelper instance is null.
        /// </summary>
        public bool HasConnection
        {
            get { return this._hasConnection; }
        }

        private bool _hasConnection;

        /// <summary>
        /// Gets the DbProviderFactory used by this ConnectionHelper instance.
        /// </summary>
        public DbProviderFactory Provider
        {
            get { return this._dbProvFactory; }
        }
        private DbProviderFactory _dbProvFactory;

        #endregion

        #region "Methods"

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("ConnectionHelper");
            }
        }

        /// <summary>
        /// Uses this ConnectionHelper instance's connection to create and return a new DbCommand instance.
        /// </summary>
        public DbCommand CreateCommand()
        {
            this.CheckDisposed();
            return this.Connection.CreateCommand();
        }

        /// <summary>
        /// Creates a new DbCommand with the given command text. CommandType is automatically set to CommandType.Text.
        /// </summary>
        public DbCommand CreateTextCommand(string commandText)
        {
            var command = this.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;

            return command;
        }

        /// <summary>
        /// Creates a DbParameter instance with the given parameter name and value.
        /// </summary>
        public DbParameter CreateParameter(string parameterName, object value)
        {
            return CreateParameter(parameterName, value, null, null);
        }

        /// <summary>
        /// Creates a DbParameter instance with the given parameter name, value, and DB type.
        /// </summary>
        public DbParameter CreateParameter(string parameterName, object value, DbType dbType)
        {
            return CreateParameter(parameterName, value, dbType, null);
        }

        /// <summary>
        /// Creates a DbParameter instance with the given parameter name, value, DB type, and size.
        /// </summary>
        public DbParameter CreateParameter(string parameterName, object value, DbType? dbType, int? size)
        {
            this.CheckDisposed();

            var param = this.Provider.CreateParameter();

            if (param == null)
            {
                throw new NullReferenceException("DbBlogProvider");
            }
            else
            {
                param.ParameterName = parameterName;
                param.Value = value;

                if (dbType.HasValue)
                    param.DbType = dbType.Value;

                if (size.HasValue)
                    param.Size = size.Value;

                return param;
            }
        }

        #endregion

        #region "IDisposable"

        private bool isDisposed;

        private void Dispose(bool disposing)
        {
            try
            {
                if (!this.isDisposed && disposing)
                {
                    if (this._connection != null)
                    {
                        this._connection.Dispose();
                    }
                }
            }
            finally
            {
                this._dbProvFactory = null;
                this._connection = null;
                this._hasConnection = false;
                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Disposes this DbConnectionHelper and its underlying connection.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
