CREATE TABLE [dbo].[Interactions]
(
	[Id]                   BIGINT           IDENTITY (1, 1) NOT NULL,
	[UserId]               UNIQUEIDENTIFIER NOT NULL,
	[SessionId]            NVARCHAR(64)     NOT NULL,
	[ClientInteractionId]  NVARCHAR(64)     NOT NULL,
	[Seq]                  INT              NOT NULL,
	[At]                   DATETIME2(7)     NOT NULL,
	[SurfacePath]          NVARCHAR(512)    NOT NULL,
	[ControlId]            NVARCHAR(128)    NULL,
	[Action]               NVARCHAR(64)     NOT NULL,
	[ValueBefore]          NVARCHAR(256)    NULL,
	[ValueAfter]           NVARCHAR(256)    NULL,
	[Causation]            NVARCHAR(32)     NOT NULL CONSTRAINT [DF_Interactions_Causation] DEFAULT (N'user'),
	[Reversal]             BIT              NOT NULL CONSTRAINT [DF_Interactions_Reversal] DEFAULT (0),
	[EntityJson]           NVARCHAR(MAX)    NULL,
	[ChoiceSetJson]        NVARCHAR(MAX)    NULL,
	[MetaJson]             NVARCHAR(MAX)    NULL,
	CONSTRAINT [PK_Interactions] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_Interactions_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Interactions_UserId_SessionId_Seq]
	ON [dbo].[Interactions] ([UserId] ASC, [SessionId] ASC, [Seq] ASC);
GO
