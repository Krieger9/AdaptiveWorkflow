CREATE TABLE [dbo].[RoleSpecialties]
(
    [Id]              INT            NOT NULL,
    [PositionTypeId]  INT            NOT NULL,
    [Code]            NVARCHAR(64)   NOT NULL,
    [Name]            NVARCHAR(100)  NOT NULL,
    CONSTRAINT [PK_RoleSpecialties] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RoleSpecialties_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [FK_RoleSpecialties_PositionTypes]
        FOREIGN KEY ([PositionTypeId]) REFERENCES [dbo].[PositionTypes] ([Id])
);
GO
