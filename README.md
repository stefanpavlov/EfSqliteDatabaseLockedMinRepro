This is a minimal reproduction of a bug where executing a large nomber of delete commands against a SQLite database causes a small percent of them to fail with `SQLite Error 5: 'database is locked'.` exception after a small amount of time even though there is a huge command timeout (1 day).

There is a sample database file with approximately 100k entries.

To reproduce the issue start the project **without debugging** and execute the /TestParallelDelete endpoint. It will select 1000 rows and attempt to delete them in parallel.
On my test machine it reliably fails to delete between 30 and 80 rows in 5-7 seconds.

>[!NOTE]
>Starting the project with debugging causes the issue to be much more pronounced - between 300 and 500 deletes fail and the whole process takes upwards of 2 minutes.
