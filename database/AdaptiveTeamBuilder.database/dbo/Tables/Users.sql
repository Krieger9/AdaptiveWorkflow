CREATE TABLE [dbo].[Users]
(
    [Id]                UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Users_Id] DEFAULT (NEWSEQUENTIALID()),
    [AzureAdObjectId]   NVARCHAR(64)     NOT NULL,
    [UserName]          NVARCHAR(256)    NOT NULL,
    [FirstName]         NVARCHAR(100)    NULL,
    [LastName]          NVARCHAR(100)    NULL,
    [DisplayName]       NVARCHAR(200)    NULL,
    [CreatedDate]       DATETIME2(7)     NOT NULL,
    [ModifiedDate]      DATETIME2(7)     NOT NULL,
    [LastLoggedInDate]  DATETIME2(7)     NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_AzureAdObjectId] UNIQUE NONCLUSTERED ([AzureAdObjectId] ASC),
    CONSTRAINT [UQ_Users_UserName] UNIQUE NONCLUSTERED ([UserName] ASC)
);
GO
