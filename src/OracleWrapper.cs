namespace DbWrapper
{
    class OracleWrapper : AbstractDbWrapper
    {

        public OracleWrapper(string serverUrl, string username, string password, string dbName, int port) : base(serverUrl, username, password, dbName, port)
        {
        }

        public OracleWrapper(string serverUrl, string username, string password, string dbName, int port, bool keepAlive) : base(serverUrl, username, password, dbName, port, keepAlive)
        {
        }

        public OracleWrapper(string serverUrl, string username, string password, string dbName, int port, bool keepAlive, long timeOutPeriodSeconds) : base(serverUrl, username, password, dbName, port, keepAlive, timeOutPeriodSeconds)
        {
        }

        protected override DbConnection InitDbConnection(string connectionString)
        {
            return new OracleConnection(connectionString);
        }

        protected override string InitConnectionString()
        {
            return $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={ServerUrl})(PORT={Port})))(CONNECT_DATA=(SERVICE_NAME=XE))); User Id={Username}; Password={Password};";
        }

        protected override IDbDataParameter CreateSqlParameter(string key, object value)
        {
            return new OracleParameter(key, value);
        }

        protected override DbCommand CreateDbCommand(string queryStr)   
        {
            OracleCommand oracleCommand = new OracleCommand(queryStr, (OracleConnection)_connection);
            return oracleCommand;
        }
    }
}
