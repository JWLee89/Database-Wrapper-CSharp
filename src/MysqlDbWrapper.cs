namespace DbWrapper
{
    // Default port is 3306 for Mysql
    class MysqlDbWrapper : AbstractDbWrapper
    {

        public MysqlDbWrapper(string serverUrl, string username, string password, string dbName, int port) : base(serverUrl, username, password, dbName, port)
        {
            Console.WriteLine("Connected to MYSQL");
        }

        public MysqlDbWrapper(string serverUrl, string username, string password, string dbName, int port, bool keepAlive) : base(serverUrl, username, password, dbName, port, keepAlive)
        {
        }

        public MysqlDbWrapper(string serverUrl, string username, string password, string dbName, int port, bool keepAlive, long timeOutMs) : base(serverUrl, username, password, dbName, port, keepAlive, timeOutMs)
        {
        }

        protected override string InitConnectionString()
        {
            return $"SERVER={this.ServerUrl};DATABASE={this.DbName};UID={this.Username};PASSWORD={this.Password};";
        }

        protected override DbCommand CreateDbCommand(string queryStr)
        {
            return new MySqlCommand(queryStr, (MySqlConnection) base._connection);
        }

        protected override IDbDataParameter CreateSqlParameter(string key, object value)
        {
            return new MySqlParameter(key, value);
        }

        protected override DbConnection InitDbConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }
    }
}
