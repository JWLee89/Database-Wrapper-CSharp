# Database-Wrapper-CSharp
A simple database wrapper written in C# to enable users to user various types of databases in the .Net framework. In order to create a concrete implementation of your database connector, all you need to do is extend the base class and define the abstract methods. 

The abstract methods are used to define certain objects that are specific to the database such as the connection string. 

The following concrete implementations are available as of now.

* Postgresql
* Oracle DB
* My Sql

# Basic Benchmark Test Results

The following scenario was the test case: 

Insert 15 rows of data into each of the following database 
- Oracle DB
- Mysql 
- Postgres Sql

The insertion was tested on databases hosted on AWS and also on my Local PC (16 GB ram, I7-6700 HQ CPU @ 2.60 GHZ (8 CPUS)).

The results were as follows. This was tested with 10 times with different tables (note that performing insert on same table right away, with similar dataset, will result in performance increasing by 1-2 ms thanks to caching), each row having 7 columns. Note that results may differ based on different environment, but this should give users a brief overview on the performance of the insertion operation.

- Oracle 1 ms per row 
- Postgresql 1 ms per row
- Mysql 1 ms per row

Note: The initial connection operation in Postgresql takes 95 seconds. I am assuming Mysql will yield roughly the same result, with oracle connection likely to be slightly faster. When I have time, I will be sure to measure the performance on my local PC. If the keepalive feature is set to true, since connection will be kept open, additional insertions will only take 1 millisecond.

# API Documentation 

Please note that this is currently a work in progess. The API documentation will be continuously updated.

## Public Methods

For each of the databases, the following <code>public</code> methods will be available.

``T SelectOne(string queryStr, FormatGenericData<T> genericDataDelegate)``

Used to select a single item from the database. This is often used with queries that are similar to the following query

```SELECT COUNT(*) FROM TABLE_NAME;```

`List<T> SelectList<T>(string queryStr, FormatGenericData<T> genericDataDelegate)`

Use this to select multiple rows of data from the database.
