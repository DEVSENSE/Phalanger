[exact php]
[file]
<?
  include('Phalanger.inc');
  class A
  {
    var $x;

    function f()
    {
      $this->x->y = "Master";
      $this->x[0] = "Blaster";
      __var_dump($this);
    }
  }
?>