/**
 * Generic postback control.
 */
Prado.WebUI.CallbackControl = Class.extend(Prado.WebUI.PostBackControl,
{
	onPostBack : function(event, options)
	{
		var request = new Prado.CallbackRequest(options.EventTarget, options);
		request.dispatch();
		Event.stop(event);
	}
});

/**
 * TActiveButton control.
 */
Prado.WebUI.TActiveButton = Class.extend(Prado.WebUI.CallbackControl);
/**
 * TActiveLinkButton control.
 */
Prado.WebUI.TActiveLinkButton = Class.extend(Prado.WebUI.CallbackControl);

Prado.WebUI.TActiveImageButton = Class.extend(Prado.WebUI.TImageButton,
{
	onPostBack : function(event, options)
	{
		this.addXYInput(event,options);
		var request = new Prado.CallbackRequest(options.EventTarget, options);
		request.dispatch();
		Event.stop(event);
		this.removeXYInput(event,options);
	}
});
/**
 * Active check box.
 */
Prado.WebUI.TActiveCheckBox = Class.extend(Prado.WebUI.CallbackControl,
{
	onPostBack : function(event, options)
	{
		var request = new Prado.CallbackRequest(options.EventTarget, options);
		if(request.dispatch()==false)
			Event.stop(event);
	}
});

/**
 * TActiveRadioButton control.
 */
Prado.WebUI.TActiveRadioButton = Class.extend(Prado.WebUI.TActiveCheckBox);


Prado.WebUI.TActiveCheckBoxList = Base.extend(
{
	constructor : function(options)
	{
		Prado.Registry.set(options.ListID, this);
		for(var i = 0; i<options.ItemCount; i++)
		{
			var checkBoxOptions = Object.extend(
			{
				ID : options.ListID+"_c"+i,
				EventTarget : options.ListName+"$c"+i
			}, options);
			new Prado.WebUI.TActiveCheckBox(checkBoxOptions);
		}
	}
});

Prado.WebUI.TActiveRadioButtonList = Prado.WebUI.TActiveCheckBoxList;

/**
 * TActiveTextBox control, handles onchange event.
 */
Prado.WebUI.TActiveTextBox = Class.extend(Prado.WebUI.TTextBox,
{
	onInit : function(options)
	{
		this.options=options;
		if(options['TextMode'] != 'MultiLine')
			Event.observe(this.element, "keydown", this.handleReturnKey.bind(this));
		if(this.options['AutoPostBack']==true)
			Event.observe(this.element, "change", this.doCallback.bindEvent(this,options));
	},

	doCallback : function(event, options)
	{
		var request = new Prado.CallbackRequest(options.EventTarget, options);
		request.dispatch();
        if (!Prototype.Browser.IE)
		    Event.stop(event);
	}
});

/**
 * TAutoComplete control.
 */
Prado.WebUI.TAutoComplete = Class.extend(Autocompleter.Base, Prado.WebUI.TActiveTextBox.prototype);
Prado.WebUI.TAutoComplete = Class.extend(Prado.WebUI.TAutoComplete,
{
	initialize : function(options)
	{
		this.options = options;
		this.hasResults = false;
		this.baseInitialize(options.ID, options.ResultPanel, options);
		Object.extend(this.options,
		{
			onSuccess : this.onComplete.bind(this)
		});

		if(options.AutoPostBack)
			this.onInit(options);

		Prado.Registry.set(options.ID, this);
	},

	doCallback : function(event, options)
	{
		if(!this.active)
		{
			var request = new Prado.CallbackRequest(this.options.EventTarget, options);
			request.dispatch();
			Event.stop(event);
		}
	},

	 //Overrides parent implementation, fires onchange event.
	onClick: function(event)
	{
	    var element = Event.findElement(event, 'LI');
	    this.index = element.autocompleteIndex;
	    this.selectEntry();
	    this.hide();
		Event.fireEvent(this.element, "change");
	},

	getUpdatedChoices : function()
	{
		var options = new Array(this.getToken(),"__TAutoComplete_onSuggest__");
		Prado.Callback(this.options.EventTarget, options, null, this.options);
	},

	/**
	 * Overrides parent implements, don't update if no results.
	 */
	selectEntry: function()
	{
		if(this.hasResults)
		{
			this.active = false;
			this.updateElement(this.getCurrentEntry());
			var options = [this.index, "__TAutoComplete_onSuggestionSelected__"];
			Prado.Callback(this.options.EventTarget, options, null, this.options);
		}
	},

	onComplete : function(request, boundary)
  	{
  		var result = Prado.Element.extractContent(request.transport.responseText, boundary);
  		if(typeof(result) == "string")
		{
			if(result.length > 0)
			{
				this.hasResults = true;
				this.updateChoices(result);
			}
			else
			{
				this.active = false;
				this.hasResults = false;
				this.hide();
			}
		}
	}
});

/**
 * Time Triggered Callback class.
 */
Prado.WebUI.TTimeTriggeredCallback = Base.extend(
{
	constructor : function(options)
	{
		this.options = Object.extend({ Interval : 1	}, options || {});
		Prado.WebUI.TTimeTriggeredCallback.register(this);
		Prado.Registry.set(options.ID, this);
	},

	startTimer : function()
	{
		if(typeof(this.timer) == 'undefined' || this.timer == null)
			this.timer = setInterval(this.onTimerEvent.bind(this),this.options.Interval*1000);
	},

	stopTimer : function()
	{
		if(typeof(this.timer) != 'undefined')
		{
			clearInterval(this.timer);
			this.timer = null;
		}
	},
	
	resetTimer : function()
	{
		if(typeof(this.timer) != 'undefined')
		{
			clearInterval(this.timer);
			this.timer = null;
			this.timer = setInterval(this.onTimerEvent.bind(this),this.options.Interval*1000);
		}
	},

	onTimerEvent : function()
	{
		var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
		request.dispatch();
	},
	
	setInterval : function(value)
	{
		if (this.options.Interval != value){
			this.options.Interval = value;
			this.resetTimer();
		}
	}
},
//class methods
{
	timers : {},

	register : function(timer)
	{
		Prado.WebUI.TTimeTriggeredCallback.timers[timer.options.ID] = timer;
	},

	start : function(id)
	{
		if(Prado.WebUI.TTimeTriggeredCallback.timers[id])
			Prado.WebUI.TTimeTriggeredCallback.timers[id].startTimer();
	},

	stop : function(id)
	{
		if(Prado.WebUI.TTimeTriggeredCallback.timers[id])
			Prado.WebUI.TTimeTriggeredCallback.timers[id].stopTimer();
	},
	
	setInterval : function (id,value)
	{
		if(Prado.WebUI.TTimeTriggeredCallback.timers[id])
			Prado.WebUI.TTimeTriggeredCallback.timers[id].setInterval(value);
	}
});

Prado.WebUI.ActiveListControl = Base.extend(
{
	constructor : function(options)
	{
		this.element = $(options.ID);
		Prado.Registry.set(options.ID, this);
		if(this.element)
		{
			this.options = options;
			Event.observe(this.element, "change", this.doCallback.bind(this));
		}
	},

	doCallback : function(event)
	{
		var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
		request.dispatch();
		Event.stop(event);
	}
});

Prado.WebUI.TActiveDropDownList = Prado.WebUI.ActiveListControl;
Prado.WebUI.TActiveListBox = Prado.WebUI.ActiveListControl;

/**
 * Observe event of a particular control to trigger a callback request.
 */
Prado.WebUI.TEventTriggeredCallback = Base.extend(
{
	constructor : function(options)
	{
		this.options = options;
		var element = $(options['ControlID']);
		if(element)
			Event.observe(element, this.getEventName(element), this.doCallback.bind(this));
	},

	getEventName : function(element)
	{
		var name = this.options.EventName;
   		if(typeof(name) == "undefined" && element.type)
		{
      		switch (element.type.toLowerCase())
			{
          		case 'password':
		        case 'text':
		        case 'textarea':
		        case 'select-one':
		        case 'select-multiple':
          			return 'change';
      		}
		}
		return typeof(name) == "undefined"  || name == "undefined" ? 'click' : name;
    },

	doCallback : function(event)
	{
		var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
		request.dispatch();
		if(this.options.StopEvent == true)
			Event.stop(event);
	}
});

/**
 * Observe changes to a property of a particular control to trigger a callback.
 */
Prado.WebUI.TValueTriggeredCallback = Base.extend(
{
	count : 1,

	observing : true,

	constructor : function(options)
	{
		this.options = options;
		this.options.PropertyName = this.options.PropertyName || 'value';
		var element = $(options['ControlID']);
		this.value = element ? element[this.options.PropertyName] : undefined;
		Prado.WebUI.TValueTriggeredCallback.register(this);
		Prado.Registry.set(options.ID, this);
		this.startObserving();
	},

	stopObserving : function()
	{
		clearTimeout(this.timer);
		this.observing = false;
	},

	startObserving : function()
	{
		this.timer = setTimeout(this.checkChanges.bind(this), this.options.Interval*1000);
	},

	checkChanges : function()
	{
		var element = $(this.options.ControlID);
		if(element)
		{
			var value = element[this.options.PropertyName];
			if(this.value != value)
			{
				this.doCallback(this.value, value);
				this.value = value;
				this.count=1;
			}
			else
				this.count = this.count + this.options.Decay;
			if(this.observing)
				this.time = setTimeout(this.checkChanges.bind(this),
					parseInt(this.options.Interval*1000*this.count));
		}
	},

	doCallback : function(oldValue, newValue)
	{
		var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
		var param = {'OldValue' : oldValue, 'NewValue' : newValue};
		request.setCallbackParameter(param);
		request.dispatch();
	}
},
//class methods
{
	timers : {},

	register : function(timer)
	{
		Prado.WebUI.TValueTriggeredCallback.timers[timer.options.ID] = timer;
	},

	stop : function(id)
	{
		Prado.WebUI.TValueTriggeredCallback.timers[id].stopObserving();
	}
});
