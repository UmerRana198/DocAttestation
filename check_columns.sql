-- Check which columns exist in the Applications table
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Applications'
    AND COLUMN_NAME IN ('City', 'DocumentSubmissionMethod', 'RelationCNIC', 'RelationType', 'SubmissionBy', 'TCSNumber')
ORDER BY COLUMN_NAME;
GO

