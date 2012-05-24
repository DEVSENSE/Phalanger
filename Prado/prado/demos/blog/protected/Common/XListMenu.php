<?php
/**
 * XListMenu and XListMenuItem class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: XListMenu.php 1398 2006-09-08 19:31:03Z xue $
 */

Prado::using('System.Web.UI.WebControls.TListControl');

/**
 * XListMenu class
 *
 * XListMenu displays a list of hyperlinks that can be used for page menus.
 * Menu items adjust their css class automatically according to the current
 * page displayed. In particular, a menu item is considered as active if
 * the URL it represents is for the page currently displayed.
 *
 * Usage of XListMenu is similar to PRADO list controls. Each list item has
 * two extra properties: {@link XListMenuItem::setPagePath PagePath} and
 * {@link XListMenuItem::setNavigateUrl NavigateUrl}. The former is used to
 * determine if the item is active or not, while the latter specifies the
 * URL for the item. If the latter is not specified, a URL to the page is
 * generated automatically.
 *
 * In template, you may use the following tags to specify a menu:
 * <code>
 *   <com:XListMenu ActiveCssClass="class1" InactiveCssClass="class2">
 *      <com:XListMenuItem Text="Menu 1" PagePath="Page1" />
 *      <com:XListMenuItem Text="Menu 2" PagePath="Page2" NavigateUrl="/page2" />
 *   </com:XListMenu>
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class XListMenu extends TListControl
{
	public function addParsedObject($object)
	{
		if($object instanceof XListMenuItem)
			parent::addParsedObject($object);
	}

	public function getActiveCssClass()
	{
		return $this->getViewState('ActiveCssClass','');
	}

	public function setActiveCssClass($value)
	{
		$this->setViewState('ActiveCssClass',$value,'');
	}

	public function getInactiveCssClass()
	{
		return $this->getViewState('InactiveCssClass','');
	}

	public function setInactiveCssClass($value)
	{
		$this->setViewState('InactiveCssClass',$value,'');
	}

	public function render($writer)
	{
		if(($activeClass=$this->getActiveCssClass())!=='')
			$activeClass=' class="'.$activeClass.'"';
		if(($inactiveClass=$this->getInactiveCssClass())!=='')
			$inactiveClass=' class="'.$inactiveClass.'"';
		$currentPagePath=$this->getPage()->getPagePath();
		$writer->write("<ul>\n");
		foreach($this->getItems() as $item)
		{
			$pagePath=$item->getPagePath();
			//if(strpos($currentPagePath.'.',$pagePath.'.')===0)
			if($pagePath[strlen($pagePath)-1]==='*')
			{
				if(strpos($currentPagePath.'.',rtrim($pagePath,'*'))===0)
					$cssClass=$activeClass;
				else
					$cssClass=$inactiveClass;
			}
			else
			{
				if($pagePath===$currentPagePath)
					$cssClass=$activeClass;
				else
					$cssClass=$inactiveClass;
			}
			if(($url=$item->getNavigateUrl())==='')
				$url=$this->getService()->constructUrl($pagePath);
			$writer->write("<li><a href=\"$url\"$cssClass>".$item->getText()."</a></li>\n");
		}
		$writer->write("</ul>");
	}
}

class XListMenuItem extends TListItem
{
	public function getPagePath()
	{
		return $this->getValue();
	}

	public function setPagePath($value)
	{
		$this->setValue($value);
	}

	public function getNavigateUrl()
	{
		return $this->hasAttribute('NavigateUrl')?$this->getAttribute('NavigateUrl'):'';
	}

	public function setNavigateUrl($value)
	{
		$this->setAttribute('NavigateUrl',$value);
	}
}

?>