INSERT INTO JobExecutionLog (JobId, JobName, StartedAt, Status) VALUES (/*@ jobId */0, /*@ jobName */'', /*@ startedAt */'', /*@ status */'');
SELECT last_insert_rowid();
