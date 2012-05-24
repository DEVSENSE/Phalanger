<?php
/**
 * ConfigMan class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: ConfigMan.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * ConfigMan class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class ConfigMan extends BlogPage
{
	const CONFIG_FILE='Application.Data.Settings';

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$parameters=$this->Application->Parameters;
			$this->SiteTitle->Text=$parameters['SiteTitle'];
			$this->SiteSubtitle->Text=$parameters['SiteSubtitle'];
			$this->SiteOwner->Text=$parameters['SiteOwner'];
			$this->AdminEmail->Text=$parameters['AdminEmail'];
			$this->MultipleUser->Checked=TPropertyValue::ensureBoolean($parameters['MultipleUser']);
			$this->AccountApproval->Checked=TPropertyValue::ensureBoolean($parameters['AccountApproval']);
			$this->PostPerPage->Text=$parameters['PostPerPage'];
			$this->RecentComments->Text=$parameters['RecentComments'];
			$this->PostApproval->Checked=TPropertyValue::ensureBoolean($parameters['PostApproval']);
			$themes=$this->Service->ThemeManager->AvailableThemes;
			$this->ThemeName->DataSource=$themes;
			$this->ThemeName->dataBind();
			$this->ThemeName->SelectedValue=array_search($parameters['ThemeName'],$themes);
		}
	}

	public function saveButtonClicked($sender,$param)
	{
		$dom=new TXmlDocument;
		$dom->Encoding='utf-8';
		$dom->TagName='parameters';
		$elements=$dom->Elements;
		$elements[]=$this->createParameter('SiteTitle',$this->SiteTitle->Text);
		$elements[]=$this->createParameter('SiteSubtitle',$this->SiteSubtitle->Text);
		$elements[]=$this->createParameter('SiteOwner',$this->SiteOwner->Text);
		$elements[]=$this->createParameter('AdminEmail',$this->AdminEmail->Text);
		$elements[]=$this->createParameter('MultipleUser',$this->MultipleUser->Checked);
		$elements[]=$this->createParameter('AccountApproval',$this->AccountApproval->Checked);
		$elements[]=$this->createParameter('PostPerPage',$this->PostPerPage->Text);
		$elements[]=$this->createParameter('RecentComments',$this->RecentComments->Text);
		$elements[]=$this->createParameter('PostApproval',$this->PostApproval->Checked);
		$themeName=$this->ThemeName->SelectedItem->Text;
		$elements[]=$this->createParameter('ThemeName',$themeName);
		$dom->saveToFile(Prado::getPathOfNamespace(self::CONFIG_FILE,'.xml'));
		if($themeName!==$this->Theme->Name)
			$this->Response->reload();
	}

	private function createParameter($id,$value)
	{
		$element=new TXmlElement('parameter');
		$element->Attributes['id']=$id;
		$element->Attributes['value']=TPropertyValue::ensureString($value);
		return $element;
	}
}

?>