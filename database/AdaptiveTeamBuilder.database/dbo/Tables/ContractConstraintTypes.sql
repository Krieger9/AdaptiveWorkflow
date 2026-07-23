CREATE TABLE [dbo].[ContractConstraintTypes]
(
    [Id]        INT            NOT NULL,
    [Code]      NVARCHAR(64)   NOT NULL,
    [Name]      NVARCHAR(100)  NOT NULL,
    [SortOrder] INT            NOT NULL,
    CONSTRAINT [PK_ContractConstraintTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ContractConstraintTypes_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO
