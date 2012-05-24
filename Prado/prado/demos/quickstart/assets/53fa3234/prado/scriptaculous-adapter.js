/**
 * Utilities and extions to Prototype/Scriptaculous
 * @file scriptaculous-adapter.js
 */
 
/**
 * Extension to 
 * <a href="http://www.prototypejs.org/api/function" target="_blank">Prototype's Function</a>
 * @namespace Function
 */
/**
 * Similar to bindAsEventLister, but takes additional arguments.
 * @function Function.bindEvent
 */
Function.prototype.bindEvent = function()
{
	var __method = this, args = $A(arguments), object = args.shift();
	return function(event)
	{
		return __method.apply(object, [event || window.event].concat(args));
	}
};

/**
 * Extension to 
 * <a href="http://www.prototypejs.org/api/class" target="_blank">Prototype's Class</a>
 * @namespace Class
 */
 
/**
 * Creates a new class by copying class definition from
 * the <tt>base</tt> and optional <tt>definition</tt>.
 * @function {Class} Class.extend
 * @param {function} base - Base class to copy from.
 * @param {optional Array} - Additional definition 
 * @returns Constructor for the extended class
 */
Class.extend = function(base, definition)
{
		var component = Class.create();
		Object.extend(component.prototype, base.prototype);
		if(definition)
			Object.extend(component.prototype, definition);
		return component;
};

/**
 * Base, version 1.0.2
 * Copyright 2006, Dean Edwards
 * License: http://creativecommons.org/licenses/LGPL/2.1/
 * @class Base
 */
var Base = function() {
	if (arguments.length) {
		if (this == window) { // cast an object to this class
			Base.prototype.extend.call(arguments[0], arguments.callee.prototype);
		} else {
			this.extend(arguments[0]);
		}
	}
};

Base.version = "1.0.2";

Base.prototype = {
	extend: function(source, value) {
		var extend = Base.prototype.extend;
		if (arguments.length == 2) {
			var ancestor = this[source];
			// overriding?
			if ((ancestor instanceof Function) && (value instanceof Function) &&
				ancestor.valueOf() != value.valueOf() && /\bbase\b/.test(value)) {
				var method = value;
			//	var _prototype = this.constructor.prototype;
			//	var fromPrototype = !Base._prototyping && _prototype[source] == ancestor;
				value = function() {
					var previous = this.base;
				//	this.base = fromPrototype ? _prototype[source] : ancestor;
					this.base = ancestor;
					var returnValue = method.apply(this, arguments);
					this.base = previous;
					return returnValue;
				};
				// point to the underlying method
				value.valueOf = function() {
					return method;
				};
				value.toString = function() {
					return String(method);
				};
			}
			return this[source] = value;
		} else if (source) {
			var _prototype = {toSource: null};
			// do the "toString" and other methods manually
			var _protected = ["toString", "valueOf"];
			// if we are prototyping then include the constructor
			if (Base._prototyping) _protected[2] = "constructor";
			var name;
			for (var i = 0; (name = _protected[i]); i++) {
				if (source[name] != _prototype[name]) {
					extend.call(this, name, source[name]);
				}
			}
			// copy each of the source object's properties to this object
			for (var name in source) {
				if (!_prototype[name]) {
					extend.call(this, name, source[name]);
				}
			}
		}
		return this;
	},

	base: function() {
		// call this method from any other method to invoke that method's ancestor
	}
};

Base.extend = function(_instance, _static) {
	var extend = Base.prototype.extend;
	if (!_instance) _instance = {};
	// build the prototype
	Base._prototyping = true;
	var _prototype = new this;
	extend.call(_prototype, _instance);
	var constructor = _prototype.constructor;
	_prototype.constructor = this;
	delete Base._prototyping;
	// create the wrapper for the constructor function
	var klass = function() {
		if (!Base._prototyping) constructor.apply(this, arguments);
		this.constructor = klass;
	};
	klass.prototype = _prototype;
	// build the class interface
	klass.extend = this.extend;
	klass.implement = this.implement;
	klass.toString = function() {
		return String(constructor);
	};
	extend.call(klass, _static);
	// single instance
	var object = constructor ? klass : _prototype;
	// class initialisation
	if (object.init instanceof Function) object.init();
	return object;
};

Base.implement = function(_interface) {
	if (_interface instanceof Function) _interface = _interface.prototype;
	this.prototype.extend(_interface);
};

/**
 * Performs a PostBack using javascript.
 * @function Prado.PostBack
 * @param event - Event that triggered this postback
 * @param options - Postback options
 * @... {string} FormID - Form that should be posted back
 * @... {optional boolean} CausesValidation - Validate before PostBack if true
 * @... {optional string} ValidationGroup - Group to Validate 
 * @... {optional string} ID - Validation ID 
 * @... {optional string} PostBackUrl - Postback URL
 * @... {optional boolean} TrackFocus - Keep track of focused element if true
 * @... {string} EventTarget - Id of element that triggered PostBack
 * @... {string} EventParameter - EventParameter for PostBack
 */
Prado.PostBack = function(event,options)
{
	var form = $(options['FormID']);
	var canSubmit = true;

	if(options['CausesValidation'] && typeof(Prado.Validation) != "undefined")
	{
		if(!Prado.Validation.validate(options['FormID'], options['ValidationGroup'], $(options['ID'])))
			return Event.stop(event);
	}

	if(options['PostBackUrl'] && options['PostBackUrl'].length > 0)
		form.action = options['PostBackUrl'];

	if(options['TrackFocus'])
	{
		var lastFocus = $('PRADO_LASTFOCUS');
		if(lastFocus)
		{
			var active = document.activeElement; //where did this come from
			if(active)
				lastFocus.value = active.id;
			else
				lastFocus.value = options['EventTarget'];
		}
	}

	$('PRADO_POSTBACK_TARGET').value = options['EventTarget'];
	$('PRADO_POSTBACK_PARAMETER').value = options['EventParameter'];
	/**
	 * Since google toolbar prevents browser default action,
	 * we will always disable default client-side browser action
	 */
	/*if(options['StopEvent']) */
		Event.stop(event);
	Event.fireEvent(form,"submit");
};

/**
 * Prado utilities to manipulate DOM elements.
 * @object Prado.Element
 */
Prado.Element =
{
	/**
	 * Set the value of a particular element.
	 * @function ?
	 * @param {string} element - Element id
	 * @param {string} value - New element value
	 */
	setValue : function(element, value)
	{
		var el = $(element);
		if(el && typeof(el.value) != "undefined")
			el.value = value;
	},

	/**
	 * Select options from a selectable element.
	 * @function ?
	 * @param {string} element - Element id
	 * @param {string} method - Name of any {@link Prado.Element.Selection} method
	 * @param {array|boolean|string} value - Values that should be selected
	 * @param {int} total - Number of elements 
	 */
	select : function(element, method, value, total)
	{
		var el = $(element);
		if(!el) return;
		var selection = Prado.Element.Selection;
		if(typeof(selection[method]) == "function")
		{
			var control = selection.isSelectable(el) ? [el] : selection.getListElements(element,total);
			selection[method](control, value);
		}
	},

	/**
	 * Trigger a click event on a DOM element.
	 * @function ?
	 * @param {string} element - Element id
	 */
	click : function(element)
	{
		var el = $(element);
		if(el)
			Event.fireEvent(el,'click');
	},

	/**
	 * Check if an DOM element is disabled.
	 * @function {boolean} ?
	 * @param {string} element - Element id
	 * @returns true if element is disabled
	 */
	isDisabled : function(element)
	{
		if(!element.attributes['disabled']) //FF
			return false;
		var value = element.attributes['disabled'].nodeValue;
		if(typeof(value)=="string")
			return value.toLowerCase() == "disabled";
		else
			return value == true;
	},

	/**
	 * Sets an attribute of a DOM element.
	 * @function ?
	 * @param {string} element - Element id
	 * @param {string} attribute - Name of attribute
	 * @param {string} value - Value of attribute
	 */
	setAttribute : function(element, attribute, value)
	{
		var el = $(element);
		if(!el) return;
		if((attribute == "disabled" || attribute == "multiple") && value==false)
			el.removeAttribute(attribute);
		else if(attribute.match(/^on/i)) //event methods
		{
			try
			{
				eval("(func = function(event){"+value+"})");
				el[attribute] = func;
			}
			catch(e)
			{
				throw "Error in evaluating '"+value+"' for attribute "+attribute+" for element "+element.id;
			}
		}
		else
			el.setAttribute(attribute, value);
	},

	/**
	 * Sets the options for a select element. 
	 * @function ?
	 * @param {string} element - Element id
	 * @param {array[]} options - Array of options, each an array of structure 
	 *   [ "optionText" , "optionValue" , "optionGroup" ]
	 */
	setOptions : function(element, options)
	{
		var el = $(element);
		if(!el) return;
		var previousGroup = null;
		var optGroup=null;
		if(el && el.tagName.toLowerCase() == "select")
		{
			while(el.childNodes.length > 0)
				el.removeChild(el.lastChild);

			var optDom = Prado.Element.createOptions(options);
			for(var i = 0; i < optDom.length; i++)
				el.appendChild(optDom[i]);
		}
	},

	/**
	 * Create opt-group options from an array of options. 
	 * @function {array} ?
	 * @param {array[]} options - Array of options, each an array of structure 
	 *   [ "optionText" , "optionValue" , "optionGroup" ]
	 * @returns Array of option DOM elements
	 */
	createOptions : function(options)
	{
		var previousGroup = null;
		var optgroup=null;
		var result = [];
		for(var i = 0; i<options.length; i++)
		{
			var option = options[i];
			if(option.length > 2)
			{
				var group = option[2];
				if(group!=previousGroup)
				{
					if(previousGroup!=null && optgroup!=null)
					{
						result.push(optgroup);
						previousGroup=null;
						optgroup=null;
					}
					optgroup = document.createElement('optgroup');
					optgroup.label = group;
					previousGroup = group;
				}
			}
			var opt = document.createElement('option');
			opt.text = option[0];
			opt.innerText = option[0];
			opt.value = option[1];
			if(optgroup!=null)
				optgroup.appendChild(opt);
			else
				result.push(opt);
		}
		if(optgroup!=null)
			result.push(optgroup);
		return result;
	},

	/**
	 * Set focus (delayed) on a particular element.
	 * @function ?
	 * @param {string} element - Element id
	 */
	focus : function(element)
	{
		var obj = $(element);
		if(typeof(obj) != "undefined" && typeof(obj.focus) != "undefined")
			setTimeout(function(){ obj.focus(); }, 100);
		return false;
	},

	/**
	 * Replace a DOM element either with given content or
	 * with content from a CallBack response boundary
	 * using a replacement method.
	 * @function ?
	 * @param {string|element} element - DOM element or element id
	 * @param {string} method - Name of method to use for replacement
	 * @param {optional string} content - New content of element
	 * @param {optional string} boundary - Boundary of new content
	 */
	replace : function(element, method, content, boundary)
	{
		if(boundary)
		{
			var result = Prado.Element.extractContent(this.transport.responseText, boundary);
			if(result != null)
				content = result;
		}
		if(typeof(element) == "string")
		{
			if($(element))
				method.toFunction().apply(this,[element,""+content]);
		}
		else
		{
			method.toFunction().apply(this,[""+content]);
		}
	},

	/**
	 * Extract content from a text by its boundary id.
	 * Boundaries have this form:
	 * <pre>
	 * &lt;!--123456--&gt;Democontent&lt;!--//123456--&gt;
	 * </pre>
	 * @function {string} ?
	 * @param {string} text - Text that contains boundaries
	 * @param {string} boundary - Boundary id
	 * @returns Content from given boundaries
	 */
	extractContent : function(text, boundary)
	{
		var tagStart = '<!--'+boundary+'-->';
		var tagEnd = '<!--//'+boundary+'-->';
		var start = text.indexOf(tagStart);
		if(start > -1)
		{
			start += tagStart.length;
			var end = text.indexOf(tagEnd,start);
			if(end > -1)
				return text.substring(start,end);
		}
		return null;
		/*var f = RegExp('(?:<!--'+boundary+'-->)((?:.|\n|\r)+?)(?:<!--//'+boundary+'-->)',"m");
		var result = text.match(f);
		if(result && result.length >= 2)
			return result[1];
		else
			return null;*/
	},

	/**
	 * Evaluate a javascript snippet from a string.
	 * @function ?
	 * @param {string} content - String containing the script
	 */
	evaluateScript : function(content)
	{
		content.evalScripts();
	},
	
	/**
	 * Set CSS style with Camelized keys.
 	 * See <a href="http://www.prototypejs.org/api/element/setstyle" target="_blank">Prototype's 
 	 * Element.setStyle</a> for details.
	 * @function ?
	 * @param {string|element} element - DOM element or element id
	 * @param {object} styles - Object with style properties/values
	 */
	setStyle : function (element, styles)
	{
		var s = {}
		// Camelize all styles keys
		for (var property in styles)
		{
			s[property.camelize()]=styles[property].camelize();
		}
		Element.setStyle(element, s);
	}
};

/**
 * Utilities for selections.
 * @object Prado.Element.Selection
 */
Prado.Element.Selection =
{
	/**
	 * Check if an DOM element can be selected.
	 * @function {boolean} ?
	 * @param {element} el - DOM elemet
	 * @returns true if element is selectable
	 */
	isSelectable : function(el)
	{
		if(el && el.type)
		{
			switch(el.type.toLowerCase())
			{
				case 'checkbox':
				case 'radio':
				case 'select':
				case 'select-multiple':
				case 'select-one':
				return true;
			}
		}
		return false;
	},

	/**
	 * Set checked attribute of a checkbox or radiobutton to value.
	 * @function {boolean} ?
	 * @param {element} el - DOM element
	 * @param {boolean} value - New value of checked attribute
	 * @returns New value of checked attribute
	 */
	inputValue : function(el, value)
	{
		switch(el.type.toLowerCase())
		{
			case 'checkbox':
			case 'radio':
			return el.checked = value;
		}
	},

	/**
	 * Set selected attribute for elements options by value.
	 * If value is boolean, all elements options selected attribute will be set
	 * to value. Otherwhise all options that have the given value will be selected. 
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 * @param {boolean|string} value - Value of options that should be selected or boolean value of selection status
	 */
	selectValue : function(elements, value)
	{
		elements.each(function(el)
		{
			$A(el.options).each(function(option)
			{
				if(typeof(value) == "boolean")
					option.selected = value;
				else if(option.value == value)
					option.selected = true;
			});
		})
	},

	/**
	 * Set selected attribute for elements options by array of values.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 * @param {string[]} value - Array of values to select
	 */
	selectValues : function(elements, values)
	{
		var selection = this;
		values.each(function(value)
		{
			selection.selectValue(elements,value);
		})
	},

	/**
	 * Set selected attribute for elements options by option index.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 * @param {int} index - Index of option to select
	 */
	selectIndex : function(elements, index)
	{
		elements.each(function(el)
		{
			if(el.type.toLowerCase() == 'select-one')
				el.selectedIndex = index;
			else
			{
				for(var i = 0; i<el.length; i++)
				{
					if(i == index)
						el.options[i].selected = true;
				}
			}
		})
	},

	/**
	 * Set selected attribute to true for all elements options.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 */
	selectAll : function(elements)
	{
		elements.each(function(el)
		{
			if(el.type.toLowerCase() != 'select-one')
			{
				$A(el.options).each(function(option)
				{
					option.selected = true;
				})
			}
		})
	},

	/**
	 * Toggle the selected attribute for elements options.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 */
	selectInvert : function(elements)
	{
		elements.each(function(el)
		{
			if(el.type.toLowerCase() != 'select-one')
			{
				$A(el.options).each(function(option)
				{
					option.selected = !options.selected;
				})
			}
		})
	},

	/**
	 * Set selected attribute for elements options by array of option indices.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 * @param {int[]} indices - Array of option indices to select
	 */
	selectIndices : function(elements, indices)
	{
		var selection = this;
		indices.each(function(index)
		{
			selection.selectIndex(elements,index);
		})
	},

	/**
	 * Unselect elements.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 */
	selectClear : function(elements)
	{
		elements.each(function(el)
		{
			el.selectedIndex = -1;
		})
	},

	/**
	 * Get list elements of an element.
	 * @function {element[]} ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 * @param {int} total - Number of list elements to return
	 * @returns Array of list DOM elements
	 */
	getListElements : function(element, total)
	{
		var elements = new Array();
		var el;
		for(var i = 0; i < total; i++)
		{
			el = $(element+"_c"+i);
			if(el)
				elements.push(el);
		}
		return elements;
	},

	/**
	 * Set checked attribute of elements by value.
	 * If value is boolean, checked attribute will be set to value. 
	 * Otherwhise all elements that have the given value will be checked. 
	 * @function ?
	 * @param {element[]} elements - Array of checkable DOM elements
	 * @param {boolean|String} value - Value that should be checked or boolean value of checked status
	 * 	 
	 */
	checkValue : function(elements, value)
	{
		elements.each(function(el)
		{
			if(typeof(value) == "boolean")
				el.checked = value;
			else if(el.value == value)
				el.checked = true;
		});
	},

	/**
	 * Set checked attribute of elements by array of values.
	 * @function ?
	 * @param {element[]} elements - Array of checkable DOM elements
	 * @param {string[]} values - Values that should be checked
	 * 	 
	 */
	checkValues : function(elements, values)
	{
		var selection = this;
		values.each(function(value)
		{
			selection.checkValue(elements, value);
		})
	},

	/**
	 * Set checked attribute of elements by list index.
	 * @function ?
	 * @param {element[]} elements - Array of checkable DOM elements
	 * @param {int} index - Index of element to set checked
	 */
	checkIndex : function(elements, index)
	{
		for(var i = 0; i<elements.length; i++)
		{
			if(i == index)
				elements[i].checked = true;
		}
	},

	/**
	 * Set checked attribute of elements by array of list indices.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 * @param {int[]} indices - Array of list indices to set checked
	 */
	checkIndices : function(elements, indices)
	{
		var selection = this;
		indices.each(function(index)
		{
			selection.checkIndex(elements, index);
		})
	},

	/**
	 * Uncheck elements.
	 * @function ?
	 * @param {element[]} elements - Array of checkable DOM elements
	 */
	checkClear : function(elements)
	{
		elements.each(function(el)
		{
			el.checked = false;
		});
	},

	/**
	 * Set checked attribute of all elements to true.
	 * @function ?
	 * @param {element[]} elements - Array of checkable DOM elements
	 */
	checkAll : function(elements)
	{
		elements.each(function(el)
		{
			el.checked = true;
		})
	},

	/**
	 * Toggle the checked attribute of elements.
	 * @function ?
	 * @param {element[]} elements - Array of selectable DOM elements
	 */
	checkInvert : function(elements)
	{
		elements.each(function(el)
		{
			el.checked != el.checked;
		})
	}
};


/**
 * Utilities for insertion.
 * @object Prado.Element.Insert
 */
Prado.Element.Insert =
{
	/**
	 * Append content to element
	 * @function ?
	 * @param {element} element - DOM element that content should be appended to
	 * @param {element} content - DOM element to append
	 */
	append: function(element, content)
	{
		$(element).insert(content);
	},

	/**
	 * Prepend content to element
	 * @function ?
	 * @param {element} element - DOM element that content should be prepended to
	 * @param {element} content - DOM element to prepend
	 */
	prepend: function(element, content)
	{
		$(element).insert({top:content});
	},

	/**
	 * Insert content after element
	 * @function ?
	 * @param {element} element - DOM element that content should be inserted after
	 * @param {element} content - DOM element to insert
	 */
	after: function(element, content)
	{
		$(element).insert({after:content});
	},

	/**
	 * Insert content before element
	 * @function ?
	 * @param {element} element - DOM element that content should be inserted before
	 * @param {element} content - DOM element to insert
	 */
	before: function(element, content)
	{
		$(element).insert({before:content});
	}
};


/**
 * Extension to 
 * <a href="http://wiki.script.aculo.us/scriptaculous/show/builder" target="_blank">Scriptaculous' Builder</a>
 * @namespace Builder
 */

Object.extend(Builder,
{
	/**
 	 * Export scriptaculous builder utilities as window[functions]
 	 * @function ?
 	 */
	exportTags:function()
	{
		var tags=["BUTTON","TT","PRE","H1","H2","H3","BR","CANVAS","HR","LABEL","TEXTAREA","FORM","STRONG","SELECT","OPTION","OPTGROUP","LEGEND","FIELDSET","P","UL","OL","LI","TD","TR","THEAD","TBODY","TFOOT","TABLE","TH","INPUT","SPAN","A","DIV","IMG", "CAPTION"];
		tags.each(function(tag)
		{
			window[tag]=function()
			{
				var args=$A(arguments);
				if(args.length==0)
					return Builder.node(tag,null);
				if(args.length==1)
					return Builder.node(tag,args[0]);
				if(args.length>1)
					return Builder.node(tag,args.shift(),args);

			};
		});
	}
});
Builder.exportTags();

/**
 * Extension to 
 * <a href="http://www.prototypejs.org/api/string" target="_blank">Prototype's String</a>
 * @namespace String
 */
Object.extend(String.prototype, {

	/**
	 * Add padding to string
	 * @function {string} ?
	 * @param {string} side - "left" to pad the string on the left, "right" to pad right.
	 * @param {int} len - Minimum string length.
	 * @param {string} chr - Character(s) to pad
	 * @returns Padded string
	 */
	pad : function(side, len, chr) {
		if (!chr) chr = ' ';
		var s = this;
		var left = side.toLowerCase()=='left';
		while (s.length<len) s = left? chr + s : s + chr;
		return s;
	},

	/**
	 * Add left padding to string
	 * @function {string} ?
	 * @param {int} len - Minimum string length.
	 * @param {string} chr - Character(s) to pad
	 * @returns Padded string
	 */
	padLeft : function(len, chr) {
		return this.pad('left',len,chr);
	},

	/**
	 * Add right padding to string
	 * @function {string} ?
	 * @param {int} len - Minimum string length.
	 * @param {string} chr - Character(s) to pad
	 * @returns Padded string
	 */
	padRight : function(len, chr) {
		return this.pad('right',len,chr);
	},

	/**
	 * Add zeros to the right of string
	 * @function {string} ?
	 * @param {int} len - Minimum string length.
	 * @returns Padded string
	 */
	zerofill : function(len) {
		return this.padLeft(len,'0');
	},

	/**
	 * Remove white spaces from both ends of string.
	 * @function {string} ?
	 * @returns Trimmed string
	 */
	trim : function() {
		return this.replace(/^\s+|\s+$/g,'');
	},

	/**
	 * Remove white spaces from the left side of string.
	 * @function {string} ?
	 * @returns Trimmed string
	 */
	trimLeft : function() {
		return this.replace(/^\s+/,'');
	},

	/**
	 * Remove white spaces from the right side of string.
	 * @function {string} ?
	 * @returns Trimmed string
	 */
	trimRight : function() {
		return this.replace(/\s+$/,'');
	},

	/**
	 * Convert period separated function names into a function reference.
	 * <br />Example:
	 * <pre> 
	 * "Prado.AJAX.Callback.Action.setValue".toFunction()
	 * </pre>
	 * @function {function} ?
	 * @returns Reference to the corresponding function
	 */
	toFunction : function()
	{
		var commands = this.split(/\./);
		var command = window;
		commands.each(function(action)
		{
			if(command[new String(action)])
				command=command[new String(action)];
		});
		if(typeof(command) == "function")
			return command;
		else
		{
			if(typeof Logger != "undefined")
				Logger.error("Missing function", this);

			throw new Error	("Missing function '"+this+"'");
		}
	},

	/**
	 * Convert string into integer, returns null if not integer.
	 * @function {int} ?
	 * @returns Integer, null if string does not represent an integer.
	 */
	toInteger : function()
	{
		var exp = /^\s*[-\+]?\d+\s*$/;
		if (this.match(exp) == null)
			return null;
		var num = parseInt(this, 10);
		return (isNaN(num) ? null : num);
	},

	/**
	 * Convert string into a double/float value. <b>Internationalization
	 * is not supported</b>
	 * @function {double} ?
	 * @param {string} decimalchar - Decimal character, defaults to "."
	 * @returns Double, null if string does not represent a float value
	 */
	toDouble : function(decimalchar)
	{
		if(this.length <= 0) return null;
		decimalchar = decimalchar || ".";
		var exp = new RegExp("^\\s*([-\\+])?(\\d+)?(\\" + decimalchar + "(\\d+))?\\s*$");
		var m = this.match(exp);

		if (m == null)
			return null;
		m[1] = m[1] || "";
		m[2] = m[2] || "0";
		m[4] = m[4] || "0";

		var cleanInput = m[1] + (m[2].length>0 ? m[2] : "0") + "." + m[4];
		var num = parseFloat(cleanInput);
		return (isNaN(num) ? null : num);
	},

	/**
	 * Convert strings that represent a currency value to float.
	 * E.g. "10,000.50" will become "10000.50". The number
	 * of dicimal digits, grouping and decimal characters can be specified.
	 * <i>The currency input format is <b>very</b> strict, null will be returned if
	 * the pattern does not match</i>.
	 * @function {double} ?
	 * @param {string} groupchar - Grouping character, defaults to ","
	 * @param {int} digits - Number of decimal digits
	 * @param {string} decimalchar - Decimal character, defaults to "."
	 * @returns Double, null if string does not represent a currency value
	 */
	toCurrency : function(groupchar, digits, decimalchar)
	{
		groupchar = groupchar || ",";
		decimalchar = decimalchar || ".";
		digits = typeof(digits) == "undefined" ? 2 : digits;

		var exp = new RegExp("^\\s*([-\\+])?(((\\d+)\\" + groupchar + ")*)(\\d+)"
			+ ((digits > 0) ? "(\\" + decimalchar + "(\\d{1," + digits + "}))?" : "")
			+ "\\s*$");
		var m = this.match(exp);
		if (m == null)
			return null;
		var intermed = m[2] + m[5] ;
		var cleanInput = m[1] + intermed.replace(
				new RegExp("(\\" + groupchar + ")", "g"), "")
								+ ((digits > 0) ? "." + m[7] : "");
		var num = parseFloat(cleanInput);
		return (isNaN(num) ? null : num);
	},

	/**
	 * Converts the string to a date by finding values that matches the
	 * date format pattern.
	 * @function {Date} ?
	 * @param {string} format - Date format pattern, e.g. MM-dd-yyyy
	 * @returns Date extracted from the string
	 */
	toDate : function(format)
	{
		return Date.SimpleParse(this, format);
	}
});

/**
 * Extension to 
 * <a href="http://www.prototypejs.org/api/event" target="_blank">Prototype's Event</a>
 * @namespace Event
 */
Object.extend(Event,
{
	/**
	 * Register a function to be executed when the page is loaded.
	 * Note that the page is only loaded if all resources (e.g. images)
	 * are loaded.
	 * <br />Example: 
	 * <br />Show an alert box with message "Page Loaded!" when the
	 * page finished loading.
	 * <pre>
	 * Event.OnLoad(function(){ alert("Page Loaded!"); });
	 * </pre>
	 * @function ?
	 * @param {function} fn - Function to execute when page is loaded.
	 */
	OnLoad : function (fn)
	{
		// opera onload is in document, not window
		var w = document.addEventListener &&
					!window.addEventListener ? document : window;
		Event.observe(w,'load',fn);
	},

	/**
	 * Returns the unicode character generated by key.
	 * @param {event} e - Keyboard event
	 * @function {int} ?
	 * @returns Unicode character code generated by the key that was struck.
	 */
	keyCode : function(e)
	{
	   return e.keyCode != null ? e.keyCode : e.charCode
	},

	/**
	 * Checks if an Event is of type HTMLEvent.
	 * @function {boolean} ?
	 * @param {string} type - Event type or event name.
	 * @return true if event is of type HTMLEvent.
	 */
	isHTMLEvent : function(type)
	{
		var events = ['abort', 'blur', 'change', 'error', 'focus',
					'load', 'reset', 'resize', 'scroll', 'select',
					'submit', 'unload'];
		return events.include(type);
	},

	/**
	 * Checks if an Event is a mouse event.
	 * @function {boolean} ?
	 * @param {string} type - Event type or event name
	 * @return true if event is of type MouseEvent.
	 */
	isMouseEvent : function(type)
	{
		var events = ['click', 'mousedown', 'mousemove', 'mouseout',
					'mouseover', 'mouseup'];
		return events.include(type);
	},

	/**
	 * Dispatch the DOM event of a given <tt>type</tt> on a DOM
	 * <tt>element</tt>. Only HTMLEvent and MouseEvent can be
	 * dispatched, keyboard events or UIEvent can not be dispatch
	 * via javascript consistently.
	 * For the "submit" event the submit() method is called.
	 * @function ?
	 * @param {element|string} element - Element id string or DOM element.
	 * @param {string} type - Event type to dispatch.
	 */
	fireEvent : function(element,type)
	{
		element = $(element);
		if(type == "submit")
			return element.submit();
		if(document.createEvent)
        {
			if(Event.isHTMLEvent(type))
			{
				var event = document.createEvent('HTMLEvents');
	            event.initEvent(type, true, true);
			}
			else if(Event.isMouseEvent(type))
			{
				var event = document.createEvent('MouseEvents');
				if (event.initMouseEvent)
		        {
					event.initMouseEvent(type,true,true,
						document.defaultView, 1, 0, 0, 0, 0, false,
								false, false, false, 0, null);
		        }
		        else
		        {
		            // Safari
		            // TODO we should be initialising other mouse-event related attributes here
		            event.initEvent(type, true, true);
		        }
			}
            element.dispatchEvent(event);
        }
        else if(document.createEventObject)
        {
        	var evObj = document.createEventObject();
            element.fireEvent('on'+type, evObj);
        }
        else if(typeof(element['on'+type]) == "function")
            element['on'+type]();
	}
});


/**
 * Extension to 
 * <a href="http://www.prototypejs.org/api/date" target="_blank">Prototype's Date</a>
 * @namespace Date
 */
Object.extend(Date.prototype,
{
	/**
	 * SimpleFormat
	 * @function ?
	 * @param {string} format - TODO
	 * @param {string} data - TODO
	 * @returns TODO
	 */
	SimpleFormat: function(format, data)
	{
		data = data || {};
		var bits = new Array();
		bits['d'] = this.getDate();
		bits['dd'] = String(this.getDate()).zerofill(2);

		bits['M'] = this.getMonth()+1;
		bits['MM'] = String(this.getMonth()+1).zerofill(2);
		if(data.AbbreviatedMonthNames)
			bits['MMM'] = data.AbbreviatedMonthNames[this.getMonth()];
		if(data.MonthNames)
			bits['MMMM'] = data.MonthNames[this.getMonth()];
		var yearStr = "" + this.getFullYear();
		yearStr = (yearStr.length == 2) ? '19' + yearStr: yearStr;
		bits['yyyy'] = yearStr;
		bits['yy'] = bits['yyyy'].toString().substr(2,2);

		// do some funky regexs to replace the format string
		// with the real values
		var frm = new String(format);
		for (var sect in bits)
		{
			var reg = new RegExp("\\b"+sect+"\\b" ,"g");
			frm = frm.replace(reg, bits[sect]);
		}
		return frm;
	},

	/**
	 * toISODate
	 * @function {string} ?
	 * @returns TODO
	 */
	toISODate : function()
	{
		var y = this.getFullYear();
		var m = String(this.getMonth() + 1).zerofill(2);
		var d = String(this.getDate()).zerofill(2);
		return String(y) + String(m) + String(d);
	}
});

Object.extend(Date,
{
	/**
	 * SimpleParse
	 * @function ?
	 * @param {string} format - TODO
	 * @param {string} data - TODO
	 * @returns TODO
	 */
	SimpleParse: function(value, format)
	{
		var val=String(value);
		format=String(format);

		if(val.length <= 0) return null;

		if(format.length <= 0) return new Date(value);

		var isInteger = function (val)
		{
			var digits="1234567890";
			for (var i=0; i < val.length; i++)
			{
				if (digits.indexOf(val.charAt(i))==-1) { return false; }
			}
			return true;
		};

		var getInt = function(str,i,minlength,maxlength)
		{
			for (var x=maxlength; x>=minlength; x--)
			{
				var token=str.substring(i,i+x);
				if (token.length < minlength) { return null; }
				if (isInteger(token)) { return token; }
			}
			return null;
		};

		var i_val=0;
		var i_format=0;
		var c="";
		var token="";
		var token2="";
		var x,y;
		var now=new Date();
		var year=now.getFullYear();
		var month=now.getMonth()+1;
		var date=1;

		while (i_format < format.length)
		{
			// Get next token from format string
			c=format.charAt(i_format);
			token="";
			while ((format.charAt(i_format)==c) && (i_format < format.length))
			{
				token += format.charAt(i_format++);
			}

			// Extract contents of value based on format token
			if (token=="yyyy" || token=="yy" || token=="y")
			{
				if (token=="yyyy") { x=4;y=4; }
				if (token=="yy")   { x=2;y=2; }
				if (token=="y")    { x=2;y=4; }
				year=getInt(val,i_val,x,y);
				if (year==null) { return null; }
				i_val += year.length;
				if (year.length==2)
				{
					if (year > 70) { year=1900+(year-0); }
					else { year=2000+(year-0); }
				}
			}

			else if (token=="MM"||token=="M")
			{
				month=getInt(val,i_val,token.length,2);
				if(month==null||(month<1)||(month>12)){return null;}
				i_val+=month.length;
			}
			else if (token=="dd"||token=="d")
			{
				date=getInt(val,i_val,token.length,2);
				if(date==null||(date<1)||(date>31)){return null;}
				i_val+=date.length;
			}
			else
			{
				if (val.substring(i_val,i_val+token.length)!=token) {return null;}
				else {i_val+=token.length;}
			}
		}

		// If there are any trailing characters left in the value, it doesn't match
		if (i_val != val.length) { return null; }

		// Is date valid for month?
		if (month==2)
		{
			// Check for leap year
			if ( ( (year%4==0)&&(year%100 != 0) ) || (year%400==0) ) { // leap year
				if (date > 29){ return null; }
			}
			else { if (date > 28) { return null; } }
		}

		if ((month==4)||(month==6)||(month==9)||(month==11))
		{
			if (date > 30) { return null; }
		}

		var newdate=new Date(year,month-1,date, 0, 0, 0);
		return newdate;
	}
});

/**
 * Prado utilities for effects.
 * @object Prado.Effect
 */
Prado.Effect = 
{
	/**
	 * Highlights an element
	 * @function ?
	 * @param {element} element - DOM element to highlight
	 * @param {optional object} options - Highlight options
	 */
	Highlight : function (element,options)
	{
		new Effect.Highlight(element,options);
	}
};
