<?php
/**
 * TInlineFrame class file.
 *
 * @author Jason Ragsdale <jrags@jasrags.net>
 * @author Harry Pottash <hpottash@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TInlineFrame.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TInlineFrame class
 *
 * TInlineFrame displays an inline frame (iframe) on a Web page.
 * The location of the frame content is specified by {@link setFrameUrl FrameUrl}.
 * The frame's alignment is specified by {@link setAlign Align}.
 * The {@link setMarginWidth MarginWidth} and {@link setMarginHeight MarginHeight}
 * properties define the number of pixels to use as the left/right margins and
 * top/bottom margins, respectively, within the inline frame.
 * The {@link setScrollBars ScrollBars} property specifies whether scrollbars are
 * provided for the inline frame. And {@link setDescriptionUrl DescriptionUrl}
 * gives the URI of a long description of the frame's contents.
 *
 * Original Prado v2 IFrame Author Information
 * @author Jason Ragsdale <jrags@jasrags.net>
 * @author Harry Pottash <hpottash@gmail.com>
 * @version $Id: TInlineFrame.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TInlineFrame extends TWebControl implements IDataRenderer
{
	/**
	 * @return string tag name of the iframe.
	 */
	protected function getTagName()
	{
		return 'iframe';
	}

	/**
	 * @return TInlineFrameAlign alignment of the iframe. Defaults to TInlineFrameAlign::NotSet.
	 */
	public function getAlign()
	{
		return $this->getViewState('Align',TInlineFrameAlign::NotSet);
	}

	/**
	 * @param TInlineFrameAlign alignment of the iframe.
	 */
	public function setAlign($value)
	{
		$this->setViewState('Align',TPropertyValue::ensureEnum($value,'TInlineFrameAlign'),TInlineFrameAlign::NotSet);
	}

	/**
	 * @return string the URL to long description
	 */
	public function getDescriptionUrl()
	{
		return $this->getViewState('DescriptionUrl','');
	}

	/**
	 * @param string the URL to the long description of the image.
	 */
	public function setDescriptionUrl($value)
	{
		$this->setViewState('DescriptionUrl',$value,'');
	}

	/**
	 * @return boolean whether there should be a visual separator between the frames. Defaults to true.
	 */
	public function getShowBorder()
	{
		return $this->getViewState('ShowBorder',true);
	}

	/**
	 * @param boolean whether there should be a visual separator between the frames.
	 */
	public function setShowBorder($value)
	{
		$this->setViewState('ShowBorder',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return string URL that this iframe will load content from. Defaults to ''.
	 */
	public function getFrameUrl()
	{
		return $this->getViewState('FrameUrl','');
	}

	/**
	 * @param string URL that this iframe will load content from.
	 */
	public function setFrameUrl($value)
	{
		$this->setViewState('FrameUrl',$value,'');
	}

	/**
	 * Returns the URL that this iframe will load content from
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getFrameUrl()}.
	 * @return string the URL that this iframe will load content from
	 * @see getFrameUrl
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->getFrameUrl();
	}

	/**
	 * Sets the URL that this iframe will load content from.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setFrameUrl()}.
	 * @param string the URL that this iframe will load content from
	 * @see setFrameUrl
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->setFrameUrl($value);
	}

	/**
	 * @return TInlineFrameScrollBars the visibility and position of scroll bars in an iframe. Defaults to TInlineFrameScrollBars::Auto.
	 */
	public function getScrollBars()
	{
		return $this->getViewState('ScrollBars',TInlineFrameScrollBars::Auto);
	}

	/**
	 * @param TInlineFrameScrollBars the visibility and position of scroll bars in an iframe.
	 */
	public function setScrollBars($value)
	{
		$this->setViewState('ScrollBars',TPropertyValue::ensureEnum($value,'TInlineFrameScrollBars'),TInlineFrameScrollBars::Auto);
	}

	/**
	 * @return integer the amount of space, in pixels, that should be left between
	 * the frame's contents and the left and right margins. Defaults to -1, meaning not set.
	 */
	public function getMarginWidth()
	{
		return $this->getViewState('MarginWidth',-1);
	}

	/**
	 * @param integer the amount of space, in pixels, that should be left between
	 * the frame's contents and the left and right margins.
	 */
	public function setMarginWidth($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=-1;
		$this->setViewState('MarginWidth',$value,-1);
	}

	/**
	 * @return integer the amount of space, in pixels, that should be left between
	 * the frame's contents and the top and bottom margins. Defaults to -1, meaning not set.
	 */
	public function getMarginHeight()
	{
		return $this->getViewState('MarginHeight',-1);
	}

	/**
	 * @param integer the amount of space, in pixels, that should be left between
	 * the frame's contents and the top and bottom margins.
	 */
	public function setMarginHeight($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=-1;
		$this->setViewState('MarginHeight',$value,-1);
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * This overrides the parent implementation with additional button specific attributes.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		if($this->getID()!=='')
			$writer->addAttribute('name',$this->getUniqueID());

		if(($src=$this->getFrameUrl())!=='')
			$writer->addAttribute('src',$src);

		if(($align=strtolower($this->getAlign()))!=='notset')
			$writer->addAttribute('align',$align);

		$scrollBars=$this->getScrollBars();
		if($scrollBars===TInlineFrameScrollBars::None)
			$writer->addAttribute('scrolling','no');
		else if($scrollBars===TInlineFrameScrollBars::Both)
			$writer->addAttribute('scrolling','yes');

		if (!$this->getShowBorder())
			$writer->addAttribute('frameborder','0');

		if(($longdesc=$this->getDescriptionUrl())!=='')
			$writer->addAttribute('longdesc',$longdesc);

		if(($marginheight=$this->getMarginHeight())!==-1)
			$writer->addAttribute('marginheight',$marginheight);

		if(($marginwidth=$this->getMarginWidth())!==-1)
			$writer->addAttribute('marginwidth',$marginwidth);

		parent::addAttributesToRender($writer);
	}
}

/**
 * TInlineFrameAlign class.
 * TInlineFrameAlign defines the enumerable type for the possible alignments
 * that the content in a {@link TInlineFrame} could be.
 *
 * The following enumerable values are defined:
 * - NotSet: the alignment is not specified.
 * - Left: left aligned
 * - Right: right aligned
 * - Top: top aligned
 * - Middle: middle aligned
 * - Bottom: bottom aligned
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TInlineFrame.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TInlineFrameAlign extends TEnumerable
{
	const NotSet='NotSet';
	const Left='Left';
	const Right='Right';
	const Top='Top';
	const Middle='Middle';
	const Bottom='Bottom';
}

/**
 * TInlineFrameScrollBars class.
 * TInlineFrameScrollBars defines the enumerable type for the possible scroll bar mode
 * that a {@link TInlineFrame} control could use.
 *
 * The following enumerable values are defined:
 * - None: no scroll bars.
 * - Auto: scroll bars automatically appeared when needed.
 * - Both: show both horizontal and vertical scroll bars all the time.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TInlineFrame.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TInlineFrameScrollBars extends TEnumerable
{
	const None='None';
	const Auto='Auto';
	const Both='Both';
}
