CREATE TABLE [dbo].[ContractDeliverables]
(
    [Id]         UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ContractDeliverables_Id] DEFAULT (NEWSEQUENTIALID()),
    [ContractId] UNIQUEIDENTIFIER NOT NULL,
    [SortOrder]  INT              NOT NULL,
    [Title]      NVARCHAR(200)    NOT NULL,
    [Detail]     NVARCHAR(500)    NULL,
    CONSTRAINT [PK_ContractDeliverables] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ContractDeliverables_Contract]
        FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ContractDeliverables_ContractId_SortOrder]
    ON [dbo].[ContractDeliverables] ([ContractId] ASC, [SortOrder] ASC);
GO
