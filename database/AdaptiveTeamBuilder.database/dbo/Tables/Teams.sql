CREATE TABLE [dbo].[Teams]
(
    [Id]            UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Teams_Id] DEFAULT (NEWSEQUENTIALID()),
    [Name]          NVARCHAR(200)    NOT NULL,
    [ContractId]    UNIQUEIDENTIFIER NOT NULL,
    [CreatedDate]   DATETIME2(7)     NOT NULL,
    [ModifiedDate]  DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_Teams] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Teams_ContractId_Name] UNIQUE NONCLUSTERED ([ContractId] ASC, [Name] ASC),
    CONSTRAINT [FK_Teams_Contract]
        FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_Teams_ContractId]
    ON [dbo].[Teams] ([ContractId]);
GO
