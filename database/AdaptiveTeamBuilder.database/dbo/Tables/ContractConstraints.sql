CREATE TABLE [dbo].[ContractConstraints]
(
    [ContractId]       UNIQUEIDENTIFIER NOT NULL,
    [ConstraintTypeId] INT              NOT NULL,
    CONSTRAINT [PK_ContractConstraints] PRIMARY KEY CLUSTERED ([ContractId] ASC, [ConstraintTypeId] ASC),
    CONSTRAINT [FK_ContractConstraints_Contract]
        FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContractConstraints_ConstraintType]
        FOREIGN KEY ([ConstraintTypeId]) REFERENCES [dbo].[ContractConstraintTypes] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_ContractConstraints_ConstraintTypeId]
    ON [dbo].[ContractConstraints] ([ConstraintTypeId]);
GO
