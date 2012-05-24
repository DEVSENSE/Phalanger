CREATE TABLE tblUsers (
  id			INTEGER NOT NULL PRIMARY KEY,
  name			VARCHAR(128) NOT NULL UNIQUE,
  full_name		VARCHAR(128) DEFAULT '',
  role          INTEGER NOT NULL DEFAULT 0, /* 0: user, 1: admin */
  passwd		VARCHAR(128) NOT NULL,
  vcode			VARCHAR(128) DEFAULT '',
  email		    VARCHAR(128) NOT NULL,
  reg_time		INTEGER NOT NULL,
  status		INTEGER NOT NULL DEFAULT 0, /* 0: normal, 1: disabled, 2: pending approval */
  website	    VARCHAR(128) DEFAULT ''
);

CREATE TABLE tblPosts (
  id			INTEGER NOT NULL PRIMARY KEY,
  author_id		INTEGER NOT NULL,
  create_time	INTEGER NOT NULL,
  modify_time	INTEGER DEFAULT 0,
  title			VARCHAR(256) NOT NULL,
  content		TEXT NOT NULL,
  status		INTEGER NOT NULL DEFAULT 0, /* 0: published, 1: draft, 2: pending approval */
  comment_count	INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE tblComments (
  id			INTEGER NOT NULL PRIMARY KEY,
  post_id		INTEGER NOT NULL,
  author_name	VARCHAR(64) NOT NULL,
  author_email	VARCHAR(128) NOT NULL,
  author_website VARCHAR(128) DEFAULT '',
  author_ip		CHAR(16) NOT NULL,
  create_time	INTEGER NOT NULL,
  status		INTEGER NOT NULL DEFAULT 0, /* 0: published, 1: pending approval */
  content		TEXT NOT NULL
);

CREATE TABLE tblCategories (
  id			INTEGER NOT NULL PRIMARY KEY,
  name			VARCHAR(128) NOT NULL UNIQUE,
  description	TEXT DEFAULT '',
  post_count	INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE tblAttachments (
  id			VARCHAR(128) NOT NULL PRIMARY KEY,
  post_id		INTEGER NOT NULL,
  create_time	INTEGER NOT NULL,
  file_name		VARCHAR(128) NOT NULL,
  file_size		INTEGER NOT NULL,
  mime_type		VARCHAR(32) NOT NULL DEFAULT 'text/html',
  download_count INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE tblPost2Category (
  post_id		INTEGER NOT NULL,
  category_id	INTEGER NOT NULL,
  PRIMARY KEY (post_id, category_id)
);

INSERT INTO tblUsers (id,name,full_name,role,status,passwd,email,reg_time,website)
	VALUES (1,'admin','Prado User',1,0,'4d688da592969d0a56b5accec3ce8554','admin@example.com',1148819681,'http://www.pradosoft.com');

INSERT INTO tblPosts (id,author_id,create_time,title,content,status)
	VALUES (1,1,1148819691,'Welcome to Prado Weblog','Congratulations! You have successfully installed Prado Blog -- a PRADO-driven weblog system. A default administrator account has been created. Please login with <b>admin/prado</b> and update your password as soon as possible.',0);

INSERT INTO tblCategories (name,description,post_count)
	VALUES ('Miscellaneous','This category holds posts on any topic.',1);

INSERT INTO tblCategories (name,description,post_count)
	VALUES ('PRADO','Topics related with the PRADO framework.',0);

INSERT INTO tblCategories (name,description,post_count)
	VALUES ('PHP','Topics related with PHP.',0);

INSERT INTO tblPost2Category (post_id,category_id)
	VALUES (1,1);
