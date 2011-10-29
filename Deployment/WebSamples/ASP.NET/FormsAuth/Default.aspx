<%@ Page Language="PHP" AutoEventWireup="false" CodeFile="Default.aspx.php" Inherits="_Default" Title="Secret page" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Secret page</title>
</head>
<body style="font-family: Arial, Helvetica, sans-serif">
    <form id="form1" runat="server">
        <center>
            <table border="0" cellpadding="0" cellspacing="5">
                <tr>
                    <td style="text-align: center">
                	    You are currently logged in as <b><%= \System\Web\HttpContext::$Current->User->Identity->Name %></b>.
                    </td>
                </tr>
                <tr>
                    <td>
                        This page is protected by 
                        <a href="http://msdn.microsoft.com/library/en-us/dnpag2/html/PAGExplained0002.asp?frame=true">Forms Authentication</a>.
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center">
                        <asp:Button ID="ButtonLogout" runat="server" Text="Logout" OnClick="ButtonLogout_Click" />
                    </td>
                </tr>
            </table>
        </center>
    </form>
</body>
</html>
