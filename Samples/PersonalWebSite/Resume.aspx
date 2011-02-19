<%@ Page Language="PHP" MasterPageFile="~/Default.master" Title="Your Name Here | Resume"
    CodeFile="Resume.aspx.php" Inherits="Resume_aspx" %>

<asp:content id="Content1" contentplaceholderid="Main" runat="server">

    <div class="shim column"></div>
    
    <div class="page" id="resume">
        <div id="content" class="resume">
			<table border="0" cellpadding="0" cellspacing="0" class="photo-frame" id="photo">
				<tr>
					<td class="topx--"></td>
					<td class="top-x-"></td>
					<td class="top--x"></td>
				</tr>
				<tr>
					<td class="midx--"></td>
					<td><img src="images/resume-photo.jpg" class="photo_198" style="border:4px solid white" alt="Resume Photo"/></td>
					<td class="mid--x"></td>
				</tr>
				<tr>
					<td class="botx--"></td>
					<td class="bot-x-"></td>
					<td class="bot--x"></td>
				</tr>
			</table>
			<h3>Your Name Here </h3>
			<p>resume 1/23/04</p>
			<p>555-555-1212 fax<br />
			555-555-1212 voice<br />
			someone@example.com<br />
			www.example.com<br />
			City, State &nbsp;Country</p>
			<p><a href="#"><asp:image id="downloadresume" runat="Server" AlternateText="download resume in word format" skinid="dwn_res" /></a></p>
			<h4>Objective</h4>
			<p class="first">Lorem ipsum dolor sit amet, consectetuer adipiscing elit.</p>
			<h4>Experience</h4>
			<p class="first">1999 - 2004&nbsp; Lorem ipsum dolor sit amet, consectetuer adipiscing elit.<br />
				Sed diam nonummy nibh euismod </p>
			<ul>
				<li>Ttincidunt ut laoreet dolore magna aliquam erat volutpat. </li>
				<li>Ut wisi enim ad minim veniam, quis nostrud exercitation consequat. </li>
				<li>Duis autem veleum iriure dolor in hendrerit in vel willum.</li>
			</ul>
			<p>1995 - 1999 &nbsp; Lorem ipsum dolor sit amet, consectetuer adipiscing elit.<br />
				Sed diam nonummy nibh euismod </p>
			<ul>
				<li>Ttincidunt ut laoreet dolore magna aliquam erat volutpat. </li>
				<li>Ut wisi enim ad minim veniam, quis nostrud exercitation consequat. </li>
				<li>Duis autem veleum iriure dolor in hendrerit in vel willum.</li>
			</ul>
			<p>1993 - 1995 &nbsp; Lorem ipsum dolor sit amet, consectetuer adipiscing elit.<br />
				Sed diam nonummy nibh euismod </p>
			<ul>
				<li>Ttincidunt ut laoreet dolore magna aliquam erat volutpat. </li>
				<li>Ut wisi enim ad minim veniam, quis nostrud exercitation consequat. </li>
				<li>Duis autem veleum iriure dolor in hendrerit in vel willum.</li>
			</ul>
			<p>1987 - 1993 &nbsp; Lorem ipsum dolor sit amet, consectetuer adipiscing elit.<br />
				Sed diam nonummy nibh euismod </p>
			<ul>
				<li>Ttincidunt ut laoreet dolore magna aliquam erat volutpat. </li>
				<li>Ut wisi enim ad minim veniam, quis nostrud exercitation consequat. </li>
				<li>Duis autem veleum iriure dolor in hendrerit in vel willum.</li>
			</ul>
			<h4>Education</h4>
			<p class="first">1984 - 1987 &nbsp; Lorem ipsum dolor sit amet, consectetuer adipiscing elit.<br />
			Sed diam nonummy nibh euismod </p>
			<ul>
				<li>Ttincidunt ut laoreet dolore magna aliquam erat volutpat. </li>
				<li>Ut wisi enim ad minim veniam, quis nostrud exercitation consequat.</li>
			</ul>
			<p>Lorem ipsum dolor sit amet, consectetuer adipiscing elit.</p>
	        
			</div>
        
    </div>
      
</asp:content>
