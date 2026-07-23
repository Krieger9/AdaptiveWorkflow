CREATE TABLE [dbo].[Teams]
(
    [Id]            UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Teams_Id] DEFAULT (NEWSEQUENTIALID()),
    [Name]          NVARCHAR(200)    NOT NULL,
    [CreatedDate]   DATETIME2(7)     NOT NULL,
    [ModifiedDate]  DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_Teams] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Teams_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO
