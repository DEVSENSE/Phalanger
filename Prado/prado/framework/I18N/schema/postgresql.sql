
-- Table structure for table catalogue

CREATE TABLE catalogue (
  cat_id serial NOT NULL primary key,
  name varchar(100) NOT NULL default '',
  source_lang varchar(100) NOT NULL default '',
  target_lang varchar(100) NOT NULL default '',
  date_created int NOT NULL default 0,
  date_modified int NOT NULL default 0,
  author varchar(255) NOT NULL default ''
);

-- Dumping data for table catalogue

INSERT INTO catalogue VALUES (nextval('catalogue_cat_id_seq'), 'messages', '', '', 0, 0, '');
INSERT INTO catalogue VALUES (nextval('catalogue_cat_id_seq'), 'messages.en', '', '', 0, 0, '');
INSERT INTO catalogue VALUES (nextval('catalogue_cat_id_seq'), 'messages.en_AU', '', '', 0, 0, '');

-- Table structure for table trans_unit

CREATE TABLE trans_unit (
  msg_id serial NOT NULL primary key,
  cat_id int NOT NULL default 1,
  id varchar(255) NOT NULL default '',
  source text NOT NULL,
  target text NOT NULL default '',
  comments text NOT NULL default '',
  date_added int NOT NULL default 0,
  date_modified int NOT NULL default 0,
  author varchar(255) NOT NULL default '',
  translated smallint NOT NULL default 0
);

INSERT INTO trans_unit VALUES (nextval('trans_unit_msg_id_seq'), 1, '1', 'Hello', 'Hello World', '', 0, 0, '', 1);
INSERT INTO trans_unit VALUES (nextval('trans_unit_msg_id_seq'), 2, '', 'Hello', 'Hello :)', '', 0, 0, '', 0);
INSERT INTO trans_unit VALUES (nextval('trans_unit_msg_id_seq'), 1, '1', 'Welcome', 'Welcome', '', 0, 0, '', 0);
INSERT INTO trans_unit VALUES (nextval('trans_unit_msg_id_seq'), 3, '', 'Hello', 'G''day Mate!', '', 0, 0, '', 0);
INSERT INTO trans_unit VALUES (nextval('trans_unit_msg_id_seq'), 3, '', 'Welcome', 'Welcome Mate!', '', 0, 0, '', 0);    

