namespace DbWrapper
{
    class PgsqlWrapper : AbstractDbWrapper
    {
      /**
        * ===========================================
        * =========== BASIC DATABASE INFO ===========
        * ===========================================
        * 
        * Connection string information
        * PostgeSQL style connection string
        * Server   - specifies the server location
        * User Id  - Username used to access the database
        * Port     - default is 5432
        * Password - the password for the database user
        * Database - Name of the database that you will be accessing
        */
      /**
       * ===========================================
       * =============== Constructor ===============
       * ===========================================
       * 
       * First  - Basic default settings
       * Second - Keep Alive
       * Third  - TimeoutPeriod 
       */
        // Keep alive default is set to false
        public NsePgsqlWrapper(string serverUrl, string username, string password, string dbName, int port = 5432) 
            : base(serverUrl, username, password, dbName, port)
        {
            base._connection = new NpgsqlConnection(this.ConnectionString);
        }

        // Contstructor for changing keep alive property
        public NsePgsqlWrapper(string serverUrl, string username, string password, string dbName, int port, bool keepAlive) 
            : this(serverUrl, username, password, dbName, port)
        {
            this.KeepAlive = keepAlive;
        }

        // constructor for adding keep alive plus timeout period
        public NsePgsqlWrapper(string serverUrl, string username, string password,
            string dbName, int port, bool keepAlive, long timeoutPeriod) 
            : this(serverUrl, username, password, dbName, port, keepAlive)
        {
            this.TimeoutPeriodMs = timeoutPeriod;
        }

        protected override DbConnection InitDbConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        /// <summary>
        /// Create specific DB Command for PGSQL DB Object
        /// </summary>
        protected override DbCommand CreateDbCommand(string queryStr)
        {
            return new NpgsqlCommand(queryStr, (NpgsqlConnection)_connection);
        }

        protected override IDbDataParameter CreateSqlParameter(string key, object value)
        {
            return new NpgsqlParameter(key, value);
        }

        /// <summary>
        /// Generate the connection string used to connect to the database with the required information.
        /// This needs to be overwritten properly for the database connection to work in .Net
        /// </summary>
        protected override string InitConnectionString()
        {
            return $"Server={this.ServerUrl};Port={this.Port};User Id={this.Username};Password={this.Password};Database={this.DbName};";
        }
    }
}
