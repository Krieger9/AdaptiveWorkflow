CREATE TABLE [dbo].[PositionTypes]
(
    [Id]         INT            NOT NULL,
    [Code]       NVARCHAR(32)   NOT NULL,
    [Name]       NVARCHAR(100)  NOT NULL,
    [SortOrder]  INT            NOT NULL,
    CONSTRAINT [PK_PositionTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PositionTypes_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO
