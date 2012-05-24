# Database: messages.db for I18N in PRADO
# --------------------------------------------------------

#
# Table structure for table: catalogue
#
CREATE TABLE catalogue ( 
  cat_id INTEGER PRIMARY KEY, 
  name VARCHAR NOT NULL, 
  source_lang VARCHAR , 
  target_lang VARCHAR , 
  date_created INT, 
  date_modified INT, 
  author VARCHAR );

#
# Dumping data for table: catalogue
#
INSERT INTO catalogue VALUES ('1', 'messages', '', '', '', '', '');
INSERT INTO catalogue VALUES ('2', 'messages.en', '', '', '', '', '');
INSERT INTO catalogue VALUES ('3', 'messages.en_AU', '', '', '', '', '');
# --------------------------------------------------------


#
# Table structure for table: trans_unit
#
CREATE TABLE trans_unit ( 
  msg_id INTEGER PRIMARY KEY, 
  cat_id INTEGER NOT NULL DEFAULT '1', 
  id VARCHAR, 
  source TEXT, 
  target TEXT, 
  comments TEXT, 
  date_added INT, 
  date_modified INT, 
  author VARCHAR, 
  translated INT(1) NOT NULL DEFAULT '0' );

#
# Dumping data for table: trans_unit
#
INSERT INTO trans_unit VALUES ('1', '1', '1', 'Hello', 'Hello World', '', '', '', '', '1');
INSERT INTO trans_unit VALUES ('2', '2', '', 'Hello', 'Hello :)', '', '', '', '', '0');
INSERT INTO trans_unit VALUES ('3', '1', '1', 'Welcome', 'Welcome', '', '', '', '', '0');
INSERT INTO trans_unit VALUES ('4', '3', '', 'Hello', 'G''day Mate!', '', '', '', '', '0');
INSERT INTO trans_unit VALUES ('5', '3', '', 'Welcome', 'Welcome Mate!', '', '', '', '', '0');
# --------------------------------------------------------

