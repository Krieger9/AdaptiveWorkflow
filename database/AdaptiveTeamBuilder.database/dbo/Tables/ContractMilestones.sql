CREATE TABLE [dbo].[ContractMilestones]
(
    [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ContractMilestones_Id] DEFAULT (NEWSEQUENTIALID()),
    [ContractId]  UNIQUEIDENTIFIER NOT NULL,
    [SortOrder]   INT              NOT NULL,
    [Name]        NVARCHAR(200)    NOT NULL,
    [TargetDate]  DATE             NULL,
    [Description] NVARCHAR(500)    NULL,
    CONSTRAINT [PK_ContractMilestones] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ContractMilestones_Contract]
        FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ContractMilestones_ContractId_SortOrder]
    ON [dbo].[ContractMilestones] ([ContractId] ASC, [SortOrder] ASC);
GO
