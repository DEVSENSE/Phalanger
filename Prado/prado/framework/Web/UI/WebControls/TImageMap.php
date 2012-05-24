<?php
/**
 * TImageMap and related class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TImage class file
 */
Prado::using('System.Web.UI.WebControls.TImage');

/**
 * TImageMap class
 *
 * TImageMap represents an image on a page. Hotspot regions can be defined
 * within the image. Depending on the {@link setHotSpotMode HotSpotMode},
 * clicking on the hotspots may trigger a postback or navigate to a specified
 * URL. The hotspots defined may be accessed via {@link getHotSpots HotSpots}.
 * Each hotspot is described as a {@link THotSpot}, which can be a circle,
 * rectangle, polygon, etc. To add hotspot in a template, use the following,
 * <code>
 *  <com:TImageMap>
 *    <com:TCircleHotSpot ... />
 *    <com:TRectangleHotSpot ... />
 *    <com:TPolygonHotSpot ... />
 *  </com:TImageMap>
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TImageMap extends TImage implements IPostBackEventHandler
{
	const MAP_NAME_PREFIX='ImageMap';

	/**
	 * Processes an object that is created during parsing template.
	 * This method adds {@link THotSpot} objects into the hotspot collection
	 * of the imagemap.
	 * @param string|TComponent text string or component parsed and instantiated in template
	 */
	public function addParsedObject($object)
	{
		if($object instanceof THotSpot)
			$this->getHotSpots()->add($object);
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * This overrides the parent implementation with additional imagemap specific attributes.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		if($this->getHotSpots()->getCount()>0)
		{
			$writer->addAttribute('usemap','#'.self::MAP_NAME_PREFIX.$this->getClientID());
			$writer->addAttribute('id',$this->getUniqueID());
		}
		if($this->getEnabled() && !$this->getEnabled(true))
			$writer->addAttribute('disabled','disabled');
	}

	/**
	 * Renders this imagemap.
	 * @param THtmlWriter
	 */
	public function render($writer)
	{
		parent::render($writer);

		$hotspots=$this->getHotSpots();

		if($hotspots->getCount()>0)
		{
			$clientID=$this->getClientID();
			$cs=$this->getPage()->getClientScript();
			$writer->writeLine();
			$writer->addAttribute('name',self::MAP_NAME_PREFIX.$clientID);
			$writer->renderBeginTag('map');
			$writer->writeLine();
			if(($mode=$this->getHotSpotMode())===THotSpotMode::NotSet)
				$mode=THotSpotMode::Navigate;
			$target=$this->getTarget();
			$i=0;
			$options['EventTarget'] = $this->getUniqueID();
			$options['StopEvent'] = true;
			$cs=$this->getPage()->getClientScript();
			foreach($hotspots as $hotspot)
			{
				if($hotspot->getHotSpotMode()===THotSpotMode::NotSet)
					$hotspot->setHotSpotMode($mode);
				if($target!=='' && $hotspot->getTarget()==='')
					$hotspot->setTarget($target);
				if($hotspot->getHotSpotMode()===THotSpotMode::PostBack)
				{
					$id=$clientID.'_'.$i;
					$writer->addAttribute('id',$id);
					$writer->addAttribute('href','#'.$id); //create unique no-op url references
					$options['ID']=$id;
					$options['EventParameter']="$i";
					$options['CausesValidation']=$hotspot->getCausesValidation();
					$options['ValidationGroup']=$hotspot->getValidationGroup();
					$cs->registerPostBackControl($this->getClientClassName(),$options);
				}
				$hotspot->render($writer);
				$writer->writeLine();
				$i++;
			}
			$writer->renderEndTag();
		}
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TImageMap';
	}

	/**
	 * Raises the postback event.
	 * This method is required by {@link IPostBackEventHandler} interface.
	 * This method is mainly used by framework and control developers.
	 * @param TEventParameter the event parameter
	 */
	public function raisePostBackEvent($param)
	{
		$postBackValue=null;
		if($param!=='')
		{
			$index=TPropertyValue::ensureInteger($param);
			$hotspots=$this->getHotSpots();
			if($index>=0 && $index<$hotspots->getCount())
			{
				$hotspot=$hotspots->itemAt($index);
				if(($mode=$hotspot->getHotSpotMode())===THotSpotMode::NotSet)
					$mode=$this->getHotSpotMode();
				if($mode===THotSpotMode::PostBack)
				{
					$postBackValue=$hotspot->getPostBackValue();
					if($hotspot->getCausesValidation())
						$this->getPage()->validate($hotspot->getValidationGroup());
				}
			}
		}
		if($postBackValue!==null)
			$this->onClick(new TImageMapEventParameter($postBackValue));
	}

	/**
	 * @return THotSpotMode the behavior of hotspot regions in this imagemap when they are clicked. Defaults to THotSpotMode::NotSet.
	 */
	public function getHotSpotMode()
	{
		return $this->getViewState('HotSpotMode',THotSpotMode::NotSet);
	}

	/**
	 * Sets the behavior of hotspot regions in this imagemap when they are clicked.
	 * If an individual hotspot has a mode other than 'NotSet', the mode set in this
	 * imagemap will be ignored. By default, 'NotSet' is equivalent to 'Navigate'.
	 * @param THotSpotMode the behavior of hotspot regions in this imagemap when they are clicked.
	 */
	public function setHotSpotMode($value)
	{
		$this->setViewState('HotSpotMode',TPropertyValue::ensureEnum($value,'THotSpotMode'),THotSpotMode::NotSet);
	}

	/**
	 * @return THotSpotCollection collection of hotspots defined in this imagemap.
	 */
	public function getHotSpots()
	{
		if(($hotspots=$this->getViewState('HotSpots',null))===null)
		{
			$hotspots=new THotSpotCollection;
			$this->setViewState('HotSpots',$hotspots);
		}
		return $hotspots;
	}

	/**
	 * @return string  the target window or frame to display the new page when a hotspot region is clicked within the imagemap. Defaults to ''.
	 */
	public function getTarget()
	{
		return $this->getViewState('Target','');
	}

	/**
	 * @param string  the target window or frame to display the new page when a hotspot region is clicked within the imagemap.
	 */
	public function setTarget($value)
	{
		$this->setViewState('Target',TPropertyValue::ensureString($value),'');
	}

	/**
	 * Raises <b>OnClick</b> event.
	 * This method is invoked when a hotspot region is clicked within the imagemap.
	 * If you override this method, be sure to call the parent implementation
	 * so that the event handler can be invoked.
	 * @param TImageMapEventParameter event parameter to be passed to the event handlers
	 */
	public function onClick($param)
	{
		$this->raiseEvent('OnClick',$this,$param);
	}
}

/**
 * TImageMapEventParameter class.
 *
 * TImageMapEventParameter represents a postback event parameter
 * when a hotspot is clicked and posts back in a {@link TImageMap}.
 * To retrieve the post back value associated with the hotspot being clicked,
 * access {@link getPostBackValue PostBackValue}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TImageMapEventParameter extends TEventParameter
{
	private $_postBackValue;

	/**
	 * Constructor.
	 * @param string post back value associated with the hotspot clicked
	 */
	public function __construct($postBackValue)
	{
		$this->_postBackValue=$postBackValue;
	}

	/**
	 * @return string post back value associated with the hotspot clicked
	 */
	public function getPostBackValue()
	{
		return $this->_postBackValue;
	}
}

/**
 * THotSpotCollection class.
 *
 * THotSpotCollection represents a collection of hotspots in an imagemap.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class THotSpotCollection extends TList
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by inserting only {@link THotSpot}.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a THotSpot.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof THotSpot)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('hotspotcollection_hotspot_required');
	}
}


/**
 * THotSpot class.
 *
 * THotSpot implements the basic functionality common to all hot spot shapes.
 * Derived classes include {@link TCircleHotSpot}, {@link TPolygonHotSpot}
 * and {@link TRectangleHotSpot}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
abstract class THotSpot extends TComponent
{
	private $_viewState=array();

	/**
	 * Returns a viewstate value.
	 *
	 * This function is very useful in defining getter functions for component properties
	 * that must be kept in viewstate.
	 * @param string the name of the viewstate value to be returned
	 * @param mixed the default value. If $key is not found in viewstate, $defaultValue will be returned
	 * @return mixed the viewstate value corresponding to $key
	 */
	protected function getViewState($key,$defaultValue=null)
	{
		return isset($this->_viewState[$key])?$this->_viewState[$key]:$defaultValue;
	}

	/**
	 * Sets a viewstate value.
	 *
	 * This function is very useful in defining setter functions for control properties
	 * that must be kept in viewstate.
	 * Make sure that the viewstate value must be serializable and unserializable.
	 * @param string the name of the viewstate value
	 * @param mixed the viewstate value to be set
	 * @param mixed default value. If $value===$defaultValue, the item will be cleared from the viewstate.
	 */
	protected function setViewState($key,$value,$defaultValue=null)
	{
		if($value===$defaultValue)
			unset($this->_viewState[$key]);
		else
			$this->_viewState[$key]=$value;
	}

	/**
	 * @return string shape of the hotspot, can be 'circle', 'rect', 'poly', etc.
	 */
	abstract public function getShape();
	/**
	 * @return string coordinates defining the hotspot shape.
	 */
	abstract public function getCoordinates();

	/**
	 * @return string the access key that allows you to quickly navigate to the HotSpot region. Defaults to ''.
	 */
	public function getAccessKey()
	{
		return $this->getViewState('AccessKey','');
	}

	/**
	 * @param string the access key that allows you to quickly navigate to the HotSpot region.
	 */
	public function setAccessKey($value)
	{
		$this->setViewState('AccessKey',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string the alternate text to display for a HotSpot object. Defaults to ''.
	 */
	public function getAlternateText()
	{
		return $this->getViewState('AlternateText','');
	}

	/**
	 * @param string the alternate text to display for a HotSpot object.
	 */
	public function setAlternateText($value)
	{
		$this->setViewState('AlternateText',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return THotSpotMode the behavior of a HotSpot object when it is clicked. Defaults to THotSpotMode::NotSet.
	 */
	public function getHotSpotMode()
	{
		return $this->getViewState('HotSpotMode',THotSpotMode::NotSet);
	}

	/**
	 * @param THotSpotMode the behavior of a HotSpot object when it is clicked.
	 */
	public function setHotSpotMode($value)
	{
		$this->setViewState('HotSpotMode',TPropertyValue::ensureEnum($value,'THotSpotMode'),THotSpotMode::NotSet);
	}

	/**
	 * @return string the URL to navigate to when a HotSpot object is clicked. Defaults to ''.
	 */
	public function getNavigateUrl()
	{
		return $this->getViewState('NavigateUrl','');
	}

	/**
	 * @param string the URL to navigate to when a HotSpot object is clicked.
	 */
	public function setNavigateUrl($value)
	{
		$this->setViewState('NavigateUrl',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string a value that is post back when the HotSpot is clicked. Defaults to ''.
	 */
	public function getPostBackValue()
	{
		return $this->getViewState('PostBackValue','');
	}

	/**
	 * @param string a value that is post back when the HotSpot is clicked.
	 */
	public function setPostBackValue($value)
	{
		$this->setViewState('PostBackValue',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return integer the tab index of the HotSpot region. Defaults to 0.
	 */
	public function getTabIndex()
	{
		return $this->getViewState('TabIndex',0);
	}

	/**
	 * @param integer the tab index of the HotSpot region.
	 */
	public function setTabIndex($value)
	{
		$this->setViewState('TabIndex',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * @return boolean whether postback event trigger by this hotspot will cause input validation, default is true
	 */
	public function getCausesValidation()
	{
		return $this->getViewState('CausesValidation',true);
	}

	/**
	 * @param boolean whether postback event trigger by this hotspot will cause input validation
	 */
	public function setCausesValidation($value)
	{
		$this->setViewState('CausesValidation',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return string the group of validators which the hotspot causes validation upon postback
	 */
	public function getValidationGroup()
	{
		return $this->getViewState('ValidationGroup','');
	}

	/**
	 * @param string the group of validators which the hotspot causes validation upon postback
	 */
	public function setValidationGroup($value)
	{
		$this->setViewState('ValidationGroup',$value,'');
	}

	/**
	 * @return string  the target window or frame to display the new page when the HotSpot region
	 * is clicked. Defaults to ''.
	 */
	public function getTarget()
	{
		return $this->getViewState('Target','');
	}

	/**
	 * @param string  the target window or frame to display the new page when the HotSpot region
	 * is clicked.
	 */
	public function setTarget($value)
	{
		$this->setViewState('Target',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return boolean whether the hotspot has custom attributes
	 */
	public function getHasAttributes()
	{
		if($attributes=$this->getViewState('Attributes',null))
			return $attributes->getCount()>0;
		else
			return false;
	}

	/**
	 * Returns the list of custom attributes.
	 * Custom attributes are name-value pairs that may be rendered
	 * as HTML tags' attributes.
	 * @return TAttributeCollection the list of custom attributes
	 */
	public function getAttributes()
	{
		if($attributes=$this->getViewState('Attributes',null))
			return $attributes;
		else
		{
			$attributes=new TAttributeCollection;
			$this->setViewState('Attributes',$attributes,null);
			return $attributes;
		}
	}

	/**
	 * @return boolean whether the named attribute exists
	 */
	public function hasAttribute($name)
	{
		if($attributes=$this->getViewState('Attributes',null))
			return $attributes->contains($name);
		else
			return false;
	}

	/**
	 * @return string attribute value, null if attribute does not exist
	 */
	public function getAttribute($name)
	{
		if($attributes=$this->getViewState('Attributes',null))
			return $attributes->itemAt($name);
		else
			return null;
	}

	/**
	 * Sets a custom hotspot attribute.
	 * @param string attribute name
	 * @param string value of the attribute
	 */
	public function setAttribute($name,$value)
	{
		$this->getAttributes()->add($name,$value);
	}

	/**
	 * Removes the named attribute.
	 * @param string the name of the attribute to be removed.
	 * @return string attribute value removed, null if attribute does not exist.
	 */
	public function removeAttribute($name)
	{
		if($attributes=$this->getViewState('Attributes',null))
			return $attributes->remove($name);
		else
			return null;
	}

	/**
	 * Renders this hotspot.
	 * @param THtmlWriter
	 */
	public function render($writer)
	{
		$writer->addAttribute('shape',$this->getShape());
		$writer->addAttribute('coords',$this->getCoordinates());
		if(($mode=$this->getHotSpotMode())===THotSpotMode::NotSet)
			$mode=THotSpotMode::Navigate;
		if($mode===THotSpotMode::Navigate)
		{
			$writer->addAttribute('href',$this->getNavigateUrl());
			if(($target=$this->getTarget())!=='')
				$writer->addAttribute('target',$target);
		}
		else if($mode===THotSpotMode::Inactive)
			$writer->addAttribute('nohref','true');
		$text=$this->getAlternateText();
		$writer->addAttribute('title',$text);
		$writer->addAttribute('alt',$text);
		if(($accessKey=$this->getAccessKey())!=='')
			$writer->addAttribute('accesskey',$accessKey);
		if(($tabIndex=$this->getTabIndex())!==0)
			$writer->addAttribute('tabindex',"$tabIndex");
		if($this->getHasAttributes())
		{
			foreach($this->getAttributes() as $name=>$value)
				$writer->addAttribute($name,$value);
		}
		$writer->renderBeginTag('area');
		$writer->renderEndTag();
	}
}

/**
 * Class TCircleHotSpot.
 *
 * TCircleHotSpot defines a circular hot spot region in a {@link TImageMap}
 * control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TCircleHotSpot extends THotSpot
{
	/**
	 * @return string shape of this hotspot.
	 */
	public function getShape()
	{
		return 'circle';
	}

	/**
	 * @return string coordinates defining this hotspot shape
	 */
	public function getCoordinates()
	{
		return $this->getX().','.$this->getY().','.$this->getRadius();
	}

	/**
	 * @return integer radius of the circular HotSpot region. Defaults to 0.
	 */
	public function getRadius()
	{
		return $this->getViewState('Radius',0);
	}

	/**
	 * @param integer radius of the circular HotSpot region.
	 */
	public function setRadius($value)
	{
		$this->setViewState('Radius',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * @return integer the X coordinate of the center of the circular HotSpot region. Defaults to 0.
	 */
	public function getX()
	{
		return $this->getViewState('X',0);
	}

	/**
	 * @param integer the X coordinate of the center of the circular HotSpot region.
	 */
	public function setX($value)
	{
		$this->setViewState('X',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * @return integer the Y coordinate of the center of the circular HotSpot region. Defaults to 0.
	 */
	public function getY()
	{
		return $this->getViewState('Y',0);
	}

	/**
	 * @param integer the Y coordinate of the center of the circular HotSpot region.
	 */
	public function setY($value)
	{
		$this->setViewState('Y',TPropertyValue::ensureInteger($value),0);
	}
}

/**
 * Class TRectangleHotSpot.
 *
 * TRectangleHotSpot defines a rectangle hot spot region in a {@link
 * TImageMap} control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRectangleHotSpot extends THotSpot
{
	/**
	 * @return string shape of this hotspot.
	 */
	public function getShape()
	{
		return 'rect';
	}

	/**
	 * @return string coordinates defining this hotspot shape
	 */
	public function getCoordinates()
	{
		return $this->getLeft().','.$this->getTop().','.$this->getRight().','.$this->getBottom();
	}

	/**
	 * @return integer the Y coordinate of the bottom side of the rectangle HotSpot region. Defaults to 0.
	 */
	public function getBottom()
	{
		return $this->getViewState('Bottom',0);
	}

	/**
	 * @param integer the Y coordinate of the bottom side of the rectangle HotSpot region.
	 */
	public function setBottom($value)
	{
		$this->setViewState('Bottom',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * @return integer the X coordinate of the right side of the rectangle HotSpot region. Defaults to 0.
	 */
	public function getLeft()
	{
		return $this->getViewState('Left',0);
	}

	/**
	 * @param integer the X coordinate of the right side of the rectangle HotSpot region.
	 */
	public function setLeft($value)
	{
		$this->setViewState('Left',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * @return integer the X coordinate of the right side of the rectangle HotSpot region. Defaults to 0.
	 */
	public function getRight()
	{
		return $this->getViewState('Right',0);
	}

	/**
	 * @param integer the X coordinate of the right side of the rectangle HotSpot region.
	 */
	public function setRight($value)
	{
		$this->setViewState('Right',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * @return integer the Y coordinate of the top side of the rectangle HotSpot region. Defaults to 0.
	 */
	public function getTop()
	{
		return $this->getViewState('Top',0);
	}

	/**
	 * @param integer the Y coordinate of the top side of the rectangle HotSpot region.
	 */
	public function setTop($value)
	{
		$this->setViewState('Top',TPropertyValue::ensureInteger($value),0);
	}
}

/**
 * Class TPolygonHotSpot.
 *
 * TPolygonHotSpot defines a polygon hot spot region in a {@link
 * TImageMap} control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TPolygonHotSpot extends THotSpot
{
	/**
	 * @return string shape of this hotspot.
	 */
	public function getShape()
	{
		return 'poly';
	}

	/**
	 * @return string coordinates of the vertices defining the polygon.
	 * Coordinates are concatenated together with comma ','. Each pair
	 * represents (x,y) of a vertex.
	 */
	public function getCoordinates()
	{
		return $this->getViewState('Coordinates','');
	}

	/**
	 * @param string coordinates of the vertices defining the polygon.
	 * Coordinates are concatenated together with comma ','. Each pair
	 * represents (x,y) of a vertex.
	 */
	public function setCoordinates($value)
	{
		$this->setViewState('Coordinates',$value,'');
	}
}


/**
 * THotSpotMode class.
 * THotSpotMode defines the enumerable type for the possible hot spot modes.
 *
 * The following enumerable values are defined:
 * - NotSet: the mode is not specified
 * - Navigate: clicking on the hotspot will redirect the browser to a different page
 * - PostBack: clicking on the hotspot will cause a postback
 * - Inactive: the hotspot is inactive (not clickable)
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageMap.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class THotSpotMode extends TEnumerable
{
	const NotSet='NotSet';
	const Navigate='Navigate';
	const PostBack='PostBack';
	const Inactive='Inactive';
}

