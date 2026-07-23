CREATE TABLE [dbo].[ContractSkillPriorities]
(
    [Id]        INT            NOT NULL,
    [Code]      NVARCHAR(32)   NOT NULL,
    [Name]      NVARCHAR(100)  NOT NULL,
    [SortOrder] INT            NOT NULL,
    CONSTRAINT [PK_ContractSkillPriorities] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ContractSkillPriorities_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO
