<?php
/**
 * MessageSource_Database class file.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the BSD License.
 *
 * Copyright(c) 2004 by Qiang Xue. All rights reserved.
 *
 * To contact the author write to {@link mailto:qiang.xue@gmail.com Qiang Xue}
 * The latest version of PRADO can be obtained from:
 * {@link http://prado.sourceforge.net/}
 *
 * @package System.I18N.core
 */

/**
 * Get the MessageSource class file.
 */
require_once(dirname(__FILE__).'/MessageSource.php');

/**
 * MessageSource_Database class.
 *
 * Retrive the message translation from a database.
 *
 * See the MessageSource::factory() method to instantiate this class.
 *
 * @package System.I18N.core
 */
class MessageSource_Database extends MessageSource
{
	private $_connID='';
	private $_conn;

	/**
	 * Constructor.
	 * Create a new message source using a Database
	 * @param string MySQL datasource, in PEAR's DB DSN format.
	 * @see MessageSource::factory();
	 */
	function __construct($source)
	{
		$this->_connID= (string)$source;
	}

	/**
	 * @return TDbConnection the database connection that may be used to retrieve messages.
	 */
	public function getDbConnection()
	{
		if($this->_conn===null)
		{
			$this->_conn=$this->createDbConnection($this->_connID);
			$this->_conn->setActive(true);
		}
		return $this->_conn;
	}

	/**
	 * Creates the DB connection.
	 * @param string the module ID for TDataSourceConfig
	 * @return TDbConnection the created DB connection
	 * @throws TConfigurationException if module ID is invalid or empty
	 */
	protected function createDbConnection($connectionID)
	{
		if($connectionID!=='')
		{
			$conn=Prado::getApplication()->getModule($connectionID);
			if($conn instanceof TDataSourceConfig)
				return $conn->getDbConnection();
			else
				throw new TConfigurationException('messagesource_connectionid_invalid',$connectionID);
		}
		else
			throw new TConfigurationException('messagesource_connectionid_required');
	}

	/**
	 * Get an array of messages for a particular catalogue and cultural
	 * variant.
	 * @param string the catalogue name + variant
	 * @return array translation messages.
	 */
	protected function &loadData($variant)
	{
		$command=$this->getDBConnection()->createCommand(
			'SELECT t.id, t.source, t.target, t.comments
				FROM trans_unit t, catalogue c
				WHERE c.cat_id =  t.cat_id
					AND c.name = :variant
				ORDER BY id ASC');
		$command->bindParameter(':variant',$variant,PDO::PARAM_STR);
		$dataReader=$command->query();

		$result = array();

		foreach ($dataReader as $row)
			$result[$row['source']] = array($row['target'],$row['id'],$row['comments']);

		return $result;
	}
	
	/**
	 * Get the last modified unix-time for this particular catalogue+variant.
	 * We need to query the database to get the date_modified.
	 * @param string catalogue+variant
	 * @return int last modified in unix-time format.
	 */
	protected function getLastModified($source)
	{
		$command=$this->getDBConnection()->createCommand(
			'SELECT date_modified FROM catalogue WHERE name = :source');
		$command->bindParameter(':source',$source,PDO::PARAM_STR);
		$result=$command->queryScalar();
		return $result ? $result : 0;
	}


	/**
	 * Check if a particular catalogue+variant exists in the database.
	 * @param string catalogue+variant
	 * @return boolean true if the catalogue+variant is in the database,
	 * false otherwise.
	 */
	protected function isValidSource($variant)
	{
		$command=$this->getDBConnection()->createCommand(
			'SELECT COUNT(*) FROM catalogue WHERE name = :variant');
		$command->bindParameter(':variant',$variant,PDO::PARAM_STR);
		return $command->queryScalar()==1;
	}

	/**
	 * Get all the variants of a particular catalogue.
	 * @param string catalogue name
	 * @return array list of all variants for this catalogue.
	 */
	protected function getCatalogueList($catalogue)
	{
		$variants = explode('_',$this->culture);

		$catalogues = array($catalogue);

		$variant = null;

		for($i = 0, $k = count($variants); $i < $k; ++$i)
		{
			if(isset($variants[$i]{0}))
			{
				$variant .= ($variant)?'_'.$variants[$i]:$variants[$i];
				$catalogues[] = $catalogue.'.'.$variant;
			}
		}
		return array_reverse($catalogues);
	}

	/**
	 * Retrive catalogue details, array($cat_id, $variant, $count).
	 * @param string catalogue
	 * @return array catalogue details, array($cat_id, $variant, $count).
	 */
	private function getCatalogueDetails($catalogue='messages')
	{
		if(empty($catalogue))
			$catalogue = 'messages';

		$variant = $catalogue.'.'.$this->culture;

		$command=$this->getDBConnection()->createCommand(
			'SELECT cat_id FROM catalogue WHERE name = :variant');
		$command->bindParameter(':variant',$variant,PDO::PARAM_STR);
		$cat_id=$command->queryScalar();

		if ($cat_id===null) return false;

		$command=$this->getDBConnection()->createCommand(
			'SELECT COUNT(msg_id) FROM trans_unit WHERE cat_id = :catid ');
		$command->bindParameter(':catid',$cat_id,PDO::PARAM_INT);
		$count=$command->queryScalar();

		return array($cat_id, $variant, $count);
	}

	/**
	 * Update the catalogue last modified time.
	 * @return boolean true if updated, false otherwise.
	 */
	private function updateCatalogueTime($cat_id, $variant)
	{
		$time = time();
		$command=$this->getDBConnection()->createCommand(
			'UPDATE catalogue SET date_modified = :moddate WHERE cat_id = :catid');
		$command->bindParameter(':moddate',$time,PDO::PARAM_INT);
		$command->bindParameter(':catid',$cat_id,PDO::PARAM_INT);
		$result=$command->execute();

		if(!empty($this->cache))
			$this->cache->clean($variant, $this->culture);

		return $result;
	}

	/**
	 * Save the list of untranslated blocks to the translation source.
	 * If the translation was not found, you should add those
	 * strings to the translation source via the <b>append()</b> method.
	 * @param string the catalogue to add to
	 * @return boolean true if saved successfuly, false otherwise.
	 */
	function save($catalogue='messages')
	{
		$messages = $this->untranslated;

		if(count($messages) <= 0) return false;

		$details = $this->getCatalogueDetails($catalogue);

		if($details)
			list($cat_id, $variant, $count) = $details;
		else
			return false;

		if($cat_id <= 0) return false;
		$inserted = 0;

		$time = time();

		$command=$this->getDBConnection()->createCommand(
			'INSERT INTO trans_unit (cat_id,id,source,date_added) VALUES (:catid,:id,:source,:dateadded)');
		$command->bindParameter(':catid',$cat_id,PDO::PARAM_INT);
		$command->bindParameter(':id',$count,PDO::PARAM_INT);
		$command->bindParameter(':source',$message,PDO::PARAM_STR);
		$command->bindParameter(':dateadded',$time,PDO::PARAM_INT);
		foreach($messages as $message)
		{
			if (empty($message)) continue;
			$count++; $inserted++;
			$command->execute();
		}
		if($inserted > 0)
			$this->updateCatalogueTime($cat_id, $variant);

		return $inserted > 0;
	}

	/**
	 * Delete a particular message from the specified catalogue.
	 * @param string the source message to delete.
	 * @param string the catalogue to delete from.
	 * @return boolean true if deleted, false otherwise.
	 */
	function delete($message, $catalogue='messages')
	{
		$details = $this->getCatalogueDetails($catalogue);
		if($details)
			list($cat_id, $variant, $count) = $details;
		else
			return false;

		$command=$this->getDBConnection()->createCommand(
			'DELETE FROM trans_unit WHERE cat_id = :catid AND source = :message');
		$command->bindParameter(':catid',$cat_id,PDO::PARAM_INT);
		$command->bindParameter(':message',$message,PDO::PARAM_STR);

		return ($command->execute()==1) ? $this->updateCatalogueTime($cat_id, $variant) : false;

	}

	/**
	 * Update the translation.
	 * @param string the source string.
	 * @param string the new translation string.
	 * @param string comments
	 * @param string the catalogue of the translation.
	 * @return boolean true if translation was updated, false otherwise.
	 */
	function update($text, $target, $comments, $catalogue='messages')
	{
		$details = $this->getCatalogueDetails($catalogue);
		if($details)
			list($cat_id, $variant, $count) = $details;
		else
			return false;

		$time = time();
		$command=$this->getDBConnection()->createCommand(
			'UPDATE trans_unit SET target = :target, comments = :comments, date_modified = :datemod
					WHERE cat_id = :catid AND source = :source');
		$command->bindParameter(':target',$target,PDO::PARAM_STR);
		$command->bindParameter(':comments',$comments,PDO::PARAM_STR);
		$command->bindParameter(':datemod',$time,PDO::PARAM_INT);
		$command->bindParameter(':catid',$cat_id,PDO::PARAM_INT);
		$command->bindParameter(':source',$text,PDO::PARAM_STR);

		return ($command->execute()==1) ? $this->updateCatalogueTime($cat_id, $variant) : false;
	}

	/**
	 * Returns a list of catalogue as key and all it variants as value.
	 * @return array list of catalogues
	 */
	function catalogues()
	{
		$command=$this->getDBConnection()->createCommand( 'SELECT name FROM catalogue ORDER BY name');
		$dataReader=$command->query();

		$result = array();

		foreach ($dataReader as $row)
		{
			$details = explode('.',$row[0]);
			if(!isset($details[1])) $details[1] = null;

			$result[] = $details;
		}

		return $result;
	}

}
?>
