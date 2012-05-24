<?php
class PHP_ShellPrototypes {
    static private $instance = null;

    protected $prototype = array (
  'XMLReader::close' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Closes xmlreader - current frees resources until xmlTextReaderClose is fixed in libxml',
  ),
  'XMLReader::getAttribute' => 
  array (
    'return' => 'string',
    'params' => 'string name',
    'description' => 'Get value of an attribute from current element',
  ),
  'XMLReader::getAttributeNo' => 
  array (
    'return' => 'string',
    'params' => 'int index',
    'description' => 'Get value of an attribute at index from current element',
  ),
  'XMLReader::getAttributeNs' => 
  array (
    'return' => 'string',
    'params' => 'string name, string namespaceURI',
    'description' => 'Get value of a attribute via name and namespace from current element',
  ),
  'XMLReader::getParserProperty' => 
  array (
    'return' => 'boolean',
    'params' => 'int property',
    'description' => 'Indicates whether given property (one of the parser option constants) is set or not on parser',
  ),
  'XMLReader::isValid' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns boolean indicating if parsed document is valid or not.Must set XMLREADER_LOADDTD or XMLREADER_VALIDATE parser option prior to the first call to reador this method will always return FALSE',
  ),
  'XMLReader::lookupNamespace' => 
  array (
    'return' => 'string',
    'params' => 'string prefix',
    'description' => 'Return namespaceURI for associated prefix on current node',
  ),
  'XMLReader::moveToAttribute' => 
  array (
    'return' => 'boolean',
    'params' => 'string name',
    'description' => 'Positions reader at specified attribute - Returns TRUE on success and FALSE on failure',
  ),
  'XMLReader::moveToAttributeNo' => 
  array (
    'return' => 'boolean',
    'params' => 'int index',
    'description' => 'Positions reader at attribute at spcecified index.Returns TRUE on success and FALSE on failure',
  ),
  'XMLReader::moveToAttributeNs' => 
  array (
    'return' => 'boolean',
    'params' => 'string name, string namespaceURI',
    'description' => 'Positions reader at attribute spcified by name and namespaceURI.Returns TRUE on success and FALSE on failure',
  ),
  'XMLReader::moveToElement' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Moves the position of the current instance to the node that contains the current Attribute node.',
  ),
  'XMLReader::moveToFirstAttribute' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Moves the position of the current instance to the first attribute associated with the current node.',
  ),
  'XMLReader::moveToNextAttribute' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Moves the position of the current instance to the next attribute associated with the current node.',
  ),
  'XMLReader::read' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Moves the position of the current instance to the next node in the stream.',
  ),
  'XMLReader::next' => 
  array (
    'return' => 'boolean',
    'params' => '[string localname]',
    'description' => 'Moves the position of the current instance to the next node in the stream.',
  ),
  'XMLReader::open' => 
  array (
    'return' => 'boolean',
    'params' => 'string URI',
    'description' => 'Sets the URI that the the XMLReader will parse.',
  ),
  'XMLReader::setParserProperty' => 
  array (
    'return' => 'boolean',
    'params' => 'int property, boolean value',
    'description' => 'Sets parser property (one of the parser option constants).Properties must be set after open() or XML() and before the first read() is called',
  ),
  'XMLReader::setRelaxNGSchemaSource' => 
  array (
    'return' => 'boolean',
    'params' => 'string source',
    'description' => 'Sets the string that the the XMLReader will parse.',
  ),
  'XMLReader::XML' => 
  array (
    'return' => 'boolean',
    'params' => 'string source',
    'description' => 'Sets the string that the the XMLReader will parse.',
  ),
  'XMLReader::expand' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Moves the position of the current instance to the next node in the stream.',
  ),
  'SimpleXMLElement::asXML' => 
  array (
    'return' => 'string',
    'params' => '[string filename]',
    'description' => 'Return a well-formed XML string based on SimpleXML element',
  ),
  'SimpleXMLElement::getNamespaces' => 
  array (
    'return' => 'string',
    'params' => '[bool recursve]',
    'description' => 'Return all namespaces in use',
  ),
  'SimpleXMLElement::getDocNamespaces' => 
  array (
    'return' => 'string',
    'params' => '[bool recursive]',
    'description' => 'Return all namespaces registered with document',
  ),
  'SimpleXMLElement::children' => 
  array (
    'return' => 'object',
    'params' => '[string ns]',
    'description' => 'Finds children of given node',
  ),
  'SimpleXMLElement::getName' => 
  array (
    'return' => 'object',
    'params' => '',
    'description' => 'Finds children of given node',
  ),
  'SimpleXMLElement::attributes' => 
  array (
    'return' => 'array',
    'params' => '[string ns]',
    'description' => 'Identifies an element\'s attributes',
  ),
  'SimpleXMLElement::addChild' => 
  array (
    'return' => 'void',
    'params' => 'string qName [, string value [,string ns]]',
    'description' => 'Add Element with optional namespace information',
  ),
  'SimpleXMLElement::addAttribute' => 
  array (
    'return' => 'void',
    'params' => 'string qName, string value [,string ns]',
    'description' => 'Add Attribute with optional namespace information',
  ),
  'simplexml_load_file' => 
  array (
    'return' => 'simplemxml_element',
    'params' => 'string filename [, string class_name [, int options]]',
    'description' => 'Load a filename and return a simplexml_element object to allow for processing',
  ),
  'simplexml_load_string' => 
  array (
    'return' => 'simplemxml_element',
    'params' => 'string data [, string class_name [, int options]]',
    'description' => 'Load a string and return a simplexml_element object to allow for processing',
  ),
  'simplexml_import_dom' => 
  array (
    'return' => 'simplemxml_element',
    'params' => 'domNode node [, string class_name]',
    'description' => 'Get a simplexml_element object from dom to allow for processing',
  ),
  'snmpget' => 
  array (
    'return' => 'string',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch a SNMP object',
  ),
  'snmpgetnext' => 
  array (
    'return' => 'string',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch a SNMP object',
  ),
  'snmpwalk' => 
  array (
    'return' => 'array',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Return all objects under the specified object id',
  ),
  'snmprealwalk' => 
  array (
    'return' => 'array',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Return all objects including their respective object id withing the specified one',
  ),
  'snmp_get_quick_print' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Return the current status of quick_print',
  ),
  'snmp_set_quick_print' => 
  array (
    'return' => 'void',
    'params' => 'int quick_print',
    'description' => 'Return all objects including their respective object id withing the specified one',
  ),
  'snmp_set_enum_print' => 
  array (
    'return' => 'void',
    'params' => 'int enum_print',
    'description' => 'Return all values that are enums with their enum value instead of the raw integer',
  ),
  'snmp_set_oid_numeric_print' => 
  array (
    'return' => 'void',
    'params' => 'int oid_numeric_print',
    'description' => 'Return all objects including their respective object id withing the specified one',
  ),
  'snmpset' => 
  array (
    'return' => 'int',
    'params' => 'string host, string community, string object_id, string type, mixed value [, int timeout [, int retries]]',
    'description' => 'Set the value of a SNMP object',
  ),
  'snmp2_get' => 
  array (
    'return' => 'string',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch a SNMP object',
  ),
  'snmp2_getnext' => 
  array (
    'return' => 'string',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch a SNMP object',
  ),
  'snmp2_walk' => 
  array (
    'return' => 'array',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Return all objects under the specified object id',
  ),
  'snmp2_real_walk' => 
  array (
    'return' => 'array',
    'params' => 'string host, string community, string object_id [, int timeout [, int retries]]',
    'description' => 'Return all objects including their respective object id withing the specified one',
  ),
  'snmp2_set' => 
  array (
    'return' => 'int',
    'params' => 'string host, string community, string object_id, string type, mixed value [, int timeout [, int retries]]',
    'description' => 'Set the value of a SNMP object',
  ),
  'php_snmpv3' => 
  array (
    'return' => 'void',
    'params' => 'INTERNAL_FUNCTION_PARAMETERS, int st',
    'description' => '** Generic SNMPv3 object fetcher* From here is passed on the the common internal object fetcher.** st=SNMP_CMD_GET   snmp3_get() - query an agent and return a single value.* st=SNMP_CMD_GETNEXT   snmp3_getnext() - query an agent and return the next single value.* st=SNMP_CMD_WALK   snmp3_walk() - walk the mib and return a single dimensional array*                       containing the values.* st=SNMP_CMD_REALWALK   snmp3_real_walk() - walk the mib and return an*                            array of oid,value pairs.* st=SNMP_CMD_SET  snmp3_set() - query an agent and set a single value*',
  ),
  'snmp3_get' => 
  array (
    'return' => 'int',
    'params' => 'string host, string sec_name, string sec_level, string auth_protocol, string auth_passphrase, string priv_protocol, string priv_passphrase, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch the value of a SNMP object',
  ),
  'snmp3_getnext' => 
  array (
    'return' => 'int',
    'params' => 'string host, string sec_name, string sec_level, string auth_protocol, string auth_passphrase, string priv_protocol, string priv_passphrase, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch the value of a SNMP object',
  ),
  'snmp3_walk' => 
  array (
    'return' => 'int',
    'params' => 'string host, string sec_name, string sec_level, string auth_protocol, string auth_passphrase, string priv_protocol, string priv_passphrase, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch the value of a SNMP object',
  ),
  'snmp3_real_walk' => 
  array (
    'return' => 'int',
    'params' => 'string host, string sec_name, string sec_level, string auth_protocol, string auth_passphrase, string priv_protocol, string priv_passphrase, string object_id [, int timeout [, int retries]]',
    'description' => 'Fetch the value of a SNMP object',
  ),
  'snmp3_set' => 
  array (
    'return' => 'int',
    'params' => 'string host, string sec_name, string sec_level, string auth_protocol, string auth_passphrase, string priv_protocol, string priv_passphrase, string object_id, string type, mixed value [, int timeout [, int retries]]',
    'description' => 'Fetch the value of a SNMP object',
  ),
  'snmp_set_valueretrieval' => 
  array (
    'return' => 'int',
    'params' => 'int method',
    'description' => 'Specify the method how the SNMP values will be returned',
  ),
  'snmp_get_valueretrieval' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Return the method how the SNMP values will be returned',
  ),
  'snmp_read_mib' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Reads and parses a MIB file into the active MIB tree.',
  ),
  'mysqli_embedded_server_start' => 
  array (
    'return' => 'bool',
    'params' => 'bool start, array arguments, array groups',
    'description' => 'initialize and start embedded server',
  ),
  'mysqli_embedded_server_end' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => '',
  ),
  'mysqli_connect' => 
  array (
    'return' => 'object',
    'params' => '[string hostname [,string username [,string passwd [,string dbname [,int port [,string socket]]]]]]',
    'description' => 'Open a connection to a mysql server',
  ),
  'mysqli_connect_errno' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns the numerical value of the error message from last connect command',
  ),
  'mysqli_connect_error' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns the text of the error message from previous MySQL operation',
  ),
  'mysqli_multi_query' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string query',
    'description' => 'Binary-safe version of mysql_query()',
  ),
  'mysqli_set_charset' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string csname',
    'description' => 'sets client character set',
  ),
  'mysqli_get_charset' => 
  array (
    'return' => 'object',
    'params' => 'object link',
    'description' => 'returns a character set object',
  ),
  'mysqli_affected_rows' => 
  array (
    'return' => 'mixed',
    'params' => 'object link',
    'description' => 'Get number of affected rows in previous MySQL operation',
  ),
  'mysqli_autocommit' => 
  array (
    'return' => 'bool',
    'params' => 'object link, bool mode',
    'description' => 'Turn auto commit on or of',
  ),
  'mysqli_stmt_bind_param' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt, string types, mixed variable [,mixed,....]',
    'description' => 'Bind variables to a prepared statement as parameters',
  ),
  'mysqli_stmt_bind_result' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt, mixed var, [,mixed, ...]',
    'description' => 'Bind variables to a prepared statement for result storage',
  ),
  'mysqli_change_user' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string user, string password, string database',
    'description' => 'Change logged-in user of the active connection',
  ),
  'mysqli_character_set_name' => 
  array (
    'return' => 'string',
    'params' => 'object link',
    'description' => 'Returns the name of the character set used for this connection',
  ),
  'mysqli_close' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => 'Close connection',
  ),
  'mysqli_commit' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => 'Commit outstanding actions and close transaction',
  ),
  'mysqli_data_seek' => 
  array (
    'return' => 'bool',
    'params' => 'object result, int offset',
    'description' => 'Move internal result pointer',
  ),
  'mysqli_debug' => 
  array (
    'return' => 'void',
    'params' => 'string debug',
    'description' => '',
  ),
  'mysqli_dump_debug_info' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_errno' => 
  array (
    'return' => 'int',
    'params' => 'object link',
    'description' => 'Returns the numerical value of the error message from previous MySQL operation',
  ),
  'mysqli_error' => 
  array (
    'return' => 'string',
    'params' => 'object link',
    'description' => 'Returns the text of the error message from previous MySQL operation',
  ),
  'mysqli_stmt_execute' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt',
    'description' => 'Execute a prepared statement',
  ),
  'mysqli_stmt_fetch' => 
  array (
    'return' => 'mixed',
    'params' => 'object stmt',
    'description' => 'Fetch results from a prepared statement into the bound variables',
  ),
  'mysqli_field_count' => 
  array (
    'return' => 'int',
    'params' => 'object link',
    'description' => 'Fetch the number of fields returned by the last query for the given link',
  ),
  'mysqli_field_seek' => 
  array (
    'return' => 'int',
    'params' => 'object result, int fieldnr',
    'description' => 'Set result pointer to a specified field offset',
  ),
  'mysqli_field_tell' => 
  array (
    'return' => 'int',
    'params' => 'object result',
    'description' => 'Get current field offset of result pointer',
  ),
  'mysqli_free_result' => 
  array (
    'return' => 'void',
    'params' => 'object result',
    'description' => 'Free query result memory for the given result handle',
  ),
  'mysqli_get_client_info' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Get MySQL client info',
  ),
  'mysqli_get_client_version' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get MySQL client info',
  ),
  'mysqli_get_proto_info' => 
  array (
    'return' => 'int',
    'params' => 'object link',
    'description' => 'Get MySQL protocol information',
  ),
  'mysqli_get_server_info' => 
  array (
    'return' => 'string',
    'params' => 'object link',
    'description' => 'Get MySQL server info',
  ),
  'mysqli_get_server_version' => 
  array (
    'return' => 'int',
    'params' => 'object link',
    'description' => 'Return the MySQL version for the server referenced by the given link',
  ),
  'mysqli_info' => 
  array (
    'return' => 'string',
    'params' => 'object link',
    'description' => 'Get information about the most recent query',
  ),
  'mysqli_init' => 
  array (
    'return' => 'resource',
    'params' => 'void',
    'description' => 'Initialize mysqli and return a resource for use with mysql_real_connect',
  ),
  'mysqli_insert_id' => 
  array (
    'return' => 'mixed',
    'params' => 'object link',
    'description' => 'Get the ID generated from the previous INSERT operation',
  ),
  'mysqli_kill' => 
  array (
    'return' => 'bool',
    'params' => 'object link, int processid',
    'description' => 'Kill a mysql process on the server',
  ),
  'mysqli_set_local_infile_handler' => 
  array (
    'return' => 'bool',
    'params' => 'object link, callback read_func',
    'description' => 'Set callback functions for LOAD DATA LOCAL INFILE',
  ),
  'mysqli_more_results' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => 'check if there any more query results from a multi query',
  ),
  'mysqli_next_result' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => 'read next result from multi_query',
  ),
  'mysqli_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'object result',
    'description' => 'Get number of fields in result',
  ),
  'mysqli_num_rows' => 
  array (
    'return' => 'mixed',
    'params' => 'object result',
    'description' => 'Get number of rows in result',
  ),
  'mysqli_options' => 
  array (
    'return' => 'bool',
    'params' => 'object link, int flags, mixed values',
    'description' => 'Set options',
  ),
  'mysqli_ping' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => 'Ping a server connection or reconnect if there is no connection',
  ),
  'mysqli_prepare' => 
  array (
    'return' => 'mixed',
    'params' => 'object link, string query',
    'description' => 'Prepare a SQL statement for execution',
  ),
  'mysqli_real_connect' => 
  array (
    'return' => 'bool',
    'params' => 'object link [,string hostname [,string username [,string passwd [,string dbname [,int port [,string socket [,int flags]]]]]]]',
    'description' => 'Open a connection to a mysql server',
  ),
  'mysqli_real_query' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string query',
    'description' => 'Binary-safe version of mysql_query()',
  ),
  'mysqli_real_escape_string' => 
  array (
    'return' => 'string',
    'params' => 'object link, string escapestr',
    'description' => 'Escapes special characters in a string for use in a SQL statement, taking into account the current charset of the connection',
  ),
  'mysqli_rollback' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => 'Undo actions from current transaction',
  ),
  'mysqli_send_long_data' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt, int param_nr, string data',
    'description' => '',
  ),
  'mysqli_stmt_affected_rows' => 
  array (
    'return' => 'mixed',
    'params' => 'object stmt',
    'description' => 'Return the number of rows affected in the last query for the given link',
  ),
  'mysqli_stmt_close' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt',
    'description' => 'Close statement',
  ),
  'mysqli_stmt_data_seek' => 
  array (
    'return' => 'void',
    'params' => 'object stmt, int offset',
    'description' => 'Move internal result pointer',
  ),
  'mysqli_stmt_free_result' => 
  array (
    'return' => 'void',
    'params' => 'object stmt',
    'description' => 'Free stored result memory for the given statement handle',
  ),
  'mysqli_stmt_insert_id' => 
  array (
    'return' => 'mixed',
    'params' => 'object stmt',
    'description' => 'Get the ID generated from the previous INSERT operation',
  ),
  'mysqli_stmt_reset' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt',
    'description' => 'reset a prepared statement',
  ),
  'mysqli_stmt_num_rows' => 
  array (
    'return' => 'mixed',
    'params' => 'object stmt',
    'description' => 'Return the number of rows in statements result set',
  ),
  'mysqli_select_db' => 
  array (
    'return' => 'string',
    'params' => 'object link, string dbname',
    'description' => 'Select a MySQL database',
  ),
  'mysqli_sqlstate' => 
  array (
    'return' => 'string',
    'params' => 'object link',
    'description' => 'Returns the SQLSTATE error from previous MySQL operation',
  ),
  'mysqli_ssl_set' => 
  array (
    'return' => 'bool',
    'params' => 'object link ,string key ,string cert ,string ca ,string capath ,string cipher]',
    'description' => '',
  ),
  'mysqli_stat' => 
  array (
    'return' => 'mixed',
    'params' => 'object link',
    'description' => 'Get current system status',
  ),
  'mysqli_stmt_attr_set' => 
  array (
    'return' => 'int',
    'params' => 'object stmt, long attr, bool mode',
    'description' => '',
  ),
  'mysqli_stmt_attr_get' => 
  array (
    'return' => 'int',
    'params' => 'object stmt, long attr',
    'description' => '',
  ),
  'mysqli_stmt_errno' => 
  array (
    'return' => 'int',
    'params' => 'object stmt',
    'description' => '',
  ),
  'mysqli_stmt_error' => 
  array (
    'return' => 'string',
    'params' => 'object stmt',
    'description' => '',
  ),
  'mysqli_stmt_init' => 
  array (
    'return' => 'mixed',
    'params' => 'object link',
    'description' => 'Initialize statement object',
  ),
  'mysqli_stmt_prepare' => 
  array (
    'return' => 'bool',
    'params' => 'object stmt, string query',
    'description' => 'prepare server side statement with query',
  ),
  'mysqli_stmt_result_metadata' => 
  array (
    'return' => 'mixed',
    'params' => 'object stmt',
    'description' => 'return result set from statement',
  ),
  'mysqli_stmt_store_result' => 
  array (
    'return' => 'bool',
    'params' => 'stmt',
    'description' => '',
  ),
  'mysqli_stmt_sqlstate' => 
  array (
    'return' => 'string',
    'params' => 'object stmt',
    'description' => '',
  ),
  'mysqli_store_result' => 
  array (
    'return' => 'object',
    'params' => 'object link',
    'description' => 'Buffer result set on client',
  ),
  'mysqli_thread_id' => 
  array (
    'return' => 'int',
    'params' => 'object link',
    'description' => 'Return the current thread ID',
  ),
  'mysqli_thread_safe' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Return whether thread safety is given or not',
  ),
  'mysqli_use_result' => 
  array (
    'return' => 'mixed',
    'params' => 'object link',
    'description' => 'Directly retrieve query results - do not buffer results on client side',
  ),
  'mysqli_disable_reads_from_master' => 
  array (
    'return' => 'void',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_disable_rpl_parse' => 
  array (
    'return' => 'void',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_enable_reads_from_master' => 
  array (
    'return' => 'void',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_enable_rpl_parse' => 
  array (
    'return' => 'void',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_master_query' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string query',
    'description' => 'Enforce execution of a query on the master in a master/slave setup',
  ),
  'mysqli_rpl_parse_enabled' => 
  array (
    'return' => 'int',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_rpl_probe' => 
  array (
    'return' => 'bool',
    'params' => 'object link',
    'description' => '',
  ),
  'mysqli_rpl_query_type' => 
  array (
    'return' => 'int',
    'params' => 'string query',
    'description' => '',
  ),
  'mysqli_send_query' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string query',
    'description' => '',
  ),
  'mysqli_slave_query' => 
  array (
    'return' => 'bool',
    'params' => 'object link, string query',
    'description' => 'Enforce execution of a query on a slave in a master/slave setup',
  ),
  'imap_open' => 
  array (
    'return' => 'resource',
    'params' => 'string mailbox, string user, string password [, int options]',
    'description' => 'Open an IMAP stream to a mailbox',
  ),
  'imap_reopen' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string mailbox [, int options]',
    'description' => 'Reopen an IMAP stream to a new mailbox',
  ),
  'imap_append' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string folder, string message [, string options]',
    'description' => 'Append a new message to a specified mailbox',
  ),
  'imap_num_msg' => 
  array (
    'return' => 'int',
    'params' => 'resource stream_id',
    'description' => 'Gives the number of messages in the current mailbox',
  ),
  'imap_ping' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id',
    'description' => 'Check if the IMAP stream is still active',
  ),
  'imap_num_recent' => 
  array (
    'return' => 'int',
    'params' => 'resource stream_id',
    'description' => 'Gives the number of recent messages in current mailbox',
  ),
  'imap_get_quota' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string qroot',
    'description' => 'Returns the quota set to the mailbox account qroot',
  ),
  'imap_get_quotaroot' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string mbox',
    'description' => 'Returns the quota set to the mailbox account mbox',
  ),
  'imap_set_quota' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string qroot, int mailbox_size',
    'description' => 'Will set the quota for qroot mailbox',
  ),
  'imap_setacl' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string mailbox, string id, string rights',
    'description' => 'Sets the ACL for a given mailbox',
  ),
  'imap_getacl' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string mailbox',
    'description' => 'Gets the ACL for a given mailbox',
  ),
  'imap_expunge' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id',
    'description' => 'Permanently delete all messages marked for deletion',
  ),
  'imap_close' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id [, int options]',
    'description' => 'Close an IMAP stream',
  ),
  'imap_headers' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id',
    'description' => 'Returns headers for all messages in a mailbox',
  ),
  'imap_body' => 
  array (
    'return' => 'string',
    'params' => 'resource stream_id, int msg_no [, int options]',
    'description' => 'Read the message body',
  ),
  'imap_mail_copy' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, int msg_no, string mailbox [, int options]',
    'description' => 'Copy specified message to a mailbox',
  ),
  'imap_mail_move' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, int msg_no, string mailbox [, int options]',
    'description' => 'Move specified message to a mailbox',
  ),
  'imap_createmailbox' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string mailbox',
    'description' => 'Create a new mailbox',
  ),
  'imap_renamemailbox' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string old_name, string new_name',
    'description' => 'Rename a mailbox',
  ),
  'imap_deletemailbox' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string mailbox',
    'description' => 'Delete a mailbox',
  ),
  'imap_list' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string ref, string pattern',
    'description' => 'Read the list of mailboxes',
  ),
  'imap_getmailboxes' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string ref, string pattern',
    'description' => 'Reads the list of mailboxes and returns a full array of objects containing name, attributes, and delimiter',
  ),
  'imap_scan' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string ref, string pattern, string content',
    'description' => 'Read list of mailboxes containing a certain string',
  ),
  'imap_check' => 
  array (
    'return' => 'object',
    'params' => 'resource stream_id',
    'description' => 'Get mailbox properties',
  ),
  'imap_delete' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, int msg_no [, int options]',
    'description' => 'Mark a message for deletion',
  ),
  'imap_undelete' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, int msg_no',
    'description' => 'Remove the delete flag from a message',
  ),
  'imap_headerinfo' => 
  array (
    'return' => 'object',
    'params' => 'resource stream_id, int msg_no [, int from_length [, int subject_length [, string default_host]]]',
    'description' => 'Read the headers of the message',
  ),
  'imap_rfc822_parse_headers' => 
  array (
    'return' => 'object',
    'params' => 'string headers [, string default_host]',
    'description' => 'Parse a set of mail headers contained in a string, and return an object similar to imap_headerinfo()',
  ),
  'imap_lsub' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string ref, string pattern',
    'description' => 'Return a list of subscribed mailboxes',
  ),
  'imap_getsubscribed' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string ref, string pattern',
    'description' => 'Return a list of subscribed mailboxes, in the same format as imap_getmailboxes()',
  ),
  'imap_subscribe' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string mailbox',
    'description' => 'Subscribe to a mailbox',
  ),
  'imap_unsubscribe' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string mailbox',
    'description' => 'Unsubscribe from a mailbox',
  ),
  'imap_fetchstructure' => 
  array (
    'return' => 'object',
    'params' => 'resource stream_id, int msg_no [, int options]',
    'description' => 'Read the full structure of a message',
  ),
  'imap_fetchbody' => 
  array (
    'return' => 'string',
    'params' => 'resource stream_id, int msg_no, string section [, int options]',
    'description' => 'Get a specific body section',
  ),
  'imap_savebody' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string|resource file, int msg_no[, string section = ""[, int options = 0]]',
    'description' => 'Save a specific body section to a file',
  ),
  'imap_base64' => 
  array (
    'return' => 'string',
    'params' => 'string text',
    'description' => 'Decode BASE64 encoded text',
  ),
  'imap_qprint' => 
  array (
    'return' => 'string',
    'params' => 'string text',
    'description' => 'Convert a quoted-printable string to an 8-bit string',
  ),
  'imap_8bit' => 
  array (
    'return' => 'string',
    'params' => 'string text',
    'description' => 'Convert an 8-bit string to a quoted-printable string',
  ),
  'imap_binary' => 
  array (
    'return' => 'string',
    'params' => 'string text',
    'description' => 'Convert an 8bit string to a base64 string',
  ),
  'imap_mailboxmsginfo' => 
  array (
    'return' => 'object',
    'params' => 'resource stream_id',
    'description' => 'Returns info about the current mailbox',
  ),
  'imap_rfc822_write_address' => 
  array (
    'return' => 'string',
    'params' => 'string mailbox, string host, string personal',
    'description' => 'Returns a properly formatted email address given the mailbox, host, and personal info',
  ),
  'imap_rfc822_parse_adrlist' => 
  array (
    'return' => 'array',
    'params' => 'string address_string, string default_host',
    'description' => 'Parses an address string',
  ),
  'imap_utf8' => 
  array (
    'return' => 'string',
    'params' => 'string mime_encoded_text',
    'description' => 'Convert a mime-encoded text to UTF-8',
  ),
  'imap_utf7_decode' => 
  array (
    'return' => 'string',
    'params' => 'string buf',
    'description' => 'Decode a modified UTF-7 string',
  ),
  'imap_utf7_encode' => 
  array (
    'return' => 'string',
    'params' => 'string buf',
    'description' => 'Encode a string in modified UTF-7',
  ),
  'imap_setflag_full' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string sequence, string flag [, int options]',
    'description' => 'Sets flags on messages',
  ),
  'imap_clearflag_full' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_id, string sequence, string flag [, int options]',
    'description' => 'Clears flags on messages',
  ),
  'imap_sort' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, int criteria, int reverse [, int options [, string search_criteria [, string charset]]]',
    'description' => 'Sort an array of message headers, optionally including only messages that meet specified criteria.',
  ),
  'imap_fetchheader' => 
  array (
    'return' => 'string',
    'params' => 'resource stream_id, int msg_no [, int options]',
    'description' => 'Get the full unfiltered header for a message',
  ),
  'imap_uid' => 
  array (
    'return' => 'int',
    'params' => 'resource stream_id, int msg_no',
    'description' => 'Get the unique message id associated with a standard sequential message number',
  ),
  'imap_msgno' => 
  array (
    'return' => 'int',
    'params' => 'resource stream_id, int unique_msg_id',
    'description' => 'Get the sequence number associated with a UID',
  ),
  'imap_status' => 
  array (
    'return' => 'object',
    'params' => 'resource stream_id, string mailbox, int options',
    'description' => 'Get status info from a mailbox',
  ),
  'imap_bodystruct' => 
  array (
    'return' => 'object',
    'params' => 'resource stream_id, int msg_no, string section',
    'description' => 'Read the structure of a specified body section of a specific message',
  ),
  'imap_fetch_overview' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, int msg_no [, int options]',
    'description' => 'Read an overview of the information in the headers of the given message sequence',
  ),
  'imap_mail_compose' => 
  array (
    'return' => 'string',
    'params' => 'array envelope, array body',
    'description' => 'Create a MIME message based on given envelope and body sections',
  ),
  'imap_mail' => 
  array (
    'return' => 'bool',
    'params' => 'string to, string subject, string message [, string additional_headers [, string cc [, string bcc [, string rpath]]]]',
    'description' => 'Send an email message',
  ),
  'imap_search' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id, string criteria [, int options [, string charset]]',
    'description' => 'Return a list of messages matching the given criteria',
  ),
  'imap_alerts' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns an array of all IMAP alerts that have been generated since the last page load or since the last imap_alerts() call, whichever came last. The alert stack is cleared after imap_alerts() is called.',
  ),
  'imap_errors' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns an array of all IMAP errors generated since the last page load, or since the last imap_errors() call, whichever came last. The error stack is cleared after imap_errors() is called.',
  ),
  'imap_last_error' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns the last error that was generated by an IMAP function. The error stack is NOT cleared after this call.',
  ),
  'imap_mime_header_decode' => 
  array (
    'return' => 'array',
    'params' => 'string str',
    'description' => 'Decode mime header element in accordance with RFC 2047 and return array of objects containing \'charset\' encoding and decoded \'text\'',
  ),
  'imap_thread' => 
  array (
    'return' => 'array',
    'params' => 'resource stream_id [, int options]',
    'description' => 'Return threaded by REFERENCES tree',
  ),
  'imap_timeout' => 
  array (
    'return' => 'mixed',
    'params' => 'int timeout_type [, int timeout]',
    'description' => 'Set or fetch imap timeout',
  ),
  'session_set_cookie_params' => 
  array (
    'return' => 'void',
    'params' => 'int lifetime [, string path [, string domain [, bool secure]]]',
    'description' => 'Set session cookie parameters',
  ),
  'session_get_cookie_params' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Return the session cookie parameters',
  ),
  'session_name' => 
  array (
    'return' => 'string',
    'params' => '[string newname]',
    'description' => 'Return the current session name. If newname is given, the session name is replaced with newname',
  ),
  'session_module_name' => 
  array (
    'return' => 'string',
    'params' => '[string newname]',
    'description' => 'Return the current module name used for accessing session data. If newname is given, the module name is replaced with newname',
  ),
  'session_set_save_handler' => 
  array (
    'return' => 'void',
    'params' => 'string open, string close, string read, string write, string destroy, string gc',
    'description' => 'Sets user-level functions',
  ),
  'session_save_path' => 
  array (
    'return' => 'string',
    'params' => '[string newname]',
    'description' => 'Return the current save path passed to module_name. If newname is given, the save path is replaced with newname',
  ),
  'session_id' => 
  array (
    'return' => 'string',
    'params' => '[string newid]',
    'description' => 'Return the current session id. If newid is given, the session id is replaced with newid',
  ),
  'session_regenerate_id' => 
  array (
    'return' => 'bool',
    'params' => '[bool delete_old_session]',
    'description' => 'Update the current session id with a newly generated one. If delete_old_session is set to true, remove the old session.',
  ),
  'session_cache_limiter' => 
  array (
    'return' => 'string',
    'params' => '[string new_cache_limiter]',
    'description' => 'Return the current cache limiter. If new_cache_limited is given, the current cache_limiter is replaced with new_cache_limiter',
  ),
  'session_cache_expire' => 
  array (
    'return' => 'int',
    'params' => '[int new_cache_expire]',
    'description' => 'Return the current cache expire. If new_cache_expire is given, the current cache_expire is replaced with new_cache_expire',
  ),
  'session_register' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var_names [, mixed ...]',
    'description' => 'Adds varname(s) to the list of variables which are freezed at the session end',
  ),
  'session_unregister' => 
  array (
    'return' => 'bool',
    'params' => 'string varname',
    'description' => 'Removes varname from the list of variables which are freezed at the session end',
  ),
  'session_is_registered' => 
  array (
    'return' => 'bool',
    'params' => 'string varname',
    'description' => 'Checks if a variable is registered in session',
  ),
  'session_encode' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Serializes the current setup and returns the serialized representation',
  ),
  'session_decode' => 
  array (
    'return' => 'bool',
    'params' => 'string data',
    'description' => 'Deserializes data and reinitializes the variables',
  ),
  'session_start' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Begin session - reinitializes freezed variables, registers browsers etc',
  ),
  'session_destroy' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Destroy the current session and all data associated with it',
  ),
  'session_unset' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Unset all registered variables',
  ),
  'session_write_close' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Write session data and end session',
  ),
  'mysql_connect' => 
  array (
    'return' => 'resource',
    'params' => '[string hostname[:port][:/path/to/socket] [, string username [, string password [, bool new [, int flags]]]]]',
    'description' => 'Opens a connection to a MySQL Server',
  ),
  'mysql_pconnect' => 
  array (
    'return' => 'resource',
    'params' => '[string hostname[:port][:/path/to/socket] [, string username [, string password [, int flags]]]]',
    'description' => 'Opens a persistent connection to a MySQL Server',
  ),
  'mysql_close' => 
  array (
    'return' => 'bool',
    'params' => '[int link_identifier]',
    'description' => 'Close a MySQL connection',
  ),
  'mysql_select_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, int link_identifier]',
    'description' => 'Selects a MySQL database',
  ),
  'mysql_get_client_info' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns a string that represents the client library version',
  ),
  'mysql_get_host_info' => 
  array (
    'return' => 'string',
    'params' => '[int link_identifier]',
    'description' => 'Returns a string describing the type of connection in use, including the server host name',
  ),
  'mysql_get_proto_info' => 
  array (
    'return' => 'int',
    'params' => '[int link_identifier]',
    'description' => 'Returns the protocol version used by current connection',
  ),
  'mysql_get_server_info' => 
  array (
    'return' => 'string',
    'params' => '[int link_identifier]',
    'description' => 'Returns a string that represents the server version number',
  ),
  'mysql_info' => 
  array (
    'return' => 'string',
    'params' => '[int link_identifier]',
    'description' => 'Returns a string containing information about the most recent query',
  ),
  'mysql_thread_id' => 
  array (
    'return' => 'int',
    'params' => '[int link_identifier]',
    'description' => 'Returns the thread id of current connection',
  ),
  'mysql_stat' => 
  array (
    'return' => 'string',
    'params' => '[int link_identifier]',
    'description' => 'Returns a string containing status information',
  ),
  'mysql_client_encoding' => 
  array (
    'return' => 'string',
    'params' => '[int link_identifier]',
    'description' => 'Returns the default character set for the current connection',
  ),
  'mysql_create_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, int link_identifier]',
    'description' => 'Create a MySQL database',
  ),
  'mysql_drop_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, int link_identifier]',
    'description' => 'Drops (delete) a MySQL database',
  ),
  'mysql_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query [, int link_identifier]',
    'description' => 'Sends an SQL query to MySQL',
  ),
  'mysql_unbuffered_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query [, int link_identifier]',
    'description' => 'Sends an SQL query to MySQL, without fetching and buffering the result rows',
  ),
  'mysql_db_query' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name, string query [, int link_identifier]',
    'description' => 'Sends an SQL query to MySQL',
  ),
  'mysql_list_dbs' => 
  array (
    'return' => 'resource',
    'params' => '[int link_identifier]',
    'description' => 'List databases available on a MySQL server',
  ),
  'mysql_list_tables' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name [, int link_identifier]',
    'description' => 'List tables in a MySQL database',
  ),
  'mysql_list_fields' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name, string table_name [, int link_identifier]',
    'description' => 'List MySQL result fields',
  ),
  'mysql_list_processes' => 
  array (
    'return' => 'resource',
    'params' => '[int link_identifier]',
    'description' => 'Returns a result set describing the current server threads',
  ),
  'mysql_error' => 
  array (
    'return' => 'string',
    'params' => '[int link_identifier]',
    'description' => 'Returns the text of the error message from previous MySQL operation',
  ),
  'mysql_errno' => 
  array (
    'return' => 'int',
    'params' => '[int link_identifier]',
    'description' => 'Returns the number of the error message from previous MySQL operation',
  ),
  'mysql_affected_rows' => 
  array (
    'return' => 'int',
    'params' => '[int link_identifier]',
    'description' => 'Gets number of affected rows in previous MySQL operation',
  ),
  'mysql_escape_string' => 
  array (
    'return' => 'string',
    'params' => 'string to_be_escaped',
    'description' => 'Escape string for mysql query',
  ),
  'mysql_real_escape_string' => 
  array (
    'return' => 'string',
    'params' => 'string to_be_escaped [, int link_identifier]',
    'description' => 'Escape special characters in a string for use in a SQL statement, taking into account the current charset of the connection',
  ),
  'mysql_insert_id' => 
  array (
    'return' => 'int',
    'params' => '[int link_identifier]',
    'description' => 'Gets the ID generated from the previous INSERT operation',
  ),
  'mysql_result' => 
  array (
    'return' => 'mixed',
    'params' => 'resource result, int row [, mixed field]',
    'description' => 'Gets result data',
  ),
  'mysql_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Gets number of rows in a result',
  ),
  'mysql_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Gets number of fields in a result',
  ),
  'mysql_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'resource result',
    'description' => 'Gets a result row as an enumerated array',
  ),
  'mysql_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource result [, string class_name [, NULL|array ctor_params]]',
    'description' => 'Fetch a result row as an object',
  ),
  'mysql_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int result_type]',
    'description' => 'Fetch a result row as an array (associative, numeric or both)',
  ),
  'mysql_fetch_assoc' => 
  array (
    'return' => 'array',
    'params' => 'resource result',
    'description' => 'Fetch a result row as an associative array',
  ),
  'mysql_data_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource result, int row_number',
    'description' => 'Move internal result pointer',
  ),
  'mysql_fetch_lengths' => 
  array (
    'return' => 'array',
    'params' => 'resource result',
    'description' => 'Gets max data size of each column in a result',
  ),
  'mysql_fetch_field' => 
  array (
    'return' => 'object',
    'params' => 'resource result [, int field_offset]',
    'description' => 'Gets column information from a result and return as an object',
  ),
  'mysql_field_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource result, int field_offset',
    'description' => 'Sets result pointer to a specific field offset',
  ),
  'mysql_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_index',
    'description' => 'Gets the name of the specified field in a result',
  ),
  'mysql_field_table' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_offset',
    'description' => 'Gets name of the table the specified field is in',
  ),
  'mysql_field_len' => 
  array (
    'return' => 'int',
    'params' => 'resource result, int field_offset',
    'description' => 'Returns the length of the specified field',
  ),
  'mysql_field_type' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_offset',
    'description' => 'Gets the type of the specified field in a result',
  ),
  'mysql_field_flags' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_offset',
    'description' => 'Gets the flags associated with the specified field in a result',
  ),
  'mysql_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Free result memory',
  ),
  'mysql_ping' => 
  array (
    'return' => 'bool',
    'params' => '[int link_identifier]',
    'description' => 'Ping a server connection. If no connection then reconnect.',
  ),
  'dom_domerrorhandler_handle_error' => 
  array (
    'return' => 'dom_boolean',
    'params' => 'domerror error',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#ID-ERRORS-DOMErrorHandler-handleErrorSince:',
  ),
  'dom_document_create_element' => 
  array (
    'return' => 'DOMElement',
    'params' => 'string tagName [, string value]',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-2141741547Since:',
  ),
  'dom_document_create_document_fragment' => 
  array (
    'return' => 'DOMDocumentFragment',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-35CB04B5Since:',
  ),
  'dom_document_create_text_node' => 
  array (
    'return' => 'DOMText',
    'params' => 'string data',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1975348127Since:',
  ),
  'dom_document_create_comment' => 
  array (
    'return' => 'DOMComment',
    'params' => 'string data',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1334481328Since:',
  ),
  'dom_document_create_cdatasection' => 
  array (
    'return' => 'DOMCdataSection',
    'params' => 'string data',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-D26C0AF8Since:',
  ),
  'dom_document_create_processing_instruction' => 
  array (
    'return' => 'DOMProcessingInstruction',
    'params' => 'string target, string data',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-135944439Since:',
  ),
  'dom_document_create_attribute' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1084891198Since:',
  ),
  'dom_document_create_entity_reference' => 
  array (
    'return' => 'DOMEntityReference',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-392B75AESince:',
  ),
  'dom_document_get_elements_by_tag_name' => 
  array (
    'return' => 'DOMNodeList',
    'params' => 'string tagname',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-A6C9094Since:',
  ),
  'dom_document_import_node' => 
  array (
    'return' => 'DOMNode',
    'params' => 'DOMNode importedNode, boolean deep',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Core-Document-importNodeSince: DOM Level 2',
  ),
  'dom_document_create_element_ns' => 
  array (
    'return' => 'DOMElement',
    'params' => 'string namespaceURI, string qualifiedName [,string value]',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-DocCrElNSSince: DOM Level 2',
  ),
  'dom_document_create_attribute_ns' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'string namespaceURI, string qualifiedName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-DocCrAttrNSSince: DOM Level 2',
  ),
  'dom_document_get_elements_by_tag_name_ns' => 
  array (
    'return' => 'DOMNodeList',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-getElBTNNSSince: DOM Level 2',
  ),
  'dom_document_get_element_by_id' => 
  array (
    'return' => 'DOMElement',
    'params' => 'string elementId',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-getElBIdSince: DOM Level 2',
  ),
  'dom_document_adopt_node' => 
  array (
    'return' => 'DOMNode',
    'params' => 'DOMNode source',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-Document3-adoptNodeSince: DOM Level 3',
  ),
  'dom_document_normalize_document' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-Document3-normalizeDocumentSince: DOM Level 3',
  ),
  'dom_document_rename_node' => 
  array (
    'return' => 'DOMNode',
    'params' => 'node n, string namespaceURI, string qualifiedName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-Document3-renameNodeSince: DOM Level 3',
  ),
  'dom_document_load' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string source [, int options]',
    'description' => 'URL: http://www.w3.org/TR/DOM-Level-3-LS/load-save.html#LS-DocumentLS-loadSince: DOM Level 3',
  ),
  'dom_document_loadxml' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string source [, int options]',
    'description' => 'URL: http://www.w3.org/TR/DOM-Level-3-LS/load-save.html#LS-DocumentLS-loadXMLSince: DOM Level 3',
  ),
  'dom_document_save' => 
  array (
    'return' => 'int',
    'params' => 'string file',
    'description' => 'Convenience method to save to file',
  ),
  'dom_document_savexml' => 
  array (
    'return' => 'string',
    'params' => '[node n]',
    'description' => 'URL: http://www.w3.org/TR/DOM-Level-3-LS/load-save.html#LS-DocumentLS-saveXMLSince: DOM Level 3',
  ),
  'dom_document_xinclude' => 
  array (
    'return' => 'int',
    'params' => '[int options]',
    'description' => 'Substitutues xincludes in a DomDocument',
  ),
  'dom_document_validate' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Since: DOM extended',
  ),
  'dom_document_load_html_file' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string source',
    'description' => 'Since: DOM extended',
  ),
  'dom_document_load_html' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string source',
    'description' => 'Since: DOM extended',
  ),
  'dom_document_save_html_file' => 
  array (
    'return' => 'int',
    'params' => 'string file',
    'description' => 'Convenience method to save to file as html',
  ),
  'dom_document_save_html' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Convenience method to output as html',
  ),
  'dom_domstringlist_item' => 
  array (
    'return' => 'domstring',
    'params' => 'int index',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#DOMStringList-itemSince:',
  ),
  'dom_string_extend_find_offset16' => 
  array (
    'return' => 'int',
    'params' => 'int offset32',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#i18n-methods-StringExtend-findOffset16Since:',
  ),
  'dom_string_extend_find_offset32' => 
  array (
    'return' => 'int',
    'params' => 'int offset16',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#i18n-methods-StringExtend-findOffset32Since:',
  ),
  'dom_import_simplexml' => 
  array (
    'return' => 'somNode',
    'params' => 'sxeobject node',
    'description' => 'Get a simplexml_element object from dom to allow for processing',
  ),
  'dom_domimplementation_has_feature' => 
  array (
    'return' => 'boolean',
    'params' => 'string feature, string version',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#ID-5CED94D7Since:',
  ),
  'dom_domimplementation_create_document_type' => 
  array (
    'return' => 'DOMDocumentType',
    'params' => 'string qualifiedName, string publicId, string systemId',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Level-2-Core-DOM-createDocTypeSince: DOM Level 2',
  ),
  'dom_domimplementation_create_document' => 
  array (
    'return' => 'DOMDocument',
    'params' => 'string namespaceURI, string qualifiedName, DOMDocumentType doctype',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Level-2-Core-DOM-createDocumentSince: DOM Level 2',
  ),
  'dom_domimplementation_get_feature' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string feature, string version',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#DOMImplementation3-getFeatureSince: DOM Level 3',
  ),
  'dom_namelist_get_name' => 
  array (
    'return' => 'string',
    'params' => 'int index',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#NameList-getNameSince:',
  ),
  'dom_namelist_get_namespace_uri' => 
  array (
    'return' => 'string',
    'params' => 'int index',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#NameList-getNamespaceURISince:',
  ),
  'dom_text_split_text' => 
  array (
    'return' => 'DOMText',
    'params' => 'int offset',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-38853C1DSince:',
  ),
  'dom_text_is_whitespace_in_element_content' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-Text3-isWhitespaceInElementContentSince: DOM Level 3',
  ),
  'dom_text_replace_whole_text' => 
  array (
    'return' => 'DOMText',
    'params' => 'string content',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-Text3-replaceWholeTextSince: DOM Level 3',
  ),
  'dom_element_get_attribute' => 
  array (
    'return' => 'string',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-666EE0F9Since:',
  ),
  'dom_element_set_attribute' => 
  array (
    'return' => 'void',
    'params' => 'string name, string value',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-F68F082Since:',
  ),
  'dom_element_remove_attribute' => 
  array (
    'return' => 'void',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-6D6AC0F9Since:',
  ),
  'dom_element_get_attribute_node' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-217A91B8Since:',
  ),
  'dom_element_set_attribute_node' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'DOMAttr newAttr',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-887236154Since:',
  ),
  'dom_element_remove_attribute_node' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'DOMAttr oldAttr',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-D589198Since:',
  ),
  'dom_element_get_elements_by_tag_name' => 
  array (
    'return' => 'DOMNodeList',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1938918DSince:',
  ),
  'dom_element_get_attribute_ns' => 
  array (
    'return' => 'string',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElGetAttrNSSince: DOM Level 2',
  ),
  'dom_element_set_attribute_ns' => 
  array (
    'return' => 'void',
    'params' => 'string namespaceURI, string qualifiedName, string value',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElSetAttrNSSince: DOM Level 2',
  ),
  'dom_element_remove_attribute_ns' => 
  array (
    'return' => 'void',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElRemAtNSSince: DOM Level 2',
  ),
  'dom_element_get_attribute_node_ns' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElGetAtNodeNSSince: DOM Level 2',
  ),
  'dom_element_set_attribute_node_ns' => 
  array (
    'return' => 'DOMAttr',
    'params' => 'DOMAttr newAttr',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElSetAtNodeNSSince: DOM Level 2',
  ),
  'dom_element_get_elements_by_tag_name_ns' => 
  array (
    'return' => 'DOMNodeList',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-A6C90942Since: DOM Level 2',
  ),
  'dom_element_has_attribute' => 
  array (
    'return' => 'boolean',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElHasAttrSince: DOM Level 2',
  ),
  'dom_element_has_attribute_ns' => 
  array (
    'return' => 'boolean',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElHasAttrNSSince: DOM Level 2',
  ),
  'dom_element_set_id_attribute' => 
  array (
    'return' => 'void',
    'params' => 'string name, boolean isId',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElSetIdAttrSince: DOM Level 3',
  ),
  'dom_element_set_id_attribute_ns' => 
  array (
    'return' => 'void',
    'params' => 'string namespaceURI, string localName, boolean isId',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElSetIdAttrNSSince: DOM Level 3',
  ),
  'dom_element_set_id_attribute_node' => 
  array (
    'return' => 'void',
    'params' => 'attr idAttr, boolean isId',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-ElSetIdAttrNodeSince: DOM Level 3',
  ),
  'dom_userdatahandler_handle' => 
  array (
    'return' => 'dom_void',
    'params' => 'short operation, string key, domobject data, node src, node dst',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#ID-handleUserDataEventSince:',
  ),
  'dom_characterdata_substring_data' => 
  array (
    'return' => 'string',
    'params' => 'int offset, int count',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-6531BCCFSince:',
  ),
  'dom_characterdata_append_data' => 
  array (
    'return' => 'void',
    'params' => 'string arg',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-32791A2FSince:',
  ),
  'dom_characterdata_insert_data' => 
  array (
    'return' => 'void',
    'params' => 'int offset, string arg',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-3EDB695FSince:',
  ),
  'dom_characterdata_delete_data' => 
  array (
    'return' => 'void',
    'params' => 'int offset, int count',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-7C603781Since:',
  ),
  'dom_characterdata_replace_data' => 
  array (
    'return' => 'void',
    'params' => 'int offset, int count, string arg',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-E5CBA7FBSince:',
  ),
  'dom_domimplementationsource_get_domimplementation' => 
  array (
    'return' => 'domdomimplementation',
    'params' => 'string features',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#ID-getDOMImplSince:',
  ),
  'dom_domimplementationsource_get_domimplementations' => 
  array (
    'return' => 'domimplementationlist',
    'params' => 'string features',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#ID-getDOMImplsSince:',
  ),
  'dom_node_insert_before' => 
  array (
    'return' => 'domnode',
    'params' => 'DomNode newChild, DomNode refChild',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-952280727Since:',
  ),
  'dom_node_replace_child' => 
  array (
    'return' => 'DomNode',
    'params' => 'DomNode newChild, DomNode oldChild',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-785887307Since:',
  ),
  'dom_node_remove_child' => 
  array (
    'return' => 'DomNode',
    'params' => 'DomNode oldChild',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1734834066Since:',
  ),
  'dom_node_append_child' => 
  array (
    'return' => 'DomNode',
    'params' => 'DomNode newChild',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-184E7107Since:',
  ),
  'dom_node_has_child_nodes' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-810594187Since:',
  ),
  'dom_node_clone_node' => 
  array (
    'return' => 'DomNode',
    'params' => 'boolean deep',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-3A0ED0A4Since:',
  ),
  'dom_node_normalize' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-normalizeSince:',
  ),
  'dom_node_is_supported' => 
  array (
    'return' => 'boolean',
    'params' => 'string feature, string version',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-Level-2-Core-Node-supportsSince: DOM Level 2',
  ),
  'dom_node_has_attributes' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-NodeHasAttrsSince: DOM Level 2',
  ),
  'dom_node_compare_document_position' => 
  array (
    'return' => 'short',
    'params' => 'DomNode other',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-compareDocumentPositionSince: DOM Level 3',
  ),
  'dom_node_is_same_node' => 
  array (
    'return' => 'boolean',
    'params' => 'DomNode other',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-isSameNodeSince: DOM Level 3',
  ),
  'dom_node_lookup_prefix' => 
  array (
    'return' => 'string',
    'params' => 'string namespaceURI',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-lookupNamespacePrefixSince: DOM Level 3',
  ),
  'dom_node_is_default_namespace' => 
  array (
    'return' => 'boolean',
    'params' => 'string namespaceURI',
    'description' => 'URL: http://www.w3.org/TR/DOM-Level-3-Core/core.html#Node3-isDefaultNamespaceSince: DOM Level 3',
  ),
  'dom_node_lookup_namespace_uri' => 
  array (
    'return' => 'string',
    'params' => 'string prefix',
    'description' => 'URL: http://www.w3.org/TR/DOM-Level-3-Core/core.html#Node3-lookupNamespaceURISince: DOM Level 3',
  ),
  'dom_node_is_equal_node' => 
  array (
    'return' => 'boolean',
    'params' => 'DomNode arg',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-isEqualNodeSince: DOM Level 3',
  ),
  'dom_node_get_feature' => 
  array (
    'return' => 'DomNode',
    'params' => 'string feature, string version',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-getFeatureSince: DOM Level 3',
  ),
  'dom_node_set_user_data' => 
  array (
    'return' => 'DomUserData',
    'params' => 'string key, DomUserData data, userdatahandler handler',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-setUserDataSince: DOM Level 3',
  ),
  'dom_node_get_user_data' => 
  array (
    'return' => 'DomUserData',
    'params' => 'string key',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Node3-getUserDataSince: DOM Level 3',
  ),
  'dom_domconfiguration_set_parameter' => 
  array (
    'return' => 'dom_void',
    'params' => 'string name, domuserdata value',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#DOMConfiguration-propertySince:',
  ),
  'dom_domconfiguration_get_parameter' => 
  array (
    'return' => 'domdomuserdata',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#DOMConfiguration-getParameterSince:',
  ),
  'dom_domconfiguration_can_set_parameter' => 
  array (
    'return' => 'boolean',
    'params' => 'string name, domuserdata value',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#DOMConfiguration-canSetParameterSince:',
  ),
  'dom_namednodemap_get_named_item' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1074577549Since:',
  ),
  'dom_namednodemap_set_named_item' => 
  array (
    'return' => 'DOMNode',
    'params' => 'DOMNode arg',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-1025163788Since:',
  ),
  'dom_namednodemap_remove_named_item' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string name',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-D58B193Since:',
  ),
  'dom_namednodemap_item' => 
  array (
    'return' => 'DOMNode',
    'params' => 'int index',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-349467F9Since:',
  ),
  'dom_namednodemap_get_named_item_ns' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-getNamedItemNSSince: DOM Level 2',
  ),
  'dom_namednodemap_set_named_item_ns' => 
  array (
    'return' => 'DOMNode',
    'params' => 'DOMNode arg',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-setNamedItemNSSince: DOM Level 2',
  ),
  'dom_namednodemap_remove_named_item_ns' => 
  array (
    'return' => 'DOMNode',
    'params' => 'string namespaceURI, string localName',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#core-ID-removeNamedItemNSSince: DOM Level 2',
  ),
  'dom_attr_is_id' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Attr-isIdSince: DOM Level 3',
  ),
  'dom_domimplementationlist_item' => 
  array (
    'return' => 'domdomimplementation',
    'params' => 'int index',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#DOMImplementationList-itemSince:',
  ),
  'dom_nodelist_item' => 
  array (
    'return' => 'DOMNode',
    'params' => 'int index',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#ID-844377136Since:',
  ),
  'PDO::pgsqlLOBCreate' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Creates a new large object, returning its identifier.  Must be called inside a transaction.',
  ),
  'PDO::pgsqlLOBOpen' => 
  array (
    'return' => 'resource',
    'params' => 'string oid [, string mode = \'rb\']',
    'description' => 'Opens an existing large object stream.  Must be called inside a transaction.',
  ),
  'PDO::pgsqlLOBUnlink' => 
  array (
    'return' => 'bool',
    'params' => 'string oid',
    'description' => 'Deletes the large object identified by oid.  Must be called inside a transaction.',
  ),
  'xmlrpc_encode_request' => 
  array (
    'return' => 'string',
    'params' => 'string method, mixed params',
    'description' => 'Generates XML for a method request',
  ),
  'xmlrpc_encode' => 
  array (
    'return' => 'string',
    'params' => 'mixed value',
    'description' => 'Generates XML for a PHP value',
  ),
  'xmlrpc_decode_request' => 
  array (
    'return' => 'array',
    'params' => 'string xml, string& method [, string encoding]',
    'description' => 'Decodes XML into native PHP types',
  ),
  'xmlrpc_decode' => 
  array (
    'return' => 'array',
    'params' => 'string xml [, string encoding]',
    'description' => 'Decodes XML into native PHP types',
  ),
  'xmlrpc_server_create' => 
  array (
    'return' => 'resource',
    'params' => 'void',
    'description' => 'Creates an xmlrpc server',
  ),
  'xmlrpc_server_destroy' => 
  array (
    'return' => 'int',
    'params' => 'resource server',
    'description' => 'Destroys server resources',
  ),
  'xmlrpc_server_register_method' => 
  array (
    'return' => 'bool',
    'params' => 'resource server, string method_name, string function',
    'description' => 'Register a PHP function to handle method matching method_name',
  ),
  'xmlrpc_server_register_introspection_callback' => 
  array (
    'return' => 'bool',
    'params' => 'resource server, string function',
    'description' => 'Register a PHP function to generate documentation',
  ),
  'xmlrpc_server_call_method' => 
  array (
    'return' => 'mixed',
    'params' => 'resource server, string xml, mixed user_data [, array output_options]',
    'description' => 'Parses XML requests and call methods',
  ),
  'xmlrpc_server_add_introspection_data' => 
  array (
    'return' => 'int',
    'params' => 'resource server, array desc',
    'description' => 'Adds introspection documentation',
  ),
  'xmlrpc_parse_method_descriptions' => 
  array (
    'return' => 'array',
    'params' => 'string xml',
    'description' => 'Decodes XML into a list of method descriptions',
  ),
  'xmlrpc_set_type' => 
  array (
    'return' => 'bool',
    'params' => 'string value, string type',
    'description' => 'Sets xmlrpc type, base64 or datetime, for a PHP string value',
  ),
  'xmlrpc_get_type' => 
  array (
    'return' => 'string',
    'params' => 'mixed value',
    'description' => 'Gets xmlrpc type for a PHP value. Especially useful for base64 and datetime strings',
  ),
  'xmlrpc_is_fault' => 
  array (
    'return' => 'bool',
    'params' => 'array',
    'description' => 'Determines if an array value represents an XMLRPC fault.',
  ),
  'textdomain' => 
  array (
    'return' => 'string',
    'params' => 'string domain',
    'description' => 'Set the textdomain to "domain". Returns the current domain',
  ),
  'gettext' => 
  array (
    'return' => 'string',
    'params' => 'string msgid',
    'description' => 'Return the translation of msgid for the current domain, or msgid unaltered if a translation does not exist',
  ),
  'dgettext' => 
  array (
    'return' => 'string',
    'params' => 'string domain_name, string msgid',
    'description' => 'Return the translation of msgid for domain_name, or msgid unaltered if a translation does not exist',
  ),
  'dcgettext' => 
  array (
    'return' => 'string',
    'params' => 'string domain_name, string msgid, long category',
    'description' => 'Return the translation of msgid for domain_name and category, or msgid unaltered if a translation does not exist',
  ),
  'bindtextdomain' => 
  array (
    'return' => 'string',
    'params' => 'string domain_name, string dir',
    'description' => 'Bind to the text domain domain_name, looking for translations in dir. Returns the current domain',
  ),
  'ngettext' => 
  array (
    'return' => 'string',
    'params' => 'string MSGID1, string MSGID2, int N',
    'description' => 'Plural version of gettext()',
  ),
  'msg_set_queue' => 
  array (
    'return' => 'bool',
    'params' => 'resource queue, array data',
    'description' => 'Set information for a message queue',
  ),
  'msg_stat_queue' => 
  array (
    'return' => 'array',
    'params' => 'resource queue',
    'description' => 'Returns information about a message queue',
  ),
  'msg_get_queue' => 
  array (
    'return' => 'resource',
    'params' => 'int key [, int perms]',
    'description' => 'Attach to a message queue',
  ),
  'msg_remove_queue' => 
  array (
    'return' => 'bool',
    'params' => 'resource queue',
    'description' => 'Destroy the queue',
  ),
  'msg_receive' => 
  array (
    'return' => 'mixed',
    'params' => 'resource queue, int desiredmsgtype, int &msgtype, int maxsize, mixed message [, bool unserialize=true [, int flags=0 [, int errorcode]]]',
    'description' => 'Send a message of type msgtype (must be > 0) to a message queue',
  ),
  'msg_send' => 
  array (
    'return' => 'bool',
    'params' => 'resource queue, int msgtype, mixed message [, bool serialize=true [, bool blocking=true [, int errorcode]]]',
    'description' => 'Send a message of type msgtype (must be > 0) to a message queue',
  ),
  'xml_parser_create' => 
  array (
    'return' => 'resource',
    'params' => '[string encoding]',
    'description' => 'Create an XML parser',
  ),
  'xml_parser_create_ns' => 
  array (
    'return' => 'resource',
    'params' => '[string encoding [, string sep]]',
    'description' => 'Create an XML parser',
  ),
  'xml_set_object' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, object &obj',
    'description' => 'Set up object which should be used for callbacks',
  ),
  'xml_set_element_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string shdl, string ehdl',
    'description' => 'Set up start and end element handlers',
  ),
  'xml_set_character_data_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up character data handler',
  ),
  'xml_set_processing_instruction_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up processing instruction (PI) handler',
  ),
  'xml_set_default_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up default handler',
  ),
  'xml_set_unparsed_entity_decl_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up unparsed entity declaration handler',
  ),
  'xml_set_notation_decl_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up notation declaration handler',
  ),
  'xml_set_external_entity_ref_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up external entity reference handler',
  ),
  'xml_set_start_namespace_decl_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up character data handler',
  ),
  'xml_set_end_namespace_decl_handler' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string hdl',
    'description' => 'Set up character data handler',
  ),
  'xml_parse' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string data [, int isFinal]',
    'description' => 'Start parsing an XML document',
  ),
  'xml_parse_into_struct' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, string data, array &struct, array &index',
    'description' => 'Parsing a XML document',
  ),
  'xml_get_error_code' => 
  array (
    'return' => 'int',
    'params' => 'resource parser',
    'description' => 'Get XML parser error code',
  ),
  'xml_error_string' => 
  array (
    'return' => 'string',
    'params' => 'int code',
    'description' => 'Get XML parser error string',
  ),
  'xml_get_current_line_number' => 
  array (
    'return' => 'int',
    'params' => 'resource parser',
    'description' => 'Get current line number for an XML parser',
  ),
  'xml_get_current_column_number' => 
  array (
    'return' => 'int',
    'params' => 'resource parser',
    'description' => 'Get current column number for an XML parser',
  ),
  'xml_get_current_byte_index' => 
  array (
    'return' => 'int',
    'params' => 'resource parser',
    'description' => 'Get current byte index for an XML parser',
  ),
  'xml_parser_free' => 
  array (
    'return' => 'int',
    'params' => 'resource parser',
    'description' => 'Free an XML parser',
  ),
  'xml_parser_set_option' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, int option, mixed value',
    'description' => 'Set options in an XML parser',
  ),
  'xml_parser_get_option' => 
  array (
    'return' => 'int',
    'params' => 'resource parser, int option',
    'description' => 'Get options from an XML parser',
  ),
  'utf8_encode' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Encodes an ISO-8859-1 string to UTF-8',
  ),
  'utf8_decode' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Converts a UTF-8 encoded string to ISO-8859-1',
  ),
  'shm_attach' => 
  array (
    'return' => 'int',
    'params' => 'int key [, int memsize [, int perm]]',
    'description' => 'Creates or open a shared memory segment',
  ),
  'shm_detach' => 
  array (
    'return' => 'bool',
    'params' => 'int shm_identifier',
    'description' => 'Disconnects from shared memory segment',
  ),
  'shm_remove' => 
  array (
    'return' => 'bool',
    'params' => 'int shm_identifier',
    'description' => 'Removes shared memory from Unix systems',
  ),
  'shm_put_var' => 
  array (
    'return' => 'bool',
    'params' => 'int shm_identifier, int variable_key, mixed variable',
    'description' => 'Inserts or updates a variable in shared memory',
  ),
  'shm_get_var' => 
  array (
    'return' => 'mixed',
    'params' => 'int id, int variable_key',
    'description' => 'Returns a variable from shared memory',
  ),
  'shm_remove_var' => 
  array (
    'return' => 'bool',
    'params' => 'int id, int variable_key',
    'description' => 'Removes variable from shared memory',
  ),
  'sqlite_popen' => 
  array (
    'return' => 'resource',
    'params' => 'string filename [, int mode [, string &error_message]]',
    'description' => 'Opens a persistent handle to a SQLite database. Will create the database if it does not exist.',
  ),
  'sqlite_open' => 
  array (
    'return' => 'resource',
    'params' => 'string filename [, int mode [, string &error_message]]',
    'description' => 'Opens a SQLite database. Will create the database if it does not exist.',
  ),
  'sqlite_factory' => 
  array (
    'return' => 'object',
    'params' => 'string filename [, int mode [, string &error_message]]',
    'description' => 'Opens a SQLite database and creates an object for it. Will create the database if it does not exist.',
  ),
  'sqlite_busy_timeout' => 
  array (
    'return' => 'void',
    'params' => 'resource db, int ms',
    'description' => 'Set busy timeout duration. If ms <= 0, all busy handlers are disabled.',
  ),
  'sqlite_close' => 
  array (
    'return' => 'void',
    'params' => 'resource db',
    'description' => 'Closes an open sqlite database.',
  ),
  'sqlite_unbuffered_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query, resource db [ , int result_type [, string &error_message]]',
    'description' => 'Executes a query that does not prefetch and buffer all data.',
  ),
  'sqlite_fetch_column_types' => 
  array (
    'return' => 'resource',
    'params' => 'string table_name, resource db [, int result_type]',
    'description' => 'Return an array of column types from a particular table.',
  ),
  'sqlite_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query, resource db [, int result_type [, string &error_message]]',
    'description' => 'Executes a query against a given database and returns a result handle.',
  ),
  'sqlite_exec' => 
  array (
    'return' => 'boolean',
    'params' => 'string query, resource db[, string &error_message]',
    'description' => 'Executes a result-less query against a given database',
  ),
  'sqlite_fetch_all' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int result_type [, bool decode_binary]]',
    'description' => 'Fetches all rows from a result set as an array of arrays.',
  ),
  'sqlite_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int result_type [, bool decode_binary]]',
    'description' => 'Fetches the next row from a result set as an array.',
  ),
  'sqlite_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource result [, string class_name [, NULL|array ctor_params [, bool decode_binary]]]',
    'description' => 'Fetches the next row from a result set as an object.',
  ),
  'sqlite_array_query' => 
  array (
    'return' => 'array',
    'params' => 'resource db, string query [ , int result_type [, bool decode_binary]]',
    'description' => 'Executes a query against a given database and returns an array of arrays.',
  ),
  'sqlite_single_query' => 
  array (
    'return' => 'array',
    'params' => 'resource db, string query [, bool first_row_only [, bool decode_binary]]',
    'description' => 'Executes a query and returns either an array for one single column or the value of the first row.',
  ),
  'sqlite_fetch_single' => 
  array (
    'return' => 'string',
    'params' => 'resource result [, bool decode_binary]',
    'description' => 'Fetches the first column of a result set as a string.',
  ),
  'sqlite_current' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int result_type [, bool decode_binary]]',
    'description' => 'Fetches the current row from a result set as an array.',
  ),
  'sqlite_column' => 
  array (
    'return' => 'mixed',
    'params' => 'resource result, mixed index_or_name [, bool decode_binary]',
    'description' => 'Fetches a column from the current row of a result set.',
  ),
  'sqlite_libversion' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Returns the version of the linked SQLite library.',
  ),
  'sqlite_libencoding' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Returns the encoding (iso8859 or UTF-8) of the linked SQLite library.',
  ),
  'sqlite_changes' => 
  array (
    'return' => 'int',
    'params' => 'resource db',
    'description' => 'Returns the number of rows that were changed by the most recent SQL statement.',
  ),
  'sqlite_last_insert_rowid' => 
  array (
    'return' => 'int',
    'params' => 'resource db',
    'description' => 'Returns the rowid of the most recently inserted row.',
  ),
  'sqlite_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Returns the number of rows in a buffered result set.',
  ),
  'sqlite_valid' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Returns whether more rows are available.',
  ),
  'sqlite_has_prev' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => '* Returns whether a previous row is available.',
  ),
  'sqlite_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Returns the number of fields in a result set.',
  ),
  'sqlite_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_index',
    'description' => 'Returns the name of a particular field of a result set.',
  ),
  'sqlite_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource result, int row',
    'description' => 'Seek to a particular row number of a buffered result set.',
  ),
  'sqlite_rewind' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Seek to the first row number of a buffered result set.',
  ),
  'sqlite_next' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Seek to the next row number of a result set.',
  ),
  'sqlite_key' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Return the current row index of a buffered result.',
  ),
  'sqlite_prev' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => '* Seek to the previous row number of a result set.',
  ),
  'sqlite_escape_string' => 
  array (
    'return' => 'string',
    'params' => 'string item',
    'description' => 'Escapes a string for use as a query parameter.',
  ),
  'sqlite_last_error' => 
  array (
    'return' => 'int',
    'params' => 'resource db',
    'description' => 'Returns the error code of the last error for a database.',
  ),
  'sqlite_error_string' => 
  array (
    'return' => 'string',
    'params' => 'int error_code',
    'description' => 'Returns the textual description of an error code.',
  ),
  'sqlite_create_aggregate' => 
  array (
    'return' => 'bool',
    'params' => 'resource db, string funcname, mixed step_func, mixed finalize_func[, long num_args]',
    'description' => 'Registers an aggregate function for queries.',
  ),
  'sqlite_create_function' => 
  array (
    'return' => 'bool',
    'params' => 'resource db, string funcname, mixed callback[, long num_args]',
    'description' => 'Registers a "regular" function for queries.',
  ),
  'sqlite_udf_encode_binary' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Apply binary encoding (if required) to a string to return from an UDF.',
  ),
  'sqlite_udf_decode_binary' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Decode binary encoding on a string parameter passed to an UDF.',
  ),
  'socket_select' => 
  array (
    'return' => 'int',
    'params' => 'array &read_fds, array &write_fds, &array except_fds, int tv_sec[, int tv_usec]',
    'description' => 'Runs the select() system call on the sets mentioned with a timeout specified by tv_sec and tv_usec',
  ),
  'socket_create_listen' => 
  array (
    'return' => 'resource',
    'params' => 'int port[, int backlog]',
    'description' => 'Opens a socket on port to accept connections',
  ),
  'socket_accept' => 
  array (
    'return' => 'resource',
    'params' => 'resource socket',
    'description' => 'Accepts a connection on the listening socket fd',
  ),
  'socket_set_nonblock' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket',
    'description' => 'Sets nonblocking mode on a socket resource',
  ),
  'socket_set_block' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket',
    'description' => 'Sets blocking mode on a socket resource',
  ),
  'socket_listen' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket[, int backlog]',
    'description' => 'Sets the maximum number of connections allowed to be waited for on the socket specified by fd',
  ),
  'socket_close' => 
  array (
    'return' => 'void',
    'params' => 'resource socket',
    'description' => 'Closes a file descriptor',
  ),
  'socket_write' => 
  array (
    'return' => 'int',
    'params' => 'resource socket, string buf[, int length]',
    'description' => 'Writes the buffer to the socket resource, length is optional',
  ),
  'socket_read' => 
  array (
    'return' => 'string',
    'params' => 'resource socket, int length [, int type]',
    'description' => 'Reads a maximum of length bytes from socket',
  ),
  'socket_getsockname' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, string &addr[, int &port]',
    'description' => 'Queries the remote side of the given socket which may either result in host/port or in a UNIX filesystem path, dependent on its type.',
  ),
  'socket_getpeername' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, string &addr[, int &port]',
    'description' => 'Queries the remote side of the given socket which may either result in host/port or in a UNIX filesystem path, dependent on its type.',
  ),
  'socket_create' => 
  array (
    'return' => 'resource',
    'params' => 'int domain, int type, int protocol',
    'description' => 'Creates an endpoint for communication in the domain specified by domain, of type specified by type',
  ),
  'socket_connect' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, string addr [, int port]',
    'description' => 'Opens a connection to addr:port on the socket specified by socket',
  ),
  'socket_strerror' => 
  array (
    'return' => 'string',
    'params' => 'int errno',
    'description' => 'Returns a string describing an error',
  ),
  'socket_bind' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, string addr [, int port]',
    'description' => 'Binds an open socket to a listening port, port is only specified in AF_INET family.',
  ),
  'socket_recv' => 
  array (
    'return' => 'int',
    'params' => 'resource socket, string &buf, int len, int flags',
    'description' => 'Receives data from a connected socket',
  ),
  'socket_send' => 
  array (
    'return' => 'int',
    'params' => 'resource socket, string buf, int len, int flags',
    'description' => 'Sends data to a connected socket',
  ),
  'socket_recvfrom' => 
  array (
    'return' => 'int',
    'params' => 'resource socket, string &buf, int len, int flags, string &name [, int &port]',
    'description' => 'Receives data from a socket, connected or not',
  ),
  'socket_sendto' => 
  array (
    'return' => 'int',
    'params' => 'resource socket, string buf, int len, int flags, string addr [, int port]',
    'description' => 'Sends a message to a socket, whether it is connected or not',
  ),
  'socket_get_option' => 
  array (
    'return' => 'mixed',
    'params' => 'resource socket, int level, int optname',
    'description' => 'Gets socket options for the socket',
  ),
  'socket_set_option' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, int level, int optname, int|array optval',
    'description' => 'Sets socket options for the socket',
  ),
  'socket_create_pair' => 
  array (
    'return' => 'bool',
    'params' => 'int domain, int type, int protocol, array &fd',
    'description' => 'Creates a pair of indistinguishable sockets and stores them in fds.',
  ),
  'socket_shutdown' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket[, int how]',
    'description' => 'Shuts down a socket for receiving, sending, or both.',
  ),
  'socket_last_error' => 
  array (
    'return' => 'int',
    'params' => '[resource socket]',
    'description' => 'Returns the last socket error (either the last used or the provided socket resource)',
  ),
  'socket_clear_error' => 
  array (
    'return' => 'void',
    'params' => '[resource socket]',
    'description' => 'Clears the error on the socket or the last error code.',
  ),
  'sybase_connect' => 
  array (
    'return' => 'int',
    'params' => '[string host [, string user [, string password [, string charset [, string appname]]]]]',
    'description' => 'Open Sybase server connection',
  ),
  'sybase_pconnect' => 
  array (
    'return' => 'int',
    'params' => '[string host [, string user [, string password [, string charset [, string appname]]]]]',
    'description' => 'Open persistent Sybase connection',
  ),
  'sybase_close' => 
  array (
    'return' => 'bool',
    'params' => '[int link_id]',
    'description' => 'Close Sybase connection',
  ),
  'sybase_select_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database [, int link_id]',
    'description' => 'Select Sybase database',
  ),
  'sybase_query' => 
  array (
    'return' => 'int',
    'params' => 'string query [, int link_id]',
    'description' => 'Send Sybase query',
  ),
  'sybase_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'int result',
    'description' => 'Free result memory',
  ),
  'sybase_get_last_message' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns the last message from server (over min_message_severity)',
  ),
  'sybase_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'int result',
    'description' => 'Get number of rows in result',
  ),
  'sybase_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'int result',
    'description' => 'Get number of fields in result',
  ),
  'sybase_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'int result',
    'description' => 'Get row as enumerated array',
  ),
  'sybase_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'int result [, mixed object]',
    'description' => 'Fetch row as object',
  ),
  'sybase_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'int result',
    'description' => 'Fetch row as array',
  ),
  'sybase_data_seek' => 
  array (
    'return' => 'bool',
    'params' => 'int result, int offset',
    'description' => 'Move internal row pointer',
  ),
  'sybase_fetch_field' => 
  array (
    'return' => 'object',
    'params' => 'int result [, int offset]',
    'description' => 'Get field information',
  ),
  'sybase_field_seek' => 
  array (
    'return' => 'bool',
    'params' => 'int result, int offset',
    'description' => 'Set field offset',
  ),
  'sybase_result' => 
  array (
    'return' => 'string',
    'params' => 'int result, int row, mixed field',
    'description' => 'Get result data',
  ),
  'sybase_affected_rows' => 
  array (
    'return' => 'int',
    'params' => '[int link_id]',
    'description' => 'Get number of affected rows in last query',
  ),
  'sybase_min_error_severity' => 
  array (
    'return' => 'void',
    'params' => 'int severity',
    'description' => 'Sets the minimum error severity',
  ),
  'sybase_min_message_severity' => 
  array (
    'return' => 'void',
    'params' => 'int severity',
    'description' => 'Sets the minimum message severity',
  ),
  'confirm_extname_compiled' => 
  array (
    'return' => 'string',
    'params' => 'string arg',
    'description' => 'Return a string to confirm that the module is compiled in',
  ),
  'fdf_open' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Opens a new FDF document',
  ),
  'fdf_open_string' => 
  array (
    'return' => 'resource',
    'params' => 'string fdf_data',
    'description' => 'Opens a new FDF document from string',
  ),
  'fdf_create' => 
  array (
    'return' => 'resource',
    'params' => 'void',
    'description' => 'Creates a new FDF document',
  ),
  'fdf_close' => 
  array (
    'return' => 'void',
    'params' => 'resource fdfdoc',
    'description' => 'Closes the FDF document',
  ),
  'fdf_get_value' => 
  array (
    'return' => 'string',
    'params' => 'resource fdfdoc, string fieldname [, int which]',
    'description' => 'Gets the value of a field as string',
  ),
  'fdf_set_value' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, mixed value [, int isname]',
    'description' => 'Sets the value of a field',
  ),
  'fdf_next_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource fdfdoc [, string fieldname]',
    'description' => 'Gets the name of the next field name or the first field name',
  ),
  'fdf_set_ap' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int face, string filename, int pagenr',
    'description' => 'Sets the appearence of a field',
  ),
  'fdf_get_ap' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int face, string filename',
    'description' => 'Gets the appearance of a field and creates a PDF document out of it.',
  ),
  'fdf_get_encoding' => 
  array (
    'return' => 'string',
    'params' => 'resource fdf',
    'description' => 'Gets FDF file encoding scheme',
  ),
  'fdf_set_status' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string status',
    'description' => 'Sets the value of /Status key',
  ),
  'fdf_get_status' => 
  array (
    'return' => 'string',
    'params' => 'resource fdfdoc',
    'description' => 'Gets the value of /Status key',
  ),
  'fdf_set_file' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string filename [, string target_frame]',
    'description' => 'Sets the value of /F key',
  ),
  'fdf_get_file' => 
  array (
    'return' => 'string',
    'params' => 'resource fdfdoc',
    'description' => 'Gets the value of /F key',
  ),
  'fdf_save' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc [, string filename]',
    'description' => 'Writes out the FDF file',
  ),
  'fdf_save_string' => 
  array (
    'return' => 'string',
    'params' => 'resource fdfdoc',
    'description' => 'Returns the FDF file as a string',
  ),
  'fdf_add_template' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, int newpage, string filename, string template, int rename',
    'description' => 'Adds a template into the FDF document',
  ),
  'fdf_set_flags' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int whichflags, int newflags',
    'description' => 'Sets flags for a field in the FDF document',
  ),
  'fdf_get_flags' => 
  array (
    'return' => 'int',
    'params' => 'resorce fdfdoc, string fieldname, int whichflags',
    'description' => 'Gets the flags of a field',
  ),
  'fdf_set_opt' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int element, string value, string name',
    'description' => 'Sets a value in the opt array for a field',
  ),
  'fdf_get_opt' => 
  array (
    'return' => 'mixed',
    'params' => 'resource fdfdof, string fieldname [, int element]',
    'description' => 'Gets a value from the opt array of a field',
  ),
  'fdf_set_submit_form_action' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int whichtrigger, string url, int flags',
    'description' => 'Sets the submit form action for a field',
  ),
  'fdf_set_javascript_action' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int whichtrigger, string script',
    'description' => 'Sets the javascript action for a field',
  ),
  'fdf_set_encoding' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdf_document, string encoding',
    'description' => 'Sets FDF encoding (either "Shift-JIS" or "Unicode")',
  ),
  'fdf_errno' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Gets error code for last operation',
  ),
  'fdf_error' => 
  array (
    'return' => 'string',
    'params' => '[int errno]',
    'description' => 'Gets error description for error code',
  ),
  'fdf_get_version' => 
  array (
    'return' => 'string',
    'params' => '[resource fdfdoc]',
    'description' => 'Gets version number for FDF api or file',
  ),
  'fdf_set_version' => 
  array (
    'return' => 'bool',
    'params' => 'resourece fdfdoc, string version',
    'description' => 'Sets FDF version for a file',
  ),
  'fdf_add_doc_javascript' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string scriptname, string script',
    'description' => 'Add javascript code to the fdf file',
  ),
  'fdf_set_on_import_javascript' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string script [, bool before_data_import]',
    'description' => 'Adds javascript code to be executed when Acrobat opens the FDF',
  ),
  'fdf_set_target_frame' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string target',
    'description' => 'Sets target frame for form',
  ),
  'fdf_remove_item' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, string fieldname, int item',
    'description' => 'Sets target frame for form',
  ),
  'fdf_get_attachment' => 
  array (
    'return' => 'array',
    'params' => 'resource fdfdoc, string fieldname, string savepath',
    'description' => 'Get attached uploaded file',
  ),
  'fdf_enum_values' => 
  array (
    'return' => 'bool',
    'params' => 'resource fdfdoc, callback function [, mixed userdata]',
    'description' => 'Call a user defined function for each document value',
  ),
  'fdf_header' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Set FDF specific HTTP headers',
  ),
  'variant_set' => 
  array (
    'return' => 'void',
    'params' => 'object variant, mixed value',
    'description' => 'Assigns a new value for a variant object',
  ),
  'variant_add' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => '"Adds" two variant values together and returns the result',
  ),
  'variant_cat' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'concatenates two variant values together and returns the result',
  ),
  'variant_sub' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'subtracts the value of the right variant from the left variant value and returns the result',
  ),
  'variant_mul' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'multiplies the values of the two variants and returns the result',
  ),
  'variant_and' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'performs a bitwise AND operation between two variants and returns the result',
  ),
  'variant_div' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Returns the result from dividing two variants',
  ),
  'variant_eqv' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Performs a bitwise equivalence on two variants',
  ),
  'variant_idiv' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Converts variants to integers and then returns the result from dividing them',
  ),
  'variant_imp' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Performs a bitwise implication on two variants',
  ),
  'variant_mod' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Divides two variants and returns only the remainder',
  ),
  'variant_or' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Performs a logical disjunction on two variants',
  ),
  'variant_pow' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Returns the result of performing the power function with two variants',
  ),
  'variant_xor' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, mixed right',
    'description' => 'Performs a logical exclusion on two variants',
  ),
  'variant_abs' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left',
    'description' => 'Returns the absolute value of a variant',
  ),
  'variant_fix' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left',
    'description' => 'Returns the integer part ? of a variant',
  ),
  'variant_int' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left',
    'description' => 'Returns the integer portion of a variant',
  ),
  'variant_neg' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left',
    'description' => 'Performs logical negation on a variant',
  ),
  'variant_not' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left',
    'description' => 'Performs bitwise not negation on a variant',
  ),
  'variant_round' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed left, int decimals',
    'description' => 'Rounds a variant to the specified number of decimal places',
  ),
  'variant_cmp' => 
  array (
    'return' => 'int',
    'params' => 'mixed left, mixed right [, int lcid [, int flags]]',
    'description' => 'Compares two variants',
  ),
  'variant_date_to_timestamp' => 
  array (
    'return' => 'int',
    'params' => 'object variant',
    'description' => 'Converts a variant date/time value to unix timestamp',
  ),
  'variant_date_from_timestamp' => 
  array (
    'return' => 'object',
    'params' => 'int timestamp',
    'description' => 'Returns a variant date representation of a unix timestamp',
  ),
  'variant_get_type' => 
  array (
    'return' => 'int',
    'params' => 'object variant',
    'description' => 'Returns the VT_XXX type code for a variant',
  ),
  'variant_set_type' => 
  array (
    'return' => 'void',
    'params' => 'object variant, int type',
    'description' => 'Convert a variant into another type.  Variant is modified "in-place"',
  ),
  'variant_cast' => 
  array (
    'return' => 'object',
    'params' => 'object variant, int type',
    'description' => 'Convert a variant into a new variant object of another type',
  ),
  'com_get_active_object' => 
  array (
    'return' => 'object',
    'params' => 'string progid [, int code_page ]',
    'description' => 'Returns a handle to an already running instance of a COM object',
  ),
  'com_create_guid' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Generate a globally unique identifier (GUID)',
  ),
  'com_event_sink' => 
  array (
    'return' => 'bool',
    'params' => 'object comobject, object sinkobject [, mixed sinkinterface]',
    'description' => 'Connect events from a COM object to a PHP object',
  ),
  'com_print_typeinfo' => 
  array (
    'return' => 'bool',
    'params' => 'object comobject | string typelib, string dispinterface, bool wantsink',
    'description' => 'Print out a PHP class definition for a dispatchable interface',
  ),
  'com_message_pump' => 
  array (
    'return' => 'bool',
    'params' => '[int timeoutms]',
    'description' => 'Process COM messages, sleeping for up to timeoutms milliseconds',
  ),
  'com_load_typelib' => 
  array (
    'return' => 'bool',
    'params' => 'string typelib_name [, int case_insensitive]',
    'description' => 'Loads a Typelibrary and registers its constants',
  ),
  'COMPersistHelper::GetCurFile' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Determines the filename into which an object will be saved, or false if none is set, via IPersistFile::GetCurFile',
  ),
  'COMPersistHelper::SaveToFile' => 
  array (
    'return' => 'bool',
    'params' => 'string filename [, bool remember]',
    'description' => 'Persist object data to file, via IPersistFile::Save',
  ),
  'COMPersistHelper::LoadFromFile' => 
  array (
    'return' => 'bool',
    'params' => 'string filename [, int flags]',
    'description' => 'Load object data from file, via IPersistFile::Load',
  ),
  'COMPersistHelper::GetMaxStreamSize' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Gets maximum stream size required to store the object data, via IPersistStream::GetSizeMax (or IPersistStreamInit::GetSizeMax)',
  ),
  'COMPersistHelper::InitNew' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Initializes the object to a default state, via IPersistStreamInit::InitNew',
  ),
  'COMPersistHelper::LoadFromStream' => 
  array (
    'return' => 'mixed',
    'params' => 'resource stream',
    'description' => 'Initializes an object from the stream where it was previously saved, via IPersistStream::Load or OleLoadFromStream',
  ),
  'COMPersistHelper::SaveToStream' => 
  array (
    'return' => 'int',
    'params' => 'resource stream',
    'description' => 'Saves the object to a stream, via IPersistStream::Save',
  ),
  'COMPersistHelper::__construct' => 
  array (
    'return' => 'int',
    'params' => '[object com_object]',
    'description' => 'Creates a persistence helper object, usually associated with a com_object',
  ),
  'pg_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string connection_string[, int connect_type] | [string host, string port [, string options [, string tty,]]] string database',
    'description' => 'Open a PostgreSQL connection',
  ),
  'pg_pconnect' => 
  array (
    'return' => 'resource',
    'params' => 'string connection_string | [string host, string port [, string options [, string tty,]]] string database',
    'description' => 'Open a persistent PostgreSQL connection',
  ),
  'pg_close' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection]',
    'description' => 'Close a PostgreSQL connection',
  ),
  'pg_dbname' => 
  array (
    'return' => 'string',
    'params' => '[resource connection]',
    'description' => 'Get the database name',
  ),
  'pg_last_error' => 
  array (
    'return' => 'string',
    'params' => '[resource connection]',
    'description' => 'Get the error message string',
  ),
  'pg_options' => 
  array (
    'return' => 'string',
    'params' => '[resource connection]',
    'description' => 'Get the options associated with the connection',
  ),
  'pg_port' => 
  array (
    'return' => 'int',
    'params' => '[resource connection]',
    'description' => 'Return the port number associated with the connection',
  ),
  'pg_tty' => 
  array (
    'return' => 'string',
    'params' => '[resource connection]',
    'description' => 'Return the tty name associated with the connection',
  ),
  'pg_host' => 
  array (
    'return' => 'string',
    'params' => '[resource connection]',
    'description' => 'Returns the host name associated with the connection',
  ),
  'pg_version' => 
  array (
    'return' => 'array',
    'params' => '[resource connection]',
    'description' => 'Returns an array with client, protocol and server version (when available)',
  ),
  'pg_parameter_status' => 
  array (
    'return' => 'string|false',
    'params' => '[resource connection,] string param_name',
    'description' => 'Returns the value of a server parameter',
  ),
  'pg_ping' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection]',
    'description' => 'Ping database. If connection is bad, try to reconnect.',
  ),
  'pg_query' => 
  array (
    'return' => 'resource',
    'params' => '[resource connection,] string query',
    'description' => 'Execute a query',
  ),
  'pg_query_params' => 
  array (
    'return' => 'resource',
    'params' => '[resource connection,] string query, array params',
    'description' => 'Execute a query',
  ),
  'pg_prepare' => 
  array (
    'return' => 'resource',
    'params' => '[resource connection,] string stmtname, string query',
    'description' => 'Prepare a query for future execution',
  ),
  'pg_execute' => 
  array (
    'return' => 'resource',
    'params' => '[resource connection,] string stmtname, array params',
    'description' => 'Execute a prepared query',
  ),
  'pg_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Return the number of rows in the result',
  ),
  'pg_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Return the number of fields in the result',
  ),
  'pg_affected_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource result',
    'description' => 'Returns the number of affected tuples',
  ),
  'pg_last_notice' => 
  array (
    'return' => 'string',
    'params' => 'resource connection',
    'description' => 'Returns the last notice set by the backend',
  ),
  'pg_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_number',
    'description' => 'Returns the name of the field',
  ),
  'pg_field_size' => 
  array (
    'return' => 'int',
    'params' => 'resource result, int field_number',
    'description' => 'Returns the internal size of the field',
  ),
  'pg_field_type' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_number',
    'description' => 'Returns the type name for the given field',
  ),
  'pg_field_type_oid' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int field_number',
    'description' => 'Returns the type oid for the given field',
  ),
  'pg_field_num' => 
  array (
    'return' => 'int',
    'params' => 'resource result, string field_name',
    'description' => 'Returns the field number of the named field',
  ),
  'pg_fetch_result' => 
  array (
    'return' => 'mixed',
    'params' => 'resource result, [int row_number,] mixed field_name',
    'description' => 'Returns values from a result identifier',
  ),
  'pg_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int row [, int result_type]]',
    'description' => 'Get a row as an enumerated array',
  ),
  'pg_fetch_assoc' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int row]',
    'description' => 'Fetch a row as an assoc array',
  ),
  'pg_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int row [, int result_type]]',
    'description' => 'Fetch a row as an array',
  ),
  'pg_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource result [, int row [, string class_name [, NULL|array ctor_params]]]',
    'description' => 'Fetch a row as an object',
  ),
  'pg_fetch_all' => 
  array (
    'return' => 'array',
    'params' => 'resource result',
    'description' => 'Fetch all rows into array',
  ),
  'pg_fetch_all_columns' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int column_number]',
    'description' => 'Fetch all rows into array',
  ),
  'pg_result_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource result, int offset',
    'description' => 'Set internal row offset',
  ),
  'pg_field_prtlen' => 
  array (
    'return' => 'int',
    'params' => 'resource result, [int row,] mixed field_name_or_number',
    'description' => 'Returns the printed length',
  ),
  'pg_field_is_null' => 
  array (
    'return' => 'int',
    'params' => 'resource result, [int row,] mixed field_name_or_number',
    'description' => 'Test if a field is NULL',
  ),
  'pg_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Free result memory',
  ),
  'pg_last_oid' => 
  array (
    'return' => 'string',
    'params' => 'resource result',
    'description' => 'Returns the last object identifier',
  ),
  'pg_trace' => 
  array (
    'return' => 'bool',
    'params' => 'string filename [, string mode [, resource connection]]',
    'description' => 'Enable tracing a PostgreSQL connection',
  ),
  'pg_untrace' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection]',
    'description' => 'Disable tracing of a PostgreSQL connection',
  ),
  'pg_lo_create' => 
  array (
    'return' => 'int',
    'params' => '[resource connection]',
    'description' => 'Create a large object',
  ),
  'pg_lo_unlink' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection,] string large_object_oid',
    'description' => 'Delete a large object',
  ),
  'pg_lo_open' => 
  array (
    'return' => 'resource',
    'params' => '[resource connection,] int large_object_oid, string mode',
    'description' => 'Open a large object and return fd',
  ),
  'pg_lo_close' => 
  array (
    'return' => 'bool',
    'params' => 'resource large_object',
    'description' => 'Close a large object',
  ),
  'pg_lo_read' => 
  array (
    'return' => 'string',
    'params' => 'resource large_object [, int len]',
    'description' => 'Read a large object',
  ),
  'pg_lo_write' => 
  array (
    'return' => 'int',
    'params' => 'resource large_object, string buf [, int len]',
    'description' => 'Write a large object',
  ),
  'pg_lo_read_all' => 
  array (
    'return' => 'int',
    'params' => 'resource large_object',
    'description' => 'Read a large object and send straight to browser',
  ),
  'pg_lo_import' => 
  array (
    'return' => 'int',
    'params' => '[resource connection, ] string filename',
    'description' => 'Import large object direct from filesystem',
  ),
  'pg_lo_export' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection, ] int objoid, string filename',
    'description' => 'Export large object direct to filesystem',
  ),
  'pg_lo_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource large_object, int offset [, int whence]',
    'description' => 'Seeks position of large object',
  ),
  'pg_lo_tell' => 
  array (
    'return' => 'int',
    'params' => 'resource large_object',
    'description' => 'Returns current position of large object',
  ),
  'pg_set_error_verbosity' => 
  array (
    'return' => 'int',
    'params' => '[resource connection,] int verbosity',
    'description' => 'Set error verbosity',
  ),
  'pg_set_client_encoding' => 
  array (
    'return' => 'int',
    'params' => '[resource connection,] string encoding',
    'description' => 'Set client encoding',
  ),
  'pg_client_encoding' => 
  array (
    'return' => 'string',
    'params' => '[resource connection]',
    'description' => 'Get the current client encoding',
  ),
  'pg_end_copy' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection]',
    'description' => 'Sync with backend. Completes the Copy command',
  ),
  'pg_put_line' => 
  array (
    'return' => 'bool',
    'params' => '[resource connection,] string query',
    'description' => 'Send null-terminated string to backend server',
  ),
  'pg_copy_to' => 
  array (
    'return' => 'array',
    'params' => 'resource connection, string table_name [, string delimiter [, string null_as]]',
    'description' => 'Copy table to array',
  ),
  'pg_copy_from' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection, string table_name , array rows [, string delimiter [, string null_as]]',
    'description' => 'Copy table from array',
  ),
  'pg_escape_string' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Escape string for text/char type',
  ),
  'pg_escape_bytea' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Escape binary for bytea type',
  ),
  'pg_unescape_bytea' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Unescape binary for bytea type',
  ),
  'pg_result_error' => 
  array (
    'return' => 'string',
    'params' => 'resource result',
    'description' => 'Get error message associated with result',
  ),
  'pg_result_error_field' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int fieldcode',
    'description' => 'Get error message field associated with result',
  ),
  'pg_connection_status' => 
  array (
    'return' => 'int',
    'params' => 'resource connnection',
    'description' => 'Get connection status',
  ),
  'pg_transaction_status' => 
  array (
    'return' => 'int',
    'params' => 'resource connnection',
    'description' => 'Get transaction status',
  ),
  'pg_connection_reset' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection',
    'description' => 'Reset connection (reconnect)',
  ),
  'pg_cancel_query' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection',
    'description' => 'Cancel request',
  ),
  'pg_connection_busy' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection',
    'description' => 'Get connection is busy or not',
  ),
  'pg_send_query' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection, string query',
    'description' => 'Send asynchronous query',
  ),
  'pg_send_query_params' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection, string query',
    'description' => 'Send asynchronous parameterized query',
  ),
  'pg_send_prepare' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection, string stmtname, string query',
    'description' => 'Asynchronously prepare a query for future execution',
  ),
  'pg_send_execute' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection, string stmtname, array params',
    'description' => 'Executes prevriously prepared stmtname asynchronously',
  ),
  'pg_get_result' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection',
    'description' => 'Get asynchronous query result',
  ),
  'pg_result_status' => 
  array (
    'return' => 'mixed',
    'params' => 'resource result[, long result_type]',
    'description' => 'Get status of query result',
  ),
  'pg_get_notify' => 
  array (
    'return' => 'array',
    'params' => '[resource connection[, result_type]]',
    'description' => 'Get asynchronous notification',
  ),
  'pg_get_pid' => 
  array (
    'return' => 'int',
    'params' => '[resource connection',
    'description' => 'Get backend(server) pid',
  ),
  'pg_meta_data' => 
  array (
    'return' => 'array',
    'params' => 'resource db, string table',
    'description' => 'Get meta_data',
  ),
  'pg_convert' => 
  array (
    'return' => 'array',
    'params' => 'resource db, string table, array values[, int options]',
    'description' => 'Check and convert values for PostgreSQL SQL statement',
  ),
  'pg_insert' => 
  array (
    'return' => 'mixed',
    'params' => 'resource db, string table, array values[, int options]',
    'description' => 'Insert values (filed=>value) to table',
  ),
  'pg_update' => 
  array (
    'return' => 'mixed',
    'params' => 'resource db, string table, array fields, array ids[, int options]',
    'description' => 'Update table using values (field=>value) and ids (id=>value)',
  ),
  'pg_delete' => 
  array (
    'return' => 'mixed',
    'params' => 'resource db, string table, array ids[, int options]',
    'description' => 'Delete records has ids (id=>value)',
  ),
  'pg_select' => 
  array (
    'return' => 'mixed',
    'params' => 'resource db, string table, array ids[, int options]',
    'description' => 'Select records that has ids (id=>value)',
  ),
  'filepro' => 
  array (
    'return' => 'bool',
    'params' => 'string directory',
    'description' => 'Read and verify the map file',
  ),
  'filepro_rowcount' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Find out how many rows are in a filePro database',
  ),
  'filepro_fieldname' => 
  array (
    'return' => 'string',
    'params' => 'int fieldnumber',
    'description' => 'Gets the name of a field',
  ),
  'filepro_fieldtype' => 
  array (
    'return' => 'string',
    'params' => 'int field_number',
    'description' => 'Gets the type of a field',
  ),
  'filepro_fieldwidth' => 
  array (
    'return' => 'int',
    'params' => 'int field_number',
    'description' => 'Gets the width of a field',
  ),
  'filepro_fieldcount' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Find out how many fields are in a filePro database',
  ),
  'filepro_retrieve' => 
  array (
    'return' => 'string',
    'params' => 'int row_number, int field_number',
    'description' => 'Retrieves data from a filePro database',
  ),
  'bzread' => 
  array (
    'return' => 'string',
    'params' => 'int bz[, int length]',
    'description' => 'Reads up to length bytes from a BZip2 stream, or 1024 bytes if length is not specified',
  ),
  'bzopen' => 
  array (
    'return' => 'resource',
    'params' => 'string|int file|fp, string mode',
    'description' => 'Opens a new BZip2 stream',
  ),
  'bzerrno' => 
  array (
    'return' => 'int',
    'params' => 'resource bz',
    'description' => 'Returns the error number',
  ),
  'bzerrstr' => 
  array (
    'return' => 'string',
    'params' => 'resource bz',
    'description' => 'Returns the error string',
  ),
  'bzerror' => 
  array (
    'return' => 'array',
    'params' => 'resource bz',
    'description' => 'Returns the error number and error string in an associative array',
  ),
  'bzcompress' => 
  array (
    'return' => 'string',
    'params' => 'string source [, int blocksize100k [, int workfactor]]',
    'description' => 'Compresses a string into BZip2 encoded data',
  ),
  'bzdecompress' => 
  array (
    'return' => 'string',
    'params' => 'string source [, int small]',
    'description' => 'Decompresses BZip2 compressed data',
  ),
  'dba_popen' => 
  array (
    'return' => 'resource',
    'params' => 'string path, string mode [, string handlername, string ...]',
    'description' => 'Opens path using the specified handler in mode persistently',
  ),
  'dba_open' => 
  array (
    'return' => 'resource',
    'params' => 'string path, string mode [, string handlername, string ...]',
    'description' => 'Opens path using the specified handler in mode',
  ),
  'dba_close' => 
  array (
    'return' => 'void',
    'params' => 'resource handle',
    'description' => 'Closes database',
  ),
  'dba_exists' => 
  array (
    'return' => 'bool',
    'params' => 'string key, resource handle',
    'description' => 'Checks, if the specified key exists',
  ),
  'dba_fetch' => 
  array (
    'return' => 'string',
    'params' => 'string key, [int skip ,] resource handle',
    'description' => 'Fetches the data associated with key',
  ),
  'dba_key_split' => 
  array (
    'return' => 'array|false',
    'params' => 'string key',
    'description' => 'Splits an inifile key into an array of the form array(0=>group,1=>value_name) but returns false if input is false or null',
  ),
  'dba_firstkey' => 
  array (
    'return' => 'string',
    'params' => 'resource handle',
    'description' => 'Resets the internal key pointer and returns the first key',
  ),
  'dba_nextkey' => 
  array (
    'return' => 'string',
    'params' => 'resource handle',
    'description' => 'Returns the next key',
  ),
  'dba_delete' => 
  array (
    'return' => 'bool',
    'params' => 'string key, resource handle',
    'description' => 'Deletes the entry associated with keyIf inifile: remove all other key lines',
  ),
  'dba_insert' => 
  array (
    'return' => 'bool',
    'params' => 'string key, string value, resource handle',
    'description' => 'If not inifile: Insert value as key, return false, if key exists alreadyIf inifile: Add vakue as key (next instance of key)',
  ),
  'dba_replace' => 
  array (
    'return' => 'bool',
    'params' => 'string key, string value, resource handle',
    'description' => 'Inserts value as key, replaces key, if key exists alreadyIf inifile: remove all other key lines',
  ),
  'dba_optimize' => 
  array (
    'return' => 'bool',
    'params' => 'resource handle',
    'description' => 'Optimizes (e.g. clean up, vacuum) database',
  ),
  'dba_sync' => 
  array (
    'return' => 'bool',
    'params' => 'resource handle',
    'description' => 'Synchronizes database',
  ),
  'dba_handlers' => 
  array (
    'return' => 'array',
    'params' => '[bool full_info]',
    'description' => 'List configured database handlers',
  ),
  'dba_list' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'List opened databases',
  ),
  'iconv_strlen' => 
  array (
    'return' => 'int',
    'params' => 'string str [, string charset]',
    'description' => 'Returns the character count of str',
  ),
  'iconv_substr' => 
  array (
    'return' => 'string',
    'params' => 'string str, int offset, [int length, string charset]',
    'description' => 'Returns specified part of a string',
  ),
  'iconv_strpos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle, int offset [, string charset]',
    'description' => 'Finds position of first occurrence of needle within part of haystack beginning with offset',
  ),
  'iconv_strrpos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, string charset]',
    'description' => 'Finds position of last occurrence of needle within part of haystack beginning with offset',
  ),
  'iconv_mime_encode' => 
  array (
    'return' => 'string',
    'params' => 'string field_name, string field_value, [, array preference]',
    'description' => 'Composes a mime header field with field_name and field_value in a specified scheme',
  ),
  'iconv_mime_decode' => 
  array (
    'return' => 'string',
    'params' => 'string encoded_string [, int mode, string charset]',
    'description' => 'Decodes a mime header field',
  ),
  'iconv_mime_decode_headers' => 
  array (
    'return' => 'array',
    'params' => 'string headers [, int mode, string charset]',
    'description' => 'Decodes multiple mime header fields',
  ),
  'iconv' => 
  array (
    'return' => 'string',
    'params' => 'string in_charset, string out_charset, string str',
    'description' => 'Returns str converted to the out_charset character set',
  ),
  'ob_iconv_handler' => 
  array (
    'return' => 'string',
    'params' => 'string contents, int status',
    'description' => 'Returns str in output buffer converted to the iconv.output_encoding character set',
  ),
  'iconv_set_encoding' => 
  array (
    'return' => 'bool',
    'params' => 'string type, string charset',
    'description' => 'Sets internal encoding and output encoding for ob_iconv_handler()',
  ),
  'iconv_get_encoding' => 
  array (
    'return' => 'mixed',
    'params' => '[string type]',
    'description' => 'Get internal encoding and output encoding for ob_iconv_handler()',
  ),
  'ctype_alnum' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for alphanumeric character(s)',
  ),
  'ctype_alpha' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for alphabetic character(s)',
  ),
  'ctype_cntrl' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for control character(s)',
  ),
  'ctype_digit' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for numeric character(s)',
  ),
  'ctype_lower' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for lowercase character(s)',
  ),
  'ctype_graph' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for any printable character(s) except space',
  ),
  'ctype_print' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for printable character(s)',
  ),
  'ctype_punct' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for any printable character which is not whitespace or an alphanumeric character',
  ),
  'ctype_space' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for whitespace character(s)',
  ),
  'ctype_upper' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for uppercase character(s)',
  ),
  'ctype_xdigit' => 
  array (
    'return' => 'bool',
    'params' => 'mixed c',
    'description' => 'Checks for character(s) representing a hexadecimal digit',
  ),
  'bcadd' => 
  array (
    'return' => 'string',
    'params' => 'string left_operand, string right_operand [, int scale]',
    'description' => 'Returns the sum of two arbitrary precision numbers',
  ),
  'bcsub' => 
  array (
    'return' => 'string',
    'params' => 'string left_operand, string right_operand [, int scale]',
    'description' => 'Returns the difference between two arbitrary precision numbers',
  ),
  'bcmul' => 
  array (
    'return' => 'string',
    'params' => 'string left_operand, string right_operand [, int scale]',
    'description' => 'Returns the multiplication of two arbitrary precision numbers',
  ),
  'bcdiv' => 
  array (
    'return' => 'string',
    'params' => 'string left_operand, string right_operand [, int scale]',
    'description' => 'Returns the quotient of two arbitrary precision numbers (division)',
  ),
  'bcmod' => 
  array (
    'return' => 'string',
    'params' => 'string left_operand, string right_operand',
    'description' => 'Returns the modulus of the two arbitrary precision operands',
  ),
  'bcpowmod' => 
  array (
    'return' => 'string',
    'params' => 'string x, string y, string mod [, int scale]',
    'description' => 'Returns the value of an arbitrary precision number raised to the power of another reduced by a modulous',
  ),
  'bcpow' => 
  array (
    'return' => 'string',
    'params' => 'string x, string y [, int scale]',
    'description' => 'Returns the value of an arbitrary precision number raised to the power of another',
  ),
  'bcsqrt' => 
  array (
    'return' => 'string',
    'params' => 'string operand [, int scale]',
    'description' => 'Returns the square root of an arbitray precision number',
  ),
  'bccomp' => 
  array (
    'return' => 'int',
    'params' => 'string left_operand, string right_operand [, int scale]',
    'description' => 'Compares two arbitrary precision numbers',
  ),
  'bcscale' => 
  array (
    'return' => 'bool',
    'params' => 'int scale',
    'description' => 'Sets default scale parameter for all bc math functions',
  ),
  'ldap_connect' => 
  array (
    'return' => 'resource',
    'params' => '[string host [, int port]]',
    'description' => 'Connect to an LDAP server',
  ),
  'ldap_bind' => 
  array (
    'return' => 'bool',
    'params' => 'resource link [, string dn, string password]',
    'description' => 'Bind to LDAP directory',
  ),
  'ldap_sasl_bind' => 
  array (
    'return' => 'bool',
    'params' => 'resource link [, string binddn, string password, string sasl_mech, string sasl_realm, string sasl_authz_id, string props]',
    'description' => 'Bind to LDAP directory using SASL',
  ),
  'ldap_unbind' => 
  array (
    'return' => 'bool',
    'params' => 'resource link',
    'description' => 'Unbind from LDAP directory',
  ),
  'ldap_read' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, string base_dn, string filter [, array attrs [, int attrsonly [, int sizelimit [, int timelimit [, int deref]]]]]',
    'description' => 'Read an entry',
  ),
  'ldap_list' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, string base_dn, string filter [, array attrs [, int attrsonly [, int sizelimit [, int timelimit [, int deref]]]]]',
    'description' => 'Single-level search',
  ),
  'ldap_search' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, string base_dn, string filter [, array attrs [, int attrsonly [, int sizelimit [, int timelimit [, int deref]]]]]',
    'description' => 'Search LDAP tree under base_dn',
  ),
  'ldap_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Free result memory',
  ),
  'ldap_count_entries' => 
  array (
    'return' => 'int',
    'params' => 'resource link, resource result',
    'description' => 'Count the number of entries in a search result',
  ),
  'ldap_first_entry' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, resource result',
    'description' => 'Return first result id',
  ),
  'ldap_next_entry' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, resource result_entry',
    'description' => 'Get next result entry',
  ),
  'ldap_get_entries' => 
  array (
    'return' => 'array',
    'params' => 'resource link, resource result',
    'description' => 'Get all result entries',
  ),
  'ldap_first_attribute' => 
  array (
    'return' => 'string',
    'params' => 'resource link, resource result_entry, int ber',
    'description' => 'Return first attribute',
  ),
  'ldap_next_attribute' => 
  array (
    'return' => 'string',
    'params' => 'resource link, resource result_entry, resource ber',
    'description' => 'Get the next attribute in result',
  ),
  'ldap_get_attributes' => 
  array (
    'return' => 'array',
    'params' => 'resource link, resource result_entry',
    'description' => 'Get attributes from a search result entry',
  ),
  'ldap_get_values' => 
  array (
    'return' => 'array',
    'params' => 'resource link, resource result_entry, string attribute',
    'description' => 'Get all values from a result entry',
  ),
  'ldap_get_values_len' => 
  array (
    'return' => 'array',
    'params' => 'resource link, resource result_entry, string attribute',
    'description' => 'Get all values with lengths from a result entry',
  ),
  'ldap_get_dn' => 
  array (
    'return' => 'string',
    'params' => 'resource link, resource result_entry',
    'description' => 'Get the DN of a result entry',
  ),
  'ldap_explode_dn' => 
  array (
    'return' => 'array',
    'params' => 'string dn, int with_attrib',
    'description' => 'Splits DN into its component parts',
  ),
  'ldap_dn2ufn' => 
  array (
    'return' => 'string',
    'params' => 'string dn',
    'description' => 'Convert DN to User Friendly Naming format',
  ),
  'ldap_add' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn, array entry',
    'description' => 'Add entries to LDAP directory',
  ),
  'ldap_mod_replace' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn, array entry',
    'description' => 'Replace attribute values with new ones',
  ),
  'ldap_mod_add' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn, array entry',
    'description' => 'Add attribute values to current',
  ),
  'ldap_mod_del' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn, array entry',
    'description' => 'Delete attribute values',
  ),
  'ldap_delete' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn',
    'description' => 'Delete an entry from a directory',
  ),
  'ldap_errno' => 
  array (
    'return' => 'int',
    'params' => 'resource link',
    'description' => 'Get the current ldap error number',
  ),
  'ldap_err2str' => 
  array (
    'return' => 'string',
    'params' => 'int errno',
    'description' => 'Convert error number to error string',
  ),
  'ldap_error' => 
  array (
    'return' => 'string',
    'params' => 'resource link',
    'description' => 'Get the current ldap error string',
  ),
  'ldap_compare' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn, string attr, string value',
    'description' => 'Determine if an entry has a specific value for one of its attributes',
  ),
  'ldap_sort' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, resource result, string sortfilter',
    'description' => 'Sort LDAP result entries',
  ),
  'ldap_get_option' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, int option, mixed retval',
    'description' => 'Get the current value of various session-wide parameters',
  ),
  'ldap_set_option' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, int option, mixed newval',
    'description' => 'Set the value of various session-wide parameters',
  ),
  'ldap_parse_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, resource result, int errcode, string matcheddn, string errmsg, array referrals',
    'description' => 'Extract information from result',
  ),
  'ldap_first_reference' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, resource result',
    'description' => 'Return first reference',
  ),
  'ldap_next_reference' => 
  array (
    'return' => 'resource',
    'params' => 'resource link, resource reference_entry',
    'description' => 'Get next reference',
  ),
  'ldap_parse_reference' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, resource reference_entry, array referrals',
    'description' => 'Extract information from reference entry',
  ),
  'ldap_rename' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string dn, string newrdn, string newparent, bool deleteoldrdn',
    'description' => 'Modify the name of an entry',
  ),
  'ldap_start_tls' => 
  array (
    'return' => 'bool',
    'params' => 'resource link',
    'description' => 'Start TLS',
  ),
  'ldap_set_rebind_proc' => 
  array (
    'return' => 'bool',
    'params' => 'resource link, string callback',
    'description' => 'Set a callback function to do re-binds on referral chasing.',
  ),
  'ldap_t61_to_8859' => 
  array (
    'return' => 'string',
    'params' => 'string value',
    'description' => 'Translate t61 characters to 8859 characters',
  ),
  'ldap_8859_to_t61' => 
  array (
    'return' => 'string',
    'params' => 'string value',
    'description' => 'Translate 8859 characters to t61 characters',
  ),
  'SoapServer::setClass' => 
  array (
    'return' => 'void',
    'params' => 'string class_name [, mixed args]',
    'description' => 'Sets class which will handle SOAP requests',
  ),
  'SoapServer::getFunctions' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns list of defined functions',
  ),
  'SoapServer::addFunction' => 
  array (
    'return' => 'void',
    'params' => 'mixed functions',
    'description' => 'Adds one or several functions those will handle SOAP requests',
  ),
  'SoapClient::__getLastRequestHeaders' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns last SOAP request headers',
  ),
  'SoapClient::__getLastResponseHeaders' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns last SOAP response headers',
  ),
  'SoapClient::__doRequest' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'SoapClient::__doRequest()',
  ),
  'SoapClient::__setCookie' => 
  array (
    'return' => 'void',
    'params' => 'string name [, strung value]',
    'description' => 'Sets cookie thet will sent with SOAP request.The call to this function will effect all folowing calls of SOAP methods.If value is not specified cookie is removed.',
  ),
  'SoapClient::__setSoapHeaders' => 
  array (
    'return' => 'void',
    'params' => 'array SoapHeaders',
    'description' => 'Sets SOAP headers for subsequent calls (replaces any previousvalues).If no value is specified, all of the headers are removed.',
  ),
  'SoapClient::__setLocation' => 
  array (
    'return' => 'string',
    'params' => '[string new_location]',
    'description' => 'Sets the location option (the endpoint URL that will be touched by thefollowing SOAP requests).If new_location is not specified or null then SoapClient will use endpointfrom WSDL file.The function returns old value of location options.',
  ),
  'fbsql_connect' => 
  array (
    'return' => 'resource',
    'params' => '[string hostname [, string username [, string password]]]',
    'description' => 'Create a connection to a database server',
  ),
  'fbsql_pconnect' => 
  array (
    'return' => 'resource',
    'params' => '[string hostname [, string username [, string password]]]',
    'description' => 'Create a persistant connection to a database server',
  ),
  'fbsql_close' => 
  array (
    'return' => 'bool',
    'params' => '[resource link_identifier]',
    'description' => 'Close a connection to a database server',
  ),
  'fbsql_set_transaction' => 
  array (
    'return' => 'void',
    'params' => 'resource link_identifier, int locking, int isolation',
    'description' => 'Sets the transaction locking and isolation',
  ),
  'fbsql_autocommit' => 
  array (
    'return' => 'bool',
    'params' => 'resource link_identifier [, bool OnOff]',
    'description' => 'Turns on auto-commit',
  ),
  'fbsql_commit' => 
  array (
    'return' => 'bool',
    'params' => '[resource link_identifier]',
    'description' => 'Commit the transaction',
  ),
  'fbsql_rollback' => 
  array (
    'return' => 'bool',
    'params' => '[resource link_identifier]',
    'description' => 'Rollback all statments since last commit',
  ),
  'fbsql_create_blob' => 
  array (
    'return' => 'string',
    'params' => 'string blob_data [, resource link_identifier]',
    'description' => 'Create a BLOB in the database for use with an insert or update statement',
  ),
  'fbsql_create_clob' => 
  array (
    'return' => 'string',
    'params' => 'string clob_data [, resource link_identifier]',
    'description' => 'Create a CLOB in the database for use with an insert or update statement',
  ),
  'fbsql_set_lob_mode' => 
  array (
    'return' => 'bool',
    'params' => 'resource result, int lob_mode',
    'description' => 'Sets the mode for how LOB data re retreived (actual data or a handle)',
  ),
  'fbsql_read_blob' => 
  array (
    'return' => 'string',
    'params' => 'string blob_handle [, resource link_identifier]',
    'description' => 'Read the BLOB data identified by blob_handle',
  ),
  'fbsql_read_clob' => 
  array (
    'return' => 'string',
    'params' => 'string clob_handle [, resource link_identifier]',
    'description' => 'Read the CLOB data identified by clob_handle',
  ),
  'fbsql_blob_size' => 
  array (
    'return' => 'int',
    'params' => 'string blob_handle [, resource link_identifier]',
    'description' => 'Get the size of a BLOB identified by blob_handle',
  ),
  'fbsql_clob_size' => 
  array (
    'return' => 'int',
    'params' => 'string clob_handle [, resource link_identifier]',
    'description' => 'Get the size of a CLOB identified by clob_handle',
  ),
  'fbsql_hostname' => 
  array (
    'return' => 'string',
    'params' => 'resource link_identifier [, string host_name]',
    'description' => 'Get or set the host name used with a connection',
  ),
  'fbsql_database' => 
  array (
    'return' => 'string',
    'params' => 'resource link_identifier [, string database]',
    'description' => 'Get or set the database name used with a connection',
  ),
  'fbsql_database_password' => 
  array (
    'return' => 'string',
    'params' => 'resource link_identifier [, string database_password]',
    'description' => 'Get or set the databsae password used with a connection',
  ),
  'fbsql_username' => 
  array (
    'return' => 'string',
    'params' => 'resource link_identifier [, string username]',
    'description' => 'Get or set the host user used with a connection',
  ),
  'fbsql_password' => 
  array (
    'return' => 'string',
    'params' => 'resource link_identifier [, string password]',
    'description' => 'Get or set the user password used with a connection',
  ),
  'fbsql_set_password' => 
  array (
    'return' => 'bool',
    'params' => 'resource link_identifier, string user, string password, string old_password',
    'description' => 'Change the password for a given user',
  ),
  'fbsql_select_db' => 
  array (
    'return' => 'bool',
    'params' => '[string database_name [, resource link_identifier]]',
    'description' => 'Select the database to open',
  ),
  'fbsql_set_characterset' => 
  array (
    'return' => 'void',
    'params' => 'resource link_identifier, long charcterset [, long in_out_both]]',
    'description' => 'Change input/output character set',
  ),
  'fbsql_change_user' => 
  array (
    'return' => 'int',
    'params' => 'string user, string password [, string database [, resource link_identifier]]',
    'description' => 'Change the user for a session',
  ),
  'fbsql_create_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Create a new database on the server',
  ),
  'fbsql_drop_db' => 
  array (
    'return' => 'int',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Drop a database on the server',
  ),
  'fbsql_start_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource link_identifier [, string database_options]]',
    'description' => 'Start a database on the server',
  ),
  'fbsql_stop_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Stop a database on the server',
  ),
  'fbsql_db_status' => 
  array (
    'return' => 'int',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Gets the status (Stopped, Starting, Running, Stopping) for a given database',
  ),
  'fbsql_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query [, resource link_identifier [, long batch_size]]',
    'description' => 'Send one or more SQL statements to the server and execute them',
  ),
  'fbsql_db_query' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name, string query [, resource link_identifier]',
    'description' => 'Send one or more SQL statements to a specified database on the server',
  ),
  'fbsql_list_dbs' => 
  array (
    'return' => 'resource',
    'params' => '[resource link_identifier]',
    'description' => 'Retreive a list of all databases on the server',
  ),
  'fbsql_list_tables' => 
  array (
    'return' => 'resource',
    'params' => 'string database [, int link_identifier]',
    'description' => 'Retreive a list of all tables from the specifoied database',
  ),
  'fbsql_list_fields' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name, string table_name [, resource link_identifier]',
    'description' => 'Retrieve a list of all fields for the specified database.table',
  ),
  'fbsql_error' => 
  array (
    'return' => 'string',
    'params' => '[resource link_identifier]',
    'description' => 'Returns the last error string',
  ),
  'fbsql_errno' => 
  array (
    'return' => 'int',
    'params' => '[resource link_identifier]',
    'description' => 'Returns the last error code',
  ),
  'fbsql_warnings' => 
  array (
    'return' => 'bool',
    'params' => '[int flag]',
    'description' => 'Enable or disable FrontBase warnings',
  ),
  'fbsql_affected_rows' => 
  array (
    'return' => 'int',
    'params' => '[resource link_identifier]',
    'description' => 'Get the number of rows affected by the last statement',
  ),
  'fbsql_insert_id' => 
  array (
    'return' => 'int',
    'params' => '[resource link_identifier]',
    'description' => 'Get the internal index for the last insert statement',
  ),
  'fbsql_result' => 
  array (
    'return' => 'mixed',
    'params' => 'int result [, int row [, mixed field]]',
    'description' => '???',
  ),
  'fbsql_next_result' => 
  array (
    'return' => 'bool',
    'params' => 'int result',
    'description' => 'Switch to the next result if multiple results are available',
  ),
  'fbsql_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'int result',
    'description' => 'Get number of rows',
  ),
  'fbsql_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'int result',
    'description' => 'Get number of fields in the result set',
  ),
  'fbsql_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'resource result',
    'description' => 'Fetch a row of data. Returns an indexed array',
  ),
  'fbsql_fetch_assoc' => 
  array (
    'return' => 'object',
    'params' => 'resource result',
    'description' => 'Detch a row of data. Returns an assoc array',
  ),
  'fbsql_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource result [, int result_type]',
    'description' => 'Fetch a row of data. Returns an object',
  ),
  'fbsql_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int result_type]',
    'description' => 'Fetches a result row as an array (associative, numeric or both)',
  ),
  'fbsql_data_seek' => 
  array (
    'return' => 'bool',
    'params' => 'int result, int row_number',
    'description' => 'Move the internal row counter to the specified row_number',
  ),
  'fbsql_fetch_lengths' => 
  array (
    'return' => 'array',
    'params' => 'int result',
    'description' => 'Returns an array of the lengths of each column in the result set',
  ),
  'fbsql_fetch_field' => 
  array (
    'return' => 'object',
    'params' => 'int result [, int field_index]',
    'description' => 'Get the field properties for a specified field_index',
  ),
  'fbsql_field_seek' => 
  array (
    'return' => 'bool',
    'params' => 'int result [, int field_index]',
    'description' => '???',
  ),
  'fbsql_field_name' => 
  array (
    'return' => 'string',
    'params' => 'int result [, int field_index]',
    'description' => 'Get the column name for a specified field_index',
  ),
  'fbsql_field_table' => 
  array (
    'return' => 'string',
    'params' => 'int result [, int field_index]',
    'description' => 'Get the table name for a specified field_index',
  ),
  'fbsql_field_len' => 
  array (
    'return' => 'mixed',
    'params' => 'int result [, int field_index]',
    'description' => 'Get the column length for a specified field_index',
  ),
  'fbsql_field_type' => 
  array (
    'return' => 'string',
    'params' => 'int result [, int field_index]',
    'description' => 'Get the field type for a specified field_index',
  ),
  'fbsql_field_flags' => 
  array (
    'return' => 'string',
    'params' => 'int result [, int field_index]',
    'description' => '???',
  ),
  'fbsql_table_name' => 
  array (
    'return' => 'string',
    'params' => 'resource result, int index',
    'description' => 'Retreive the table name for index after a call to fbsql_list_tables()',
  ),
  'fbsql_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'free the memory used to store a result',
  ),
  'fbsql_get_autostart_info' => 
  array (
    'return' => 'array',
    'params' => '[resource link_identifier]',
    'description' => '???',
  ),
  'sem_get' => 
  array (
    'return' => 'resource',
    'params' => 'int key [, int max_acquire [, int perm [, int auto_release]]',
    'description' => 'Return an id for the semaphore with the given key, and allow max_acquire (default 1) processes to acquire it simultaneously',
  ),
  'sem_acquire' => 
  array (
    'return' => 'bool',
    'params' => 'resource id',
    'description' => 'Acquires the semaphore with the given id, blocking if necessary',
  ),
  'sem_release' => 
  array (
    'return' => 'bool',
    'params' => 'resource id',
    'description' => 'Releases the semaphore with the given id',
  ),
  'sem_remove' => 
  array (
    'return' => 'bool',
    'params' => 'resource id',
    'description' => 'Removes semaphore from Unix systems',
  ),
  'token_get_all' => 
  array (
    'return' => 'array',
    'params' => 'string source',
    'description' => '',
  ),
  'token_name' => 
  array (
    'return' => 'string',
    'params' => 'int type',
    'description' => '',
  ),
  'gzfile' => 
  array (
    'return' => 'array',
    'params' => 'string filename [, int use_include_path]',
    'description' => 'Read und uncompress entire .gz-file into an array',
  ),
  'gzopen' => 
  array (
    'return' => 'resource',
    'params' => 'string filename, string mode [, int use_include_path]',
    'description' => 'Open a .gz-file and return a .gz-file pointer',
  ),
  'readgzfile' => 
  array (
    'return' => 'int',
    'params' => 'string filename [, int use_include_path]',
    'description' => 'Output a .gz-file',
  ),
  'gzcompress' => 
  array (
    'return' => 'string',
    'params' => 'string data [, int level]',
    'description' => 'Gzip-compress a string',
  ),
  'gzuncompress' => 
  array (
    'return' => 'string',
    'params' => 'string data [, int length]',
    'description' => 'Unzip a gzip-compressed string',
  ),
  'gzdeflate' => 
  array (
    'return' => 'string',
    'params' => 'string data [, int level]',
    'description' => 'Gzip-compress a string',
  ),
  'gzinflate' => 
  array (
    'return' => 'string',
    'params' => 'string data [, int length]',
    'description' => 'Unzip a gzip-compressed string',
  ),
  'zlib_get_coding_type' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns the coding type used for output compression',
  ),
  'gzencode' => 
  array (
    'return' => 'string',
    'params' => 'string data [, int level [, int encoding_mode]]',
    'description' => 'GZ encode a string',
  ),
  'ob_gzhandler' => 
  array (
    'return' => 'string',
    'params' => 'string str, int mode',
    'description' => 'Encode str based on accept-encoding setting - designed to be called from ob_start()',
  ),
  'msql_connect' => 
  array (
    'return' => 'int',
    'params' => '[string hostname[:port]] [, string username] [, string password]',
    'description' => 'Open a connection to an mSQL Server',
  ),
  'msql_pconnect' => 
  array (
    'return' => 'int',
    'params' => '[string hostname[:port]] [, string username] [, string password]',
    'description' => 'Open a persistent connection to an mSQL Server',
  ),
  'msql_close' => 
  array (
    'return' => 'bool',
    'params' => '[resource link_identifier]',
    'description' => 'Close an mSQL connection',
  ),
  'msql_select_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Select an mSQL database',
  ),
  'msql_create_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Create an mSQL database',
  ),
  'msql_drop_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'Drop (delete) an mSQL database',
  ),
  'msql_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query [, resource link_identifier]',
    'description' => 'Send an SQL query to mSQL',
  ),
  'msql_db_query' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name, string query [, resource link_identifier]',
    'description' => 'Send an SQL query to mSQL',
  ),
  'msql_list_dbs' => 
  array (
    'return' => 'resource',
    'params' => '[resource link_identifier]',
    'description' => 'List databases available on an mSQL server',
  ),
  'msql_list_tables' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name [, resource link_identifier]',
    'description' => 'List tables in an mSQL database',
  ),
  'msql_list_fields' => 
  array (
    'return' => 'resource',
    'params' => 'string database_name, string table_name [, resource link_identifier]',
    'description' => 'List mSQL result fields',
  ),
  'msql_error' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns the text of the error message from previous mSQL operation',
  ),
  'msql_result' => 
  array (
    'return' => 'string',
    'params' => 'int query, int row [, mixed field]',
    'description' => 'Get result data',
  ),
  'msql_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource query',
    'description' => 'Get number of rows in a result',
  ),
  'msql_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource query',
    'description' => 'Get number of fields in a result',
  ),
  'msql_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'resource query',
    'description' => 'Get a result row as an enumerated array',
  ),
  'msql_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource query [, resource result_type]',
    'description' => 'Fetch a result row as an object',
  ),
  'msql_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'resource query [, int result_type]',
    'description' => 'Fetch a result row as an associative array',
  ),
  'msql_data_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource query, int row_number',
    'description' => 'Move internal result pointer',
  ),
  'msql_fetch_field' => 
  array (
    'return' => 'object',
    'params' => 'resource query [, int field_offset]',
    'description' => 'Get column information from a result and return as an object',
  ),
  'msql_field_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource query, int field_offset',
    'description' => 'Set result pointer to a specific field offset',
  ),
  'msql_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource query, int field_index',
    'description' => 'Get the name of the specified field in a result',
  ),
  'msql_field_table' => 
  array (
    'return' => 'string',
    'params' => 'resource query, int field_offset',
    'description' => 'Get name of the table the specified field is in',
  ),
  'msql_field_len' => 
  array (
    'return' => 'int',
    'params' => 'int query, int field_offet',
    'description' => 'Returns the length of the specified field',
  ),
  'msql_field_type' => 
  array (
    'return' => 'string',
    'params' => 'resource query, int field_offset',
    'description' => 'Get the type of the specified field in a result',
  ),
  'msql_field_flags' => 
  array (
    'return' => 'string',
    'params' => 'resource query, int field_offset',
    'description' => 'Get the flags associated with the specified field in a result',
  ),
  'msql_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource query',
    'description' => 'Free result memory',
  ),
  'msql_affected_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource query',
    'description' => 'Return number of affected rows',
  ),
  'PDO::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string dsn, string username, string passwd [, array options]',
    'description' => '',
  ),
  'PDO::prepare' => 
  array (
    'return' => 'object',
    'params' => 'string statment [, array options]',
    'description' => 'Prepares a statement for execution and returns a statement object',
  ),
  'PDO::beginTransaction' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Initiates a transaction',
  ),
  'PDO::commit' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Commit a transaction',
  ),
  'PDO::rollBack' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'roll back a transaction',
  ),
  'PDO::setAttribute' => 
  array (
    'return' => 'bool',
    'params' => 'long attribute, mixed value',
    'description' => 'Set an attribute',
  ),
  'PDO::getAttribute' => 
  array (
    'return' => 'mixed',
    'params' => 'long attribute',
    'description' => 'Get an attribute',
  ),
  'PDO::exec' => 
  array (
    'return' => 'long',
    'params' => 'string query',
    'description' => 'Execute a query that does not return a row set, returning the number of affected rows',
  ),
  'PDO::lastInsertId' => 
  array (
    'return' => 'string',
    'params' => '[string seqname]',
    'description' => 'Returns the id of the last row that we affected on this connection.  Some databases require a sequence or table name to be passed in.  Not always meaningful.',
  ),
  'PDO::errorCode' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Fetch the error code associated with the last operation on the database handle',
  ),
  'PDO::errorInfo' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Fetch extended error information associated with the last operation on the database handle',
  ),
  'PDO::query' => 
  array (
    'return' => 'object',
    'params' => 'string sql [, PDOStatement::setFetchMode() args]',
    'description' => 'Prepare and execute $sql; returns the statement object for iteration',
  ),
  'PDO::quote' => 
  array (
    'return' => 'string',
    'params' => 'string string [, int paramtype]',
    'description' => 'quotes string for use in a query.  The optional paramtype acts as a hint for drivers that have alternate quoting styles.  The default value is PDO_PARAM_STR',
  ),
  'PDO::__wakeup' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Prevents use of a PDO instance that has been unserialized',
  ),
  'PDO::__sleep' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Prevents serialization of a PDO instance',
  ),
  'pdo_drivers' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Return array of available PDO drivers',
  ),
  'PDOStatement::execute' => 
  array (
    'return' => 'bool',
    'params' => '[array $bound_input_params]',
    'description' => 'Execute a prepared statement, optionally binding parameters',
  ),
  'PDOStatement::fetch' => 
  array (
    'return' => 'mixed',
    'params' => '[int $how = PDO_FETCH_BOTH [, int $orientation [, int $offset]]]',
    'description' => 'Fetches the next row and returns it, or false if there are no more rows',
  ),
  'PDOStatement::fetchObject' => 
  array (
    'return' => 'mixed',
    'params' => 'string class_name [, NULL|array ctor_args]',
    'description' => 'Fetches the next row and returns it as an object.',
  ),
  'PDOStatement::fetchColumn' => 
  array (
    'return' => 'string',
    'params' => '[int column_number]',
    'description' => 'Returns a data of the specified column in the result set.',
  ),
  'PDOStatement::fetchAll' => 
  array (
    'return' => 'array',
    'params' => '[int $how = PDO_FETCH_BOTH [, string class_name [, NULL|array ctor_args]]]',
    'description' => 'Returns an array of all of the results.',
  ),
  'PDOStatement::bindValue' => 
  array (
    'return' => 'bool',
    'params' => 'mixed $paramno, mixed $param [, int $type ]',
    'description' => 'bind an input parameter to the value of a PHP variable.  $paramno is the 1-based position of the placeholder in the SQL statement (but can be the parameter name for drivers that support named placeholders).  It should be called prior to execute().',
  ),
  'PDOStatement::bindParam' => 
  array (
    'return' => 'bool',
    'params' => 'mixed $paramno, mixed &$param [, int $type [, int $maxlen [, mixed $driverdata]]]',
    'description' => 'bind a parameter to a PHP variable.  $paramno is the 1-based position of the placeholder in the SQL statement (but can be the parameter name for drivers that support named placeholders).  This isn\'t supported by all drivers.  It should be called prior to execute().',
  ),
  'PDOStatement::bindColumn' => 
  array (
    'return' => 'bool',
    'params' => 'mixed $column, mixed &$param [, int $type [, int $maxlen [, mixed $driverdata]]]',
    'description' => 'bind a column to a PHP variable.  On each row fetch $param will contain the value of the corresponding column.  $column is the 1-based offset of the column, or the column name.  For portability, don\'t call this before execute().',
  ),
  'PDOStatement::rowCount' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the number of rows in a result set, or the number of rows affected by the last execute().  It is not always meaningful.',
  ),
  'PDOStatement::errorCode' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Fetch the error code associated with the last operation on the statement handle',
  ),
  'PDOStatement::errorInfo' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Fetch extended error information associated with the last operation on the statement handle',
  ),
  'PDOStatement::setAttribute' => 
  array (
    'return' => 'bool',
    'params' => 'long attribute, mixed value',
    'description' => 'Set an attribute',
  ),
  'PDOStatement::getAttribute' => 
  array (
    'return' => 'mixed',
    'params' => 'long attribute',
    'description' => 'Get an attribute',
  ),
  'PDOStatement::columnCount' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the number of columns in the result set',
  ),
  'PDOStatement::getColumnMeta' => 
  array (
    'return' => 'array',
    'params' => 'int $column',
    'description' => 'Returns meta data for a numbered column',
  ),
  'PDOStatement::setFetchMode' => 
  array (
    'return' => 'bool',
    'params' => 'int mode [mixed* params]',
    'description' => 'Changes the default fetch mode for subsequent fetches (params have different meaning for different fetch modes)',
  ),
  'PDOStatement::nextRowset' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Advances to the next rowset in a multi-rowset statement handle. Returns true if it succeded, false otherwise',
  ),
  'PDOStatement::closeCursor' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Closes the cursor, leaving the statement ready for re-execution.',
  ),
  'PDOStatement::debugDumpParams' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'A utility for internals hackers to debug parameter internals',
  ),
  'PDOStatement::__wakeup' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Prevents use of a PDOStatement instance that has been unserialized',
  ),
  'PDOStatement::__sleep' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Prevents serialization of a PDOStatement instance',
  ),
  'xsl_xsltprocessor_import_stylesheet' => 
  array (
    'return' => 'void',
    'params' => 'domdocument doc',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Since:',
  ),
  'xsl_xsltprocessor_transform_to_doc' => 
  array (
    'return' => 'domdocument',
    'params' => 'domnode doc',
    'description' => 'URL: http://www.w3.org/TR/2003/WD-DOM-Level-3-Core-20030226/DOM3-Core.html#Since:',
  ),
  'xsl_xsltprocessor_transform_to_uri' => 
  array (
    'return' => 'int',
    'params' => 'domdocument doc, string uri',
    'description' => '',
  ),
  'xsl_xsltprocessor_transform_to_xml' => 
  array (
    'return' => 'string',
    'params' => 'domdocument doc',
    'description' => '',
  ),
  'xsl_xsltprocessor_set_parameter' => 
  array (
    'return' => 'bool',
    'params' => 'string namespace, mixed name [, string value]',
    'description' => '',
  ),
  'xsl_xsltprocessor_get_parameter' => 
  array (
    'return' => 'string',
    'params' => 'string namespace, string name',
    'description' => '',
  ),
  'xsl_xsltprocessor_remove_parameter' => 
  array (
    'return' => 'bool',
    'params' => 'string namespace, string name',
    'description' => '',
  ),
  'xsl_xsltprocessor_register_php_functions' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => '',
  ),
  'xsl_xsltprocessor_has_exslt_support' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => '',
  ),
  'libxml_set_streams_context' => 
  array (
    'return' => 'void',
    'params' => 'resource streams_context',
    'description' => 'Set the streams context for the next libxml document load or write',
  ),
  'libxml_use_internal_errors' => 
  array (
    'return' => 'void',
    'params' => 'boolean use_errors',
    'description' => 'Disable libxml errors and allow user to fetch error information as needed',
  ),
  'libxml_get_last_error' => 
  array (
    'return' => 'object',
    'params' => '',
    'description' => 'Retrieve last error from libxml',
  ),
  'libxml_get_errors' => 
  array (
    'return' => 'object',
    'params' => '',
    'description' => 'Retrieve array of errors',
  ),
  'libxml_clear_errors' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Clear last error from libxml',
  ),
  'mssql_connect' => 
  array (
    'return' => 'int',
    'params' => '[string servername [, string username [, string password [, bool new_link]]]',
    'description' => 'Establishes a connection to a MS-SQL server',
  ),
  'mssql_pconnect' => 
  array (
    'return' => 'int',
    'params' => '[string servername [, string username [, string password [, bool new_link]]]]',
    'description' => 'Establishes a persistent connection to a MS-SQL server',
  ),
  'mssql_close' => 
  array (
    'return' => 'bool',
    'params' => '[resource conn_id]',
    'description' => 'Closes a connection to a MS-SQL server',
  ),
  'mssql_select_db' => 
  array (
    'return' => 'bool',
    'params' => 'string database_name [, resource conn_id]',
    'description' => 'Select a MS-SQL database',
  ),
  'mssql_fetch_batch' => 
  array (
    'return' => 'int',
    'params' => 'resource result_index',
    'description' => 'Returns the next batch of records',
  ),
  'mssql_query' => 
  array (
    'return' => 'resource',
    'params' => 'string query [, resource conn_id [, int batch_size]]',
    'description' => 'Perform an SQL query on a MS-SQL server database',
  ),
  'mssql_rows_affected' => 
  array (
    'return' => 'int',
    'params' => 'resource conn_id',
    'description' => 'Returns the number of records affected by the query',
  ),
  'mssql_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_index',
    'description' => 'Free a MS-SQL result index',
  ),
  'mssql_get_last_message' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Gets the last message from the MS-SQL server',
  ),
  'mssql_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource mssql_result_index',
    'description' => 'Returns the number of rows fetched in from the result id specified',
  ),
  'mssql_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource mssql_result_index',
    'description' => 'Returns the number of fields fetched in from the result id specified',
  ),
  'mssql_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'resource result_id',
    'description' => 'Returns an array of the current row in the result set specified by result_id',
  ),
  'mssql_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource result_id [, int result_type]',
    'description' => 'Returns a psuedo-object of the current row in the result set specified by result_id',
  ),
  'mssql_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'resource result_id [, int result_type]',
    'description' => 'Returns an associative array of the current row in the result set specified by result_id',
  ),
  'mssql_fetch_assoc' => 
  array (
    'return' => 'array',
    'params' => 'resource result_id',
    'description' => 'Returns an associative array of the current row in the result set specified by result_id',
  ),
  'mssql_data_seek' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id, int offset',
    'description' => 'Moves the internal row pointer of the MS-SQL result associated with the specified result identifier to pointer to the specified row number',
  ),
  'mssql_fetch_field' => 
  array (
    'return' => 'object',
    'params' => 'resource result_id [, int offset]',
    'description' => 'Gets information about certain fields in a query result',
  ),
  'mssql_field_length' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id [, int offset]',
    'description' => 'Get the length of a MS-SQL field',
  ),
  'mssql_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource result_id [, int offset]',
    'description' => 'Returns the name of the field given by offset in the result set given by result_id',
  ),
  'mssql_field_type' => 
  array (
    'return' => 'string',
    'params' => 'resource result_id [, int offset]',
    'description' => 'Returns the type of a field',
  ),
  'mssql_field_seek' => 
  array (
    'return' => 'bool',
    'params' => 'int result_id, int offset',
    'description' => 'Seeks to the specified field offset',
  ),
  'mssql_result' => 
  array (
    'return' => 'string',
    'params' => 'resource result_id, int row, mixed field',
    'description' => 'Returns the contents of one cell from a MS-SQL result set',
  ),
  'mssql_next_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id',
    'description' => 'Move the internal result pointer to the next result',
  ),
  'mssql_min_error_severity' => 
  array (
    'return' => 'void',
    'params' => 'int severity',
    'description' => 'Sets the lower error severity',
  ),
  'mssql_min_message_severity' => 
  array (
    'return' => 'void',
    'params' => 'int severity',
    'description' => 'Sets the lower message severity',
  ),
  'mssql_init' => 
  array (
    'return' => 'int',
    'params' => 'string sp_name [, resource conn_id]',
    'description' => 'Initializes a stored procedure or a remote stored procedure',
  ),
  'mssql_bind' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt, string param_name, mixed var, int type [, int is_output [, int is_null [, int maxlen]]]',
    'description' => 'Adds a parameter to a stored procedure or a remote stored procedure',
  ),
  'mssql_execute' => 
  array (
    'return' => 'mixed',
    'params' => 'resource stmt [, bool skip_results = false]',
    'description' => 'Executes a stored procedure on a MS-SQL server database',
  ),
  'mssql_free_statement' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_index',
    'description' => 'Free a MS-SQL statement index',
  ),
  'mssql_guid_string' => 
  array (
    'return' => 'string',
    'params' => 'string binary [,int short_format]',
    'description' => 'Converts a 16 byte binary GUID to a string',
  ),
  'oci_define_by_name' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt, string name, mixed &var [, int type]',
    'description' => 'Define a PHP variable to an Oracle column by name',
  ),
  'oci_bind_by_name' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt, string name, mixed &var, [, int maxlength [, int type]]',
    'description' => 'Bind a PHP variable to an Oracle placeholder by name',
  ),
  'oci_bind_array_by_name' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt, string name, array &var, int max_table_length [, int max_item_length [, int type ]]',
    'description' => 'Bind a PHP array to an Oracle PL/SQL type by name',
  ),
  'oci_free_descriptor' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Deletes large object description',
  ),
  'oci_lob_save' => 
  array (
    'return' => 'bool',
    'params' => ' string data [, int offset ]',
    'description' => 'Saves a large object',
  ),
  'oci_lob_import' => 
  array (
    'return' => 'bool',
    'params' => ' string filename ',
    'description' => 'Loads file into a LOB',
  ),
  'oci_lob_load' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Loads a large object',
  ),
  'oci_lob_read' => 
  array (
    'return' => 'string',
    'params' => ' int length ',
    'description' => 'Reads particular part of a large object',
  ),
  'oci_lob_eof' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Checks if EOF is reached',
  ),
  'oci_lob_tell' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Tells LOB pointer position',
  ),
  'oci_lob_rewind' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Rewind pointer of a LOB',
  ),
  'oci_lob_seek' => 
  array (
    'return' => 'bool',
    'params' => ' int offset [, int whence ]',
    'description' => 'Moves the pointer of a LOB',
  ),
  'oci_lob_size' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns size of a large object',
  ),
  'oci_lob_write' => 
  array (
    'return' => 'int',
    'params' => ' string string [, int length ]',
    'description' => 'Writes data to current position of a LOB',
  ),
  'oci_lob_append' => 
  array (
    'return' => 'bool',
    'params' => ' object lob ',
    'description' => 'Appends data from a LOB to another LOB',
  ),
  'oci_lob_truncate' => 
  array (
    'return' => 'bool',
    'params' => ' [ int length ]',
    'description' => 'Truncates a LOB',
  ),
  'oci_lob_erase' => 
  array (
    'return' => 'int',
    'params' => ' [ int offset [, int length ] ] ',
    'description' => 'Erases a specified portion of the internal LOB, starting at a specified offset',
  ),
  'oci_lob_flush' => 
  array (
    'return' => 'bool',
    'params' => ' [ int flag ] ',
    'description' => 'Flushes the LOB buffer',
  ),
  'ocisetbufferinglob' => 
  array (
    'return' => 'bool',
    'params' => ' boolean flag ',
    'description' => 'Enables/disables buffering for a LOB',
  ),
  'ocigetbufferinglob' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns current state of buffering for a LOB',
  ),
  'oci_lob_copy' => 
  array (
    'return' => 'bool',
    'params' => ' object lob_to, object lob_from [, int length ] ',
    'description' => 'Copies data from a LOB to another LOB',
  ),
  'oci_lob_is_equal' => 
  array (
    'return' => 'bool',
    'params' => ' object lob1, object lob2 ',
    'description' => 'Tests to see if two LOB/FILE locators are equal',
  ),
  'oci_lob_export' => 
  array (
    'return' => 'bool',
    'params' => '[string filename [, int start [, int length]]]',
    'description' => 'Writes a large object into a file',
  ),
  'oci_lob_write_temporary' => 
  array (
    'return' => 'bool',
    'params' => 'string var [, int lob_type]',
    'description' => 'Writes temporary blob',
  ),
  'oci_lob_close' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Closes lob descriptor',
  ),
  'oci_new_descriptor' => 
  array (
    'return' => 'object',
    'params' => 'resource connection [, int type]',
    'description' => 'Initialize a new empty descriptor LOB/FILE (LOB is default)',
  ),
  'oci_rollback' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection',
    'description' => 'Rollback the current context',
  ),
  'oci_commit' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection',
    'description' => 'Commit the current context',
  ),
  'oci_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource stmt, int col',
    'description' => 'Tell the name of a column',
  ),
  'oci_field_size' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt, int col',
    'description' => 'Tell the maximum data size of a column',
  ),
  'oci_field_scale' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt, int col',
    'description' => 'Tell the scale of a column',
  ),
  'oci_field_precision' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt, int col',
    'description' => 'Tell the precision of a column',
  ),
  'oci_field_type' => 
  array (
    'return' => 'mixed',
    'params' => 'resource stmt, int col',
    'description' => 'Tell the data type of a column',
  ),
  'oci_field_type_raw' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt, int col',
    'description' => 'Tell the raw oracle data type of a column',
  ),
  'oci_field_is_null' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt, int col',
    'description' => 'Tell whether a column is NULL',
  ),
  'oci_internal_debug' => 
  array (
    'return' => 'void',
    'params' => 'int onoff',
    'description' => 'Toggle internal debugging output for the OCI extension',
  ),
  'oci_execute' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt [, int mode]',
    'description' => 'Execute a parsed statement',
  ),
  'oci_cancel' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt',
    'description' => 'Cancel reading from a cursor',
  ),
  'oci_fetch' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt',
    'description' => 'Prepare a new row of data for reading',
  ),
  'ocifetchinto' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt, array &output [, int mode]',
    'description' => 'Fetch a row of result data into an array',
  ),
  'oci_fetch_all' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt, array &output[, int skip[, int maxrows[, int flags]]]',
    'description' => 'Fetch all rows of result data into an array',
  ),
  'oci_fetch_object' => 
  array (
    'return' => 'object',
    'params' => ' resource stmt ',
    'description' => 'Fetch a result row as an object',
  ),
  'oci_fetch_row' => 
  array (
    'return' => 'array',
    'params' => ' resource stmt ',
    'description' => 'Fetch a result row as an enumerated array',
  ),
  'oci_fetch_assoc' => 
  array (
    'return' => 'array',
    'params' => ' resource stmt ',
    'description' => 'Fetch a result row as an associative array',
  ),
  'oci_fetch_array' => 
  array (
    'return' => 'array',
    'params' => ' resource stmt [, int mode ]',
    'description' => 'Fetch a result row as an array',
  ),
  'oci_free_statement' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt',
    'description' => 'Free all resources associated with a statement',
  ),
  'oci_close' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection',
    'description' => 'Disconnect from database',
  ),
  'oci_new_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string user, string pass [, string db]',
    'description' => 'Connect to an Oracle database and log on. Returns a new session.',
  ),
  'oci_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string user, string pass [, string db [, string charset [, int session_mode ]]',
    'description' => 'Connect to an Oracle database and log on. Returns a new session.',
  ),
  'oci_pconnect' => 
  array (
    'return' => 'resource',
    'params' => 'string user, string pass [, string db [, string charset ]]',
    'description' => 'Connect to an Oracle database using a persistent connection and log on. Returns a new session.',
  ),
  'oci_error' => 
  array (
    'return' => 'array',
    'params' => '[resource stmt|connection|global]',
    'description' => 'Return the last error of stmt|connection|global. If no error happened returns false.',
  ),
  'oci_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt',
    'description' => 'Return the number of result columns in a statement',
  ),
  'oci_parse' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection, string query',
    'description' => 'Parse a query and return a statement',
  ),
  'oci_set_prefetch' => 
  array (
    'return' => 'bool',
    'params' => 'resource stmt, int prefetch_rows',
    'description' => 'Sets the number of rows to be prefetched on execute to prefetch_rows for stmt',
  ),
  'oci_password_change' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection, string username, string old_password, string new_password',
    'description' => 'Changes the password of an account',
  ),
  'oci_new_cursor' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection',
    'description' => 'Return a new cursor (Statement-Handle) - use this to bind ref-cursors!',
  ),
  'oci_result' => 
  array (
    'return' => 'string',
    'params' => 'resource stmt, mixed column',
    'description' => 'Return a single column of result data',
  ),
  'oci_server_version' => 
  array (
    'return' => 'string',
    'params' => 'resource connection',
    'description' => 'Return a string containing server version information',
  ),
  'oci_statement_type' => 
  array (
    'return' => 'string',
    'params' => 'resource stmt',
    'description' => 'Return the query type of an OCI statement',
  ),
  'oci_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource stmt',
    'description' => 'Return the row count of an OCI statement',
  ),
  'oci_free_collection' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Deletes collection object',
  ),
  'oci_collection_append' => 
  array (
    'return' => 'bool',
    'params' => 'string value',
    'description' => 'Append an object to the collection',
  ),
  'oci_collection_element_get' => 
  array (
    'return' => 'string',
    'params' => 'int ndx',
    'description' => 'Retrieve the value at collection index ndx',
  ),
  'oci_collection_assign' => 
  array (
    'return' => 'bool',
    'params' => 'object from',
    'description' => 'Assign a collection from another existing collection',
  ),
  'oci_collection_element_assign' => 
  array (
    'return' => 'bool',
    'params' => 'int index, string val',
    'description' => 'Assign element val to collection at index ndx',
  ),
  'oci_collection_size' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Return the size of a collection',
  ),
  'oci_collection_max' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Return the max value of a collection. For a varray this is the maximum length of the array',
  ),
  'oci_collection_trim' => 
  array (
    'return' => 'bool',
    'params' => 'int num',
    'description' => 'Trim num elements from the end of a collection',
  ),
  'oci_new_collection' => 
  array (
    'return' => 'object',
    'params' => 'resource connection, string tdo [, string schema]',
    'description' => 'Initialize a new collection',
  ),
  'mb_language' => 
  array (
    'return' => 'string',
    'params' => '[string language]',
    'description' => 'Sets the current language or Returns the current language as a string',
  ),
  'mb_internal_encoding' => 
  array (
    'return' => 'string',
    'params' => '[string encoding]',
    'description' => 'Sets the current internal encoding or Returns the current internal encoding as a string',
  ),
  'mb_http_input' => 
  array (
    'return' => 'mixed',
    'params' => '[string type]',
    'description' => 'Returns the input encoding',
  ),
  'mb_http_output' => 
  array (
    'return' => 'string',
    'params' => '[string encoding]',
    'description' => 'Sets the current output_encoding or returns the current output_encoding as a string',
  ),
  'mb_detect_order' => 
  array (
    'return' => 'bool|array',
    'params' => '[mixed encoding-list]',
    'description' => 'Sets the current detect_order or Return the current detect_order as a array',
  ),
  'mb_substitute_character' => 
  array (
    'return' => 'mixed',
    'params' => '[mixed substchar]',
    'description' => 'Sets the current substitute_character or returns the current substitute_character',
  ),
  'mb_preferred_mime_name' => 
  array (
    'return' => 'string',
    'params' => 'string encoding',
    'description' => 'Return the preferred MIME name (charset) as a string',
  ),
  'mb_parse_str' => 
  array (
    'return' => 'bool',
    'params' => 'string encoded_string [, array result]',
    'description' => 'Parses GET/POST/COOKIE data and sets global variables',
  ),
  'mb_output_handler' => 
  array (
    'return' => 'string',
    'params' => 'string contents, int status',
    'description' => 'Returns string in output buffer converted to the http_output encoding',
  ),
  'mb_strlen' => 
  array (
    'return' => 'int',
    'params' => 'string str [, string encoding]',
    'description' => 'Get character numbers of a string',
  ),
  'mb_strpos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, int offset [, string encoding]]',
    'description' => 'Find position of first occurrence of a string within another',
  ),
  'mb_strrpos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, string encoding]',
    'description' => 'Find the last occurrence of a character in a string within another',
  ),
  'mb_substr_count' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, string encoding]',
    'description' => 'Count the number of substring occurrences',
  ),
  'mb_substr' => 
  array (
    'return' => 'string',
    'params' => 'string str, int start [, int length [, string encoding]]',
    'description' => 'Returns part of a string',
  ),
  'mb_strcut' => 
  array (
    'return' => 'string',
    'params' => 'string str, int start [, int length [, string encoding]]',
    'description' => 'Returns part of a string',
  ),
  'mb_strwidth' => 
  array (
    'return' => 'int',
    'params' => 'string str [, string encoding]',
    'description' => 'Gets terminal width of a string',
  ),
  'mb_strimwidth' => 
  array (
    'return' => 'string',
    'params' => 'string str, int start, int width [, string trimmarker [, string encoding]]',
    'description' => 'Trim the string in terminal width',
  ),
  'mb_convert_encoding' => 
  array (
    'return' => 'string',
    'params' => 'string str, string to-encoding [, mixed from-encoding]',
    'description' => 'Returns converted string in desired encoding',
  ),
  'mb_convert_case' => 
  array (
    'return' => 'string',
    'params' => 'string sourcestring, int mode [, string encoding]',
    'description' => 'Returns a case-folded version of sourcestring',
  ),
  'mb_strtoupper' => 
  array (
    'return' => 'string',
    'params' => 'string sourcestring [, string encoding]',
    'description' => '*  Returns a uppercased version of sourcestring',
  ),
  'mb_strtolower' => 
  array (
    'return' => 'string',
    'params' => 'string sourcestring [, string encoding]',
    'description' => '*  Returns a lowercased version of sourcestring',
  ),
  'mb_detect_encoding' => 
  array (
    'return' => 'string',
    'params' => 'string str [, mixed encoding_list [, bool strict]]',
    'description' => 'Encodings of the given string is returned (as a string)',
  ),
  'mb_list_encodings' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Returns an array of all supported encodings',
  ),
  'mb_encode_mimeheader' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string charset [, string transfer-encoding [, string linefeed [, int indent]]]]',
    'description' => 'Converts the string to MIME "encoded-word" in the format of =?charset?(B|Q)?encoded_string?=',
  ),
  'mb_decode_mimeheader' => 
  array (
    'return' => 'string',
    'params' => 'string string',
    'description' => 'Decodes the MIME "encoded-word" in the string',
  ),
  'mb_convert_kana' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string option] [, string encoding]',
    'description' => 'Conversion between full-width character and half-width character (Japanese)',
  ),
  'mb_convert_variables' => 
  array (
    'return' => 'string',
    'params' => 'string to-encoding, mixed from-encoding [, mixed ...]',
    'description' => 'Converts the string resource in variables to desired encoding',
  ),
  'mb_encode_numericentity' => 
  array (
    'return' => 'string',
    'params' => 'string string, array convmap [, string encoding]',
    'description' => 'Converts specified characters to HTML numeric entities',
  ),
  'mb_decode_numericentity' => 
  array (
    'return' => 'string',
    'params' => 'string string, array convmap [, string encoding]',
    'description' => 'Converts HTML numeric entities to character code',
  ),
  'mb_send_mail' => 
  array (
    'return' => 'int',
    'params' => 'string to, string subject, string message [, string additional_headers [, string additional_parameters]]',
    'description' => '*  Sends an email message with MIME scheme',
  ),
  'mb_get_info' => 
  array (
    'return' => 'mixed',
    'params' => '[string type]',
    'description' => 'Returns the current settings of mbstring',
  ),
  'mb_check_encoding' => 
  array (
    'return' => 'bool',
    'params' => '[string var[, string encoding]]',
    'description' => 'Check if the string is valid for the specified encoding',
  ),
  'mb_regex_encoding' => 
  array (
    'return' => 'string',
    'params' => '[string encoding]',
    'description' => 'Returns the current encoding for regex as a string.',
  ),
  'mb_ereg' => 
  array (
    'return' => 'int',
    'params' => 'string pattern, string string [, array registers]',
    'description' => 'Regular expression match for multibyte string',
  ),
  'mb_eregi' => 
  array (
    'return' => 'int',
    'params' => 'string pattern, string string [, array registers]',
    'description' => 'Case-insensitive regular expression match for multibyte string',
  ),
  'mb_ereg_replace' => 
  array (
    'return' => 'string',
    'params' => 'string pattern, string replacement, string string [, string option]',
    'description' => 'Replace regular expression for multibyte string',
  ),
  'mb_eregi_replace' => 
  array (
    'return' => 'string',
    'params' => 'string pattern, string replacement, string string',
    'description' => 'Case insensitive replace regular expression for multibyte string',
  ),
  'mb_split' => 
  array (
    'return' => 'array',
    'params' => 'string pattern, string string [, int limit]',
    'description' => 'split multibyte string into array by regular expression',
  ),
  'mb_ereg_match' => 
  array (
    'return' => 'bool',
    'params' => 'string pattern, string string [,string option]',
    'description' => 'Regular expression match for multibyte string',
  ),
  'mb_ereg_search' => 
  array (
    'return' => 'bool',
    'params' => '[string pattern[, string option]]',
    'description' => 'Regular expression search for multibyte string',
  ),
  'mb_ereg_search_pos' => 
  array (
    'return' => 'array',
    'params' => '[string pattern[, string option]]',
    'description' => 'Regular expression search for multibyte string',
  ),
  'mb_ereg_search_regs' => 
  array (
    'return' => 'array',
    'params' => '[string pattern[, string option]]',
    'description' => 'Regular expression search for multibyte string',
  ),
  'mb_ereg_search_init' => 
  array (
    'return' => 'bool',
    'params' => 'string string [, string pattern[, string option]]',
    'description' => 'Initialize string and regular expression for search.',
  ),
  'mb_ereg_search_getregs' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get matched substring of the last time',
  ),
  'mb_ereg_search_getpos' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get search start position',
  ),
  'mb_ereg_search_setpos' => 
  array (
    'return' => 'bool',
    'params' => 'int position',
    'description' => 'Set search start position',
  ),
  'mb_regex_set_options' => 
  array (
    'return' => 'string',
    'params' => '[string options]',
    'description' => 'Set or get the default options for mbregex functions',
  ),
  'preg_match' => 
  array (
    'return' => 'int',
    'params' => 'string pattern, string subject [, array subpatterns [, int flags [, int offset]]]',
    'description' => 'Perform a Perl-style regular expression match',
  ),
  'preg_match_all' => 
  array (
    'return' => 'int',
    'params' => 'string pattern, string subject, array subpatterns [, int flags [, int offset]]',
    'description' => 'Perform a Perl-style global regular expression match',
  ),
  'preg_replace' => 
  array (
    'return' => 'string',
    'params' => 'mixed regex, mixed replace, mixed subject [, int limit [, count]]',
    'description' => 'Perform Perl-style regular expression replacement.',
  ),
  'preg_replace_callback' => 
  array (
    'return' => 'string',
    'params' => 'mixed regex, mixed callback, mixed subject [, int limit [, count]]',
    'description' => 'Perform Perl-style regular expression replacement using replacement callback.',
  ),
  'preg_split' => 
  array (
    'return' => 'array',
    'params' => 'string pattern, string subject [, int limit [, int flags]]',
    'description' => 'Split string into an array using a perl-style regular expression as a delimiter',
  ),
  'preg_quote' => 
  array (
    'return' => 'string',
    'params' => 'string str, string delim_char',
    'description' => 'Quote regular expression characters plus an optional character',
  ),
  'preg_grep' => 
  array (
    'return' => 'array',
    'params' => 'string regex, array input',
    'description' => 'Searches array and returns entries which match regex',
  ),
  'pcntl_fork' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Forks the currently running process following the same behavior as the UNIX fork() system call',
  ),
  'pcntl_alarm' => 
  array (
    'return' => 'int',
    'params' => 'int seconds',
    'description' => 'Set an alarm clock for delivery of a signal',
  ),
  'pcntl_waitpid' => 
  array (
    'return' => 'int',
    'params' => 'int pid, int &status, int options',
    'description' => 'Waits on or returns the status of a forked child as defined by the waitpid() system call',
  ),
  'pcntl_wait' => 
  array (
    'return' => 'int',
    'params' => 'int &status',
    'description' => 'Waits on or returns the status of a forked child as defined by the waitpid() system call',
  ),
  'pcntl_wifexited' => 
  array (
    'return' => 'bool',
    'params' => 'int status',
    'description' => 'Returns true if the child status code represents a successful exit',
  ),
  'pcntl_wifstopped' => 
  array (
    'return' => 'bool',
    'params' => 'int status',
    'description' => 'Returns true if the child status code represents a stopped process (WUNTRACED must have been used with waitpid)',
  ),
  'pcntl_wifsignaled' => 
  array (
    'return' => 'bool',
    'params' => 'int status',
    'description' => 'Returns true if the child status code represents a process that was terminated due to a signal',
  ),
  'pcntl_wexitstatus' => 
  array (
    'return' => 'int',
    'params' => 'int status',
    'description' => 'Returns the status code of a child\'s exit',
  ),
  'pcntl_wtermsig' => 
  array (
    'return' => 'int',
    'params' => 'int status',
    'description' => 'Returns the number of the signal that terminated the process who\'s status code is passed',
  ),
  'pcntl_wstopsig' => 
  array (
    'return' => 'int',
    'params' => 'int status',
    'description' => 'Returns the number of the signal that caused the process to stop who\'s status code is passed',
  ),
  'pcntl_exec' => 
  array (
    'return' => 'bool',
    'params' => 'string path [, array args [, array envs]]',
    'description' => 'Executes specified program in current process space as defined by exec(2)',
  ),
  'pcntl_signal' => 
  array (
    'return' => 'bool',
    'params' => 'int signo, callback handle [, bool restart_syscalls]',
    'description' => 'Assigns a system signal handler to a PHP function',
  ),
  'pcntl_getpriority' => 
  array (
    'return' => 'int',
    'params' => '[int pid [, int process_identifier]]',
    'description' => 'Get the priority of any process',
  ),
  'pcntl_setpriority' => 
  array (
    'return' => 'bool',
    'params' => 'int priority [, int pid [, int process_identifier]]',
    'description' => 'Change the priority of any process',
  ),
  'mcrypt_module_open' => 
  array (
    'return' => 'resource',
    'params' => 'string cipher, string cipher_directory, string mode, string mode_directory',
    'description' => 'Opens the module of the algorithm and the mode to be used',
  ),
  'mcrypt_generic_init' => 
  array (
    'return' => 'int',
    'params' => 'resource td, string key, string iv',
    'description' => 'This function initializes all buffers for the specific module',
  ),
  'mcrypt_generic' => 
  array (
    'return' => 'string',
    'params' => 'resource td, string data',
    'description' => 'This function encrypts the plaintext',
  ),
  'mdecrypt_generic' => 
  array (
    'return' => 'string',
    'params' => 'resource td, string data',
    'description' => 'This function decrypts the plaintext',
  ),
  'mcrypt_enc_get_supported_key_sizes' => 
  array (
    'return' => 'array',
    'params' => 'resource td',
    'description' => 'This function decrypts the crypttext',
  ),
  'mcrypt_enc_self_test' => 
  array (
    'return' => 'int',
    'params' => 'resource td',
    'description' => 'This function runs the self test on the algorithm specified by the descriptor td',
  ),
  'mcrypt_module_close' => 
  array (
    'return' => 'bool',
    'params' => 'resource td',
    'description' => 'Free the descriptor td',
  ),
  'mcrypt_generic_end' => 
  array (
    'return' => 'bool',
    'params' => 'resource td',
    'description' => 'This function terminates encrypt specified by the descriptor td',
  ),
  'mcrypt_generic_deinit' => 
  array (
    'return' => 'bool',
    'params' => 'resource td',
    'description' => 'This function terminates encrypt specified by the descriptor td',
  ),
  'mcrypt_enc_is_block_algorithm_mode' => 
  array (
    'return' => 'bool',
    'params' => 'resource td',
    'description' => 'Returns TRUE if the mode is for use with block algorithms',
  ),
  'mcrypt_enc_is_block_algorithm' => 
  array (
    'return' => 'bool',
    'params' => 'resource td',
    'description' => 'Returns TRUE if the alrogithm is a block algorithms',
  ),
  'mcrypt_enc_is_block_mode' => 
  array (
    'return' => 'bool',
    'params' => 'resource td',
    'description' => 'Returns TRUE if the mode outputs blocks',
  ),
  'mcrypt_enc_get_block_size' => 
  array (
    'return' => 'int',
    'params' => 'resource td',
    'description' => 'Returns the block size of the cipher specified by the descriptor td',
  ),
  'mcrypt_enc_get_key_size' => 
  array (
    'return' => 'int',
    'params' => 'resource td',
    'description' => 'Returns the maximum supported key size in bytes of the algorithm specified by the descriptor td',
  ),
  'mcrypt_enc_get_iv_size' => 
  array (
    'return' => 'int',
    'params' => 'resource td',
    'description' => 'Returns the size of the IV in bytes of the algorithm specified by the descriptor td',
  ),
  'mcrypt_enc_get_algorithms_name' => 
  array (
    'return' => 'string',
    'params' => 'resource td',
    'description' => 'Returns the name of the algorithm specified by the descriptor td',
  ),
  'mcrypt_enc_get_modes_name' => 
  array (
    'return' => 'string',
    'params' => 'resource td',
    'description' => 'Returns the name of the mode specified by the descriptor td',
  ),
  'mcrypt_module_self_test' => 
  array (
    'return' => 'bool',
    'params' => 'string algorithm [, string lib_dir]',
    'description' => 'Does a self test of the module "module"',
  ),
  'mcrypt_module_is_block_algorithm_mode' => 
  array (
    'return' => 'bool',
    'params' => 'string mode [, string lib_dir]',
    'description' => 'Returns TRUE if the mode is for use with block algorithms',
  ),
  'mcrypt_module_is_block_algorithm' => 
  array (
    'return' => 'bool',
    'params' => 'string algorithm [, string lib_dir]',
    'description' => 'Returns TRUE if the algorithm is a block algorithm',
  ),
  'mcrypt_module_is_block_mode' => 
  array (
    'return' => 'bool',
    'params' => 'string mode [, string lib_dir]',
    'description' => 'Returns TRUE if the mode outputs blocks of bytes',
  ),
  'mcrypt_module_get_algo_block_size' => 
  array (
    'return' => 'int',
    'params' => 'string algorithm [, string lib_dir]',
    'description' => 'Returns the block size of the algorithm',
  ),
  'mcrypt_module_get_algo_key_size' => 
  array (
    'return' => 'int',
    'params' => 'string algorithm [, string lib_dir]',
    'description' => 'Returns the maximum supported key size of the algorithm',
  ),
  'mcrypt_module_get_supported_key_sizes' => 
  array (
    'return' => 'array',
    'params' => 'string algorithm [, string lib_dir]',
    'description' => 'This function decrypts the crypttext',
  ),
  'mcrypt_list_algorithms' => 
  array (
    'return' => 'array',
    'params' => '[string lib_dir]',
    'description' => 'List all algorithms in "module_dir"',
  ),
  'mcrypt_list_modes' => 
  array (
    'return' => 'array',
    'params' => '[string lib_dir]',
    'description' => 'List all modes "module_dir"',
  ),
  'mcrypt_get_key_size' => 
  array (
    'return' => 'int',
    'params' => 'string cipher, string module',
    'description' => 'Get the key size of cipher',
  ),
  'mcrypt_get_block_size' => 
  array (
    'return' => 'int',
    'params' => 'string cipher, string module',
    'description' => 'Get the key size of cipher',
  ),
  'mcrypt_get_iv_size' => 
  array (
    'return' => 'int',
    'params' => 'string cipher, string module',
    'description' => 'Get the IV size of cipher (Usually the same as the blocksize)',
  ),
  'mcrypt_get_cipher_name' => 
  array (
    'return' => 'string',
    'params' => 'string cipher',
    'description' => 'Get the key size of cipher',
  ),
  'mcrypt_encrypt' => 
  array (
    'return' => 'string',
    'params' => 'string cipher, string key, string data, string mode, string iv',
    'description' => 'OFB crypt/decrypt data using key key with cipher cipher starting with iv',
  ),
  'mcrypt_decrypt' => 
  array (
    'return' => 'string',
    'params' => 'string cipher, string key, string data, string mode, string iv',
    'description' => 'OFB crypt/decrypt data using key key with cipher cipher starting with iv',
  ),
  'mcrypt_ecb' => 
  array (
    'return' => 'string',
    'params' => 'int cipher, string key, string data, int mode, string iv',
    'description' => 'ECB crypt/decrypt data using key key with cipher cipher starting with iv',
  ),
  'mcrypt_cbc' => 
  array (
    'return' => 'string',
    'params' => 'int cipher, string key, string data, int mode, string iv',
    'description' => 'CBC crypt/decrypt data using key key with cipher cipher starting with iv',
  ),
  'mcrypt_cfb' => 
  array (
    'return' => 'string',
    'params' => 'int cipher, string key, string data, int mode, string iv',
    'description' => 'CFB crypt/decrypt data using key key with cipher cipher starting with iv',
  ),
  'mcrypt_ofb' => 
  array (
    'return' => 'string',
    'params' => 'int cipher, string key, string data, int mode, string iv',
    'description' => 'OFB crypt/decrypt data using key key with cipher cipher starting with iv',
  ),
  'mcrypt_create_iv' => 
  array (
    'return' => 'string',
    'params' => 'int size, int source',
    'description' => 'Create an initialization vector (IV)',
  ),
  'readline' => 
  array (
    'return' => 'string',
    'params' => '[string prompt]',
    'description' => 'Reads a line',
  ),
  'readline_info' => 
  array (
    'return' => 'mixed',
    'params' => '[string varname] [, string newvalue]',
    'description' => 'Gets/sets various internal readline variables.',
  ),
  'readline_add_history' => 
  array (
    'return' => 'bool',
    'params' => '[string prompt]',
    'description' => 'Adds a line to the history',
  ),
  'readline_clear_history' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Clears the history',
  ),
  'readline_list_history' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Lists the history',
  ),
  'readline_read_history' => 
  array (
    'return' => 'bool',
    'params' => '[string filename] [, int from] [,int to]',
    'description' => 'Reads the history',
  ),
  'readline_write_history' => 
  array (
    'return' => 'bool',
    'params' => '[string filename]',
    'description' => 'Writes the history',
  ),
  'readline_completion_function' => 
  array (
    'return' => 'bool',
    'params' => 'string funcname',
    'description' => 'Readline completion function?',
  ),
  'readline_callback_handler_install' => 
  array (
    'return' => 'void',
    'params' => 'string prompt, mixed callback',
    'description' => 'Initializes the readline callback interface and terminal, prints the prompt and returns immediately',
  ),
  'readline_callback_read_char' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Informs the readline callback interface that a character is ready for input',
  ),
  'readline_callback_handler_remove' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Removes a previously installed callback handler and restores terminal settings',
  ),
  'readline_redisplay' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Ask readline to redraw the display',
  ),
  'readline_on_new_line' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Inform readline that the cursor has moved to a new line',
  ),
  'dbase_open' => 
  array (
    'return' => 'int',
    'params' => 'string name, int mode',
    'description' => 'Opens a dBase-format database file',
  ),
  'dbase_close' => 
  array (
    'return' => 'bool',
    'params' => 'int identifier',
    'description' => 'Closes an open dBase-format database file',
  ),
  'dbase_numrecords' => 
  array (
    'return' => 'int',
    'params' => 'int identifier',
    'description' => 'Returns the number of records in the database',
  ),
  'dbase_numfields' => 
  array (
    'return' => 'int',
    'params' => 'int identifier',
    'description' => 'Returns the number of fields (columns) in the database',
  ),
  'dbase_pack' => 
  array (
    'return' => 'bool',
    'params' => 'int identifier',
    'description' => 'Packs the database (deletes records marked for deletion)',
  ),
  'dbase_add_record' => 
  array (
    'return' => 'bool',
    'params' => 'int identifier, array data',
    'description' => 'Adds a record to the database',
  ),
  'dbase_replace_record' => 
  array (
    'return' => 'bool',
    'params' => 'int identifier, array data, int recnum',
    'description' => 'Replaces a record to the database',
  ),
  'dbase_delete_record' => 
  array (
    'return' => 'bool',
    'params' => 'int identifier, int record',
    'description' => 'Marks a record to be deleted',
  ),
  'dbase_get_record' => 
  array (
    'return' => 'array',
    'params' => 'int identifier, int record',
    'description' => 'Returns an array representing a record from the database',
  ),
  'dbase_get_record_with_names' => 
  array (
    'return' => 'array',
    'params' => 'int identifier, int record',
    'description' => 'Returns an associative array representing a record from the database',
  ),
  'dbase_create' => 
  array (
    'return' => 'bool',
    'params' => 'string filename, array fields',
    'description' => 'Creates a new dBase-format database file',
  ),
  'dbase_get_header_info' => 
  array (
    'return' => 'array',
    'params' => 'int database_handle',
    'description' => '',
  ),
  'ibase_add_user' => 
  array (
    'return' => 'bool',
    'params' => 'resource service_handle, string user_name, string password [, string first_name [, string middle_name [, string last_name]]]',
    'description' => 'Add a user to security database',
  ),
  'ibase_modify_user' => 
  array (
    'return' => 'bool',
    'params' => 'resource service_handle, string user_name, string password [, string first_name [, string middle_name [, string last_name]]]',
    'description' => 'Modify a user in security database',
  ),
  'ibase_delete_user' => 
  array (
    'return' => 'bool',
    'params' => 'resource service_handle, string user_name, string password [, string first_name [, string middle_name [, string last_name]]]',
    'description' => 'Delete a user from security database',
  ),
  'ibase_service_attach' => 
  array (
    'return' => 'resource',
    'params' => 'string host, string dba_username, string dba_password',
    'description' => 'Connect to the service manager',
  ),
  'ibase_service_detach' => 
  array (
    'return' => 'bool',
    'params' => 'resource service_handle',
    'description' => 'Disconnect from the service manager',
  ),
  'ibase_backup' => 
  array (
    'return' => 'mixed',
    'params' => 'resource service_handle, string source_db, string dest_file [, int options [, bool verbose]]',
    'description' => 'Initiates a backup task in the service manager and returns immediately',
  ),
  'ibase_restore' => 
  array (
    'return' => 'mixed',
    'params' => 'resource service_handle, string source_file, string dest_db [, int options [, bool verbose]]',
    'description' => 'Initiates a restore task in the service manager and returns immediately',
  ),
  'ibase_maintain_db' => 
  array (
    'return' => 'bool',
    'params' => 'resource service_handle, string db, int action [, int argument]',
    'description' => 'Execute a maintenance command on the database server',
  ),
  'ibase_db_info' => 
  array (
    'return' => 'string',
    'params' => 'resource service_handle, string db, int action [, int argument]',
    'description' => 'Request statistics about a database',
  ),
  'ibase_server_info' => 
  array (
    'return' => 'string',
    'params' => 'resource service_handle, int action',
    'description' => 'Request information about a database server',
  ),
  'ibase_errmsg' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return error message',
  ),
  'ibase_errcode' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Return error code',
  ),
  'ibase_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string database [, string username [, string password [, string charset [, int buffers [, int dialect [, string role]]]]]]',
    'description' => 'Open a connection to an InterBase database',
  ),
  'ibase_pconnect' => 
  array (
    'return' => 'resource',
    'params' => 'string database [, string username [, string password [, string charset [, int buffers [, int dialect [, string role]]]]]]',
    'description' => 'Open a persistent connection to an InterBase database',
  ),
  'ibase_close' => 
  array (
    'return' => 'bool',
    'params' => '[resource link_identifier]',
    'description' => 'Close an InterBase connection',
  ),
  'ibase_drop_db' => 
  array (
    'return' => 'bool',
    'params' => '[resource link_identifier]',
    'description' => 'Drop an InterBase database',
  ),
  'ibase_trans' => 
  array (
    'return' => 'resource',
    'params' => '[int trans_args [, resource link_identifier [, ... ], int trans_args [, resource link_identifier [, ... ]] [, ...]]]',
    'description' => 'Start a transaction over one or several databases',
  ),
  'ibase_commit' => 
  array (
    'return' => 'bool',
    'params' => ' resource link_identifier ',
    'description' => 'Commit transaction',
  ),
  'ibase_rollback' => 
  array (
    'return' => 'bool',
    'params' => ' resource link_identifier ',
    'description' => 'Rollback transaction',
  ),
  'ibase_commit_ret' => 
  array (
    'return' => 'bool',
    'params' => ' resource link_identifier ',
    'description' => 'Commit transaction and retain the transaction context',
  ),
  'ibase_rollback_ret' => 
  array (
    'return' => 'bool',
    'params' => ' resource link_identifier ',
    'description' => 'Rollback transaction and retain the transaction context',
  ),
  'ibase_gen_id' => 
  array (
    'return' => 'int',
    'params' => 'string generator [, int increment [, resource link_identifier ]]',
    'description' => 'Increments the named generator and returns its new value',
  ),
  'ibase_blob_create' => 
  array (
    'return' => 'resource',
    'params' => '[resource link_identifier]',
    'description' => 'Create blob for adding data',
  ),
  'ibase_blob_open' => 
  array (
    'return' => 'resource',
    'params' => '[ resource link_identifier, ] string blob_id',
    'description' => 'Open blob for retrieving data parts',
  ),
  'ibase_blob_add' => 
  array (
    'return' => 'bool',
    'params' => 'resource blob_handle, string data',
    'description' => 'Add data into created blob',
  ),
  'ibase_blob_get' => 
  array (
    'return' => 'string',
    'params' => 'resource blob_handle, int len',
    'description' => 'Get len bytes data from open blob',
  ),
  'ibase_blob_close' => 
  array (
    'return' => 'string',
    'params' => 'resource blob_handle',
    'description' => 'Close blob',
  ),
  'ibase_blob_cancel' => 
  array (
    'return' => 'bool',
    'params' => 'resource blob_handle',
    'description' => 'Cancel creating blob',
  ),
  'ibase_blob_info' => 
  array (
    'return' => 'array',
    'params' => '[ resource link_identifier, ] string blob_id',
    'description' => 'Return blob length and other useful info',
  ),
  'ibase_blob_echo' => 
  array (
    'return' => 'bool',
    'params' => '[ resource link_identifier, ] string blob_id',
    'description' => 'Output blob contents to browser',
  ),
  'ibase_blob_import' => 
  array (
    'return' => 'string',
    'params' => '[ resource link_identifier, ] resource file',
    'description' => 'Create blob, copy file in it, and close it',
  ),
  'ibase_query' => 
  array (
    'return' => 'mixed',
    'params' => '[resource link_identifier, [ resource link_identifier, ]] string query [, mixed bind_arg [, mixed bind_arg [, ...]]]',
    'description' => 'Execute a query',
  ),
  'ibase_affected_rows' => 
  array (
    'return' => 'int',
    'params' => ' [ resource link_identifier ] ',
    'description' => 'Returns the number of rows affected by the previous INSERT, UPDATE or DELETE statement',
  ),
  'ibase_num_rows' => 
  array (
    'return' => 'int',
    'params' => ' resource result_identifier ',
    'description' => 'Return the number of rows that are available in a result',
  ),
  'ibase_fetch_row' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int fetch_flags]',
    'description' => 'Fetch a row  from the results of a query',
  ),
  'ibase_fetch_assoc' => 
  array (
    'return' => 'array',
    'params' => 'resource result [, int fetch_flags]',
    'description' => 'Fetch a row  from the results of a query',
  ),
  'ibase_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'resource result [, int fetch_flags]',
    'description' => 'Fetch a object from the results of a query',
  ),
  'ibase_name_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result, string name',
    'description' => 'Assign a name to a result for use with ... WHERE CURRENT OF <name> statements',
  ),
  'ibase_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result',
    'description' => 'Free the memory used by a result',
  ),
  'ibase_prepare' => 
  array (
    'return' => 'resource',
    'params' => '[resource link_identifier, ] string query',
    'description' => 'Prepare a query for later execution',
  ),
  'ibase_execute' => 
  array (
    'return' => 'mixed',
    'params' => 'resource query [, mixed bind_arg [, mixed bind_arg [, ...]]]',
    'description' => 'Execute a previously prepared query',
  ),
  'ibase_free_query' => 
  array (
    'return' => 'bool',
    'params' => 'resource query',
    'description' => 'Free memory used by a query',
  ),
  'ibase_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource query_result',
    'description' => 'Get the number of fields in result',
  ),
  'ibase_field_info' => 
  array (
    'return' => 'array',
    'params' => 'resource query_result, int field_number',
    'description' => 'Get information about a field',
  ),
  'ibase_num_params' => 
  array (
    'return' => 'int',
    'params' => 'resource query',
    'description' => 'Get the number of params in a prepared query',
  ),
  'ibase_param_info' => 
  array (
    'return' => 'array',
    'params' => 'resource query, int field_number',
    'description' => 'Get information about a parameter',
  ),
  'ibase_wait_event' => 
  array (
    'return' => 'string',
    'params' => '[resource link_identifier,] string event [, string event [, ...]]',
    'description' => 'Waits for any one of the passed Interbase events to be posted by the database, and returns its name',
  ),
  'ibase_set_event_handler' => 
  array (
    'return' => 'resource',
    'params' => '[resource link_identifier,] callback handler, string event [, string event [, ...]]',
    'description' => 'Register the callback for handling each of the named events',
  ),
  'ibase_free_event_handler' => 
  array (
    'return' => 'bool',
    'params' => 'resource event',
    'description' => 'Frees the event handler set by ibase_set_event_handler()',
  ),
  'openssl_x509_export_to_file' => 
  array (
    'return' => 'bool',
    'params' => 'mixed x509, string outfilename [, bool notext = true]',
    'description' => 'Exports a CERT to file or a var',
  ),
  'openssl_x509_export' => 
  array (
    'return' => 'bool',
    'params' => 'mixed x509, string &out [, bool notext = true]',
    'description' => 'Exports a CERT to file or a var',
  ),
  'openssl_x509_check_private_key' => 
  array (
    'return' => 'bool',
    'params' => 'mixed cert, mixed key',
    'description' => 'Checks if a private key corresponds to a CERT',
  ),
  'openssl_x509_parse' => 
  array (
    'return' => 'array',
    'params' => 'mixed x509 [, bool shortnames=true]',
    'description' => 'Returns an array of the fields/values of the CERT',
  ),
  'openssl_x509_checkpurpose' => 
  array (
    'return' => 'int',
    'params' => 'mixed x509cert, int purpose, array cainfo [, string untrustedfile]',
    'description' => 'Checks the CERT to see if it can be used for the purpose in purpose. cainfo holds information about trusted CAs',
  ),
  'openssl_x509_read' => 
  array (
    'return' => 'resource',
    'params' => 'mixed cert',
    'description' => 'Reads X.509 certificates',
  ),
  'openssl_x509_free' => 
  array (
    'return' => 'void',
    'params' => 'resource x509',
    'description' => 'Frees X.509 certificates',
  ),
  'openssl_csr_export_to_file' => 
  array (
    'return' => 'bool',
    'params' => 'resource csr, string outfilename [, bool notext=true]',
    'description' => 'Exports a CSR to file',
  ),
  'openssl_csr_export' => 
  array (
    'return' => 'bool',
    'params' => 'resource csr, string &out [, bool notext=true]',
    'description' => 'Exports a CSR to file or a var',
  ),
  'openssl_csr_sign' => 
  array (
    'return' => 'resource',
    'params' => 'mixed csr, mixed x509, mixed priv_key, long days [, array config_args [, long serial]]',
    'description' => 'Signs a cert with another CERT',
  ),
  'openssl_csr_new' => 
  array (
    'return' => 'bool',
    'params' => 'array dn, resource &privkey [, array configargs, array extraattribs]',
    'description' => 'Generates a privkey and CSR',
  ),
  'openssl_pkey_new' => 
  array (
    'return' => 'resource',
    'params' => '[array configargs]',
    'description' => 'Generates a new private key',
  ),
  'openssl_pkey_export_to_file' => 
  array (
    'return' => 'bool',
    'params' => 'mixed key, string outfilename [, string passphrase, array config_args',
    'description' => 'Gets an exportable representation of a key into a file',
  ),
  'openssl_pkey_export' => 
  array (
    'return' => 'bool',
    'params' => 'mixed key, &mixed out [, string passphrase [, array config_args]]',
    'description' => 'Gets an exportable representation of a key into a string or file',
  ),
  'openssl_pkey_get_public' => 
  array (
    'return' => 'int',
    'params' => 'mixed cert',
    'description' => 'Gets public key from X.509 certificate',
  ),
  'openssl_pkey_free' => 
  array (
    'return' => 'void',
    'params' => 'int key',
    'description' => 'Frees a key',
  ),
  'openssl_pkey_get_private' => 
  array (
    'return' => 'int',
    'params' => 'string key [, string passphrase]',
    'description' => 'Gets private keys',
  ),
  'openssl_pkcs7_verify' => 
  array (
    'return' => 'bool',
    'params' => 'string filename, long flags [, string signerscerts [, array cainfo [, string extracerts [, string content]]]]',
    'description' => 'Verifys that the data block is intact, the signer is who they say they are, and returns the CERTs of the signers',
  ),
  'openssl_pkcs7_encrypt' => 
  array (
    'return' => 'bool',
    'params' => 'string infile, string outfile, mixed recipcerts, array headers [, long flags [, long cipher]]',
    'description' => 'Encrypts the message in the file named infile with the certificates in recipcerts and output the result to the file named outfile',
  ),
  'openssl_pkcs7_sign' => 
  array (
    'return' => 'bool',
    'params' => 'string infile, string outfile, mixed signcert, mixed signkey, array headers [, long flags [, string extracertsfilename]]',
    'description' => 'Signs the MIME message in the file named infile with signcert/signkey and output the result to file name outfile. headers lists plain text headers to exclude from the signed portion of the message, and should include to, from and subject as a minimum',
  ),
  'openssl_pkcs7_decrypt' => 
  array (
    'return' => 'bool',
    'params' => 'string infilename, string outfilename, mixed recipcert [, mixed recipkey]',
    'description' => 'Decrypts the S/MIME message in the file name infilename and output the results to the file name outfilename.  recipcert is a CERT for one of the recipients. recipkey specifies the private key matching recipcert, if recipcert does not include the key',
  ),
  'openssl_private_encrypt' => 
  array (
    'return' => 'bool',
    'params' => 'string data, string crypted, mixed key [, int padding]',
    'description' => 'Encrypts data with private key',
  ),
  'openssl_private_decrypt' => 
  array (
    'return' => 'bool',
    'params' => 'string data, string decrypted, mixed key [, int padding]',
    'description' => 'Decrypts data with private key',
  ),
  'openssl_public_encrypt' => 
  array (
    'return' => 'bool',
    'params' => 'string data, string crypted, mixed key [, int padding]',
    'description' => 'Encrypts data with public key',
  ),
  'openssl_public_decrypt' => 
  array (
    'return' => 'bool',
    'params' => 'string data, string crypted, resource key [, int padding]',
    'description' => 'Decrypts data with public key',
  ),
  'openssl_error_string' => 
  array (
    'return' => 'mixed',
    'params' => 'void',
    'description' => 'Returns a description of the last error, and alters the index of the error messages. Returns false when the are no more messages',
  ),
  'openssl_sign' => 
  array (
    'return' => 'bool',
    'params' => 'string data, &string signature, mixed key',
    'description' => 'Signs data',
  ),
  'openssl_verify' => 
  array (
    'return' => 'int',
    'params' => 'string data, string signature, mixed key',
    'description' => 'Verifys data',
  ),
  'openssl_seal' => 
  array (
    'return' => 'int',
    'params' => 'string data, &string sealdata, &array ekeys, array pubkeys',
    'description' => 'Seals data',
  ),
  'openssl_open' => 
  array (
    'return' => 'bool',
    'params' => 'string data, &string opendata, string ekey, mixed privkey',
    'description' => 'Opens data',
  ),
  'date' => 
  array (
    'return' => 'string',
    'params' => 'string format [, long timestamp]',
    'description' => 'Format a local date/time',
  ),
  'gmdate' => 
  array (
    'return' => 'string',
    'params' => 'string format [, long timestamp]',
    'description' => 'Format a GMT date/time',
  ),
  'idate' => 
  array (
    'return' => 'int',
    'params' => 'string format [, int timestamp]',
    'description' => 'Format a local time/date as integer',
  ),
  'strtotime' => 
  array (
    'return' => 'int',
    'params' => 'string time [, int now ]',
    'description' => 'Convert string representation of date and time to a timestamp',
  ),
  'mktime' => 
  array (
    'return' => 'int',
    'params' => 'int hour, int min, int sec, int mon, int day, int year',
    'description' => 'Get UNIX timestamp for a date',
  ),
  'gmmktime' => 
  array (
    'return' => 'int',
    'params' => 'int hour, int min, int sec, int mon, int day, int year',
    'description' => 'Get UNIX timestamp for a GMT date',
  ),
  'checkdate' => 
  array (
    'return' => 'bool',
    'params' => 'int month, int day, int year',
    'description' => 'Returns true(1) if it is a valid date in gregorian calendar',
  ),
  'strftime' => 
  array (
    'return' => 'string',
    'params' => 'string format [, int timestamp]',
    'description' => 'Format a local time/date according to locale settings',
  ),
  'gmstrftime' => 
  array (
    'return' => 'string',
    'params' => 'string format [, int timestamp]',
    'description' => 'Format a GMT/UCT time/date according to locale settings',
  ),
  'time' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Return current UNIX timestamp',
  ),
  'localtime' => 
  array (
    'return' => 'array',
    'params' => '[int timestamp [, bool associative_array]]',
    'description' => 'Returns the results of the C system call localtime as an associative array if the associative_array argument is set to 1 other wise it is a regular array',
  ),
  'getdate' => 
  array (
    'return' => 'array',
    'params' => '[int timestamp]',
    'description' => 'Get date/time information',
  ),
  'date_default_timezone_set' => 
  array (
    'return' => 'bool',
    'params' => 'string timezone_identifier',
    'description' => 'Sets the default timezone used by all date/time functions in a script',
  ),
  'date_default_timezone_get' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Gets the default timezone used by all date/time functions in a script',
  ),
  'date_sunrise' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed time [, int format [, float latitude [, float longitude [, float zenith [, float gmt_offset]]]]]',
    'description' => 'Returns time of sunrise for a given day and location',
  ),
  'date_sunset' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed time [, int format [, float latitude [, float longitude [, float zenith [, float gmt_offset]]]]]',
    'description' => 'Returns time of sunset for a given day and location',
  ),
  'date_sun_info' => 
  array (
    'return' => 'array',
    'params' => 'long time, float latitude, float longitude',
    'description' => 'Returns an array with information about sun set/rise and twilight begin/end',
  ),
  'wddx_serialize_value' => 
  array (
    'return' => 'string',
    'params' => 'mixed var [, string comment]',
    'description' => 'Creates a new packet and serializes the given value',
  ),
  'wddx_serialize_vars' => 
  array (
    'return' => 'string',
    'params' => 'mixed var_name [, mixed ...]',
    'description' => 'Creates a new packet and serializes given variables into a struct',
  ),
  'wddx_packet_start' => 
  array (
    'return' => 'int',
    'params' => '[string comment]',
    'description' => 'Starts a WDDX packet with optional comment and returns the packet id',
  ),
  'wddx_packet_end' => 
  array (
    'return' => 'string',
    'params' => 'int packet_id',
    'description' => 'Ends specified WDDX packet and returns the string containing the packet',
  ),
  'wddx_add_vars' => 
  array (
    'return' => 'int',
    'params' => 'int packet_id,  mixed var_names [, mixed ...]',
    'description' => 'Serializes given variables and adds them to packet given by packet_id',
  ),
  'wddx_deserialize' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed packet',
    'description' => 'Deserializes given packet and returns a PHP value',
  ),
  'gmp_init' => 
  array (
    'return' => 'resource',
    'params' => 'mixed number [, int base]',
    'description' => 'Initializes GMP number',
  ),
  'gmp_intval' => 
  array (
    'return' => 'int',
    'params' => 'resource gmpnumber',
    'description' => 'Gets signed long value of GMP number',
  ),
  'gmp_strval' => 
  array (
    'return' => 'string',
    'params' => 'resource gmpnumber [, int base]',
    'description' => 'Gets string representation of GMP number',
  ),
  'gmp_add' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Add a and b',
  ),
  'gmp_sub' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Subtract b from a',
  ),
  'gmp_mul' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Multiply a and b',
  ),
  'gmp_div_qr' => 
  array (
    'return' => 'array',
    'params' => 'resource a, resource b [, int round]',
    'description' => 'Divide a by b, returns quotient and reminder',
  ),
  'gmp_div_r' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b [, int round]',
    'description' => 'Divide a by b, returns reminder only',
  ),
  'gmp_div_q' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b [, int round]',
    'description' => 'Divide a by b, returns quotient only',
  ),
  'gmp_mod' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Computes a modulo b',
  ),
  'gmp_divexact' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Divide a by b using exact division algorithm',
  ),
  'gmp_neg' => 
  array (
    'return' => 'resource',
    'params' => 'resource a',
    'description' => 'Negates a number',
  ),
  'gmp_abs' => 
  array (
    'return' => 'resource',
    'params' => 'resource a',
    'description' => 'Calculates absolute value',
  ),
  'gmp_fact' => 
  array (
    'return' => 'resource',
    'params' => 'int a',
    'description' => 'Calculates factorial function',
  ),
  'gmp_pow' => 
  array (
    'return' => 'resource',
    'params' => 'resource base, int exp',
    'description' => 'Raise base to power exp',
  ),
  'gmp_powm' => 
  array (
    'return' => 'resource',
    'params' => 'resource base, resource exp, resource mod',
    'description' => 'Raise base to power exp and take result modulo mod',
  ),
  'gmp_sqrt' => 
  array (
    'return' => 'resource',
    'params' => 'resource a',
    'description' => 'Takes integer part of square root of a',
  ),
  'gmp_sqrtrem' => 
  array (
    'return' => 'array',
    'params' => 'resource a',
    'description' => 'Square root with remainder',
  ),
  'gmp_perfect_square' => 
  array (
    'return' => 'bool',
    'params' => 'resource a',
    'description' => 'Checks if a is an exact square',
  ),
  'gmp_prob_prime' => 
  array (
    'return' => 'int',
    'params' => 'resource a[, int reps]',
    'description' => 'Checks if a is "probably prime"',
  ),
  'gmp_gcd' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Computes greatest common denominator (gcd) of a and b',
  ),
  'gmp_gcdext' => 
  array (
    'return' => 'array',
    'params' => 'resource a, resource b',
    'description' => 'Computes G, S, and T, such that AS + BT = G = `gcd\' (A, B)',
  ),
  'gmp_invert' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Computes the inverse of a modulo b',
  ),
  'gmp_jacobi' => 
  array (
    'return' => 'int',
    'params' => 'resource a, resource b',
    'description' => 'Computes Jacobi symbol',
  ),
  'gmp_legendre' => 
  array (
    'return' => 'int',
    'params' => 'resource a, resource b',
    'description' => 'Computes Legendre symbol',
  ),
  'gmp_cmp' => 
  array (
    'return' => 'int',
    'params' => 'resource a, resource b',
    'description' => 'Compares two numbers',
  ),
  'gmp_sign' => 
  array (
    'return' => 'int',
    'params' => 'resource a',
    'description' => 'Gets the sign of the number',
  ),
  'gmp_random' => 
  array (
    'return' => 'resource',
    'params' => '[int limiter]',
    'description' => 'Gets random number',
  ),
  'gmp_and' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Calculates logical AND of a and b',
  ),
  'gmp_or' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Calculates logical OR of a and b',
  ),
  'gmp_com' => 
  array (
    'return' => 'resource',
    'params' => 'resource a',
    'description' => 'Calculates one\'s complement of a',
  ),
  'gmp_xor' => 
  array (
    'return' => 'resource',
    'params' => 'resource a, resource b',
    'description' => 'Calculates logical exclusive OR of a and b',
  ),
  'gmp_setbit' => 
  array (
    'return' => 'void',
    'params' => 'resource &a, int index[, bool set_clear]',
    'description' => 'Sets or clear bit in a',
  ),
  'gmp_clrbit' => 
  array (
    'return' => 'void',
    'params' => 'resource &a, int index',
    'description' => 'Clears bit in a',
  ),
  'gmp_popcount' => 
  array (
    'return' => 'int',
    'params' => 'resource a',
    'description' => 'Calculates the population count of a',
  ),
  'gmp_hamdist' => 
  array (
    'return' => 'int',
    'params' => 'resource a, resource b',
    'description' => 'Calculates hamming distance between a and b',
  ),
  'gmp_scan0' => 
  array (
    'return' => 'int',
    'params' => 'resource a, int start',
    'description' => 'Finds first zero bit',
  ),
  'gmp_scan1' => 
  array (
    'return' => 'int',
    'params' => 'resource a, int start',
    'description' => 'Finds first non-zero bit',
  ),
  'gd_info' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => '',
  ),
  'imageloadfont' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Load a new font',
  ),
  'imagesetstyle' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, array styles',
    'description' => 'Set the line drawing styles for use with imageline and IMG_COLOR_STYLED.',
  ),
  'imagecreatetruecolor' => 
  array (
    'return' => 'resource',
    'params' => 'int x_size, int y_size',
    'description' => 'Create a new true color image',
  ),
  'imageistruecolor' => 
  array (
    'return' => 'bool',
    'params' => 'resource im',
    'description' => 'return true if the image uses truecolor',
  ),
  'imagetruecolortopalette' => 
  array (
    'return' => 'void',
    'params' => 'resource im, bool ditherFlag, int colorsWanted',
    'description' => 'Convert a true colour image to a palette based image with a number of colours, optionally using dithering.',
  ),
  'imagecolormatch' => 
  array (
    'return' => 'bool',
    'params' => 'resource im1, resource im2',
    'description' => 'Makes the colors of the palette version of an image more closely match the true color version',
  ),
  'imagesetthickness' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int thickness',
    'description' => 'Set line thickness for drawing lines, ellipses, rectangles, polygons etc.',
  ),
  'imagefilledellipse' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int cx, int cy, int w, int h, int color',
    'description' => 'Draw an ellipse',
  ),
  'imagefilledarc' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int cx, int cy, int w, int h, int s, int e, int col, int style',
    'description' => 'Draw a filled partial ellipse',
  ),
  'imagealphablending' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, bool on',
    'description' => 'Turn alpha blending mode on or off for the given image',
  ),
  'imagesavealpha' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, bool on',
    'description' => 'Include alpha channel to a saved image',
  ),
  'imagelayereffect' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int effect',
    'description' => 'Set the alpha blending flag to use the bundled libgd layering effects',
  ),
  'imagecolorallocatealpha' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue, int alpha',
    'description' => 'Allocate a color with an alpha level.  Works for true color and palette based images',
  ),
  'imagecolorresolvealpha' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue, int alpha',
    'description' => 'Resolve/Allocate a colour with an alpha level.  Works for true colour and palette based images',
  ),
  'imagecolorclosestalpha' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue, int alpha',
    'description' => 'Find the closest matching colour with alpha transparency',
  ),
  'imagecolorexactalpha' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue, int alpha',
    'description' => 'Find exact match for colour with transparency',
  ),
  'imagecopyresampled' => 
  array (
    'return' => 'bool',
    'params' => 'resource dst_im, resource src_im, int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h, int src_w, int src_h',
    'description' => 'Copy and resize part of an image using resampling to help ensure clarity',
  ),
  'imagerotate' => 
  array (
    'return' => 'resource',
    'params' => 'resource src_im, float angle, int bgdcolor',
    'description' => 'Rotate an image using a custom angle',
  ),
  'imagesettile' => 
  array (
    'return' => 'bool',
    'params' => 'resource image, resource tile',
    'description' => 'Set the tile image to $tile when filling $image with the "IMG_COLOR_TILED" color',
  ),
  'imagesetbrush' => 
  array (
    'return' => 'bool',
    'params' => 'resource image, resource brush',
    'description' => 'Set the brush image to $brush when filling $image with the "IMG_COLOR_BRUSHED" color',
  ),
  'imagecreate' => 
  array (
    'return' => 'resource',
    'params' => 'int x_size, int y_size',
    'description' => 'Create a new image',
  ),
  'imagetypes' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Return the types of images supported in a bitfield - 1=GIF, 2=JPEG, 4=PNG, 8=WBMP, 16=XPM',
  ),
  'imagecreatefromstring' => 
  array (
    'return' => 'resource',
    'params' => 'string image',
    'description' => 'Create a new image from the image stream in the string',
  ),
  'imagecreatefromgif' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from GIF file or URL',
  ),
  'imagecreatefromjpeg' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from JPEG file or URL',
  ),
  'imagecreatefrompng' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from PNG file or URL',
  ),
  'imagecreatefromxbm' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from XBM file or URL',
  ),
  'imagecreatefromxpm' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from XPM file or URL',
  ),
  'imagecreatefromwbmp' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from WBMP file or URL',
  ),
  'imagecreatefromgd' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from GD file or URL',
  ),
  'imagecreatefromgd2' => 
  array (
    'return' => 'resource',
    'params' => 'string filename',
    'description' => 'Create a new image from GD2 file or URL',
  ),
  'imagecreatefromgd2part' => 
  array (
    'return' => 'resource',
    'params' => 'string filename, int srcX, int srcY, int width, int height',
    'description' => 'Create a new image from a given part of GD2 file or URL',
  ),
  'imagexbm' => 
  array (
    'return' => 'int',
    'params' => 'int im, string filename [, int foreground]',
    'description' => 'Output XBM image to browser or file',
  ),
  'imagegif' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename]',
    'description' => 'Output GIF image to browser or file',
  ),
  'imagepng' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename]',
    'description' => 'Output PNG image to browser or file',
  ),
  'imagejpeg' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename [, int quality]]',
    'description' => 'Output JPEG image to browser or file',
  ),
  'imagewbmp' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename, [, int foreground]]',
    'description' => 'Output WBMP image to browser or file',
  ),
  'imagegd' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename]',
    'description' => 'Output GD image to browser or file',
  ),
  'imagegd2' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename, [, int chunk_size, [, int type]]]',
    'description' => 'Output GD2 image to browser or file',
  ),
  'imagedestroy' => 
  array (
    'return' => 'bool',
    'params' => 'resource im',
    'description' => 'Destroy an image',
  ),
  'imagecolorallocate' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue',
    'description' => 'Allocate a color for an image',
  ),
  'imagepalettecopy' => 
  array (
    'return' => 'void',
    'params' => 'resource dst, resource src',
    'description' => 'Copy the palette from the src image onto the dst image',
  ),
  'imagecolorat' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int x, int y',
    'description' => 'Get the index of the color of a pixel',
  ),
  'imagecolorclosest' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue',
    'description' => 'Get the index of the closest color to the specified color',
  ),
  'imagecolorclosesthwb' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue',
    'description' => 'Get the index of the color which has the hue, white and blackness nearest to the given color',
  ),
  'imagecolordeallocate' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int index',
    'description' => 'De-allocate a color for an image',
  ),
  'imagecolorresolve' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue',
    'description' => 'Get the index of the specified color or its closest possible alternative',
  ),
  'imagecolorexact' => 
  array (
    'return' => 'int',
    'params' => 'resource im, int red, int green, int blue',
    'description' => 'Get the index of the specified color',
  ),
  'imagecolorset' => 
  array (
    'return' => 'void',
    'params' => 'resource im, int col, int red, int green, int blue',
    'description' => 'Set the color for the specified palette index',
  ),
  'imagecolorsforindex' => 
  array (
    'return' => 'array',
    'params' => 'resource im, int col',
    'description' => 'Get the colors for an index',
  ),
  'imagegammacorrect' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, float inputgamma, float outputgamma',
    'description' => 'Apply a gamma correction to a GD image',
  ),
  'imagesetpixel' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x, int y, int col',
    'description' => 'Set a single pixel',
  ),
  'imageline' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x1, int y1, int x2, int y2, int col',
    'description' => 'Draw a line',
  ),
  'imagedashedline' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x1, int y1, int x2, int y2, int col',
    'description' => 'Draw a dashed line',
  ),
  'imagerectangle' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x1, int y1, int x2, int y2, int col',
    'description' => 'Draw a rectangle',
  ),
  'imagefilledrectangle' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x1, int y1, int x2, int y2, int col',
    'description' => 'Draw a filled rectangle',
  ),
  'imagearc' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int cx, int cy, int w, int h, int s, int e, int col',
    'description' => 'Draw a partial ellipse',
  ),
  'imageellipse' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int cx, int cy, int w, int h, int color',
    'description' => 'Draw an ellipse',
  ),
  'imagefilltoborder' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x, int y, int border, int col',
    'description' => 'Flood fill to specific color',
  ),
  'imagefill' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int x, int y, int col',
    'description' => 'Flood fill',
  ),
  'imagecolorstotal' => 
  array (
    'return' => 'int',
    'params' => 'resource im',
    'description' => 'Find out the number of colors in an image\'s palette',
  ),
  'imagecolortransparent' => 
  array (
    'return' => 'int',
    'params' => 'resource im [, int col]',
    'description' => 'Define a color as transparent',
  ),
  'imageinterlace' => 
  array (
    'return' => 'int',
    'params' => 'resource im [, int interlace]',
    'description' => 'Enable or disable interlace',
  ),
  'imagepolygon' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, array point, int num_points, int col',
    'description' => 'Draw a polygon',
  ),
  'imagefilledpolygon' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, array point, int num_points, int col',
    'description' => 'Draw a filled polygon',
  ),
  'imagefontwidth' => 
  array (
    'return' => 'int',
    'params' => 'int font',
    'description' => 'Get font width',
  ),
  'imagefontheight' => 
  array (
    'return' => 'int',
    'params' => 'int font',
    'description' => 'Get font height',
  ),
  'imagechar' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int font, int x, int y, string c, int col',
    'description' => 'Draw a character',
  ),
  'imagecharup' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int font, int x, int y, string c, int col',
    'description' => 'Draw a character rotated 90 degrees counter-clockwise',
  ),
  'imagestring' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int font, int x, int y, string str, int col',
    'description' => 'Draw a string horizontally',
  ),
  'imagestringup' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, int font, int x, int y, string str, int col',
    'description' => 'Draw a string vertically - rotated 90 degrees counter-clockwise',
  ),
  'imagecopy' => 
  array (
    'return' => 'bool',
    'params' => 'resource dst_im, resource src_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h',
    'description' => 'Copy part of an image',
  ),
  'imagecopymerge' => 
  array (
    'return' => 'bool',
    'params' => 'resource src_im, resource dst_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, int pct',
    'description' => 'Merge one part of an image with another',
  ),
  'imagecopymergegray' => 
  array (
    'return' => 'bool',
    'params' => 'resource src_im, resource dst_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, int pct',
    'description' => 'Merge one part of an image with another',
  ),
  'imagecopyresized' => 
  array (
    'return' => 'bool',
    'params' => 'resource dst_im, resource src_im, int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h, int src_w, int src_h',
    'description' => 'Copy and resize part of an image',
  ),
  'imagesx' => 
  array (
    'return' => 'int',
    'params' => 'resource im',
    'description' => 'Get image width',
  ),
  'imagesy' => 
  array (
    'return' => 'int',
    'params' => 'resource im',
    'description' => 'Get image height',
  ),
  'imageftbbox' => 
  array (
    'return' => 'array',
    'params' => 'float size, float angle, string font_file, string text [, array extrainfo]',
    'description' => 'Give the bounding box of a text using fonts via freetype2',
  ),
  'imagefttext' => 
  array (
    'return' => 'array',
    'params' => 'resource im, float size, float angle, int x, int y, int col, string font_file, string text [, array extrainfo]',
    'description' => 'Write text to the image using fonts via freetype2',
  ),
  'imagettfbbox' => 
  array (
    'return' => 'array',
    'params' => 'float size, float angle, string font_file, string text',
    'description' => 'Give the bounding box of a text using TrueType fonts',
  ),
  'imagettftext' => 
  array (
    'return' => 'array',
    'params' => 'resource im, float size, float angle, int x, int y, int col, string font_file, string text',
    'description' => 'Write text to the image using a TrueType font',
  ),
  'imagepsloadfont' => 
  array (
    'return' => 'resource',
    'params' => 'string pathname',
    'description' => 'Load a new font from specified file',
  ),
  'imagepscopyfont' => 
  array (
    'return' => 'int',
    'params' => 'int font_index',
    'description' => 'Make a copy of a font for purposes like extending or reenconding',
  ),
  'imagepsfreefont' => 
  array (
    'return' => 'bool',
    'params' => 'resource font_index',
    'description' => 'Free memory used by a font',
  ),
  'imagepsencodefont' => 
  array (
    'return' => 'bool',
    'params' => 'resource font_index, string filename',
    'description' => 'To change a fonts character encoding vector',
  ),
  'imagepsextendfont' => 
  array (
    'return' => 'bool',
    'params' => 'resource font_index, float extend',
    'description' => 'Extend or or condense (if extend < 1) a font',
  ),
  'imagepsslantfont' => 
  array (
    'return' => 'bool',
    'params' => 'resource font_index, float slant',
    'description' => 'Slant a font',
  ),
  'imagepstext' => 
  array (
    'return' => 'array',
    'params' => 'resource image, string text, resource font, int size, int xcoord, int ycoord [, int space, int tightness, float angle, int antialias]',
    'description' => 'Rasterize a string over an image',
  ),
  'imagepsbbox' => 
  array (
    'return' => 'array',
    'params' => 'string text, resource font, int size [, int space, int tightness, int angle]',
    'description' => 'Return the bounding box needed by a string if rasterized',
  ),
  'image2wbmp' => 
  array (
    'return' => 'bool',
    'params' => 'resource im [, string filename [, int threshold]]',
    'description' => 'Output WBMP image to browser or file',
  ),
  'imagefilter' => 
  array (
    'return' => 'bool',
    'params' => 'resource src_im, int filtertype, [args] ',
    'description' => 'Applies Filter an image using a custom angle',
  ),
  'imageconvolution' => 
  array (
    'return' => 'resource',
    'params' => 'resource src_im, array matrix3x3, double div, double offset',
    'description' => 'Apply a 3x3 convolution matrix, using coefficient div and offset',
  ),
  'imageantialias' => 
  array (
    'return' => 'bool',
    'params' => 'resource im, bool on',
    'description' => 'Should antialiased functions used or not',
  ),
  'recode_string' => 
  array (
    'return' => 'string',
    'params' => 'string request, string str',
    'description' => 'Recode string str according to request string',
  ),
  'recode_file' => 
  array (
    'return' => 'bool',
    'params' => 'string request, resource input, resource output',
    'description' => 'Recode file input into file output according to request',
  ),
  'posix_kill' => 
  array (
    'return' => 'bool',
    'params' => 'int pid, int sig',
    'description' => 'Send a signal to a process (POSIX.1, 3.3.2)',
  ),
  'posix_getpid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current process id (POSIX.1, 4.1.1)',
  ),
  'posix_getppid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the parent process id (POSIX.1, 4.1.1)',
  ),
  'posix_getuid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current user id (POSIX.1, 4.2.1)',
  ),
  'posix_getgid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current group id (POSIX.1, 4.2.1)',
  ),
  'posix_geteuid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current effective user id (POSIX.1, 4.2.1)',
  ),
  'posix_getegid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current effective group id (POSIX.1, 4.2.1)',
  ),
  'posix_setuid' => 
  array (
    'return' => 'bool',
    'params' => 'long uid',
    'description' => 'Set user id (POSIX.1, 4.2.2)',
  ),
  'posix_setgid' => 
  array (
    'return' => 'bool',
    'params' => 'int uid',
    'description' => 'Set group id (POSIX.1, 4.2.2)',
  ),
  'posix_seteuid' => 
  array (
    'return' => 'bool',
    'params' => 'long uid',
    'description' => 'Set effective user id',
  ),
  'posix_setegid' => 
  array (
    'return' => 'bool',
    'params' => 'long uid',
    'description' => 'Set effective group id',
  ),
  'posix_getgroups' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get supplementary group id\'s (POSIX.1, 4.2.3)',
  ),
  'posix_getlogin' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Get user name (POSIX.1, 4.2.4)',
  ),
  'posix_getpgrp' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get current process group id (POSIX.1, 4.3.1)',
  ),
  'posix_setsid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Create session and set process group id (POSIX.1, 4.3.2)',
  ),
  'posix_setpgid' => 
  array (
    'return' => 'bool',
    'params' => 'int pid, int pgid',
    'description' => 'Set process group id for job control (POSIX.1, 4.3.3)',
  ),
  'posix_getpgid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the process group id of the specified process (This is not a POSIX function, but a SVR4ism, so we compile conditionally)',
  ),
  'posix_getsid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get process group id of session leader (This is not a POSIX function, but a SVR4ism, so be compile conditionally)',
  ),
  'posix_uname' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get system name (POSIX.1, 4.4.1)',
  ),
  'posix_times' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get process times (POSIX.1, 4.5.2)',
  ),
  'posix_ctermid' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Generate terminal path name (POSIX.1, 4.7.1)',
  ),
  'posix_ttyname' => 
  array (
    'return' => 'string',
    'params' => 'int fd',
    'description' => 'Determine terminal device name (POSIX.1, 4.7.2)',
  ),
  'posix_isatty' => 
  array (
    'return' => 'bool',
    'params' => 'int fd',
    'description' => 'Determine if filedesc is a tty (POSIX.1, 4.7.1)',
  ),
  'posix_getcwd' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Get working directory pathname (POSIX.1, 5.2.2)',
  ),
  'posix_mkfifo' => 
  array (
    'return' => 'bool',
    'params' => 'string pathname, int mode',
    'description' => 'Make a FIFO special file (POSIX.1, 5.4.2)',
  ),
  'posix_mknod' => 
  array (
    'return' => 'bool',
    'params' => 'string pathname, int mode [, int major [, int minor]]',
    'description' => 'Make a special or ordinary file (POSIX.1)',
  ),
  'posix_access' => 
  array (
    'return' => 'bool',
    'params' => 'string file [, int mode]',
    'description' => 'Determine accessibility of a file (POSIX.1 5.6.3)',
  ),
  'posix_getgrnam' => 
  array (
    'return' => 'array',
    'params' => 'string groupname',
    'description' => 'Group database access (POSIX.1, 9.2.1)',
  ),
  'posix_getgrgid' => 
  array (
    'return' => 'array',
    'params' => 'long gid',
    'description' => 'Group database access (POSIX.1, 9.2.1)',
  ),
  'posix_getpwnam' => 
  array (
    'return' => 'array',
    'params' => 'string groupname',
    'description' => 'User database access (POSIX.1, 9.2.2)',
  ),
  'posix_getpwuid' => 
  array (
    'return' => 'array',
    'params' => 'long uid',
    'description' => 'User database access (POSIX.1, 9.2.2)',
  ),
  'posix_getrlimit' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get system resource consumption limits (This is not a POSIX function, but a BSDism and a SVR4ism. We compile conditionally)',
  ),
  'posix_get_last_error' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Retrieve the error number set by the last posix function which failed.',
  ),
  'posix_strerror' => 
  array (
    'return' => 'string',
    'params' => 'int errno',
    'description' => 'Retrieve the system error message associated with the given errno.',
  ),
  'curl_multi_init' => 
  array (
    'return' => 'resource',
    'params' => 'void',
    'description' => 'Returns a new cURL multi handle',
  ),
  'curl_multi_add_handle' => 
  array (
    'return' => 'int',
    'params' => 'resource multi, resource ch',
    'description' => 'Add a normal cURL handle to a cURL multi handle',
  ),
  'curl_multi_remove_handle' => 
  array (
    'return' => 'int',
    'params' => 'resource mh, resource ch',
    'description' => 'Remove a multi handle from a set of cURL handles',
  ),
  'curl_multi_select' => 
  array (
    'return' => 'int',
    'params' => 'resource mh[, double timeout]',
    'description' => 'Get all the sockets associated with the cURL extension, which can then be "selected"',
  ),
  'curl_multi_exec' => 
  array (
    'return' => 'int',
    'params' => 'resource mh, int &still_running',
    'description' => 'Run the sub-connections of the current cURL handle',
  ),
  'curl_multi_getcontent' => 
  array (
    'return' => 'string',
    'params' => 'resource ch',
    'description' => 'Return the content of a cURL handle if CURLOPT_RETURNTRANSFER is set',
  ),
  'curl_multi_info_read' => 
  array (
    'return' => 'array',
    'params' => 'resource mh',
    'description' => 'Get information about the current transfers',
  ),
  'curl_multi_close' => 
  array (
    'return' => 'void',
    'params' => 'resource mh',
    'description' => 'Close a set of cURL handles',
  ),
  'curl_version' => 
  array (
    'return' => 'array',
    'params' => '[int version]',
    'description' => 'Return cURL version information.',
  ),
  'curl_init' => 
  array (
    'return' => 'resource',
    'params' => '[string url]',
    'description' => 'Initialize a CURL session',
  ),
  'curl_copy_handle' => 
  array (
    'return' => 'resource',
    'params' => 'resource ch',
    'description' => 'Copy a cURL handle along with all of it\'s preferences',
  ),
  'curl_setopt' => 
  array (
    'return' => 'bool',
    'params' => 'resource ch, int option, mixed value',
    'description' => 'Set an option for a CURL transfer',
  ),
  'curl_setopt_array' => 
  array (
    'return' => 'bool',
    'params' => 'resource ch, array options',
    'description' => 'Set an array of option for a CURL transfer',
  ),
  'curl_exec' => 
  array (
    'return' => 'bool',
    'params' => 'resource ch',
    'description' => 'Perform a CURL session',
  ),
  'curl_getinfo' => 
  array (
    'return' => 'mixed',
    'params' => 'resource ch, int opt',
    'description' => 'Get information regarding a specific transfer',
  ),
  'curl_error' => 
  array (
    'return' => 'string',
    'params' => 'resource ch',
    'description' => 'Return a string contain the last error for the current session',
  ),
  'curl_errno' => 
  array (
    'return' => 'int',
    'params' => 'resource ch',
    'description' => 'Return an integer containing the last error number',
  ),
  'curl_close' => 
  array (
    'return' => 'void',
    'params' => 'resource ch',
    'description' => 'Close a CURL session',
  ),
  'ncurses_addch' => 
  array (
    'return' => 'int',
    'params' => 'int ch',
    'description' => 'Adds character at current position and advance cursor',
  ),
  'ncurses_waddch' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int ch',
    'description' => 'Adds character at current position in a window and advance cursor',
  ),
  'ncurses_color_set' => 
  array (
    'return' => 'int',
    'params' => 'int pair',
    'description' => 'Sets fore- and background color',
  ),
  'ncurses_delwin' => 
  array (
    'return' => 'bool',
    'params' => 'resource window',
    'description' => 'Deletes a ncurses window',
  ),
  'ncurses_end' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Stops using ncurses, clean up the screen',
  ),
  'ncurses_getch' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Reads a character from keyboard',
  ),
  'ncurses_has_colors' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Checks if terminal has colors',
  ),
  'ncurses_init' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Initializes ncurses',
  ),
  'ncurses_init_pair' => 
  array (
    'return' => 'int',
    'params' => 'int pair, int fg, int bg',
    'description' => 'Allocates a color pair',
  ),
  'ncurses_move' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x',
    'description' => 'Moves output position',
  ),
  'ncurses_newpad' => 
  array (
    'return' => 'resource',
    'params' => 'int rows, int cols',
    'description' => 'Creates a new pad (window)',
  ),
  'ncurses_prefresh' => 
  array (
    'return' => 'int',
    'params' => 'resource pad, int pminrow, int pmincol, int sminrow, int smincol, int smaxrow, int smaxcol',
    'description' => 'Copys a region from a pad into the virtual screen',
  ),
  'ncurses_pnoutrefresh' => 
  array (
    'return' => 'int',
    'params' => 'resource pad, int pminrow, int pmincol, int sminrow, int smincol, int smaxrow, int smaxcol',
    'description' => 'Copys a region from a pad into the virtual screen',
  ),
  'ncurses_newwin' => 
  array (
    'return' => 'int',
    'params' => 'int rows, int cols, int y, int x',
    'description' => 'Creates a new window',
  ),
  'ncurses_refresh' => 
  array (
    'return' => 'int',
    'params' => 'int ch',
    'description' => 'Refresh screen',
  ),
  'ncurses_start_color' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Starts using colors',
  ),
  'ncurses_standout' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Starts using \'standout\' attribute',
  ),
  'ncurses_standend' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Stops using \'standout\' attribute',
  ),
  'ncurses_baudrate' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns baudrate of terminal',
  ),
  'ncurses_beep' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Let the terminal beep',
  ),
  'ncurses_can_change_color' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Checks if we can change terminals colors',
  ),
  'ncurses_cbreak' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Switches of input buffering',
  ),
  'ncurses_clear' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Clears screen',
  ),
  'ncurses_clrtobot' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Clears screen from current position to bottom',
  ),
  'ncurses_clrtoeol' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Clears screen from current position to end of line',
  ),
  'ncurses_reset_prog_mode' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Resets the prog mode saved by def_prog_mode',
  ),
  'ncurses_reset_shell_mode' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Resets the shell mode saved by def_shell_mode',
  ),
  'ncurses_def_prog_mode' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Saves terminals (program) mode',
  ),
  'ncurses_def_shell_mode' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Saves terminal (shell) mode',
  ),
  'ncurses_delch' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Deletes character at current position, move rest of line left',
  ),
  'ncurses_deleteln' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Deletes line at current position, move rest of screen up',
  ),
  'ncurses_doupdate' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Writes all prepared refreshes to terminal',
  ),
  'ncurses_echo' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Activates keyboard input echo',
  ),
  'ncurses_erase' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Erases terminal screen',
  ),
  'ncurses_erasechar' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns current erase character',
  ),
  'ncurses_flash' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Flashes terminal screen (visual bell)',
  ),
  'ncurses_flushinp' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Flushes keyboard input buffer',
  ),
  'ncurses_has_ic' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Checks for insert- and delete-capabilities',
  ),
  'ncurses_has_il' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Checks for line insert- and delete-capabilities',
  ),
  'ncurses_inch' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Gets character and attribute at current position',
  ),
  'ncurses_insertln' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Inserts a line, move rest of screen down',
  ),
  'ncurses_isendwin' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Ncurses is in endwin mode, normal screen output may be performed',
  ),
  'ncurses_killchar' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns current line kill character',
  ),
  'ncurses_nl' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Translates newline and carriage return / line feed',
  ),
  'ncurses_nocbreak' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Switches terminal to cooked mode',
  ),
  'ncurses_noecho' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Switches off keyboard input echo',
  ),
  'ncurses_nonl' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Do not ranslate newline and carriage return / line feed',
  ),
  'ncurses_noraw' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Switches terminal out of raw mode',
  ),
  'ncurses_raw' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Switches terminal into raw mode',
  ),
  'ncurses_meta' => 
  array (
    'return' => 'int',
    'params' => 'resource window, bool 8bit',
    'description' => 'Enables/Disable 8-bit meta key information',
  ),
  'ncurses_werase' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'Erase window contents',
  ),
  'ncurses_resetty' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Restores saved terminal state',
  ),
  'ncurses_savetty' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Saves terminal state',
  ),
  'ncurses_termattrs' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns a logical OR of all attribute flags supported by terminal',
  ),
  'ncurses_use_default_colors' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Assigns terminal default colors to color id -1',
  ),
  'ncurses_slk_attr' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns current soft label keys attribute',
  ),
  'ncurses_slk_clear' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Clears soft label keys from screen',
  ),
  'ncurses_slk_noutrefresh' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Copies soft label keys to virtual screen',
  ),
  'ncurses_slk_refresh' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Copies soft label keys to screen',
  ),
  'ncurses_slk_restore' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Restores soft label keys',
  ),
  'ncurses_slk_touch' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Forces output when ncurses_slk_noutrefresh is performed',
  ),
  'ncurses_slk_set' => 
  array (
    'return' => 'bool',
    'params' => 'int labelnr, string label, int format',
    'description' => 'Sets function key labels',
  ),
  'ncurses_attroff' => 
  array (
    'return' => 'int',
    'params' => 'int attributes',
    'description' => 'Turns off the given attributes',
  ),
  'ncurses_attron' => 
  array (
    'return' => 'int',
    'params' => 'int attributes',
    'description' => 'Turns on the given attributes',
  ),
  'ncurses_attrset' => 
  array (
    'return' => 'int',
    'params' => 'int attributes',
    'description' => 'Sets given attributes',
  ),
  'ncurses_bkgd' => 
  array (
    'return' => 'int',
    'params' => 'int attrchar',
    'description' => 'Sets background property for terminal screen',
  ),
  'ncurses_curs_set' => 
  array (
    'return' => 'int',
    'params' => 'int visibility',
    'description' => 'Sets cursor state',
  ),
  'ncurses_delay_output' => 
  array (
    'return' => 'int',
    'params' => 'int milliseconds',
    'description' => 'Delays output on terminal using padding characters',
  ),
  'ncurses_echochar' => 
  array (
    'return' => 'int',
    'params' => 'int character',
    'description' => 'Single character output including refresh',
  ),
  'ncurses_halfdelay' => 
  array (
    'return' => 'int',
    'params' => 'int tenth',
    'description' => 'Puts terminal into halfdelay mode',
  ),
  'ncurses_has_key' => 
  array (
    'return' => 'int',
    'params' => 'int keycode',
    'description' => 'Checks for presence of a function key on terminal keyboard',
  ),
  'ncurses_insch' => 
  array (
    'return' => 'int',
    'params' => 'int character',
    'description' => 'Inserts character moving rest of line including character at current position',
  ),
  'ncurses_insdelln' => 
  array (
    'return' => 'int',
    'params' => 'int count',
    'description' => 'Inserts lines before current line scrolling down (negative numbers delete and scroll up)',
  ),
  'ncurses_mouseinterval' => 
  array (
    'return' => 'int',
    'params' => 'int milliseconds',
    'description' => 'Sets timeout for mouse button clicks',
  ),
  'ncurses_napms' => 
  array (
    'return' => 'int',
    'params' => 'int milliseconds',
    'description' => 'Sleep',
  ),
  'ncurses_scrl' => 
  array (
    'return' => 'int',
    'params' => 'int count',
    'description' => 'Scrolls window content up or down without changing current position',
  ),
  'ncurses_slk_attroff' => 
  array (
    'return' => 'int',
    'params' => 'int intarg',
    'description' => '???',
  ),
  'ncurses_slk_attron' => 
  array (
    'return' => 'int',
    'params' => 'int intarg',
    'description' => '???',
  ),
  'ncurses_slk_attrset' => 
  array (
    'return' => 'int',
    'params' => 'int intarg',
    'description' => '???',
  ),
  'ncurses_slk_color' => 
  array (
    'return' => 'int',
    'params' => 'int intarg',
    'description' => 'Sets color for soft label keys',
  ),
  'ncurses_slk_init' => 
  array (
    'return' => 'int',
    'params' => 'int intarg',
    'description' => 'Inits soft label keys',
  ),
  'ncurses_typeahead' => 
  array (
    'return' => 'int',
    'params' => 'int fd',
    'description' => 'Specifys different filedescriptor for typeahead checking',
  ),
  'ncurses_ungetch' => 
  array (
    'return' => 'int',
    'params' => 'int keycode',
    'description' => 'Puts a character back into the input stream',
  ),
  'ncurses_vidattr' => 
  array (
    'return' => 'int',
    'params' => 'int intarg',
    'description' => '???',
  ),
  'ncurses_use_extended_names' => 
  array (
    'return' => 'int',
    'params' => 'bool flag',
    'description' => 'Controls use of extended names in terminfo descriptions',
  ),
  'ncurses_bkgdset' => 
  array (
    'return' => 'void',
    'params' => 'int attrchar',
    'description' => 'Controls screen background',
  ),
  'ncurses_filter' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => '',
  ),
  'ncurses_noqiflush' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Do not flush on signal characters',
  ),
  'ncurses_qiflush' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Flushes on signal characters',
  ),
  'ncurses_timeout' => 
  array (
    'return' => 'void',
    'params' => 'int millisec',
    'description' => 'Sets timeout for special key sequences',
  ),
  'ncurses_use_env' => 
  array (
    'return' => 'void',
    'params' => 'int flag',
    'description' => 'Controls use of environment information about terminal size',
  ),
  'ncurses_addstr' => 
  array (
    'return' => 'int',
    'params' => 'string text',
    'description' => 'Outputs text at current position',
  ),
  'ncurses_putp' => 
  array (
    'return' => 'int',
    'params' => 'string text',
    'description' => '???',
  ),
  'ncurses_scr_dump' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Dumps screen content to file',
  ),
  'ncurses_scr_init' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Initializes screen from file dump',
  ),
  'ncurses_scr_restore' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Restores screen from file dump',
  ),
  'ncurses_scr_set' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Inherits screen from file dump',
  ),
  'ncurses_mvaddch' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, int c',
    'description' => 'Moves current position and add character',
  ),
  'ncurses_mvaddchnstr' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, string s, int n',
    'description' => 'Moves position and add attrributed string with specified length',
  ),
  'ncurses_addchnstr' => 
  array (
    'return' => 'int',
    'params' => 'string s, int n',
    'description' => 'Adds attributed string with specified length at current position',
  ),
  'ncurses_mvaddchstr' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, string s',
    'description' => 'Moves position and add attributed string',
  ),
  'ncurses_addchstr' => 
  array (
    'return' => 'int',
    'params' => 'string s',
    'description' => 'Adds attributed string at current position',
  ),
  'ncurses_mvaddnstr' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, string s, int n',
    'description' => 'Moves position and add string with specified length',
  ),
  'ncurses_addnstr' => 
  array (
    'return' => 'int',
    'params' => 'string s, int n',
    'description' => 'Adds string with specified length at current position',
  ),
  'ncurses_mvaddstr' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, string s',
    'description' => 'Moves position and add string',
  ),
  'ncurses_mvdelch' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x',
    'description' => 'Moves position and delete character, shift rest of line left',
  ),
  'ncurses_mvgetch' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x',
    'description' => 'Moves position and get character at new position',
  ),
  'ncurses_mvinch' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x',
    'description' => 'Moves position and get attributed character at new position',
  ),
  'ncurses_insstr' => 
  array (
    'return' => 'int',
    'params' => 'string text',
    'description' => 'Inserts string at current position, moving rest of line right',
  ),
  'ncurses_instr' => 
  array (
    'return' => 'int',
    'params' => 'string &buffer',
    'description' => 'Reads string from terminal screen',
  ),
  'ncurses_mvhline' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, int attrchar, int n',
    'description' => 'Sets new position and draw a horizontal line using an attributed character and max. n characters long',
  ),
  'ncurses_mvvline' => 
  array (
    'return' => 'int',
    'params' => 'int y, int x, int attrchar, int n',
    'description' => 'Sets new position and draw a vertical line using an attributed character and max. n characters long',
  ),
  'ncurses_mvcur' => 
  array (
    'return' => 'int',
    'params' => 'int old_y,int old_x, int new_y, int new_x',
    'description' => 'Moves cursor immediately',
  ),
  'ncurses_init_color' => 
  array (
    'return' => 'int',
    'params' => 'int color, int r, int g, int b',
    'description' => 'Sets new RGB value for color',
  ),
  'ncurses_color_content' => 
  array (
    'return' => 'int',
    'params' => 'int color, int &r, int &g, int &b',
    'description' => 'Gets the RGB value for color',
  ),
  'ncurses_pair_content' => 
  array (
    'return' => 'int',
    'params' => 'int pair, int &f, int &b',
    'description' => 'Gets the RGB value for color',
  ),
  'ncurses_border' => 
  array (
    'return' => 'int',
    'params' => 'int left, int right, int top, int bottom, int tl_corner, int tr_corner, int bl_corner, int br_corner',
    'description' => 'Draws a border around the screen using attributed characters',
  ),
  'ncurses_wborder' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int left, int right, int top, int bottom, int tl_corner, int tr_corner, int bl_corner, int br_corner',
    'description' => 'Draws a border around the window using attributed characters',
  ),
  'ncurses_assume_default_colors' => 
  array (
    'return' => 'int',
    'params' => 'int fg, int bg',
    'description' => 'Defines default colors for color 0',
  ),
  'ncurses_define_key' => 
  array (
    'return' => 'int',
    'params' => 'string definition, int keycode',
    'description' => 'Defines a keycode',
  ),
  'ncurses_hline' => 
  array (
    'return' => 'int',
    'params' => 'int charattr, int n',
    'description' => 'Draws a horizontal line at current position using an attributed character and max. n characters long',
  ),
  'ncurses_vline' => 
  array (
    'return' => 'int',
    'params' => 'int charattr, int n',
    'description' => 'Draws a vertical line at current position using an attributed character and max. n characters long',
  ),
  'ncurses_whline' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int charattr, int n',
    'description' => 'Draws a horizontal line in a window at current position using an attributed character and max. n characters long',
  ),
  'ncurses_wvline' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int charattr, int n',
    'description' => 'Draws a vertical line in a window at current position using an attributed character and max. n characters long',
  ),
  'ncurses_keyok' => 
  array (
    'return' => 'int',
    'params' => 'int keycode, int enable',
    'description' => 'Enables or disable a keycode',
  ),
  'ncurses_mvwaddstr' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int y, int x, string text',
    'description' => 'Adds string at new position in window',
  ),
  'ncurses_wrefresh' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'Refreshes window on terminal screen',
  ),
  'ncurses_termname' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns terminal name',
  ),
  'ncurses_longname' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Returns terminal description',
  ),
  'ncurses_mousemask' => 
  array (
    'return' => 'int',
    'params' => 'int newmask, int &oldmask',
    'description' => 'Returns and sets mouse options',
  ),
  'ncurses_getmouse' => 
  array (
    'return' => 'bool',
    'params' => 'array &mevent',
    'description' => 'Reads mouse event from queue. The content of mevent is cleared before new data is added.',
  ),
  'ncurses_ungetmouse' => 
  array (
    'return' => 'int',
    'params' => 'array mevent',
    'description' => 'Pushes mouse event to queue',
  ),
  'ncurses_mouse_trafo' => 
  array (
    'return' => 'bool',
    'params' => 'int &y, int &x, bool toscreen',
    'description' => 'Transforms coordinates',
  ),
  'ncurses_wmouse_trafo' => 
  array (
    'return' => 'bool',
    'params' => 'resource window, int &y, int &x, bool toscreen',
    'description' => 'Transforms window/stdscr coordinates',
  ),
  'ncurses_getyx' => 
  array (
    'return' => 'void',
    'params' => 'resource window, int &y, int &x',
    'description' => 'Returns the current cursor position for a window',
  ),
  'ncurses_getmaxyx' => 
  array (
    'return' => 'void',
    'params' => 'resource window, int &y, int &x',
    'description' => 'Returns the size of a window',
  ),
  'ncurses_wmove' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int y, int x',
    'description' => 'Moves windows output position',
  ),
  'ncurses_keypad' => 
  array (
    'return' => 'int',
    'params' => 'resource window, bool bf',
    'description' => 'Turns keypad on or off',
  ),
  'ncurses_wcolor_set' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int color_pair',
    'description' => 'Sets windows color pairings',
  ),
  'ncurses_wclear' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'Clears window',
  ),
  'ncurses_wnoutrefresh' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'Copies window to virtual screen',
  ),
  'ncurses_waddstr' => 
  array (
    'return' => 'int',
    'params' => 'resource window, string str [, int n]',
    'description' => 'Outputs text at current postion in window',
  ),
  'ncurses_wgetch' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'Reads a character from keyboard (window)',
  ),
  'ncurses_wattroff' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int attrs',
    'description' => 'Turns off attributes for a window',
  ),
  'ncurses_wattron' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int attrs',
    'description' => 'Turns on attributes for a window',
  ),
  'ncurses_wattrset' => 
  array (
    'return' => 'int',
    'params' => 'resource window, int attrs',
    'description' => 'Set the attributes for a window',
  ),
  'ncurses_wstandend' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'End standout mode for a window',
  ),
  'ncurses_wstandout' => 
  array (
    'return' => 'int',
    'params' => 'resource window',
    'description' => 'Enter standout mode for a window',
  ),
  'ncurses_new_panel' => 
  array (
    'return' => 'resource',
    'params' => 'resource window',
    'description' => 'Create a new panel and associate it with window',
  ),
  'ncurses_del_panel' => 
  array (
    'return' => 'bool',
    'params' => 'resource panel',
    'description' => 'Remove panel from the stack and delete it (but not the associated window)',
  ),
  'ncurses_hide_panel' => 
  array (
    'return' => 'int',
    'params' => 'resource panel',
    'description' => 'Remove panel from the stack, making it invisible',
  ),
  'ncurses_show_panel' => 
  array (
    'return' => 'int',
    'params' => 'resource panel',
    'description' => 'Places an invisible panel on top of the stack, making it visible',
  ),
  'ncurses_top_panel' => 
  array (
    'return' => 'int',
    'params' => 'resource panel',
    'description' => 'Moves a visible panel to the top of the stack',
  ),
  'ncurses_bottom_panel' => 
  array (
    'return' => 'int',
    'params' => 'resource panel',
    'description' => 'Moves a visible panel to the bottom of the stack',
  ),
  'ncurses_move_panel' => 
  array (
    'return' => 'int',
    'params' => 'resource panel, int startx, int starty',
    'description' => 'Moves a panel so that it\'s upper-left corner is at [startx, starty]',
  ),
  'ncurses_replace_panel' => 
  array (
    'return' => 'int',
    'params' => 'resource panel, resource window',
    'description' => 'Replaces the window associated with panel',
  ),
  'ncurses_panel_above' => 
  array (
    'return' => 'resource',
    'params' => 'resource panel',
    'description' => 'Returns the panel above panel. If panel is null, returns the bottom panel in the stack',
  ),
  'ncurses_panel_below' => 
  array (
    'return' => 'resource',
    'params' => 'resource panel',
    'description' => 'Returns the panel below panel. If panel is null, returns the top panel in the stack',
  ),
  'ncurses_panel_window' => 
  array (
    'return' => 'resource',
    'params' => 'resource panel',
    'description' => 'Returns the window associated with panel',
  ),
  'ncurses_update_panels' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Refreshes the virtual screen to reflect the relations between panels in the stack.',
  ),
  'ftp_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string host [, int port [, int timeout]]',
    'description' => 'Opens a FTP stream',
  ),
  'ftp_ssl_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string host [, int port [, int timeout]]',
    'description' => 'Opens a FTP-SSL stream',
  ),
  'ftp_login' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string username, string password',
    'description' => 'Logs into the FTP server',
  ),
  'ftp_pwd' => 
  array (
    'return' => 'string',
    'params' => 'resource stream',
    'description' => 'Returns the present working directory',
  ),
  'ftp_cdup' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream',
    'description' => 'Changes to the parent directory',
  ),
  'ftp_chdir' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string directory',
    'description' => 'Changes directories',
  ),
  'ftp_exec' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string command',
    'description' => 'Requests execution of a program on the FTP server',
  ),
  'ftp_raw' => 
  array (
    'return' => 'array',
    'params' => 'resource stream, string command',
    'description' => 'Sends a literal command to the FTP server',
  ),
  'ftp_mkdir' => 
  array (
    'return' => 'string',
    'params' => 'resource stream, string directory',
    'description' => 'Creates a directory and returns the absolute path for the new directory or false on error',
  ),
  'ftp_rmdir' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string directory',
    'description' => 'Removes a directory',
  ),
  'ftp_chmod' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, int mode, string filename',
    'description' => 'Sets permissions on a file',
  ),
  'ftp_alloc' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, int size[, &response]',
    'description' => 'Attempt to allocate space on the remote FTP server',
  ),
  'ftp_nlist' => 
  array (
    'return' => 'array',
    'params' => 'resource stream, string directory',
    'description' => 'Returns an array of filenames in the given directory',
  ),
  'ftp_rawlist' => 
  array (
    'return' => 'array',
    'params' => 'resource stream, string directory [, bool recursive]',
    'description' => 'Returns a detailed listing of a directory as an array of output lines',
  ),
  'ftp_systype' => 
  array (
    'return' => 'string',
    'params' => 'resource stream',
    'description' => 'Returns the system type identifier',
  ),
  'ftp_fget' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, resource fp, string remote_file, int mode[, int resumepos]',
    'description' => 'Retrieves a file from the FTP server and writes it to an open file',
  ),
  'ftp_nb_fget' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, resource fp, string remote_file, int mode[, int resumepos]',
    'description' => 'Retrieves a file from the FTP server asynchronly and writes it to an open file',
  ),
  'ftp_pasv' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, bool pasv',
    'description' => 'Turns passive mode on or off',
  ),
  'ftp_get' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string local_file, string remote_file, int mode[, int resume_pos]',
    'description' => 'Retrieves a file from the FTP server and writes it to a local file',
  ),
  'ftp_nb_get' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string local_file, string remote_file, int mode[, int resume_pos]',
    'description' => 'Retrieves a file from the FTP server nbhronly and writes it to a local file',
  ),
  'ftp_nb_continue' => 
  array (
    'return' => 'int',
    'params' => 'resource stream',
    'description' => 'Continues retrieving/sending a file nbronously',
  ),
  'ftp_fput' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string remote_file, resource fp, int mode[, int startpos]',
    'description' => 'Stores a file from an open file to the FTP server',
  ),
  'ftp_nb_fput' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string remote_file, resource fp, int mode[, int startpos]',
    'description' => 'Stores a file from an open file to the FTP server nbronly',
  ),
  'ftp_put' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string remote_file, string local_file, int mode[, int startpos]',
    'description' => 'Stores a file on the FTP server',
  ),
  'ftp_nb_put' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string remote_file, string local_file, int mode[, int startpos]',
    'description' => 'Stores a file on the FTP server',
  ),
  'ftp_size' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string filename',
    'description' => 'Returns the size of the file, or -1 on error',
  ),
  'ftp_mdtm' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string filename',
    'description' => 'Returns the last modification time of the file, or -1 on error',
  ),
  'ftp_rename' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string src, string dest',
    'description' => 'Renames the given file to a new path',
  ),
  'ftp_delete' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string file',
    'description' => 'Deletes a file',
  ),
  'ftp_site' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, string cmd',
    'description' => 'Sends a SITE command to the server',
  ),
  'ftp_close' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream',
    'description' => 'Closes the FTP stream',
  ),
  'ftp_set_option' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, int option, mixed value',
    'description' => 'Sets an FTP option',
  ),
  'ftp_get_option' => 
  array (
    'return' => 'mixed',
    'params' => 'resource stream, int option',
    'description' => 'Gets an FTP option',
  ),
  'birdstep_connect' => 
  array (
    'return' => 'int',
    'params' => 'string server, string user, string pass',
    'description' => '',
  ),
  'birdstep_close' => 
  array (
    'return' => 'bool',
    'params' => 'int id',
    'description' => '',
  ),
  'birdstep_exec' => 
  array (
    'return' => 'int',
    'params' => 'int index, string exec_str',
    'description' => '',
  ),
  'birdstep_fetch' => 
  array (
    'return' => 'bool',
    'params' => 'int index',
    'description' => '',
  ),
  'birdstep_result' => 
  array (
    'return' => 'mixed',
    'params' => 'int index, int col',
    'description' => '',
  ),
  'birdstep_freeresult' => 
  array (
    'return' => 'bool',
    'params' => 'int index',
    'description' => '',
  ),
  'birdstep_autocommit' => 
  array (
    'return' => 'bool',
    'params' => 'int index',
    'description' => '',
  ),
  'birdstep_off_autocommit' => 
  array (
    'return' => 'bool',
    'params' => 'int index',
    'description' => '',
  ),
  'birdstep_commit' => 
  array (
    'return' => 'bool',
    'params' => 'int index',
    'description' => '',
  ),
  'birdstep_rollback' => 
  array (
    'return' => 'bool',
    'params' => 'int index',
    'description' => '',
  ),
  'birdstep_fieldname' => 
  array (
    'return' => 'string',
    'params' => 'int index, int col',
    'description' => '',
  ),
  'birdstep_fieldnum' => 
  array (
    'return' => 'int',
    'params' => 'int index',
    'description' => '',
  ),
  'odbc_close_all' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Close all ODBC connections',
  ),
  'odbc_binmode' => 
  array (
    'return' => 'bool',
    'params' => 'int result_id, int mode',
    'description' => 'Handle binary column data',
  ),
  'odbc_longreadlen' => 
  array (
    'return' => 'bool',
    'params' => 'int result_id, int length',
    'description' => 'Handle LONG columns',
  ),
  'odbc_prepare' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string query',
    'description' => 'Prepares a statement for execution',
  ),
  'odbc_execute' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id [, array parameters_array]',
    'description' => 'Execute a prepared statement',
  ),
  'odbc_cursor' => 
  array (
    'return' => 'string',
    'params' => 'resource result_id',
    'description' => 'Get cursor name',
  ),
  'odbc_data_source' => 
  array (
    'return' => 'array',
    'params' => 'resource connection_id, int fetch_type',
    'description' => 'Return information about the currently connected data source',
  ),
  'odbc_exec' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string query [, int flags]',
    'description' => 'Prepare and execute an SQL statement',
  ),
  'odbc_fetch_object' => 
  array (
    'return' => 'object',
    'params' => 'int result [, int rownumber]',
    'description' => 'Fetch a result row as an object',
  ),
  'odbc_fetch_array' => 
  array (
    'return' => 'array',
    'params' => 'int result [, int rownumber]',
    'description' => 'Fetch a result row as an associative array',
  ),
  'odbc_fetch_into' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id, array result_array, [, int rownumber]',
    'description' => 'Fetch one result row into an array',
  ),
  'solid_fetch_prev' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id',
    'description' => '',
  ),
  'odbc_fetch_row' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id [, int row_number]',
    'description' => 'Fetch a row',
  ),
  'odbc_result' => 
  array (
    'return' => 'mixed',
    'params' => 'resource result_id, mixed field',
    'description' => 'Get result data',
  ),
  'odbc_result_all' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id [, string format]',
    'description' => 'Print result as HTML table',
  ),
  'odbc_free_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id',
    'description' => 'Free resources associated with a result',
  ),
  'odbc_connect' => 
  array (
    'return' => 'resource',
    'params' => 'string DSN, string user, string password [, int cursor_option]',
    'description' => 'Connect to a datasource',
  ),
  'odbc_pconnect' => 
  array (
    'return' => 'resource',
    'params' => 'string DSN, string user, string password [, int cursor_option]',
    'description' => 'Establish a persistent connection to a datasource',
  ),
  'odbc_close' => 
  array (
    'return' => 'void',
    'params' => 'resource connection_id',
    'description' => 'Close an ODBC connection',
  ),
  'odbc_num_rows' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id',
    'description' => 'Get number of rows in a result',
  ),
  'odbc_next_result' => 
  array (
    'return' => 'bool',
    'params' => 'resource result_id',
    'description' => 'Checks if multiple results are avaiable',
  ),
  'odbc_num_fields' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id',
    'description' => 'Get number of columns in a result',
  ),
  'odbc_field_name' => 
  array (
    'return' => 'string',
    'params' => 'resource result_id, int field_number',
    'description' => 'Get a column name',
  ),
  'odbc_field_type' => 
  array (
    'return' => 'string',
    'params' => 'resource result_id, int field_number',
    'description' => 'Get the datatype of a column',
  ),
  'odbc_field_len' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id, int field_number',
    'description' => 'Get the length (precision) of a column',
  ),
  'odbc_field_scale' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id, int field_number',
    'description' => 'Get the scale of a column',
  ),
  'odbc_field_num' => 
  array (
    'return' => 'int',
    'params' => 'resource result_id, string field_name',
    'description' => 'Return column number',
  ),
  'odbc_autocommit' => 
  array (
    'return' => 'mixed',
    'params' => 'resource connection_id [, int OnOff]',
    'description' => 'Toggle autocommit mode or get status',
  ),
  'odbc_commit' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection_id',
    'description' => 'Commit an ODBC transaction',
  ),
  'odbc_rollback' => 
  array (
    'return' => 'bool',
    'params' => 'resource connection_id',
    'description' => 'Rollback a transaction',
  ),
  'odbc_error' => 
  array (
    'return' => 'string',
    'params' => '[resource connection_id]',
    'description' => 'Get the last error code',
  ),
  'odbc_errormsg' => 
  array (
    'return' => 'string',
    'params' => '[resource connection_id]',
    'description' => 'Get the last error message',
  ),
  'odbc_setoption' => 
  array (
    'return' => 'bool',
    'params' => 'resource conn_id|result_id, int which, int option, int value',
    'description' => 'Sets connection or statement options',
  ),
  'odbc_tables' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id [, string qualifier [, string owner [, string name [, string table_types]]]]',
    'description' => 'Call the SQLTables function',
  ),
  'odbc_columns' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id [, string qualifier [, string owner [, string table_name [, string column_name]]]]',
    'description' => 'Returns a result identifier that can be used to fetch a list of column names in specified tables',
  ),
  'odbc_columnprivileges' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string catalog, string schema, string table, string column',
    'description' => 'Returns a result identifier that can be used to fetch a list of columns and associated privileges for the specified table',
  ),
  'odbc_foreignkeys' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string pk_qualifier, string pk_owner, string pk_table, string fk_qualifier, string fk_owner, string fk_table',
    'description' => 'Returns a result identifier to either a list of foreign keys in the specified table or a list of foreign keys in other tables that refer to the primary key in the specified table',
  ),
  'odbc_gettypeinfo' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id [, int data_type]',
    'description' => 'Returns a result identifier containing information about data types supported by the data source',
  ),
  'odbc_primarykeys' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string qualifier, string owner, string table',
    'description' => 'Returns a result identifier listing the column names that comprise the primary key for a table',
  ),
  'odbc_procedurecolumns' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id [, string qualifier, string owner, string proc, string column]',
    'description' => 'Returns a result identifier containing the list of input and output parameters, as well as the columns that make up the result set for the specified procedures',
  ),
  'odbc_procedures' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id [, string qualifier, string owner, string name]',
    'description' => 'Returns a result identifier containg the list of procedure names in a datasource',
  ),
  'odbc_specialcolumns' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, int type, string qualifier, string owner, string table, int scope, int nullable',
    'description' => 'Returns a result identifier containing either the optimal set of columns that uniquely identifies a row in the table or columns that are automatically updated when any value in the row is updated by a transaction',
  ),
  'odbc_statistics' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string qualifier, string owner, string name, int unique, int accuracy',
    'description' => 'Returns a result identifier that contains statistics about a single table and the indexes associated with the table',
  ),
  'odbc_tableprivileges' => 
  array (
    'return' => 'resource',
    'params' => 'resource connection_id, string qualifier, string owner, string name',
    'description' => 'Returns a result identifier containing a list of tables and the privileges associated with each table',
  ),
  'pspell_new' => 
  array (
    'return' => 'int',
    'params' => 'string language [, string spelling [, string jargon [, string encoding [, int mode]]]]',
    'description' => 'Load a dictionary',
  ),
  'pspell_new_personal' => 
  array (
    'return' => 'int',
    'params' => 'string personal, string language [, string spelling [, string jargon [, string encoding [, int mode]]]]',
    'description' => 'Load a dictionary with a personal wordlist',
  ),
  'pspell_new_config' => 
  array (
    'return' => 'int',
    'params' => 'int config',
    'description' => 'Load a dictionary based on the given config',
  ),
  'pspell_check' => 
  array (
    'return' => 'bool',
    'params' => 'int pspell, string word',
    'description' => 'Returns true if word is valid',
  ),
  'pspell_suggest' => 
  array (
    'return' => 'array',
    'params' => 'int pspell, string word',
    'description' => 'Returns array of suggestions',
  ),
  'pspell_store_replacement' => 
  array (
    'return' => 'bool',
    'params' => 'int pspell, string misspell, string correct',
    'description' => 'Notify the dictionary of a user-selected replacement',
  ),
  'pspell_add_to_personal' => 
  array (
    'return' => 'bool',
    'params' => 'int pspell, string word',
    'description' => 'Adds a word to a personal list',
  ),
  'pspell_add_to_session' => 
  array (
    'return' => 'bool',
    'params' => 'int pspell, string word',
    'description' => 'Adds a word to the current session',
  ),
  'pspell_clear_session' => 
  array (
    'return' => 'bool',
    'params' => 'int pspell',
    'description' => 'Clears the current session',
  ),
  'pspell_save_wordlist' => 
  array (
    'return' => 'bool',
    'params' => 'int pspell',
    'description' => 'Saves the current (personal) wordlist',
  ),
  'pspell_config_create' => 
  array (
    'return' => 'int',
    'params' => 'string language [, string spelling [, string jargon [, string encoding]]]',
    'description' => 'Create a new config to be used later to create a manager',
  ),
  'pspell_config_runtogether' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, bool runtogether',
    'description' => 'Consider run-together words as valid components',
  ),
  'pspell_config_mode' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, long mode',
    'description' => 'Select mode for config (PSPELL_FAST, PSPELL_NORMAL or PSPELL_BAD_SPELLERS)',
  ),
  'pspell_config_ignore' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, int ignore',
    'description' => 'Ignore words <= n chars',
  ),
  'pspell_config_personal' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, string personal',
    'description' => 'Use a personal dictionary for this config',
  ),
  'pspell_config_dict_dir' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, string directory',
    'description' => 'location of the main word list',
  ),
  'pspell_config_data_dir' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, string directory',
    'description' => 'location of language data files',
  ),
  'pspell_config_repl' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, string repl',
    'description' => 'Use a personal dictionary with replacement pairs for this config',
  ),
  'pspell_config_save_repl' => 
  array (
    'return' => 'bool',
    'params' => 'int conf, bool save',
    'description' => 'Save replacement pairs when personal list is saved for this config',
  ),
  'dl' => 
  array (
    'return' => 'int',
    'params' => 'string extension_filename',
    'description' => 'Load a PHP extension at runtime',
  ),
  'ftok' => 
  array (
    'return' => 'int',
    'params' => 'string pathname, string proj',
    'description' => 'Convert a pathname and a project identifier to a System V IPC key',
  ),
  'assert' => 
  array (
    'return' => 'int',
    'params' => 'string|bool assertion',
    'description' => 'Checks if assertion is false',
  ),
  'assert_options' => 
  array (
    'return' => 'mixed',
    'params' => 'int what [, mixed value]',
    'description' => 'Set/get the various assert flags',
  ),
  'sprintf' => 
  array (
    'return' => 'string',
    'params' => 'string format [, mixed arg1 [, mixed ...]]',
    'description' => 'Return a formatted string',
  ),
  'vsprintf' => 
  array (
    'return' => 'string',
    'params' => 'string format, array args',
    'description' => 'Return a formatted string',
  ),
  'printf' => 
  array (
    'return' => 'int',
    'params' => 'string format [, mixed arg1 [, mixed ...]]',
    'description' => 'Output a formatted string',
  ),
  'vprintf' => 
  array (
    'return' => 'int',
    'params' => 'string format, array args',
    'description' => 'Output a formatted string',
  ),
  'fprintf' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string format [, mixed arg1 [, mixed ...]]',
    'description' => 'Output a formatted string into a stream',
  ),
  'vfprintf' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, string format, array args',
    'description' => 'Output a formatted string into a stream',
  ),
  'stream_socket_pair' => 
  array (
    'return' => 'array',
    'params' => 'int domain, int type, int protocol',
    'description' => 'Creates a pair of connected, indistinguishable socket streams',
  ),
  'stream_socket_client' => 
  array (
    'return' => 'resource',
    'params' => 'string remoteaddress [, long &errcode, string &errstring, double timeout, long flags, resource context]',
    'description' => 'Open a client connection to a remote address',
  ),
  'stream_socket_server' => 
  array (
    'return' => 'resource',
    'params' => 'string localaddress [, long &errcode, string &errstring, long flags, resource context]',
    'description' => 'Create a server socket bound to localaddress',
  ),
  'stream_socket_accept' => 
  array (
    'return' => 'resource',
    'params' => 'resource serverstream, [ double timeout, string &peername ]',
    'description' => 'Accept a client connection from a server socket',
  ),
  'stream_socket_get_name' => 
  array (
    'return' => 'string',
    'params' => 'resource stream, bool want_peer',
    'description' => 'Returns either the locally bound or remote name for a socket stream',
  ),
  'stream_socket_sendto' => 
  array (
    'return' => 'long',
    'params' => 'resouce stream, string data [, long flags [, string target_addr]]',
    'description' => 'Send data to a socket stream.  If target_addr is specified it must be in dotted quad (or [ipv6]) format',
  ),
  'stream_socket_recvfrom' => 
  array (
    'return' => 'string',
    'params' => 'resource stream, long amount [, long flags [, string &remote_addr]]',
    'description' => 'Receives data from a socket stream',
  ),
  'stream_get_contents' => 
  array (
    'return' => 'long',
    'params' => 'resource source [, long maxlen [, long offset]]',
    'description' => 'Reads all remaining bytes (or up to maxlen bytes) from a stream and returns them as a string.',
  ),
  'stream_copy_to_stream' => 
  array (
    'return' => 'long',
    'params' => 'resource source, resource dest [, long maxlen [, long pos]]',
    'description' => 'Reads up to maxlen bytes from source stream and writes them to dest stream.',
  ),
  'stream_get_meta_data' => 
  array (
    'return' => 'resource',
    'params' => 'resource fp',
    'description' => 'Retrieves header/meta data from streams/file pointers',
  ),
  'stream_get_transports' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Retrieves list of registered socket transports',
  ),
  'stream_get_wrappers' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Retrieves list of registered stream wrappers',
  ),
  'stream_select' => 
  array (
    'return' => 'int',
    'params' => 'array &read_streams, array &write_streams, array &except_streams, int tv_sec[, int tv_usec]',
    'description' => 'Runs the select() system call on the sets of streams with a timeout specified by tv_sec and tv_usec',
  ),
  'stream_context_get_options' => 
  array (
    'return' => 'array',
    'params' => 'resource context|resource stream',
    'description' => 'Retrieve options for a stream/wrapper/context',
  ),
  'stream_context_set_option' => 
  array (
    'return' => 'bool',
    'params' => 'resource context|resource stream, string wrappername, string optionname, mixed value',
    'description' => 'Set an option for a wrapper',
  ),
  'stream_context_set_params' => 
  array (
    'return' => 'bool',
    'params' => 'resource context|resource stream, array options',
    'description' => 'Set parameters for a file context',
  ),
  'stream_context_get_default' => 
  array (
    'return' => 'resource',
    'params' => '[array options]',
    'description' => 'Get a handle on the default file/stream context and optionally set parameters',
  ),
  'stream_context_create' => 
  array (
    'return' => 'resource',
    'params' => '[array options]',
    'description' => 'Create a file context and optionally set parameters',
  ),
  'stream_filter_prepend' => 
  array (
    'return' => 'resource',
    'params' => 'resource stream, string filtername[, int read_write[, string filterparams]]',
    'description' => 'Prepend a filter to a stream',
  ),
  'stream_filter_append' => 
  array (
    'return' => 'resource',
    'params' => 'resource stream, string filtername[, int read_write[, string filterparams]]',
    'description' => 'Append a filter to a stream',
  ),
  'stream_filter_remove' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream_filter',
    'description' => 'Flushes any data in the filter\'s internal buffer, removes it from the chain, and frees the resource',
  ),
  'stream_get_line' => 
  array (
    'return' => 'string',
    'params' => 'resource stream, int maxlen [, string ending]',
    'description' => 'Read up to maxlen bytes from a stream or until the ending string is found',
  ),
  'stream_set_blocking' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, int mode',
    'description' => 'Set blocking/non-blocking mode on a socket or stream',
  ),
  'set_socket_blocking' => 
  array (
    'return' => 'bool',
    'params' => 'resource socket, int mode',
    'description' => 'Set blocking/non-blocking mode on a socket',
  ),
  'stream_set_timeout' => 
  array (
    'return' => 'bool',
    'params' => 'resource stream, int seconds, int microseconds',
    'description' => 'Set timeout on stream read to seconds + microseonds',
  ),
  'stream_set_write_buffer' => 
  array (
    'return' => 'int',
    'params' => 'resource fp, int buffer',
    'description' => 'Set file write buffer',
  ),
  'stream_socket_enable_crypto' => 
  array (
    'return' => 'int',
    'params' => 'resource stream, bool enable [, int cryptokind, resource sessionstream]',
    'description' => 'Enable or disable a specific kind of crypto on the stream',
  ),
  'proc_terminate' => 
  array (
    'return' => 'int',
    'params' => 'resource process [, long signal]',
    'description' => 'kill a process opened by proc_open',
  ),
  'proc_close' => 
  array (
    'return' => 'int',
    'params' => 'resource process',
    'description' => 'close a process opened by proc_open',
  ),
  'proc_get_status' => 
  array (
    'return' => 'array',
    'params' => 'resource process',
    'description' => 'get information about a process opened by proc_open',
  ),
  'proc_open' => 
  array (
    'return' => 'resource',
    'params' => 'string command, array descriptorspec, array &pipes [, string cwd [, array env [, array other_options]]]',
    'description' => 'Run a process with more control over it\'s file descriptors',
  ),
  'opendir' => 
  array (
    'return' => 'mixed',
    'params' => 'string path[, resource context]',
    'description' => 'Open a directory and return a dir_handle',
  ),
  'dir' => 
  array (
    'return' => 'object',
    'params' => 'string directory[, resource context]',
    'description' => 'Directory class with properties, handle and class and methods read, rewind and close',
  ),
  'closedir' => 
  array (
    'return' => 'void',
    'params' => '[resource dir_handle]',
    'description' => 'Close directory connection identified by the dir_handle',
  ),
  'chroot' => 
  array (
    'return' => 'bool',
    'params' => 'string directory',
    'description' => 'Change root directory',
  ),
  'chdir' => 
  array (
    'return' => 'bool',
    'params' => 'string directory',
    'description' => 'Change the current directory',
  ),
  'getcwd' => 
  array (
    'return' => 'mixed',
    'params' => 'void',
    'description' => 'Gets the current directory',
  ),
  'rewinddir' => 
  array (
    'return' => 'void',
    'params' => '[resource dir_handle]',
    'description' => 'Rewind dir_handle back to the start',
  ),
  'readdir' => 
  array (
    'return' => 'string',
    'params' => '[resource dir_handle]',
    'description' => 'Read directory entry from dir_handle',
  ),
  'glob' => 
  array (
    'return' => 'array',
    'params' => 'string pattern [, int flags]',
    'description' => 'Find pathnames matching a pattern',
  ),
  'scandir' => 
  array (
    'return' => 'array',
    'params' => 'string dir [, int sorting_order [, resource context]]',
    'description' => 'List files & directories inside the specified path',
  ),
  'disk_total_space' => 
  array (
    'return' => 'float',
    'params' => 'string path',
    'description' => 'Get total disk space for filesystem that path is on',
  ),
  'disk_free_space' => 
  array (
    'return' => 'float',
    'params' => 'string path',
    'description' => 'Get free disk space for filesystem that path is on',
  ),
  'chgrp' => 
  array (
    'return' => 'bool',
    'params' => 'string filename, mixed group',
    'description' => 'Change file group',
  ),
  'lchgrp' => 
  array (
    'return' => 'bool',
    'params' => 'string filename, mixed group',
    'description' => 'Change symlink group',
  ),
  'chmod' => 
  array (
    'return' => 'bool',
    'params' => 'string filename, int mode',
    'description' => 'Change file mode',
  ),
  'touch' => 
  array (
    'return' => 'bool',
    'params' => 'string filename [, int time [, int atime]]',
    'description' => 'Set modification time of file',
  ),
  'clearstatcache' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Clear file stat cache',
  ),
  'fileperms' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get file permissions',
  ),
  'fileinode' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get file inode',
  ),
  'filesize' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get file size',
  ),
  'fileowner' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get file owner',
  ),
  'filegroup' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get file group',
  ),
  'fileatime' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get last access time of file',
  ),
  'filemtime' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get last modification time of file',
  ),
  'filectime' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Get inode modification time of file',
  ),
  'filetype' => 
  array (
    'return' => 'string',
    'params' => 'string filename',
    'description' => 'Get file type',
  ),
  'is_writable' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if file can be written',
  ),
  'is_readable' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if file can be read',
  ),
  'is_executable' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if file is executable',
  ),
  'is_file' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if file is a regular file',
  ),
  'is_dir' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if file is directory',
  ),
  'is_link' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if file is symbolic link',
  ),
  'file_exists' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Returns true if filename exists',
  ),
  'lstat' => 
  array (
    'return' => 'array',
    'params' => 'string filename',
    'description' => 'Give information about a file or symbolic link',
  ),
  'stat' => 
  array (
    'return' => 'array',
    'params' => 'string filename',
    'description' => 'Give information about a file',
  ),
  'convert_cyr_string' => 
  array (
    'return' => 'string',
    'params' => 'string str, string from, string to',
    'description' => 'Convert from one Cyrillic character set to another',
  ),
  'krsort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg [, int sort_flags]',
    'description' => 'Sort an array by key value in reverse order',
  ),
  'ksort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg [, int sort_flags]',
    'description' => 'Sort an array by key',
  ),
  'count' => 
  array (
    'return' => 'int',
    'params' => 'mixed var [, int mode]',
    'description' => 'Count the number of elements in a variable (usually an array)',
  ),
  'natsort' => 
  array (
    'return' => 'void',
    'params' => 'array array_arg',
    'description' => 'Sort an array using natural sort',
  ),
  'natcasesort' => 
  array (
    'return' => 'void',
    'params' => 'array array_arg',
    'description' => 'Sort an array using case-insensitive natural sort',
  ),
  'asort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg [, int sort_flags]',
    'description' => 'Sort an array and maintain index association',
  ),
  'arsort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg [, int sort_flags]',
    'description' => 'Sort an array in reverse order and maintain index association',
  ),
  'sort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg [, int sort_flags]',
    'description' => 'Sort an array',
  ),
  'rsort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg [, int sort_flags]',
    'description' => 'Sort an array in reverse order',
  ),
  'usort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg, string cmp_function',
    'description' => 'Sort an array by values using a user-defined comparison function',
  ),
  'uasort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg, string cmp_function',
    'description' => 'Sort an array with a user-defined comparison function and maintain index association',
  ),
  'uksort' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg, string cmp_function',
    'description' => 'Sort an array by keys using a user-defined comparison function',
  ),
  'end' => 
  array (
    'return' => 'mixed',
    'params' => 'array array_arg',
    'description' => 'Advances array argument\'s internal pointer to the last element and return it',
  ),
  'prev' => 
  array (
    'return' => 'mixed',
    'params' => 'array array_arg',
    'description' => 'Move array argument\'s internal pointer to the previous element and return it',
  ),
  'next' => 
  array (
    'return' => 'mixed',
    'params' => 'array array_arg',
    'description' => 'Move array argument\'s internal pointer to the next element and return it',
  ),
  'reset' => 
  array (
    'return' => 'mixed',
    'params' => 'array array_arg',
    'description' => 'Set array argument\'s internal pointer to the first element and return it',
  ),
  'current' => 
  array (
    'return' => 'mixed',
    'params' => 'array array_arg',
    'description' => 'Return the element currently pointed to by the internal array pointer',
  ),
  'key' => 
  array (
    'return' => 'mixed',
    'params' => 'array array_arg',
    'description' => 'Return the key of the element currently pointed to by the internal array pointer',
  ),
  'min' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed arg1 [, mixed arg2 [, mixed ...]]',
    'description' => 'Return the lowest value in an array or a series of arguments',
  ),
  'max' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed arg1 [, mixed arg2 [, mixed ...]]',
    'description' => 'Return the highest value in an array or a series of arguments',
  ),
  'array_walk' => 
  array (
    'return' => 'bool',
    'params' => 'array input, string funcname [, mixed userdata]',
    'description' => 'Apply a user function to every member of an array',
  ),
  'array_walk_recursive' => 
  array (
    'return' => 'bool',
    'params' => 'array input, string funcname [, mixed userdata]',
    'description' => 'Apply a user function recursively to every member of an array',
  ),
  'in_array' => 
  array (
    'return' => 'bool',
    'params' => 'mixed needle, array haystack [, bool strict]',
    'description' => 'Checks if the given value exists in the array',
  ),
  'array_search' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed needle, array haystack [, bool strict]',
    'description' => 'Searches the array for a given value and returns the corresponding key if successful',
  ),
  'extract' => 
  array (
    'return' => 'int',
    'params' => 'array var_array [, int extract_type [, string prefix]]',
    'description' => 'Imports variables into symbol table from an array',
  ),
  'compact' => 
  array (
    'return' => 'array',
    'params' => 'mixed var_names [, mixed ...]',
    'description' => 'Creates a hash containing variables and their values',
  ),
  'array_fill' => 
  array (
    'return' => 'array',
    'params' => 'int start_key, int num, mixed val',
    'description' => 'Create an array containing num elements starting with index start_key each initialized to val',
  ),
  'range' => 
  array (
    'return' => 'array',
    'params' => 'mixed low, mixed high[, int step]',
    'description' => 'Create an array containing the range of integers or characters from low to high (inclusive)',
  ),
  'shuffle' => 
  array (
    'return' => 'bool',
    'params' => 'array array_arg',
    'description' => 'Randomly shuffle the contents of an array',
  ),
  'array_push' => 
  array (
    'return' => 'int',
    'params' => 'array stack, mixed var [, mixed ...]',
    'description' => 'Pushes elements onto the end of the array',
  ),
  'array_pop' => 
  array (
    'return' => 'mixed',
    'params' => 'array stack',
    'description' => 'Pops an element off the end of the array',
  ),
  'array_shift' => 
  array (
    'return' => 'mixed',
    'params' => 'array stack',
    'description' => 'Pops an element off the beginning of the array',
  ),
  'array_unshift' => 
  array (
    'return' => 'int',
    'params' => 'array stack, mixed var [, mixed ...]',
    'description' => 'Pushes elements onto the beginning of the array',
  ),
  'array_splice' => 
  array (
    'return' => 'array',
    'params' => 'array input, int offset [, int length [, array replacement]]',
    'description' => 'Removes the elements designated by offset and length and replace them with supplied array',
  ),
  'array_slice' => 
  array (
    'return' => 'array',
    'params' => 'array input, int offset [, int length]',
    'description' => 'Returns elements specified by offset and length',
  ),
  'array_merge' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Merges elements from passed arrays into one array',
  ),
  'array_merge_recursive' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Recursively merges elements from passed arrays into one array',
  ),
  'array_keys' => 
  array (
    'return' => 'array',
    'params' => 'array input [, mixed search_value[, bool strict]]',
    'description' => 'Return just the keys from the input array, optionally only for the specified search_value',
  ),
  'array_values' => 
  array (
    'return' => 'array',
    'params' => 'array input',
    'description' => 'Return just the values from the input array',
  ),
  'array_count_values' => 
  array (
    'return' => 'array',
    'params' => 'array input',
    'description' => 'Return the value as key and the frequency of that value in input as value',
  ),
  'array_reverse' => 
  array (
    'return' => 'array',
    'params' => 'array input [, bool preserve keys]',
    'description' => 'Return input as a new array with the order of the entries reversed',
  ),
  'array_pad' => 
  array (
    'return' => 'array',
    'params' => 'array input, int pad_size, mixed pad_value',
    'description' => 'Returns a copy of input array padded with pad_value to size pad_size',
  ),
  'array_flip' => 
  array (
    'return' => 'array',
    'params' => 'array input',
    'description' => 'Return array with key <-> value flipped',
  ),
  'array_change_key_case' => 
  array (
    'return' => 'array',
    'params' => 'array input [, int case=CASE_LOWER]',
    'description' => 'Retuns an array with all string keys lowercased [or uppercased]',
  ),
  'array_unique' => 
  array (
    'return' => 'array',
    'params' => 'array input',
    'description' => 'Removes duplicate values from array',
  ),
  'array_intersect_key' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Returns the entries of arr1 that have keys which are present in all the other arguments. Kind of equivalent to array_diff(array_keys($arr1), array_keys($arr2)[,array_keys(...)]). Equivalent of array_intersect_assoc() but does not do compare of the data.',
  ),
  'array_intersect_ukey' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback key_compare_func',
    'description' => 'Returns the entries of arr1 that have keys which are present in all the other arguments. Kind of equivalent to array_diff(array_keys($arr1), array_keys($arr2)[,array_keys(...)]). The comparison of the keys is performed by a user supplied function. Equivalent of array_intersect_uassoc() but does not do compare of the data.',
  ),
  'array_intersect' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Returns the entries of arr1 that have values which are present in all the other arguments',
  ),
  'array_uintersect' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback data_compare_func',
    'description' => 'Returns the entries of arr1 that have values which are present in all the other arguments. Data is compared by using an user-supplied callback.',
  ),
  'array_intersect_assoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Returns the entries of arr1 that have values which are present in all the other arguments. Keys are used to do more restrictive check',
  ),
  'array_uintersect_assoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback data_compare_func',
    'description' => 'Returns the entries of arr1 that have values which are present in all the other arguments. Keys are used to do more restrictive check. Data is compared by using an user-supplied callback.',
  ),
  'array_intersect_uassoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback key_compare_func',
    'description' => 'Returns the entries of arr1 that have values which are present in all the other arguments. Keys are used to do more restrictive check and they are compared by using an user-supplied callback.',
  ),
  'array_uintersect_uassoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback data_compare_func, callback key_compare_func',
    'description' => 'Returns the entries of arr1 that have values which are present in all the other arguments. Keys are used to do more restrictive check. Both data and keys are compared by using user-supplied callbacks.',
  ),
  'array_diff_key' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Returns the entries of arr1 that have keys which are not present in any of the others arguments. This function is like array_diff() but works on the keys instead of the values. The associativity is preserved.',
  ),
  'array_diff_ukey' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback key_comp_func',
    'description' => 'Returns the entries of arr1 that have keys which are not present in any of the others arguments. User supplied function is used for comparing the keys. This function is like array_udiff() but works on the keys instead of the values. The associativity is preserved.',
  ),
  'array_diff' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Returns the entries of arr1 that have values which are not present in any of the others arguments.',
  ),
  'array_udiff' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback data_comp_func',
    'description' => 'Returns the entries of arr1 that have values which are not present in any of the others arguments. Elements are compared by user supplied function.',
  ),
  'array_diff_assoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...]',
    'description' => 'Returns the entries of arr1 that have values which are not present in any of the others arguments but do additional checks whether the keys are equal',
  ),
  'array_diff_uassoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback data_comp_func',
    'description' => 'Returns the entries of arr1 that have values which are not present in any of the others arguments but do additional checks whether the keys are equal. Elements are compared by user supplied function.',
  ),
  'array_udiff_assoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback key_comp_func',
    'description' => 'Returns the entries of arr1 that have values which are not present in any of the others arguments but do additional checks whether the keys are equal. Keys are compared by user supplied function.',
  ),
  'array_udiff_uassoc' => 
  array (
    'return' => 'array',
    'params' => 'array arr1, array arr2 [, array ...], callback data_comp_func, callback key_comp_func',
    'description' => 'Returns the entries of arr1 that have values which are not present in any of the others arguments but do additional checks whether the keys are equal. Keys and elements are compared by user supplied functions.',
  ),
  'array_multisort' => 
  array (
    'return' => 'bool',
    'params' => 'array ar1 [, SORT_ASC|SORT_DESC [, SORT_REGULAR|SORT_NUMERIC|SORT_STRING]] [, array ar2 [, SORT_ASC|SORT_DESC [, SORT_REGULAR|SORT_NUMERIC|SORT_STRING]], ...]',
    'description' => 'Sort multiple arrays at once similar to how ORDER BY clause works in SQL',
  ),
  'array_rand' => 
  array (
    'return' => 'mixed',
    'params' => 'array input [, int num_req]',
    'description' => 'Return key/keys for random entry/entries in the array',
  ),
  'array_sum' => 
  array (
    'return' => 'mixed',
    'params' => 'array input',
    'description' => 'Returns the sum of the array entries',
  ),
  'array_product' => 
  array (
    'return' => 'mixed',
    'params' => 'array input',
    'description' => 'Returns the product of the array entries',
  ),
  'array_reduce' => 
  array (
    'return' => 'mixed',
    'params' => 'array input, mixed callback [, int initial]',
    'description' => 'Iteratively reduce the array to a single value via the callback.',
  ),
  'array_filter' => 
  array (
    'return' => 'array',
    'params' => 'array input [, mixed callback]',
    'description' => 'Filters elements from the array via the callback.',
  ),
  'array_map' => 
  array (
    'return' => 'array',
    'params' => 'mixed callback, array input1 [, array input2 ,...]',
    'description' => 'Applies the callback to the elements in given arrays.',
  ),
  'array_key_exists' => 
  array (
    'return' => 'bool',
    'params' => 'mixed key, array search',
    'description' => 'Checks if the given key or index exists in the array',
  ),
  'array_chunk' => 
  array (
    'return' => 'array',
    'params' => 'array input, int size [, bool preserve_keys]',
    'description' => 'Split array into chunks',
  ),
  'array_combine' => 
  array (
    'return' => 'array',
    'params' => 'array keys, array values',
    'description' => 'Creates an array by using the elements of the first parameter as keys and the elements of the second as correspoding keys',
  ),
  'soundex' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Calculate the soundex key of a string',
  ),
  'strptime' => 
  array (
    'return' => 'string',
    'params' => 'string timestamp, string format',
    'description' => 'Parse a time/date generated with strftime()',
  ),
  'md5' => 
  array (
    'return' => 'string',
    'params' => 'string str, [ bool raw_output]',
    'description' => 'Calculate the md5 hash of a string',
  ),
  'md5_file' => 
  array (
    'return' => 'string',
    'params' => 'string filename [, bool raw_output]',
    'description' => 'Calculate the md5 hash of given filename',
  ),
  'header' => 
  array (
    'return' => 'void',
    'params' => 'string header [, bool replace, [int http_response_code]]',
    'description' => 'Sends a raw HTTP header',
  ),
  'setcookie' => 
  array (
    'return' => 'bool',
    'params' => 'string name [, string value [, int expires [, string path [, string domain [, bool secure]]]]]',
    'description' => 'Send a cookie',
  ),
  'setrawcookie' => 
  array (
    'return' => 'bool',
    'params' => 'string name [, string value [, int expires [, string path [, string domain [, bool secure]]]]]',
    'description' => 'Send a cookie with no url encoding of the value',
  ),
  'headers_sent' => 
  array (
    'return' => 'bool',
    'params' => '[string &$file [, int &$line]]',
    'description' => 'Returns true if headers have already been sent, false otherwise',
  ),
  'headers_list' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Return list of headers to be sent / already sent',
  ),
  'crc32' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Calculate the crc32 polynomial of a string',
  ),
  'abs' => 
  array (
    'return' => 'int',
    'params' => 'int number',
    'description' => 'Return the absolute value of the number',
  ),
  'ceil' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the next highest integer value of the number',
  ),
  'floor' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the next lowest integer value from the number',
  ),
  'round' => 
  array (
    'return' => 'float',
    'params' => 'float number [, int precision]',
    'description' => 'Returns the number rounded to specified precision',
  ),
  'sin' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the sine of the number in radians',
  ),
  'cos' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the cosine of the number in radians',
  ),
  'tan' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the tangent of the number in radians',
  ),
  'asin' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the arc sine of the number in radians',
  ),
  'acos' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Return the arc cosine of the number in radians',
  ),
  'atan' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the arc tangent of the number in radians',
  ),
  'atan2' => 
  array (
    'return' => 'float',
    'params' => 'float y, float x',
    'description' => 'Returns the arc tangent of y/x, with the resulting quadrant determined by the signs of y and x',
  ),
  'sinh' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the hyperbolic sine of the number, defined as (exp(number) - exp(-number))/2',
  ),
  'cosh' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the hyperbolic cosine of the number, defined as (exp(number) + exp(-number))/2',
  ),
  'tanh' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the hyperbolic tangent of the number, defined as sinh(number)/cosh(number)',
  ),
  'asinh' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the inverse hyperbolic sine of the number, i.e. the value whose hyperbolic sine is number',
  ),
  'acosh' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the inverse hyperbolic cosine of the number, i.e. the value whose hyperbolic cosine is number',
  ),
  'atanh' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the inverse hyperbolic tangent of the number, i.e. the value whose hyperbolic tangent is number',
  ),
  'pi' => 
  array (
    'return' => 'float',
    'params' => 'void',
    'description' => 'Returns an approximation of pi',
  ),
  'is_finite' => 
  array (
    'return' => 'bool',
    'params' => 'float val',
    'description' => 'Returns whether argument is finite',
  ),
  'is_infinite' => 
  array (
    'return' => 'bool',
    'params' => 'float val',
    'description' => 'Returns whether argument is infinite',
  ),
  'is_nan' => 
  array (
    'return' => 'bool',
    'params' => 'float val',
    'description' => 'Returns whether argument is not a number',
  ),
  'pow' => 
  array (
    'return' => 'number',
    'params' => 'number base, number exponent',
    'description' => 'Returns base raised to the power of exponent. Returns integer result when possible',
  ),
  'exp' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns e raised to the power of the number',
  ),
  'expm1' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns exp(number) - 1, computed in a way that accurate even when the value of number is close to zero',
  ),
  'log1p' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns log(1 + number), computed in a way that accurate even when the value of number is close to zero',
  ),
  'log' => 
  array (
    'return' => 'float',
    'params' => 'float number, [float base]',
    'description' => 'Returns the natural logarithm of the number, or the base log if base is specified',
  ),
  'log10' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the base-10 logarithm of the number',
  ),
  'sqrt' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Returns the square root of the number',
  ),
  'hypot' => 
  array (
    'return' => 'float',
    'params' => 'float num1, float num2',
    'description' => 'Returns sqrt(num1*num1 + num2*num2)',
  ),
  'deg2rad' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Converts the number in degrees to the radian equivalent',
  ),
  'rad2deg' => 
  array (
    'return' => 'float',
    'params' => 'float number',
    'description' => 'Converts the radian number to the equivalent number in degrees',
  ),
  'bindec' => 
  array (
    'return' => 'int',
    'params' => 'string binary_number',
    'description' => 'Returns the decimal equivalent of the binary number',
  ),
  'hexdec' => 
  array (
    'return' => 'int',
    'params' => 'string hexadecimal_number',
    'description' => 'Returns the decimal equivalent of the hexadecimal number',
  ),
  'octdec' => 
  array (
    'return' => 'int',
    'params' => 'string octal_number',
    'description' => 'Returns the decimal equivalent of an octal string',
  ),
  'decbin' => 
  array (
    'return' => 'string',
    'params' => 'int decimal_number',
    'description' => 'Returns a string containing a binary representation of the number',
  ),
  'decoct' => 
  array (
    'return' => 'string',
    'params' => 'int decimal_number',
    'description' => 'Returns a string containing an octal representation of the given number',
  ),
  'dechex' => 
  array (
    'return' => 'string',
    'params' => 'int decimal_number',
    'description' => 'Returns a string containing a hexadecimal representation of the given number',
  ),
  'base_convert' => 
  array (
    'return' => 'string',
    'params' => 'string number, int frombase, int tobase',
    'description' => 'Converts a number in a string from any base <= 36 to any base <= 36',
  ),
  'number_format' => 
  array (
    'return' => 'string',
    'params' => 'float number [, int num_decimal_places [, string dec_seperator, string thousands_seperator]]',
    'description' => 'Formats a number with grouped thousands',
  ),
  'fmod' => 
  array (
    'return' => 'float',
    'params' => 'float x, float y',
    'description' => 'Returns the remainder of dividing x by y as a float',
  ),
  'gethostbyaddr' => 
  array (
    'return' => 'string',
    'params' => 'string ip_address',
    'description' => 'Get the Internet host name corresponding to a given IP address',
  ),
  'gethostbyname' => 
  array (
    'return' => 'string',
    'params' => 'string hostname',
    'description' => 'Get the IP address corresponding to a given Internet host name',
  ),
  'gethostbynamel' => 
  array (
    'return' => 'array',
    'params' => 'string hostname',
    'description' => 'Return a list of IP addresses that a given hostname resolves to.',
  ),
  'dns_check_record' => 
  array (
    'return' => 'int',
    'params' => 'string host [, string type]',
    'description' => 'Check DNS records corresponding to a given Internet host name or IP address',
  ),
  'dns_get_record' => 
  array (
    'return' => 'array|false',
    'params' => 'string hostname [, int type[, array authns, array addtl]]',
    'description' => 'Get any Resource Record corresponding to a given Internet host name',
  ),
  'dns_get_mx' => 
  array (
    'return' => 'bool',
    'params' => 'string hostname, array mxhosts [, array weight]',
    'description' => 'Get MX records corresponding to a given Internet host name',
  ),
  'bin2hex' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'Converts the binary representation of data to hex',
  ),
  'strspn' => 
  array (
    'return' => 'int',
    'params' => 'string str, string mask [, start [, len]]',
    'description' => 'Finds length of initial segment consisting entirely of characters found in mask. If start or/and length is provided works like strspn(substr($s,$start,$len),$good_chars)',
  ),
  'strcspn' => 
  array (
    'return' => 'int',
    'params' => 'string str, string mask [, start [, len]]',
    'description' => 'Finds length of initial segment consisting entirely of characters not found in mask. If start or/and length is provide works like strcspn(substr($s,$start,$len),$bad_chars)',
  ),
  'nl_langinfo' => 
  array (
    'return' => 'string',
    'params' => 'int item',
    'description' => 'Query language and locale information',
  ),
  'strcoll' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2',
    'description' => 'Compares two strings using the current locale',
  ),
  'trim' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string character_mask]',
    'description' => 'Strips whitespace from the beginning and end of a string',
  ),
  'rtrim' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string character_mask]',
    'description' => 'Removes trailing whitespace',
  ),
  'ltrim' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string character_mask]',
    'description' => 'Strips whitespace from the beginning of a string',
  ),
  'wordwrap' => 
  array (
    'return' => 'string',
    'params' => 'string str [, int width [, string break [, boolean cut]]]',
    'description' => 'Wraps buffer to selected number of characters using string break char',
  ),
  'explode' => 
  array (
    'return' => 'array',
    'params' => 'string separator, string str [, int limit]',
    'description' => 'Splits a string on string separator and return array of components. If limit is positive only limit number of components is returned. If limit is negative all components except the last abs(limit) are returned.',
  ),
  'join' => 
  array (
    'return' => 'string',
    'params' => 'array src, string glue',
    'description' => 'An alias for implode',
  ),
  'implode' => 
  array (
    'return' => 'string',
    'params' => '[string glue,] array pieces',
    'description' => 'Joins array elements placing glue string between items and return one string',
  ),
  'strtok' => 
  array (
    'return' => 'string',
    'params' => '[string str,] string token',
    'description' => 'Tokenize a string',
  ),
  'strtoupper' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Makes a string uppercase',
  ),
  'strtolower' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Makes a string lowercase',
  ),
  'basename' => 
  array (
    'return' => 'string',
    'params' => 'string path [, string suffix]',
    'description' => 'Returns the filename component of the path',
  ),
  'dirname' => 
  array (
    'return' => 'string',
    'params' => 'string path',
    'description' => 'Returns the directory name component of the path',
  ),
  'pathinfo' => 
  array (
    'return' => 'array',
    'params' => 'string path',
    'description' => 'Returns information about a certain string',
  ),
  'stristr' => 
  array (
    'return' => 'string',
    'params' => 'string haystack, string needle',
    'description' => 'Finds first occurrence of a string within another, case insensitive',
  ),
  'strstr' => 
  array (
    'return' => 'string',
    'params' => 'string haystack, string needle',
    'description' => 'Finds first occurrence of a string within another',
  ),
  'strchr' => 
  array (
    'return' => 'string',
    'params' => 'string haystack, string needle',
    'description' => 'An alias for strstr',
  ),
  'strpos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, int offset]',
    'description' => 'Finds position of first occurrence of a string within another',
  ),
  'stripos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, int offset]',
    'description' => 'Finds position of first occurrence of a string within another, case insensitive',
  ),
  'strrpos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, int offset]',
    'description' => 'Finds position of last occurrence of a string within another string',
  ),
  'strripos' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, int offset]',
    'description' => 'Finds position of last occurrence of a string within another string',
  ),
  'strrchr' => 
  array (
    'return' => 'string',
    'params' => 'string haystack, string needle',
    'description' => 'Finds the last occurrence of a character in a string within another',
  ),
  'chunk_split' => 
  array (
    'return' => 'string',
    'params' => 'string str [, int chunklen [, string ending]]',
    'description' => 'Returns split line',
  ),
  'substr' => 
  array (
    'return' => 'string',
    'params' => 'string str, int start [, int length]',
    'description' => 'Returns part of a string',
  ),
  'substr_replace' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed str, mixed repl, mixed start [, mixed length]',
    'description' => 'Replaces part of a string with another string',
  ),
  'quotemeta' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Quotes meta characters',
  ),
  'ord' => 
  array (
    'return' => 'int',
    'params' => 'string character',
    'description' => 'Returns ASCII value of character',
  ),
  'chr' => 
  array (
    'return' => 'string',
    'params' => 'int ascii',
    'description' => 'Converts ASCII code to a character',
  ),
  'ucfirst' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Makes a string\'s first character uppercase',
  ),
  'ucwords' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Uppercase the first character of every word in a string',
  ),
  'strtr' => 
  array (
    'return' => 'string',
    'params' => 'string str, string from, string to',
    'description' => 'Translates characters in str using given translation tables',
  ),
  'strrev' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Reverse a string',
  ),
  'similar_text' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2 [, float percent]',
    'description' => 'Calculates the similarity between two strings',
  ),
  'addcslashes' => 
  array (
    'return' => 'string',
    'params' => 'string str, string charlist',
    'description' => 'Escapes all chars mentioned in charlist with backslash. It creates octal representations if asked to backslash characters with 8th bit set or with ASCII<32 (except \'\\n\', \'\\r\', \'\\t\' etc...)',
  ),
  'addslashes' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Escapes single quote, double quotes and backslash characters in a string with backslashes',
  ),
  'stripcslashes' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Strips backslashes from a string. Uses C-style conventions',
  ),
  'stripslashes' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Strips backslashes from a string',
  ),
  'str_replace' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed search, mixed replace, mixed subject [, int &replace_count]',
    'description' => 'Replaces all occurrences of search in haystack with replace',
  ),
  'str_ireplace' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed search, mixed replace, mixed subject [, int &replace_count]',
    'description' => 'Replaces all occurrences of search in haystack with replace / case-insensitive',
  ),
  'hebrev' => 
  array (
    'return' => 'string',
    'params' => 'string str [, int max_chars_per_line]',
    'description' => 'Converts logical Hebrew text to visual text',
  ),
  'hebrevc' => 
  array (
    'return' => 'string',
    'params' => 'string str [, int max_chars_per_line]',
    'description' => 'Converts logical Hebrew text to visual text with newline conversion',
  ),
  'nl2br' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Converts newlines to HTML line breaks',
  ),
  'strip_tags' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string allowable_tags]',
    'description' => 'Strips HTML and PHP tags from a string',
  ),
  'setlocale' => 
  array (
    'return' => 'string',
    'params' => 'mixed category, string locale [, string ...]',
    'description' => 'Set locale information',
  ),
  'parse_str' => 
  array (
    'return' => 'void',
    'params' => 'string encoded_string [, array result]',
    'description' => 'Parses GET/POST/COOKIE data and sets global variables',
  ),
  'str_repeat' => 
  array (
    'return' => 'string',
    'params' => 'string input, int mult',
    'description' => 'Returns the input string repeat mult times',
  ),
  'count_chars' => 
  array (
    'return' => 'mixed',
    'params' => 'string input [, int mode]',
    'description' => 'Returns info about what characters are used in input',
  ),
  'strnatcmp' => 
  array (
    'return' => 'int',
    'params' => 'string s1, string s2',
    'description' => 'Returns the result of string comparison using \'natural\' algorithm',
  ),
  'localeconv' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns numeric formatting information based on the current locale',
  ),
  'strnatcasecmp' => 
  array (
    'return' => 'int',
    'params' => 'string s1, string s2',
    'description' => 'Returns the result of case-insensitive string comparison using \'natural\' algorithm',
  ),
  'substr_count' => 
  array (
    'return' => 'int',
    'params' => 'string haystack, string needle [, int offset [, int length]]',
    'description' => 'Returns the number of times a substring occurs in the string',
  ),
  'str_pad' => 
  array (
    'return' => 'string',
    'params' => 'string input, int pad_length [, string pad_string [, int pad_type]]',
    'description' => 'Returns input string padded on the left or right to specified length with pad_string',
  ),
  'sscanf' => 
  array (
    'return' => 'mixed',
    'params' => 'string str, string format [, string ...]',
    'description' => 'Implements an ANSI C compatible sscanf',
  ),
  'str_rot13' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Perform the rot13 transform on a string',
  ),
  'str_shuffle' => 
  array (
    'return' => 'void',
    'params' => 'string str',
    'description' => 'Shuffles string. One permutation of all possible is created',
  ),
  'str_word_count' => 
  array (
    'return' => 'mixed',
    'params' => 'string str, [int format [, string charlist]]',
    'description' => 'Counts the number of words inside a string. If format of 1 is specified,then the function will return an array containing all the wordsfound inside the string. If format of 2 is specified, then the functionwill return an associated array where the position of the word is the keyand the word itself is the value.For the purpose of this function, \'word\' is defined as a locale dependentstring containing alphabetic characters, which also may contain, but not startwith "\'" and "-" characters.',
  ),
  'money_format' => 
  array (
    'return' => 'string',
    'params' => 'string format , float value',
    'description' => 'Convert monetary value(s) to string',
  ),
  'str_split' => 
  array (
    'return' => 'array',
    'params' => 'string str [, int split_length]',
    'description' => 'Convert a string to an array. If split_length is specified, break the string down into chunks each split_length characters long.',
  ),
  'strpbrk' => 
  array (
    'return' => 'array',
    'params' => 'string haystack, string char_list',
    'description' => 'Search a string for any of a set of characters',
  ),
  'substr_compare' => 
  array (
    'return' => 'int',
    'params' => 'string main_str, string str, int offset [, int length [, bool case_sensitivity]]',
    'description' => 'Binary safe optionally case insensitive comparison of 2 strings from an offset, up to length characters',
  ),
  'fsockopen' => 
  array (
    'return' => 'resource',
    'params' => 'string hostname, int port [, int errno [, string errstr [, float timeout]]]',
    'description' => 'Open Internet or Unix domain socket connection',
  ),
  'pfsockopen' => 
  array (
    'return' => 'resource',
    'params' => 'string hostname, int port [, int errno [, string errstr [, float timeout]]]',
    'description' => 'Open persistent Internet or Unix domain socket connection',
  ),
  'readlink' => 
  array (
    'return' => 'string',
    'params' => 'string filename',
    'description' => 'Return the target of a symbolic link',
  ),
  'linkinfo' => 
  array (
    'return' => 'int',
    'params' => 'string filename',
    'description' => 'Returns the st_dev field of the UNIX C stat structure describing the link',
  ),
  'symlink' => 
  array (
    'return' => 'int',
    'params' => 'string target, string link',
    'description' => 'Create a symbolic link',
  ),
  'link' => 
  array (
    'return' => 'int',
    'params' => 'string target, string link',
    'description' => 'Create a hard link',
  ),
  'getmyuid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get PHP script owner\'s UID',
  ),
  'getmygid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get PHP script owner\'s GID',
  ),
  'getmypid' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get current process ID',
  ),
  'getmyinode' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the inode of the current script being parsed',
  ),
  'getlastmod' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get time of last page modification',
  ),
  'var_dump' => 
  array (
    'return' => 'void',
    'params' => 'mixed var',
    'description' => 'Dumps a string representation of variable to output',
  ),
  'debug_zval_dump' => 
  array (
    'return' => 'void',
    'params' => 'mixed var',
    'description' => 'Dumps a string representation of an internal zend value to output.',
  ),
  'var_export' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed var [, bool return]',
    'description' => 'Outputs or returns a string representation of a variable',
  ),
  'serialize' => 
  array (
    'return' => 'string',
    'params' => 'mixed variable',
    'description' => 'Returns a string representation of variable (which can later be unserialized)',
  ),
  'unserialize' => 
  array (
    'return' => 'mixed',
    'params' => 'string variable_representation',
    'description' => 'Takes a string representation of variable and recreates it',
  ),
  'memory_get_usage' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the allocated by PHP memory',
  ),
  'ereg' => 
  array (
    'return' => 'int',
    'params' => 'string pattern, string string [, array registers]',
    'description' => 'Regular expression match',
  ),
  'eregi' => 
  array (
    'return' => 'int',
    'params' => 'string pattern, string string [, array registers]',
    'description' => 'Case-insensitive regular expression match',
  ),
  'ereg_replace' => 
  array (
    'return' => 'string',
    'params' => 'string pattern, string replacement, string string',
    'description' => 'Replace regular expression',
  ),
  'eregi_replace' => 
  array (
    'return' => 'string',
    'params' => 'string pattern, string replacement, string string',
    'description' => 'Case insensitive replace regular expression',
  ),
  'split' => 
  array (
    'return' => 'array',
    'params' => 'string pattern, string string [, int limit]',
    'description' => 'Split string into array by regular expression',
  ),
  'spliti' => 
  array (
    'return' => 'array',
    'params' => 'string pattern, string string [, int limit]',
    'description' => 'Split string into array by regular expression case-insensitive',
  ),
  'sql_regcase' => 
  array (
    'return' => 'string',
    'params' => 'string string',
    'description' => 'Make regular expression for case insensitive match',
  ),
  'crypt' => 
  array (
    'return' => 'string',
    'params' => 'string str [, string salt]',
    'description' => 'Encrypt a string',
  ),
  'ezmlm_hash' => 
  array (
    'return' => 'int',
    'params' => 'string addr',
    'description' => 'Calculate EZMLM list hash value.',
  ),
  'mail' => 
  array (
    'return' => 'int',
    'params' => 'string to, string subject, string message [, string additional_headers [, string additional_parameters]]',
    'description' => 'Send an email message',
  ),
  'srand' => 
  array (
    'return' => 'void',
    'params' => '[int seed]',
    'description' => 'Seeds random number generator',
  ),
  'mt_srand' => 
  array (
    'return' => 'void',
    'params' => '[int seed]',
    'description' => 'Seeds Mersenne Twister random number generator',
  ),
  'rand' => 
  array (
    'return' => 'int',
    'params' => '[int min, int max]',
    'description' => 'Returns a random number',
  ),
  'mt_rand' => 
  array (
    'return' => 'int',
    'params' => '[int min, int max]',
    'description' => 'Returns a random number from Mersenne Twister',
  ),
  'getrandmax' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns the maximum value a random number can have',
  ),
  'mt_getrandmax' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns the maximum value a random number from Mersenne Twister can have',
  ),
  'get_browser' => 
  array (
    'return' => 'mixed',
    'params' => '[string browser_name [, bool return_array]]',
    'description' => 'Get information about the capabilities of a browser. If browser_name is omittedor null, HTTP_USER_AGENT is used. Returns an object by default; if return_arrayis true, returns an array.',
  ),
  'iptcembed' => 
  array (
    'return' => 'array',
    'params' => 'string iptcdata, string jpeg_file_name [, int spool]',
    'description' => 'Embed binary IPTC data into a JPEG image.',
  ),
  'iptcparse' => 
  array (
    'return' => 'array',
    'params' => 'string iptcdata',
    'description' => 'Parse binary IPTC-data into associative array',
  ),
  'quoted_printable_decode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Convert a quoted-printable string to an 8 bit string',
  ),
  'pack' => 
  array (
    'return' => 'string',
    'params' => 'string format, mixed arg1 [, mixed arg2 [, mixed ...]]',
    'description' => 'Takes one or more arguments and packs them into a binary string according to the format argument',
  ),
  'unpack' => 
  array (
    'return' => 'array',
    'params' => 'string format, string input',
    'description' => 'Unpack binary string into named array elements according to format argument',
  ),
  'base64_encode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Encodes string using MIME base64 algorithm',
  ),
  'base64_decode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Decodes string using MIME base64 algorithm',
  ),
  'gettype' => 
  array (
    'return' => 'string',
    'params' => 'mixed var',
    'description' => 'Returns the type of the variable',
  ),
  'settype' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var, string type',
    'description' => 'Set the type of the variable',
  ),
  'intval' => 
  array (
    'return' => 'int',
    'params' => 'mixed var [, int base]',
    'description' => 'Get the integer value of a variable using the optional base for the conversion',
  ),
  'floatval' => 
  array (
    'return' => 'float',
    'params' => 'mixed var',
    'description' => 'Get the float value of a variable',
  ),
  'strval' => 
  array (
    'return' => 'string',
    'params' => 'mixed var',
    'description' => 'Get the string value of a variable',
  ),
  'is_null' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is null',
  ),
  'is_resource' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is a resource',
  ),
  'is_bool' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is a boolean',
  ),
  'is_long' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is a long (integer)',
  ),
  'is_float' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is float point',
  ),
  'is_string' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is a string',
  ),
  'is_array' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is an array',
  ),
  'is_object' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var',
    'description' => 'Returns true if variable is an object',
  ),
  'is_numeric' => 
  array (
    'return' => 'bool',
    'params' => 'mixed value',
    'description' => 'Returns true if value is a number or a numeric string',
  ),
  'is_scalar' => 
  array (
    'return' => 'bool',
    'params' => 'mixed value',
    'description' => 'Returns true if value is a scalar',
  ),
  'is_callable' => 
  array (
    'return' => 'bool',
    'params' => 'mixed var [, bool syntax_only [, string callable_name]]',
    'description' => 'Returns true if var is callable.',
  ),
  'version_compare' => 
  array (
    'return' => 'int',
    'params' => 'string ver1, string ver2 [, string oper]',
    'description' => 'Compares two "PHP-standardized" version number strings',
  ),
  'exec' => 
  array (
    'return' => 'string',
    'params' => 'string command [, array &output [, int &return_value]]',
    'description' => 'Execute an external program',
  ),
  'system' => 
  array (
    'return' => 'int',
    'params' => 'string command [, int &return_value]',
    'description' => 'Execute an external program and display output',
  ),
  'passthru' => 
  array (
    'return' => 'void',
    'params' => 'string command [, int &return_value]',
    'description' => 'Execute an external program and display raw output',
  ),
  'escapeshellcmd' => 
  array (
    'return' => 'string',
    'params' => 'string command',
    'description' => 'Escape shell metacharacters',
  ),
  'escapeshellarg' => 
  array (
    'return' => 'string',
    'params' => 'string arg',
    'description' => 'Quote and escape an argument for use in a shell command',
  ),
  'shell_exec' => 
  array (
    'return' => 'string',
    'params' => 'string cmd',
    'description' => 'Execute command via shell and return complete output as string',
  ),
  'proc_nice' => 
  array (
    'return' => 'bool',
    'params' => 'int priority',
    'description' => 'Change the priority of the current process',
  ),
  'constant' => 
  array (
    'return' => 'mixed',
    'params' => 'string const_name',
    'description' => 'Given the name of a constant this function will return the constants associated value',
  ),
  'inet_ntop' => 
  array (
    'return' => 'string',
    'params' => 'string in_addr',
    'description' => 'Converts a packed inet address to a human readable IP address string',
  ),
  'inet_pton' => 
  array (
    'return' => 'string',
    'params' => 'string ip_address',
    'description' => 'Converts a human readable IP address to a packed binary string',
  ),
  'ip2long' => 
  array (
    'return' => 'int',
    'params' => 'string ip_address',
    'description' => 'Converts a string containing an (IPv4) Internet Protocol dotted address into a proper address',
  ),
  'long2ip' => 
  array (
    'return' => 'string',
    'params' => 'int proper_address',
    'description' => 'Converts an (IPv4) Internet network address into a string in Internet standard dotted format',
  ),
  'getenv' => 
  array (
    'return' => 'string',
    'params' => 'string varname',
    'description' => 'Get the value of an environment variable',
  ),
  'putenv' => 
  array (
    'return' => 'bool',
    'params' => 'string setting',
    'description' => 'Set the value of an environment variable',
  ),
  'getopt' => 
  array (
    'return' => 'array',
    'params' => 'string options [, array longopts]',
    'description' => 'Get options from the command line argument list',
  ),
  'flush' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Flush the output buffer',
  ),
  'sleep' => 
  array (
    'return' => 'void',
    'params' => 'int seconds',
    'description' => 'Delay for a given number of seconds',
  ),
  'usleep' => 
  array (
    'return' => 'void',
    'params' => 'int micro_seconds',
    'description' => 'Delay for a given number of micro seconds',
  ),
  'time_nanosleep' => 
  array (
    'return' => 'mixed',
    'params' => 'long seconds, long nanoseconds',
    'description' => 'Delay for a number of seconds and nano seconds',
  ),
  'time_sleep_until' => 
  array (
    'return' => 'mixed',
    'params' => 'float timestamp',
    'description' => 'Make the script sleep until the specified time',
  ),
  'get_current_user' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Get the name of the owner of the current PHP script',
  ),
  'get_cfg_var' => 
  array (
    'return' => 'string',
    'params' => 'string option_name',
    'description' => 'Get the value of a PHP configuration option',
  ),
  'set_magic_quotes_runtime' => 
  array (
    'return' => 'bool',
    'params' => 'int new_setting',
    'description' => 'Set the current active configuration setting of magic_quotes_runtime and return previous',
  ),
  'get_magic_quotes_runtime' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current active configuration setting of magic_quotes_runtime',
  ),
  'get_magic_quotes_gpc' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the current active configuration setting of magic_quotes_gpc',
  ),
  'error_log' => 
  array (
    'return' => 'bool',
    'params' => 'string message [, int message_type [, string destination [, string extra_headers]]]',
    'description' => 'Send an error message somewhere',
  ),
  'call_user_func' => 
  array (
    'return' => 'mixed',
    'params' => 'string function_name [, mixed parmeter] [, mixed ...]',
    'description' => 'Call a user function which is the first parameter',
  ),
  'call_user_func_array' => 
  array (
    'return' => 'mixed',
    'params' => 'string function_name, array parameters',
    'description' => 'Call a user function which is the first parameter with the arguments contained in array',
  ),
  'call_user_method' => 
  array (
    'return' => 'mixed',
    'params' => 'string method_name, mixed object [, mixed parameter] [, mixed ...]',
    'description' => 'Call a user method on a specific object or class',
  ),
  'call_user_method_array' => 
  array (
    'return' => 'mixed',
    'params' => 'string method_name, mixed object, array params',
    'description' => 'Call a user method on a specific object or class using a parameter array',
  ),
  'register_shutdown_function' => 
  array (
    'return' => 'void',
    'params' => 'string function_name',
    'description' => 'Register a user-level function to be called on request termination',
  ),
  'highlight_file' => 
  array (
    'return' => 'bool',
    'params' => 'string file_name [, bool return] ',
    'description' => 'Syntax highlight a source file',
  ),
  'php_strip_whitespace' => 
  array (
    'return' => 'string',
    'params' => 'string file_name',
    'description' => 'Return source with stripped comments and whitespace',
  ),
  'highlight_string' => 
  array (
    'return' => 'bool',
    'params' => 'string string [, bool return] ',
    'description' => 'Syntax highlight a string or optionally return it',
  ),
  'ini_get' => 
  array (
    'return' => 'string',
    'params' => 'string varname',
    'description' => 'Get a configuration option',
  ),
  'ini_get_all' => 
  array (
    'return' => 'array',
    'params' => '[string extension]',
    'description' => 'Get all configuration options',
  ),
  'ini_set' => 
  array (
    'return' => 'string',
    'params' => 'string varname, string newvalue',
    'description' => 'Set a configuration option, returns false on error and the old value of the configuration option on success',
  ),
  'ini_restore' => 
  array (
    'return' => 'void',
    'params' => 'string varname',
    'description' => 'Restore the value of a configuration option specified by varname',
  ),
  'set_include_path' => 
  array (
    'return' => 'string',
    'params' => 'string new_include_path',
    'description' => 'Sets the include_path configuration option',
  ),
  'get_include_path' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get the current include_path configuration option',
  ),
  'restore_include_path' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Restore the value of the include_path configuration option',
  ),
  'print_r' => 
  array (
    'return' => 'mixed',
    'params' => 'mixed var [, bool return]',
    'description' => 'Prints out or returns information about the specified variable',
  ),
  'connection_aborted' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns true if client disconnected',
  ),
  'connection_status' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Returns the connection status bitfield',
  ),
  'ignore_user_abort' => 
  array (
    'return' => 'int',
    'params' => 'bool value',
    'description' => 'Set whether we want to ignore a user abort event or not',
  ),
  'getservbyname' => 
  array (
    'return' => 'int',
    'params' => 'string service, string protocol',
    'description' => 'Returns port associated with service. Protocol must be "tcp" or "udp"',
  ),
  'getservbyport' => 
  array (
    'return' => 'string',
    'params' => 'int port, string protocol',
    'description' => 'Returns service name associated with port. Protocol must be "tcp" or "udp"',
  ),
  'getprotobyname' => 
  array (
    'return' => 'int',
    'params' => 'string name',
    'description' => 'Returns protocol number associated with name as per /etc/protocols',
  ),
  'getprotobynumber' => 
  array (
    'return' => 'string',
    'params' => 'int proto',
    'description' => 'Returns protocol name associated with protocol number proto',
  ),
  'register_tick_function' => 
  array (
    'return' => 'bool',
    'params' => 'string function_name [, mixed arg [, mixed ... ]]',
    'description' => 'Registers a tick callback function',
  ),
  'unregister_tick_function' => 
  array (
    'return' => 'void',
    'params' => 'string function_name',
    'description' => 'Unregisters a tick callback function',
  ),
  'is_uploaded_file' => 
  array (
    'return' => 'bool',
    'params' => 'string path',
    'description' => 'Check if file was created by rfc1867 upload',
  ),
  'move_uploaded_file' => 
  array (
    'return' => 'bool',
    'params' => 'string path, string new_path',
    'description' => 'Move a file if and only if it was created by an upload',
  ),
  'parse_ini_file' => 
  array (
    'return' => 'array',
    'params' => 'string filename [, bool process_sections]',
    'description' => 'Parse configuration file',
  ),
  'import_request_variables' => 
  array (
    'return' => 'bool',
    'params' => 'string types [, string prefix]',
    'description' => 'Import GET/POST/Cookie variables into the global scope',
  ),
  'define_syslog_variables' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Initializes all syslog-related variables',
  ),
  'openlog' => 
  array (
    'return' => 'bool',
    'params' => 'string ident, int option, int facility',
    'description' => 'Open connection to system logger',
  ),
  'closelog' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Close connection to system logger',
  ),
  'syslog' => 
  array (
    'return' => 'bool',
    'params' => 'int priority, string message',
    'description' => 'Generate a system log message',
  ),
  'phpinfo' => 
  array (
    'return' => 'void',
    'params' => '[int what]',
    'description' => 'Output a page of useful information about PHP and the current request',
  ),
  'phpversion' => 
  array (
    'return' => 'string',
    'params' => '[string extension]',
    'description' => 'Return the current PHP version',
  ),
  'phpcredits' => 
  array (
    'return' => 'void',
    'params' => '[int flag]',
    'description' => 'Prints the list of people who\'ve contributed to the PHP project',
  ),
  'php_logo_guid' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return the special ID used to request the PHP logo in phpinfo screens',
  ),
  'php_real_logo_guid' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return the special ID used to request the PHP logo in phpinfo screens',
  ),
  'php_egg_logo_guid' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return the special ID used to request the PHP logo in phpinfo screens',
  ),
  'zend_logo_guid' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return the special ID used to request the Zend logo in phpinfo screens',
  ),
  'php_sapi_name' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return the current SAPI module name',
  ),
  'php_uname' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return information about the system PHP was built on',
  ),
  'php_ini_scanned_files' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return comma-separated string of .ini files parsed from the additional ini dir',
  ),
  'levenshtein' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2',
    'description' => 'Calculate Levenshtein distance between two strings',
  ),
  'lcg_value' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns a value from the combined linear congruential generator',
  ),
  'http_build_query' => 
  array (
    'return' => 'string',
    'params' => 'mixed formdata [, string prefix [, string arg_separator]]',
    'description' => 'Generates a form-encoded query string from an associative array or object.',
  ),
  'microtime' => 
  array (
    'return' => 'mixed',
    'params' => '[bool get_as_float]',
    'description' => 'Returns either a string or a float containing the current time in seconds and microseconds',
  ),
  'gettimeofday' => 
  array (
    'return' => 'array',
    'params' => '[bool get_as_float]',
    'description' => 'Returns the current time as array',
  ),
  'getrusage' => 
  array (
    'return' => 'array',
    'params' => '[int who]',
    'description' => 'Returns an array of usage statistics',
  ),
  'metaphone' => 
  array (
    'return' => 'string',
    'params' => 'string text, int phones',
    'description' => 'Break english phrases down into their phonemes',
  ),
  'htmlspecialchars' => 
  array (
    'return' => 'string',
    'params' => 'string string [, int quote_style]',
    'description' => 'Convert special HTML entities back to characters',
  ),
  'html_entity_decode' => 
  array (
    'return' => 'string',
    'params' => 'string string [, int quote_style][, string charset]',
    'description' => 'Convert all HTML entities to their applicable characters',
  ),
  'htmlentities' => 
  array (
    'return' => 'string',
    'params' => 'string string [, int quote_style][, string charset]',
    'description' => 'Convert all applicable characters to HTML entities',
  ),
  'get_html_translation_table' => 
  array (
    'return' => 'array',
    'params' => '[int table [, int quote_style]]',
    'description' => 'Returns the internal translation table used by htmlspecialchars and htmlentities',
  ),
  'stream_bucket_make_writeable' => 
  array (
    'return' => 'object',
    'params' => 'resource brigade',
    'description' => 'Return a bucket object from the brigade for operating on',
  ),
  'stream_bucket_prepend' => 
  array (
    'return' => 'void',
    'params' => 'resource brigade, resource bucket',
    'description' => 'Prepend bucket to brigade',
  ),
  'stream_bucket_append' => 
  array (
    'return' => 'void',
    'params' => 'resource brigade, resource bucket',
    'description' => 'Append bucket to brigade',
  ),
  'stream_bucket_new' => 
  array (
    'return' => 'resource',
    'params' => 'resource stream, string buffer',
    'description' => 'Create a new bucket for use on the current stream',
  ),
  'stream_get_filters' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns a list of registered filters',
  ),
  'stream_filter_register' => 
  array (
    'return' => 'bool',
    'params' => 'string filtername, string classname',
    'description' => 'Registers a custom filter handler class',
  ),
  'sha1' => 
  array (
    'return' => 'string',
    'params' => 'string str [, bool raw_output]',
    'description' => 'Calculate the sha1 hash of a string',
  ),
  'sha1_file' => 
  array (
    'return' => 'string',
    'params' => 'string filename [, bool raw_output]',
    'description' => 'Calculate the sha1 hash of given filename',
  ),
  'image_type_to_mime_type' => 
  array (
    'return' => 'string',
    'params' => 'int imagetype',
    'description' => 'Get Mime-Type for image-type returned by getimagesize, exif_read_data, exif_thumbnail, exif_imagetype',
  ),
  'image_type_to_extension' => 
  array (
    'return' => 'string',
    'params' => 'int imagetype [, bool include_dot]',
    'description' => 'Get file extension for image-type returned by getimagesize, exif_read_data, exif_thumbnail, exif_imagetype',
  ),
  'getimagesize' => 
  array (
    'return' => 'array',
    'params' => 'string imagefile [, array info]',
    'description' => 'Get the size of an image as 4-element array',
  ),
  'uniqid' => 
  array (
    'return' => 'string',
    'params' => '[string prefix , bool more_entropy]',
    'description' => 'Generates a unique ID',
  ),
  'parse_url' => 
  array (
    'return' => 'mixed',
    'params' => 'string url, [int url_component]',
    'description' => 'Parse a URL and return its components',
  ),
  'urlencode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'URL-encodes string',
  ),
  'urldecode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Decodes URL-encoded string',
  ),
  'rawurlencode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'URL-encodes string',
  ),
  'rawurldecode' => 
  array (
    'return' => 'string',
    'params' => 'string str',
    'description' => 'Decodes URL-encodes string',
  ),
  'get_headers' => 
  array (
    'return' => 'array',
    'params' => 'string url',
    'description' => 'fetches all the headers sent by the server in response to a HTTP request',
  ),
  'uuencode' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'uuencode a string',
  ),
  'uudecode' => 
  array (
    'return' => 'string',
    'params' => 'string data',
    'description' => 'decode a uuencoded string',
  ),
  'flock' => 
  array (
    'return' => 'bool',
    'params' => 'resource fp, int operation [, int &wouldblock]',
    'description' => 'Portable file locking',
  ),
  'get_meta_tags' => 
  array (
    'return' => 'array',
    'params' => 'string filename [, bool use_include_path]',
    'description' => 'Extracts all meta tag content attributes from a file and returns an array',
  ),
  'file_get_contents' => 
  array (
    'return' => 'string',
    'params' => 'string filename [, bool use_include_path [, resource context [, long offset [, long maxlen]]]]',
    'description' => 'Read the entire file into a string',
  ),
  'file_put_contents' => 
  array (
    'return' => 'int',
    'params' => 'string file, mixed data [, int flags [, resource context]]',
    'description' => 'Write/Create a file with contents data and return the number of bytes written',
  ),
  'file' => 
  array (
    'return' => 'array',
    'params' => 'string filename [, int flags[, resource context]]',
    'description' => 'Read entire file into an array',
  ),
  'tempnam' => 
  array (
    'return' => 'string',
    'params' => 'string dir, string prefix',
    'description' => 'Create a unique filename in a directory',
  ),
  'tmpfile' => 
  array (
    'return' => 'resource',
    'params' => 'void',
    'description' => 'Create a temporary file that will be deleted automatically after use',
  ),
  'fopen' => 
  array (
    'return' => 'resource',
    'params' => 'string filename, string mode [, bool use_include_path [, resource context]]',
    'description' => 'Open a file or a URL and return a file pointer',
  ),
  'fclose' => 
  array (
    'return' => 'bool',
    'params' => 'resource fp',
    'description' => 'Close an open file pointer',
  ),
  'popen' => 
  array (
    'return' => 'resource',
    'params' => 'string command, string mode',
    'description' => 'Execute a command and open either a read or a write pipe to it',
  ),
  'pclose' => 
  array (
    'return' => 'int',
    'params' => 'resource fp',
    'description' => 'Close a file pointer opened by popen()',
  ),
  'feof' => 
  array (
    'return' => 'bool',
    'params' => 'resource fp',
    'description' => 'Test for end-of-file on a file pointer',
  ),
  'fgets' => 
  array (
    'return' => 'string',
    'params' => 'resource fp[, int length]',
    'description' => 'Get a line from file pointer',
  ),
  'fgetc' => 
  array (
    'return' => 'string',
    'params' => 'resource fp',
    'description' => 'Get a character from file pointer',
  ),
  'fgetss' => 
  array (
    'return' => 'string',
    'params' => 'resource fp [, int length, string allowable_tags]',
    'description' => 'Get a line from file pointer and strip HTML tags',
  ),
  'fscanf' => 
  array (
    'return' => 'mixed',
    'params' => 'resource stream, string format [, string ...]',
    'description' => 'Implements a mostly ANSI compatible fscanf()',
  ),
  'fwrite' => 
  array (
    'return' => 'int',
    'params' => 'resource fp, string str [, int length]',
    'description' => 'Binary-safe file write',
  ),
  'fflush' => 
  array (
    'return' => 'bool',
    'params' => 'resource fp',
    'description' => 'Flushes output',
  ),
  'rewind' => 
  array (
    'return' => 'bool',
    'params' => 'resource fp',
    'description' => 'Rewind the position of a file pointer',
  ),
  'ftell' => 
  array (
    'return' => 'int',
    'params' => 'resource fp',
    'description' => 'Get file pointer\'s read/write position',
  ),
  'fseek' => 
  array (
    'return' => 'int',
    'params' => 'resource fp, int offset [, int whence]',
    'description' => 'Seek on a file pointer',
  ),
  'mkdir' => 
  array (
    'return' => 'bool',
    'params' => 'string pathname [, int mode [, bool recursive [, resource context]]]',
    'description' => 'Create a directory',
  ),
  'rmdir' => 
  array (
    'return' => 'bool',
    'params' => 'string dirname[, resource context]',
    'description' => 'Remove a directory',
  ),
  'readfile' => 
  array (
    'return' => 'int',
    'params' => 'string filename [, bool use_include_path[, resource context]]',
    'description' => 'Output a file or a URL',
  ),
  'umask' => 
  array (
    'return' => 'int',
    'params' => '[int mask]',
    'description' => 'Return or change the umask',
  ),
  'fpassthru' => 
  array (
    'return' => 'int',
    'params' => 'resource fp',
    'description' => 'Output all remaining data from a file pointer',
  ),
  'rename' => 
  array (
    'return' => 'bool',
    'params' => 'string old_name, string new_name[, resource context]',
    'description' => 'Rename a file',
  ),
  'unlink' => 
  array (
    'return' => 'bool',
    'params' => 'string filename[, context context]',
    'description' => 'Delete a file',
  ),
  'ftruncate' => 
  array (
    'return' => 'bool',
    'params' => 'resource fp, int size',
    'description' => 'Truncate file to \'size\' length',
  ),
  'fstat' => 
  array (
    'return' => 'array',
    'params' => 'resource fp',
    'description' => 'Stat() on a filehandle',
  ),
  'copy' => 
  array (
    'return' => 'bool',
    'params' => 'string source_file, string destination_file',
    'description' => 'Copy a file',
  ),
  'fread' => 
  array (
    'return' => 'string',
    'params' => 'resource fp, int length',
    'description' => 'Binary-safe file read',
  ),
  'fputcsv' => 
  array (
    'return' => 'int',
    'params' => 'resource fp, array fields [, string delimiter [, string enclosure]]',
    'description' => 'Format line as CSV and write to file pointer',
  ),
  'fgetcsv' => 
  array (
    'return' => 'array',
    'params' => 'resource fp [,int length [, string delimiter [, string enclosure]]]',
    'description' => 'Get line from file pointer and parse for CSV fields',
  ),
  'realpath' => 
  array (
    'return' => 'string',
    'params' => 'string path',
    'description' => 'Return the resolved path',
  ),
  'fnmatch' => 
  array (
    'return' => 'bool',
    'params' => 'string pattern, string filename [, int flags]',
    'description' => 'Match filename against pattern',
  ),
  'xmlwriter_set_indent' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, bool indent',
    'description' => 'Toggle indentation on/off - returns FALSE on error',
  ),
  'xmlwriter_set_indent_string' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string indentString',
    'description' => 'Set string used for indenting - returns FALSE on error',
  ),
  'xmlwriter_start_attribute' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name',
    'description' => 'Create start attribute - returns FALSE on error',
  ),
  'xmlwriter_end_attribute' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End attribute - returns FALSE on error',
  ),
  'xmlwriter_start_attribute_ns' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string prefix, string name, string uri',
    'description' => 'Create start namespaced attribute - returns FALSE on error',
  ),
  'xmlwriter_write_attribute' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string content',
    'description' => 'Write full attribute - returns FALSE on error',
  ),
  'xmlwriter_write_attribute_ns' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string prefix, string name, string uri, string content',
    'description' => 'Write full namespaced attribute - returns FALSE on error',
  ),
  'xmlwriter_start_element' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name',
    'description' => 'Create start element tag - returns FALSE on error',
  ),
  'xmlwriter_start_element_ns' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string prefix, string name, string uri',
    'description' => 'Create start namespaced element tag - returns FALSE on error',
  ),
  'xmlwriter_end_element' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current element - returns FALSE on error',
  ),
  'xmlwriter_write_element' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string content',
    'description' => 'Write full element tag - returns FALSE on error',
  ),
  'xmlwriter_write_element_ns' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string prefix, string name, string uri, string content',
    'description' => 'Write full namesapced element tag - returns FALSE on error',
  ),
  'xmlwriter_start_pi' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string target',
    'description' => 'Create start PI tag - returns FALSE on error',
  ),
  'xmlwriter_end_pi' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current PI - returns FALSE on error',
  ),
  'xmlwriter_write_pi' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string target, string content',
    'description' => 'Write full PI tag - returns FALSE on error',
  ),
  'xmlwriter_start_cdata' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'Create start CDATA tag - returns FALSE on error',
  ),
  'xmlwriter_end_cdata' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current CDATA - returns FALSE on error',
  ),
  'xmlwriter_write_cdata' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string content',
    'description' => 'Write full CDATA tag - returns FALSE on error',
  ),
  'xmlwriter_text' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string content',
    'description' => 'Write text - returns FALSE on error',
  ),
  'xmlwriter_start_comment' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'Create start comment - returns FALSE on error',
  ),
  'xmlwriter_end_comment' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'Create end comment - returns FALSE on error',
  ),
  'xmlwriter_write_comment' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string content',
    'description' => 'Write full comment tag - returns FALSE on error',
  ),
  'xmlwriter_start_document' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string version, string encoding, string standalone',
    'description' => 'Create document tag - returns FALSE on error',
  ),
  'xmlwriter_end_document' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current document - returns FALSE on error',
  ),
  'xmlwriter_start_dtd' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string pubid, string sysid',
    'description' => 'Create start DTD tag - returns FALSE on error',
  ),
  'xmlwriter_end_dtd' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current DTD - returns FALSE on error',
  ),
  'xmlwriter_write_dtd' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string pubid, string sysid, string subset',
    'description' => 'Write full DTD tag - returns FALSE on error',
  ),
  'xmlwriter_start_dtd_element' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name',
    'description' => 'Create start DTD element - returns FALSE on error',
  ),
  'xmlwriter_end_dtd_element' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current DTD element - returns FALSE on error',
  ),
  'xmlwriter_write_dtd_element' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string content',
    'description' => 'Write full DTD element tag - returns FALSE on error',
  ),
  'xmlwriter_start_dtd_attlist' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name',
    'description' => 'Create start DTD AttList - returns FALSE on error',
  ),
  'xmlwriter_end_dtd_attlist' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current DTD AttList - returns FALSE on error',
  ),
  'xmlwriter_write_dtd_attlist' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string content',
    'description' => 'Write full DTD AttList tag - returns FALSE on error',
  ),
  'xmlwriter_start_dtd_entity' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, bool isparam',
    'description' => 'Create start DTD Entity - returns FALSE on error',
  ),
  'xmlwriter_end_dtd_entity' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter',
    'description' => 'End current DTD Entity - returns FALSE on error',
  ),
  'xmlwriter_write_dtd_entity' => 
  array (
    'return' => 'bool',
    'params' => 'resource xmlwriter, string name, string content',
    'description' => 'Write full DTD Entity tag - returns FALSE on error',
  ),
  'xmlwriter_open_uri' => 
  array (
    'return' => 'resource',
    'params' => 'resource xmlwriter, string source',
    'description' => 'Create new xmlwriter using source uri for output',
  ),
  'xmlwriter_open_memory' => 
  array (
    'return' => 'resource',
    'params' => '',
    'description' => 'Create new xmlwriter using memory for string output',
  ),
  'xmlwriter_output_memory' => 
  array (
    'return' => 'string',
    'params' => 'resource xmlwriter [,bool flush]',
    'description' => 'Output current buffer as string',
  ),
  'xmlwriter_flush' => 
  array (
    'return' => 'mixed',
    'params' => 'resource xmlwriter [,bool empty]',
    'description' => 'Output current buffer',
  ),
  'easter_date' => 
  array (
    'return' => 'int',
    'params' => '[int year]',
    'description' => 'Return the timestamp of midnight on Easter of a given year (defaults to current year)',
  ),
  'easter_days' => 
  array (
    'return' => 'int',
    'params' => '[int year, [int method]]',
    'description' => 'Return the number of days after March 21 that Easter falls on for a given year (defaults to current year)',
  ),
  'cal_info' => 
  array (
    'return' => 'array',
    'params' => 'int calendar',
    'description' => 'Returns information about a particular calendar',
  ),
  'cal_days_in_month' => 
  array (
    'return' => 'int',
    'params' => 'int calendar, int month, int year',
    'description' => 'Returns the number of days in a month for a given year and calendar',
  ),
  'cal_to_jd' => 
  array (
    'return' => 'int',
    'params' => 'int calendar, int month, int day, int year',
    'description' => 'Converts from a supported calendar to Julian Day Count',
  ),
  'cal_from_jd' => 
  array (
    'return' => 'array',
    'params' => 'int jd, int calendar',
    'description' => 'Converts from Julian Day Count to a supported calendar and return extended information',
  ),
  'jdtogregorian' => 
  array (
    'return' => 'string',
    'params' => 'int juliandaycount',
    'description' => 'Converts a julian day count to a gregorian calendar date',
  ),
  'gregoriantojd' => 
  array (
    'return' => 'int',
    'params' => 'int month, int day, int year',
    'description' => 'Converts a gregorian calendar date to julian day count',
  ),
  'jdtojulian' => 
  array (
    'return' => 'string',
    'params' => 'int juliandaycount',
    'description' => 'Convert a julian day count to a julian calendar date',
  ),
  'juliantojd' => 
  array (
    'return' => 'int',
    'params' => 'int month, int day, int year',
    'description' => 'Converts a julian calendar date to julian day count',
  ),
  'jdtojewish' => 
  array (
    'return' => 'string',
    'params' => 'int juliandaycount [, bool hebrew [, int fl]]',
    'description' => 'Converts a julian day count to a jewish calendar date',
  ),
  'jewishtojd' => 
  array (
    'return' => 'int',
    'params' => 'int month, int day, int year',
    'description' => 'Converts a jewish calendar date to a julian day count',
  ),
  'jdtofrench' => 
  array (
    'return' => 'string',
    'params' => 'int juliandaycount',
    'description' => 'Converts a julian day count to a french republic calendar date',
  ),
  'frenchtojd' => 
  array (
    'return' => 'int',
    'params' => 'int month, int day, int year',
    'description' => 'Converts a french republic calendar date to julian day count',
  ),
  'jddayofweek' => 
  array (
    'return' => 'mixed',
    'params' => 'int juliandaycount [, int mode]',
    'description' => 'Returns name or number of day of week from julian day count',
  ),
  'jdmonthname' => 
  array (
    'return' => 'string',
    'params' => 'int juliandaycount, int mode',
    'description' => 'Returns name of month for julian day count',
  ),
  'unixtojd' => 
  array (
    'return' => 'int',
    'params' => '[int timestamp]',
    'description' => 'Convert UNIX timestamp to Julian Day',
  ),
  'jdtounix' => 
  array (
    'return' => 'int',
    'params' => 'int jday',
    'description' => 'Convert Julian Day to UNIX timestamp',
  ),
  'mime_content_type' => 
  array (
    'return' => 'string',
    'params' => 'string filename|resource stream',
    'description' => 'Return content-type for file',
  ),
  'exif_tagname' => 
  array (
    'return' => 'string',
    'params' => 'index',
    'description' => 'Get headername for index or false if not defined',
  ),
  'exif_read_data' => 
  array (
    'return' => 'array',
    'params' => 'string filename [, sections_needed [, sub_arrays[, read_thumbnail]]]',
    'description' => 'Reads header data from the JPEG/TIFF image filename and optionally reads the internal thumbnails',
  ),
  'exif_thumbnail' => 
  array (
    'return' => 'string',
    'params' => 'string filename [, &width, &height [, &imagetype]]',
    'description' => 'Reads the embedded thumbnail',
  ),
  'exif_imagetype' => 
  array (
    'return' => 'int',
    'params' => 'string imagefile',
    'description' => 'Get the type of an image',
  ),
  'ming_setscale' => 
  array (
    'return' => 'void',
    'params' => 'int scale',
    'description' => 'Set scale (?)',
  ),
  'ming_useswfversion' => 
  array (
    'return' => 'void',
    'params' => 'int version',
    'description' => 'Use SWF version (?)',
  ),
  'ming_useconstants' => 
  array (
    'return' => 'void',
    'params' => 'int use',
    'description' => 'Use constant pool (?)',
  ),
  'swfaction::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string',
    'description' => 'Creates a new SWFAction object, compiling the given script',
  ),
  'swfbitmap::__construct' => 
  array (
    'return' => 'void',
    'params' => 'mixed file [, mixed maskfile]',
    'description' => 'Creates a new SWFBitmap object from jpg (with optional mask) or dbl file',
  ),
  'swfbitmap::getWidth' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the width of this bitmap',
  ),
  'swfbitmap::getHeight' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the height of this bitmap',
  ),
  'swfbutton::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates a new SWFButton object',
  ),
  'swfbutton::setHit' => 
  array (
    'return' => 'void',
    'params' => 'object SWFCharacter',
    'description' => 'Sets the character for this button\'s hit test state',
  ),
  'swfbutton::setOver' => 
  array (
    'return' => 'void',
    'params' => 'object SWFCharacter',
    'description' => 'Sets the character for this button\'s over state',
  ),
  'swfbutton::setUp' => 
  array (
    'return' => 'void',
    'params' => 'object SWFCharacter',
    'description' => 'Sets the character for this button\'s up state',
  ),
  'swfbutton::setDown' => 
  array (
    'return' => 'void',
    'params' => 'object SWFCharacter',
    'description' => 'Sets the character for this button\'s down state',
  ),
  'swfbutton::addShape' => 
  array (
    'return' => 'void',
    'params' => 'object SWFCharacter, int flags',
    'description' => 'Sets the character to display for the condition described in flags',
  ),
  'swfbutton::setMenu' => 
  array (
    'return' => 'void',
    'params' => 'int flag',
    'description' => 'enable track as menu button behaviour',
  ),
  'swfbutton::setAction' => 
  array (
    'return' => 'void',
    'params' => 'object SWFAction',
    'description' => 'Sets the action to perform when button is pressed',
  ),
  'swfbutton::addASound' => 
  array (
    'return' => 'SWFSoundInstance',
    'params' => 'SWFSound sound, int flags',
    'description' => 'associates a sound with a button transitionNOTE: the transitions are all wrong _UP, _OVER, _DOWN _HIT',
  ),
  'swfbutton::addAction' => 
  array (
    'return' => 'void',
    'params' => 'object SWFAction, int flags',
    'description' => 'Sets the action to perform when conditions described in flags is met',
  ),
  'ming_keypress' => 
  array (
    'return' => 'int',
    'params' => 'string str',
    'description' => 'Returns the action flag for keyPress(char)',
  ),
  'swfdisplayitem::moveTo' => 
  array (
    'return' => 'void',
    'params' => 'int x, int y',
    'description' => 'Moves this SWFDisplayItem to movie coordinates (x, y)',
  ),
  'swfdisplayitem::move' => 
  array (
    'return' => 'void',
    'params' => 'float dx, float dy',
    'description' => 'Displaces this SWFDisplayItem by (dx, dy) in movie coordinates',
  ),
  'swfdisplayitem::scaleTo' => 
  array (
    'return' => 'void',
    'params' => 'float xScale [, float yScale]',
    'description' => 'Scales this SWFDisplayItem by xScale in the x direction, yScale in the y, or both to xScale if only one arg',
  ),
  'swfdisplayitem::scale' => 
  array (
    'return' => 'void',
    'params' => 'float xScale, float yScale',
    'description' => 'Multiplies this SWFDisplayItem\'s current x scale by xScale, its y scale by yScale',
  ),
  'swfdisplayitem::rotateTo' => 
  array (
    'return' => 'void',
    'params' => 'float degrees',
    'description' => 'Rotates this SWFDisplayItem the given (clockwise) degrees from its original orientation',
  ),
  'swfdisplayitem::rotate' => 
  array (
    'return' => 'void',
    'params' => 'float degrees',
    'description' => 'Rotates this SWFDisplayItem the given (clockwise) degrees from its current orientation',
  ),
  'swfdisplayitem::skewXTo' => 
  array (
    'return' => 'void',
    'params' => 'float xSkew',
    'description' => 'Sets this SWFDisplayItem\'s x skew value to xSkew',
  ),
  'swfdisplayitem::skewX' => 
  array (
    'return' => 'void',
    'params' => 'float xSkew',
    'description' => 'Adds xSkew to this SWFDisplayItem\'s x skew value',
  ),
  'swfdisplayitem::skewYTo' => 
  array (
    'return' => 'void',
    'params' => 'float ySkew',
    'description' => 'Sets this SWFDisplayItem\'s y skew value to ySkew',
  ),
  'swfdisplayitem::skewY' => 
  array (
    'return' => 'void',
    'params' => 'float ySkew',
    'description' => 'Adds ySkew to this SWFDisplayItem\'s y skew value',
  ),
  'swfdisplayitem::setMatrix' => 
  array (
    'return' => 'void',
    'params' => 'float a, float b, float c, float d, float x, float y',
    'description' => 'Sets the item\'s transform matrix',
  ),
  'swfdisplayitem::setDepth' => 
  array (
    'return' => 'void',
    'params' => 'int depth',
    'description' => 'Sets this SWFDisplayItem\'s z-depth to depth.  Items with higher depth values are drawn on top of those with lower values',
  ),
  'swfdisplayitem::setRatio' => 
  array (
    'return' => 'void',
    'params' => 'float ratio',
    'description' => 'Sets this SWFDisplayItem\'s ratio to ratio.  Obviously only does anything if displayitem was created from an SWFMorph',
  ),
  'swfdisplayitem::addColor' => 
  array (
    'return' => 'void',
    'params' => 'int r, int g, int b [, int a]',
    'description' => 'Sets the add color part of this SWFDisplayItem\'s CXform to (r, g, b [, a]), a defaults to 0',
  ),
  'swfdisplayitem::multColor' => 
  array (
    'return' => 'void',
    'params' => 'float r, float g, float b [, float a]',
    'description' => 'Sets the multiply color part of this SWFDisplayItem\'s CXform to (r, g, b [, a]), a defaults to 1.0',
  ),
  'swfdisplayitem::setName' => 
  array (
    'return' => 'void',
    'params' => 'string name',
    'description' => 'Sets this SWFDisplayItem\'s name to name',
  ),
  'swfdisplayitem::addAction' => 
  array (
    'return' => 'void',
    'params' => 'object SWFAction, int flags',
    'description' => 'Adds this SWFAction to the given SWFSprite instance',
  ),
  'swfdisplayitem::setMaskLevel' => 
  array (
    'return' => 'void',
    'params' => 'int level',
    'description' => 'defines a MASK layer at level',
  ),
  'swfdisplayitem::endMask' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'another way of defining a MASK layer',
  ),
  'swffill::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates a new SWFFill object',
  ),
  'swffill::moveTo' => 
  array (
    'return' => 'void',
    'params' => 'float x, float y',
    'description' => 'Moves this SWFFill to shape coordinates (x,y)',
  ),
  'swffill::scaleTo' => 
  array (
    'return' => 'void',
    'params' => 'float xScale [, float yScale]',
    'description' => 'Scales this SWFFill by xScale in the x direction, yScale in the y, or both to xScale if only one arg',
  ),
  'swffill::rotateTo' => 
  array (
    'return' => 'void',
    'params' => 'float degrees',
    'description' => 'Rotates this SWFFill the given (clockwise) degrees from its original orientation',
  ),
  'swffill::skewXTo' => 
  array (
    'return' => 'void',
    'params' => 'float xSkew',
    'description' => 'Sets this SWFFill\'s x skew value to xSkew',
  ),
  'swffill::skewYTo' => 
  array (
    'return' => 'void',
    'params' => 'float ySkew',
    'description' => 'Sets this SWFFill\'s y skew value to ySkew',
  ),
  'swffontcha::raddChars' => 
  array (
    'return' => 'void',
    'params' => 'string',
    'description' => 'adds characters to a font for exporting font',
  ),
  'swffontchar::addChars' => 
  array (
    'return' => 'void',
    'params' => 'string',
    'description' => 'adds characters to a font for exporting font',
  ),
  'swffont::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string filename',
    'description' => 'Creates a new SWFFont object from given file',
  ),
  'swffont::getWidth' => 
  array (
    'return' => 'float',
    'params' => 'string str',
    'description' => 'Calculates the width of the given string in this font at full height',
  ),
  'swffont::getUTF8Width' => 
  array (
    'return' => 'int',
    'params' => 'string',
    'description' => 'Calculates the width of the given string in this font at full height',
  ),
  'swffont::getWideWidth' => 
  array (
    'return' => 'int',
    'params' => 'string',
    'description' => 'Calculates the width of the given string in this font at full height',
  ),
  'swffont::getAscent' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the ascent of the font, or 0 if not available',
  ),
  'swffont::getDescent' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the descent of the font, or 0 if not available',
  ),
  'swffont::getLeading' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the leading of the font, or 0 if not available',
  ),
  'swffont::addChars' => 
  array (
    'return' => 'void',
    'params' => 'string',
    'description' => 'adds characters to a font required within textfields',
  ),
  'swffont::getShape' => 
  array (
    'return' => 'string',
    'params' => 'code',
    'description' => 'Returns the glyph shape of a char as a text string',
  ),
  'swfgradient::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates a new SWFGradient object',
  ),
  'swfgradient::addEntry' => 
  array (
    'return' => 'void',
    'params' => 'float ratio, int r, int g, int b [, int a]',
    'description' => 'Adds given entry to the gradient',
  ),
  'swfmorph::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates a new SWFMorph object',
  ),
  'swfmorph::getShape1' => 
  array (
    'return' => 'object',
    'params' => '',
    'description' => 'Return\'s this SWFMorph\'s start shape object',
  ),
  'swfmorph::getShape2' => 
  array (
    'return' => 'object',
    'params' => '',
    'description' => 'Return\'s this SWFMorph\'s start shape object',
  ),
  'swfsound::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string filename, int flags',
    'description' => 'Creates a new SWFSound object from given file',
  ),
  'swfvideostream_init' => 
  array (
    'return' => 'class',
    'params' => '[file]',
    'description' => 'Returns a SWVideoStream object',
  ),
  'swfprebuiltclip_init' => 
  array (
    'return' => 'class',
    'params' => '[file]',
    'description' => 'Returns a SWFPrebuiltClip object',
  ),
  'swfmovie::__construct' => 
  array (
    'return' => 'void',
    'params' => 'int version',
    'description' => 'Creates swfmovie object according to the passed version',
  ),
  'swfmovie::nextframe' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => '',
  ),
  'swfmovie::labelframe' => 
  array (
    'return' => 'void',
    'params' => 'object SWFBlock',
    'description' => '',
  ),
  'swfmovie::add' => 
  array (
    'return' => 'object',
    'params' => 'object SWFBlock',
    'description' => '',
  ),
  'swfmovie::output' => 
  array (
    'return' => 'int',
    'params' => '[int compression]',
    'description' => '',
  ),
  'swfmovie::saveToFile' => 
  array (
    'return' => 'int',
    'params' => 'stream x [, int compression]',
    'description' => '',
  ),
  'swfmovie::save' => 
  array (
    'return' => 'int',
    'params' => 'mixed where [, int compression]',
    'description' => 'Saves the movie. \'where\' can be stream and the movie will be saved there otherwise it is treated as string and written in file with that name',
  ),
  'swfmovie::setBackground' => 
  array (
    'return' => 'void',
    'params' => 'int r, int g, int b',
    'description' => 'Sets background color (r,g,b)',
  ),
  'swfmovie::setRate' => 
  array (
    'return' => 'void',
    'params' => 'float rate',
    'description' => 'Sets movie rate',
  ),
  'swfmovie::setDimension' => 
  array (
    'return' => 'void',
    'params' => 'float x, float y',
    'description' => 'Sets movie dimension',
  ),
  'swfmovie::setFrames' => 
  array (
    'return' => 'void',
    'params' => 'int frames',
    'description' => 'Sets number of frames',
  ),
  'swfmovie::streamMP3' => 
  array (
    'return' => 'void',
    'params' => 'mixed file',
    'description' => 'Sets sound stream of the SWF movie. The parameter can be stream or string.',
  ),
  'swfshape::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates a new SWFShape object',
  ),
  'swfshape::setline' => 
  array (
    'return' => 'void',
    'params' => 'int width, int r, int g, int b [, int a]',
    'description' => 'Sets the current line style for this SWFShape',
  ),
  'swfshape::addfill' => 
  array (
    'return' => 'object',
    'params' => 'mixed arg1, int arg2, [int b [, int a]]',
    'description' => 'Returns a fill object, for use with swfshape_setleftfill and swfshape_setrightfill. If 1 or 2 parameter(s) is (are) passed first should be object (from gradient class) and the second int (flags). Gradient fill is performed. If 3 or 4 parameters are passed : r, g, b [, a]. Solid fill is performed.',
  ),
  'swfshape::setleftfill' => 
  array (
    'return' => 'void',
    'params' => 'int arg1 [, int g ,int b [,int a]]',
    'description' => 'Sets the right side fill style to fill in case only one parameter is passed. When 3 or 4 parameters are passed they are treated as : int r, int g, int b, int a . Solid fill is performed in this case before setting right side fill type.',
  ),
  'swfshape::movepento' => 
  array (
    'return' => 'void',
    'params' => 'float x, float y',
    'description' => 'Moves the pen to shape coordinates (x, y)',
  ),
  'swfshape::movepen' => 
  array (
    'return' => 'void',
    'params' => 'float x, float y',
    'description' => 'Moves the pen from its current location by vector (x, y)',
  ),
  'swfshape::drawlineto' => 
  array (
    'return' => 'void',
    'params' => 'float x, float y',
    'description' => 'Draws a line from the current pen position to shape coordinates (x, y) in the current line style',
  ),
  'swfshape::drawline' => 
  array (
    'return' => 'void',
    'params' => 'float dx, float dy',
    'description' => 'Draws a line from the current pen position (x, y) to the point (x+dx, y+dy) in the current line style',
  ),
  'swfshape::drawcurveto' => 
  array (
    'return' => 'void',
    'params' => 'float ax, float ay, float bx, float by [, float dx, float dy]',
    'description' => 'Draws a curve from the current pen position (x,y) to the point (bx, by) in the current line style, using point (ax, ay) as a control point. Or draws a cubic bezier to point (dx, dy) with control points (ax, ay) and (bx, by)',
  ),
  'swfshape::drawcurve' => 
  array (
    'return' => 'void',
    'params' => 'float adx, float ady, float bdx, float bdy [, float cdx, float cdy]',
    'description' => 'Draws a curve from the current pen position (x, y) to the point (x+bdx, y+bdy) in the current line style, using point (x+adx, y+ady) as a control point or draws a cubic bezier to point (x+cdx, x+cdy) with control points (x+adx, y+ady) and (x+bdx, y+bdy)',
  ),
  'swfshape::drawglyph' => 
  array (
    'return' => 'void',
    'params' => 'SWFFont font, string character [, int size]',
    'description' => 'Draws the first character in the given string into the shape using the glyph definition from the given font',
  ),
  'swfshape::drawcircle' => 
  array (
    'return' => 'void',
    'params' => 'float r',
    'description' => 'Draws a circle of radius r centered at the current location, in a counter-clockwise fashion',
  ),
  'swfshape::drawarc' => 
  array (
    'return' => 'void',
    'params' => 'float r, float startAngle, float endAngle',
    'description' => 'Draws an arc of radius r centered at the current location, from angle startAngle to angle endAngle measured clockwise from 12 o\'clock',
  ),
  'swfshape::drawcubic' => 
  array (
    'return' => 'void',
    'params' => 'float bx, float by, float cx, float cy, float dx, float dy',
    'description' => 'Draws a cubic bezier curve using the current position and the three given points as control points',
  ),
  'swfsprite::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates a new SWFSprite object',
  ),
  'swfsprite::add' => 
  array (
    'return' => 'object',
    'params' => 'object SWFCharacter',
    'description' => 'Adds the character to the sprite, returns a displayitem object',
  ),
  'swfsprite::remove' => 
  array (
    'return' => 'void',
    'params' => 'object SWFDisplayItem',
    'description' => 'Remove the named character from the sprite\'s display list',
  ),
  'swfsprite::nextFrame' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Moves the sprite to the next frame',
  ),
  'swfsprite::labelFrame' => 
  array (
    'return' => 'void',
    'params' => 'string label',
    'description' => 'Labels frame',
  ),
  'swfsprite::setFrames' => 
  array (
    'return' => 'void',
    'params' => 'int frames',
    'description' => 'Sets the number of frames in this SWFSprite',
  ),
  'swftext::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Creates new SWFText object',
  ),
  'swftext::setFont' => 
  array (
    'return' => 'void',
    'params' => 'object font',
    'description' => 'Sets this SWFText object\'s current font to given font',
  ),
  'swftext::setHeight' => 
  array (
    'return' => 'void',
    'params' => 'float height',
    'description' => 'Sets this SWFText object\'s current height to given height',
  ),
  'swftext::setSpacing' => 
  array (
    'return' => 'void',
    'params' => 'float spacing',
    'description' => 'Sets this SWFText object\'s current letterspacing to given spacing',
  ),
  'swftext::setColor' => 
  array (
    'return' => 'void',
    'params' => 'int r, int g, int b [, int a]',
    'description' => 'Sets this SWFText object\'s current color to the given color',
  ),
  'swftext::moveTo' => 
  array (
    'return' => 'void',
    'params' => 'float x, float y',
    'description' => 'Moves this SWFText object\'s current pen position to (x, y) in text coordinates',
  ),
  'swftext::addString' => 
  array (
    'return' => 'void',
    'params' => 'string text',
    'description' => 'Writes the given text into this SWFText object at the current pen position, using the current font, height, spacing, and color',
  ),
  'swftext::addUTF8String' => 
  array (
    'return' => 'void',
    'params' => 'string text',
    'description' => 'Writes the given text into this SWFText object at the current pen position,using the current font, height, spacing, and color',
  ),
  'swftext::addWideString' => 
  array (
    'return' => 'void',
    'params' => 'string text',
    'description' => 'Writes the given text into this SWFText object at the current pen position,using the current font, height, spacing, and color',
  ),
  'swftext::getWidth' => 
  array (
    'return' => 'float',
    'params' => 'string str',
    'description' => 'Calculates the width of the given string in this text objects current font and size',
  ),
  'swftext::getUTF8Width' => 
  array (
    'return' => 'double',
    'params' => 'string',
    'description' => 'calculates the width of the given string in this text objects current font and size',
  ),
  'swftext::getWideWidth' => 
  array (
    'return' => 'double',
    'params' => 'string',
    'description' => 'calculates the width of the given string in this text objects current font and size',
  ),
  'swftext::getAscent' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the ascent of the current font at its current size, or 0 if not available',
  ),
  'swftext::getDescent' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the descent of the current font at its current size, or 0 if not available',
  ),
  'swftext::getLeading' => 
  array (
    'return' => 'float',
    'params' => '',
    'description' => 'Returns the leading of the current font at its current size, or 0 if not available',
  ),
  'swftextfield::__construct' => 
  array (
    'return' => 'void',
    'params' => '[int flags]',
    'description' => 'Creates a new SWFTextField object',
  ),
  'swftextfield::setFont' => 
  array (
    'return' => 'void',
    'params' => 'object font',
    'description' => 'Sets the font for this textfield',
  ),
  'swftextfield::setBounds' => 
  array (
    'return' => 'void',
    'params' => 'float width, float height',
    'description' => 'Sets the width and height of this textfield',
  ),
  'swftextfield::align' => 
  array (
    'return' => 'void',
    'params' => 'int alignment',
    'description' => 'Sets the alignment of this textfield',
  ),
  'swftextfield::setHeight' => 
  array (
    'return' => 'void',
    'params' => 'float height',
    'description' => 'Sets the font height of this textfield',
  ),
  'swftextfield::setLeftMargin' => 
  array (
    'return' => 'void',
    'params' => 'float margin',
    'description' => 'Sets the left margin of this textfield',
  ),
  'swftextfield::setRightMargin' => 
  array (
    'return' => 'void',
    'params' => 'float margin',
    'description' => 'Sets the right margin of this textfield',
  ),
  'swftextfield::setMargins' => 
  array (
    'return' => 'void',
    'params' => 'float left, float right',
    'description' => 'Sets both margins of this textfield',
  ),
  'swftextfield::setIndentation' => 
  array (
    'return' => 'void',
    'params' => 'float indentation',
    'description' => 'Sets the indentation of the first line of this textfield',
  ),
  'swftextfield::setLineSpacing' => 
  array (
    'return' => 'void',
    'params' => 'float space',
    'description' => 'Sets the line spacing of this textfield',
  ),
  'swftextfield::setColor' => 
  array (
    'return' => 'void',
    'params' => 'int r, int g, int b [, int a]',
    'description' => 'Sets the color of this textfield',
  ),
  'swftextfield::setName' => 
  array (
    'return' => 'void',
    'params' => 'string var_name',
    'description' => 'Sets the variable name of this textfield',
  ),
  'swftextfield::addString' => 
  array (
    'return' => 'void',
    'params' => 'string str',
    'description' => 'Adds the given string to this textfield',
  ),
  'swftextfield::setPadding' => 
  array (
    'return' => 'void',
    'params' => 'float padding',
    'description' => 'Sets the padding of this textfield',
  ),
  'swftextfield::addChars' => 
  array (
    'return' => 'void',
    'params' => 'string',
    'description' => 'adds characters to a font that will be available within a textfield',
  ),
  'SplObjectStorage::attach' => 
  array (
    'return' => 'void',
    'params' => '$obj',
    'description' => 'Attaches an object to the storage if not yet contained',
  ),
  'SplObjectStorage::detach' => 
  array (
    'return' => 'void',
    'params' => '$obj',
    'description' => 'Detaches an object from the storage',
  ),
  'SplObjectStorage::contains' => 
  array (
    'return' => 'bool',
    'params' => '$obj',
    'description' => 'Determine whethe an object is contained in the storage',
  ),
  'SplObjectStorage::count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Determine number of objects in storage',
  ),
  'SplObjectStorage::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => '',
  ),
  'SplObjectStorage::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => '',
  ),
  'SplObjectStorage::key' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => '',
  ),
  'SplObjectStorage::current' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => '',
  ),
  'SplObjectStorage::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => '',
  ),
  'RecursiveIteratorIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind the iterator to the first element of the top level inner iterator.',
  ),
  'RecursiveIteratorIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the current position is valid',
  ),
  'RecursiveIteratorIterator::key' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => 'Access the current key',
  ),
  'RecursiveIteratorIterator::current' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => 'Access the current element value',
  ),
  'RecursiveIteratorIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move forward to the next element',
  ),
  'RecursiveIteratorIterator::getDepth' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get the current depth of the recursive iteration',
  ),
  'RecursiveIteratorIterator::getSubIterator' => 
  array (
    'return' => 'RecursiveIterator',
    'params' => '[int level]',
    'description' => 'The current active sub iterator or the iterator at specified level',
  ),
  'RecursiveIteratorIterator::getInnerIterator' => 
  array (
    'return' => 'RecursiveIterator',
    'params' => '',
    'description' => 'The current active sub iterator',
  ),
  'RecursiveIteratorIterator::beginIteration' => 
  array (
    'return' => 'RecursiveIterator',
    'params' => '',
    'description' => 'Called when iteration begins (after first rewind() call)',
  ),
  'RecursiveIteratorIterator::endIteration' => 
  array (
    'return' => 'RecursiveIterator',
    'params' => '',
    'description' => 'Called when iteration ends (when valid() first returns false',
  ),
  'RecursiveIteratorIterator::callHasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Called for each element to test whether it has children',
  ),
  'RecursiveIteratorIterator::callGetChildren' => 
  array (
    'return' => 'RecursiveIterator',
    'params' => '',
    'description' => 'Return children of current element',
  ),
  'RecursiveIteratorIterator::beginChildren' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Called when recursing one level down',
  ),
  'RecursiveIteratorIterator::endChildren' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Called when end recursing one level',
  ),
  'RecursiveIteratorIterator::nextElement' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Called when the next element is available',
  ),
  'RecursiveIteratorIterator::setMaxDepth' => 
  array (
    'return' => 'void',
    'params' => '[$max_depth = -1]',
    'description' => 'Set the maximum allowed depth (or any depth if pmax_depth = -1]',
  ),
  'RecursiveIteratorIterator::getMaxDepth' => 
  array (
    'return' => 'int|false',
    'params' => '',
    'description' => 'Return the maximum accepted depth or false if any depth is allowed',
  ),
  'FilterIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'Iterator it',
    'description' => 'Create an Iterator from another iterator',
  ),
  'FilterIterator::getInnerIterator' => 
  array (
    'return' => 'Iterator',
    'params' => '',
    'description' => 'proto Iterator CachingIterator::getInnerIterator()proto Iterator LimitIterator::getInnerIterator()proto Iterator ParentIterator::getInnerIterator()Get the inner iterator',
  ),
  'ParentIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'proto void IteratorIterator::rewind()Rewind the iterator',
  ),
  'FilterIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'proto bool ParentIterator::valid()proto bool IteratorIterator::valid()proto bool NoRewindIterator::valid()Check whether the current element is valid',
  ),
  'FilterIterator::key' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => 'proto mixed CachingIterator::key()proto mixed LimitIterator::key()proto mixed ParentIterator::key()proto mixed IteratorIterator::key()proto mixed NoRewindIterator::key()proto mixed AppendIterator::key()Get the current key',
  ),
  'FilterIterator::current' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => 'proto mixed CachingIterator::current()proto mixed LimitIterator::current()proto mixed ParentIterator::current()proto mixed IteratorIterator::current()proto mixed NoRewindIterator::current()proto mixed AppendIterator::current()Get the current element value',
  ),
  'ParentIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'proto void IteratorIterator::next()proto void NoRewindIterator::next()Move the iterator forward',
  ),
  'FilterIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind the iterator',
  ),
  'FilterIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move the iterator forward',
  ),
  'RecursiveFilterIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'RecursiveIterator it',
    'description' => 'Create a RecursiveFilterIterator from a RecursiveIterator',
  ),
  'RecursiveFilterIterator::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the inner iterator\'s current element has children',
  ),
  'RecursiveFilterIterator::getChildren' => 
  array (
    'return' => 'RecursiveFilterIterator',
    'params' => '',
    'description' => 'Return the inner iterator\'s children contained in a RecursiveFilterIterator',
  ),
  'ParentIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'RecursiveIterator it',
    'description' => 'Create a ParentIterator from a RecursiveIterator',
  ),
  'ParentIterator::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the inner iterator\'s current element has children',
  ),
  'ParentIterator::getChildren' => 
  array (
    'return' => 'ParentIterator',
    'params' => '',
    'description' => 'Return the inner iterator\'s children contained in a ParentIterator',
  ),
  'LimitIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind the iterator to the specified starting offset',
  ),
  'LimitIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the current element is valid',
  ),
  'LimitIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move the iterator forward',
  ),
  'LimitIterator::seek' => 
  array (
    'return' => 'void',
    'params' => 'int position',
    'description' => 'Seek to the given position',
  ),
  'LimitIterator::getPosition' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Return the current position',
  ),
  'CachingIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'Iterator it [, flags = CIT_CALL_TOSTRING]',
    'description' => 'Construct a CachingIterator from an Iterator',
  ),
  'CachingIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind the iterator',
  ),
  'CachingIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the current element is valid',
  ),
  'CachingIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move the iterator forward',
  ),
  'CachingIterator::hasNext' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the inner iterator has a valid next element',
  ),
  'CachingIterator::__toString' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return the string representation of the current element',
  ),
  'RecursiveCachingIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'RecursiveIterator it [, flags = CIT_CALL_TOSTRING]',
    'description' => 'Create an iterator from a RecursiveIterator',
  ),
  'RecursiveCachingIterator::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether the current element of the inner iterator has children',
  ),
  'RecursiveCachingIterator::getChildren' => 
  array (
    'return' => 'RecursiveCachingIterator',
    'params' => '',
    'description' => 'Return the inner iterator\'s children as a RecursiveCachingIterator',
  ),
  'IteratorIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'Traversable it',
    'description' => 'Create an iterator from anything that is traversable',
  ),
  'NoRewindIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'Iterator it',
    'description' => 'Create an iterator from another iterator',
  ),
  'NoRewindIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Prevent a call to inner iterators rewind()',
  ),
  'NoRewindIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Return inner iterators valid()',
  ),
  'NoRewindIterator::key' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => 'Return inner iterators key()',
  ),
  'NoRewindIterator::current' => 
  array (
    'return' => 'mixed',
    'params' => '',
    'description' => 'Return inner iterators current()',
  ),
  'NoRewindIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Return inner iterators next()',
  ),
  'InfiniteIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'Iterator it',
    'description' => 'Create an iterator from another iterator',
  ),
  'InfiniteIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Prevent a call to inner iterators rewind() (internally the current data will be fetched if valid())',
  ),
  'EmptyIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Does nothing',
  ),
  'EmptyIterator::valid' => 
  array (
    'return' => 'false',
    'params' => '',
    'description' => 'Return false',
  ),
  'EmptyIterator::key' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Throws exception BadMethodCallException',
  ),
  'EmptyIterator::current' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Throws exception BadMethodCallException',
  ),
  'EmptyIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Does nothing',
  ),
  'AppendIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Create an AppendIterator',
  ),
  'AppendIterator::append' => 
  array (
    'return' => 'void',
    'params' => 'Iterator it',
    'description' => 'Append an iterator',
  ),
  'AppendIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind to the first iterator and rewind the first iterator, too',
  ),
  'AppendIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check if the current state is valid',
  ),
  'AppendIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Forward to next element',
  ),
  'iterator_to_array' => 
  array (
    'return' => 'array',
    'params' => 'Traversable it',
    'description' => 'Copy the iterator into an array',
  ),
  'iterator_count' => 
  array (
    'return' => 'int',
    'params' => 'Traversable it',
    'description' => 'Count the elements in an iterator',
  ),
  'class_parents' => 
  array (
    'return' => 'array',
    'params' => 'object instance',
    'description' => 'Return an array containing the names of all parent classes',
  ),
  'class_implements' => 
  array (
    'return' => 'array',
    'params' => 'mixed what [, bool autoload ]',
    'description' => 'Return all classes and interfaces implemented by SPL',
  ),
  'spl_classes' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Return an array containing the names of all clsses and interfaces defined in SPL',
  ),
  'spl_autoload' => 
  array (
    'return' => 'void',
    'params' => 'string class_name [, string file_extensions]',
    'description' => 'Default implementation for __autoload()',
  ),
  'spl_autoload_extensions' => 
  array (
    'return' => 'string',
    'params' => '[string file_extensions]',
    'description' => 'Register and return default file extensions for spl_autoload',
  ),
  'spl_autoload_call' => 
  array (
    'return' => 'void',
    'params' => 'string class_name',
    'description' => 'Try all registerd autoload function to load the requested class',
  ),
  'spl_autoload_register' => 
  array (
    'return' => 'bool',
    'params' => '[mixed autoload_function = "spl_autoload" [, throw = true]]',
    'description' => 'Register given function as __autoload() implementation',
  ),
  'spl_autoload_unregister' => 
  array (
    'return' => 'bool',
    'params' => 'mixed autoload_function',
    'description' => 'Unregister given function as __autoload() implementation',
  ),
  'spl_autoload_functions' => 
  array (
    'return' => 'false|array',
    'params' => '',
    'description' => 'Return all registered __autoload() functionns',
  ),
  'SimpleXMLIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind to first element',
  ),
  'SimpleXMLIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether iteration is valid',
  ),
  'SimpleXMLIterator::current' => 
  array (
    'return' => 'SimpleXMLIterator',
    'params' => '',
    'description' => 'Get current element',
  ),
  'SimpleXMLIterator::key' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get name of current child element',
  ),
  'SimpleXMLIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move to next element',
  ),
  'SimpleXMLIterator::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether element has children (elements)',
  ),
  'SimpleXMLIterator::getChildren' => 
  array (
    'return' => 'SimpleXMLIterator',
    'params' => '',
    'description' => 'Get child element iterator',
  ),
  'SimpleXMLIterator::count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get number of child elements',
  ),
  'DirectoryIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string path',
    'description' => 'Cronstructs a new dir iterator from a path.',
  ),
  'DirectoryIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind dir back to the start',
  ),
  'DirectoryIterator::key' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return current dir entry',
  ),
  'DirectoryIterator::current' => 
  array (
    'return' => 'DirectoryIterator',
    'params' => '',
    'description' => 'Return this (needed for Iterator interface)',
  ),
  'DirectoryIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move to next entry',
  ),
  'DirectoryIterator::valid' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Check whether dir contains more entries',
  ),
  'SplFileInfo::getPath' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return the path',
  ),
  'SplFileInfo::getFilename' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return filename only',
  ),
  'DirectoryIterator::getFilename' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return filename of current dir entry',
  ),
  'SplFileInfo::getPathname' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return path and filename',
  ),
  'RecursiveDirectoryIterator::key' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return getPathname() or getFilename() depending on flags',
  ),
  'RecursiveDirectoryIterator::current' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return getFilename(), getFileInfo() or $this depending on flags',
  ),
  'DirectoryIterator::isDot' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if current entry is \'.\' or  \'..\'',
  ),
  'SplFileInfo::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string file_name',
    'description' => 'Cronstructs a new SplFileInfo from a path.',
  ),
  'SplFileInfo::getPerms' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get file permissions',
  ),
  'SplFileInfo::getInode' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get file inode',
  ),
  'SplFileInfo::getSize' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get file size',
  ),
  'SplFileInfo::getOwner' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get file owner',
  ),
  'SplFileInfo::getGroup' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get file group',
  ),
  'SplFileInfo::getATime' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get last access time of file',
  ),
  'SplFileInfo::getMTime' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get last modification time of file',
  ),
  'SplFileInfo::getCTime' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get inode modification time of file',
  ),
  'SplFileInfo::getType' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get file type',
  ),
  'SplFileInfo::isWritable' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if file can be written',
  ),
  'SplFileInfo::isReadable' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if file can be read',
  ),
  'SplFileInfo::isExecutable' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if file is executable',
  ),
  'SplFileInfo::isFile' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if file is a regular file',
  ),
  'SplFileInfo::isDir' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if file is directory',
  ),
  'SplFileInfo::isLink' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Returns true if file is symbolic link',
  ),
  'SplFileInfo::openFile' => 
  array (
    'return' => 'SplFileObject',
    'params' => '[string mode = \'r\' [, bool use_include_path  [, resource context]]]',
    'description' => 'Open the current file',
  ),
  'SplFileInfo::setFileClass' => 
  array (
    'return' => 'void',
    'params' => '[string class_name]',
    'description' => 'Class to use in openFile()',
  ),
  'SplFileInfo::setInfoClass' => 
  array (
    'return' => 'void',
    'params' => '[string class_name]',
    'description' => 'Class to use in getFileInfo(), getPathInfo()',
  ),
  'SplFileInfo::getFileInfo' => 
  array (
    'return' => 'SplFileInfo',
    'params' => '[string $class_name]',
    'description' => 'Get/copy file info',
  ),
  'SplFileInfo::getPathInfo' => 
  array (
    'return' => 'SplFileInfo',
    'params' => '[string $class_name]',
    'description' => 'Get/copy file info',
  ),
  'RecursiveDirectoryIterator::__construct' => 
  array (
    'return' => 'void',
    'params' => 'string path [, int flags]',
    'description' => 'Cronstructs a new dir iterator from a path.',
  ),
  'RecursiveDirectoryIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind dir back to the start',
  ),
  'RecursiveDirectoryIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move to next entry',
  ),
  'RecursiveDirectoryIterator::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '[bool $allow_links = false]',
    'description' => 'Returns whether current entry is a directory and not \'.\' or \'..\'',
  ),
  'DirectoryIterator::getChildren' => 
  array (
    'return' => 'RecursiveDirectoryIterator',
    'params' => '',
    'description' => 'Returns an iterator for the current entry if it is a directory',
  ),
  'RecursiveDirectoryIterator::getSubPath' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Get sub path',
  ),
  'RecursiveDirectoryIterator::getSubPathname' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Get sub path and file name',
  ),
  'SplFileObject::__construct' => 
  array (
    'return' => 'void',
    'params' => '[int max_memory]',
    'description' => 'Construct a new temp file object',
  ),
  'SplFileObject::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind the file and read the first line',
  ),
  'SplFileObject::getFilename' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return the filename',
  ),
  'SplFileObject::eof' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Return whether end of file is reached',
  ),
  'SplFileObject::valid' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Return !eof()',
  ),
  'SplFileObject::fgets' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Rturn next line from file',
  ),
  'SplFileObject::current' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return current line from file',
  ),
  'SplFileObject::key' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Return line number',
  ),
  'SplFileObject::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Read next line',
  ),
  'SplFileObject::setFlags' => 
  array (
    'return' => 'void',
    'params' => 'int flags',
    'description' => 'Set file handling flags',
  ),
  'SplFileObject::getFlags' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get file handling flags',
  ),
  'SplFileObject::setMaxLineLen' => 
  array (
    'return' => 'void',
    'params' => 'int max_len',
    'description' => 'Set maximum line length',
  ),
  'SplFileObject::getMaxLineLen' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get maximum line length',
  ),
  'SplFileObject::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Return false',
  ),
  'SplFileObject::getChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Read NULL',
  ),
  'SplFileObject::fgetcsv' => 
  array (
    'return' => 'array',
    'params' => '[string delimiter [, string enclosure]]',
    'description' => 'Return current line as csv',
  ),
  'SplFileObject::flock' => 
  array (
    'return' => 'bool',
    'params' => 'int operation [, int &wouldblock]',
    'description' => 'Portable file locking',
  ),
  'SplFileObject::fflush' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Flush the file',
  ),
  'SplFileObject::ftell' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Return current file position',
  ),
  'SplFileObject::fseek' => 
  array (
    'return' => 'int',
    'params' => 'int pos [, int whence = SEEK_SET]',
    'description' => 'Return current file position',
  ),
  'SplFileObject::fgetc' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get a character form the file',
  ),
  'SplFileObject::fgetss' => 
  array (
    'return' => 'string',
    'params' => '[string allowable_tags]',
    'description' => 'Get a line from file pointer and strip HTML tags',
  ),
  'SplFileObject::fpassthru' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Output all remaining data from a file pointer',
  ),
  'SplFileObject::fscanf' => 
  array (
    'return' => 'bool',
    'params' => 'string format [, string ...]',
    'description' => 'Implements a mostly ANSI compatible fscanf()',
  ),
  'SplFileObject::fwrite' => 
  array (
    'return' => 'mixed',
    'params' => 'string str [, int length]',
    'description' => 'Binary-safe file write',
  ),
  'SplFileObject::fstat' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Stat() on a filehandle',
  ),
  'SplFileObject::ftruncate' => 
  array (
    'return' => 'bool',
    'params' => 'int size',
    'description' => 'Truncate file to \'size\' length',
  ),
  'SplFileObject::seek' => 
  array (
    'return' => 'void',
    'params' => 'int line_pos',
    'description' => 'Seek to specified line',
  ),
  'ArrayObject::offsetExists' => 
  array (
    'return' => 'bool',
    'params' => 'mixed $index',
    'description' => 'proto bool ArrayIterator::offsetExists(mixed $index)Returns whether the requested $index exists.',
  ),
  'ArrayObject::offsetGet' => 
  array (
    'return' => 'bool',
    'params' => 'mixed $index',
    'description' => 'proto bool ArrayIterator::offsetGet(mixed $index)Returns the value at the specified $index.',
  ),
  'ArrayObject::offsetSet' => 
  array (
    'return' => 'void',
    'params' => 'mixed $index, mixed $newval',
    'description' => 'proto void ArrayIterator::offsetSet(mixed $index, mixed $newval)Sets the value at the specified $index to $newval.',
  ),
  'ArrayObject::append' => 
  array (
    'return' => 'void',
    'params' => 'mixed $newval',
    'description' => 'proto void ArrayIterator::append(mixed $newval)Appends the value (cannot be called for objects).',
  ),
  'ArrayObject::offsetUnset' => 
  array (
    'return' => 'void',
    'params' => 'mixed $index',
    'description' => 'proto void ArrayIterator::offsetUnset(mixed $index)Unsets the value at the specified $index.',
  ),
  'ArrayObject::__construct' => 
  array (
    'return' => 'void',
    'params' => 'array|object ar = array() [, int flags = 0 [, string iterator_class = "ArrayIterator"]]',
    'description' => 'proto void ArrayIterator::__construct(array|object ar = array() [, int flags = 0])Cronstructs a new array iterator from a path.',
  ),
  'ArrayObject::setIteratorClass' => 
  array (
    'return' => 'void',
    'params' => 'string iterator_class',
    'description' => 'Set the class used in getIterator.',
  ),
  'ArrayObject::getIteratorClass' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get the class used in getIterator.',
  ),
  'ArrayObject::getFlags' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get flags',
  ),
  'ArrayObject::setFlags' => 
  array (
    'return' => 'void',
    'params' => 'int flags',
    'description' => 'Set flags',
  ),
  'ArrayObject::exchangeArray' => 
  array (
    'return' => 'Array|Object',
    'params' => 'Array|Object ar = array()',
    'description' => 'Replace the referenced array or object with a new one and return the old one (right now copy - to be changed)',
  ),
  'ArrayObject::getIterator' => 
  array (
    'return' => 'ArrayIterator',
    'params' => '',
    'description' => 'Create a new iterator from a ArrayObject instance',
  ),
  'ArrayIterator::rewind' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Rewind array back to the start',
  ),
  'ArrayIterator::seek' => 
  array (
    'return' => 'void',
    'params' => 'int $position',
    'description' => 'Seek to position.',
  ),
  'ArrayObject::count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'proto int ArrayIterator::count()Return the number of elements in the Iterator.',
  ),
  'ArrayIterator::current' => 
  array (
    'return' => 'mixed|NULL',
    'params' => '',
    'description' => 'Return current array entry',
  ),
  'ArrayIterator::key' => 
  array (
    'return' => 'mixed|NULL',
    'params' => '',
    'description' => 'Return current array key',
  ),
  'ArrayIterator::next' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => 'Move to next entry',
  ),
  'ArrayIterator::valid' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether array contains more entries',
  ),
  'RecursiveArrayIterator::hasChildren' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => 'Check whether current element has children (e.g. is an array)',
  ),
  'RecursiveArrayIterator::getChildren' => 
  array (
    'return' => 'object',
    'params' => '',
    'description' => 'Create a sub iterator for the current element (same class as $this)',
  ),
  'hash' => 
  array (
    'return' => 'string',
    'params' => 'string algo, string data[, bool raw_output = false]',
    'description' => 'Generate a hash of a given input stringReturns lowercase hexits by default',
  ),
  'hash_file' => 
  array (
    'return' => 'string',
    'params' => 'string algo, string filename[, bool raw_output = false]',
    'description' => 'Generate a hash of a given fileReturns lowercase hexits by default',
  ),
  'hash_hmac' => 
  array (
    'return' => 'string',
    'params' => 'string algo, string data, string key[, bool raw_output = false]',
    'description' => 'Generate a hash of a given input string with a key using HMACReturns lowercase hexits by default',
  ),
  'hash_hmac_file' => 
  array (
    'return' => 'string',
    'params' => 'string algo, string filename, string key[, bool raw_output = false]',
    'description' => 'Generate a hash of a given file with a key using HMACReturns lowercase hexits by default',
  ),
  'hash_init' => 
  array (
    'return' => 'resource',
    'params' => 'string algo[, int options, string key]',
    'description' => 'Initialize a hashing context',
  ),
  'hash_update' => 
  array (
    'return' => 'bool',
    'params' => 'resource context, string data',
    'description' => 'Pump data into the hashing algorithm',
  ),
  'hash_update_stream' => 
  array (
    'return' => 'int',
    'params' => 'resource context, resource handle[, integer length]',
    'description' => 'Pump data into the hashing algorithm from an open stream',
  ),
  'hash_update_file' => 
  array (
    'return' => 'bool',
    'params' => 'resource context, string filename[, resource context]',
    'description' => 'Pump data into the hashing algorithm from a file',
  ),
  'hash_final' => 
  array (
    'return' => 'string',
    'params' => 'resource context[, bool raw_output=false]',
    'description' => 'Output resulting digest',
  ),
  'hash_algos' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Return a list of registered hashing algorithms',
  ),
  'sybase_unbuffered_query' => 
  array (
    'return' => 'int',
    'params' => 'string query [, int link_id]',
    'description' => 'Send Sybase query',
  ),
  'sybase_fetch_assoc' => 
  array (
    'return' => 'array',
    'params' => 'int result',
    'description' => 'Fetch row as array without numberic indices',
  ),
  'sybase_min_client_severity' => 
  array (
    'return' => 'void',
    'params' => 'int severity',
    'description' => 'Sets minimum client severity',
  ),
  'sybase_min_server_severity' => 
  array (
    'return' => 'void',
    'params' => 'int severity',
    'description' => 'Sets minimum server severity',
  ),
  'sybase_deadlock_retry_count' => 
  array (
    'return' => 'void',
    'params' => 'int retry_count',
    'description' => 'Sets deadlock retry count',
  ),
  'sybase_set_message_handler' => 
  array (
    'return' => 'bool',
    'params' => 'mixed error_func [, resource connection]',
    'description' => 'Set the error handler, to be called when a server message is raised.If error_func is NULL the handler will be deleted',
  ),
  'mhash_count' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Gets the number of available hashes',
  ),
  'mhash_get_block_size' => 
  array (
    'return' => 'int',
    'params' => 'int hash',
    'description' => 'Gets the block size of hash',
  ),
  'mhash_get_hash_name' => 
  array (
    'return' => 'string',
    'params' => 'int hash',
    'description' => 'Gets the name of hash',
  ),
  'mhash' => 
  array (
    'return' => 'string',
    'params' => 'int hash, string data [, string key]',
    'description' => 'Hash data with hash',
  ),
  'mhash_keygen_s2k' => 
  array (
    'return' => 'string',
    'params' => 'int hash, string input_password, string salt, int bytes',
    'description' => 'Generates a key using hash functions',
  ),
  'tidy_parse_string' => 
  array (
    'return' => 'bool',
    'params' => 'string input [, mixed config_options [, string encoding]]',
    'description' => 'Parse a document stored in a string',
  ),
  'tidy_get_error_buffer' => 
  array (
    'return' => 'string',
    'params' => '[boolean detailed]',
    'description' => 'Return warnings and errors which occured parsing the specified document',
  ),
  'tidy_get_output' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Return a string representing the parsed tidy markup',
  ),
  'tidy_parse_file' => 
  array (
    'return' => 'boolean',
    'params' => 'string file [, mixed config_options [, string encoding [, bool use_include_path]]]',
    'description' => 'Parse markup in file or URI',
  ),
  'tidy_clean_repair' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Execute configured cleanup and repair operations on parsed markup',
  ),
  'tidy_repair_string' => 
  array (
    'return' => 'boolean',
    'params' => 'string data [, mixed config_file [, string encoding]]',
    'description' => 'Repair a string using an optionally provided configuration file',
  ),
  'tidy_repair_file' => 
  array (
    'return' => 'boolean',
    'params' => 'string filename [, mixed config_file [, string encoding [, bool use_include_path]]]',
    'description' => 'Repair a file using an optionally provided configuration file',
  ),
  'tidy_diagnose' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Run configured diagnostics on parsed and repaired markup.',
  ),
  'tidy_get_release' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get release date (version) for Tidy library',
  ),
  'tidy_get_opt_doc' => 
  array (
    'return' => 'string',
    'params' => 'tidy resource, string optname',
    'description' => 'Returns the documentation for the given option name',
  ),
  'tidy_get_config' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Get current Tidy configuarion',
  ),
  'tidy_get_status' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get status of specfied document.',
  ),
  'tidy_get_html_ver' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get the Detected HTML version for the specified document.',
  ),
  'tidy_is_xhtml' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Indicates if the document is a generic (non HTML/XHTML) XML document.',
  ),
  'tidy_error_count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the Number of Tidy errors encountered for specified document.',
  ),
  'tidy_warning_count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the Number of Tidy warnings encountered for specified document.',
  ),
  'tidy_access_count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the Number of Tidy accessibility warnings encountered for specified document.',
  ),
  'tidy_config_count' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Returns the Number of Tidy configuration errors encountered for specified document.',
  ),
  'tidy_getopt' => 
  array (
    'return' => 'mixed',
    'params' => 'string option',
    'description' => 'Returns the value of the specified configuration option for the tidy document.',
  ),
  'tidy_get_root' => 
  array (
    'return' => 'TidyNode',
    'params' => '',
    'description' => 'Returns a TidyNode Object representing the root of the tidy parse tree',
  ),
  'tidy_get_html' => 
  array (
    'return' => 'TidyNode',
    'params' => '',
    'description' => 'Returns a TidyNode Object starting from the <HTML> tag of the tidy parse tree',
  ),
  'tidy_get_head' => 
  array (
    'return' => 'TidyNode',
    'params' => '',
    'description' => 'Returns a TidyNode Object starting from the <HEAD> tag of the tidy parse tree',
  ),
  'tidy_get_body' => 
  array (
    'return' => 'TidyNode',
    'params' => 'resource tidy',
    'description' => 'Returns a TidyNode Object starting from the <BODY> tag of the tidy parse tree',
  ),
  'tidyNode::hasChildren' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node has children',
  ),
  'tidyNode::hasSiblings' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node has siblings',
  ),
  'tidyNode::isComment' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node represents a comment',
  ),
  'tidyNode::isHtml' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node is part of a HTML document',
  ),
  'tidyNode::isXhtml' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node is part of a XHTML document',
  ),
  'tidyNode::isXml' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node is part of a XML document',
  ),
  'tidyNode::isText' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node represents text (no markup)',
  ),
  'tidyNode::isJste' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node is JSTE',
  ),
  'tidyNode::isAsp' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node is ASP',
  ),
  'tidyNode::isPhp' => 
  array (
    'return' => 'boolean',
    'params' => '',
    'description' => 'Returns true if this node is PHP',
  ),
  'smfi_setflags' => 
  array (
    'return' => 'string',
    'params' => 'long flags',
    'description' => 'Sets the flags describing the actions the filter may take.',
  ),
  'smfi_settimeout' => 
  array (
    'return' => 'string',
    'params' => 'long timeout',
    'description' => 'Sets the number of seconds libmilter will wait for an MTA connection before timing out a socket.',
  ),
  'smfi_getsymval' => 
  array (
    'return' => 'string',
    'params' => 'string macro',
    'description' => 'Returns the value of the given macro or NULL if the macro is not defined.',
  ),
  'smfi_setreply' => 
  array (
    'return' => 'string',
    'params' => 'string rcode, string xcode, string message',
    'description' => 'Directly set the SMTP error reply code for this connection.This code will be used on subsequent error replies resulting from actions taken by this filter.',
  ),
  'smfi_addheader' => 
  array (
    'return' => 'string',
    'params' => 'string headerf, string headerv',
    'description' => 'Adds a header to the current message.',
  ),
  'smfi_chgheader' => 
  array (
    'return' => 'string',
    'params' => 'string headerf, string headerv',
    'description' => 'Changes a header\'s value for the current message.',
  ),
  'smfi_addrcpt' => 
  array (
    'return' => 'string',
    'params' => 'string rcpt',
    'description' => 'Add a recipient to the message envelope.',
  ),
  'smfi_delrcpt' => 
  array (
    'return' => 'string',
    'params' => 'string rcpt',
    'description' => 'Removes the named recipient from the current message\'s envelope.',
  ),
  'smfi_replacebody' => 
  array (
    'return' => 'string',
    'params' => 'string body',
    'description' => 'Replaces the body of the current message. If called more than once,subsequent calls result in data being appended to the new body.',
  ),
  'virtual' => 
  array (
    'return' => 'bool',
    'params' => 'string filename',
    'description' => 'Perform an Apache sub-request',
  ),
  'getallheaders' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Alias for apache_request_headers()',
  ),
  'apache_response_headers' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Fetch all HTTP response headers',
  ),
  'apache_note' => 
  array (
    'return' => 'string',
    'params' => 'string note_name [, string note_value]',
    'description' => 'Get and set Apache request notes',
  ),
  'apache_setenv' => 
  array (
    'return' => 'bool',
    'params' => 'string variable, string value [, bool walk_to_top]',
    'description' => 'Set an Apache subprocess_env variable',
  ),
  'apache_getenv' => 
  array (
    'return' => 'bool',
    'params' => 'string variable [, bool walk_to_top]',
    'description' => 'Get an Apache subprocess_env variable',
  ),
  'apache_get_version' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Fetch Apache version',
  ),
  'apache_get_modules' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get a list of loaded Apache modules',
  ),
  'nsapi_virtual' => 
  array (
    'return' => 'bool',
    'params' => 'string uri',
    'description' => 'Perform an NSAPI sub-request',
  ),
  'nsapi_request_headers' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get all headers from the request',
  ),
  'nsapi_response_headers' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Get all headers from the response',
  ),
  'ApacheRequest::filename' => 
  array (
    'return' => 'string',
    'params' => '[string new_filename]',
    'description' => '',
  ),
  'ApacheRequest::uri' => 
  array (
    'return' => 'string',
    'params' => '[string new_uri]',
    'description' => '',
  ),
  'ApacheRequest::unparsed_uri' => 
  array (
    'return' => 'string',
    'params' => '[string new_unparsed_uri]',
    'description' => '',
  ),
  'ApacheRequest::path_info' => 
  array (
    'return' => 'string',
    'params' => '[string new_path_info]',
    'description' => '',
  ),
  'ApacheRequest::args' => 
  array (
    'return' => 'string',
    'params' => '[string new_args]',
    'description' => '',
  ),
  'ApacheRequest::boundary' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::content_type' => 
  array (
    'return' => 'string',
    'params' => '[string new_type]',
    'description' => '',
  ),
  'ApacheRequest::content_encoding' => 
  array (
    'return' => 'string',
    'params' => '[string new_encoding]',
    'description' => '',
  ),
  'ApacheRequest::handler' => 
  array (
    'return' => 'string',
    'params' => '[string new_handler]',
    'description' => '',
  ),
  'ApacheRequest::the_request' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::protocol' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::hostname' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::status_line' => 
  array (
    'return' => 'string',
    'params' => '[string new_status_line]',
    'description' => '',
  ),
  'ApacheRequest::method' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::proto_num' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::assbackwards' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::proxyreq' => 
  array (
    'return' => 'int',
    'params' => '[int new_proxyreq]',
    'description' => '',
  ),
  'ApacheRequest::chunked' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::header_only' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::request_time' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::status' => 
  array (
    'return' => 'int',
    'params' => '[int new_status]',
    'description' => '',
  ),
  'ApacheRequest::method_number' => 
  array (
    'return' => 'int',
    'params' => '[int method_number]',
    'description' => '',
  ),
  'ApacheRequest::allowed' => 
  array (
    'return' => 'int',
    'params' => '[int allowed]',
    'description' => '',
  ),
  'ApacheRequest::bytes_sent' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::mtime' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::content_length' => 
  array (
    'return' => 'int',
    'params' => '[int new_content_length]',
    'description' => '',
  ),
  'ApacheRequest::remaining' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::no_cache' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::no_local_copy' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'ApacheRequest::read_body' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'apache_request_headers_in' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => '* fetch all incoming request headers',
  ),
  'apache_request_headers_out' => 
  array (
    'return' => 'array',
    'params' => '[{string name|array list} [, string value [, bool replace = false]]]',
    'description' => '* fetch all outgoing request headers',
  ),
  'apache_request_err_headers_out' => 
  array (
    'return' => 'array',
    'params' => '[{string name|array list} [, string value [, bool replace = false]]]',
    'description' => '* fetch all headers that go out in case of an error or a subrequest',
  ),
  'apache_request_server_port' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => '',
  ),
  'apache_request_remote_host' => 
  array (
    'return' => 'int',
    'params' => '[int type]',
    'description' => '',
  ),
  'apache_request_update_mtime' => 
  array (
    'return' => 'long',
    'params' => '[int dependency_mtime]',
    'description' => '',
  ),
  'apache_request_set_etag' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => '',
  ),
  'apache_request_set_last_modified' => 
  array (
    'return' => 'void',
    'params' => '',
    'description' => '',
  ),
  'apache_request_meets_conditions' => 
  array (
    'return' => 'long',
    'params' => '',
    'description' => '',
  ),
  'apache_request_discard_request_body' => 
  array (
    'return' => 'long',
    'params' => '',
    'description' => '',
  ),
  'apache_request_satisfies' => 
  array (
    'return' => 'long',
    'params' => '',
    'description' => '',
  ),
  'apache_request_is_initial_req' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => '',
  ),
  'apache_request_some_auth_required' => 
  array (
    'return' => 'bool',
    'params' => '',
    'description' => '',
  ),
  'apache_request_auth_type' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'apache_request_auth_name' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => '',
  ),
  'apache_request_log_error' => 
  array (
    'return' => 'boolean',
    'params' => 'string message, [long facility]',
    'description' => '',
  ),
  'apache_request_sub_req_lookup_uri' => 
  array (
    'return' => 'object',
    'params' => 'string uri',
    'description' => 'Returns sub-request for the specified uri.  You wouldneed to run it yourself with run()',
  ),
  'apache_request_sub_req_lookup_file' => 
  array (
    'return' => 'object',
    'params' => 'string file',
    'description' => 'Returns sub-request for the specified file.  You wouldneed to run it yourself with run().',
  ),
  'apache_request_sub_req_method_uri' => 
  array (
    'return' => 'object',
    'params' => 'string method, string uri',
    'description' => 'Returns sub-request for the specified file.  You wouldneed to run it yourself with run().',
  ),
  'apache_request_run' => 
  array (
    'return' => 'long',
    'params' => '',
    'description' => 'This is a wrapper for ap_sub_run_req and ap_destory_sub_req.  It takessub_request, runs it, destroys it, and returns it\'s status.',
  ),
  'apache_child_terminate' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Terminate apache process after this request',
  ),
  'apache_request_headers' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Fetch all HTTP request headers',
  ),
  'apache_lookup_uri' => 
  array (
    'return' => 'object',
    'params' => 'string URI',
    'description' => 'Perform a partial request of the given URI to obtain information about it',
  ),
  'apache_reset_timeout' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Reset the Apache write timer',
  ),
  'stream_wrapper_register' => 
  array (
    'return' => 'bool',
    'params' => 'string protocol, string classname',
    'description' => 'Registers a custom URL protocol handler class',
  ),
  'stream_wrapper_unregister' => 
  array (
    'return' => 'bool',
    'params' => 'string protocol',
    'description' => 'Unregister a wrapper for the life of the current request.',
  ),
  'stream_wrapper_restore' => 
  array (
    'return' => 'bool',
    'params' => 'string protocol',
    'description' => 'Restore the original protocol handler, overriding if necessary',
  ),
  'set_time_limit' => 
  array (
    'return' => 'bool',
    'params' => 'int seconds',
    'description' => 'Sets the maximum time a script can run',
  ),
  'ob_list_handlers' => 
  array (
    'return' => 'false|array',
    'params' => '',
    'description' => '*  List all output_buffers in an array',
  ),
  'ob_start' => 
  array (
    'return' => 'bool',
    'params' => '[ string|array user_function [, int chunk_size [, bool erase]]]',
    'description' => 'Turn on Output Buffering (specifying an optional output handler).',
  ),
  'ob_flush' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Flush (send) contents of the output buffer. The last buffer content is sent to next buffer',
  ),
  'ob_clean' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Clean (delete) the current output buffer',
  ),
  'ob_end_flush' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Flush (send) the output buffer, and delete current output buffer',
  ),
  'ob_end_clean' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Clean the output buffer, and delete current output buffer',
  ),
  'ob_get_flush' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Get current buffer contents, flush (send) the output buffer, and delete current output buffer',
  ),
  'ob_get_clean' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Get current buffer contents and delete current output buffer',
  ),
  'ob_get_contents' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Return the contents of the output buffer',
  ),
  'ob_get_level' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Return the nesting level of the output buffer',
  ),
  'ob_get_length' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Return the length of the output buffer',
  ),
  'ob_get_status' => 
  array (
    'return' => 'false|array',
    'params' => '[bool full_status]',
    'description' => 'Return the status of the active or all output buffers',
  ),
  'ob_implicit_flush' => 
  array (
    'return' => 'void',
    'params' => '[int flag]',
    'description' => 'Turn implicit flush on/off and is equivalent to calling flush() after every output call',
  ),
  'output_reset_rewrite_vars' => 
  array (
    'return' => 'bool',
    'params' => 'void',
    'description' => 'Reset(clear) URL rewriter values',
  ),
  'output_add_rewrite_var' => 
  array (
    'return' => 'bool',
    'params' => 'string name, string value',
    'description' => 'Add URL rewriter values',
  ),
  'zend_version' => 
  array (
    'return' => 'string',
    'params' => 'void',
    'description' => 'Get the version of the Zend Engine',
  ),
  'func_num_args' => 
  array (
    'return' => 'int',
    'params' => 'void',
    'description' => 'Get the number of arguments that were passed to the function',
  ),
  'func_get_arg' => 
  array (
    'return' => 'mixed',
    'params' => 'int arg_num',
    'description' => 'Get the $arg_num\'th argument that was passed to the function',
  ),
  'func_get_args' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Get an array of the arguments that were passed to the function',
  ),
  'strlen' => 
  array (
    'return' => 'int',
    'params' => 'string str',
    'description' => 'Get string length',
  ),
  'strcmp' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2',
    'description' => 'Binary safe string comparison',
  ),
  'strncmp' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2, int len',
    'description' => 'Binary safe string comparison',
  ),
  'strcasecmp' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2',
    'description' => 'Binary safe case-insensitive string comparison',
  ),
  'strncasecmp' => 
  array (
    'return' => 'int',
    'params' => 'string str1, string str2, int len',
    'description' => 'Binary safe string comparison',
  ),
  'each' => 
  array (
    'return' => 'array',
    'params' => 'array arr',
    'description' => 'Return the currently pointed key..value pair in the passed array, and advance the pointer to the next element',
  ),
  'error_reporting' => 
  array (
    'return' => 'int',
    'params' => 'int new_error_level=null',
    'description' => 'Return the current error_reporting level, and if an argument was passed - change to the new level',
  ),
  'define' => 
  array (
    'return' => 'bool',
    'params' => 'string constant_name, mixed value, boolean case_sensitive=true',
    'description' => 'Define a new constant',
  ),
  'defined' => 
  array (
    'return' => 'bool',
    'params' => 'string constant_name',
    'description' => 'Check whether a constant exists',
  ),
  'get_class' => 
  array (
    'return' => 'string',
    'params' => '[object object]',
    'description' => 'Retrieves the class name',
  ),
  'get_parent_class' => 
  array (
    'return' => 'string',
    'params' => '[mixed object]',
    'description' => 'Retrieves the parent class name for object or class or current scope.',
  ),
  'is_subclass_of' => 
  array (
    'return' => 'bool',
    'params' => 'object object, string class_name',
    'description' => 'Returns true if the object has this class as one of its parents',
  ),
  'is_a' => 
  array (
    'return' => 'bool',
    'params' => 'object object, string class_name',
    'description' => 'Returns true if the object is of this class or has this class as one of its parents',
  ),
  'get_class_vars' => 
  array (
    'return' => 'array',
    'params' => 'string class_name',
    'description' => 'Returns an array of default properties of the class.',
  ),
  'get_object_vars' => 
  array (
    'return' => 'array',
    'params' => 'object obj',
    'description' => 'Returns an array of object properties',
  ),
  'get_class_methods' => 
  array (
    'return' => 'array',
    'params' => 'mixed class',
    'description' => 'Returns an array of method names for class or class instance.',
  ),
  'method_exists' => 
  array (
    'return' => 'bool',
    'params' => 'object object, string method',
    'description' => 'Checks if the class method exists',
  ),
  'property_exists' => 
  array (
    'return' => 'bool',
    'params' => 'mixed object_or_class, string property_name',
    'description' => 'Checks if the object or class has a property',
  ),
  'class_exists' => 
  array (
    'return' => 'bool',
    'params' => 'string classname [, bool autoload]',
    'description' => 'Checks if the class exists',
  ),
  'interface_exists' => 
  array (
    'return' => 'bool',
    'params' => 'string classname [, bool autoload]',
    'description' => 'Checks if the class exists',
  ),
  'function_exists' => 
  array (
    'return' => 'bool',
    'params' => 'string function_name',
    'description' => 'Checks if the function exists',
  ),
  'leak' => 
  array (
    'return' => 'void',
    'params' => 'int num_bytes=3',
    'description' => 'Cause an intentional memory leak, for testing/debugging purposes',
  ),
  'get_included_files' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns an array with the file names that were include_once()\'d',
  ),
  'trigger_error' => 
  array (
    'return' => 'void',
    'params' => 'string messsage [, int error_type]',
    'description' => 'Generates a user-level error/warning/notice message',
  ),
  'set_error_handler' => 
  array (
    'return' => 'string',
    'params' => 'string error_handler [, int error_types]',
    'description' => 'Sets a user-defined error handler function.  Returns the previously defined error handler, or false on error',
  ),
  'restore_error_handler' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Restores the previously defined error handler function',
  ),
  'set_exception_handler' => 
  array (
    'return' => 'string',
    'params' => 'callable exception_handler',
    'description' => 'Sets a user-defined exception handler function.  Returns the previously defined exception handler, or false on error',
  ),
  'restore_exception_handler' => 
  array (
    'return' => 'void',
    'params' => 'void',
    'description' => 'Restores the previously defined exception handler function',
  ),
  'get_declared_classes' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Returns an array of all declared classes.',
  ),
  'get_declared_interfaces' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Returns an array of all declared interfaces.',
  ),
  'get_defined_functions' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns an array of all defined functions',
  ),
  'get_defined_vars' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Returns an associative array of names and values of all currently defined variable names (variables in the current scope)',
  ),
  'create_function' => 
  array (
    'return' => 'string',
    'params' => 'string args, string code',
    'description' => 'Creates an anonymous function, and returns its name (funny, eh?)',
  ),
  'get_resource_type' => 
  array (
    'return' => 'string',
    'params' => 'resource res',
    'description' => 'Get the resource type name for a given resource',
  ),
  'get_loaded_extensions' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Return an array containing names of loaded extensions',
  ),
  'get_defined_constants' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Return an array containing the names and values of all defined constants',
  ),
  'debug_backtrace' => 
  array (
    'return' => 'array',
    'params' => 'void',
    'description' => 'Return backtrace as array',
  ),
  'extension_loaded' => 
  array (
    'return' => 'bool',
    'params' => 'string extension_name',
    'description' => 'Returns true if the named extension is loaded',
  ),
  'get_extension_funcs' => 
  array (
    'return' => 'array',
    'params' => 'string extension_name',
    'description' => 'Returns an array with the names of functions belonging to the named extension',
  ),
  'Exception::__clone' => 
  array (
    'return' => 'Exception',
    'params' => '',
    'description' => 'Clone the exception object',
  ),
  'Exception::getFile' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get the file in which the exception occurred',
  ),
  'Exception::getLine' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get the line in which the exception occurred',
  ),
  'Exception::getMessage' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Get the exception message',
  ),
  'Exception::getCode' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get the exception code',
  ),
  'Exception::getTrace' => 
  array (
    'return' => 'array',
    'params' => '',
    'description' => 'Get the stack trace for the location in which the exception occurred',
  ),
  'ErrorException::getSeverity' => 
  array (
    'return' => 'int',
    'params' => '',
    'description' => 'Get the exception severity',
  ),
  'Exception::getTraceAsString' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Obtain the backtrace for the exception as a string (instead of an array)',
  ),
  'Exception::__toString' => 
  array (
    'return' => 'string',
    'params' => '',
    'description' => 'Obtain the string representation of the Exception object',
  ),
)    ;

    public function get($k) {
        if (isset($this->prototype[$k])) {
            return $this->prototype[$k];
        } else {
            return false;
        }
    }

    static function getInstance() {
        if (is_null(self::$instance)) {
            $class = __CLASS__;
            self::$instance = new $class();
        }
        return self::$instance;
    }
}