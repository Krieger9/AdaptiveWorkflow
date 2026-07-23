CREATE TABLE [dbo].[Contracts]
(
    [Id]                        UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Contracts_Id] DEFAULT (NEWSEQUENTIALID()),
    [Code]                      NVARCHAR(64)     NOT NULL,
    [Title]                     NVARCHAR(200)    NOT NULL,
    [ClientName]                NVARCHAR(200)    NOT NULL,
    [OutcomeSummary]            NVARCHAR(500)    NOT NULL,
    [ScopeSummary]              NVARCHAR(2000)   NOT NULL,
    [EngagementTypeId]          INT              NOT NULL,
    [WorkModeId]                INT              NOT NULL,
    [DurationWeeks]             INT              NULL,
    [StartDate]                 DATE             NULL,
    [TargetDeliveryDate]        DATE             NULL,
    [EstimatedContractValue]    DECIMAL(18, 2)   NOT NULL,
    [EstimatedProfit]           DECIMAL(18, 2)   NOT NULL,
    [EstimatedMarginPercent]    DECIMAL(5, 2)    NOT NULL,
    [WinProbabilityPercent]     DECIMAL(5, 2)    NOT NULL,
    [DeliveryRiskId]            INT              NOT NULL,
    [StrategicValueId]          INT              NOT NULL,
    [StaffingFte]               DECIMAL(6, 1)    NOT NULL,
    [SpecialistStaffingNeeded]  NVARCHAR(200)    NULL,
    [IsDefault]                 BIT              NOT NULL CONSTRAINT [DF_Contracts_IsDefault] DEFAULT (0),
    [CreatedDate]               DATETIME2(7)     NOT NULL,
    [ModifiedDate]              DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Contracts_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [FK_Contracts_EngagementType]
        FOREIGN KEY ([EngagementTypeId]) REFERENCES [dbo].[ContractEngagementTypes] ([Id]),
    CONSTRAINT [FK_Contracts_WorkMode]
        FOREIGN KEY ([WorkModeId]) REFERENCES [dbo].[ContractWorkModes] ([Id]),
    CONSTRAINT [FK_Contracts_DeliveryRisk]
        FOREIGN KEY ([DeliveryRiskId]) REFERENCES [dbo].[ContractDeliveryRiskLevels] ([Id]),
    CONSTRAINT [FK_Contracts_StrategicValue]
        FOREIGN KEY ([StrategicValueId]) REFERENCES [dbo].[ContractStrategicValueLevels] ([Id]),
    CONSTRAINT [CK_Contracts_DurationWeeks]
        CHECK ([DurationWeeks] IS NULL OR [DurationWeeks] >= 0),
    CONSTRAINT [CK_Contracts_EstimatedContractValue]
        CHECK ([EstimatedContractValue] >= 0),
    CONSTRAINT [CK_Contracts_EstimatedProfit]
        CHECK ([EstimatedProfit] >= 0),
    CONSTRAINT [CK_Contracts_EstimatedMarginPercent]
        CHECK ([EstimatedMarginPercent] >= 0 AND [EstimatedMarginPercent] <= 100),
    CONSTRAINT [CK_Contracts_WinProbabilityPercent]
        CHECK ([WinProbabilityPercent] >= 0 AND [WinProbabilityPercent] <= 100),
    CONSTRAINT [CK_Contracts_StaffingFte]
        CHECK ([StaffingFte] > 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Contracts_IsDefault]
    ON [dbo].[Contracts] ([IsDefault])
    WHERE [IsDefault] = 1;
GO

CREATE NONCLUSTERED INDEX [IX_Contracts_EngagementTypeId]
    ON [dbo].[Contracts] ([EngagementTypeId]);
GO

CREATE NONCLUSTERED INDEX [IX_Contracts_WorkModeId]
    ON [dbo].[Contracts] ([WorkModeId]);
GO

CREATE NONCLUSTERED INDEX [IX_Contracts_DeliveryRiskId]
    ON [dbo].[Contracts] ([DeliveryRiskId]);
GO

CREATE NONCLUSTERED INDEX [IX_Contracts_StrategicValueId]
    ON [dbo].[Contracts] ([StrategicValueId]);
GO
