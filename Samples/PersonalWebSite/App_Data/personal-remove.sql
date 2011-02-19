if exists (select * from dbo.sysobjects where id = object_id(N'[FK_Photos_Albums]') and OBJECTPROPERTY(id, N'IsForeignKey') = 1)
ALTER TABLE [Photos] DROP CONSTRAINT FK_Photos_Albums
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[AddAlbum]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [AddAlbum]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[AddPhoto]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [AddPhoto]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[EditAlbum]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [EditAlbum]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[EditPhoto]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [EditPhoto]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[GetAlbums]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetAlbums]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[GetFirstPhoto]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetFirstPhoto]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[GetNonEmptyAlbums]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetNonEmptyAlbums]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[GetPhoto]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetPhoto]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[GetPhotos]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetPhotos]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[RemoveAlbum]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [RemoveAlbum]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[RemovePhoto]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [RemovePhoto]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[Albums]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [Albums]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[Photos]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [Photos]
GO