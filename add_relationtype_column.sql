-- Add RelationType column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Applications]') AND name = 'RelationType')
BEGIN
    ALTER TABLE [Applications] ADD [RelationType] nvarchar(max) NULL;
    PRINT 'RelationType column added successfully.';
END
ELSE
BEGIN
    PRINT 'RelationType column already exists.';
END
GO

