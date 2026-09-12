INSERT INTO JobDefinition
    (Name, Description, ProfileName, RegionName, ServiceType, Operation, ParametersJson, CronExpression, CronTimeZone, IsEnabled, CreatedAt, UpdatedAt)
VALUES
    (/*@ name */'', /*@ description */'', /*@ profileName */'', /*@ regionName */'', /*@ serviceType */'', /*@ operation */'', /*@ parametersJson */'', /*@ cronExpression */'', /*@ cronTimeZone */'', /*@ isEnabled */1, /*@ createdAt */'', /*@ updatedAt */'');
SELECT last_insert_rowid();
