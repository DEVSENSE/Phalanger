<?php

class Person
{
    public $ID = -1;
    public $FirstName = '';
    public $LastName = '';

    public $WeightInKilograms = 0.0;
    public $HeightInMeters = 0.0;

    private $_birthDate = '';

    //setters and getter for BirthDate
    public function getBirthDate()
    {
        return $this->_birthDate;
    }

    public function setBirthDate($value)
    {
        $this->_birthDate = $value;
    }
}

?>