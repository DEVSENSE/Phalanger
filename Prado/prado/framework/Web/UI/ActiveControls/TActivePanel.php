<?php
/**
 * TActivePanel file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActivePanel.php 2954 2011-06-01 23:01:03Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active control adapter.
 */
Prado::using('System.Web.UI.ActiveControls.TActiveControlAdapter');

/**
 * TActivePanel is the TPanel active control counterpart.
 *
 * TActivePanel allows the client-side panel contents to be updated during a
 * callback response using the {@link render} method.
 *
 * Example: Assume $param is an instance of TCallbackEventParameter attached to
 * the OnCallback event of a TCallback with ID "callback1", and
 * "panel1" is the ID of a TActivePanel.
 * <code>
 * function callback1_requested($sender, $param)
 * {
 * 	   $this->panel1->render($param->getNewWriter());
 * }
 * </code>
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActivePanel.php 2954 2011-06-01 23:01:03Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActivePanel extends TPanel implements IActiveControl
{
	/**
	 * Creates a new callback control, sets the adapter to
	 * TActiveControlAdapter. If you override this class, be sure to set the
	 * adapter appropriately by, for example, by calling this constructor.
	 */
	public function __construct()
	{
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
	}

	/**
	 * @return TBaseActiveControl standard active control options.
	 */
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * Adds attribute id to the renderer.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer) {
	    $writer->addAttribute('id',$this->getClientID());
	    parent::addAttributesToRender($writer);
	}

	/**
	 * Renders and replaces the panel's content on the client-side.
	 * When render() is called before the OnPreRender event, such as when render()
	 * is called during a callback event handler, the rendering
	 * is defered until OnPreRender event is raised.
	 * @param THtmlWriter html writer
	 */
	public function render($writer)
	{
		if($this->getHasPreRendered())
		{
			parent::render($writer);
			if($this->getActiveControl()->canUpdateClientSide())
				$this->getPage()->getCallbackClient()->replaceContent($this,$writer);
		}
		else
		{
			$this->getPage()->getAdapter()->registerControlToRender($this,$writer);
			if ($this->getHasControls())
			{
				// If we update a TActivePanel on callback,
				// We shouldn't update all childs, because the whole content will be replaced by 
				// the parent
				foreach ($this->findControlsByType('IActiveControl', false) as $control)
				{
						$control->getActiveControl()->setEnableUpdate(false);
				}
			}
		}
	}
}

