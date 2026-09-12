DELETE FROM JobExecutionLog
WHERE JobId = /*@ jobId */0
  AND Id NOT IN (SELECT Id FROM JobExecutionLog WHERE JobId = /*@ jobId */0 ORDER BY StartedAt DESC LIMIT /*@ retainCount */100)
