-- 
-- Database: `time-tracker`
-- 

-- --------------------------------------------------------

-- 
-- Table structure for table `categories`
-- 

CREATE TABLE IF NOT EXISTS `categories` (
  `CategoryID` int(11) NOT NULL auto_increment,
  `Name` varchar(255) NOT NULL,
  `ProjectID` int(11) NOT NULL,
  `ParentCategoryID` int(11) default '0',
  `Abbreviation` varchar(255) default NULL,
  `EstimateDuration` float(10,2) default '0.00',
  PRIMARY KEY  (`CategoryID`),
  UNIQUE KEY `UniqueNamePerProject` (`Name`,`ProjectID`),
  KEY `ProjectID` (`ProjectID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `categories`
-- 


-- --------------------------------------------------------

-- 
-- Table structure for table `project`
-- 

CREATE TABLE IF NOT EXISTS `project` (
  `ProjectID` int(11) NOT NULL auto_increment,
  `Name` varchar(255) NOT NULL,
  `Description` varchar(255) default NULL,
  `CreationDate` datetime NOT NULL,
  `CompletionDate` datetime NOT NULL,
  `Disabled` tinyint(1) NOT NULL default '0',
  `EstimateDuration` float(10,2) NOT NULL default '0.00',
  `CreatorID` varchar(50) NOT NULL,
  `ManagerID` varchar(50) default NULL,
  PRIMARY KEY  (`ProjectID`),
  KEY `Name` (`Name`),
  KEY `CreatorID` (`CreatorID`),
  KEY `ManagerID` (`ManagerID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `project`
-- 


-- --------------------------------------------------------

-- 
-- Table structure for table `project_members`
-- 

CREATE TABLE IF NOT EXISTS `project_members` (
  `UserID` varchar(50) NOT NULL,
  `ProjectID` int(11) NOT NULL,
  PRIMARY KEY  (`UserID`,`ProjectID`),
  KEY `ProjectID` (`ProjectID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `project_members`
-- 


-- --------------------------------------------------------

-- 
-- Table structure for table `role_types`
-- 

CREATE TABLE IF NOT EXISTS `role_types` (
  `RoleType` varchar(50) NOT NULL,
  `Description` varchar(255) NOT NULL,
  PRIMARY KEY  (`RoleType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `role_types`
-- 

INSERT INTO `role_types` (`RoleType`, `Description`) VALUES ('admin', 'Project administrator may additionally view the list of all users.'),
('consultant', 'Consultant may log time entries only.'),
('manager', 'Project manager may additionally edit all projects and view reports.');

-- --------------------------------------------------------

-- 
-- Table structure for table `signon`
-- 

CREATE TABLE IF NOT EXISTS `signon` (
  `SessionToken` varchar(32) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `LastSignOnDate` datetime NOT NULL,
  PRIMARY KEY  (`SessionToken`),
  KEY `Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `signon`
-- 


-- --------------------------------------------------------

-- 
-- Table structure for table `time_entry`
-- 

CREATE TABLE IF NOT EXISTS `time_entry` (
  `EntryID` int(11) NOT NULL auto_increment,
  `EntryCreated` datetime NOT NULL,
  `Duration` float(10,2) NOT NULL default '0.00',
  `Description` varchar(1000) default NULL,
  `CategoryID` int(11) NOT NULL default '0',
  `EntryDate` datetime default NULL,
  `CreatorID` varchar(50) NOT NULL,
  `UserID` varchar(50) NOT NULL,
  PRIMARY KEY  (`EntryID`),
  KEY `CategoryID` (`CategoryID`),
  KEY `CreatorID` (`CreatorID`),
  KEY `UserID` (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `time_entry`
-- 


-- --------------------------------------------------------

-- 
-- Table structure for table `user_roles`
-- 

CREATE TABLE IF NOT EXISTS `user_roles` (
  `UserID` varchar(50) NOT NULL,
  `RoleType` varchar(50) NOT NULL,
  PRIMARY KEY  (`UserID`,`RoleType`),
  KEY `RoleType` (`RoleType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `user_roles`
-- 

INSERT INTO `user_roles` (`UserID`, `RoleType`) VALUES ('admin', 'admin'),
('admin', 'consultant'),
('consultant', 'consultant'),
('manager', 'consultant'),
('admin', 'manager'),
('manager', 'manager');

-- --------------------------------------------------------

-- 
-- Table structure for table `users`
-- 

CREATE TABLE IF NOT EXISTS `users` (
  `Username` varchar(50) NOT NULL,
  `Password` varchar(50) NOT NULL,
  `EmailAddress` varchar(100) NOT NULL,
  `Disabled` tinyint(1) NOT NULL default '0',
  PRIMARY KEY  (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- 
-- Dumping data for table `users`
-- 

INSERT INTO `users` (`Username`, `Password`, `EmailAddress`, `Disabled`) VALUES ('admin', '21232f297a57a5a743894a0e4a801fc3', 'admin@pradosoft.com', 0),
('consultant', '7adfa4f2ba9323e6c1e024de375434b0', 'consultant@pradosoft.com', 0),
('manager', '1d0258c2440a8d19e716292b231e3190', 'manager@pradosoft.com', 0);

-- 
-- Constraints for dumped tables
-- 

-- 
-- Constraints for table `categories`
-- 
ALTER TABLE `categories`
  ADD CONSTRAINT `categories_ibfk_1` FOREIGN KEY (`ProjectID`) REFERENCES `project` (`ProjectID`) ON DELETE CASCADE;

-- 
-- Constraints for table `project`
-- 
ALTER TABLE `project`
  ADD CONSTRAINT `project_ibfk_6` FOREIGN KEY (`ManagerID`) REFERENCES `users` (`Username`),
  ADD CONSTRAINT `project_ibfk_5` FOREIGN KEY (`CreatorID`) REFERENCES `users` (`Username`);

-- 
-- Constraints for table `project_members`
-- 
ALTER TABLE `project_members`
  ADD CONSTRAINT `project_members_ibfk_6` FOREIGN KEY (`ProjectID`) REFERENCES `project` (`ProjectID`) ON DELETE CASCADE,
  ADD CONSTRAINT `project_members_ibfk_5` FOREIGN KEY (`UserID`) REFERENCES `users` (`Username`) ON DELETE CASCADE;

-- 
-- Constraints for table `signon`
-- 
ALTER TABLE `signon`
  ADD CONSTRAINT `signon_ibfk_1` FOREIGN KEY (`Username`) REFERENCES `users` (`Username`);

-- 
-- Constraints for table `time_entry`
-- 
ALTER TABLE `time_entry`
  ADD CONSTRAINT `time_entry_ibfk_8` FOREIGN KEY (`UserID`) REFERENCES `users` (`Username`),
  ADD CONSTRAINT `time_entry_ibfk_6` FOREIGN KEY (`CategoryID`) REFERENCES `categories` (`CategoryID`) ON DELETE CASCADE,
  ADD CONSTRAINT `time_entry_ibfk_7` FOREIGN KEY (`CreatorID`) REFERENCES `users` (`Username`);

-- 
-- Constraints for table `user_roles`
-- 
ALTER TABLE `user_roles`
  ADD CONSTRAINT `user_roles_ibfk_2` FOREIGN KEY (`RoleType`) REFERENCES `role_types` (`RoleType`),
  ADD CONSTRAINT `user_roles_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`Username`);
