CREATE TABLE [dbo].[ContractEngagementTypes]
(
    [Id]        INT            NOT NULL,
    [Code]      NVARCHAR(32)   NOT NULL,
    [Name]      NVARCHAR(100)  NOT NULL,
    [SortOrder] INT            NOT NULL,
    CONSTRAINT [PK_ContractEngagementTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ContractEngagementTypes_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO
