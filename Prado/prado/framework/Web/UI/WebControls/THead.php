<?php
/**
 * THead class file
 *
 * @author Marcus Nyeholt <tanus@users.sourceforge.net> and Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: THead.php 2943 2011-06-01 20:01:35Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 */

/**
 * THead class
 *
 * THead displays a head element on a page. It displays the content
 * enclosed in its body and the page title set by the
 * {@link setTitle Title} property. In addition, stylesheets and JavaScripts registered via
 * {@link TClientScriptManager::registerStyleSheet}, {@link TClientScriptManager::registerStyleSheetFile}
 * {@link TClientScriptManager::registerHeadJavaScript}, and
 * {@link TClientScriptManager::registerHeadJavaScriptFile} will also be displayed
 * in the head.
 * THead also manages and displays meta tags through its {@link getMetaTags MetaTags}
 * property. You can add a meta object to the collection in code dynamically,
 * or add it in template using the following syntax,
 * <code>
 * <com:THead>
 *   <com:TMetaTag HttpEquiv="Pragma" Content="no-cache" />
 *   <com:TMetaTag Name="keywords" Content="Prado" />
 * </com:THead>
 * </code>
 *
 * Note, {@link TPage} has a property {@link TPage::getHead Head} that refers to
 * the THead control currently on the page. A page can have at most one THead
 * control. Although not required, it is recommended to place a THead on your page.
 * Without a THead on the page, stylesheets and javascripts in the current page
 * theme will not be rendered.
 *
 * @author Marcus Nyeholt <tanus@users.sourceforge.net> and Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THead.php 2943 2011-06-01 20:01:35Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 * @since 3.0
 */
class THead extends TControl
{
	/**
	 * @var TList list of meta name tags to be loaded by {@link THead}
	 */
	private $_metaTags=null;

	/**
	 * Registers the head control with the current page.
	 * This method is invoked when the control enters 'Init' stage.
	 * The method raises 'Init' event.
	 * If you override this method, be sure to call the parent implementation
	 * so that the event handlers can be invoked.
	 * @param TEventParameter event parameter to be passed to the event handlers
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		$this->getPage()->setHead($this);
	}

	/**
	 * Processes an object that is created during parsing template.
	 * This method adds TMetaTag components into the {@link getMetaTags MetaTags}
	 * collection of the head control.
	 * @param string|TComponent text string or component parsed and instantiated in template
	 * @see createdOnTemplate
	 */
	public function addParsedObject($object)
	{
		if($object instanceof TMetaTag)
			$this->getMetaTags()->add($object);
		else
			parent::addParsedObject($object);
	}

	/**
	 * @return string the page title.
	 */
	public function getTitle()
	{
		return $this->getViewState('Title','');
	}

	/**
	 * Sets the page title.
	 * This title will be rendered only if the {@link TPage::getTitle Title} property
	 * of the page is empty.
	 * @param string the page title.
	 */
	public function setTitle($value)
	{
		$this->setViewState('Title',$value,'');
	}

	/**
	 * @return string base URL of the page. This URL is rendered as the 'href' attribute of <base> tag. Defaults to ''.
	 */
	public function getBaseUrl()
	{
		return $this->getViewState('BaseUrl','');
	}

	/**
	 * @param string base URL of the page. This URL is rendered as the 'href' attribute of <base> tag.
	 */
	public function setBaseUrl($url)
	{
		$this->setViewState('BaseUrl',$url,'');
	}

	/**
	 * @return string the URL for the shortcut icon of the page. Defaults to ''.
	 */
	public function getShortcutIcon()
	{
		return $this->getViewState('ShortcutIcon','');
	}

	/**
	 * @param string the URL for the shortcut icon of the page.
	 */
	public function setShortcutIcon($url)
	{
		$this->setViewState('ShortcutIcon',$url,'');
	}

	/**
	 * @return TMetaTagCollection meta tag collection
	 */
	public function getMetaTags()
	{
		if(($metaTags=$this->getViewState('MetaTags',null))===null)
		{
			$metaTags=new TMetaTagCollection;
			$this->setViewState('MetaTags',$metaTags,null);
		}
		return $metaTags;
	}

	/**
	 * Renders the head control.
	 * @param THtmlWriter the writer for rendering purpose.
	 */
	public function render($writer)
	{
		$page=$this->getPage();
		$title=$this->getTitle();
		$writer->write("<head>\n<title>".THttpUtility::htmlEncode($title)."</title>\n");
		if(($baseUrl=$this->getBaseUrl())!=='')
			$writer->write('<base href="'.$baseUrl."\" />\n");
		if(($icon=$this->getShortcutIcon())!=='')
			$writer->write('<link rel="shortcut icon" href="'.$icon."\" />\n");

		if(($metaTags=$this->getMetaTags())!==null)
		{
			foreach($metaTags as $metaTag)
			{
				$metaTag->render($writer);
				$writer->writeLine();
			}
		}
		$cs=$page->getClientScript();
		$cs->renderStyleSheetFiles($writer);
		$cs->renderStyleSheets($writer);
		if($page->getClientSupportsJavaScript())
		{
			$cs->renderHeadScriptFiles($writer);
			$cs->renderHeadScripts($writer);
		}
		parent::render($writer);
		$writer->write("</head>\n");
	}
}

/**
 * TMetaTag class.
 *
 * TMetaTag represents a meta tag appearing in a page head section.
 * You can set its {@link setID ID}, {@link setHttpEquiv HttpEquiv},
 * {@link setName Name}, {@link setContent Content}, {@link setScheme Scheme}
 * properties, which correspond to id, http-equiv, name, content, and scheme
 * attributes for a meta tag, respectively.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THead.php 2943 2011-06-01 20:01:35Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TMetaTag extends TComponent
{
	/**
	 * @var string id of the meta tag
	 */
	private $_id='';
	/**
	 * @var string http-equiv attribute of the meta tag
	 */
	private $_httpEquiv='';
	/**
	 * @var string name attribute of the meta tag
	 */
	private $_name='';
	/**
	 * @var string content attribute of the meta tag
	 */
	private $_content='';
	/**
	 * @var string scheme attribute of the meta tag
	 */
	private $_scheme='';

	/**
	 * @return string id of the meta tag
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * @param string id of the meta tag
	 */
	public function setID($value)
	{
		$this->_id=$value;
	}

	/**
	 * @return string http-equiv attribute of the meta tag
	 */
	public function getHttpEquiv()
	{
		return $this->_httpEquiv;
	}

	/**
	 * @param string http-equiv attribute of the meta tag
	 */
	public function setHttpEquiv($value)
	{
		$this->_httpEquiv=$value;
	}

	/**
	 * @return string name attribute of the meta tag
	 */
	public function getName()
	{
		return $this->_name;
	}

	/**
	 * @param string name attribute of the meta tag
	 */
	public function setName($value)
	{
		$this->_name=$value;
	}

	/**
	 * @return string content attribute of the meta tag
	 */
	public function getContent()
	{
		return $this->_content;
	}

	/**
	 * @param string content attribute of the meta tag
	 */
	public function setContent($value)
	{
		$this->_content=$value;
	}

	/**
	 * @return string scheme attribute of the meta tag
	 */
	public function getScheme()
	{
		return $this->_scheme;
	}

	/**
	 * @param string scheme attribute of the meta tag
	 */
	public function setScheme($value)
	{
		$this->_scheme=$value;
	}

	/**
	 * Renders the meta tag.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function render($writer)
	{
		if($this->_id!=='')
			$writer->addAttribute('id',$this->_id);
		if($this->_name!=='')
			$writer->addAttribute('name',$this->_name);
		if($this->_httpEquiv!=='')
			$writer->addAttribute('http-equiv',$this->_httpEquiv);
		if($this->_scheme!=='')
			$writer->addAttribute('scheme',$this->_scheme);
		$writer->addAttribute('content',$this->_content);
		$writer->renderBeginTag('meta');
		$writer->renderEndTag();
	}
}


/**
 * TMetaTagCollection class
 *
 * TMetaTagCollection represents a collection of meta tags
 * contained in a {@link THead} control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THead.php 2943 2011-06-01 20:01:35Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TMetaTagCollection extends TList
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing type
	 * check on the item being added.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a {@link TMetaTag}
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TMetaTag)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('metatagcollection_metatag_invalid');
	}

	/**
	 * Finds the lowest cardinal index of the meta tag whose id is the one being looked for.
	 * @param string the ID of the meta tag to be looked for
	 * @return integer the index of the meta tag found, -1 if not found.
	 */
	public function findIndexByID($id)
	{
		$index=0;
		foreach($this as $item)
		{
			if($item->getID()===$id)
				return $index;
			$index++;
		}
		return -1;
	}

	/**
	 * Finds the item whose value is the one being looked for.
	 * @param string the id of the meta tag to be looked for
	 * @return TMetaTag the meta tag found, null if not found.
	 */
	public function findMetaTagByID($id)
	{
		if(($index=$this->findIndexByID($id))>=0)
			return $this->itemAt($index);
		else
			return null;
	}
}

?>
