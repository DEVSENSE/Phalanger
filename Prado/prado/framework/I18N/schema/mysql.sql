# 
# Database : `messages` for I18N in PRADO
# 

# --------------------------------------------------------

#
# Table structure for table `catalogue`
#

CREATE TABLE `catalogue` (
  `cat_id` int(11) NOT NULL auto_increment,
  `name` varchar(100) NOT NULL default '',
  `source_lang` varchar(100) NOT NULL default '',
  `target_lang` varchar(100) NOT NULL default '',
  `date_created` int(11) NOT NULL default '0',
  `date_modified` int(11) NOT NULL default '0',
  `author` varchar(255) NOT NULL default '',
  PRIMARY KEY  (`cat_id`)
) TYPE=MyISAM AUTO_INCREMENT=4 ;

#
# Dumping data for table `catalogue`
#

INSERT INTO `catalogue` VALUES (1, 'messages', '', '', 0, 0, '');
INSERT INTO `catalogue` VALUES (2, 'messages.en', '', '', 0, 0, '');
INSERT INTO `catalogue` VALUES (3, 'messages.en_AU', '', '', 0, 0, '');

# --------------------------------------------------------

#
# Table structure for table `trans_unit`
#

CREATE TABLE `trans_unit` (
  `msg_id` int(11) NOT NULL auto_increment,
  `cat_id` int(11) NOT NULL default '1',
  `id` varchar(255) NOT NULL default '',
  `source` text NOT NULL,
  `target` text NOT NULL,
  `comments` text NOT NULL,
  `date_added` int(11) NOT NULL default '0',
  `date_modified` int(11) NOT NULL default '0',
  `author` varchar(255) NOT NULL default '',
  `translated` tinyint(1) NOT NULL default '0',
  PRIMARY KEY  (`msg_id`)
) TYPE=MyISAM AUTO_INCREMENT=6 ;

#
# Dumping data for table `trans_unit`
#

INSERT INTO `trans_unit` VALUES (1, 1, '1', 'Hello', 'Hello World', '', 0, 0, '', 1);
INSERT INTO `trans_unit` VALUES (2, 2, '', 'Hello', 'Hello :)', '', 0, 0, '', 0);
INSERT INTO `trans_unit` VALUES (3, 1, '1', 'Welcome', 'Welcome', '', 0, 0, '', 0);
INSERT INTO `trans_unit` VALUES (4, 3, '', 'Hello', 'G\'day Mate!', '', 0, 0, '', 0);
INSERT INTO `trans_unit` VALUES (5, 3, '', 'Welcome', 'Welcome Mate!', '', 0, 0, '', 0);    