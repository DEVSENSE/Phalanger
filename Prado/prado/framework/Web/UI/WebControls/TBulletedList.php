<?php
/**
 * TBulletedList class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TBulletedList.php 2673 2009-06-07 07:12:35Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TListControl class
 */
Prado::using('System.Web.UI.WebControls.TListControl');

/**
 * TBulletedList class
 *
 * TBulletedList displays items in a bullet format.
 * The bullet style is specified by {@link setBulletStyle BulletStyle}. When
 * the style is 'CustomImage', the {@link setBackImageUrl BulletImageUrl}
 * specifies the image used as bullets.
 *
 * TBulletedList displays the item texts in three different modes, specified
 * via {@link setDisplayMode DisplayMode}. When the mode is Text, the item texts
 * are displayed as static texts; When the mode is 'HyperLink', each item
 * is displayed as a hyperlink whose URL is given by the item value, and the
 * {@link setTarget Target} property can be used to specify the target browser window;
 * When the mode is 'LinkButton', each item is displayed as a link button which
 * posts back to the page if a user clicks on that and the event {@link onClick OnClick}
 * will be raised under such a circumstance.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TBulletedList.php 2673 2009-06-07 07:12:35Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TBulletedList extends TListControl implements IPostBackEventHandler
{
	/**
	 * @var boolean cached property value of Enabled
	 */
	private $_isEnabled;
	/**
	 * @var TPostBackOptions postback options
	 */
	private $_postBackOptions;

	private $_currentRenderItemIndex;

	/**
	 * Raises the postback event.
	 * This method is required by {@link IPostBackEventHandler} interface.
	 * If {@link getCausesValidation CausesValidation} is true, it will
	 * invoke the page's {@link TPage::validate validate} method first.
	 * It will raise {@link onClick OnClick} events.
	 * This method is mainly used by framework and control developers.
	 * @param TEventParameter the event parameter
	 */
	public function raisePostBackEvent($param)
	{
		if($this->getCausesValidation())
			$this->getPage()->validate($this->getValidationGroup());
		$this->onClick(new TBulletedListEventParameter((int)$param));
	}

	/**
	 * @return string tag name of the bulleted list
	 */
	protected function getTagName()
	{
		switch($this->getBulletStyle())
		{
			case TBulletStyle::Numbered:
			case TBulletStyle::LowerAlpha:
			case TBulletStyle::UpperAlpha:
			case TBulletStyle::LowerRoman:
			case TBulletStyle::UpperRoman:
				return 'ol';
		}
		return 'ul';
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TBulletedList';
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * This overrides the parent implementation with additional bulleted list specific attributes.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		$needStart=false;
		switch($this->getBulletStyle())
		{
			case TBulletStyle::None:
				$writer->addStyleAttribute('list-style-type','none');
				$needStart=true;
				break;
			case TBulletStyle::Numbered:
				$writer->addStyleAttribute('list-style-type','decimal');
				$needStart=true;
				break;
			case TBulletStyle::LowerAlpha:
				$writer->addStyleAttribute('list-style-type','lower-alpha');
				$needStart=true;
				break;
			case TBulletStyle::UpperAlpha:
				$writer->addStyleAttribute('list-style-type','upper-alpha');
				$needStart=true;
				break;
			case TBulletStyle::LowerRoman:
				$writer->addStyleAttribute('list-style-type','lower-roman');
				$needStart=true;
				break;
			case TBulletStyle::UpperRoman:
				$writer->addStyleAttribute('list-style-type','upper-roman');
				$needStart=true;
				break;
			case TBulletStyle::Disc:
				$writer->addStyleAttribute('list-style-type','disc');
				break;
			case TBulletStyle::Circle:
				$writer->addStyleAttribute('list-style-type','circle');
				break;
			case TBulletStyle::Square:
				$writer->addStyleAttribute('list-style-type','square');
				break;
			case TBulletStyle::CustomImage:
				$url=$this->getBulletImageUrl();
				$writer->addStyleAttribute('list-style-image',"url($url)");
				break;
		}
		if($needStart && ($start=$this->getFirstBulletNumber())!=1)
			$writer->addAttribute('start',"$start");
		parent::addAttributesToRender($writer);
	}

	/**
	 * @return string image URL used for bullets when {@link getBulletStyle BulletStyle} is 'CustomImage'.
	 */
	public function getBulletImageUrl()
	{
		return $this->getViewState('BulletImageUrl','');
	}

	/**
	 * @param string image URL used for bullets when {@link getBulletStyle BulletStyle} is 'CustomImage'.
	 */
	public function setBulletImageUrl($value)
	{
		$this->setViewState('BulletImageUrl',$value,'');
	}

	/**
	 * @return TBulletStyle style of bullets. Defaults to TBulletStyle::NotSet.
	 */
	public function getBulletStyle()
	{
		return $this->getViewState('BulletStyle',TBulletStyle::NotSet);
	}

	/**
	 * @param TBulletStyle style of bullets.
	 */
	public function setBulletStyle($value)
	{
		$this->setViewState('BulletStyle',TPropertyValue::ensureEnum($value,'TBulletStyle'),TBulletStyle::NotSet);
	}

	/**
	 * @return TBulletedListDisplayMode display mode of the list. Defaults to TBulletedListDisplayMode::Text.
	 */
	public function getDisplayMode()
	{
		return $this->getViewState('DisplayMode',TBulletedListDisplayMode::Text);
	}

	/**
	 * @return TBulletedListDisplayMode display mode of the list.
	 */
	public function setDisplayMode($value)
	{
		$this->setViewState('DisplayMode',TPropertyValue::ensureEnum($value,'TBulletedListDisplayMode'),TBulletedListDisplayMode::Text);
	}

	/**
	 * @return integer starting index when {@link getBulletStyle BulletStyle} is one of
	 * the following: 'Numbered', 'LowerAlpha', 'UpperAlpha', 'LowerRoman', 'UpperRoman'.
	 * Defaults to 1.
	 */
	public function getFirstBulletNumber()
	{
		return $this->getViewState('FirstBulletNumber',1);
	}

	/**
	 * @param integer starting index when {@link getBulletStyle BulletStyle} is one of
	 * the following: 'Numbered', 'LowerAlpha', 'UpperAlpha', 'LowerRoman', 'UpperRoman'.
	 */
	public function setFirstBulletNumber($value)
	{
		$this->setViewState('FirstBulletNumber',TPropertyValue::ensureInteger($value),1);
	}

	/**
	 * Raises 'OnClick' event.
	 * This method is invoked when the {@link getDisplayMode DisplayMode} is 'LinkButton'
	 * and end-users click on one of the buttons.
	 * @param TBulletedListEventParameter event parameter.
	 */
	public function onClick($param)
	{
		$this->raiseEvent('OnClick',$this,$param);
	}

	/**
	 * @return string the target window or frame to display the Web page content
	 * linked to when {@link getDisplayMode DisplayMode} is 'HyperLink' and one of
	 * the hyperlinks is clicked.
	 */
	public function getTarget()
	{
		return $this->getViewState('Target','');
	}

	/**
	 * @param string the target window or frame to display the Web page content
	 * linked to when {@link getDisplayMode DisplayMode} is 'HyperLink' and one of
	 * the hyperlinks is clicked.
	 */
	public function setTarget($value)
	{
		$this->setViewState('Target',$value,'');
	}

	/**
	 * Renders the control.
	 * @param THtmlWriter the writer for the rendering purpose.
	 */
	public function render($writer)
	{
		if($this->getHasItems())
			parent::render($writer);
	}

	/**
	 * Renders the body contents.
	 * @param THtmlWriter the writer for the rendering purpose.
	 */
	public function renderContents($writer)
	{
		$this->_isEnabled=$this->getEnabled(true);
		$this->_postBackOptions=$this->getPostBackOptions();
		$writer->writeLine();
		foreach($this->getItems() as $index=>$item)
		{
			if($item->getHasAttributes())
				$writer->addAttributes($item->getAttributes());
			$writer->renderBeginTag('li');
			$this->renderBulletText($writer,$item,$index);
			$writer->renderEndTag();
			$writer->writeLine();
		}
	}

	/**
	 * Renders each item
	 * @param THtmlWriter writer for the rendering purpose
	 * @param TListItem item to be rendered
	 * @param integer index of the item being rendered
	 */
	protected function renderBulletText($writer,$item,$index)
	{
		switch($this->getDisplayMode())
		{
			case TBulletedListDisplayMode::Text:
				$this->renderTextItem($writer, $item, $index);
				break;
			case TBulletedListDisplayMode::HyperLink:
				$this->renderHyperLinkItem($writer, $item, $index);
				break;
			case TBulletedListDisplayMode::LinkButton:
				$this->renderLinkButtonItem($writer, $item, $index);
				break;
		}
	}

	protected function renderTextItem($writer, $item, $index)
	{
		if($item->getEnabled())
			$writer->write(THttpUtility::htmlEncode($item->getText()));
		else
		{
			$writer->addAttribute('disabled','disabled');
			$writer->renderBeginTag('span');
			$writer->write(THttpUtility::htmlEncode($item->getText()));
			$writer->renderEndTag();
		}
	}

	protected function renderHyperLinkItem($writer, $item, $index)
	{
		if(!$this->_isEnabled || !$item->getEnabled())
			$writer->addAttribute('disabled','disabled');
		else
		{
			$writer->addAttribute('href',$item->getValue());
			if(($target=$this->getTarget())!=='')
				$writer->addAttribute('target',$target);
		}
		if(($accesskey=$this->getAccessKey())!=='')
			$writer->addAttribute('accesskey',$accesskey);
		$writer->renderBeginTag('a');
		$writer->write(THttpUtility::htmlEncode($item->getText()));
		$writer->renderEndTag();
	}

	protected function renderLinkButtonItem($writer, $item, $index)
	{
		if(!$this->_isEnabled || !$item->getEnabled())
			$writer->addAttribute('disabled','disabled');
		else
		{
			$this->_currentRenderItemIndex = $index;
			$writer->addAttribute('id', $this->getClientID().$index);
			$writer->addAttribute('href', "javascript:;//".$this->getClientID().$index);
			$cs = $this->getPage()->getClientScript();
			$cs->registerPostBackControl($this->getClientClassName(),$this->getPostBackOptions());
		}
		if(($accesskey=$this->getAccessKey())!=='')
			$writer->addAttribute('accesskey',$accesskey);
		$writer->renderBeginTag('a');
		$writer->write(THttpUtility::htmlEncode($item->getText()));
		$writer->renderEndTag();
	}

	/**
	 * @return array postback options used for linkbuttons.
	 */
	protected function getPostBackOptions()
	{
		$options['ValidationGroup'] = $this->getValidationGroup();
		$options['CausesValidation'] = $this->getCausesValidation();
		$options['EventTarget'] = $this->getUniqueID();
		$options['EventParameter'] = $this->_currentRenderItemIndex;
		$options['ID'] = $this->getClientID().$this->_currentRenderItemIndex;
		$options['StopEvent'] = true;
		return $options;
	}

	protected function canCauseValidation()
	{
		$group = $this->getValidationGroup();
		$hasValidators = $this->getPage()->getValidators($group)->getCount()>0;
		return $this->getCausesValidation() && $hasValidators;
	}

	/**
	 * @throws TNotSupportedException if this method is invoked
	 */
	public function setAutoPostBack($value)
	{
		throw new TNotSupportedException('bulletedlist_autopostback_unsupported');
	}

	/**
	 * @throws TNotSupportedException if this method is invoked
	 */
	public function setSelectedIndex($index)
	{
		throw new TNotSupportedException('bulletedlist_selectedindex_unsupported');
	}

	/**
	 * @throws TNotSupportedException if this method is invoked
	 */
	public function setSelectedIndices($indices)
	{
		throw new TNotSupportedException('bulletedlist_selectedindices_unsupported');
	}

	/**
	 * @throws TNotSupportedException if this method is invoked
	 */
	public function setSelectedValue($value)
	{
		throw new TNotSupportedException('bulletedlist_selectedvalue_unsupported');
	}

	/**
	 * @throws TNotSupportedException if this method is invoked
	 */
	public function setSelectedValues($values)
	{
		throw new TNotSupportedException('bulletedlist_selectedvalue_unsupported');
	}
}

/**
 * TBulletedListEventParameter
 * Event parameter for {@link TBulletedList::onClick Click} event of the
 * bulleted list. The {@link getIndex Index} gives the zero-based index
 * of the item that is currently being clicked.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TBulletedList.php 2673 2009-06-07 07:12:35Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TBulletedListEventParameter extends TEventParameter
{
	/**
	 * @var integer index of the item clicked
	 */
	private $_index;

	/**
	 * Constructor.
	 * @param integer index of the item clicked
	 */
	public function __construct($index)
	{
		$this->_index=$index;
	}

	/**
	 * @return integer zero-based index of the item (rendered as a link button) that is clicked
	 */
	public function getIndex()
	{
		return $this->_index;
	}
}

/**
 * TBulletStyle class.
 * TBulletStyle defines the enumerable type for the possible bullet styles that may be used
 * for a {@link TBulletedList} control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TBulletedList.php 2673 2009-06-07 07:12:35Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TBulletStyle extends TEnumerable
{
	const NotSet='NotSet';
	const None='None';
	const Numbered='Numbered';
	const LowerAlpha='LowerAlpha';
	const UpperAlpha='UpperAlpha';
	const LowerRoman='LowerRoman';
	const UpperRoman='UpperRoman';
	const Disc='Disc';
	const Circle='Circle';
	const Square='Square';
	const CustomImage='CustomImage';
}

/**
 * TBulletedListDisplayMode class.
 * TBulletedListDisplayMode defines the enumerable type for the possible display mode
 * of a {@link TBulletedList} control.
 *
 * The following enumerable values are defined:
 * - Text: the bulleted list items are displayed as plain texts
 * - HyperLink: the bulleted list items are displayed as hyperlinks
 * - LinkButton: the bulleted list items are displayed as link buttons that can cause postbacks
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TBulletedList.php 2673 2009-06-07 07:12:35Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TBulletedListDisplayMode extends TEnumerable
{
	const Text='Text';
	const HyperLink='HyperLink';
	const LinkButton='LinkButton';
}

