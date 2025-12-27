BEGIN TRANSACTION;
GO

ALTER TABLE [Applications] ADD [City] nvarchar(max) NULL;
GO

ALTER TABLE [Applications] ADD [DocumentSubmissionMethod] int NULL;
GO

ALTER TABLE [Applications] ADD [RelationCNIC] nvarchar(max) NULL;
GO

ALTER TABLE [Applications] ADD [RelationType] nvarchar(max) NULL;
GO

ALTER TABLE [Applications] ADD [SubmissionBy] int NULL;
GO

ALTER TABLE [Applications] ADD [TCSNumber] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251227103516_AddCityAndDocumentSubmissionFields', N'8.0.0');
GO

COMMIT;
GO

