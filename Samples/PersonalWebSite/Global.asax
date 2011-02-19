<%@ Application Language="PHP" %>

<script runat="server">

	[Export]
	private static function Application_Start($sender, System:::EventArgs $e) {
		SiteMap::$SiteMapResolve->Add(new SiteMapResolveEventHandler(array("self", "AppendQueryString")));
		if (!Roles::RoleExists("Administrators")) Roles::CreateRole("Administrators");
		if (!Roles::RoleExists("Friends")) Roles::CreateRole("Friends");
	}

	private static function AppendQueryString($o, $e) {
		if (SiteMap::$CurrentNode != NULL) {
			$temp = SiteMap::$CurrentNode->Clone(true);
			$u = new Uri($e->Context->Request->Url->ToString());
			$temp->Url .= $u->Query;
			if ($temp->ParentNode != NULL) {
				$temp->ParentNode->Url .= $u->Query;
			}
			return $temp;
		} else {
			return NULL;
		}
	}
	
</script>
