<%@ Page Language="PHP" MasterPageFile="~/Default.master" Title="Your Name Here | Albums"
    CodeFile="Albums.aspx.php" Inherits="Albums_aspx" %>

<asp:content id="Content1" contentplaceholderid="Main" runat="server">

    <div class="shim gradient"></div>

    <div class="page" id="albums">

        <h3>Welcome to My Photo Galleries</h3>
        <p>Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod 
        tincidunt ut laoreet dolore magna aliquam erat volutpat. Ut wisi enim ad minim veniam, quis 
        nostrud exercitation consequat. esse molestie consequat, vel willum.</p>
        <hr />
        <asp:DataList ID="DataList1" runat="Server"  dataSourceID="ObjectDataSource1" cssclass="view"
            repeatColumns="2" repeatdirection="Horizontal" borderwidth="0" cellpadding="0" cellspacing="0">
            <ItemStyle cssClass="item" />
            <ItemTemplate>
                <table border="0" cellpadding="0" cellspacing="0" class="album-frame">
                    <tr>
                        <td class="topx----"><asp:image runat="Server" id="b01" skinid="b01" /></td>
                        <td class="top-x---"><asp:image runat="Server" id="b02" skinid="b02" /></td>
                        <td class="top--x--"></td>
                        <td class="top---x-"><asp:image runat="Server" id="b03" skinid="b03" /></td>
                        <td class="top----x"><asp:image runat="Server" id="b04" skinid="b04" /></td>
                    </tr>
                    <tr>
                        <td class="mtpx----"><asp:image runat="Server" id="b05" skinid="b05" /></td>
                        <td colspan="3" rowspan="3"><a href='Photos.aspx?AlbumID=<%# $this->Eval("AlbumID") %>' ><img src="Handler.ashx?AlbumID=<%# $this->Eval("AlbumID") %>&Size=M" class="photo_198" style="border:4px solid white" alt='Sample Photo from Album Number <%# $this->Eval("AlbumID") %>' /></a></td>
                        <td class="mtp----x"><asp:image runat="Server" id="b06" skinid="b06" /></td>
                    </tr>
                    <tr>
                        <td class="midx----"></td>
                        <td class="mid----x"></td>
                    </tr>
                    <tr>
                        <td class="mbtx----"><asp:image runat="Server" id="b07" skinid="b07" /></td>
                        <td class="mbt----x"><asp:image runat="Server" id="b08" skinid="b08" /></td>
                    </tr>
                    <tr>
                        <td class="botx----"><asp:image runat="Server" id="b09" skinid="b09" /></td>
                        <td class="bot-x---"><asp:image runat="Server" id="b10" skinid="b10" /></td>
                        <td class="bot--x--"></td>
                        <td class="bot---x-"><asp:image runat="Server" id="b11" skinid="b11" /></td>
                        <td class="bot----x"><asp:image runat="Server" id="b12" skinid="b12" /></td>
                    </tr>
                </table>
				<h4><a href="Photos.aspx?AlbumID=<%# $this->Eval("AlbumID") %>"><%# $this->Server->HtmlEncode($this->Eval("Caption")) %></a></h4>
				<%# $this->Eval("Count") %> Photo(s)
            </ItemTemplate>
        </asp:DataList>
    
    </div>

	<asp:ObjectDataSource ID="ObjectDataSource1" Runat="server" TypeName="PhotoManager" 
		SelectMethod="GetAlbums">
	</asp:ObjectDataSource>
    

</asp:content>