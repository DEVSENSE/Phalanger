[clr]
[expect exact]
Works!
DONE
[file]
<?php

class Handler
{
    function DoWork()
    {
        echo "Works!\n";
    }
}

function test()
{
    $bw = new \System\ComponentModel\BackgroundWorker;
    $handler = new Handler;

    $bw->DoWork->Add( new \System\ComponentModel\DoWorkEventHandler( array($handler, "DoWork") ) );
    $bw->RunWorkerAsync();

    while ($bw->IsBusy)
    {
        \System\Threading\Thread::Sleep(100);
    }
}

test();

echo "DONE"

?>