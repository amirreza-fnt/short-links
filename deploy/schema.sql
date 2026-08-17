IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [LinkGroups] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(64) NOT NULL,
    [Description] nvarchar(512) NULL,
    [UtmParamsJson] nvarchar(2048) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_LinkGroups] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ShortLinks] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(32) NOT NULL,
    [TargetUrl] nvarchar(2048) NOT NULL,
    [GroupId] bigint NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NULL,
    [IsActive] bit NOT NULL,
    [ClickCount] bigint NOT NULL,
    [LastRedirectAt] datetimeoffset NULL,
    CONSTRAINT [PK_ShortLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShortLinks_LinkGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [LinkGroups] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [ClickStats] (
    [Id] bigint NOT NULL IDENTITY,
    [ShortLinkId] bigint NOT NULL,
    [ClickedAt] datetimeoffset NOT NULL,
    [IpAddress] nvarchar(64) NULL,
    [UserAgent] nvarchar(512) NULL,
    [DeviceType] nvarchar(32) NULL,
    [Browser] nvarchar(64) NULL,
    [Referrer] nvarchar(2048) NULL,
    [UtmTemplate] nvarchar(64) NULL,
    [QueryString] nvarchar(1024) NULL,
    CONSTRAINT [PK_ClickStats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClickStats_ShortLinks_ShortLinkId] FOREIGN KEY ([ShortLinkId]) REFERENCES [ShortLinks] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ClickStats_ShortLinkId_ClickedAt] ON [ClickStats] ([ShortLinkId], [ClickedAt]);
GO

CREATE UNIQUE INDEX [IX_LinkGroups_Name] ON [LinkGroups] ([Name]);
GO

CREATE UNIQUE INDEX [IX_ShortLinks_Code] ON [ShortLinks] ([Code]);
GO

CREATE INDEX [IX_ShortLinks_CreatedAt] ON [ShortLinks] ([CreatedAt]);
GO

CREATE INDEX [IX_ShortLinks_ExpiresAt] ON [ShortLinks] ([ExpiresAt]);
GO

CREATE INDEX [IX_ShortLinks_GroupId] ON [ShortLinks] ([GroupId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260817061012_InitialCreate', N'8.0.30');
GO

COMMIT;
GO

