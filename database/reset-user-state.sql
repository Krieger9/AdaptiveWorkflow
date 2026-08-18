/*
    Reset Adaptive Team Builder to a fresh user state.

    Deletes:
      - Users
      - Beliefs and belief documents
      - Interaction tracking
      - Turn digests and revisions
      - Teams, including members, hidden profiles, and position requirements
      - Legacy collaboration state tables, when still present

    Preserves:
      - Contracts and their supporting data
      - Employee profiles and skills
      - Lookup/reference tables

    This script is intentionally kept outside the SQL database project so it is
    never executed as part of a normal DACPAC publish.

    Note: the application also archives JSONL interaction logs and versioned
    belief documents under its configured DataDirectory. Those files are not
    database records and cannot be removed by this SQL script. After this reset,
    they are orphaned because replacement users receive new database IDs.
*/

USE [AdaptiveTeamBuilder];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Change to 1 only when you intend to permanently remove all user state.
DECLARE @ConfirmReset bit = 0;

IF @ConfirmReset <> 1
BEGIN
    THROW 50000, 'Reset cancelled. Set @ConfirmReset to 1, review the target database, and run again.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Deleted table
    (
        [SortOrder] int           NOT NULL,
        [TableName] nvarchar(128) NOT NULL,
        [RowsDeleted] int         NOT NULL
    );

    -- Revisions must be deleted before Users because that foreign key does not cascade.
    DELETE FROM [dbo].[Revisions];
    INSERT INTO @Deleted VALUES (10, N'Revisions', @@ROWCOUNT);

    DELETE FROM [dbo].[TurnDigests];
    INSERT INTO @Deleted VALUES (20, N'TurnDigests', @@ROWCOUNT);

    DELETE FROM [dbo].[BeliefDocuments];
    INSERT INTO @Deleted VALUES (30, N'BeliefDocuments', @@ROWCOUNT);

    DELETE FROM [dbo].[Beliefs];
    INSERT INTO @Deleted VALUES (40, N'Beliefs', @@ROWCOUNT);

    DELETE FROM [dbo].[Interactions];
    INSERT INTO @Deleted VALUES (50, N'Interactions', @@ROWCOUNT);

    -- These pre-framework tables are no longer part of the database project, but
    -- may remain in databases published before DropObjectsNotInSource was enabled.
    IF OBJECT_ID(N'dbo.CollaborationStateChangeLogs', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[CollaborationStateChangeLogs];
        INSERT INTO @Deleted VALUES (51, N'CollaborationStateChangeLogs (legacy)', @@ROWCOUNT);
    END;

    IF OBJECT_ID(N'dbo.CollaborationTurnDigests', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[CollaborationTurnDigests];
        INSERT INTO @Deleted VALUES (52, N'CollaborationTurnDigests (legacy)', @@ROWCOUNT);
    END;

    IF OBJECT_ID(N'dbo.UserCollaborationStates', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[UserCollaborationStates];
        INSERT INTO @Deleted VALUES (53, N'UserCollaborationStates (legacy)', @@ROWCOUNT);
    END;

    DELETE FROM [dbo].[Users];
    INSERT INTO @Deleted VALUES (60, N'Users', @@ROWCOUNT);

    -- These child rows cascade, but explicit deletes make the reset scope and counts clear.
    DELETE FROM [dbo].[TeamMembers];
    INSERT INTO @Deleted VALUES (70, N'TeamMembers', @@ROWCOUNT);

    DELETE FROM [dbo].[TeamHiddenProfiles];
    INSERT INTO @Deleted VALUES (80, N'TeamHiddenProfiles', @@ROWCOUNT);

    DELETE FROM [dbo].[TeamPositionRequirements];
    INSERT INTO @Deleted VALUES (90, N'TeamPositionRequirements', @@ROWCOUNT);

    DELETE FROM [dbo].[Teams];
    INSERT INTO @Deleted VALUES (100, N'Teams', @@ROWCOUNT);

    -- Start tracking identifiers at 1 again.
    DBCC CHECKIDENT (N'dbo.Revisions', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.TurnDigests', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Beliefs', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Interactions', RESEED, 0) WITH NO_INFOMSGS;

    IF OBJECT_ID(N'dbo.CollaborationStateChangeLogs', N'U') IS NOT NULL
    BEGIN
        DBCC CHECKIDENT (N'dbo.CollaborationStateChangeLogs', RESEED, 0) WITH NO_INFOMSGS;
    END;

    IF OBJECT_ID(N'dbo.CollaborationTurnDigests', N'U') IS NOT NULL
    BEGIN
        DBCC CHECKIDENT (N'dbo.CollaborationTurnDigests', RESEED, 0) WITH NO_INFOMSGS;
    END;

    COMMIT TRANSACTION;

    SELECT [TableName], [RowsDeleted]
    FROM @Deleted
    ORDER BY [SortOrder];

    PRINT N'Adaptive Team Builder user state reset completed.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO
