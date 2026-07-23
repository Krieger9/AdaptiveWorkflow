CREATE TABLE [dbo].[Skills]
(
    [Id]    INT            NOT NULL IDENTITY(1,1),
    [Name]  NVARCHAR(100)  NOT NULL,
    CONSTRAINT [PK_Skills] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Skills_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO
