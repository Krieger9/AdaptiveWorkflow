CREATE TABLE [dbo].[ContractStrategicValueLevels]
(
    [Id]        INT            NOT NULL,
    [Code]      NVARCHAR(32)   NOT NULL,
    [Name]      NVARCHAR(100)  NOT NULL,
    [SortOrder] INT            NOT NULL,
    CONSTRAINT [PK_ContractStrategicValueLevels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ContractStrategicValueLevels_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO
