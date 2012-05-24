
Prado.AjaxRequest = Class.create();
Prado.AjaxRequest.prototype = Object.clone(Ajax.Request.prototype);

/**
 * Override Prototype's response implementation.
 */
Object.extend(Prado.AjaxRequest.prototype,
{
	/*initialize: function(request)
	{
		this.CallbackRequest = request;
		this.transport = Ajax.getTransport();
		this.setOptions(request.options);
		this.request(request.url);
	},*/

	/**
	 * Customize the response, dispatch onXXX response code events, and
	 * tries to execute response actions (javascript statements).
	 */
	respondToReadyState : function(readyState)
	{
	    var event = Ajax.Request.Events[readyState];
	    var transport = this.transport, json = this.getBodyDataPart(Prado.CallbackRequest.DATA_HEADER);

	    if (event == 'Complete')
	    {
			var redirectUrl = this.getBodyContentPart(Prado.CallbackRequest.REDIRECT_HEADER);
	    	if(redirectUrl)
	    		document.location.href = redirectUrl;

	      if ((this.getHeader('Content-type') || '').match(/^text\/javascript/i))
	      {
	        try
			{
	           json = eval('(' + transport.responseText + ')');
	        }catch (e)
			{
				if(typeof(json) == "string")
					json = Prado.CallbackRequest.decode(result);
			}
	      }

	      try
	      {
	      	Prado.CallbackRequest.updatePageState(this,transport);
			Ajax.Responders.dispatch('on' + transport.status, this, transport, json);
			Prado.CallbackRequest.dispatchActions(transport,this.getBodyDataPart(Prado.CallbackRequest.ACTION_HEADER));

	        (this.options['on' + this.transport.status]
	         || this.options['on' + (this.success() ? 'Success' : 'Failure')]
	         || Prototype.emptyFunction)(this, json);
	  	      } catch (e) {
	        this.dispatchException(e);
	      }
	    }

	    try {
	      (this.options['on' + event] || Prototype.emptyFunction)(this, json);
	      Ajax.Responders.dispatch('on' + event, this, transport, json);
	    } catch (e) {
	      this.dispatchException(e);
	    }

	    /* Avoid memory leak in MSIE: clean up the oncomplete event handler */
	    if (event == 'Complete')
	      this.transport.onreadystatechange = Prototype.emptyFunction;
	},

	/**
	 * Gets header data assuming JSON encoding.
	 * @param string header name
	 * @return object header data as javascript structures.
	 */
	getHeaderData : function(name)
	{
		return this.getJsonData(this.getHeader(name));
	},

	getBodyContentPart : function(name)
	{
		if(typeof(this.transport.responseText)=="string")
			return Prado.Element.extractContent(this.transport.responseText, name);
	},

	getJsonData : function(json)
	{
		try
		{
			return eval('(' + json + ')');
		}
		catch (e)
		{
			if(typeof(json) == "string")
				return Prado.CallbackRequest.decode(json);
		}
	},

	getBodyDataPart : function(name)
	{
		return this.getJsonData(this.getBodyContentPart(name));
	}
});

/**
 * Prado Callback client-side request handler.
 */
Prado.CallbackRequest = Class.create();

/**
 * Static definitions.
 */
Object.extend(Prado.CallbackRequest,
{
	/**
	 * Callback request target POST field name.
	 */
	FIELD_CALLBACK_TARGET : 'PRADO_CALLBACK_TARGET',
	/**
	 * Callback request parameter POST field name.
	 */
	FIELD_CALLBACK_PARAMETER : 'PRADO_CALLBACK_PARAMETER',
	/**
	 * Callback request page state field name,
	 */
	FIELD_CALLBACK_PAGESTATE : 'PRADO_PAGESTATE',

	FIELD_POSTBACK_TARGET : 'PRADO_POSTBACK_TARGET',

	FIELD_POSTBACK_PARAMETER : 'PRADO_POSTBACK_PARAMETER',

	/**
	 * List of form fields that will be collected during callback.
	 */
	PostDataLoaders : [],
	/**
	 * Response data header name.
	 */
	DATA_HEADER : 'X-PRADO-DATA',
	/**
	 * Response javascript execution statement header name.
	 */
	ACTION_HEADER : 'X-PRADO-ACTIONS',
	/**
	 * Response errors/exceptions header name.
	 */
	ERROR_HEADER : 'X-PRADO-ERROR',
	/**
	 * Page state header name.
	 */
	PAGESTATE_HEADER : 'X-PRADO-PAGESTATE',

	REDIRECT_HEADER : 'X-PRADO-REDIRECT',

	requestQueue : [],

	//all request objects
	requests : {},

	getRequestById : function(id)
	{
		var requests = Prado.CallbackRequest.requests;
		if(typeof(requests[id]) != "undefined")
			return requests[id];
	},

	dispatch : function(id)
	{
		var requests = Prado.CallbackRequest.requests;
		if(typeof(requests[id]) != "undefined")
			requests[id].dispatch();
	},

	/**
	 * Add ids of inputs element to post in the request.
	 */
	addPostLoaders : function(ids)
	{
		var self = Prado.CallbackRequest;
		self.PostDataLoaders = self.PostDataLoaders.concat(ids);
		var list = [];
		self.PostDataLoaders.each(function(id)
		{
			if(list.indexOf(id) < 0)
				list.push(id);
		});
		self.PostDataLoaders = list;
	},

	/**
	 * Dispatch callback response actions.
	 */
	dispatchActions : function(transport,actions)
	{
		var self = Prado.CallbackRequest;
		if(actions && actions.length > 0)
			actions.each(self.__run.bind(self,transport));
	},

	/**
	 * Prase and evaluate a Callback clien-side action
	 */
	__run : function(transport, command)
	{
		var self = Prado.CallbackRequest;
		self.transport = transport;
		for(var method in command)
		{
			try
			{
				method.toFunction().apply(self,command[method]);
			}
			catch(e)
			{
				if(typeof(Logger) != "undefined")
					self.Exception.onException(null,e);
			}
		}
	},

	/**
	 * Respond to Prado Callback request exceptions.
	 */
	Exception :
	{
		/**
		 * Server returns 500 exception. Just log it.
		 */
		"on500" : function(request, transport, data)
		{
			var e = request.getHeaderData(Prado.CallbackRequest.ERROR_HEADER);
			if (e)
				Logger.error("Callback Server Error "+e.code, this.formatException(e));
			else
				Logger.error("Callback Server Error Unknown",'');
		},

		/**
		 * Callback OnComplete event,logs reponse and data to console.
		 */
		'on200' : function(request, transport, data)
		{
			if(transport.status < 500)
			{
				var msg = 'HTTP '+transport.status+" with response : \n";
				if(transport.responseText.trim().length >0)
				{
					var f = RegExp('(<!--X-PRADO[^>]+-->)([\\s\\S\\w\\W]*)(<!--//X-PRADO[^>]+-->)',"m");
					msg += transport.responseText.replace(f,'') + "\n";
				}
				if(typeof(data)!="undefined" && data != null)
					msg += "Data : \n"+inspect(data)+"\n";
				data = request.getBodyDataPart(Prado.CallbackRequest.ACTION_HEADER);
				if(data && data.length > 0)
				{
					msg += "Actions : \n";
					data.each(function(action)
					{
						msg += inspect(action)+"\n";
					});
				}
				Logger.info(msg);
			}
		},

		/**
		 * Uncaught exceptions during callback response.
		 */
		onException : function(request,e)
		{
			var msg = "";
			$H(e).each(function(item)
			{
				msg += item.key+": "+item.value+"\n";
			})
			Logger.error('Uncaught Callback Client Exception:', msg);
		},

		/**
		 * Formats the exception message for display in console.
		 */
		formatException : function(e)
		{
			var msg = e.type + " with message \""+e.message+"\"";
			msg += " in "+e.file+"("+e.line+")\n";
			msg += "Stack trace:\n";
			var trace = e.trace;
			for(var i = 0; i<trace.length; i++)
			{
				msg += "  #"+i+" "+trace[i].file;
				msg += "("+trace[i].line+"): ";
				msg += trace[i]["class"]+"->"+trace[i]["function"]+"()"+"\n";
			}
			msg += e.version+" "+e.time+"\n";
			return msg;
		}
	},

	/**
	 * @return string JSON encoded data.
	 */
	encode : function(data)
	{
		return Prado.JSON.stringify(data);
	},

	/**
	 * @return mixed javascript data decoded from string using JSON decoding.
	 */
	decode : function(data)
	{
		if(typeof(data) == "string" && data.trim().length > 0)
			return Prado.JSON.parse(data);
		else
			return null;
	},

	/**
	 * Dispatch a normal request, no timeouts or aborting of requests.
	 */
	dispatchNormalRequest : function(callback)
	{
		callback.options.postBody = callback._getPostData(),
		callback.request(callback.url);
		return true;
	},

	/**
	 * Abort the current priority request in progress.
	 */
	tryNextRequest : function()
	{
		var self = Prado.CallbackRequest;
		//Logger.debug('trying next request');
		if(typeof(self.currentRequest) == 'undefined' || self.currentRequest==null)
		{
			if(self.requestQueue.length > 0)
				return self.dispatchQueue();
			//else
				//Logger.warn('empty queque');
		}
		//else
			//Logger.warn('current request ' + self.currentRequest.id);
	},

	/**
	 * Updates the page state. It will update only if EnablePageStateUpdate and
	 * HasPriority options are both true.
	 */
	updatePageState : function(request, transport)
	{
		var self = Prado.CallbackRequest;
		var pagestate = $(self.FIELD_CALLBACK_PAGESTATE);
		var enabled = request.ActiveControl.EnablePageStateUpdate && request.ActiveControl.HasPriority;
		var aborted = typeof(self.currentRequest) == 'undefined' || self.currentRequest == null;
		if(enabled && !aborted && pagestate)
		{
			var data = request.getBodyContentPart(self.PAGESTATE_HEADER);
			if(typeof(data) == "string" && data.length > 0)
				pagestate.value = data;
			else
			{
				if(typeof(Logger) != "undefined")
					Logger.warn("Missing page state:"+data);
				//Logger.warn('## bad state: setting current request to null');
				self.endCurrentRequest();
				//self.tryNextRequest();
				return false;
			}
		}
		self.endCurrentRequest();
		//Logger.warn('## state updated: setting current request to null');
		//self.tryNextRequest();
		return true;
	},

	enqueue : function(callback)
	{
		var self = Prado.CallbackRequest;
		self.requestQueue.push(callback);
		//Logger.warn("equeued "+callback.id+", current queque length="+self.requestQueue.length);
		self.tryNextRequest();
	},

	dispatchQueue : function()
	{
		var self = Prado.CallbackRequest;
		//Logger.warn("dispatching queque, length="+self.requestQueue.length+" request="+self.currentRequest);
		var callback = self.requestQueue.shift();
		self.currentRequest = callback;

		//get data
		callback.options.postBody = callback._getPostData(),

		//callback.request = new Prado.AjaxRequest(callback);
		callback.timeout = setTimeout(function()
		{
			//Logger.warn("priority timeout");
			self.abortRequest(callback.id);
		},callback.ActiveControl.RequestTimeOut);
		callback.request(callback.url);
		//Logger.debug("dispatched "+self.currentRequest.id + " ...")
	},

	endCurrentRequest : function()
	{
		var self = Prado.CallbackRequest;
		if(typeof(self.currentRequest) != 'undefined' && self.currentRequest != null)
			clearTimeout(self.currentRequest.timeout);
		self.currentRequest=null;
	},

	abortRequest : function(id)
	{
		//Logger.warn("abort id="+id);
		var self = Prado.CallbackRequest;
		if(typeof(self.currentRequest) != 'undefined'
			&& self.currentRequest != null && self.currentRequest.id == id)
		{
			var request = self.currentRequest;
			if(request.transport.readyState < 4)
				request.transport.abort();
			//Logger.warn('## aborted: setting current request to null');
			self.endCurrentRequest();
		}
		self.tryNextRequest();
	}
});

/**
 * Automatically aborts the current request when a priority request has returned.
 */
Ajax.Responders.register({onComplete : function(request)
{
	if(request && request instanceof Prado.AjaxRequest)
	{
		if(request.ActiveControl.HasPriority)
			Prado.CallbackRequest.tryNextRequest();
	}
}});

//Add HTTP exception respones when logger is enabled.
Event.OnLoad(function()
{
	if(typeof Logger != "undefined")
		Ajax.Responders.register(Prado.CallbackRequest.Exception);
});

/**
 * Create and prepare a new callback request.
 * Call the dispatch() method to start the callback request.
 * <code>
 * request = new Prado.CallbackRequest(UniqueID, callback);
 * request.dispatch();
 * </code>
 */
Prado.CallbackRequest.prototype = Object.extend(Prado.AjaxRequest.prototype,
{

	/**
	 * Prepare and inititate a callback request.
	 */
	initialize : function(id, options)
	{
		/**
		 * Callback URL, same url as the current page.
		 */
		this.url = this.getCallbackUrl();
		
		this.transport = Ajax.getTransport();
		this.Enabled = true;
		this.id = id;
		
		if(typeof(id)=="string"){
			Prado.CallbackRequest.requests[id] = this;
		}
		
		this.setOptions(Object.extend(
		{
			RequestTimeOut : 30000, // 30 second timeout.
			EnablePageStateUpdate : true,
			HasPriority : true,
			CausesValidation : true,
			ValidationGroup : null,
			PostInputs : true
		}, options || {}));

		this.ActiveControl = this.options;
	},
	
	/**
	 * Sets the request options
	 * @return {Array} request options.
	 */
	setOptions: function(options){
		
		this.options = {
			method:       'post',
			asynchronous: true,
			contentType:  'application/x-www-form-urlencoded',
			encoding:     'UTF-8',
			parameters:   '',
			evalJSON:     true,
			evalJS:       true
		};
		
		Object.extend(this.options, options || { });

		this.options.method = this.options.method.toLowerCase();
		if(Object.isString(this.options.parameters)){
			this.options.parameters = this.options.parameters.toQueryParams();
		}
	},
	
	/**
	 * Gets the url from the forms that contains the PRADO_PAGESTATE
	 * @return {String} callback url.
	 */
	getCallbackUrl : function()
	{
		return $('PRADO_PAGESTATE').form.action;
	},

	/**
	 * Sets the request parameter
	 * @param {Object} parameter value
	 */
	setCallbackParameter : function(value)
	{
		this.ActiveControl['CallbackParameter'] = value;
	},

	/**
	 * @return {Object} request paramater value.
	 */
	getCallbackParameter : function()
	{
		return this.ActiveControl['CallbackParameter'];
	},

	/**
	 * Sets the callback request timeout.
	 * @param {integer} timeout in  milliseconds
	 */
	setRequestTimeOut : function(timeout)
	{
		this.ActiveControl['RequestTimeOut'] = timeout;
	},

	/**
	 * @return {integer} request timeout in milliseconds
	 */
	getRequestTimeOut : function()
	{
		return this.ActiveControl['RequestTimeOut'];
	},

	/**
	 * Set true to enable validation on callback dispatch.
	 * @param {boolean} true to validate
	 */
	setCausesValidation : function(validate)
	{
		this.ActiveControl['CausesValidation'] = validate;
	},

	/**
	 * @return {boolean} validate on request dispatch
	 */
	getCausesValidation : function()
	{
		return this.ActiveControl['CausesValidation'];
	},

	/**
	 * Sets the validation group to validate during request dispatch.
	 * @param {string} validation group name
	 */
	setValidationGroup : function(group)
	{
		this.ActiveControl['ValidationGroup'] = group;
	},

	/**
	 * @return {string} validation group name.
	 */
	getValidationGroup : function()
	{
		return this.ActiveControl['ValidationGroup'];
	},

	/**
	 * Dispatch the callback request.
	 */
	dispatch : function()
	{
		//Logger.info("dispatching request");
		//trigger tinyMCE to save data.
		if(typeof tinyMCE != "undefined")
			tinyMCE.triggerSave();

		if(this.ActiveControl.CausesValidation && typeof(Prado.Validation) != "undefined")
		{
			var form =  this.ActiveControl.Form || Prado.Validation.getForm();
			if(Prado.Validation.validate(form,this.ActiveControl.ValidationGroup,this) == false)
				return false;
		}

		if(this.ActiveControl.onPreDispatch)
			this.ActiveControl.onPreDispatch(this,null);

		if(!this.Enabled)
			return;
	
		// Opera don't have onLoading/onLoaded state, so, simulate them just
		// before sending the request.
		if (Prototype.Browser.Opera)
		{
			if (this.ActiveControl.onLoading)
			{
				this.ActiveControl.onLoading(this,null);
				Ajax.Responders.dispatch('onLoading',this, this.transport,null);
			}
			if (this.ActiveControl.onLoaded)
			{
				this.ActiveControl.onLoaded(this,null);
				Ajax.Responders.dispatch('onLoaded',this, this.transport,null);
			}
		}
		
		var result;
		if(this.ActiveControl.HasPriority)
		{
			return Prado.CallbackRequest.enqueue(this);
			//return Prado.CallbackRequest.dispatchPriorityRequest(this);
		}
		else
			return Prado.CallbackRequest.dispatchNormalRequest(this);
	},

	abort : function()
	{
		return Prado.CallbackRequest.abortRequest(this.id);
	},

	/**
	 * Collects the form inputs, encode the parameters, and sets the callback
	 * target id. The resulting string is the request content body.
	 * @return string request body content containing post data.
	 */
	_getPostData : function()
	{
		var data = {};
		var callback = Prado.CallbackRequest;
		if(this.ActiveControl.PostInputs != false)
		{
			callback.PostDataLoaders.each(function(name)
			{
				$A(document.getElementsByName(name)).each(function(element)
				{
					//IE will try to get elements with ID == name as well.
					if(element.type && element.name == name)
					{
						var value = $F(element);
						if(typeof(value) != "undefined" && value != null)
							data[name] = value;
					}
				})
			})
		}
		if(typeof(this.ActiveControl.CallbackParameter) != "undefined")
			data[callback.FIELD_CALLBACK_PARAMETER] = callback.encode(this.ActiveControl.CallbackParameter);
		var pageState = $F(callback.FIELD_CALLBACK_PAGESTATE);
		if(typeof(pageState) != "undefined")
			data[callback.FIELD_CALLBACK_PAGESTATE] = pageState;
		data[callback.FIELD_CALLBACK_TARGET] = this.id;
		if(this.ActiveControl.EventTarget)
			data[callback.FIELD_POSTBACK_TARGET] = this.ActiveControl.EventTarget;
		if(this.ActiveControl.EventParameter)
			data[callback.FIELD_POSTBACK_PARAMETER] = this.ActiveControl.EventParameter;
		return $H(data).toQueryString();
	}
});

/**
 * Create a new callback request using default settings.
 * @param string callback handler unique ID.
 * @param mixed parameter to pass to callback handler on the server side.
 * @param function client side onSuccess event handler.
 * @param object additional request options.
 * @return boolean always false.
 */
Prado.Callback = function(UniqueID, parameter, onSuccess, options)
{
	var callback =
	{
		'CallbackParameter' : parameter || '',
		'onSuccess' : onSuccess || Prototype.emptyFunction
	};

	Object.extend(callback, options || {});

	var request = new Prado.CallbackRequest(UniqueID, callback);
	request.dispatch();
	return false;
};
