CREATE TABLE IF NOT EXISTS JobExecutionLog (
    Id          INTEGER  NOT NULL,
    JobId       INTEGER  NOT NULL,
    JobName     TEXT     NOT NULL,
    StartedAt   TEXT     NOT NULL,
    FinishedAt  TEXT,
    Status      TEXT     NOT NULL,
    Message     TEXT,
    ErrorDetail TEXT,
    PRIMARY KEY (Id AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_JobExecutionLog_JobId_StartedAt ON JobExecutionLog (JobId, StartedAt DESC);
