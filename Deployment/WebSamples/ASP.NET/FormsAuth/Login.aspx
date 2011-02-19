<%@ Page Language="PHP" AutoEventWireup="false" CodeFile="Login.aspx.php" Inherits="Login" Title="Login page" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Login page</title>
</head>
<body style="font-family: Arial, Helvetica, sans-serif">
    <form id="form1" runat="server">
        <center>
            <table border="0" cellpadding="0" cellspacing="5">
                <tr>
                    <td colspan="2" style="text-align: center">
                	    <b>Please enter your login name and password:</b>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: right">
                        Login:
                    </td>
                    <td>
                        <asp:TextBox ID="TextBoxLogin" runat="server" Width="200px" />
                    </td>
                </tr>
                <tr>
                    <td style="text-align: right">
                        Password:
                    </td>
                    <td>
                        <asp:TextBox ID="TextBoxPassword" runat="server" Width="200px" TextMode="Password" />
                    </td>
                </tr>
                <tr>
                    <td colspan="2" style="text-align: center">
                        <asp:Button ID="ButtonSubmit" runat="server" Text="Submit" OnClick="ButtonSubmit_Click" />
                    </td>
                </tr>
                <tr>
                    <td colspan="2">
                        <asp:CustomValidator ID="CustomLoginValidator" runat="server" Display="Dynamic"
                        ErrorMessage="Invalid login or password! Please try again." Font-Bold="True" />
                    </td>
                </tr>
            </table>
        </center>
    </form>
</body>
</html>
