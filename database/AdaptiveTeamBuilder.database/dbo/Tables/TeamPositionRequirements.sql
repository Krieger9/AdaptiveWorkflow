CREATE TABLE [dbo].[TeamPositionRequirements]
(
    [TeamId]          UNIQUEIDENTIFIER NOT NULL,
    [PositionTypeId]  INT              NOT NULL,
    [RequiredCount]   INT              NOT NULL CONSTRAINT [DF_TeamPositionRequirements_RequiredCount] DEFAULT (0),
    CONSTRAINT [PK_TeamPositionRequirements] PRIMARY KEY CLUSTERED ([TeamId] ASC, [PositionTypeId] ASC),
    CONSTRAINT [CK_TeamPositionRequirements_RequiredCount] CHECK ([RequiredCount] >= 0),
    CONSTRAINT [FK_TeamPositionRequirements_Teams]
        FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Teams] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TeamPositionRequirements_PositionTypes]
        FOREIGN KEY ([PositionTypeId]) REFERENCES [dbo].[PositionTypes] ([Id])
);
GO
