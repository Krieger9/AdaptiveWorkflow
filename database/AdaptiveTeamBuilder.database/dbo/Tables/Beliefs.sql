CREATE TABLE [dbo].[Beliefs]
(
	[Id]              BIGINT           IDENTITY (1, 1) NOT NULL,
	[UserId]          UNIQUEIDENTIFIER NOT NULL,
	[SurfacePath]     NVARCHAR(512)    NOT NULL,
	[Dimension]       NVARCHAR(128)    NOT NULL,
	[Statement]       NVARCHAR(MAX)    NOT NULL,
	[Conviction]      NVARCHAR(32)     NOT NULL CONSTRAINT [DF_Beliefs_Conviction] DEFAULT (N'noticed'),
	[Tenure]          NVARCHAR(MAX)    NULL,
	[LeaningOn]       NVARCHAR(MAX)    NULL,
	[ChangeCriteria]  NVARCHAR(MAX)    NULL,
	[UpdatedAt]       DATETIME2(7)     NOT NULL,
	CONSTRAINT [PK_Beliefs] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_Beliefs_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Beliefs_UserId_SurfacePath_Dimension]
	ON [dbo].[Beliefs] ([UserId] ASC, [SurfacePath] ASC, [Dimension] ASC);
GO
