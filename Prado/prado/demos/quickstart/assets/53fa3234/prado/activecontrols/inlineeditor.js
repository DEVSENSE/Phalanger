Prado.WebUI.TInPlaceTextBox = Base.extend(
{
	constructor : function(options)
	{

		this.isSaving = false;
		this.isEditing = false;
		this.editField = null;
		this.readOnly = options.ReadOnly;

		this.options = Object.extend(
		{
			LoadTextFromSource : false,
			TextMode : 'SingleLine'

		}, options || {});
		this.element = $(this.options.ID);
		Prado.WebUI.TInPlaceTextBox.register(this);
		this.createEditorInput();
		this.initializeListeners();

		Prado.Registry.set(options.ID, this);
	},

	/**
	 * Initialize the listeners.
	 */
	initializeListeners : function()
	{
		this.onclickListener = this.enterEditMode.bindAsEventListener(this);
	    Event.observe(this.element, 'click', this.onclickListener);
	    if (this.options.ExternalControl)
	        // Issue 181
	        $(this.options.ExternalControl).stopObserving('click', this.onclickListener);
			Event.observe($(this.options.ExternalControl), 'click', this.onclickListener);
	},

	/**
	 * Changes the panel to an editable input.
	 * @param {Event} evt event source
	 */
	enterEditMode :  function(evt)
	{
	    if (this.isSaving || this.isEditing || this.readOnly) return;
	    this.isEditing = true;
		this.onEnterEditMode();
		this.createEditorInput();
		this.showTextBox();
		this.editField.disabled = false;
		if(this.options.LoadTextOnEdit)
			this.loadExternalText();
		Prado.Element.focus(this.editField);
		if (evt)
			Event.stop(evt);
    	return false;
	},

	exitEditMode : function(evt)
	{
		this.isEditing = false;
		this.isSaving = false;
		this.editField.disabled = false;
		this.element.innerHTML = this.editField.value;
		this.showLabel();
	},

	showTextBox : function()
	{
		Element.hide(this.element);
		Element.show(this.editField);
	},

	showLabel : function()
	{
		Element.show(this.element);
		Element.hide(this.editField);
	},

	/**
	 * Create the edit input field.
	 */
	createEditorInput : function()
	{
		if(this.editField == null)
			this.createTextBox();

		this.editField.value = this.getText();
	},

	loadExternalText : function()
	{
		this.editField.disabled = true;
		this.onLoadingText();
		var options = new Array('__InlineEditor_loadExternalText__', this.getText());
		var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
		request.setCausesValidation(false);
		request.setCallbackParameter(options);
		request.ActiveControl.onSuccess = this.onloadExternalTextSuccess.bind(this);
		request.ActiveControl.onFailure = this.onloadExternalTextFailure.bind(this);
		request.dispatch();
	},

	/**
	 * Create a new input textbox or textarea
	 */
	createTextBox : function()
	{
		var cssClass= this.element.className || '';
		var inputName = this.options.EventTarget;
		var options = {'className' : cssClass, name : inputName, id : this.options.TextBoxID};
		if(this.options.TextMode == 'SingleLine')
		{
			if(this.options.MaxLength > 0)
				options['maxlength'] = this.options.MaxLength;
			if(this.options.Columns > 0)
				options['size'] = this.options.Columns;
			this.editField = INPUT(options);
		}
		else
		{
			if(this.options.Rows > 0)
				options['rows'] = this.options.Rows;
			if(this.options.Columns > 0)
				options['cols'] = this.options.Columns;
			if(this.options.Wrap)
				options['wrap'] = 'off';
			this.editField = TEXTAREA(options);
		}

		this.editField.style.display="none";
		this.element.parentNode.insertBefore(this.editField,this.element)
        
        // Issue 181
        $(this.editField).stopObserving();
        
		//handle return key within single line textbox
		if(this.options.TextMode == 'SingleLine')
		{
			Event.observe(this.editField, "keydown", function(e)
			{
				 if(Event.keyCode(e) == Event.KEY_RETURN)
		        {
					var target = Event.element(e);
					if(target)
					{
						Event.fireEvent(target, "blur");
						Event.stop(e);
					}
				}
			});
		}

		Event.observe(this.editField, "blur", this.onTextBoxBlur.bind(this));
		Event.observe(this.editField, "keypress", this.onKeyPressed.bind(this));
	},

	/**
	 * @return {String} panel inner html text.
	 */
	getText: function()
	{
    	return this.element.innerHTML;
  	},

	/**
	 * Edit mode entered, calls optional event handlers.
	 */
	onEnterEditMode : function()
	{
		if(typeof(this.options.onEnterEditMode) == "function")
			this.options.onEnterEditMode(this,null);
	},

	onTextBoxBlur : function(e)
	{
		var text = this.element.innerHTML;
		if(this.options.AutoPostBack && text != this.editField.value)
		{
			if(this.isEditing)
				this.onTextChanged(text);
		}
		else
		{
			this.element.innerHTML = this.editField.value;
			this.isEditing = false;
			if(this.options.AutoHide)
				this.showLabel();
		}
	},

	onKeyPressed : function(e)
	{
		if (Event.keyCode(e) == Event.KEY_ESC)
		{
			this.editField.value = this.getText();
			this.isEditing = false;
			if(this.options.AutoHide)
				this.showLabel();
		}
		else if (Event.keyCode(e) == Event.KEY_RETURN && this.options.TextMode != 'MultiLine')
			Event.stop(e);
	},

	/**
	 * When the text input value has changed.
	 * @param {String} original text
	 */
	onTextChanged : function(text)
	{
		var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
		request.setCallbackParameter(text);
		request.ActiveControl.onSuccess = this.onTextChangedSuccess.bind(this);
		request.ActiveControl.onFailure = this.onTextChangedFailure.bind(this);
		if(request.dispatch())
		{
			this.isSaving = true;
			this.editField.disabled = true;
		}
	},

	/**
	 * When loading external text.
	 */
	onLoadingText : function()
	{
		//Logger.info("on loading text");
	},

	onloadExternalTextSuccess : function(request, parameter)
	{
		this.isEditing = true;
		this.editField.disabled = false;
		this.editField.value = this.getText();
		Prado.Element.focus(this.editField);
		if(typeof(this.options.onSuccess)=="function")
			this.options.onSuccess(sender,parameter);
	},

	onloadExternalTextFailure : function(request, parameter)
	{
		this.isSaving = false;
		this.isEditing = false;
		this.showLabel();
		if(typeof(this.options.onFailure)=="function")
			this.options.onFailure(sender,parameter);
	},

	/**
	 * Text change successfully.
	 * @param {Object} sender
	 * @param {Object} parameter
	 */
	onTextChangedSuccess : function(sender, parameter)
	{
		this.isSaving = false;
		this.isEditing = false;
		if(this.options.AutoHide)
			this.showLabel();
		this.element.innerHTML = parameter == null ? this.editField.value : parameter;
		this.editField.disabled = false;
		if(typeof(this.options.onSuccess)=="function")
			this.options.onSuccess(sender,parameter);
	},

	onTextChangedFailure : function(sender, parameter)
	{
		this.editField.disabled = false;
		this.isSaving = false;
		this.isEditing = false;
		if(typeof(this.options.onFailure)=="function")
			this.options.onFailure(sender,parameter);
	}
},
{
	textboxes : {},

	register : function(obj)
	{
		Prado.WebUI.TInPlaceTextBox.textboxes[obj.options.TextBoxID] = obj;
	},

	setDisplayTextBox : function(id,value)
	{
		var textbox = Prado.WebUI.TInPlaceTextBox.textboxes[id];
		if(textbox)
		{
			if(value)
				textbox.enterEditMode(null);
			else
			{
				textbox.exitEditMode(null);
			}
		}
	},

	setReadOnly : function(id, value)
	{
		var textbox = Prado.WebUI.TInPlaceTextBox.textboxes[id];
		if (textbox)
		{
			textbox.readOnly=value;
		}
	}
});