<?php
/**
 * TTabPanel class file.
 *
 * @author Tomasz Wolny <tomasz.wolny@polecam.to.pl> and Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTabPanel.php 3013 2011-07-16 11:19:23Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */

/**
 * Class TTabPanel.
 *
 * TTabPanel displays a tabbed panel. Users can click on the tab bar to switching among
 * different tab views. Each tab view is an independent panel that can contain arbitrary content.
 *
 * A TTabPanel control consists of one or several {@link TTabView} controls representing the possible
 * tab views. At any time, only one tab view is visible (active), which is specified by any of
 * the following properties:
 * - {@link setActiveViewIndex ActiveViewIndex} - the zero-based integer index of the view in the view collection.
 * - {@link setActiveViewID ActiveViewID} - the text ID of the visible view.
 * - {@link setActiveView ActiveView} - the visible view instance.
 * If both {@link setActiveViewIndex ActiveViewIndex} and {@link setActiveViewID ActiveViewID}
 * are set, the latter takes precedence.
 *
 * TTabPanel uses CSS to specify the appearance of the tab bar and panel. By default,
 * an embedded CSS file will be published which contains the default CSS for TTabPanel.
 * You may also use your own CSS file by specifying the {@link setCssUrl CssUrl} property.
 * The following properties specify the CSS classes used for elements in a TTabPanel:
 * - {@link setCssClass CssClass} - the CSS class name for the outer-most div element (defaults to 'tab-panel');
 * - {@link setTabCssClass TabCssClass} - the CSS class name for nonactive tab div elements (defaults to 'tab-normal');
 * - {@link setActiveTabCssClass ActiveTabCssClass} - the CSS class name for the active tab div element (defaults to 'tab-active');
 * - {@link setViewCssClass ViewCssClass} - the CSS class for the div element enclosing view content (defaults to 'tab-view');
 *
 * To use TTabPanel, write a template like following:
 * <code>
 * <com:TTabPanel>
 *   <com:TTabView Caption="View 1">
 *     content for view 1
 *   </com:TTabView>
 *   <com:TTabView Caption="View 2">
 *     content for view 2
 *   </com:TTabView>
 *   <com:TTabView Caption="View 3">
 *     content for view 3
 *   </com:TTabView>
 * </com:TTabPanel>
 * </code>
 *
 * @author Tomasz Wolny <tomasz.wolny@polecam.to.pl> and Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTabPanel.php 3013 2011-07-16 11:19:23Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TTabPanel extends TWebControl implements IPostBackDataHandler
{
	private $_dataChanged=false;

	/**
	 * @return string tag name for the control
	 */
	protected function getTagName()
	{
		return 'div';
	}

	/**
	 * Adds object parsed from template to the control.
	 * This method adds only {@link TTabView} objects into the {@link getViews Views} collection.
	 * All other objects are ignored.
	 * @param mixed object parsed from template
	 */
	public function addParsedObject($object)
	{
		if($object instanceof TTabView)
			$this->getControls()->add($object);
	}

	/**
     * Returns the index of the active tab view.
     * Note, this property may not return the correct index.
     * To ensure the correctness, call {@link getActiveView()} first.
	 * @return integer the zero-based index of the active tab view. If -1, it means no active tab view. Default is 0 (the first view is active).
	 */
	public function getActiveViewIndex()
	{
		return $this->getViewState('ActiveViewIndex',0);
	}

	/**
	 * @param integer the zero-based index of the current view in the view collection. -1 if no active view.
	 * @throws TInvalidDataValueException if the view index is invalid
	 */
	public function setActiveViewIndex($value)
	{
		$this->setViewState('ActiveViewIndex',TPropertyValue::ensureInteger($value),0);
	}

    /**
     * Returns the ID of the active tab view.
     * Note, this property may not return the correct ID.
     * To ensure the correctness, call {@link getActiveView()} first.
     * @return string The ID of the active tab view. Defaults to '', meaning not set.
     */
    public function getActiveViewID()
    {
		return $this->getViewState('ActiveViewID','');
    }

    /**
     * @param string The ID of the active tab view.
     */
    public function setActiveViewID($value)
    {
		$this->setViewState('ActiveViewID',$value,'');
    }

	/**
	 * Returns the currently active view.
	 * This method will examin the ActiveViewID, ActiveViewIndex and Views collection to
	 * determine which view is currently active. It will update ActiveViewID and ActiveViewIndex accordingly.
	 * @return TTabView the currently active view, null if no active view
	 * @throws TInvalidDataValueException if the active view ID or index set previously is invalid
	 */
	public function getActiveView()
	{
		$activeView=null;
		$views=$this->getViews();
		if(($id=$this->getActiveViewID())!=='')
		{
			if(($index=$views->findIndexByID($id))>=0)
				$activeView=$views->itemAt($index);
			else
				throw new TInvalidDataValueException('tabpanel_activeviewid_invalid',$id);
		}
		else if(($index=$this->getActiveViewIndex())>=0)
		{
			if($index<$views->getCount())
				$activeView=$views->itemAt($index);
			else
				throw new TInvalidDataValueException('tabpanel_activeviewindex_invalid',$index);
		}
		else
		{
			foreach($views as $index=>$view)
			{
				if($view->getActive())
				{
					$activeView=$view;
					break;
				}
			}
		}
		if($activeView!==null)
			$this->activateView($activeView);
		return $activeView;
	}

	/**
	 * @param TTabView the view to be activated
	 * @throws TInvalidOperationException if the view is not in the view collection
	 */
	public function setActiveView($view)
	{
		if($this->getViews()->indexOf($view)>=0)
			$this->activateView($view);
		else
			throw new TInvalidOperationException('tabpanel_view_inexistent');
	}

    /**
     * @return string URL for the CSS file including all relevant CSS class definitions. Defaults to ''.
     */
    public function getCssUrl()
    {
        return $this->getViewState('CssUrl','default');
    }

    /**
     * @param string URL for the CSS file including all relevant CSS class definitions.
     */
    public function setCssUrl($value)
    {
        $this->setViewState('CssUrl',TPropertyValue::ensureString($value),'');
    }

    /**
     * @return string CSS class for the whole tab control div. Defaults to 'tab-panel'.
     */
    public function getCssClass()
    {
    	$cssClass=parent::getCssClass();
    	return $cssClass===''?'tab-panel':$cssClass;
    }

    /**
     * @return string CSS class for the currently displayed view div. Defaults to 'tab-view'.
     */
    public function getViewCssClass()
    {
        return $this->getViewStyle()->getCssClass();
    }

    /**
     * @param string CSS class for the currently displayed view div.
     */
    public function setViewCssClass($value)
    {
        $this->getViewStyle()->setCssClass($value);
    }

	/**
	 * @return TStyle the style for all the view div
	 */
	public function getViewStyle()
	{
		if(($style=$this->getViewState('ViewStyle',null))===null)
		{
			$style=new TStyle;
			$style->setCssClass('tab-view');
			$this->setViewState('ViewStyle',$style,null);
		}
		return $style;
	}

    /**
     * @return string CSS class for non-active tabs. Defaults to 'tab-normal'.
     */
    public function getTabCssClass()
    {
        return $this->getTabStyle()->getCssClass();
    }

    /**
     * @param string CSS class for non-active tabs.
     */
    public function setTabCssClass($value)
    {
        $this->getTabStyle()->setCssClass($value);
    }

	/**
	 * @return TStyle the style for all the inactive tab div
	 */
	public function getTabStyle()
	{
		if(($style=$this->getViewState('TabStyle',null))===null)
		{
			$style=new TStyle;
			$style->setCssClass('tab-normal');
			$this->setViewState('TabStyle',$style,null);
		}
		return $style;
	}

    /**
     * @return string CSS class for the active tab. Defaults to 'tab-active'.
     */
    public function getActiveTabCssClass()
    {
        return $this->getActiveTabStyle()->getCssClass();
    }

    /**
     * @param string CSS class for the active tab.
     */
    public function setActiveTabCssClass($value)
    {
        $this->getActiveTabStyle()->setCssClass($value);
    }

	/**
	 * @return TStyle the style for the active tab div
	 */
	public function getActiveTabStyle()
	{
		if(($style=$this->getViewState('ActiveTabStyle',null))===null)
		{
			$style=new TStyle;
			$style->setCssClass('tab-active');
			$this->setViewState('ActiveTabStyle',$style,null);
		}
		return $style;
	}

	/**
	 * Activates the specified view.
	 * If there is any other view currently active, it will be deactivated.
	 * @param TTabView the view to be activated. If null, all views will be deactivated.
	 */
	protected function activateView($view)
	{
		$this->setActiveViewIndex(-1);
		$this->setActiveViewID('');
		foreach($this->getViews() as $index=>$v)
		{
			if($view===$v)
			{
				$this->setActiveViewIndex($index);
				$this->setActiveViewID($view->getID(false));
				$view->setActive(true);
			}
			else
				$v->setActive(false);
		}
	}

	/**
	 * Loads user input data.
	 * This method is primarly used by framework developers.
	 * @param string the key that can be used to retrieve data from the input data collection
	 * @param array the input data collection
	 * @return boolean whether the data of the control has been changed
	 */
	public function loadPostData($key,$values)
	{
		if(($index=$values[$this->getClientID().'_1'])!==null)
		{
			$index=(int)$index;
			$currentIndex=$this->getActiveViewIndex();
			if($currentIndex!==$index)
			{
				$this->setActiveViewID(''); // clear up view ID
				$this->setActiveViewIndex($index);
				return $this->_dataChanged=true;
			}
		}
		return false;
	}

	/**
	 * Raises postdata changed event.
	 * This method is required by {@link IPostBackDataHandler} interface.
	 * It is invoked by the framework when {@link getActiveViewIndex ActiveViewIndex} property
	 * is changed on postback.
	 * This method is primarly used by framework developers.
	 */
	public function raisePostDataChangedEvent()
	{
		// do nothing
	}

	/**
	 * Returns a value indicating whether postback has caused the control data change.
	 * This method is required by the IPostBackDataHandler interface.
	 * @return boolean whether postback has caused the control data change. False if the page is not in postback mode.
	 */
	public function getDataChanged()
	{
		return $this->_dataChanged;
	}

	/**
	 * Adds attributes to renderer.
	 * @param THtmlWriter the renderer
	 */
	protected function addAttributesToRender($writer)
	{
		$writer->addAttribute('id',$this->getClientID());
		$this->setCssClass($this->getCssClass());
		parent::addAttributesToRender($writer);
	}

	/**
	 * Registers CSS and JS.
	 * This method is invoked right before the control rendering, if the control is visible.
	 * @param mixed event parameter
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$this->getActiveView();  // determine the active view
		$this->registerStyleSheet();
		$this->registerClientScript();
	}

	/**
	 * Registers the CSS relevant to the TTabControl.
	 * It will register the CSS file specified by {@link getCssUrl CssUrl}.
	 * If that is not set, it will use the default CSS.
	 */
	protected function registerStyleSheet()
	{
		$url = $this->getCssUrl();
		
		if($url === '') {
			return;
		}
		
		if($url === 'default') {
			$url = $this->getApplication()->getAssetManager()->publishFilePath(dirname(__FILE__).DIRECTORY_SEPARATOR.'assets'.DIRECTORY_SEPARATOR.'tabpanel.css');
		}
		
		if($url !== '') {
			$this->getPage()->getClientScript()->registerStyleSheetFile($url, $url);
		}
	}

	/**
	 * Registers the relevant JavaScript.
	 */
	protected function registerClientScript()
	{
		$id=$this->getClientID();
		$options=TJavaScript::encode($this->getClientOptions());
		$className=$this->getClientClassName();
		$page=$this->getPage();
		$cs=$page->getClientScript();
		$cs->registerPradoScript('tabpanel');
		$code="new $className($options);";
		$cs->registerEndScript("prado:$id", $code);
		// ensure an item is always active and visible
		$index = $this->getActiveViewIndex();
		if(!$this->getViews()->itemAt($index)->Visible)
			$index=0;
		$cs->registerHiddenField($id.'_1', $index);
		$page->registerRequiresPostData($this);
		$page->registerRequiresPostData($id."_1");
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TTabPanel';
	}

	/**
	 * @return array the options for JavaScript
	 */
	protected function getClientOptions()
	{
		$options['ID']=$this->getClientID();
		$options['ActiveCssClass']=$this->getActiveTabCssClass();
		$options['NormalCssClass']=$this->getTabCssClass();
		$viewIDs=array();
		$viewVis=array();
		foreach($this->getViews() as $view)
		{
			$viewIDs[]=$view->getClientID();
			$viewVis[]=$view->getVisible();
		}
		$options['Views']='[\''.implode('\',\'',$viewIDs).'\']';
		$options['ViewsVis']='[\''.implode('\',\'',$viewVis).'\']';

		return $options;
	}

	/**
	 * Creates a control collection object that is to be used to hold child controls
	 * @return TTabViewCollection control collection
	 */
	protected function createControlCollection()
	{
		return new TTabViewCollection($this);
	}

	/**
	 * @return TTabViewCollection list of {@link TTabView} controls
	 */
	public function getViews()
	{
		return $this->getControls();
	}

	/**
	 * Renders body contents of the tab control.
	 * @param THtmlWriter the writer used for the rendering purpose.
	 */
	public function renderContents($writer)
	{
		$views=$this->getViews();
		if($views->getCount()>0)
		{
			$writer->writeLine();
			// render tab bar
			foreach($views as $view)
			{
				$view->renderTab($writer);
				$writer->writeLine();
			}
			// render tab views
			foreach($views as $view)
			{
				$view->renderControl($writer);
				$writer->writeLine();
			}
		}
	}
}

/**
 * TTabView class.
 *
 * TTabView represents a view in a {@link TTabPanel} control.
 *
 * The content in a TTabView can be specified by the {@link setText Text} property
 * or its child controls. In template syntax, the latter means enclosing the content
 * within the TTabView component element. If both are set, {@link getText Text} takes precedence.
 *
 * Each TTabView is associated with a tab in the tab bar of the TTabPanel control.
 * The tab caption is specified by {@link setCaption Caption}. If {@link setNavigateUrl NavigateUrl}
 * is set, the tab will contain a hyperlink pointing to the specified URL. In this case,
 * clicking on the tab will redirect the browser to the specified URL.
 *
 * TTabView may be toggled between visible (active) and invisible (inactive) by
 * setting the {@link setActive Active} property.
 *
 * @author Tomasz Wolny <tomasz.wolny@polecam.to.pl> and Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTabPanel.php 3013 2011-07-16 11:19:23Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TTabView extends TWebControl
{
	private $_active=false;

	/**
	 * @return the tag name for the view element
	 */
	protected function getTagName()
	{
		return 'div';
	}

	/**
	 * Adds attributes to renderer.
	 * @param THtmlWriter the renderer
	 */
	protected function addAttributesToRender($writer)
	{
		if(!$this->getActive() && $this->getPage()->getClientSupportsJavaScript())
			$this->getStyle()->setStyleField('display','none');

		$this->getStyle()->mergeWith($this->getParent()->getViewStyle());

		parent::addAttributesToRender($writer);

		$writer->addAttribute('id',$this->getClientID());
	}

	/**
	 * @return string the caption displayed on this tab. Defaults to ''.
	 */
	public function getCaption()
	{
		return $this->getViewState('Caption','');
	}

	/**
	 * @param string the caption displayed on this tab
	 */
	public function setCaption($value)
	{
		$this->setViewState('Caption',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string the URL of the target page. Defaults to ''.
	 */
	public function getNavigateUrl()
	{
		return $this->getViewState('NavigateUrl','');
	}

	/**
	 * Sets the URL of the target page.
	 * If not empty, clicking on this tab will redirect the browser to the specified URL.
	 * @param string the URL of the target page.
	 */
	public function setNavigateUrl($value)
	{
		$this->setViewState('NavigateUrl',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string the text content displayed on this view. Defaults to ''.
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * Sets the text content to be displayed on this view.
	 * If this is not empty, the child content of the view will be ignored.
	 * @param string the text content displayed on this view
	 */
	public function setText($value)
	{
		$this->setViewState('Text',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return boolean whether this tab view is active. Defaults to false.
	 */
	public function getActive()
	{
		return $this->_active;
	}

	/**
	 * @param boolean whether this tab view is active.
	 */
	public function setActive($value)
	{
		$this->_active=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * Renders body contents of the tab view.
	 * @param THtmlWriter the writer used for the rendering purpose.
	 */
	public function renderContents($writer)
	{
		if(($text=$this->getText())!=='')
			$writer->write($text);
		else if($this->getHasControls())
			parent::renderContents($writer);
	}

	/**
	 * Renders the tab associated with the tab view.
	 * @param THtmlWriter the writer for rendering purpose.
	 */
	public function renderTab($writer)
	{
		if($this->getVisible(false) && $this->getPage()->getClientSupportsJavaScript())
		{
			$writer->addAttribute('id',$this->getClientID().'_0');

			$style=$this->getActive()?$this->getParent()->getActiveTabStyle():$this->getParent()->getTabStyle();
			$style->addAttributesToRender($writer);

			$writer->renderBeginTag($this->getTagName());

			$this->renderTabContent($writer);

			$writer->renderEndTag();
		}
	}

	/**
	 * Renders the content in the tab.
	 * By default, a hyperlink is displayed.
	 * @param THtmlWriter the HTML writer
	 */
	protected function renderTabContent($writer)
	{
		if(($url=$this->getNavigateUrl())==='')
			$url='javascript://';
		if(($caption=$this->getCaption())==='')
			$caption='&nbsp;';
		$writer->write("<a href=\"{$url}\">{$caption}</a>");
	}
}

/**
 * TTabViewCollection class.
 *
 * TTabViewCollection is used to maintain a list of views belong to a {@link TTabPanel}.
 *
 * @author Tomasz Wolny <tomasz.wolny@polecam.to.pl> and Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTabPanel.php 3013 2011-07-16 11:19:23Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TTabViewCollection extends TControlCollection
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing sanity check on the type of new item.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a {@link TTabView} object.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TTabView)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('tabviewcollection_tabview_required');
	}

	/**
	 * Finds the index of the tab view whose ID is the same as the one being looked for.
	 * @param string the explicit ID of the tab view to be looked for
	 * @return integer the index of the tab view found, -1 if not found.
	 */
	public function findIndexByID($id)
	{
		foreach($this as $index=>$view)
		{
			if($view->getID(false)===$id)
				return $index;
		}
		return -1;
	}
}

?>
