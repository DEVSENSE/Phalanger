//-------------------- ricoColor.js
if(typeof(Rico) == "undefined") Rico = {};

Rico.Color = Class.create();

Rico.Color.prototype = {

   initialize: function(red, green, blue) {
      this.rgb = { r: red, g : green, b : blue };
   },

   setRed: function(r) {
      this.rgb.r = r;
   },

   setGreen: function(g) {
      this.rgb.g = g;
   },

   setBlue: function(b) {
      this.rgb.b = b;
   },

   setHue: function(h) {

      // get an HSB model, and set the new hue...
      var hsb = this.asHSB();
      hsb.h = h;

      // convert back to RGB...
      this.rgb = Rico.Color.HSBtoRGB(hsb.h, hsb.s, hsb.b);
   },

   setSaturation: function(s) {
      // get an HSB model, and set the new hue...
      var hsb = this.asHSB();
      hsb.s = s;

      // convert back to RGB and set values...
      this.rgb = Rico.Color.HSBtoRGB(hsb.h, hsb.s, hsb.b);
   },

   setBrightness: function(b) {
      // get an HSB model, and set the new hue...
      var hsb = this.asHSB();
      hsb.b = b;

      // convert back to RGB and set values...
      this.rgb = Rico.Color.HSBtoRGB( hsb.h, hsb.s, hsb.b );
   },

   darken: function(percent) {
      var hsb  = this.asHSB();
      this.rgb = Rico.Color.HSBtoRGB(hsb.h, hsb.s, Math.max(hsb.b - percent,0));
   },

   brighten: function(percent) {
      var hsb  = this.asHSB();
      this.rgb = Rico.Color.HSBtoRGB(hsb.h, hsb.s, Math.min(hsb.b + percent,1));
   },

   blend: function(other) {
      this.rgb.r = Math.floor((this.rgb.r + other.rgb.r)/2);
      this.rgb.g = Math.floor((this.rgb.g + other.rgb.g)/2);
      this.rgb.b = Math.floor((this.rgb.b + other.rgb.b)/2);
   },

   isBright: function() {
      var hsb = this.asHSB();
      return this.asHSB().b > 0.5;
   },

   isDark: function() {
      return ! this.isBright();
   },

   asRGB: function() {
      return "rgb(" + this.rgb.r + "," + this.rgb.g + "," + this.rgb.b + ")";
   },

   asHex: function() {
      return "#" + this.rgb.r.toColorPart() + this.rgb.g.toColorPart() + this.rgb.b.toColorPart();
   },

   asHSB: function() {
      return Rico.Color.RGBtoHSB(this.rgb.r, this.rgb.g, this.rgb.b);
   },

   toString: function() {
      return this.asHex();
   }

};

Rico.Color.createFromHex = function(hexCode) {

   if ( hexCode.indexOf('#') == 0 )
      hexCode = hexCode.substring(1);

   var red = "ff", green = "ff", blue="ff";
   if(hexCode.length > 4)
	{
	   red   = hexCode.substring(0,2);
	   green = hexCode.substring(2,4);
	   blue  = hexCode.substring(4,6);
	}
	else if(hexCode.length > 0 & hexCode.length < 4)
	{
	  var r = hexCode.substring(0,1);
	  var g = hexCode.substring(1,2);
	  var b = hexCode.substring(2);
	  red = r+r;
	  green = g+g;
	  blue = b+b;
	}
   return new Rico.Color( parseInt(red,16), parseInt(green,16), parseInt(blue,16) );
};

/**
 * Factory method for creating a color from the background of
 * an HTML element.
 */
Rico.Color.createColorFromBackground = function(elem) {

   var actualColor = Element.getStyle($(elem), "background-color");
  if ( actualColor == "transparent" && elem.parent )
      return Rico.Color.createColorFromBackground(elem.parent);

   if ( actualColor == null )
      return new Rico.Color(255,255,255);

   if ( actualColor.indexOf("rgb(") == 0 ) {
      var colors = actualColor.substring(4, actualColor.length - 1 );
      var colorArray = colors.split(",");
      return new Rico.Color( parseInt( colorArray[0] ),
                            parseInt( colorArray[1] ),
                            parseInt( colorArray[2] )  );

   }
   else if ( actualColor.indexOf("#") == 0 ) {
	  return Rico.Color.createFromHex(actualColor);
   }
   else
      return new Rico.Color(255,255,255);
};

Rico.Color.HSBtoRGB = function(hue, saturation, brightness) {

   var red   = 0;
	var green = 0;
	var blue  = 0;

   if (saturation == 0) {
      red = parseInt(brightness * 255.0 + 0.5);
	   green = red;
	   blue = red;
	}
	else {
      var h = (hue - Math.floor(hue)) * 6.0;
      var f = h - Math.floor(h);
      var p = brightness * (1.0 - saturation);
      var q = brightness * (1.0 - saturation * f);
      var t = brightness * (1.0 - (saturation * (1.0 - f)));

      switch (parseInt(h)) {
         case 0:
            red   = (brightness * 255.0 + 0.5);
            green = (t * 255.0 + 0.5);
            blue  = (p * 255.0 + 0.5);
            break;
         case 1:
            red   = (q * 255.0 + 0.5);
            green = (brightness * 255.0 + 0.5);
            blue  = (p * 255.0 + 0.5);
            break;
         case 2:
            red   = (p * 255.0 + 0.5);
            green = (brightness * 255.0 + 0.5);
            blue  = (t * 255.0 + 0.5);
            break;
         case 3:
            red   = (p * 255.0 + 0.5);
            green = (q * 255.0 + 0.5);
            blue  = (brightness * 255.0 + 0.5);
            break;
         case 4:
            red   = (t * 255.0 + 0.5);
            green = (p * 255.0 + 0.5);
            blue  = (brightness * 255.0 + 0.5);
            break;
          case 5:
            red   = (brightness * 255.0 + 0.5);
            green = (p * 255.0 + 0.5);
            blue  = (q * 255.0 + 0.5);
            break;
	    }
	}

   return { r : parseInt(red), g : parseInt(green) , b : parseInt(blue) };
};

Rico.Color.RGBtoHSB = function(r, g, b) {

   var hue;
   var saturation;
   var brightness;

   var cmax = (r > g) ? r : g;
   if (b > cmax)
      cmax = b;

   var cmin = (r < g) ? r : g;
   if (b < cmin)
      cmin = b;

   brightness = cmax / 255.0;
   if (cmax != 0)
      saturation = (cmax - cmin)/cmax;
   else
      saturation = 0;

   if (saturation == 0)
      hue = 0;
   else {
      var redc   = (cmax - r)/(cmax - cmin);
    	var greenc = (cmax - g)/(cmax - cmin);
    	var bluec  = (cmax - b)/(cmax - cmin);

    	if (r == cmax)
    	   hue = bluec - greenc;
    	else if (g == cmax)
    	   hue = 2.0 + redc - bluec;
      else
    	   hue = 4.0 + greenc - redc;

    	hue = hue / 6.0;
    	if (hue < 0)
    	   hue = hue + 1.0;
   }

   return { h : hue, s : saturation, b : brightness };
};


Prado.WebUI.TColorPicker = Class.create();

Object.extend(Prado.WebUI.TColorPicker,
{
	palettes:
	{
		Small : [["fff", "fcc", "fc9", "ff9", "ffc", "9f9", "9ff", "cff", "ccf", "fcf"],
				["ccc", "f66", "f96", "ff6", "ff3", "6f9", "3ff", "6ff", "99f", "f9f"],
				["c0c0c0", "f00", "f90", "fc6", "ff0", "3f3", "6cc", "3cf", "66c", "c6c"],
				["999", "c00", "f60", "fc3", "fc0", "3c0", "0cc", "36f", "63f", "c3c"],
				["666", "900", "c60", "c93", "990", "090", "399", "33f", "60c", "939"],
				["333", "600", "930", "963", "660", "060", "366", "009", "339", "636"],
				["000", "300", "630", "633", "330", "030", "033", "006", "309", "303"]],

		Tiny : [["ffffff"/*white*/, "00ff00"/*lime*/, "008000"/*green*/, "0000ff"/*blue*/],
				["c0c0c0"/*silver*/, "ffff00"/*yellow*/, "ff00ff"/*fuchsia*/, "000080"/*navy*/],
				["808080"/*gray*/, "ff0000"/*red*/, "800080"/*purple*/, "000000"/*black*/]]
	},

	UIImages :
	{
		'button.gif' : 'button.gif',
//		'target_black.gif' : 'target_black.gif',
//		'target_white.gif' : 'target_white.gif',
		'background.png' : 'background.png'
//		'slider.gif' : 'slider.gif',
//		'hue.gif' : 'hue.gif'
	}
});

Object.extend(Prado.WebUI.TColorPicker.prototype,
{
	initialize : function(options)
	{
		var basics =
		{
			Palette : 'Small',
			ClassName : 'TColorPicker',
			Mode : 'Basic',
			OKButtonText : 'OK',
			CancelButtonText : 'Cancel',
			ShowColorPicker : true
		}

		this.element = null;
		this.showing = false;

		options = Object.extend(basics, options);
		this.options = options;
		this.input = $(options['ID']);
		this.button = $(options['ID']+'_button');
		this._buttonOnClick = this.buttonOnClick.bind(this);
		if(options['ShowColorPicker'])
			Event.observe(this.button, "click", this._buttonOnClick);
		Event.observe(this.input, "change", this.updatePicker.bind(this));
		
		Prado.Registry.set(options.ID, this);
	},

	updatePicker : function(e)
	{
		var color = Rico.Color.createFromHex(this.input.value);
		this.button.style.backgroundColor = color.toString();
	},

	buttonOnClick : function(event)
	{
		var mode = this.options['Mode'];
		if(this.element == null)
		{
			var constructor = mode == "Basic" ? "getBasicPickerContainer": "getFullPickerContainer"
			this.element = this[constructor](this.options['ID'], this.options['Palette'])
			this.input.parentNode.appendChild(this.element);
			this.element.style.display = "none";

			if(Prado.Browser().ie)
			{
				this.iePopUp = document.createElement('iframe');
				this.iePopUp.src = Prado.WebUI.TColorPicker.UIImages['button.gif'];
				this.iePopUp.style.position = "absolute"
				this.iePopUp.scrolling="no"
				this.iePopUp.frameBorder="0"
				this.input.parentNode.appendChild(this.iePopUp);
			}
			if(mode == "Full")
				this.initializeFullPicker();
		}
		this.show(mode);
	},

	show : function(type)
	{
		if(!this.showing)
		{
			var pos = this.input.positionedOffset();
			pos[1] += this.input.offsetHeight;

			this.element.style.top = (pos[1]-1) + "px";
			this.element.style.left = pos[0] + "px";
			this.element.style.display = "block";

			this.ieHack(type);

			//observe for clicks on the document body
			this._documentClickEvent = this.hideOnClick.bindEvent(this, type);
			this._documentKeyDownEvent = this.keyPressed.bindEvent(this, type);
			Event.observe(document.body, "click", this._documentClickEvent);
			Event.observe(document,"keydown", this._documentKeyDownEvent);
			this.showing = true;

			if(type == "Full")
			{
				this.observeMouseMovement();
				var color = Rico.Color.createFromHex(this.input.value);
				this.inputs.oldColor.style.backgroundColor = color.asHex();
				this.setColor(color,true);
			}
		}
	},

	hide : function(event)
	{
		if(this.showing)
		{
			if(this.iePopUp)
				this.iePopUp.style.display = "none";

			this.element.style.display = "none";
			this.showing = false;
			Event.stopObserving(document.body, "click", this._documentClickEvent);
			Event.stopObserving(document,"keydown", this._documentKeyDownEvent);

			if(this._observingMouseMove)
			{
				Event.stopObserving(document.body, "mousemove", this._onMouseMove);
				this._observingMouseMove = false;
			}
		}
	},

	keyPressed : function(event,type)
	{
		if(Event.keyCode(event) == Event.KEY_ESC)
			this.hide(event,type);
	},

	hideOnClick : function(ev)
	{
		if(!this.showing) return;
		var el = Event.element(ev);
		var within = false;
		do
		{	within = within || String(el.className).indexOf('FullColorPicker') > -1
			within = within || el == this.button;
			within = within || el == this.input;
			if(within) break;
			el = el.parentNode;
		}
		while(el);
		if(!within) this.hide(ev);
	},

	ieHack : function()
	{
		// IE hack
		if(this.iePopUp)
		{
			this.iePopUp.style.display = "block";
			this.iePopUp.style.top = (this.element.offsetTop) + "px";
			this.iePopUp.style.left = (this.element.offsetLeft)+ "px";
			this.iePopUp.style.width = Math.abs(this.element.offsetWidth)+ "px";
			this.iePopUp.style.height = (this.element.offsetHeight + 1)+ "px";
		}
	},

	getBasicPickerContainer : function(pickerID, palette)
	{
		var table = TABLE({className:'basic_colors palette_'+palette},TBODY());
		var colors = Prado.WebUI.TColorPicker.palettes[palette];
		var pickerOnClick = this.cellOnClick.bind(this);
		colors.each(function(color)
		{
			var row = document.createElement("tr");
			color.each(function(c)
			{
				var td = document.createElement("td");
				var img = IMG({src:Prado.WebUI.TColorPicker.UIImages['button.gif'],width:16,height:16});
				img.style.backgroundColor = "#"+c;
				Event.observe(img,"click", pickerOnClick);
				Event.observe(img,"mouseover", function(e)
				{
					Element.addClassName(Event.element(e), "pickerhover");
				});
				Event.observe(img,"mouseout", function(e)
				{
					Element.removeClassName(Event.element(e), "pickerhover");
				});
				td.appendChild(img);
				row.appendChild(td);
			});
			table.childNodes[0].appendChild(row);
		});
		return DIV({className:this.options['ClassName']+" BasicColorPicker",
					id:pickerID+"_picker"}, table);
	},

	cellOnClick : function(e)
	{
		var el = Event.element(e);
		if(el.tagName.toLowerCase() != "img")
			return;
		var color = Rico.Color.createColorFromBackground(el);
		this.updateColor(color);
	},

	updateColor : function(color)
	{
		this.input.value = color.toString().toUpperCase();
		this.button.style.backgroundColor = color.toString();
		if(typeof(this.onChange) == "function")
			this.onChange(color);
		if(this.options.OnColorSelected)
			this.options.OnColorSelected(this,color);
	},

	getFullPickerContainer : function(pickerID)
	{
		//create the 3 buttons
		this.buttons =
		{
			//Less   : INPUT({value:'Less Colors', className:'button', type:'button'}),
			OK	   : INPUT({value:this.options.OKButtonText, className:'button', type:'button'}),
			Cancel : INPUT({value:this.options.CancelButtonText, className:'button', type:'button'})
		};

		//create the 6 inputs
		var inputs = {};
		['H','S','V','R','G','B'].each(function(type)
		{
			inputs[type] = INPUT({type:'text',size:'3',maxlength:'3'});
		});

		//create the HEX input
		inputs['HEX'] = INPUT({className:'hex',type:'text',size:'6',maxlength:'6'});
		this.inputs = inputs;

		var images = Prado.WebUI.TColorPicker.UIImages;

		this.inputs['currentColor'] = SPAN({className:'currentColor'});
		this.inputs['oldColor'] = SPAN({className:'oldColor'});

		var inputsTable =
			TABLE({className:'inputs'}, TBODY(null,
				TR(null,
					TD({className:'currentcolor',colSpan:2},
						this.inputs['currentColor'], this.inputs['oldColor'])),

				TR(null,
					TD(null,'H:'),
					TD(null,this.inputs['H'], '??')),

				TR(null,
					TD(null,'S:'),
					TD(null,this.inputs['S'], '%')),

				TR(null,
					TD(null,'V:'),
					TD(null,this.inputs['V'], '%')),

				TR(null,
					TD({className:'gap'},'R:'),
					TD({className:'gap'},this.inputs['R'])),

				TR(null,
					TD(null,'G:'),
					TD(null, this.inputs['G'])),

				TR(null,
					TD(null,'B:'),
					TD(null, this.inputs['B'])),

				TR(null,
					TD({className:'gap'},'#'),
					TD({className:'gap'},this.inputs['HEX']))
			));

		var UIimages =
		{
			selector : SPAN({className:'selector'}),
			background : SPAN({className:'colorpanel'}),
			slider : SPAN({className:'slider'}),
			hue : SPAN({className:'strip'})
		}

		//png alpha channels for IE
		if(Prado.Browser().ie)
		{
			var filter = "filter:progid:DXImageTransform.Microsoft.AlphaImageLoader";
			UIimages['background'] = SPAN({className:'colorpanel',style:filter+"(src='"+images['background.png']+"' sizingMethod=scale);"})
		}

		this.inputs = Object.extend(this.inputs, UIimages);

		var pickerTable =
			TABLE(null,TBODY(null,
				TR({className:'selection'},
					TD({className:'colors'},UIimages['selector'],UIimages['background']),
					TD({className:'hue'},UIimages['slider'],UIimages['hue']),
					TD({className:'inputs'}, inputsTable)
				),
				TR({className:'options'},
					TD({colSpan:3},
						this.buttons['OK'],
						this.buttons['Cancel'])
				)
			));

		return DIV({className:this.options['ClassName']+" FullColorPicker",
						id:pickerID+"_picker"},pickerTable);
	},

	initializeFullPicker : function()
	{
		var color = Rico.Color.createFromHex(this.input.value);
		this.inputs.oldColor.style.backgroundColor = color.asHex();
		this.setColor(color,true);

		var i = 0;
		for(var type in this.inputs)
		{
			Event.observe(this.inputs[type], "change",
				this.onInputChanged.bindEvent(this,type));
			i++;

			if(i > 6) break;
		}

		this.isMouseDownOnColor = false;
		this.isMouseDownOnHue = false;

		this._onColorMouseDown = this.onColorMouseDown.bind(this);
		this._onHueMouseDown = this.onHueMouseDown.bind(this);
		this._onMouseUp = this.onMouseUp.bind(this);
		this._onMouseMove = this.onMouseMove.bind(this);

		Event.observe(this.inputs.background, "mousedown", this._onColorMouseDown);
		Event.observe(this.inputs.selector, "mousedown", this._onColorMouseDown);
		Event.observe(this.inputs.hue, "mousedown", this._onHueMouseDown);
		Event.observe(this.inputs.slider, "mousedown", this._onHueMouseDown);

		Event.observe(document.body, "mouseup", this._onMouseUp);

		this.observeMouseMovement();

		Event.observe(this.buttons.Cancel, "click", this.hide.bindEvent(this,this.options['Mode']));
		Event.observe(this.buttons.OK, "click", this.onOKClicked.bind(this));
	},

	observeMouseMovement : function()
	{
		if(!this._observingMouseMove)
		{
			Event.observe(document.body, "mousemove", this._onMouseMove);
			this._observingMouseMove = true;
		}
	},

	onColorMouseDown : function(ev)
	{
		this.isMouseDownOnColor = true;
		this.onMouseMove(ev);
		Event.stop(ev);
	},

	onHueMouseDown : function(ev)
	{
		this.isMouseDownOnHue = true;
		this.onMouseMove(ev);
		Event.stop(ev);
	},

	onMouseUp : function(ev)
	{
		this.isMouseDownOnColor = false;
		this.isMouseDownOnHue = false;
		Event.stop(ev);
	},

	onMouseMove : function(ev)
	{
		if(this.isMouseDownOnColor)
			this.changeSV(ev);
		if(this.isMouseDownOnHue)
			this.changeH(ev);
		Event.stop(ev);
	},

	changeSV : function(ev)
	{
		var px = Event.pointerX(ev);
		var py = Event.pointerY(ev);
		var pos = this.inputs.background.cumulativeOffset();

		var x = this.truncate(px - pos[0],0,255);
		var y = this.truncate(py - pos[1],0,255);


		var s = x/255;
		var b = (255-y)/255;

		var current_s = parseInt(this.inputs.S.value);
		var current_b = parseInt(this.inputs.V.value);

		if(current_s == parseInt(s*100) && current_b == parseInt(b*100)) return;

		var h = this.truncate(this.inputs.H.value,0,360)/360;

		var color = new Rico.Color();
		color.rgb = Rico.Color.HSBtoRGB(h,s,b);


		this.inputs.selector.style.left = x+"px";
		this.inputs.selector.style.top = y+"px";

		this.inputs.currentColor.style.backgroundColor = color.asHex();

		return this.setColor(color);
	},

	changeH : function(ev)
	{
		var py = Event.pointerY(ev);
		var pos = this.inputs.background.cumulativeOffset();
		var y = this.truncate(py - pos[1],0,255);

		var h = (255-y)/255;
		var current_h = this.truncate(this.inputs.H.value,0,360);
		current_h = current_h == 0 ? 360 : current_h;
		if(current_h == parseInt(h*360)) return;

		var s = parseInt(this.inputs.S.value)/100;
		var b = parseInt(this.inputs.V.value)/100;
		var color = new Rico.Color();
		color.rgb = Rico.Color.HSBtoRGB(h,s,b);

		var hue = new Rico.Color(color.rgb.r,color.rgb.g,color.rgb.b);
		hue.setSaturation(1); hue.setBrightness(1);

		this.inputs.background.style.backgroundColor = hue.asHex();
		this.inputs.currentColor.style.backgroundColor = color.asHex();

		this.inputs.slider.style.top = this.truncate(y,0,255)+"px";
		return this.setColor(color);

	},

	onOKClicked : function(ev)
	{
		var r = this.truncate(this.inputs.R.value,0,255);///255;
		var g = this.truncate(this.inputs.G.value,0,255);///255;
		var b = this.truncate(this.inputs.B.value,0,255);///255;
		var color = new Rico.Color(r,g,b);
		this.updateColor(color);
		this.inputs.oldColor.style.backgroundColor = color.asHex();
		this.hide(ev);
	},

	onInputChanged : function(ev, type)
	{
		if(this.isMouseDownOnColor || isMouseDownOnHue)
			return;


		switch(type)
		{
			case "H": case "S": case "V":
				var h = this.truncate(this.inputs.H.value,0,360)/360;
				var s = this.truncate(this.inputs.S.value,0,100)/100;
				var b = this.truncate(this.inputs.V.value,0,100)/100;
				var color = new Rico.Color();
				color.rgb = Rico.Color.HSBtoRGB(h,s,b);
				return this.setColor(color,true);
			case "R": case "G": case "B":
				var r = this.truncate(this.inputs.R.value,0,255);///255;
				var g = this.truncate(this.inputs.G.value,0,255);///255;
				var b = this.truncate(this.inputs.B.value,0,255);///255;
				var color = new Rico.Color(r,g,b);
				return this.setColor(color,true);
			case "HEX":
				var color = Rico.Color.createFromHex(this.inputs.HEX.value);
				return this.setColor(color,true);
		}
	},

	setColor : function(color, update)
	{
		var hsb = color.asHSB();

		this.inputs.H.value = parseInt(hsb.h*360);
		this.inputs.S.value = parseInt(hsb.s*100);
		this.inputs.V.value = parseInt(hsb.b*100);
		this.inputs.R.value = color.rgb.r;
		this.inputs.G.value = color.rgb.g;
		this.inputs.B.value = color.rgb.b;
		this.inputs.HEX.value = color.asHex().substring(1).toUpperCase();

		var images = Prado.WebUI.TColorPicker.UIImages;

		var changeCss = color.isBright() ? 'removeClassName' : 'addClassName';
		Element[changeCss](this.inputs.selector, 'target_white');

		if(update)
			this.updateSelectors(color);
	},

	updateSelectors : function(color)
	{
		var hsb = color.asHSB();
		var pos = [hsb.s*255, hsb.b*255, hsb.h*255];

		this.inputs.selector.style.left = this.truncate(pos[0],0,255)+"px";
		this.inputs.selector.style.top = this.truncate(255-pos[1],0,255)+"px";
		this.inputs.slider.style.top = this.truncate(255-pos[2],0,255)+"px";

		var hue = new Rico.Color(color.rgb.r,color.rgb.g,color.rgb.b);
		hue.setSaturation(1); hue.setBrightness(1);
		this.inputs.background.style.backgroundColor = hue.asHex();
		this.inputs.currentColor.style.backgroundColor = color.asHex();
	},

	truncate : function(value, min, max)
	{
		value = parseInt(value);
		return value < min ? min : value > max ? max : value;
	}
});
