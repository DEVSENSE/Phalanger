<?php
// $Id: Home.php 2474 2008-07-03 17:28:20Z mikl $
class Home extends TPage
{
    public function suggestNames($sender,$param) {
        // Get the token
        $token=$param->getToken();
        // Sender is the Suggestions repeater
        $sender->DataSource=$this->getDummyData($token);
        $sender->dataBind();                                                                                                     
    }

    public function suggestionSelected1($sender,$param) {
        $id=$sender->Suggestions->DataKeys[ $param->selectedIndex ];
        $this->Selection1->Text='Selected ID: '.$id;
    }

    public function suggestionSelected2($sender,$param) {
        $id=$sender->Suggestions->DataKeys[ $param->selectedIndex ];
        $this->Selection2->Text='Selected ID: '.$id;
    }

    public function getDummyData($token) {
        // You would look for matches to the given token here
        return array(
            array('id'=>1, 'name'=>'John'),
            array('id'=>2, 'name'=>'Paul'),
            array('id'=>3, 'name'=>'George'),
            array('id'=>4, 'name'=>'Ringo')
        );
    }
}

?>
