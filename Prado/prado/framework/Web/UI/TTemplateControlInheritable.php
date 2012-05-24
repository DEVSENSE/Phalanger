<?php

/**
 * TTemplateControlInheritable class file.
 *
 * @author Schlaue-Kids.net <info@schlaue-kids.net>
 * @link http://www.schlaue-kids.net/
 * @copyright Copyright &copy; 2010 Schlaue-Kids.net
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Web.UI
 */

Prado::using('System.Web.UI.TTemplateControl');

/**
 * TTemplateControlInheritable class.
 * TTemplateControlInheritable is an extension to the base class for all controls that use templates.
 * By default, a control template is assumed to be in a file under the same
 * directory with the control class file. They have the same file name and
 * different extension name. For template file, the extension name is ".tpl".
 * If a TTemplateControlInheritable is inherited it uses the base class template unless the
 * inheriting control defines an own.
 *
 * @author Schlaue-Kids.net <info@schlaue-kids.net>
 * @author Kyle Caine <http://www.pradosoft.com/forum/index.php?action=profile;u=1752>
 * @version $Id$
 * @package System.Web.UI
 * @since 3.1.8
 */
class TTemplateControlInheritable extends TTemplateControl
{
	// methods

	/**
	 * Creates child controls.
	 * This method is overridden to load and instantiate control template.
	 * This method should only be used by framework and control developers.
	 * Uses the controls template if available or the base class template otherwise.
	 *
	 * @return void
	 * @throws TConfigurationException if a template control directive is invalid
	 */	
	public function createChildControls()
	{
		if(null === ($_template = $this->getTemplate())) {
			return $this->doCreateChildControlsFor(get_class($this));
		}

		foreach($_template->getDirective() as $_name => $_value) {
			if(!is_string($_value)) {
				throw new TConfigurationException('templatecontrol_directive_invalid', get_class($this), $name);
			}
			
			$this->setSubProperty($_name, $_value);
		}

		$_template->instantiateIn($this);
	}

	/**
	 * This method creates the cild controls for the given class
	 *
	 * @param string $parentClass The class to generate the child controls for
	 * @return void
	 */
	public function doCreateChildControlsFor($parentClass)
	{
		if(false !== ($_parentClass = get_parent_class($parentClass)) && 'TTemplateControl' != $_parentClass) {
			$this->doCreateChildControlsFor($_parentClass);
		}

		$this->doTemplateForClass($parentClass);
	}

	/**
	 * This method creates the template object for the given class
	 *
	 * @param string $p_class The class to create the template from
	 * @return void
	 * @throws TConfigurationException if a template control directive is invalid
	 */
	public function doTemplateForClass($parentClass)
	{
		if(null !== ($_template = $this->getService()->getTemplateManager()->getTemplateByClassName($parentClass))) {
			foreach($_template->getDirective() as $_name => $_value) {
				if(!is_string($_value)) {
					throw new TConfigurationException('templatecontrol_directive_invalid', get_class(this), $_name);
				}
				
				$this->setSubProperty($_name, $_value);
			}

			$_template->instantiateIn($this);
		}
	}
	
	// getter/setter

	/**
	 * A source template control loads its template from external storage,
	 * such as file, db, rather than from within another template.
	 *
	 * @return boolean whether the current control is a source template control
	 */
	public function getIsSourceTemplateControl()
	{
		if(null !== ($_template = $this->getTemplate())) {
			return $_template->getIsSourceTemplate();
		}

		return ($_template = $this->getService()->getTemplateManager()->getTemplateByClassName(get_parent_class($this)))
			? $_template->getIsSourceTemplate()
			: false;
	}
}