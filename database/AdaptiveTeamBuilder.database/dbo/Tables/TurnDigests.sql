CREATE TABLE [dbo].[TurnDigests]
(
	[Id]           BIGINT           IDENTITY (1, 1) NOT NULL,
	[UserId]       UNIQUEIDENTIFIER NOT NULL,
	[SurfacePath]  NVARCHAR(512)    NOT NULL,
	[Sequence]     INT              NOT NULL,
	[CreatedAt]    DATETIME2(7)     NOT NULL,
	[DigestText]   NVARCHAR(MAX)    NOT NULL,
	CONSTRAINT [PK_TurnDigests] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_TurnDigests_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_TurnDigests_UserId_Sequence]
	ON [dbo].[TurnDigests] ([UserId] ASC, [Sequence] ASC);
GO
