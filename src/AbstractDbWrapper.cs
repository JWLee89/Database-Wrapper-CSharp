/**
 * @Author Jay Lee
 */
namespace DbWrapper {
    // Delegate for handling error logs
    public delegate void ErrorLoggingDelegate(Exception exc);

    public abstract class AbstractDbWrapper : IDisposable
    {
        /**
        * ============================================
        * ============= Public Properties ============
        * ============================================
        */
        public string ServerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string DbName { get; set; }
        public string ConnectionString { get; set; }
        public bool KeepAlive { get; set; }
        // Number of milliseconds to wait after a transaction
        // before closing an open connection 
        public long TimeoutPeriodMs { get; set; }
        public bool EnableErrorLog { get; set; }

        // Default behavior. Overwrite it later with logging features
        // Such as logging to file with Log4Net. Or something.
        // Override this in production
        public ErrorLoggingDelegate ErrorLoggingDelegate { get; set; } = exc =>
        {
            Console.WriteLine("Exception has occurred");
            Console.WriteLine(exc);
        };

        /**
        * ============================================
        * ======== Protected/Private Properties ======
        * ============================================
        */
        // Generic C# Database Connection
        protected DbConnection _connection;
        private Timer _timer;

        /**
        * ============================================
        * =============== Constructors ===============
        * ============================================
        */
        protected NseDbWrapper(string serverUrl, string username, string password, string dbName, int port)
        {
            ServerUrl = serverUrl;
            Username = username;
            Password = password;
            DbName = dbName;
            Port = port;
            KeepAlive = false;

            // initialize default values
            ConnectionString = InitConnectionString();
            TimeoutPeriodMs = Common.MILLISECONDS_IN_MINUTES * 2;   // Default TimeOut Period is 2 minutes. 
            _connection = InitDbConnection(ConnectionString);
        }

        protected NseDbWrapper(string serverUrl, string username, string password, string dbName, int port,
            bool keepAlive)
        {
            ServerUrl = serverUrl;
            Username = username;
            Password = password;
            DbName = dbName;
            Port = port;
            KeepAlive = keepAlive;

            // initialize default values
            ConnectionString = InitConnectionString();
            TimeoutPeriodMs = Common.MILLISECONDS_IN_MINUTES * 2;   // Default TimeOut Period is 2 minutes. 
            _connection = InitDbConnection(ConnectionString);
        }

        protected NseDbWrapper(string serverUrl, string username, string password, string dbName, int port,
            bool keepAlive, long timeOutMs)
        {
            ServerUrl = serverUrl;
            Username = username;
            Password = password;
            DbName = dbName;
            Port = port;
            KeepAlive = keepAlive;

            // initialize default values
            ConnectionString = InitConnectionString();
            TimeoutPeriodMs = timeOutMs;
            _connection = InitDbConnection(ConnectionString);
        }

        // Private methods
        // TODO: Refactor later
        private void AddParamsForInsert(DbCommand command, object itemToInsert)
        {
            // Prepare parameters using reflection to get properties.
            // Do not get properties with NseDbWrapperAttributes on insert
            IEnumerable<PropertyInfo> objProperties = itemToInsert.GetType().GetProperties()
                .Where(prop => !prop.IsDefined(typeof(NseDbWrapperAttributes), false));
            foreach (PropertyInfo prop in objProperties)
            {                
                command.Parameters.Add(CreateSqlParameter(prop.Name, prop.GetValue(itemToInsert, null)));
            }
        }

        // Exception logging method
        private void PerformErrorLogging(ErrorLoggingDelegate del, Exception exception)
        {
            if (EnableErrorLog)
                del(exception);
        }

        // Initialze Timer
        private void InitTimer()
        {
            this._timer = new Timer(this.TimeoutPeriodMs);
            this._timer.AutoReset = true;   // Fire event only once when multiple events overlap
            this._timer.Enabled = true;
            this._timer.Elapsed += CloseConnection;
            _timer.Start();
        }

        // Close connection after specified time has passed
        private void CloseConnection(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Close();
            CloseDbConnection();
        }

        /**
          * ===========================================
          * =========== DB Wrapper Interface ==========
          * ===========================================
          */

        // Generate connection string based on database type.
        protected abstract string InitConnectionString();

        // create new Command and data reader
        protected abstract DbCommand CreateDbCommand(string queryStr);
        protected abstract IDbDataParameter CreateSqlParameter(string key, object value);
        protected abstract DbConnection InitDbConnection(string connectionString);

        // Delegate for handling generic data in "SelectList<T> and SelectOne<T>"
        public delegate T FormatGenericData<T>(DbDataReader dr);

        protected void ResetTimer()
        {
            this._timer.Stop();
            this._timer.Start();
        }

        protected void StopTimer()
        {
            this._timer.Stop();
        }
        
         // Connection status check
        public bool IsOpen()
        {
            return this._connection.State == ConnectionState.Open;
        }

        public void OpenDbConnection()
        {
            try
            {
                this._connection.Open();
            }
            catch (DbException ex)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, ex);
            }
            catch (Exception ex)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, ex);
            }
        }

        public void CloseDbConnection()
        {
            try
            {
                if (this._connection.State == ConnectionState.Open)
                {
                    this._connection.Close();
                }
            }
            catch (DbException ex)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, ex);
            }
            catch (Exception ex)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, ex);
            }
        }

        public virtual T SelectOne<T>(string queryStr, FormatGenericData<T> genericDataDelegate)
        {
            T result = default(T);
            try
            {
                // Manually open connection so that connection does not 
                // close after database transaction is performed.
                LazyOpenConnection();
                using (DbCommand command = CreateDbCommand(queryStr))
                using (DbDataReader reader = command.ExecuteReader())
                {
                    bool hasMultipleRows = false;
                    while (reader.Read())
                    {
                        if (hasMultipleRows)
                            throw new Exception("Cannot return more than one row in SelectOne()");

                        result = genericDataDelegate(reader);
                        hasMultipleRows = true;
                    }
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return result;
        }

        public virtual List<T> SelectList<T>(string queryStr, FormatGenericData<T> genericDataDelegate)
        {
            List<T> result = new List<T>();
            try
            {
                // Manually open connection so that connection does not 
                // close after database transaction is performed.
                LazyOpenConnection();

                using (DbCommand command = CreateDbCommand(queryStr))
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(genericDataDelegate(reader));
                    }
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return result;
        }

        public virtual List<T> SelectList<T>(string queryStr, FormatGenericData<T> genericDataDelegate, DbParameter[] parameters)
        {
            List<T> result = new List<T>();
            try
            {
                // Manually open connection so that connection does not 
                // close after database transaction is performed.
                LazyOpenConnection();
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    command.Parameters.AddRange(parameters);
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(genericDataDelegate(reader));
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return result;
        }

        /// <summary>
        /// Insert data to the database and return the number 
        /// of rows inserted into the database. 
        /// </summary>
        /// <param name="queryStr">The SQL Query string</param>
        /// <param name="itemToInsert">Item to persist onto the database</param>
        /// <returns></returns>
        public virtual int Insert(string queryStr, object itemToInsert)
        {
            int recordAffected = 0;
            try
            {
                LazyOpenConnection();
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    // Prepare parameters using reflection to get properties.
                    AddParamsForInsert(command, itemToInsert);
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        // Insert into table (id, val) values (@id, @val)
        // { new DbParameter("id", 1), new DbParameter("val", 132.6) }
        // E.g. PGSql ==> NpgsqlParameter class
        public virtual int Insert(string queryStr, DbParameter[] parameters)
        {
            int recordAffected = 0;
            try
            {
                LazyOpenConnection();
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    command.Parameters.AddRange(parameters);
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        /// <summary>
        /// Insert data to the database and return the number 
        /// of rows inserted into the database. 
        /// </summary>
        /// <param name="queryStr">The SQL Query string</param>
        /// <returns></returns>
        public virtual int Insert(string queryStr)
        {
            int recordAffected = 0;
            LazyOpenConnection();
            try
            {
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        public virtual int Update(string queryStr)
        {
            int recordAffected = 0;
            LazyOpenConnection();
            try
            {
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    // Execute SQL command.
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        // Update table set val = @Val where id = @Id
        // { new DbParameter("id", 1), new DbParameter("val", 132.6) }
        // E.g.: PGSql ==> NpgsqlParameter class
        public virtual int Update(string queryStr, DbParameter[] parameters)
        {
            int recordAffected = 0;
            try
            {
                LazyOpenConnection();
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    command.Parameters.AddRange(parameters);
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        public virtual int Delete(string queryStr)
        {
            int recordAffected = 0;
            try
            {
                LazyOpenConnection();
                // Execute SQL command.
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        // Delete from table where id = @Id 
        // { new DbParameter("id", 1) }
        // E.g. PGSql ==> NpgsqlParameter class
        public virtual int Delete(string queryStr, DbParameter[] parameters)
        {
            int recordAffected = 0;
            try
            {
                LazyOpenConnection();
                using (DbCommand command = CreateDbCommand(queryStr))
                {
                    command.Parameters.AddRange(parameters);
                    // Execute SQL command.
                    recordAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (DbException e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            catch (Exception e)
            {
                PerformErrorLogging(this.ErrorLoggingDelegate, e);
            }
            finally
            {
                PerformKeepAliveCheck();
            }
            return recordAffected;
        }

        /// <summary>
        /// Only open connection if it is closed. 
        /// </summary>
        protected void LazyOpenConnection()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
                if (KeepAlive)
                {   
                    if (_timer != null)
                    {
                        ResetTimer();
                    }
                    // First DB Transaction. Initialize timer.
                    else
                    {
                        InitTimer();
                    }
                }
            }
        }

        /// <summary>
        /// Check if allocated keep alive time has passed since
        /// last database transaction was made.
        /// </summary>
        protected virtual void PerformKeepAliveCheck()
        {
            // If keepalive is set to true, reset timer.
            if (this.KeepAlive)
            {
                ResetTimer();
            }
            else
            {
                if (_timer != null)
                    StopTimer();
                CloseDbConnection();
            }
        }

        // Release resources
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Dispose();
                _timer?.Dispose();  // Null propagation
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
