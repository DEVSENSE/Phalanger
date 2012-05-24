This directory contains templates used to display PRADO exception and error messages to end-users.

All error template files follow the following naming convention:

    error<status code>-<language code>.html

where <status code> refers to a HTTP status code used when raising THttpException, and
<language code> refers to a valid language such as en, zh, fr.

The naming convention for exception template files is similar to that of error template files.


CAUTION: When saving a template file, please make sure the file is saved using UTF-8 encoding.
On Windows, you may use Notepad.exe to accomplish such saving.


Qiang Xue
Jan. 3, 2006