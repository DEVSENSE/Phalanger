<%@ Page Language="PHP" MasterPageFile="~/Default.master" Title="Your Name Here | Register"
    CodeFile="Register.aspx.php" Inherits="Register_aspx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="Main" Runat="server">

    <div class="shim column"></div>

    <div class="page" id="register">
		<div id="content">
            <h3>Request an Account</h3>
            <p>Accounts will be activated pending the approval of the Administrator.</p>
            <asp:CreateUserWizard ID="CreateUserWizard1" Runat="server" 
				ContinueDestinationPageUrl="default.aspx"
                DisableCreatedUser="True"
                EmailRegularExpression="\S+@\S+\.\S+"
                EmailRegularExpressionErrorMessage="The email format is invalid.">
            </asp:CreateUserWizard>
        </div>
    </div>

</asp:Content>