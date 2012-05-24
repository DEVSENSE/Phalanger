<?php
/**
 * BlogDataModule class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: BlogDataModule.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * BlogDataModule class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class BlogDataModule extends TModule
{
	const DB_FILE_EXT='.db';
	const DEFAULT_DB_FILE='Application.Data.Blog';
	private $_db=null;
	private $_dbFile=null;

	public function init($config)
	{
		$this->connectDatabase();
	}

	public function getDbFile()
	{
		if($this->_dbFile===null)
			$this->_dbFile=Prado::getPathOfNamespace(self::DEFAULT_DB_FILE,self::DB_FILE_EXT);
		return $this->_dbFile;
	}

	public function setDbFile($value)
	{
		if(($this->_dbFile=Prado::getPathOfNamespace($value,self::DB_FILE_EXT))===null)
			throw new BlogException(500,'blogdatamodule_dbfile_invalid',$value);
	}

	protected function createDatabase()
	{
		$schemaFile=dirname(__FILE__).'/schema.sql';
		$statements=explode(';',file_get_contents($schemaFile));
		foreach($statements as $statement)
		{
			if(trim($statement)!=='')
			{
				if(@sqlite_query($this->_db,$statement)===false)
					throw new BlogException(500,'blogdatamodule_createdatabase_failed',sqlite_error_string(sqlite_last_error($this->_db)),$statement);
			}
		}
	}

	protected function connectDatabase()
	{
		$dbFile=$this->getDbFile();
		$newDb=!is_file($dbFile);
		$error='';
		if(($this->_db=sqlite_open($dbFile,0666,$error))===false)
			throw new BlogException(500,'blogdatamodule_dbconnect_failed',$error);
		if($newDb)
			$this->createDatabase();
	}

	protected function generateModifier($filter,$orderBy,$limit)
	{
		$modifier='';
		if($filter!=='')
			$modifier=' WHERE '.$filter;
		if($orderBy!=='')
			$modifier.=' ORDER BY '.$orderBy;
		if($limit!=='')
			$modifier.=' LIMIT '.$limit;
		return $modifier;
	}

	public function query($sql)
	{
		if(($result=@sqlite_query($this->_db,$sql))!==false)
			return $result;
		else
			throw new BlogException(500,'blogdatamodule_query_failed',sqlite_error_string(sqlite_last_error($this->_db)),$sql);
	}

	protected function populateUserRecord($row)
	{
		$userRecord=new UserRecord;
		$userRecord->ID=(integer)$row['id'];
		$userRecord->Name=$row['name'];
		$userRecord->FullName=$row['full_name'];
		$userRecord->Role=(integer)$row['role'];
		$userRecord->Password=$row['passwd'];
		$userRecord->VerifyCode=$row['vcode'];
		$userRecord->Email=$row['email'];
		$userRecord->CreateTime=(integer)$row['reg_time'];
		$userRecord->Status=(integer)$row['status'];
		$userRecord->Website=$row['website'];
		return $userRecord;
	}

	public function queryUsers($filter='',$orderBy='',$limit='')
	{
		if($filter!=='')
			$filter='WHERE '.$filter;
		$sql="SELECT * FROM tblUsers $filter $orderBy $limit";
		$result=$this->query($sql);
		$rows=sqlite_fetch_all($result,SQLITE_ASSOC);
		$users=array();
		foreach($rows as $row)
			$users[]=$this->populateUserRecord($row);
		return $users;
	}

	public function queryUserCount($filter)
	{
		if($filter!=='')
			$filter='WHERE '.$filter;
		$sql="SELECT COUNT(id) AS user_count FROM tblUsers $filter";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $row['user_count'];
		else
			return 0;
	}

	public function queryUserByID($id)
	{
		$sql="SELECT * FROM tblUsers WHERE id=$id";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $this->populateUserRecord($row);
		else
			return null;
	}

	public function queryUserByName($name)
	{
		$name=sqlite_escape_string($name);
		$sql="SELECT * FROM tblUsers WHERE name='$name'";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $this->populateUserRecord($row);
		else
			return null;
	}

	public function insertUser($user)
	{
		$name=sqlite_escape_string($user->Name);
		$fullName=sqlite_escape_string($user->FullName);
		$passwd=sqlite_escape_string($user->Password);
		$email=sqlite_escape_string($user->Email);
		$website=sqlite_escape_string($user->Website);
		$createTime=time();
		$sql="INSERT INTO tblUsers ".
				"(name,full_name,role,passwd,email,reg_time,status,website) ".
				"VALUES ('$name','$fullName',{$user->Role},'$passwd','$email',$createTime,{$user->Status},'$website')";
		$this->query($sql);
		$user->ID=sqlite_last_insert_rowid($this->_db);
	}

	public function updateUser($user)
	{
		$name=sqlite_escape_string($user->Name);
		$fullName=sqlite_escape_string($user->FullName);
		$passwd=sqlite_escape_string($user->Password);
		$email=sqlite_escape_string($user->Email);
		$website=sqlite_escape_string($user->Website);
		$sql="UPDATE tblUsers SET
				name='$name',
				full_name='$fullName',
				role={$user->Role},
				passwd='$passwd',
				vcode='{$user->VerifyCode}',
				email='$email',
				status={$user->Status},
				website='$website'
				WHERE id={$user->ID}";
		$this->query($sql);
	}

	public function deleteUser($id)
	{
		$this->query("DELETE FROM tblUsers WHERE id=$id");
	}

	protected function populatePostRecord($row)
	{
		$postRecord=new PostRecord;
		$postRecord->ID=(integer)$row['id'];
		$postRecord->AuthorID=(integer)$row['author_id'];
		if($row['author_full_name']!=='')
			$postRecord->AuthorName=$row['author_full_name'];
		else
			$postRecord->AuthorName=$row['author_name'];
		$postRecord->CreateTime=(integer)$row['create_time'];
		$postRecord->ModifyTime=(integer)$row['modify_time'];
		$postRecord->Title=$row['title'];
		$postRecord->Content=$row['content'];
		$postRecord->Status=(integer)$row['status'];
		$postRecord->CommentCount=(integer)$row['comment_count'];
		return $postRecord;
	}

	public function queryPosts($postFilter,$categoryFilter,$orderBy,$limit)
	{
		$filter='';
		if($postFilter!=='')
			$filter.=" AND $postFilter";
		if($categoryFilter!=='')
			$filter.=" AND a.id IN (SELECT post_id AS id FROM tblPost2Category WHERE $categoryFilter)";
		$sql="SELECT a.id AS id,
					a.author_id AS author_id,
					b.name AS author_name,
					b.full_name AS author_full_name,
					a.create_time AS create_time,
					a.modify_time AS modify_time,
					a.title AS title,
					a.content AS content,
					a.status AS status,
					a.comment_count AS comment_count
				FROM tblPosts a, tblUsers b
				WHERE a.author_id=b.id $filter $orderBy $limit";
		$result=$this->query($sql);
		$rows=sqlite_fetch_all($result,SQLITE_ASSOC);
		$posts=array();
		foreach($rows as $row)
			$posts[]=$this->populatePostRecord($row);
		return $posts;
	}

	public function queryPostCount($postFilter,$categoryFilter)
	{
		$filter='';
		if($postFilter!=='')
			$filter.=" AND $postFilter";
		if($categoryFilter!=='')
			$filter.=" AND a.id IN (SELECT post_id AS id FROM tblPost2Category WHERE $categoryFilter)";
		$sql="SELECT COUNT(a.id) AS post_count
				FROM tblPosts a, tblUsers b
				WHERE a.author_id=b.id $filter";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $row['post_count'];
		else
			return 0;
	}

	public function queryPostByID($id)
	{
		$sql="SELECT a.id AS id,
		             a.author_id AS author_id,
		             b.name AS author_name,
		             b.full_name AS author_full_name,
		             a.create_time AS create_time,
		             a.modify_time AS modify_time,
		             a.title AS title,
		             a.content AS content,
		             a.status AS status,
		             a.comment_count AS comment_count
		      FROM tblPosts a, tblUsers b
		      WHERE a.id=$id AND a.author_id=b.id";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $this->populatePostRecord($row);
		else
			return null;
	}

	public function escapeString($string)
	{
		return sqlite_escape_string($string);
	}

	public function insertPost($post,$catIDs)
	{
		$title=sqlite_escape_string($post->Title);
		$content=sqlite_escape_string($post->Content);
		$sql="INSERT INTO tblPosts
				(author_id,create_time,modify_time,title,content,status)
				VALUES ({$post->AuthorID},{$post->CreateTime},{$post->ModifyTime},'$title','$content',{$post->Status})";
		$this->query($sql);
		$post->ID=sqlite_last_insert_rowid($this->_db);
		foreach($catIDs as $catID)
			$this->insertPostCategory($post->ID,$catID);
	}

	public function updatePost($post,$newCatIDs=null)
	{
		if($newCatIDs!==null)
		{
			$cats=$this->queryCategoriesByPostID($post->ID);
			$catIDs=array();
			foreach($cats as $cat)
				$catIDs[]=$cat->ID;
			$deleteIDs=array_diff($catIDs,$newCatIDs);
			foreach($deleteIDs as $id)
				$this->deletePostCategory($post->ID,$id);
			$insertIDs=array_diff($newCatIDs,$catIDs);
			foreach($insertIDs as $id)
				$this->insertPostCategory($post->ID,$id);
		}

		$title=sqlite_escape_string($post->Title);
		$content=sqlite_escape_string($post->Content);
		$sql="UPDATE tblPosts SET
				modify_time={$post->ModifyTime},
				title='$title',
				content='$content',
				status={$post->Status}
				WHERE id={$post->ID}";
		$this->query($sql);
	}

	public function deletePost($id)
	{
		$cats=$this->queryCategoriesByPostID($id);
		foreach($cats as $cat)
			$this->deletePostCategory($id,$cat->ID);
		$this->query("DELETE FROM tblComments WHERE post_id=$id");
		$this->query("DELETE FROM tblPosts WHERE id=$id");
	}

	protected function populateCommentRecord($row)
	{
		$commentRecord=new CommentRecord;
		$commentRecord->ID=(integer)$row['id'];
		$commentRecord->PostID=(integer)$row['post_id'];
		$commentRecord->AuthorName=$row['author_name'];
		$commentRecord->AuthorEmail=$row['author_email'];
		$commentRecord->AuthorWebsite=$row['author_website'];
		$commentRecord->AuthorIP=$row['author_ip'];
		$commentRecord->CreateTime=(integer)$row['create_time'];
		$commentRecord->Content=$row['content'];
		$commentRecord->Status=(integer)$row['status'];
		return $commentRecord;
	}

	public function queryComments($filter,$orderBy,$limit)
	{
		if($filter!=='')
			$filter='WHERE '.$filter;
		$sql="SELECT * FROM tblComments $filter $orderBy $limit";
		$result=$this->query($sql);
		$rows=sqlite_fetch_all($result,SQLITE_ASSOC);
		$comments=array();
		foreach($rows as $row)
			$comments[]=$this->populateCommentRecord($row);
		return $comments;
	}

	public function queryCommentsByPostID($id)
	{
		$sql="SELECT * FROM tblComments WHERE post_id=$id ORDER BY create_time DESC";
		$result=$this->query($sql);
		$rows=sqlite_fetch_all($result,SQLITE_ASSOC);
		$comments=array();
		foreach($rows as $row)
			$comments[]=$this->populateCommentRecord($row);
		return $comments;
	}

	public function insertComment($comment)
	{
		$authorName=sqlite_escape_string($comment->AuthorName);
		$authorEmail=sqlite_escape_string($comment->AuthorEmail);
		$authorWebsite=sqlite_escape_string($comment->AuthorWebsite);
		$content=sqlite_escape_string($comment->Content);
		$sql="INSERT INTO tblComments
				(post_id,author_name,author_email,author_website,author_ip,create_time,status,content)
				VALUES ({$comment->PostID},'$authorName','$authorEmail','$authorWebsite','{$comment->AuthorIP}',{$comment->CreateTime},{$comment->Status},'$content')";
		$this->query($sql);
		$comment->ID=sqlite_last_insert_rowid($this->_db);
		$this->query("UPDATE tblPosts SET comment_count=comment_count+1 WHERE id={$comment->PostID}");
	}

	public function updateComment($comment)
	{
		$authorName=sqlite_escape_string($comment->AuthorName);
		$authorEmail=sqlite_escape_string($comment->AuthorEmail);
		$content=sqlite_escape_string($comment->Content);
		$sql="UPDATE tblComments SET status={$comment->Status} WHERE id={$comment->ID}";
		$this->query($sql);
	}

	public function deleteComment($id)
	{
		$result=$this->query("SELECT post_id FROM tblComments WHERE id=$id");
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
		{
			$postID=$row['post_id'];
			$this->query("DELETE FROM tblComments WHERE id=$id");
			$this->query("UPDATE tblPosts SET comment_count=comment_count-1 WHERE id=$postID");
		}
	}

	protected function populateCategoryRecord($row)
	{
		$catRecord=new CategoryRecord;
		$catRecord->ID=(integer)$row['id'];
		$catRecord->Name=$row['name'];
		$catRecord->Description=$row['description'];
		$catRecord->PostCount=$row['post_count'];
		return $catRecord;
	}

	public function queryCategories()
	{
		$sql="SELECT * FROM tblCategories ORDER BY name ASC";
		$result=$this->query($sql);
		$rows=sqlite_fetch_all($result,SQLITE_ASSOC);
		$cats=array();
		foreach($rows as $row)
			$cats[]=$this->populateCategoryRecord($row);
		return $cats;
	}

	public function queryCategoriesByPostID($postID)
	{
		$sql="SELECT a.id AS id,
				a.name AS name,
				a.description AS description,
				a.post_count AS post_count
				FROM tblCategories a, tblPost2Category b
				WHERE a.id=b.category_id AND b.post_id=$postID ORDER BY a.name";
		$result=$this->query($sql);
		$rows=sqlite_fetch_all($result,SQLITE_ASSOC);
		$cats=array();
		foreach($rows as $row)
			$cats[]=$this->populateCategoryRecord($row);
		return $cats;
	}

	public function queryCategoryByID($id)
	{
		$sql="SELECT * FROM tblCategories WHERE id=$id";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $this->populateCategoryRecord($row);
		else
			return null;
	}

	public function queryCategoryByName($name)
	{
		$name=sqlite_escape_string($name);
		$sql="SELECT * FROM tblCategories WHERE name='$name'";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $this->populateCategoryRecord($row);
		else
			return null;
	}

	public function insertCategory($category)
	{
		$name=sqlite_escape_string($category->Name);
		$description=sqlite_escape_string($category->Description);
		$sql="INSERT INTO tblCategories
				(name,description)
				VALUES ('$name','$description')";
		$this->query($sql);
		$category->ID=sqlite_last_insert_rowid($this->_db);
	}

	public function updateCategory($category)
	{
		$name=sqlite_escape_string($category->Name);
		$description=sqlite_escape_string($category->Description);
		$sql="UPDATE tblCategories SET name='$name', description='$description', post_count={$category->PostCount} WHERE id={$category->ID}";
		$this->query($sql);
	}

	public function deleteCategory($id)
	{
		$sql="DELETE FROM tblPost2Category WHERE category_id=$id";
		$this->query($sql);
		$sql="DELETE FROM tblCategories WHERE id=$id";
		$this->query($sql);
	}

	public function insertPostCategory($postID,$categoryID)
	{
		$sql="INSERT INTO tblPost2Category (post_id, category_id) VALUES ($postID, $categoryID)";
		$this->query($sql);
		$sql="UPDATE tblCategories SET post_count=post_count+1 WHERE id=$categoryID";
		$this->query($sql);
	}

	public function deletePostCategory($postID,$categoryID)
	{
		$sql="DELETE FROM tblPost2Category WHERE post_id=$postID AND category_id=$categoryID";
		if($this->query($sql)>0)
		{
			$sql="UPDATE tblCategories SET post_count=post_count-1 WHERE id=$categoryID";
			$this->query($sql);
		}
	}

	public function queryEarliestPostTime()
	{
		$sql="SELECT MIN(create_time) AS create_time FROM tblPosts";
		$result=$this->query($sql);
		if(($row=sqlite_fetch_array($result,SQLITE_ASSOC))!==false)
			return $row['create_time'];
		else
			return time();
	}
}

class UserRecord
{
	const ROLE_USER=0;
	const ROLE_ADMIN=1;
	const STATUS_NORMAL=0;
	const STATUS_DISABLED=1;
	const STATUS_PENDING=2;
	public $ID;
	public $Name;
	public $FullName;
	public $Role;
	public $Password;
	public $VerifyCode;
	public $Email;
	public $CreateTime;
	public $Status;
	public $Website;
}

class PostRecord
{
	const STATUS_PUBLISHED=0;
	const STATUS_DRAFT=1;
	const STATUS_PENDING=2;
	const STATUS_STICKY=3;
	public $ID;
	public $AuthorID;
	public $AuthorName;
	public $CreateTime;
	public $ModifyTime;
	public $Title;
	public $Content;
	public $Status;
	public $CommentCount;
}

class CommentRecord
{
	public $ID;
	public $PostID;
	public $AuthorName;
	public $AuthorEmail;
	public $AuthorWebsite;
	public $AuthorIP;
	public $CreateTime;
	public $Status;
	public $Content;
}

class CategoryRecord
{
	public $ID;
	public $Name;
	public $Description;
	public $PostCount;
}

?>