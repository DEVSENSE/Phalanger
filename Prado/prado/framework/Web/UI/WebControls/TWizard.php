<?php
/**
 * TWizard and the relevant class definitions.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Web.UI.WebControls.TMultiView');
Prado::using('System.Web.UI.WebControls.TPanel');
Prado::using('System.Web.UI.WebControls.TButton');
Prado::using('System.Web.UI.WebControls.TLinkButton');
Prado::using('System.Web.UI.WebControls.TImageButton');
Prado::using('System.Web.UI.WebControls.TDataList');
Prado::using('System.Web.UI.WebControls.TWizardNavigationButtonStyle');

/**
 * Class TWizard.
 *
 * TWizard splits a large form and presents the user with a series of smaller
 * forms to complete. TWizard is analogous to the installation wizard commonly
 * used to install software in Windows.
 *
 * The smaller forms are called wizard steps ({@link TWizardStep}, which can be accessed via
 * {@link getWizardSteps WizardSteps}. In template, wizard steps can be added
 * into a wizard using the following syntax,
 * <code>
 *   <com:TWizard>
 *      <com:TWizardStep Title="step 1">
 *          content in step 1, may contain other controls
 *      </com:TWizardStep>
 *      <com:TWizardStep Title="step 2">
 *          content in step 2, may contain other controls
 *      </com:TWizardStep>
 *   </com:TWizard>
 * </code>
 *
 * Each wizard step can be one of the following types:
 * - Start : the first step in the wizard.
 * - Step : the internal steps in the wizard.
 * - Finish : the last step that allows user interaction.
 * - Complete : the step that shows a summary to user (no interaction is allowed).
 * - Auto : the step type is determined by wizard automatically.
 * At any time, only one step is visible to end-users, which can be obtained
 * by {@link getActiveStep ActiveStep}. Its index in the step collection is given by
 * {@link getActiveStepIndex ActiveStepIndex}.
 *
 * Wizard content can be customized in many ways.
 *
 * The layout of a wizard consists of four parts: header, step content, navigation
 * and side bar. Their content are affected by the following properties, respectively,
 * - header: {@link setHeaderText HeaderText} and {@link setHeaderTemplate HeaderTemplate}.
 *   If both are present, the latter takes precedence.
 * - step: {@link getWizardSteps WizardSteps}.
 * - navigation: {@link setStartNavigationTemplate StartNavigationTemplate},
 *   {@link setStepNavigationTemplate StepNavigationTemplate},
 *   {@link setFinishNavigationTemplate FinishNavigationTemplate}.
 *   Default templates will be used if above templates are not set.
 * - side bar: {@link setSideBarTemplate SideBarTemplate}.
 *   A default template will be used if this template is not set.
 *   Its visibility is toggled by {@link setShowSideBar ShowSideBar}.
 *
 * The style of these wizard layout components can be customized via the following style properties,
 * - header: {@link getHeaderStyle HeaderStyle}.
 * - step: {@link getStepStyle StepStyle}.
 * - navigation: {@link getNavigationStyle NavigationStyle},
 *   {@link getStartNextButtonStyle StartNextButtonStyle},
 *   {@link getStepNextButtonStyle StepNextButtonStyle},
 *   {@link getStepPreviousButtonStyle StepPreviousButtonStyle},
 *   {@link getFinishPreviousButtonStyle FinishPreviousButtonStyle},
 *   {@link getFinishCompleteButtonStyle FinishCompleteButtonStyle},
 *   {@link getCancelButtonStyle CancelButtonStyle}.
 * - side bar: {@link getSideBarStyle SideBarStyle} and {@link getSideBarButtonStyle SideBarButtonStyle}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizard extends TWebControl implements INamingContainer
{
	/**
	 * Wizard step types.
	 * @deprecated deprecated since version 3.0.4 (use TWizardStepType constants instead)
	 */
	const ST_AUTO='Auto';
	const ST_START='Start';
	const ST_STEP='Step';
	const ST_FINISH='Finish';
	const ST_COMPLETE='Complete';
	/**
	 * Navigation commands.
	 */
	const CMD_PREVIOUS='PreviousStep';
	const CMD_NEXT='NextStep';
	const CMD_CANCEL='Cancel';
	const CMD_COMPLETE='Complete';
	const CMD_MOVETO='MoveTo';
	/**
	 * Side bar button ID
	 */
	const ID_SIDEBAR_BUTTON='SideBarButton';
	/**
	 * Side bar data list
	 */
	const ID_SIDEBAR_LIST='SideBarList';

	/**
	 * @var TMultiView multiview that contains the wizard steps
	 */
	private $_multiView=null;
	/**
	 * @var mixed navigation template for the start step.
	 */
	private $_startNavigationTemplate=null;
	/**
	 * @var mixed navigation template for internal steps.
	 */
	private $_stepNavigationTemplate=null;
	/**
	 * @var mixed navigation template for the finish step.
	 */
	private $_finishNavigationTemplate=null;
	/**
	 * @var mixed template for wizard header.
	 */
	private $_headerTemplate=null;
	/**
	 * @var mixed template for the side bar.
	 */
	private $_sideBarTemplate=null;
	/**
	 * @var TWizardStepCollection
	 */
	private $_wizardSteps=null;
	/**
	 * @var TPanel container of the wizard header
	 */
	private $_header;
	/**
	 * @var TPanel container of the wizard step content
	 */
	private $_stepContent;
	/**
	 * @var TPanel container of the wizard side bar
	 */
	private $_sideBar;
	/**
	 * @var TPanel navigation panel
	 */
	private $_navigation;
	/**
	 * @var TWizardNavigationContainer container of the start navigation
	 */
	private $_startNavigation;
	/**
	 * @var TWizardNavigationContainer container of the step navigation
	 */
	private $_stepNavigation;
	/**
	 * @var TWizardNavigationContainer container of the finish navigation
	 */
	private $_finishNavigation;
	/**
	 * @var boolean whether ActiveStepIndex was already set
	 */
	private $_activeStepIndexSet=false;
	/**
	 * @var TDataList side bar data list.
	 */
	private $_sideBarDataList;
	/**
	 * @var boolean whether navigation should be cancelled (a status set in OnSideBarButtonClick)
	 */
	private $_cancelNavigation=false;

	/**
	 * @return string tag name for the wizard
	 */
	protected function getTagName()
	{
		return 'div';
	}

	/**
	 * Adds {@link TWizardStep} objects into step collection.
	 * This method overrides the parent implementation and is
	 * invoked when template is being instantiated.
	 * @param mixed object instantiated in template
	 */
	public function addParsedObject($object)
	{
		if($object instanceof TWizardStep)
			$this->getWizardSteps()->add($object);
	}

	/**
	 * @return TWizardStep the currently active wizard step
	 */
	public function getActiveStep()
	{
		return $this->getMultiView()->getActiveView();
	}

	/**
	 * @param TWizardStep step to be activated
	 * @throws TInvalidOperationException if the step is not in the wizard step collection
	 */
	public function setActiveStep($step)
	{
		if(($index=$this->getWizardSteps()->indexOf($step))<0)
			throw new TInvalidOperationException('wizard_step_invalid');
		$this->setActiveStepIndex($index);
	}

	/**
	 * @return integer the zero-based index of the active wizard step
	 */
	public function getActiveStepIndex()
	{
		return $this->getMultiView()->getActiveViewIndex();
	}

	/**
	 * @param integer the zero-based index of the wizard step to be activated
	 */
	public function setActiveStepIndex($value)
	{
		$value=TPropertyValue::ensureInteger($value);
		$multiView=$this->getMultiView();
		if($multiView->getActiveViewIndex()!==$value)
		{
			$multiView->setActiveViewIndex($value);
			$this->_activeStepIndexSet=true;
			if($this->_sideBarDataList!==null && $this->getSideBarTemplate()!==null)
			{
				$this->_sideBarDataList->setSelectedItemIndex($this->getActiveStepIndex());
				$this->_sideBarDataList->dataBind();
			}
		}
	}

	/**
	 * @return TWizardStepCollection collection of wizard steps
	 */
	public function getWizardSteps()
	{
		if($this->_wizardSteps===null)
			$this->_wizardSteps=new TWizardStepCollection($this);
		return $this->_wizardSteps;
	}

	/**
	 * @return boolean whether to display a cancel button in each wizard step. Defaults to false.
	 */
	public function getShowCancelButton()
	{
		return $this->getViewState('ShowCancelButton',false);
	}

	/**
	 * @param boolean whether to display a cancel button in each wizard step.
	 */
	public function setShowCancelButton($value)
	{
		$this->setViewState('ShowCancelButton',TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * @return boolean whether to display a side bar that contains links to wizard steps. Defaults to true.
	 */
	public function getShowSideBar()
	{
		return $this->getViewState('ShowSideBar',true);
	}

	/**
	 * @param boolean whether to display a side bar that contains links to wizard steps.
	 */
	public function setShowSideBar($value)
	{
		$this->setViewState('ShowSideBar',TPropertyValue::ensureBoolean($value),true);
		$this->requiresControlsRecreation();
	}

	/**
	 * @return ITemplate navigation template for the start step. Defaults to null.
	 */
	public function getStartNavigationTemplate()
	{
		return $this->_startNavigationTemplate;
	}

	/**
	 * @param ITemplate navigation template for the start step.
	 */
	public function setStartNavigationTemplate($value)
	{
		$this->_startNavigationTemplate=$value;
		$this->requiresControlsRecreation();
	}

	/**
	 * @return ITemplate navigation template for internal steps. Defaults to null.
	 */
	public function getStepNavigationTemplate()
	{
		return $this->_stepNavigationTemplate;
	}

	/**
	 * @param ITemplate navigation template for internal steps.
	 */
	public function setStepNavigationTemplate($value)
	{
		$this->_stepNavigationTemplate=$value;
		$this->requiresControlsRecreation();
	}

	/**
	 * @return ITemplate navigation template for the finish step. Defaults to null.
	 */
	public function getFinishNavigationTemplate()
	{
		return $this->_finishNavigationTemplate;
	}

	/**
	 * @param ITemplate navigation template for the finish step.
	 */
	public function setFinishNavigationTemplate($value)
	{
		$this->_finishNavigationTemplate=$value;
		$this->requiresControlsRecreation();
	}

	/**
	 * @return ITemplate template for wizard header. Defaults to null.
	 */
	public function getHeaderTemplate()
	{
		return $this->_headerTemplate;
	}

	/**
	 * @param ITemplate template for wizard header.
	 */
	public function setHeaderTemplate($value)
	{
		$this->_headerTemplate=$value;
		$this->requiresControlsRecreation();
	}

	/**
	 * @return ITemplate template for the side bar. Defaults to null.
	 */
	public function getSideBarTemplate()
	{
		return $this->_sideBarTemplate;
	}

	/**
	 * @param ITemplate template for the side bar.
	 */
	public function setSideBarTemplate($value)
	{
		$this->_sideBarTemplate=$value;
		$this->requiresControlsRecreation();
	}

	/**
	 * @return string header text. Defaults to ''.
	 */
	public function getHeaderText()
	{
		return $this->getViewState('HeaderText','');
	}

	/**
	 * @param string header text.
	 */
	public function setHeaderText($value)
	{
		$this->setViewState('HeaderText',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string the URL that the browser will be redirected to if the cancel button in the
	 * wizard is clicked. Defaults to ''.
	 */
	public function getCancelDestinationUrl()
	{
		return $this->getViewState('CancelDestinationUrl','');
	}

	/**
	 * @param string the URL that the browser will be redirected to if the cancel button in the
	 * wizard is clicked.
	 */
	public function setCancelDestinationUrl($value)
	{
		$this->setViewState('CancelDestinationUrl',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string the URL that the browser will be redirected to if the wizard finishes.
	 * Defaults to ''.
	 */
	public function getFinishDestinationUrl()
	{
		return $this->getViewState('FinishDestinationUrl','');
	}

	/**
	 * @param string the URL that the browser will be redirected to if the wizard finishes.
	 */
	public function setFinishDestinationUrl($value)
	{
		$this->setViewState('FinishDestinationUrl',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return TStyle the style for the buttons displayed in the side bar.
	 */
	public function getSideBarButtonStyle()
	{
		if(($style=$this->getViewState('SideBarButtonStyle',null))===null)
		{
			$style=new TStyle;
			$this->setViewState('SideBarButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TStyle the style common for all navigation buttons.
	 */
	public function getNavigationButtonStyle()
	{
		if(($style=$this->getViewState('NavigationButtonStyle',null))===null)
		{
			$style=new TStyle;
			$this->setViewState('NavigationButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TWizardNavigationButtonStyle the style for the next button in the start wizard step.
	 */
	public function getStartNextButtonStyle()
	{
		if(($style=$this->getViewState('StartNextButtonStyle',null))===null)
		{
			$style=new TWizardNavigationButtonStyle;
			$style->setButtonText('Next');
			$this->setViewState('StartNextButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TWizardNavigationButtonStyle the style for the next button in each internal wizard step.
	 */
	public function getStepNextButtonStyle()
	{
		if(($style=$this->getViewState('StepNextButtonStyle',null))===null)
		{
			$style=new TWizardNavigationButtonStyle;
			$style->setButtonText('Next');
			$this->setViewState('StepNextButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TWizardNavigationButtonStyle the style for the previous button in the start wizard step.
	 */
	public function getStepPreviousButtonStyle()
	{
		if(($style=$this->getViewState('StepPreviousButtonStyle',null))===null)
		{
			$style=new TWizardNavigationButtonStyle;
			$style->setButtonText('Previous');
			$this->setViewState('StepPreviousButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TWizardNavigationButtonStyle the style for the complete button in the finish wizard step.
	 */
	public function getFinishCompleteButtonStyle()
	{
		if(($style=$this->getViewState('FinishCompleteButtonStyle',null))===null)
		{
			$style=new TWizardNavigationButtonStyle;
			$style->setButtonText('Complete');
			$this->setViewState('FinishCompleteButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TWizardNavigationButtonStyle the style for the previous button in the start wizard step.
	 */
	public function getFinishPreviousButtonStyle()
	{
		if(($style=$this->getViewState('FinishPreviousButtonStyle',null))===null)
		{
			$style=new TWizardNavigationButtonStyle;
			$style->setButtonText('Previous');
			$this->setViewState('FinishPreviousButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TWizardNavigationButtonStyle the style for the cancel button
	 */
	public function getCancelButtonStyle()
	{
		if(($style=$this->getViewState('CancelButtonStyle',null))===null)
		{
			$style=new TWizardNavigationButtonStyle;
			$style->setButtonText('Cancel');
			$this->setViewState('CancelButtonStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TPanelStyle the style for the side bar.
	 */
	public function getSideBarStyle()
	{
		if(($style=$this->getViewState('SideBarStyle',null))===null)
		{
			$style=new TPanelStyle;
			$this->setViewState('SideBarStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TPanelStyle the style for the header.
	 */
	public function getHeaderStyle()
	{
		if(($style=$this->getViewState('HeaderStyle',null))===null)
		{
			$style=new TPanelStyle;
			$this->setViewState('HeaderStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TPanelStyle the style for each internal wizard step.
	 */
	public function getStepStyle()
	{
		if(($style=$this->getViewState('StepStyle',null))===null)
		{
			$style=new TPanelStyle;
			$this->setViewState('StepStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return TPanelStyle the style for the navigation panel.
	 */
	public function getNavigationStyle()
	{
		if(($style=$this->getViewState('NavigationStyle',null))===null)
		{
			$style=new TPanelStyle;
			$this->setViewState('NavigationStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return boolean whether to use default layout to arrange side bar and the rest wizard components. Defaults to true.
	 */
	public function getUseDefaultLayout()
	{
		return $this->getViewState('UseDefaultLayout',true);
	}

	/**
	 * @param boolean whether to use default layout to arrange side bar and the rest wizard components.
	 * If true, an HTML table will be used which places the side bar in the left cell
	 * while the rest components in the right cell.
	 */
	public function setUseDefaultLayout($value)
	{
		$this->setViewState('UseDefaultLayout',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return TPanel container of the wizard header
	 */
	public function getHeader()
	{
		return $this->_header;
	}

	/**
	 * @return TPanel container of the wizard step content
	 */
	public function getStepContent()
	{
		return $this->_stepContent;
	}

	/**
	 * @return TPanel container of the wizard side bar
	 */
	public function getSideBar()
	{
		return $this->_sideBar;
	}

	/**
	 * @return TWizardNavigationContainer container of the start navigation
	 */
	public function getStartNavigation()
	{
		return $this->_startNavigation;
	}

	/**
	 * @return TWizardNavigationContainer container of the step navigation
	 */
	public function getStepNavigation()
	{
		return $this->_stepNavigation;
	}

	/**
	 * @return TWizardNavigationContainer container of the finish navigation
	 */
	public function getFinishNavigation()
	{
		return $this->_finishNavigation;
	}

	/**
	 * Raises <b>OnActiveStepChanged</b> event.
	 * This event is raised when the current visible step is changed in the
	 * wizard.
	 * @param TEventParameter event parameter
	 */
	public function onActiveStepChanged($param)
	{
		$this->raiseEvent('OnActiveStepChanged',$this,$param);
	}

	/**
	 * Raises <b>OnCancelButtonClick</b> event.
	 * This event is raised when a cancel navigation button is clicked in the
	 * current active step.
	 * @param TEventParameter event parameter
	 */
	public function onCancelButtonClick($param)
	{
		$this->raiseEvent('OnCancelButtonClick',$this,$param);
		if(($url=$this->getCancelDestinationUrl())!=='')
			$this->getResponse()->redirect($url);
	}

	/**
	 * Raises <b>OnCompleteButtonClick</b> event.
	 * This event is raised when a finish navigation button is clicked in the
	 * current active step.
	 * @param TWizardNavigationEventParameter event parameter
	 */
	public function onCompleteButtonClick($param)
	{
		$this->raiseEvent('OnCompleteButtonClick',$this,$param);
		if(($url=$this->getFinishDestinationUrl())!=='')
			$this->getResponse()->redirect($url);
	}

	/**
	 * Raises <b>OnNextButtonClick</b> event.
	 * This event is raised when a next navigation button is clicked in the
	 * current active step.
	 * @param TWizardNavigationEventParameter event parameter
	 */
	public function onNextButtonClick($param)
	{
		$this->raiseEvent('OnNextButtonClick',$this,$param);
	}

	/**
	 * Raises <b>OnPreviousButtonClick</b> event.
	 * This event is raised when a previous navigation button is clicked in the
	 * current active step.
	 * @param TWizardNavigationEventParameter event parameter
	 */
	public function onPreviousButtonClick($param)
	{
		$this->raiseEvent('OnPreviousButtonClick',$this,$param);
	}

	/**
	 * Raises <b>OnSideBarButtonClick</b> event.
	 * This event is raised when a link button in the side bar is clicked.
	 * @param TWizardNavigationEventParameter event parameter
	 */
	public function onSideBarButtonClick($param)
	{
		$this->raiseEvent('OnSideBarButtonClick',$this,$param);
	}

	/**
	 * Returns the multiview that holds the wizard steps.
	 * This method should only be used by control developers.
	 * @return TMultiView the multiview holding wizard steps
	 */
	public function getMultiView()
	{
		if($this->_multiView===null)
		{
			$this->_multiView=new TMultiView;
			$this->_multiView->setID('WizardMultiView');
			$this->_multiView->attachEventHandler('OnActiveViewChanged',array($this,'onActiveStepChanged'));
			$this->_multiView->ignoreBubbleEvents();
		}
		return $this->_multiView;
	}

	/**
	 * Adds a wizard step to the multiview.
	 * This method should only be used by control developers.
	 * It is invoked when a step is added into the step collection of the wizard.
	 * @param TWizardStep wizard step to be added into multiview.
	 */
	public function addedWizardStep($step)
	{
		if(($wizard=$step->getWizard())!==null)
			$wizard->getWizardSteps()->remove($step);
		$step->setWizard($this);
		$this->wizardStepsChanged();
	}

	/**
	 * Removes a wizard step from the multiview.
	 * This method should only be used by control developers.
	 * It is invoked when a step is removed from the step collection of the wizard.
	 * @param TWizardStep wizard step to be removed from multiview.
	 */
	public function removedWizardStep($step)
	{
		$step->setWizard(null);
		$this->wizardStepsChanged();
	}

	/**
	 * Creates the child controls of the wizard.
	 * This method overrides the parent implementation.
	 * @param TEventParameter event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		$this->ensureChildControls();
		if($this->getActiveStepIndex()<0 && $this->getWizardSteps()->getCount()>0)
			$this->setActiveStepIndex(0);
	}

	/**
	 * Saves the current active step index into history.
	 * This method is invoked by the framework when the control state is being saved.
	 */
	public function saveState()
	{
		$index=$this->getActiveStepIndex();
		$history=$this->getHistory();
		if(!$history->getCount() || $history->peek()!==$index)
			$history->push($index);
	}

	/**
	 * Indicates the wizard needs to recreate all child controls.
	 */
	protected function requiresControlsRecreation()
	{
		if($this->getChildControlsCreated())
			$this->setChildControlsCreated(false);
	}

	/**
	 * Renders the wizard.
	 * @param THtmlWriter
	 */
	public function render($writer)
	{
		$this->ensureChildControls();
		if($this->getHasControls())
		{
			if($this->getUseDefaultLayout())
			{
				$this->applyControlProperties();
				$this->renderBeginTag($writer);
				$writer->write("\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" height=\"100%\" width=\"100%\">\n<tr><td width=\"1\" valign=\"top\">\n");
				$this->_sideBar->renderControl($writer);
				$writer->write("\n</td><td valign=\"top\">\n");
				$this->_header->renderControl($writer);
				$this->_stepContent->renderControl($writer);
				$this->_navigation->renderControl($writer);
				$writer->write("\n</td></tr></table>\n");
				$this->renderEndTag($writer);
			}
			else
			{
				$this->applyControlProperties();
				$this->renderBeginTag($writer);
				$this->_sideBar->renderControl($writer);
				$this->_header->renderControl($writer);
				$this->_stepContent->renderControl($writer);
				$this->_navigation->renderControl($writer);
				$this->renderEndTag($writer);
			}
		}
	}

	/**
	 * Applies various properties to the components of wizard
	 */
	protected function applyControlProperties()
	{
		$this->applyHeaderProperties();
		$this->applySideBarProperties();
		$this->applyStepContentProperties();
		$this->applyNavigationProperties();
	}

	/**
	 * Applies properties to the wizard header
	 */
	protected function applyHeaderProperties()
	{
		if(($style=$this->getViewState('HeaderStyle',null))!==null)
			$this->_header->getStyle()->mergeWith($style);
		if($this->getHeaderTemplate()===null)
		{
			$this->_header->getControls()->clear();
			$this->_header->getControls()->add($this->getHeaderText());
		}
	}

	/**
	 * Applies properties to the wizard sidebar
	 */
	protected function applySideBarProperties()
	{
		$this->_sideBar->setVisible($this->getShowSideBar());
		if($this->_sideBarDataList!==null && $this->getShowSideBar())
		{
			$this->_sideBarDataList->setDataSource($this->getWizardSteps());
			$this->_sideBarDataList->setSelectedItemIndex($this->getActiveStepIndex());
			$this->_sideBarDataList->dataBind();
			if(($style=$this->getViewState('SideBarButtonStyle',null))!==null)
			{
				foreach($this->_sideBarDataList->getItems() as $item)
				{
					if(($button=$item->findControl('SideBarButton'))!==null)
						$button->getStyle()->mergeWith($style);
				}
			}
		}
		if(($style=$this->getViewState('SideBarStyle',null))!==null)
			$this->_sideBar->getStyle()->mergeWith($style);
	}

	/**
	 * Applies properties to the wizard step content
	 */
	protected function applyStepContentProperties()
	{
		if(($style=$this->getViewState('StepStyle',null))!==null)
			$this->_stepContent->getStyle()->mergeWith($style);
	}

	/**
	 * Apply properties to various navigation panels.
	 */
	protected function applyNavigationProperties()
	{
		$wizardSteps=$this->getWizardSteps();
		$activeStep=$this->getActiveStep();
		$activeStepIndex=$this->getActiveStepIndex();

		if(!$this->_navigation)
			return;
		else if($activeStepIndex<0 || $activeStepIndex>=$wizardSteps->getCount())
		{
			$this->_navigation->setVisible(false);
			return;
		}

		// set visibility of different types of navigation panel
		$showStandard=true;
		foreach($wizardSteps as $step)
		{
			if(($step instanceof TTemplatedWizardStep) && ($container=$step->getNavigationContainer())!==null)
			{
				if($activeStep===$step)
				{
					$container->setVisible(true);
					$showStandard=false;
				}
				else
					$container->setVisible(false);
			}
		}
		$activeStepType=$this->getStepType($activeStep);
		if($activeStepType===TWizardStepType::Complete)
		{
			$this->_sideBar->setVisible(false);
			$this->_header->setVisible(false);
		}
		$this->_startNavigation->setVisible($showStandard && $activeStepType===self::ST_START);
		$this->_stepNavigation->setVisible($showStandard && $activeStepType===self::ST_STEP);
		$this->_finishNavigation->setVisible($showStandard && $activeStepType===self::ST_FINISH);

		if(($navigationStyle=$this->getViewState('NavigationStyle',null))!==null)
			$this->_navigation->getStyle()->mergeWith($navigationStyle);

		$displayCancelButton=$this->getShowCancelButton();
		$cancelButtonStyle=$this->getCancelButtonStyle();
		$buttonStyle=$this->getViewState('NavigationButtonStyle',null);
		if($buttonStyle!==null)
			$cancelButtonStyle->mergeWith($buttonStyle);

		// apply styles to start navigation buttons
		if(($cancelButton=$this->_startNavigation->getCancelButton())!==null)
		{
			$cancelButton->setVisible($displayCancelButton);
			$cancelButtonStyle->apply($cancelButton);
		}
		if(($button=$this->_startNavigation->getNextButton())!==null)
		{
			$button->setVisible(true);
			$style=$this->getStartNextButtonStyle();
			if($buttonStyle!==null)
				$style->mergeWith($buttonStyle);
			$style->apply($button);
			if($activeStepType===TWizardStepType::Start)
				$this->getPage()->getClientScript()->registerDefaultButton($this, $button);
		}

		// apply styles to finish navigation buttons
		if(($cancelButton=$this->_finishNavigation->getCancelButton())!==null)
		{
			$cancelButton->setVisible($displayCancelButton);
			$cancelButtonStyle->apply($cancelButton);
		}
		if(($button=$this->_finishNavigation->getPreviousButton())!==null)
		{
			$button->setVisible($this->allowNavigationToPreviousStep());
			$style=$this->getFinishPreviousButtonStyle();
			if($buttonStyle!==null)
				$style->mergeWith($buttonStyle);
			$style->apply($button);
		}
		if(($button=$this->_finishNavigation->getCompleteButton())!==null)
		{
			$button->setVisible(true);
			$style=$this->getFinishCompleteButtonStyle();
			if($buttonStyle!==null)
				$style->mergeWith($buttonStyle);
			$style->apply($button);
			if($activeStepType===TWizardStepType::Finish)
				$this->getPage()->getClientScript()->registerDefaultButton($this, $button);
		}

		// apply styles to step navigation buttons
		if(($cancelButton=$this->_stepNavigation->getCancelButton())!==null)
		{
			$cancelButton->setVisible($displayCancelButton);
			$cancelButtonStyle->apply($cancelButton);
		}
		if(($button=$this->_stepNavigation->getPreviousButton())!==null)
		{
			$button->setVisible($this->allowNavigationToPreviousStep());
			$style=$this->getStepPreviousButtonStyle();
			if($buttonStyle!==null)
				$style->mergeWith($buttonStyle);
			$style->apply($button);
		}
		if(($button=$this->_stepNavigation->getNextButton())!==null)
		{
			$button->setVisible(true);
			$style=$this->getStepNextButtonStyle();
			if($buttonStyle!==null)
				$style->mergeWith($buttonStyle);
			$style->apply($button);
			if($activeStepType===TWizardStepType::Step)
				$this->getPage()->getClientScript()->registerDefaultButton($this, $button);
		}
	}

	/**
	 * @return TStack history containing step indexes that were navigated before
	 */
	protected function getHistory()
	{
		if(($history=$this->getControlState('History',null))===null)
		{
			$history=new TStack;
			$this->setControlState('History',$history);
		}
		return $history;
	}

	/**
	 * Determines the type of the specified wizard step.
	 * @param TWizardStep
	 * @return TWizardStepType type of the step
	 */
	protected function getStepType($wizardStep)
	{
		if(($type=$wizardStep->getStepType())===TWizardStepType::Auto)
		{
			$steps=$this->getWizardSteps();
			if(($index=$steps->indexOf($wizardStep))>=0)
			{
				$stepCount=$steps->getCount();
				if($stepCount===1 || ($index<$stepCount-1 && $steps->itemAt($index+1)->getStepType()===TWizardStepType::Complete))
					return TWizardStepType::Finish;
				else if($index===0)
					return TWizardStepType::Start;
				else if($index===$stepCount-1)
					return TWizardStepType::Finish;
				else
					return TWizardStepType::Step;
			}
			else
				return $type;
		}
		else
			return $type;
	}

	/**
	 * Clears up everything within the wizard.
	 */
	protected function reset()
	{
		$this->getControls()->clear();
		$this->_header=null;
		$this->_stepContent=null;
		$this->_sideBar=null;
		$this->_sideBarDataList=null;
		$this->_navigation=null;
		$this->_startNavigation=null;
		$this->_stepNavigation=null;
		$this->_finishNavigation=null;
	}

	/**
	 * Creates child controls within the wizard
	 */
	public function createChildControls()
	{
		$this->reset();
		$this->createSideBar();
		$this->createHeader();
		$this->createStepContent();
		$this->createNavigation();
//		$this->clearChildState();
	}

	/**
	 * Creates the wizard header.
	 */
	protected function createHeader()
	{
		$this->_header=new TPanel;
		if(($template=$this->getHeaderTemplate())!==null)
			$template->instantiateIn($this->_header);
		else
			$this->_header->getControls()->add($this->getHeaderText());
		$this->getControls()->add($this->_header);
	}

	/**
	 * Creates the wizard side bar
	 */
	protected function createSideBar()
	{
		if($this->getShowSideBar())
		{
			if(($template=$this->getSideBarTemplate())===null)
				$template=new TWizardSideBarTemplate;
			$this->_sideBar=new TPanel;
			$template->instantiateIn($this->_sideBar);
			$this->getControls()->add($this->_sideBar);

			if(($this->_sideBarDataList=$this->_sideBar->findControl(self::ID_SIDEBAR_LIST))!==null)
			{
				$this->_sideBarDataList->attachEventHandler('OnItemCommand',array($this,'dataListItemCommand'));
				$this->_sideBarDataList->attachEventHandler('OnItemDataBound',array($this,'dataListItemDataBound'));
				$this->_sideBarDataList->setDataSource($this->getWizardSteps());
				$this->_sideBarDataList->setSelectedItemIndex($this->getActiveStepIndex());
				$this->_sideBarDataList->dataBind();
			}
		}
		else
		{
			$this->_sideBar=new TPanel;
			$this->getControls()->add($this->_sideBar);
		}
	}

	/**
	 * Event handler for sidebar datalist's OnItemCommand event.
	 * This method is used internally by wizard. It mainly
	 * sets the active step index according to the button clicked in the sidebar.
	 * @param mixed sender of the event
	 * @param TDataListCommandEventParameter
	 */
	public function dataListItemCommand($sender,$param)
	{
		$item=$param->getItem();
		if($param->getCommandName()===self::CMD_MOVETO)
		{
			$stepIndex=$this->getActiveStepIndex();
			$newStepIndex=TPropertyValue::ensureInteger($param->getCommandParameter());
			$navParam=new TWizardNavigationEventParameter($stepIndex);
			$navParam->setNextStepIndex($newStepIndex);

			// if the button clicked causes validation which fails,
			// by default we will cancel navigation to the new step
			$button=$param->getCommandSource();
			if(($button instanceof IButtonControl) && $button->getCausesValidation() && ($page=$this->getPage())!==null && !$page->getIsValid())
				$navParam->setCancelNavigation(true);

			$this->_activeStepIndexSet=false;
			$this->onSideBarButtonClick($navParam);
			$this->_cancelNavigation=$navParam->getCancelNavigation();
			if(!$this->_cancelNavigation)
			{
				if(!$this->_activeStepIndexSet && $this->allowNavigationToStep($newStepIndex))
					$this->setActiveStepIndex($newStepIndex);
			}
			else
				$this->setActiveStepIndex($stepIndex);
		}
	}

	/**
	 * Event handler for sidebar datalist's OnItemDataBound event.
	 * This method is used internally by wizard. It mainly configures
	 * the buttons in the sidebar datalist.
	 * @param mixed sender of the event
	 * @param TDataListItemEventParameter
	 */
	public function dataListItemDataBound($sender,$param)
	{
		$item=$param->getItem();
		$itemType=$item->getItemType();
		if($itemType==='Item' || $itemType==='AlternatingItem' || $itemType==='SelectedItem' || $itemType==='EditItem')
		{
			if(($button=$item->findControl(self::ID_SIDEBAR_BUTTON))!==null)
			{
				$step=$item->getData();
				if(($this->getStepType($step)===TWizardStepType::Complete))
					$button->setEnabled(false);
				if(($title=$step->getTitle())!=='')
					$button->setText($title);
				else
					$button->setText($step->getID(false));
				$index=$this->getWizardSteps()->indexOf($step);
				$button->setCommandName(self::CMD_MOVETO);
				$button->setCommandParameter("$index");
			}
		}
	}

	/**
	 * Creates wizard step content.
	 */
	protected function createStepContent()
	{
		foreach($this->getWizardSteps() as $step)
		{
			if($step instanceof TTemplatedWizardStep)
				$step->ensureChildControls();
		}
		$multiView=$this->getMultiView();
		$this->_stepContent=new TPanel;
		$this->_stepContent->getControls()->add($multiView);
		$this->getControls()->add($this->_stepContent);
		if($multiView->getViews()->getCount())
			$multiView->setActiveViewIndex(0);
	}

	/**
	 * Creates navigation panel.
	 */
	protected function createNavigation()
	{
		$this->_navigation=new TPanel;
		$this->getControls()->add($this->_navigation);
		$controls=$this->_navigation->getControls();
		foreach($this->getWizardSteps() as $step)
		{
			if($step instanceof TTemplatedWizardStep)
			{
				$step->instantiateNavigationTemplate();
				if(($panel=$step->getNavigationContainer())!==null)
					$controls->add($panel);
			}
		}
		$this->_startNavigation=$this->createStartNavigation();
		$controls->add($this->_startNavigation);
		$this->_stepNavigation=$this->createStepNavigation();
		$controls->add($this->_stepNavigation);
		$this->_finishNavigation=$this->createFinishNavigation();
		$controls->add($this->_finishNavigation);
	}

	/**
	 * Creates start navigation panel.
	 */
	protected function createStartNavigation()
	{
		if(($template=$this->getStartNavigationTemplate())===null)
			$template=new TWizardStartNavigationTemplate($this);
		$navigation=new TWizardNavigationContainer;
		$template->instantiateIn($navigation);
		return $navigation;
	}

	/**
	 * Creates step navigation panel.
	 */
	protected function createStepNavigation()
	{
		if(($template=$this->getStepNavigationTemplate())===null)
			$template=new TWizardStepNavigationTemplate($this);
		$navigation=new TWizardNavigationContainer;
		$template->instantiateIn($navigation);
		return $navigation;
	}

	/**
	 * Creates finish navigation panel.
	 */
	protected function createFinishNavigation()
	{
		if(($template=$this->getFinishNavigationTemplate())===null)
			$template=new TWizardFinishNavigationTemplate($this);
		$navigation=new TWizardNavigationContainer;
		$template->instantiateIn($navigation);
		return $navigation;
	}

	/**
	 * Updates the sidebar datalist if any.
	 * This method is invoked when any wizard step is changed.
	 */
	public function wizardStepsChanged()
	{
		if($this->_sideBarDataList!==null)
		{
			$this->_sideBarDataList->setDataSource($this->getWizardSteps());
			$this->_sideBarDataList->setSelectedItemIndex($this->getActiveStepIndex());
			$this->_sideBarDataList->dataBind();
		}
	}

	/**
	 * Determines the index of the previous step based on history.
	 * @param boolean whether the first item in the history stack should be popped
	 * up after calling this method.
	 */
	protected function getPreviousStepIndex($popStack)
	{
		$history=$this->getHistory();
		if($history->getCount()>=0)
		{
			$activeStepIndex=$this->getActiveStepIndex();
			$previousStepIndex=-1;
			if($popStack)
			{
				$previousStepIndex=$history->pop();
				if($activeStepIndex===$previousStepIndex && $history->getCount()>0)
					$previousStepIndex=$history->pop();
			}
			else
			{
				$previousStepIndex=$history->peek();
				if($activeStepIndex===$previousStepIndex && $history->getCount()>1)
				{
					$saveIndex=$history->pop();
					$previousStepIndex=$history->peek();
					$history->push($saveIndex);
				}
			}
			return $activeStepIndex===$previousStepIndex ? -1 : $previousStepIndex;
		}
		else
			return -1;
	}

	/**
	 * @return boolean whether navigation to the previous step is allowed
	 */
	protected function allowNavigationToPreviousStep()
	{
		if(($index=$this->getPreviousStepIndex(false))!==-1)
			return $this->getWizardSteps()->itemAt($index)->getAllowReturn();
		else
			return false;
	}

	/**
	 * @param integer index of the step
	 * @return boolean whether navigation to the specified step is allowed
	 */
	protected function allowNavigationToStep($index)
	{
		if($this->getHistory()->contains($index))
			return $this->getWizardSteps()->itemAt($index)->getAllowReturn();
		else
			return true;
	}

	/**
	 * Handles bubbled events.
	 * This method mainly translate certain command events into
	 * wizard-specific events.
	 * @param mixed sender of the original command event
	 * @param TEventParameter event parameter
	 * @throws TInvalidDataValueException if a navigation command is associated with an invalid parameter
	 */
	public function bubbleEvent($sender,$param)
	{
		if($param instanceof TCommandEventParameter)
		{
			$command=$param->getCommandName();
			if(strcasecmp($command,self::CMD_CANCEL)===0)
			{
				$this->onCancelButtonClick($param);
				return true;
			}

			$type=$this->getStepType($this->getActiveStep());
			$index=$this->getActiveStepIndex();
			$navParam=new TWizardNavigationEventParameter($index);
			if(($sender instanceof IButtonControl) && $sender->getCausesValidation() && ($page=$this->getPage())!==null && !$page->getIsValid())
				$navParam->setCancelNavigation(true);

			$handled=false;
			$movePrev=false;
			$this->_activeStepIndexSet=false;

			if(strcasecmp($command,self::CMD_NEXT)===0)
			{
				if($type!==self::ST_START && $type!==self::ST_STEP)
					throw new TInvalidDataValueException('wizard_command_invalid',self::CMD_NEXT);
				if($index<$this->getWizardSteps()->getCount()-1)
					$navParam->setNextStepIndex($index+1);
				$this->onNextButtonClick($navParam);
				$handled=true;
			}
			else if(strcasecmp($command,self::CMD_PREVIOUS)===0)
			{
				if($type!==self::ST_FINISH && $type!==self::ST_STEP)
					throw new TInvalidDataValueException('wizard_command_invalid',self::CMD_PREVIOUS);
				$movePrev=true;
				if(($prevIndex=$this->getPreviousStepIndex(false))>=0)
					$navParam->setNextStepIndex($prevIndex);
				$this->onPreviousButtonClick($navParam);
				$handled=true;
			}
			else if(strcasecmp($command,self::CMD_COMPLETE)===0)
			{
				if($type!==self::ST_FINISH)
					throw new TInvalidDataValueException('wizard_command_invalid',self::CMD_COMPLETE);
				if($index<$this->getWizardSteps()->getCount()-1)
					$navParam->setNextStepIndex($index+1);
				$this->onCompleteButtonClick($navParam);
				$handled=true;
			}
			else if(strcasecmp($command,self::CMD_MOVETO)===0)
			{
				if($this->_cancelNavigation)  // may be set in onSideBarButtonClick
					$navParam->setCancelNavigation(true);
				$requestedStep=$param->getCommandParameter();
				if (!is_numeric($requestedStep))
				{
					$requestedIndex=-1;
					foreach ($this->getWizardSteps() as $index=>$step)
						if ($step->getId()===$requestedStep)
						{
							$requestedIndex=$index;
							break;
						}
					if ($requestedIndex<0)
						throw new TConfigurationException('wizard_step_invalid');
				}
				else
					$requestedIndex=TPropertyValue::ensureInteger($requestedStep);
				$navParam->setNextStepIndex($requestedIndex);
				$handled=true;
			}

			if($handled)
			{
				if(!$navParam->getCancelNavigation())
				{
					$nextStepIndex=$navParam->getNextStepIndex();
					if(!$this->_activeStepIndexSet && $this->allowNavigationToStep($nextStepIndex))
					{
						if($movePrev)
							$this->getPreviousStepIndex(true);  // pop out the previous move from history
						$this->setActiveStepIndex($nextStepIndex);
					}
				}
				else
					$this->setActiveStepIndex($index);
				return true;
			}
		}
		return false;
	}
}


/**
 * TWizardStep class.
 *
 * TWizardStep represents a wizard step. The wizard owning the step
 * can be obtained by {@link getWizard Wizard}.
 * To specify the type of the step, set {@link setStepType StepType};
 * For step title, set {@link setTitle Title}. If a step can be re-visited,
 * set {@link setAllowReturn AllowReturn} to true.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardStep extends TView
{
	private $_wizard;

	/**
	 * @return TWizard the wizard owning this step
	 */
	public function getWizard()
	{
		return $this->_wizard;
	}

	/**
	 * Sets the wizard owning this step.
	 * This method is used internally by {@link TWizard}.
	 * @param TWizard the wizard owning this step
	 */
	public function setWizard($wizard)
	{
		$this->_wizard=$wizard;
	}

	/**
	 * @return string the title for this step.
	 */
	public function getTitle()
	{
		return $this->getViewState('Title','');
	}

	/**
	 * @param string the title for this step.
	 */
	public function setTitle($value)
	{
		$this->setViewState('Title',$value,'');
		if($this->_wizard)
			$this->_wizard->wizardStepsChanged();
	}

	/**
	 * @return boolean whether this step can be re-visited. Default to true.
	 */
	public function getAllowReturn()
	{
		return $this->getViewState('AllowReturn',true);
	}

	/**
	 * @param boolean whether this step can be re-visited.
	 */
	public function setAllowReturn($value)
	{
		$this->setViewState('AllowReturn',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return TWizardStepType the wizard step type. Defaults to TWizardStepType::Auto.
	 */
	public function getStepType()
	{
		return $this->getViewState('StepType',TWizardStepType::Auto);
	}

	/**
	 * @param TWizardStepType the wizard step type.
	 */
	public function setStepType($type)
	{
		$type=TPropertyValue::ensureEnum($type,'TWizardStepType');
		if($type!==$this->getStepType())
		{
			$this->setViewState('StepType',$type,TWizardStepType::Auto);
			if($this->_wizard)
				$this->_wizard->wizardStepsChanged();
		}
	}
}


/**
 * TCompleteWizardStep class.
 *
 * TCompleteWizardStep represents a wizard step of type TWizardStepType::Complete.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TCompleteWizardStep extends TWizardStep
{
	/**
	 * @return TWizardStepType the wizard step type. Always TWizardStepType::Complete.
	 */
	public function getStepType()
	{
		return TWizardStepType::Complete;
	}

	/**
	 * @param string the wizard step type.
	 * @throws TInvalidOperationException whenever this method is invoked.
	 */
	public function setStepType($value)
	{
		throw new TInvalidOperationException('completewizardstep_steptype_readonly');
	}
}


/**
 * TTemplatedWizardStep class.
 *
 * TTemplatedWizardStep represents a wizard step whose content and navigation
 * can be customized using templates. To customize the step content, specify
 * {@link setContentTemplate ContentTemplate}. To customize navigation specific
 * to the step, specify {@link setNavigationTemplate NavigationTemplate}. Note,
 * if the navigation template is not specified, default navigation will be used.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTemplatedWizardStep extends TWizardStep implements INamingContainer
{
	/**
	 * @var ITemplate the template for displaying the navigation UI of a wizard step.
	 */
	private $_navigationTemplate=null;
	/**
	 * @var ITemplate the template for displaying the content within the wizard step.
	 */
	private $_contentTemplate=null;
	/**
	 * @var TWizardNavigationContainer
	 */
	private $_navigationContainer=null;

	/**
	 * Creates child controls.
	 * This method mainly instantiates the content template, if any.
	 */
	public function createChildControls()
	{
		$this->getControls()->clear();
		if($this->_contentTemplate)
			$this->_contentTemplate->instantiateIn($this);
	}

	/**
	 * Ensures child controls are created.
	 * @param mixed event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		$this->ensureChildControls();
	}

	/**
	 * @return ITemplate the template for the content of the wizard step.
	 */
	public function getContentTemplate()
	{
		return $this->_contentTemplate;
	}

	/**
	 * @param ITemplate the template for the content of the wizard step.
	 */
	public function setContentTemplate($value)
	{
		$this->_contentTemplate=$value;
	}

	/**
	 * @return ITemplate the template for displaying the navigation UI of a wizard step. Defaults to null.
	 */
	public function getNavigationTemplate()
	{
		return $this->_navigationTemplate;
	}

	/**
	 * @param ITemplate the template for displaying the navigation UI of a wizard step.
	 */
	public function setNavigationTemplate($value)
	{
		$this->_navigationTemplate=$value;
	}

	/**
	 * @return TWizardNavigationContainer the control containing the navigation.
	 * It could be null if no navigation template is specified.
	 */
	public function getNavigationContainer()
	{
		return $this->_navigationContainer;
	}

	/**
	 * Instantiates the navigation template if any
	 */
	public function instantiateNavigationTemplate()
	{
		if(!$this->_navigationContainer && $this->_navigationTemplate)
		{
			$this->_navigationContainer=new TWizardNavigationContainer;
			$this->_navigationTemplate->instantiateIn($this->_navigationContainer);
		}
	}
}


/**
 * TWizardStepCollection class.
 *
 * TWizardStepCollection represents the collection of wizard steps owned
 * by a {@link TWizard}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardStepCollection extends TList
{
	/**
	 * @var TWizard
	 */
	private $_wizard;

	/**
	 * Constructor.
	 * @param TWizard wizard that owns this collection
	 */
	public function __construct(TWizard $wizard)
	{
		$this->_wizard=$wizard;
	}

	/**
	 * Inserts an item at the specified position.
	 * This method overrides the parent implementation by checking if
	 * the item being added is a {@link TWizardStep}.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item being added is not TWizardStep.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TWizardStep)
		{
			parent::insertAt($index,$item);
			$this->_wizard->getMultiView()->getViews()->insertAt($index,$item);
			$this->_wizard->addedWizardStep($item);
		}
		else
			throw new TInvalidDataTypeException('wizardstepcollection_wizardstep_required');
	}

	/**
	 * Removes an item at the specified position.
	 * @param integer the index of the item to be removed.
	 * @return mixed the removed item.
	 */
	public function removeAt($index)
	{
		$step=parent::removeAt($index);
		$this->_wizard->getMultiView()->getViews()->remove($step);
		$this->_wizard->removedWizardStep($step);
		return $step;
	}
}


/**
 * TWizardNavigationContainer class.
 *
 * TWizardNavigationContainer represents a control containing
 * a wizard navigation. The navigation may contain a few buttons, including
 * {@link getPreviousButton PreviousButton}, {@link getNextButton NextButton},
 * {@link getCancelButton CancelButton}, {@link getCompleteButton CompleteButton}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardNavigationContainer extends TControl implements INamingContainer
{
	private $_previousButton=null;
	private $_nextButton=null;
	private $_cancelButton=null;
	private $_completeButton=null;

	/**
	 * @return mixed the previous button
	 */
	public function getPreviousButton()
	{
		return $this->_previousButton;
	}

	/**
	 * @param mixed the previous button
	 */
	public function setPreviousButton($value)
	{
		$this->_previousButton=$value;
	}

	/**
	 * @return mixed the next button
	 */
	public function getNextButton()
	{
		return $this->_nextButton;
	}

	/**
	 * @param mixed the next button
	 */
	public function setNextButton($value)
	{
		$this->_nextButton=$value;
	}

	/**
	 * @return mixed the cancel button
	 */
	public function getCancelButton()
	{
		return $this->_cancelButton;
	}

	/**
	 * @param mixed the cancel button
	 */
	public function setCancelButton($value)
	{
		$this->_cancelButton=$value;
	}

	/**
	 * @return mixed the complete button
	 */
	public function getCompleteButton()
	{
		return $this->_completeButton;
	}

	/**
	 * @param mixed the complete button
	 */
	public function setCompleteButton($value)
	{
		$this->_completeButton=$value;
	}
}


/**
 * TWizardNavigationEventParameter class.
 *
 * TWizardNavigationEventParameter represents the parameter for
 * {@link TWizard}'s navigation events.
 *
 * The index of the currently active step can be obtained from
 * {@link getCurrentStepIndex CurrentStepIndex}, while the index
 * of the candidate new step is in {@link getNextStepIndex NextStepIndex}.
 * By modifying {@link setNextStepIndex NextStepIndex}, the new step
 * can be changed to another one. If there is anything wrong with
 * the navigation and it is not wanted, set {@link setCancelNavigation CancelNavigation}
 * to true.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardNavigationEventParameter extends TEventParameter
{
	private $_cancel=false;
	private $_currentStep;
	private $_nextStep;

	/**
	 * Constructor.
	 * @param integer current step index
	 */
	public function __construct($currentStep)
	{
		$this->_currentStep=$currentStep;
		$this->_nextStep=$currentStep;
	}

	/**
	 * @return integer the zero-based index of the currently active step.
	 */
	public function getCurrentStepIndex()
	{
		return $this->_currentStep;
	}

	/**
	 * @return integer the zero-based index of the next step. Default to {@link getCurrentStepIndex CurrentStepIndex}.
	 */
	public function getNextStepIndex()
	{
		return $this->_nextStep;
	}

	/**
	 * @param integer the zero-based index of the next step.
	 */
	public function setNextStepIndex($index)
	{
		$this->_nextStep=TPropertyValue::ensureInteger($index);
	}

	/**
	 * @return boolean whether navigation to the next step should be canceled. Default to false.
	 */
	public function getCancelNavigation()
	{
		return $this->_cancel;
	}

	/**
	 * @param boolean whether navigation to the next step should be canceled.
	 */
	public function setCancelNavigation($value)
	{
		$this->_cancel=TPropertyValue::ensureBoolean($value);
	}
}

/**
 * TWizardSideBarTemplate class.
 * TWizardSideBarTemplate is the default template for wizard sidebar.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardSideBarTemplate extends TComponent implements ITemplate
{
	/**
	 * Instantiates the template.
	 * It creates a {@link TDataList} control.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
		$dataList=new TDataList;
		$dataList->setID(TWizard::ID_SIDEBAR_LIST);
		$dataList->getSelectedItemStyle()->getFont()->setBold(true);
		$dataList->setItemTemplate(new TWizardSideBarListItemTemplate);
		$parent->getControls()->add($dataList);
	}
}

/**
 * TWizardSideBarListItemTemplate class.
 * TWizardSideBarListItemTemplate is the default template for each item in the sidebar datalist.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardSideBarListItemTemplate extends TComponent implements ITemplate
{
	/**
	 * Instantiates the template.
	 * It creates a {@link TLinkButton}.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
		$button=new TLinkButton;
		$button->setID(TWizard::ID_SIDEBAR_BUTTON);
		$parent->getControls()->add($button);
	}
}

/**
 * TWizardNavigationTemplate class.
 * TWizardNavigationTemplate is the base class for various navigation templates.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardNavigationTemplate extends TComponent implements ITemplate
{
	private $_wizard;

	/**
	 * Constructor.
	 * @param TWizard the wizard owning this template
	 */
	public function __construct($wizard)
	{
		$this->_wizard=$wizard;
	}

	/**
	 * @return TWizard the wizard owning this template
	 */
	public function getWizard()
	{
		return $this->_wizard;
	}

	/**
	 * Instantiates the template.
	 * Derived classes should override this method.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
	}

	/**
	 * Creates a navigation button.
	 * It creates a {@link TButton}, {@link TLinkButton}, or {@link TImageButton},
	 * depending on the given parameters.
	 * @param TWizardNavigationButtonStyle button style
	 * @param boolean whether the button should cause validation
	 * @param string command name for the button's OnCommand event
	 * @throws TInvalidDataValueException if the button type is not recognized
	 */
	protected function createNavigationButton($buttonStyle,$causesValidation,$commandName)
	{
		switch($buttonStyle->getButtonType())
		{
			case TWizardNavigationButtonType::Button:
				$button=new TButton;
				break;
			case TWizardNavigationButtonType::Link:
				$button=new TLinkButton;
				break;
			case TWizardNavigationButtonType::Image:
				$button=new TImageButton;
				$button->setImageUrl($buttonStyle->getImageUrl());
				break;
			default:
				throw new TInvalidDataValueException('wizard_buttontype_unknown',$buttonStyle->getButtonType());
		}
		$button->setText($buttonStyle->getButtonText());
		$button->setCausesValidation($causesValidation);
		$button->setCommandName($commandName);
		return $button;
	}
}

/**
 * TWizardStartNavigationTemplate class.
 * TWizardStartNavigationTemplate is the template used as default wizard start navigation panel.
 * It consists of two buttons, Next and Cancel.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardStartNavigationTemplate extends TWizardNavigationTemplate
{
	/**
	 * Instantiates the template.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
		$nextButton=$this->createNavigationButton($this->getWizard()->getStartNextButtonStyle(),true,TWizard::CMD_NEXT);
		$cancelButton=$this->createNavigationButton($this->getWizard()->getCancelButtonStyle(),false,TWizard::CMD_CANCEL);

		$controls=$parent->getControls();
		$controls->add($nextButton);
		$controls->add("\n");
		$controls->add($cancelButton);

		$parent->setNextButton($nextButton);
		$parent->setCancelButton($cancelButton);
	}
}

/**
 * TWizardFinishNavigationTemplate class.
 * TWizardFinishNavigationTemplate is the template used as default wizard finish navigation panel.
 * It consists of three buttons, Previous, Complete and Cancel.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardFinishNavigationTemplate extends TWizardNavigationTemplate
{
	/**
	 * Instantiates the template.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
		$previousButton=$this->createNavigationButton($this->getWizard()->getFinishPreviousButtonStyle(),false,TWizard::CMD_PREVIOUS);
		$completeButton=$this->createNavigationButton($this->getWizard()->getFinishCompleteButtonStyle(),true,TWizard::CMD_COMPLETE);
		$cancelButton=$this->createNavigationButton($this->getWizard()->getCancelButtonStyle(),false,TWizard::CMD_CANCEL);

		$controls=$parent->getControls();
		$controls->add($previousButton);
		$controls->add("\n");
		$controls->add($completeButton);
		$controls->add("\n");
		$controls->add($cancelButton);

		$parent->setPreviousButton($previousButton);
		$parent->setCompleteButton($completeButton);
		$parent->setCancelButton($cancelButton);
	}
}

/**
 * TWizardStepNavigationTemplate class.
 * TWizardStepNavigationTemplate is the template used as default wizard step navigation panel.
 * It consists of three buttons, Previous, Next and Cancel.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardStepNavigationTemplate extends TWizardNavigationTemplate
{
	/**
	 * Instantiates the template.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
		$previousButton=$this->createNavigationButton($this->getWizard()->getStepPreviousButtonStyle(),false,TWizard::CMD_PREVIOUS);
		$nextButton=$this->createNavigationButton($this->getWizard()->getStepNextButtonStyle(),true,TWizard::CMD_NEXT);
		$cancelButton=$this->createNavigationButton($this->getWizard()->getCancelButtonStyle(),false,TWizard::CMD_CANCEL);

		$controls=$parent->getControls();
		$controls->add($previousButton);
		$controls->add("\n");
		$controls->add($nextButton);
		$controls->add("\n");
		$controls->add($cancelButton);

		$parent->setPreviousButton($previousButton);
		$parent->setNextButton($nextButton);
		$parent->setCancelButton($cancelButton);
	}
}


/**
 * TWizardNavigationButtonType class.
 * TWizardNavigationButtonType defines the enumerable type for the possible types of buttons
 * that can be used in the navigation part of a {@link TWizard}.
 *
 * The following enumerable values are defined:
 * - Button: a regular click button
 * - Image: an image button
 * - Link: a hyperlink button
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TWizardNavigationButtonType extends TEnumerable
{
	const Button='Button';
	const Image='Image';
	const Link='Link';
}


/**
 * TWizardStepType class.
 * TWizardStepType defines the enumerable type for the possible types of {@link TWizard wizard} steps.
 *
 * The following enumerable values are defined:
 * - Auto: the type is automatically determined based on the location of the wizard step in the whole step collection.
 * - Complete: the step is the last summary step.
 * - Start: the step is the first step
 * - Step: the step is between the begin and the end steps.
 * - Finish: the last step before the Complete step.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizard.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TWizardStepType extends TEnumerable
{
	const Auto='Auto';
	const Complete='Complete';
	const Start='Start';
	const Step='Step';
	const Finish='Finish';
}

