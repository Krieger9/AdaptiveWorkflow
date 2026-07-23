CREATE TABLE [dbo].[ContractSkills]
(
    [ContractId]  UNIQUEIDENTIFIER NOT NULL,
    [SkillId]     INT              NOT NULL,
    [PriorityId]  INT              NOT NULL,
    CONSTRAINT [PK_ContractSkills] PRIMARY KEY CLUSTERED ([ContractId] ASC, [SkillId] ASC),
    CONSTRAINT [FK_ContractSkills_Contract]
        FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContractSkills_Skill]
        FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([Id]),
    CONSTRAINT [FK_ContractSkills_Priority]
        FOREIGN KEY ([PriorityId]) REFERENCES [dbo].[ContractSkillPriorities] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_ContractSkills_SkillId]
    ON [dbo].[ContractSkills] ([SkillId]);
GO

CREATE NONCLUSTERED INDEX [IX_ContractSkills_PriorityId]
    ON [dbo].[ContractSkills] ([PriorityId]);
GO
