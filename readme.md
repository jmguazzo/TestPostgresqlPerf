# PostgresqlPerf test #

Test project that does some load on a postgresql server.


It expects to connect to a database with a table ```table_a``` created with the following script :

    create table table_a(
    thread_id int,
    a_value int);

    create index on table_a(thread_id);

    insert into table_a(thread_id,a_value)
    select * from generate_series(1,10) as a ,generate_series(1,100000) as b;

