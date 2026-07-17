DECLARE @TableName NVARCHAR(128);
DECLARE @IndexName NVARCHAR(128);
DECLARE @SchemaName NVARCHAR(128);
DECLARE @SQL NVARCHAR(MAX);
DECLARE @Message NVARCHAR(MAX);
DECLARE @Fragmentation FLOAT;

DECLARE IndexCursor CURSOR FOR
SELECT 
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM 
    sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
    JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
    JOIN sys.tables t ON ips.object_id = t.object_id
    JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE 
    ips.avg_fragmentation_in_percent > 10  -- Fragmentation threshold
    AND i.type > 0  -- Only non-clustered and clustered indexes (exclude heap)
ORDER BY 
    ips.avg_fragmentation_in_percent DESC;

OPEN IndexCursor;

FETCH NEXT FROM IndexCursor INTO @SchemaName, @TableName, @IndexName, @Fragmentation;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Check the fragmentation level and decide whether to rebuild or reorganize
    IF @Fragmentation > 30
        SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON [' + @SchemaName + '].[' + @TableName + '] REBUILD;';
    ELSE
        SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON [' + @SchemaName + '].[' + @TableName + '] REORGANIZE;';
    
    -- Execute the reorganize or rebuild command
	SET @Message = @SQL + ' Fragmentation: ' + CONVERT(VARCHAR(50), ROUND(@Fragmentation, 0)) + '%';
	RAISERROR('Working: %s', 0, 1, @Message) WITH NOWAIT;
    EXEC sp_executesql @SQL;

    FETCH NEXT FROM IndexCursor INTO @SchemaName, @TableName, @IndexName, @Fragmentation;
END

CLOSE IndexCursor;
DEALLOCATE IndexCursor;
