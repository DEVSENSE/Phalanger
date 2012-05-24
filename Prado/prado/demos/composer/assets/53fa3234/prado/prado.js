/**
 * Prado base namespace
 * @namespace Prado
 */
var Prado =
{
	/**
	 * Version of Prado clientscripts
	 * @var Version
	 */
	Version: '3.1',
	
	/**
	 * Registry for Prado components
	 * @var Registry
	 */
	Registry: $H(),

	/**
	 * Returns browser information. 
	 * <pre>
	 * var browser = Prado.Browser();
	 * alert(browser.ie); //should ouput true if IE, false otherwise
	 * </pre>
	 * @function {object} ?
	 * @version 1.0
	 * @returns browserinfo
	 * @... {string} agent - Reported user agent
	 * @... {string} ver - Reported agent version
	 * @... {0|1} dom - 1 for DOM browsers 
	 * @... {0|1} ns4 - 1 for Netscape 4
	 * @... {0|1} ns6 - 1 for Netscape 6 and Firefox
	 * @... {boolean} ie3 - true for IE 3
	 * @... {0|1} ie5 - 1 for IE 5
	 * @... {0|1} ie6 - 1 for IE 6
	 * @... {0|1} ie4 - 1 for IE 4
	 * @... {0|1} ie - 1 for IE 4-6
	 * @... {0|1} hotjava - 1 for HotJava
	 * @... {0|1} ver3 - 1 for IE3 and HotJava
	 * @... {0|1} opera - 1 for Opera
	 * @... {boolean} opera7 - true for Opera 7
	 * @... {0|1} operaOld - 1 for older Opera    
	 * @... {0|1} bw - 1 for IE 4-6, Netscape 4&6, Firefox and Opera
	 * @... {boolean} mac - true for mac systems 
	 * @... {static} Version - Version of returned structure (1.0)
	 */
	Browser : function()
	{
		var info = { Version : "1.0" };
		var is_major = parseInt( navigator.appVersion );
		info.nver = is_major;
		info.ver = navigator.appVersion;
		info.agent = navigator.userAgent;
		info.dom = document.getElementById ? 1 : 0;
		info.opera = window.opera ? 1 : 0;
		info.ie5 = ( info.ver.indexOf( "MSIE 5" ) > -1 && info.dom && !info.opera ) ? 1 : 0;
		info.ie6 = ( info.ver.indexOf( "MSIE 6" ) > -1 && info.dom && !info.opera ) ? 1 : 0;
		info.ie4 = ( document.all && !info.dom && !info.opera ) ? 1 : 0;
		info.ie = info.ie4 || info.ie5 || info.ie6;
		info.mac = info.agent.indexOf( "Mac" ) > -1;
		info.ns6 = ( info.dom && parseInt( info.ver ) >= 5 ) ? 1 : 0;
		info.ie3 = ( info.ver.indexOf( "MSIE" ) && ( is_major < 4 ) );
		info.hotjava = ( info.agent.toLowerCase().indexOf( 'hotjava' ) != -1 ) ? 1 : 0;
		info.ns4 = ( document.layers && !info.dom && !info.hotjava ) ? 1 : 0;
		info.bw = ( info.ie6 || info.ie5 || info.ie4 || info.ns4 || info.ns6 || info.opera );
		info.ver3 = ( info.hotjava || info.ie3 );
		info.opera7 = ( ( info.agent.toLowerCase().indexOf( 'opera 7' ) > -1 ) || ( info.agent.toLowerCase().indexOf( 'opera/7' ) > -1 ) );
		info.operaOld = info.opera && !info.opera7;
		return info;
	},

	/**
	 * Import CSS from Url.
	 * @function ?
	 * @param doc - document DOM object
	 * @param css_file - Url to CSS file
	 */
	ImportCss : function(doc, css_file)
	{
		if (Prado.Browser().ie)
			var styleSheet = doc.createStyleSheet(css_file);
		else
		{
			var elm = doc.createElement("link");

			elm.rel = "stylesheet";
			elm.href = css_file;
			var headArr;

			if (headArr = doc.getElementsByTagName("head"))
				headArr[0].appendChild(elm);
		}
	}
};
