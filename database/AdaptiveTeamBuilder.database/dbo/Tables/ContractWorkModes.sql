CREATE TABLE [dbo].[ContractWorkModes]
(
    [Id]        INT            NOT NULL,
    [Code]      NVARCHAR(32)   NOT NULL,
    [Name]      NVARCHAR(100)  NOT NULL,
    [SortOrder] INT            NOT NULL,
    CONSTRAINT [PK_ContractWorkModes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ContractWorkModes_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO
