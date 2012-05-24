Prado.WebUI.TKeyboard = Class.create();
Prado.WebUI.TKeyboard.prototype =
{
	initialize : function(options)
	{
		this.element = $(options.ID);
		this.onInit(options);
		Prado.Registry.set(options.ID, this);
	},

	onInit : function(options)
    {
		this.cssClass = options['CssClass'];
        this.forControl = document.getElementById(options['ForControl']);
        this.autoHide = options['AutoHide'];

        this.flagShift = false;
        this.flagCaps = false;
        this.flagHover = false;
        this.flagFocus = false;

        this.keys = new Array
        (
            new Array('` ~ D', '1 ! D', '2 @ D', '3 # D', '4 $ D', '5 % D', '6 ^ D', '7 &amp; D', '8 * D', '9 ( D', '0 ) D', '- _ D', '= + D', 'Bksp Bksp Bksp'),
            new Array('Del Del Del', 'q Q L', 'w W L', 'e E L', 'r R L', 't T L', 'y Y L', 'u U L', 'i I L', 'o O L', 'p P L', '[ { D', '] } D', '\\ | \\'),
            new Array('Caps Caps Caps', 'a A L', 's S L', 'd D L', 'f F L', 'g G L', 'h H L', 'j J L', 'k K L', 'l L L', '; : D', '\' " D', 'Exit Exit Exit'),
            new Array('Shift Shift Shift', 'z Z L', 'x X L', 'c C L', 'v V L', 'b B L', 'n N L', 'm M L', ', &lt; D', '. &gt; D', '/ ? D', 'Shift Shift Shift')
        );

        if (this.isObject(this.forControl))
        {
            this.forControl.keyboard = this;
            this.forControl.onfocus = function() {this.keyboard.show(); };
            this.forControl.onblur = function() {if (this.keyboard.flagHover == false) this.keyboard.hide();};
            this.forControl.onkeydown = function(e) {if (!e) e = window.event; var key = (e.keyCode)?e.keyCode:e.which; if(key == 9)  this.keyboard.hide();;};
            this.forControl.onselect = this.saveSelection;
            this.forControl.onclick = this.saveSelection;
            this.forControl.onkeyup = this.saveSelection;
        }

        this.render();

        this.tagKeyboard.onmouseover = function() {this.keyboard.flagHover = true;};
        this.tagKeyboard.onmouseout = function() {this.keyboard.flagHover = false;};

        if (!this.autoHide) this.show();
    },

	isObject : function(a)
	{
		return (typeof a == 'object' && !!a) || typeof a == 'function';
	},

	createElement : function(tagName, attributes, parent)
    {
        var tagElement = document.createElement(tagName);
        if (this.isObject(attributes)) for (attribute in attributes) tagElement[attribute] = attributes[attribute];
        if (this.isObject(parent)) parent.appendChild(tagElement);
        return tagElement;
    },

	onmouseover : function()
	{
		this.className += ' Hover';
	},

	onmouseout : function()
	{
		this.className = this.className.replace(/( Hover| Active)/ig, '');
	},

    onmousedown : function()
    {
    	this.className += ' Active';
	},

    onmouseup : function()
    {
    	this.className = this.className.replace(/( Active)/ig, '');
    	this.keyboard.type(this.innerHTML);
	},

	render : function()
    {
        this.tagKeyboard = this.createElement('div', {className: this.cssClass, onselectstart: function() {return false;}}, this.element);
        this.tagKeyboard.keyboard = this;

        for (var line = 0; line < this.keys.length; line++)
        {
            var tagLine = this.createElement('div', {className: 'Line'}, this.tagKeyboard);
            for (var key = 0; key < this.keys[line].length; key++)
            {
                var split = this.keys[line][key].split(' ');
                var tagKey = this.createElement('div', {className: 'Key ' + split[2]}, tagLine);
                var tagKey1 = this.createElement('div', {className: 'Key1', innerHTML: split[0], keyboard: this, onmouseover: this.onmouseover, onmouseout: this.onmouseout, onmousedown: this.onmousedown, onmouseup: this.onmouseup}, tagKey);
                var tagKey2 = this.createElement('div', {className: 'Key2', innerHTML: split[1], keyboard: this, onmouseover: this.onmouseover, onmouseout: this.onmouseout, onmousedown: this.onmousedown, onmouseup: this.onmouseup}, tagKey);
            }
        }
    },

    isShown : function()
    {
        return (this.tagKeyboard.style.visibility.toLowerCase() == 'visible');
    },

    show : function()
    {
        if (this.isShown() == false) this.tagKeyboard.style.visibility = 'visible';
    },

    hide : function()
    {
        if (this.isShown() == true && this.autoHide) {this.tagKeyboard.style.visibility = 'hidden'; }
    },

    type : function(key)
    {
        var input = this.forControl;
        var command = key.toLowerCase();

        if (command == 'exit') {this.hide();}
        else if (input != 'undefined' && input != null && command == 'bksp') {this.insert(input, 'bksp');}
        else if (input != 'undefined' && input != null && command == 'del') {this.insert(input, 'del');}
        else if (command == 'shift') {this.tagKeyboard.className = this.flagShift?'Keyboard Off':'Keyboard Shift';this.flagShift = this.flagShift?false:true;}
        else if (command == 'caps') {this.tagKeyboard.className = this.caps?'Keyboard Off':'Keyboard Caps';this.caps = this.caps?false:true;}
        else if (input != 'undefined' && input != null)
        {
            if (this.flagShift == true) {this.flagShift = false; this.tagKeyboard.className = 'Keyboard Off';}
            key = key.replace(/&gt;/, '>'); key = key.replace(/&lt;/, '<'); key = key.replace(/&amp;/, '&');
            this.insert(input, key);
        }

        if (command != 'exit') input.focus();
    },

    saveSelection : function()
    {
        if (this.keyboard.forControl.createTextRange)
        {
            this.keyboard.selection = document.selection.createRange().duplicate();
            return;
        }
    },

    insert : function(field, value)
    {
        if (this.forControl.createTextRange && this.selection)
        {
            if (value == 'bksp') {this.selection.moveStart("character", -1); this.selection.text = '';}
            else if (value == 'del') {this.selection.moveEnd("character", 1); this.selection.text = '';}
            else {this.selection.text = value;}
            this.selection.select();
        }
        else
        {
            var selectStart = this.forControl.selectionStart;
            var selectEnd = this.forControl.selectionEnd;
            var start = (this.forControl.value).substring(0, selectStart);
            var end = (this.forControl.value).substring(selectEnd, this.forControl.textLength);

            if (value == 'bksp') {start = start.substring(0, start.length - 1); selectStart -= 1; value = '';}
            if (value == 'del') {end = end.substring(1, end.length); value = '';}

            this.forControl.value = start + value + end;
            this.forControl.selectionStart = selectEnd + value.length;
            this.forControl.selectionEnd = selectStart + value.length;
        }
    }
};
