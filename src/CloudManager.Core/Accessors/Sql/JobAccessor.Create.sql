CREATE TABLE IF NOT EXISTS JobDefinition (
    Id              INTEGER  NOT NULL,
    Name            TEXT     NOT NULL,
    Description     TEXT,
    ProfileName     TEXT     NOT NULL,
    RegionName      TEXT     NOT NULL,
    ServiceType     TEXT     NOT NULL,
    Operation       TEXT     NOT NULL,
    ParametersJson  TEXT     NOT NULL,
    CronExpression  TEXT     NOT NULL,
    CronTimeZone    TEXT     NOT NULL,
    IsEnabled       INTEGER  NOT NULL,
    CreatedAt       TEXT     NOT NULL,
    UpdatedAt       TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);
