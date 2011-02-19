CREATE TABLE [Albums] (
	[AlbumID] [int] IDENTITY (1, 1) NOT NULL ,
	[Caption] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[IsPublic] [bit] NOT NULL 
) ON [PRIMARY]
GO

CREATE TABLE [Photos] (
	[PhotoID] [int] IDENTITY (1, 1) NOT NULL ,
	[AlbumID] [int] NOT NULL ,
	[Caption] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[BytesOriginal] [image] NOT NULL ,
	[BytesFull] [image] NOT NULL ,
	[BytesPoster] [image] NOT NULL ,
	[BytesThumb] [image] NOT NULL 
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [Albums]
	ADD CONSTRAINT [PK_Albums] PRIMARY KEY
		CLUSTERED ([AlbumID])ON [PRIMARY]
GO

ALTER TABLE [Photos]
	ADD CONSTRAINT [PK_Photos] PRIMARY KEY
		CLUSTERED ([PhotoID]) ON [PRIMARY]
GO

ALTER TABLE [Photos]
	ADD CONSTRAINT [FK_Photos_Albums] FOREIGN KEY ([AlbumID])
	REFERENCES [Albums] ([AlbumID])
		ON DELETE CASCADE
		ON UPDATE CASCADE
GO


SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO


CREATE PROCEDURE AddAlbum
	@Caption nvarchar(50),
	@IsPublic bit
AS
	INSERT INTO [Albums] ([Caption],[IsPublic]) VALUES (@Caption, @IsPublic)
RETURN
GO

CREATE PROCEDURE AddPhoto
		@AlbumID int,
		@Caption nvarchar(50),
		@BytesOriginal image,
		@BytesFull image,
		@BytesPoster image,
		@BytesThumb image
AS
	INSERT INTO [Photos] (
		[AlbumID],
		[BytesOriginal],
		[Caption],
		[BytesFull],
		[BytesPoster],
		[BytesThumb] )
	VALUES (
		@AlbumID,
		@BytesOriginal,
		@Caption,
		@BytesFull,
		@BytesPoster,
		@BytesThumb )
RETURN
GO

CREATE PROCEDURE EditAlbum
	@Caption nvarchar(50),
	@IsPublic bit,
	@AlbumID int
AS
	UPDATE [Albums] 
	SET 
		[Caption] = @Caption, 
		[IsPublic] = @IsPublic 
	WHERE 
		[AlbumID] = @AlbumID
RETURN
GO

CREATE PROCEDURE EditPhoto
	@Caption nvarchar(50),
	@PhotoID int
AS
	UPDATE [Photos]
	SET [Caption] = @Caption
	WHERE [PhotoID]	= @PhotoID
RETURN
GO

CREATE PROCEDURE GetAlbums
	@IsPublic bit
AS
	SELECT 
		[Albums].[AlbumID], 
		[Albums].[Caption], 
		[Albums].[IsPublic], 
		Count([Photos].[PhotoID]) AS NumberOfPhotos 
	FROM [Albums] LEFT JOIN [Photos] 
		ON [Albums].[AlbumID] = [Photos].[AlbumID] 
	WHERE
		([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	GROUP BY 
		[Albums].[AlbumID], 
		[Albums].[Caption], 
		[Albums].[IsPublic]
RETURN
GO

CREATE PROCEDURE GetFirstPhoto
	@AlbumID int,
	@Size int,
	@IsPublic bit
AS
	IF @Size = 1
		SELECT TOP 1 [BytesThumb] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [Albums].[AlbumID] = @AlbumID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE IF @Size = 2
		SELECT TOP 1 [BytesPoster] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [Albums].[AlbumID] = @AlbumID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE IF @Size = 3
		SELECT TOP 1 [BytesFull] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [Albums].[AlbumID] = @AlbumID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE IF @Size = 4
		SELECT TOP 1 [BytesOriginal] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [Albums].[AlbumID] = @AlbumID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE
		SELECT TOP 1 [BytesPoster] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [Albums].[AlbumID] = @AlbumID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
RETURN
GO

CREATE PROCEDURE GetNonEmptyAlbums
AS
	SELECT 
		[Albums].[AlbumID]
	FROM [Albums] LEFT JOIN [Photos] 
		ON [Albums].[AlbumID] = [Photos].[AlbumID] 
	WHERE
		[Albums].[IsPublic] = 1
	GROUP BY 
		[Albums].[AlbumID], 
		[Albums].[Caption], 
		[Albums].[IsPublic]
	HAVING
		Count([Photos].[PhotoID]) > 0
RETURN
GO

CREATE PROCEDURE GetPhoto
	@PhotoID int,
	@Size int,
	@IsPublic bit
AS
	IF @Size = 1
		SELECT TOP 1 [BytesThumb] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [PhotoID] = @PhotoID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE IF @Size = 2
		SELECT TOP 1 [BytesPoster] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [PhotoID] = @PhotoID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE IF @Size = 3
		SELECT TOP 1 [BytesFull] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [PhotoID] = @PhotoID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE IF @Size = 4
		SELECT TOP 1 [BytesOriginal] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [PhotoID] = @PhotoID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
	ELSE
		SELECT TOP 1 [BytesPoster] FROM [Photos] LEFT JOIN [Albums] ON [Albums].[AlbumID] = [Photos].[AlbumID] WHERE [PhotoID] = @PhotoID AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
RETURN
GO

CREATE PROCEDURE GetPhotos
	@AlbumID int,
	@IsPublic bit
AS
	SELECT *
	FROM [Photos] LEFT JOIN [Albums]
		ON [Albums].[AlbumID] = [Photos].[AlbumID] 
	WHERE [Photos].[AlbumID] = @AlbumID
		AND ([Albums].[IsPublic] = @IsPublic OR [Albums].[IsPublic] = 1)
RETURN
GO

CREATE PROCEDURE RemoveAlbum
	@AlbumID int
AS
	DELETE FROM [Albums] WHERE [AlbumID] = @AlbumID
RETURN
GO

CREATE PROCEDURE RemovePhoto
	@PhotoID int
AS
	DELETE FROM [Photos]
	WHERE [PhotoID]	= @PhotoID
RETURN
GO


SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO
