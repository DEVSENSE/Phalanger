<?php
/**
 * TActiveDataList class file
 * 
 * @author Marcos Aurelio Nobre <marconobre@gmail.com>
 * @copyright Copyright &copy; 2008, PradoSoft
 * @license http://www.pradosoft.com/license
 * @package System.Web.UI.ActiveControls
 * @version $Id: TActiveDataList.php 2706 2009-09-24 14:42:30Z rojaro $
 */

/**
 * TActiveDataList class
 *
 * TActiveDataList represents a data bound and updatable grid control which is the
 * active counterpart to the original {@link TDataList} control.
 * 
 * This component can be used in the same way as the regular datalist, the only
 * difference is that the active datalist uses callbacks instead of postbacks
 * for interaction.
 *
 * Please refer to the original documentation of the regular counterparts for usage.
 *
 * @author Marcos Aurelio Nobre <marconobre@gmail.com>
 * @package System.Web.UI.ActiveControls
 */
class TActiveDataList extends TDataList implements IActiveControl {
  
  /**
   * Creates a new callback control, sets the adapter to
   * TActiveControlAdapter. 
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
   * Sets the data source object associated with the repeater control.
   * In addition, the render method of all connected pagers is called so they
   * get updated when the data source is changed. Also the repeater registers
   * itself for rendering in order to get it's content replaced on client side.
   * @param Traversable|array|string data source object
   */
  public function setDataSource($value)
  {
    parent::setDataSource($value);
    if($this->getActiveControl()->canUpdateClientSide()) {
      $this->renderPager();
      $this->getPage()->getAdapter()->registerControlToRender($this,$this->getResponse()->createHtmlWriter());
    }
  }
  
  /**
   * Returns the id of the surrounding container (span).
   * @return string container id
   */
  protected function getContainerID()
  {
    return $this->ClientID.'_Container';
  }
  
  /**
   * Renders the repeater.
   * If the repeater did not pass the prerender phase yet, it will register itself for rendering later.
   * Else it will call the {@link renderRepeater()} method which will do the rendering of the repeater.
   * @param THtmlWriter writer for the rendering purpose
   */
  public function render($writer)
  {
    if($this->getHasPreRendered()) {
      $this->renderDataList($writer);
      if($this->getActiveControl()->canUpdateClientSide()) $this->getPage()->getCallbackClient()->replaceContent($this->getContainerID(),$writer);
    }
    else {
      $this->getPage()->getAdapter()->registerControlToRender($this,$writer);
    }
  }
  
  /**
   * Loops through all {@link TActivePager} on the page and registers the ones which are set to paginate
   * the repeater for rendering. This is to ensure that the connected pagers are also rendered if the 
   * data source changed.
   */
  private function renderPager()
  {
    $pager=$this->getPage()->findControlsByType('TActivePager', false);
    foreach($pager as $item)
    {
      if($item->ControlToPaginate==$this->ID) {
        $writer=$this->getResponse()->createHtmlWriter();
        $this->getPage()->getAdapter()->registerControlToRender($item,$writer);
      }
    }
  }
  
  /**
   * Renders the repeater by writing a span tag with the container id obtained from {@link getContainerID()}
   * which will be called by the replacement method of the client script to update it's content.
   * @param THtmlWriter writer for the rendering purpose
   */
  private function renderDataList($writer)
  {
    $writer->write('<span id="'.$this->getContainerID().'">');
    parent::render($writer);
    $writer->write('</span>');
  }
}
