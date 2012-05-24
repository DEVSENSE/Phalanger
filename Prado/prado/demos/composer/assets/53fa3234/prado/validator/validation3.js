/**
 * Prado client-side javascript validation fascade.
 *
 * <p>There are 4 basic classes: {@link Prado.Validation}, 
 * {@link Prado.ValidationManager}, {@link Prado.WebUI.TValidationSummary}
 * and {@link Prado.WebUI.TBaseValidator}, 
 * that interact together to perform validation.
 * The {@link Prado.Validation} class co-ordinates together the
 * validation scheme and is responsible for maintaining references
 * to ValidationManagers.</p>
 *
 * <p>The {@link Prado.ValidationManager} class is responsible for 
 * maintaining refereneces
 * to individual validators, validation summaries and their associated
 * groupings.</p>
 *
 * <p>The {@link Prado.WebUI.TValidationSummary} takes care of displaying 
 * the validator error messages
 * as html output or an alert output.</p>
 *
 * <p>The {@link Prado.WebUI.TBaseValidator} is the base class for all 
 * validators and contains
 * methods to interact with the actual inputs, data type conversion.</p>
 *
 * <p>An instance of {@link Prado.ValidationManager} must be instantiated first for a
 * particular form before instantiating validators and summaries.</p>
 *
 * <p>Usage example: adding a required field to a text box input with
 * ID "input1" in a form with ID "form1".</p>
 * <pre>
 * &lt;script type="text/javascript" src="../prado.js"&gt;&lt;/script&gt;
 * &lt;script type="text/javascript" src="../validator.js"&gt;&lt;/script&gt;
 * &lt;form id="form1" action="..."&gt;
 * &lt;div&gt;
 * 	&lt;input type="text" id="input1" /&gt;
 *  &lt;span id="validator1" style="display:none; color:red"&gt;*&lt;/span&gt;
 *  &lt;input type="submit text="submit" /&gt;
 * &lt;script type="text/javascript"&gt;
 * new Prado.ValidationManager({FormID : 'form1'});
 * var options =
 * {
 *		ID :				'validator1',
 *		FormID :			'form1',
 *		ErrorMessage :		'*',
 *		ControlToValidate : 'input1'
 * }
 * new Prado.WebUI.TRequiredFieldValidator(options);
 * new Prado.WebUI.TValidationSummary({ID:'summary1',FormID:'form1'});
 *
 * //watch the form onsubmit event, check validators, stop if not valid.
 * Event.observe("form1", "submit" function(ev)
 * {
 * 	 if(Prado.WebUI.Validation.isValid("form1") == false)
 * 		Event.stop(ev);
 * });
 * &lt;/script&gt;
 * &lt;/div&gt;
 * &lt;/form&gt;
 * </pre>
 * 
 * @module validation
 */
 
Prado.Validation =  Class.create();

/**
 * Global Validation Object.
 * 
 * <p>To validate the inputs of a particular form, call
 * <code>{@link Prado.Validation.validate}(formID, groupID)</code>
 * where <tt>formID</tt> is the HTML form ID, and the optional
 * <tt>groupID</tt> if present will only validate the validators
 * in a particular group.</p>
 * <p>Use <code>{@link Prado.Validation.validateControl}(controlClientID)</code>
 * to trigger validation for a single control.</p>
 * 
 * @object {static} Prado.Validation
 */
Object.extend(Prado.Validation,
{
	/**
	 * Hash of registered validation managers
	 * @var managers
	 */
	managers : {},

	/**
	 * Validate the validators (those that <strong>DO NOT</strong>
	 * belong to a particular group) in the form specified by the
	 * <tt>formID</tt> parameter. If <tt>groupID</tt> is specified
	 * only validators belonging to that group will be validated.
	 * @function {boolean} ?
	 * @param {string} formID - ID of the form to validate
	 * @param {string} groupID - ID of the ValidationGroup to validate.
	 * @param {element} invoker - DOM element that calls for validation
	 * @returns true if validation succeeded
	 */
	validate : function(formID, groupID, invoker)
	{
		formID = formID || this.getForm();
		if(this.managers[formID])
		{
			return this.managers[formID].validate(groupID, invoker);
		}
		else
		{
			throw new Error("Form '"+formID+"' is not registered with Prado.Validation");
		}
	},

	/**
	 * Validate all validators of a specific control.
	 * @function {boolean} ?
	 * @param {string} id - ID of DOM element to validate 
	 * @return true if all validators are valid or no validators present, false otherwise.
	 */
    validateControl : function(id) 
    {
        var formId=this.getForm();

		if (this.managers[formId])
        {
            return this.managers[formId].validateControl(id);
        } else {
			throw new Error("A validation manager needs to be created first.");
        }
    },

	/**
	 * Return first registered form
	 * @function {string} ?
	 * @returns ID of first form.
	 */
	getForm : function()
	{
		var keys = $H(this.managers).keys();
		return keys[0];
	},

	/**
	 * Check if the validators are valid for a particular form (and group).
	 * The validators states will not be changed.
	 * The <tt>validate</tt> function should be called first.
	 * @function {boolean} ?
	 * @param {string} formID - ID of the form to validate
	 * @param {string} groupID - ID of the ValiationGroup to validate.
	 * @return true if form is valid
	 */
	isValid : function(formID, groupID)
	{
		formID = formID || this.getForm();
		if(this.managers[formID])
			return this.managers[formID].isValid(groupID);
		return true;
	},

	/**
	 * Reset the validators for a given group.
	 * The group is searched in the first registered form.
	 * @function ?
	 * @param {string} groupID - ID of the ValidationGroup to reset.
	 */
	reset : function(groupID)
	{
		var formID = this.getForm();
		if(this.managers[formID])
			this.managers[formID].reset(groupID);
	},

	/**
	 * Add a new validator to a particular form.
	 * @function {ValidationManager} ?
	 * @param {string} formID - ID of the form that the validator belongs to.
	 * @param {TBaseValidator} validator - Validator object
	 * @return ValidationManager for the form
	 */
	addValidator : function(formID, validator)
	{
		if(this.managers[formID])
			this.managers[formID].addValidator(validator);
		else
			throw new Error("A validation manager for form '"+formID+"' needs to be created first.");
		return this.managers[formID];
	},

	/**
	 * Add a new validation summary.
	 * @function {ValidationManager} ?
	 * @param {string} formID - ID of the form that the validation summary belongs to.
	 * @param {TValidationSummary} validator - TValidationSummary object
	 * @return ValidationManager for the form
	 */
	addSummary : function(formID, validator)
	{
		if(this.managers[formID])
			this.managers[formID].addSummary(validator);
		else
			throw new Error("A validation manager for form '"+formID+"' needs to be created first.");
		return this.managers[formID];
	},

	setErrorMessage : function(validatorID, message)
	{
		$H(Prado.Validation.managers).each(function(manager)
		{
			manager[1].validators.each(function(validator)
			{
				if(validator.options.ID == validatorID)
				{
					validator.options.ErrorMessage = message;
					$(validatorID).innerHTML = message;
				}
			});
		});
	}
});

/**
 * Manages validators for a particular HTML form.
 *  
 * <p>The manager contains references to all the validators
 * summaries, and their groupings for a particular form.
 * Generally, {@link Prado.Validation} methods should be called rather
 * than calling directly the ValidationManager.</p>
 * 
 * @class Prado.ValidationManager
 */
Prado.ValidationManager = Class.create();
Prado.ValidationManager.prototype =
{
	/**
	 * Hash of registered validators by control's clientID
	 * @var controls
	 */
    controls: {},

	/**
	 * Initialize ValidationManager.
	 * @constructor {protected} ?
	 * @param {object} options - Options for initialization
	 * @... {string} FormID - ID of form of this manager
	 */
	initialize : function(options)
	{
		if(!Prado.Validation.managers[options.FormID])
		{
			/**
			 * List of validators
			 * @var {TBaseValidator[]} validators
			 */
			this.validators = []; 
			/**
			 * List of validation summaries
			 * @var {TValidationSummary[]} summaries
			 */
			this.summaries = []; 
			/**
			 * List of ValidationGroups
			 * @var {string[]} groups
			 */
			this.groups = []; 
			/**
			 * Options of this ValidationManager
			 * @var {object} options 
			 */
			this.options = {};

			this.options = options;

			Prado.Validation.managers[options.FormID] = this;
		}
		else
		{
			var manager = Prado.Validation.managers[options.FormID];
			this.validators = manager.validators;
			this.summaries = manager.summaries;
			this.groups = manager.groups;
			this.options = manager.options;
		}
	},

	/**
	 * Reset all validators in the given group.
	 * If group is null, validators without a group are used.
	 * @function ?
	 * @param {string} group - ID of ValidationGroup
	 */
	reset : function(group)
	{
		this.validatorPartition(group)[0].invoke('reset');
		this.updateSummary(group, true);
	},

	/**
	 * Validate the validators managed by this validation manager.
	 * If group is set, only validate validators in that group.
	 * @function {boolean} ?
	 * @param {optional string} group - ID of ValidationGroup
	 * @param {element} source - DOM element that calls for validation
	 * @return true if all validators are valid, false otherwise.
	 */
	validate : function(group, source)
	{
		var partition = this.validatorPartition(group);
		var valid = partition[0].invoke('validate', source).all();
		this.focusOnError(partition[0]);
		partition[1].invoke('hide');
		this.updateSummary(group, true);
		return valid;
	},

	/**
	 * Perform validation for all validators of a single control.
	 * @function {boolean} ?
	 * @param {string} id - ID of DOM element to validate 
	 * @return true if all validators are valid or no validators present, false otherwise.
	 */
    validateControl : function (id) 
    {
        return this.controls[id] ? this.controls[id].invoke('validate',null).all() : true;
    },

	/**
	 * Focus on the first validator that is invalid and options.FocusOnError is true.
	 * @function ?
	 * @param {TBaseValidator[]} validators - Array of validator objects
	 */
	focusOnError : function(validators)
	{
		for(var i = 0; i < validators.length; i++)
		{
			if(!validators[i].isValid && validators[i].options.FocusOnError)
				return Prado.Element.focus(validators[i].options.FocusElementID);
		}
	},

	/**
	 * Get all validators in a group and all other validators.
	 * Returns an array with two arrays of validators. The first 
	 * array contains all validators in the group if group is given, 
	 * otherwhise all validators without a group. The second array
	 * contains all other validators.
	 * @function {[ TBaseValidator[] , TBaseValidator[] ]} ?
	 * @param {optional string} group - ID of ValidationGroup
	 * @return Array with two arrays of validators.
	 */
	validatorPartition : function(group)
	{
		return group ? this.validatorsInGroup(group) : this.validatorsWithoutGroup();
	},

	/**
	 * Get all validators in a group.
	 * Returns an array with two arrays of validators. The first 
	 * array contains all validators in the group. The second array
	 * contains all other validators.
	 * @function {[ TBaseValidator[] , TBaseValidator[] ]} ?
	 * @param {optional string} groupID - ID of ValidationGroup
	 * @return Array with two arrays of validators.
	 */
	validatorsInGroup : function(groupID)
	{
		if(this.groups.include(groupID))
		{
			return this.validators.partition(function(val)
			{
				return val.group == groupID;
			});
		}
		else
			return [[],[]];
	},

	/**
	 * Get all validators without a group.
	 * Returns an array with two arrays of validators. The first 
	 * array contains all validators without a group. The second 
	 * array contains all other validators.
	 * @function {[ TBaseValidator[] , TBaseValidator[] ]} ?
	 * @return Array with two arrays of validators: Array[0] has all
	 * validators without a group, Array[1] all other validators.
	 */
	validatorsWithoutGroup : function()
	{
		return this.validators.partition(function(val)
		{
			return !val.group;
		});
	},

	/**
	 * Get the state of validators.
	 * If group is set, only validators in that group are checked. 
	 * Otherwhise only validators without a group are checked.
	 * @function {booelan} ?
	 * @param {optional string} group - ID of ValidationGroup
	 * @return true if all validators (in a group, if supplied) are valid.
	 */
	isValid : function(group)
	{
		return this.validatorPartition(group)[0].pluck('isValid').all();
	},

	/**
	 * Add a validator to this manager.
	 * @function ?
	 * @param {TBaseValidator} validator - Validator object
	 */
	addValidator : function(validator)
	{
		// Remove previously registered validator with same ID
        // to prevent stale validators created by AJAX updates
        this.removeValidator(validator);

		this.validators.push(validator);
		if(validator.group && !this.groups.include(validator.group))
			this.groups.push(validator.group);

        if (typeof this.controls[validator.control.id] === 'undefined')
            this.controls[validator.control.id] = Array();
        this.controls[validator.control.id].push(validator);
	},

	/**
	 * Add a validation summary.
	 * @function ?
	 * @param {TValidationSummary} summary - Validation summary.
	 */
	addSummary : function(summary)
	{
		this.summaries.push(summary);
	},

	/**
	 * Remove a validator from this manager
	 * @function ?
	 * @param {TBaseValidator} validator - Validator object
	 */
    removeValidator : function(validator)
    {
		this.validators = this.validators.reject(function(v)
		{
			return (v.options.ID==validator.options.ID);
		});
        if (this.controls[validator.control.id])
            this.controls[validator.control.id].reject( function(v)
            {
                return (v.options.ID==validator.options.ID)
            });
    },

	/**
	 * Gets validators with errors.
	 * If group is set, only validators in that group are returned. 
	 * Otherwhise only validators without a group are returned.
	 * @function {TBaseValidator[]} ?
	 * @param {optional string} group - ID of ValidationGroup
	 * @return array list of validators with error.
	 */
	getValidatorsWithError : function(group)
	{
		return this.validatorPartition(group)[0].findAll(function(validator)
		{
			return !validator.isValid;
		});
	},

	/**
	 * Update the summary of a particular group.
	 * If group is set, only the summary for validators in that 
	 * group is updated. Otherwhise only the summary for validators 
	 * without a group is updated.
	 * @function ?
	 * @param {optional string} group - ID of ValidationGroup
	 * @param {boolean} refresh - Wether the summary should be refreshed
	 */
	updateSummary : function(group, refresh)
	{
		var validators = this.getValidatorsWithError(group);
		this.summaries.each(function(summary)
		{
			var inGroup = group && summary.group == group;
			var noGroup = !group && !summary.group;
			if(inGroup || noGroup)
				summary.updateSummary(validators, refresh);
			else
				summary.hideSummary(true);
		});
	}
};

/**
 * TValidationSummary displays a summary of validation errors.
 * 
 * <p>The summary is displayed inline on a Web page,
 * in a message box, or both. By default, a validation summary will collect
 * <tt>ErrorMessage</tt> of all failed validators on the page. If
 * <tt>ValidationGroup</tt> is not empty, only those validators who belong
 * to the group will show their error messages in the summary.</p>
 *
 * <p>The summary can be displayed as a list, as a bulleted list, or as a single
 * paragraph based on the <tt>DisplayMode</tt> option.
 * The messages shown can be prefixed with <tt>HeaderText</tt>.</p>
 *
 * <p>The summary can be displayed on the Web page and in a message box by setting
 * the <tt>ShowSummary</tt> and <tt>ShowMessageBox</tt>
 * options, respectively.</p>
 * 
 * @class Prado.WebUI.TValidationSummary
 */
Prado.WebUI.TValidationSummary = Class.create();
Prado.WebUI.TValidationSummary.prototype =
{
	/**
	 * Initialize TValidationSummary.
	 * @constructor {protected} ?
	 * @param {object} options - Options for initialization
	 * @... {string} ID - ID of validation summary element
	 * @... {string} FormID - ID of form of this manager
	 * @... {optional string} ValidationGroup - ID of ValidationGroup.
	 * @... {optional boolean} ShowMessageBox - true to show the summary in an alert box.
	 * @... {optional boolean} ShowSummary - true to show the inline summary.
	 * @... {optional string} HeaderText - Summary header text
	 * @... {optional string} DisplayMode - Summary display style, 'BulletList', 'List', 'SingleParagraph'
	 * @... {optional boolean} Refresh - true to update the summary upon validator state change.
	 * @... {optional string} Display - Display mode, 'None', 'Fixed', 'Dynamic'.
	 * @... {optional boolean} ScrollToSummary - true to scroll to the validation summary upon refresh.
	 * @... {optional function} OnHideSummary - Called on hide event.
	 * @... {optional function} OnShowSummary - Called on show event.
	 */
	initialize : function(options)
	{
		/**
		 * Validator options
		 * @var {object} options 
		 */
		this.options = options;
		/**
		 * ValidationGroup
		 * @var {string} group
		 */
		this.group = options.ValidationGroup;
		/**
		 * Summary DOM element
		 * @var {element} messages
		 */
		this.messages = $(options.ID);
		Prado.Registry.set(options.ID, this);
		if(this.messages)
		{
			/**
			 * Current visibility state of summary 
			 * @var {boolean} visible 
			 */
			this.visible = this.messages.style.visibility != "hidden"
			this.visible = this.visible && this.messages.style.display != "none";
			Prado.Validation.addSummary(options.FormID, this);
		}
	},

	/**
	 * Update the validation summary.
	 * @function ?
	 * @param {TBaseValidator[]} validators - List of validators that failed validation.
	 * @param {boolean} update - true if visible summary should be updated
	 */
	updateSummary : function(validators, update)
	{
		if(validators.length <= 0)
		{
			if(update || this.options.Refresh != false)
			{
				return this.hideSummary(validators);
			}
			return;
		}

		var refresh = update || this.visible == false || this.options.Refresh != false;
		// Also, do not refresh summary if at least 1 validator is waiting for callback response.
		// This will avoid the flickering of summary if the validator passes its test
		refresh = refresh && validators.any(function(v) { return !v.requestDispatched; });

		if(this.options.ShowSummary != false && refresh)
		{
			this.updateHTMLMessages(this.getMessages(validators));
			this.showSummary(validators);
		}

		if(this.options.ScrollToSummary != false && refresh)
			window.scrollTo(this.messages.offsetLeft-20, this.messages.offsetTop-20);

		if(this.options.ShowMessageBox == true && refresh)
		{
			this.alertMessages(this.getMessages(validators));
			this.visible = true;
		}
	},

	/**
	 * Display the validator error messages as inline HTML.
	 * @function ?
	 * @param {string[]} messages - Array of error messages.
	 */
	updateHTMLMessages : function(messages)
	{
		while(this.messages.childNodes.length > 0)
			this.messages.removeChild(this.messages.lastChild);
		this.messages.insert(this.formatSummary(messages));
	},

	/**
	 * Display the validator error messages as an alert box.
	 * @function ?
	 * @param {string[]} messages - Array of error messages.
	 */
	alertMessages : function(messages)
	{
		var text = this.formatMessageBox(messages);
		setTimeout(function(){ alert(text); },20);
	},

	/**
	 * Get messages from validators.
	 * @function {string[]} ? 
	 * @param {TBaseValidator[]} validators - Array of validators.
	 * @return Array of validator error messages.
	 */
	getMessages : function(validators)
	{
		var messages = [];
		validators.each(function(validator)
		{
			var message = validator.getErrorMessage();
			if(typeof(message) == 'string' && message.length > 0)
				messages.push(message);
		})
		return messages;
	},

	/**
	 * Hide the validation summary.
	 * @function ?
	 * @param {TBaseValidator[]} validators - Array of validators.
	 */
	hideSummary : function(validators)
	{	if(typeof(this.options.OnHideSummary) == "function")
		{
			this.messages.style.visibility="visible";
			this.options.OnHideSummary(this,validators)
		}
		else
		{
			this.messages.style.visibility="hidden";
			if(this.options.Display == "None" || this.options.Display == "Dynamic")
				this.messages.hide();
		}
		this.visible = false;
	},

	/**
	 * Shows the validation summary.
	 * @function ?
	 * @param {TBaseValidator[]} validators - Array of validators.
	 */
	showSummary : function(validators)
	{
		this.messages.style.visibility="visible";
		if(typeof(this.options.OnShowSummary) == "function")
			this.options.OnShowSummary(this,validators);
		else
			this.messages.show();
		this.visible = true;
	},

	/**
	 * Return the format parameters for the summary.
	 * @function {object} ?
	 * @param {string} type - Format type: "List", "SingleParagraph", "HeaderOnly" or "BulletList" (default)
	 * @return Object with format parameters:
	 * @... {string} header - Text for header
	 * @... {string} first - Text to prepend before message list
	 * @... {string} pre - Text to prepend before each message
	 * @... {string} post - Text to append after each message
	 * @... {string} first - Text to append after message list
	 */
	formats : function(type)
	{
		switch(type)
		{
			case "SimpleList":
				return { header : "<br />", first : "", pre : "", post : "<br />", last : ""};
			case "SingleParagraph":
				return { header : " ", first : "", pre : "", post : " ", last : "<br />"};
			case "HeaderOnly":
				return { header : "", first : "<!--", pre : "", post : "", last : "-->"};
			case "BulletList":
			default:
				return { header : "", first : "<ul>", pre : "<li>", post : "</li>", last : "</ul>"};
		}
	},

	/**
	 * Format the message summary.
	 * @function {string} ?
	 * @param {string[]} messages - Array of error messages.
	 * @return Formatted message
	 */
	formatSummary : function(messages)
	{
		var format = this.formats(this.options.DisplayMode);
		var output = this.options.HeaderText ? this.options.HeaderText + format.header : "";
		output += format.first;
		messages.each(function(message)
		{
			output += message.length > 0 ? format.pre + message + format.post : "";
		});
//		for(var i = 0; i < messages.length; i++)
	//		output += (messages[i].length>0) ? format.pre + messages[i] + format.post : "";
		output += format.last;
		return output;
	},
	/**
	 * Format the message alert box.
	 * @function {string} ?
	 * @param {string[]} messages - Array of error messages.
	 * @return Formatted message for alert
	 */
	formatMessageBox : function(messages)
	{
		if(this.options.DisplayMode == 'HeaderOnly' && this.options.HeaderText)
			return this.options.HeaderText;
		
		var output = this.options.HeaderText ? this.options.HeaderText + "\n" : "";
		for(var i = 0; i < messages.length; i++)
		{
			switch(this.options.DisplayMode)
			{
				case "List":
					output += messages[i] + "\n";
					break;
				case "BulletList":
                default:
					output += "  - " + messages[i] + "\n";
					break;
				case "SingleParagraph":
					output += messages[i] + " ";
					break;
			}
		}
		return output;
	}
};

/**
 * TBaseValidator serves as the base class for validator controls.
 *
 * <p>Validation is performed when a postback control, such as a TButton,
 * a TLinkButton or a TTextBox (under AutoPostBack mode) is submitting
 * the page and its <tt>CausesValidation</tt> option is true.
 * The input control to be validated is specified by <tt>ControlToValidate</tt>
 * option.</p>
 * 
 * @class Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TBaseValidator = Class.create();
Prado.WebUI.TBaseValidator.prototype =
{
	/**
	 * Initialize TBaseValidator.
	 * @constructor {protected} ?
	 * @param {object} options - Options for initialization.
	 * @... {string} ID - ID of validator
	 * @... {string} FormID - ID of form of this manager.
	 * @... {string} ControlToValidate - ID of control to validate.
	 * @... {optional string} InitialValue - Initial value of control to validate.
	 * @... {optional string} ErrorMessage - Validation error message.
	 * @... {optional string} ValidationGroup - ID of ValidationGroup.
	 * @... {optional string} Display - Display mode, 'None', 'Fixed', 'Dynamic'.
	 * @... {optional boolean} ObserveChanges - True to observer changes of ControlToValidate
	 * @... {optional boolean} FocusOnError - True to focus on validation error.
	 * @... {optional string} FocusElementID - ID of element to focus on error.
	 * @... {optional string} ControlCssClass - Css class to use on ControlToValidate on error
	 * @... {optional function} OnValidate - Called immediately after validation.
	 * @... {optional function} OnValidationSuccess - Called after successful validation.
	 * @... {optional function} OnValidationError - Called after validation error.
	 */
	initialize : function(options)
	{
	/*	options.OnValidate = options.OnValidate || Prototype.emptyFunction;
		options.OnSuccess = options.OnSuccess || Prototype.emptyFunction;
		options.OnError = options.OnError || Prototype.emptyFunction;
	*/

		/**
		 * Wether the validator is enabled (default true)
		 * @var {boolean} enabled
		 */
		this.enabled = options.Enabled;
		/**
		 * Visibility state of validator(default false)
		 * @var {boolean} visible
		 */
		this.visible = false;
		/**
		 * State of validation (default true)
		 * @var {boolean} isValid
		 */
		this.isValid = true;
		/**
		 * DOM elements that are observed by this validator
		 * @var {private element[]} _isObserving
		 */
		this._isObserving = {};
		/**
		 * ValidationGroup
		 * @var {string} group
		 */
		this.group = null;
		/**
		 * Wether a request was dispatched (default false)
		 * @var {boolean} requestDispatched
		 */
		this.requestDispatched = false;

		/**
		 * Validator options
		 * @var {object} options 
		 */
		this.options = options;
		/**
		 * DOM element of control to validate
		 * @var {element} control
		 */
		this.control = $(options.ControlToValidate);
		/**
		 * DOM element of validator
		 * @var {element} message
		 */
		this.message = $(options.ID);

		Prado.Registry.set(options.ID, this);
		if(this.control && this.message)
		{
			this.group = options.ValidationGroup;

			/**
			 * ValidationManager of this validator
			 * @var {ValidationManager} manager
			 */
			this.manager = Prado.Validation.addValidator(options.FormID, this);
		}
	},

	/**
	 * Get error message.
	 * @function {string} ?
	 * @return Validation error message.
	 */
	getErrorMessage : function()
	{
		return this.options.ErrorMessage;
	},

	/**
	 * Update the validator.
	 * Updating the validator control will set the validator
	 * <tt>visible</tt> property to true.
	 * @function ?
	 */
	updateControl: function()
	{
		this.refreshControlAndMessage();

		//if(this.options.FocusOnError && !this.isValid )
		//	Prado.Element.focus(this.options.FocusElementID);

		this.visible = true;
	},

	/**
	 * Updates span and input CSS class.
	 * @function ?
	 */
	refreshControlAndMessage : function()
	{
		this.visible = true;
		if(this.message)
		{
			if(this.options.Display == "Dynamic")
			{
				var msg=this.message;
				this.isValid ? setTimeout(function() { msg.hide(); }, 250) : msg.show();
			}
			this.message.style.visibility = this.isValid ? "hidden" : "visible";
		}
		if(this.control)
			this.updateControlCssClass(this.control, this.isValid);
	},

	/**
	 * Update CSS class of control to validate.
	 * Add a css class to the input control if validator is invalid,
	 * removes the css class if valid.
	 * @function ?
	 * @param {element} control - DOM element of control to validate
	 * @param {boolean} valid - Validation state of control
	 */
	updateControlCssClass : function(control, valid)
	{
		var CssClass = this.options.ControlCssClass;
		if(typeof(CssClass) == "string" && CssClass.length > 0)
		{
			if(valid)
			{
				if (control.lastValidator == this.options.ID)
				{
					control.lastValidator = null;
					control.removeClassName(CssClass);
				}
			}
			else
			{
				control.lastValidator = this.options.ID;
				control.addClassName(CssClass);
			}
		}
	},

	/**
	 * Hide the validator messages and remove any validation changes.
	 * @function ?
	 */
	hide : function()
	{
		this.reset();
		this.visible = false;
	},

	/**
	 * Reset validator.
	 * Sets isValid = true and updates the validator display.
	 * @function ?
	 */
	reset : function()
	{
		this.isValid = true;
		this.updateControl();
	},

	/**
	 * Perform validation.
	 * Calls evaluateIsValid() function to set the value of isValid property.
	 * Triggers onValidate event and onSuccess or onError event.
	 * @function {boolean} ?
	 * @param {element} invoker - DOM element that triggered validation
	 * @return Valdation state of control.
	 */
	validate : function(invoker)
	{
		//try to find the control.
		if(!this.control)
			this.control = $(this.options.ControlToValidate);

		if(!this.control || this.control.disabled)
		{
			this.isValid = true;
			return this.isValid;
		}

		if(typeof(this.options.OnValidate) == "function")
		{
			if(this.requestDispatched == false)
				this.options.OnValidate(this, invoker);
		}

		if(this.enabled && !this.control.getAttribute('disabled'))
			this.isValid = this.evaluateIsValid();
		else
			this.isValid = true;

		this.updateValidationDisplay(invoker);
		this.observeChanges(this.control);

		return this.isValid;
	},

	/**
	 * Update validation display.
	 * Updates the validation messages and the control to validate.
	 * @param {element} invoker - DOM element that triggered validation
	 */
	updateValidationDisplay : function(invoker)
	{
		if(this.isValid)
		{
			if(typeof(this.options.OnValidationSuccess) == "function")
			{
				if(this.requestDispatched == false)
				{
					this.refreshControlAndMessage();
					this.options.OnValidationSuccess(this, invoker);
				}
			}
			else
				this.updateControl();
		}
		else
		{
			if(typeof(this.options.OnValidationError) == "function")
			{
				if(this.requestDispatched == false)
				{
					this.refreshControlAndMessage();
					this.options.OnValidationError(this, invoker)
				}
			}
			else
				this.updateControl();
		}
	},

	/**
	 * Add control to observe for changes.
	 * Re-validates upon change. If the validator is not visible, 
	 * no updates are propagated.
	 * @function ?
	 * @param {element} control - DOM element of control to observe
	 */
	observeChanges : function(control)
	{
		if(!control) return;

		var canObserveChanges = this.options.ObserveChanges != false;
		var currentlyObserving = this._isObserving[control.id+this.options.ID];

		if(canObserveChanges && !currentlyObserving)
		{
			var validator = this;

			Event.observe(control, 'change', function()
			{
				if(validator.visible)
				{
					validator.validate();
					validator.manager.updateSummary(validator.group);
				}
			});
			this._isObserving[control.id+this.options.ID] = true;
		}
	},

	/**
	 * Trim a string.
	 * @function {string} ?
	 * @param {string} value - String that should be trimmed.
	 * @return Trimmed string, empty string if value is not string.
	 */
	trim : function(value)
	{
		return typeof(value) == "string" ? value.trim() : "";
	},

	/**
	 * Convert the value to a specific data type.
	 * @function {mixed|null} ?
	 * @param {string} dataType - Data type: "Integer", "Double", "Date" or "String"
	 * @param {mixed} value - Value to convert.
	 * @return Converted data value.
	 */
	convert : function(dataType, value)
	{
		if(typeof(value) == "undefined")
			value = this.getValidationValue();
		var string = new String(value);
		switch(dataType)
		{
			case "Integer":
				return string.toInteger();
			case "Double" :
			case "Float" :
				return string.toDouble(this.options.DecimalChar);
			case "Date":
				if(typeof(value) != "string")
					return value;
				else
				{
					var value = string.toDate(this.options.DateFormat);
					if(value && typeof(value.getTime) == "function")
						return value.getTime();
					else
						return null;
				}
			case "String":
				return string.toString();
		}
		return value;
	},

	/**
	 * Get value that should be validated.
	 * The ControlType property comes from TBaseValidator::getClientControlClass()
	 * Be sure to update the TBaseValidator::$_clientClass if new cases are added.
	 * @function {mixed} ?
	 * @param {optional element} control - Control to get value from (default: this.control) 
	 * @return Control value to validate
	 */
	 getRawValidationValue : function(control)
	 {
	 	if(!control)
	 		control = this.control
	 	switch(this.options.ControlType)
	 	{
	 		case 'TDatePicker':
	 			if(control.type == "text")
	 			{
	 				var value = this.trim($F(control));

					if(this.options.DateFormat)
	 				{
	 					var date = value.toDate(this.options.DateFormat);
	 					return date == null ? value : date;
	 				}
	 				else
		 				return value;
	 			}
	 			else
	 			{
	 				this.observeDatePickerChanges();

	 				return Prado.WebUI.TDatePicker.getDropDownDate(control);//.getTime();
	 			}
	 		case 'THtmlArea':
	 			if(typeof tinyMCE != "undefined")
					tinyMCE.triggerSave();
				return $F(control);
			case 'TRadioButton':
				if(this.options.GroupName)
					return this.getRadioButtonGroupValue();
	 		default:
	 			if(this.isListControlType())
	 				return this.getFirstSelectedListValue();
	 			else
		 			return $F(control);
	 	}
	 },
	
	/**
	 * Get a trimmed value that should be validated.
	 * The ControlType property comes from TBaseValidator::getClientControlClass()
	 * Be sure to update the TBaseValidator::$_clientClass if new cases are added.
	 * @function {mixed} ?
	 * @param {optional element} control - Control to get value fron (default: this.control)
	 * @return Control value to validate
	 */
	 getValidationValue : function(control)
	 {
	 	var value = this.getRawValidationValue(control);
		if(!control)
			control = this.control
		switch(this.options.ControlType)
		{
			case 'TDatePicker':
				return value;
			case 'THtmlArea':
				return this.trim(value);
			case 'TRadioButton':
				return value;
			default:
				if(this.isListControlType())
					return value;
				else
					return this.trim(value);
		}
	 },

	/**
	 * Get value of radio button group
	 * @function {string} ?
	 * @return Value of a radio button group
	 */
	getRadioButtonGroupValue : function()
	{
		var name = this.control.name;
		var value = "";
		$A(document.getElementsByName(name)).each(function(el)
		{
			if(el.checked)
				value =  el.value;
		});
		return value;
	},

	 /**
	  * Observe changes in the drop down list date picker, IE only.
	  * @function ?
	  */
	 observeDatePickerChanges : function()
	 {
	 	if(Prado.Browser().ie)
	 	{
	 		var DatePicker = Prado.WebUI.TDatePicker;
	 		this.observeChanges(DatePicker.getDayListControl(this.control));
			this.observeChanges(DatePicker.getMonthListControl(this.control));
			this.observeChanges(DatePicker.getYearListControl(this.control));
	 	}
	 },

	/**
	 * Gets number of selections and their values.
	 * @function {object} ?
	 * @param {element[]} elements - Elements to get values from.
	 * @param {string} initialValue - Initial value of element
	 * @return Object:
	 * @... {mixed[]} values - Array of selected values
	 * @... {int} checks - Number of selections
	 */
	getSelectedValuesAndChecks : function(elements, initialValue)
	{
		var checked = 0;
		var values = [];
		var isSelected = this.isCheckBoxType(elements[0]) ? 'checked' : 'selected';
		elements.each(function(element)
		{
			if(element[isSelected] && element.value != initialValue)
			{
				checked++;
				values.push(element.value);
			}
		});
		return {'checks' : checked, 'values' : values};
	},

	/**
	 * Get list elements of TCheckBoxList or TListBox.
	 * Gets an array of the list control item input elements, for TCheckBoxList
	 * checkbox input elements are returned, for TListBox HTML option elements 
	 * are returned.
	 * @function {element[]} ?
	 * @return Array of list control option DOM elements.
	 */
	getListElements : function()
	{
		switch(this.options.ControlType)
		{
			case 'TCheckBoxList': case 'TRadioButtonList':
				var elements = [];
				for(var i = 0; i < this.options.TotalItems; i++)
				{
					var element = $(this.options.ControlToValidate+"_c"+i);
					if(this.isCheckBoxType(element))
						elements.push(element);
				}
				return elements;
			case 'TListBox':
				var elements = [];
				var element = $(this.options.ControlToValidate);
				var type;
				if(element && (type = element.type.toLowerCase()))
				{
					if(type == "select-one" || type == "select-multiple")
						elements = $A(element.options);
				}
				return elements;
			default:
				return [];
		}
	},

	/**
	 * Check if control is of type checkbox or radio.
	 * @function {boolean} ?
	 * @param {element} element - DOM element to check.
	 * @return True if element is of checkbox or radio type.
	 */
	isCheckBoxType : function(element)
	{
		if(element && element.type)
		{
			var type = element.type.toLowerCase();
			return type == "checkbox" || type == "radio";
		}
		return false;
	},

	/**
	 * Check if control to validate is a TListControl type.
	 * @function {boolean} ?
	 * @return True if control to validate is a TListControl type.
	 */
	isListControlType : function()
	{
		var list = ['TCheckBoxList', 'TRadioButtonList', 'TListBox'];
		return list.include(this.options.ControlType);
	},

	/**
	 * Get first selected list value or initial value if none found.
	 * @function {string} ?
	 * @return First selected list value, initial value if none found.
	 */
	getFirstSelectedListValue : function()
	{
		var initial = "";
		if(typeof(this.options.InitialValue) != "undefined")
			initial = this.options.InitialValue;
		var elements = this.getListElements();
		var selection = this.getSelectedValuesAndChecks(elements, initial);
		return selection.values.length > 0 ? selection.values[0] : initial;
	}
}


/**
 * TRequiredFieldValidator makes the associated input control a required field.
 * 
 * <p>The input control fails validation if its value does not change from
 * the <tt>InitialValue</tt> option upon losing focus.</p>
 * 
 * @class Prado.WebUI.TRequiredFieldValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TRequiredFieldValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if the input value is not empty nor equal to the initial value.
	 */
	evaluateIsValid : function()
	{
    	var a = this.getValidationValue();
    	var b = this.trim(this.options.InitialValue);
    	return(a != b);
	}
});


/**
 * TCompareValidator compares the value entered by the user into an input
 * control with the value entered into another input control or a constant value.
 * 
 * <p>To compare the associated input control with another input control,
 * set the <tt>ControlToCompare</tt> option to the ID path
 * of the control to compare with. To compare the associated input control with
 * a constant value, specify the constant value to compare with by setting the
 * <tt>ValueToCompare</tt> option.</p>
 *
 * <p>The <tt>DataType</tt> property is used to specify the data type
 * of both comparison values. Both values are automatically converted to this data
 * type before the comparison operation is performed. The following value types are supported:
 * - <b>Integer</b> A 32-bit signed integer data type.
 * - <b>Float</b> A double-precision floating point number data type.
 * - <b>Date</b> A date data type. The format can be set by the <tt>DateFormat</tt> option.
 * - <b>String</b> A string data type.</p>
 *
 * Use the <tt>Operator</tt> property to specify the type of comparison
 * to perform. Valid operators include Equal, NotEqual, GreaterThan, GreaterThanEqual,
 * LessThan and LessThanEqual.
 * 
 * @class Prado.WebUI.TCompareValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TCompareValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Additional constructor options.
	 * @constructor initialize
	 * @param {object} options - Additional constructor options:
	 * @... {string} ControlToCompare - Control with compare value.
	 * @... {string} ValueToCompare - Value to compare.
	 * @... {string} Operator - Type of comparison: "Equal", "NotEqual", "GreaterThan",
	 *   "GreaterThanEqual", "LessThan" or "LessThanEqual".
	 * @... {string} Type - Type of values: "Integer", "Float", "Date" or "String".
	 * @... {string} DateFormat - Valid date format.
	 */

	//_observingComparee : false,

	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if comparision condition is met.
	 */
	evaluateIsValid : function()
	{
		var value = this.getValidationValue();
	    if (value.length <= 0)
	    	return true;

    	var comparee = $(this.options.ControlToCompare);

		if(comparee)
			var compareTo = this.getValidationValue(comparee);
		else
			var compareTo = this.options.ValueToCompare || "";

	    var isValid =  this.compare(value, compareTo);

		if(comparee)
		{
			this.updateControlCssClass(comparee, isValid);
			this.observeChanges(comparee);
		}
		return isValid;
	},

	/**
	 * Compare two operands.
	 * The operand values are casted to type defined
	 * by <tt>DataType</tt> option. False is returned if the first
	 * operand converts to null. Returns true if the second operand
	 * converts to null. The comparision is done based on the
	 * <tt>Operator</tt> option.
	 * @function ?
	 * @param {mixed} operand1 - First operand.
	 * @param {mixed} operand2 - Second operand.
	 */
	compare : function(operand1, operand2)
	{
		var op1, op2;
		if((op1 = this.convert(this.options.DataType, operand1)) == null)
			return false;
		if ((op2 = this.convert(this.options.DataType, operand2)) == null)
        	return true;
    	switch (this.options.Operator)
		{
	        case "NotEqual":
	            return (op1 != op2);
	        case "GreaterThan":
	            return (op1 > op2);
	        case "GreaterThanEqual":
	            return (op1 >= op2);
	        case "LessThan":
	            return (op1 < op2);
	        case "LessThanEqual":
	            return (op1 <= op2);
	        default:
	            return (op1 == op2);
	    }
	}
});

/**
 * TCustomValidator performs user-defined client-side validation on an
 * input component.
 *
 * <p>To create a client-side validation function, add the client-side
 * validation javascript function to the page template.
 * The function should have the following signature:</p>
 * 
 * <pre>
 * &lt;script type="text/javascript"&gt;
 * function ValidationFunctionName(sender, parameter)
 * {
 *    if(parameter == ...)
 *       return true;
 *    else
 *       return false;
 * }
 * &lt;/script&gt;
 * </pre>
 * 
 * <p>Use the <tt>ClientValidationFunction</tt> option
 * to specify the name of the client-side validation script function associated
 * with the TCustomValidator.</p>
 * 
 * @class Prado.WebUI.TCustomValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TCustomValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Additional constructor options.
	 * @constructor initialize
	 * @param {object} options - Additional constructor options:
	 * @... {function} ClientValidationFunction - Custom validation function.
	 */

	/**
	 * Evaluate validation state
	 * Returns true if no valid custom validation function is present.
	 * @function {boolean} ?
	 * @return True if custom validation returned true.
	 */
	evaluateIsValid : function()
	{
		var value = this.getValidationValue();
		var clientFunction = this.options.ClientValidationFunction;
		if(typeof(clientFunction) == "string" && clientFunction.length > 0)
		{
			var validate = clientFunction.toFunction();
			return validate(this, value);
		}
		return true;
	}
});

/**
 * Uses callback request to perform validation.
 * 
 * @class Prado.WebUI.TActiveCustomValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TActiveCustomValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Value to validate
	 * @var {string} validatingValue
	 */
	validatingValue : null,
	/**
	 * DOM element that triggered validation
	 * @var {element} invoker
	 */
	invoker : null,

	/**
	 * Override the parent implementation to store the invoker, in order to
	 * re-validate after the callback has returned
	 * Calls evaluateIsValid() function to set the value of isValid property.
	 * Triggers onValidate event and onSuccess or onError event.
	 * @function {boolean} ?
	 * @param {element} invoker - DOM element that triggered validation
	 * @return True if valid.
	 */
	validate : function(invoker)
	{
		this.invoker = invoker;

		//try to find the control.
		if(!this.control)
			this.control = $(this.options.ControlToValidate);

		if(!this.control || this.control.disabled)
		{
			this.isValid = true;
			return this.isValid;
		}

		if(typeof(this.options.OnValidate) == "function")
		{
			if(this.requestDispatched == false)
				this.options.OnValidate(this, invoker);
		}

		if(this.enabled && !this.control.getAttribute('disabled'))
			this.isValid = this.evaluateIsValid();
		else
			this.isValid = true;

		// Only update the message if the callback has already return !
		if (!this.requestDispatched)
			this.updateValidationDisplay(invoker);

		this.observeChanges(this.control);

		return this.isValid;
	},

	/**
	 * Send CallBack to start serverside validation.
	 * @function {boolean} ?
	 * @return True if valid.
	 */
	evaluateIsValid : function()
	{
		var value = this.getValidationValue();
		if(!this.requestDispatched && (""+value) != (""+this.validatingValue))
		{
			this.validatingValue = value;
			var request = new Prado.CallbackRequest(this.options.EventTarget, this.options);
			if(this.options.DateFormat && value instanceof Date) //change date to string with formatting.
				value = value.SimpleFormat(this.options.DateFormat);
			request.setCallbackParameter(value);
			request.setCausesValidation(false);
			request.options.onSuccess = this.callbackOnSuccess.bind(this);
			request.options.onFailure = this.callbackOnFailure.bind(this);
			request.dispatch();
			this.requestDispatched = true;
			return false;
		}
		return this.isValid;
	},

	/**
	 * Parse CallBack response data on success.
	 * @function ?
	 * @param {CallbackRequest} request - CallbackRequest.
	 * @param {string} data - Response data.
	 */
	callbackOnSuccess : function(request, data)
	{
		this.isValid = data;
		this.requestDispatched = false;
		if(typeof(this.options.onSuccess) == "function")
			this.options.onSuccess(request,data);
		this.updateValidationDisplay();
		this.manager.updateSummary(this.group);
		// Redispatch initial request if any
		if(this.isValid) {
			if(this.invoker instanceof Prado.CallbackRequest) {
				this.invoker.dispatch();
			} else {
				this.invoker.click();
			}
		}
	},

	/**
	 * Handle callback failure.
	 * @function ?
	 * @param {CallbackRequest} request - CallbackRequest.
	 * @param {string} data - Response data.
	 */
	callbackOnFailure : function(request, data)
	{
		this.requestDispatched = false;
		if(typeof(this.options.onFailure) == "function")
			this.options.onFailure(request,data);
	}
});

/**
 * TRangeValidator tests whether an input value is within a specified range.
 *
 * <p>TRangeValidator uses three key properties to perform its validation.</p>
 * 
 * <p>The <tt>MinValue</tt> and <tt>MaxValue</tt> options specify the minimum
 * and maximum values of the valid range.</p> 
 * <p>The <tt>DataType</tt> option is
 * used to specify the data type of the value and the minimum and maximum range values.
 * The values are converted to this data type before the validation
 * operation is performed. The following value types are supported:</p>
 * 
 * - <b>Integer</b> A 32-bit signed integer data type.<br />
 * - <b>Float</b> A double-precision floating point number data type.<br />
 * - <b>Date</b> A date data type. The date format can be specified by<br />
 *   setting <tt>DateFormat</tt> option, which must be recognizable
 *   by <tt>Date.SimpleParse</tt> javascript function.
 * - <b>String</b> A string data type.
 * 
 * @class Prado.WebUI.TRangeValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TRangeValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Additional constructor options.
	 * @constructor initialize
	 * @param {object} options - Additional constructor options:
	 * @... {string} MinValue - Minimum range value
	 * @... {string} MaxValue - Maximum range value
	 * @... {string} DataType - Value data type: "Integer", "Float", "Date" or "String"
	 * @... {string} DateFormat - Date format for data type Date.
	 */

	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if value is in range or value is empty, 
	 * false otherwhise and when type conversion failed.
	 */
	evaluateIsValid : function()
	{
		var value = this.getValidationValue();
		if(value.length <= 0)
			return true;
		if(typeof(this.options.DataType) == "undefined")
			this.options.DataType = "String";

		if(this.options.DataType != "StringLength")
		{
			var min = this.convert(this.options.DataType, this.options.MinValue || null);
			var max = this.convert(this.options.DataType, this.options.MaxValue || null);
			value = this.convert(this.options.DataType, value);
		}
		else
		{
			var min = this.options.MinValue || 0;
			var max = this.options.MaxValue || Number.POSITIVE_INFINITY;
			value = value.length;
		}

		if(value == null)
			return false;

		var valid = true;

		if(min != null)
			valid = valid && (this.options.StrictComparison ? value > min : value >= min);
		if(max != null)
			valid = valid && (this.options.StrictComparison ? value < max : value <= max);
		return valid;
	}
});

/**
 * TRegularExpressionValidator validates whether the value of an associated
 * input component matches the pattern specified by a regular expression.
 * 
 * @class Prado.WebUI.TRegularExpressionValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TRegularExpressionValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Additional constructor option.
	 * @constructor initialize
	 * @param {object} options - Additional constructor option:
	 * @... {string} ValidationExpression - Regular expression to match against.
	 * @... {string} PatternModifiers - Pattern modifiers: combinations of g, i, and m
	 */

	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if value matches regular expression or value is empty. 
	 */
	evaluateIsValid : function()
	{
		var value = this.getRawValidationValue();
		if (value.length <= 0)
	    	return true;

		var rx = new RegExp('^'+this.options.ValidationExpression+'$',this.options.PatternModifiers);
		var matches = rx.exec(value);
		return (matches != null && value == matches[0]);
	}
});

/**
 * TEmailAddressValidator validates whether the value of an associated
 * input component is a valid email address.
 * 
 * @class Prado.WebUI.TEmailAddressValidator
 * @extends Prado.WebUI.TRegularExpressionValidator
 */
Prado.WebUI.TEmailAddressValidator = Prado.WebUI.TRegularExpressionValidator;


/**
 * TListControlValidator checks the number of selection and their values
 * for a TListControl that allows multiple selections.
 * 
 * @class Prado.WebUI.TListControlValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TListControlValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if number of selections and/or their values match requirements. 
	 */
	evaluateIsValid : function()
	{
		var elements = this.getListElements();
		if(elements && elements.length <= 0)
			return true;

		this.observeListElements(elements);

		var selection = this.getSelectedValuesAndChecks(elements);
		return this.isValidList(selection.checks, selection.values);
	},

	/**
	 * Observe list elements for of changes (only IE)
	 * @function ?
	 * @param {element[]} elements - Array of DOM elements to observe
	 */
	 observeListElements : function(elements)
	 {
		if(Prado.Browser().ie && this.isCheckBoxType(elements[0]))
		{
			var validator = this;
			elements.each(function(element)
			{
				validator.observeChanges(element);
			});
		}
	 },

	/**
	 * Check if list is valid.
	 * Determine if the number of checked values and the checked values
	 * satisfy the required number of checks and/or the checked values
	 * equal to the required values.
	 * @function {boolean} ?
	 * @param {int} checked - Number of required checked values
	 * @param {string[]} values - Array of required checked values
	 * @return True if checked values and number of checks are satisfied.
	 */
	isValidList : function(checked, values)
	{
		var exists = true;

		//check the required values
		var required = this.getRequiredValues();
		if(required.length > 0)
		{
			if(values.length < required.length)
				return false;
			required.each(function(requiredValue)
			{
				exists = exists && values.include(requiredValue);
			});
		}

		var min = typeof(this.options.Min) == "undefined" ?
					Number.NEGATIVE_INFINITY : this.options.Min;
		var max = typeof(this.options.Max) == "undefined" ?
					Number.POSITIVE_INFINITY : this.options.Max;
		return exists && checked >= min && checked <= max;
	},

	/**
	 * Get list of required values.
	 * @function {string[]} ?
	 * @return Array of required values that must be selected.
	 */
	getRequiredValues : function()
	{
		var required = [];
		if(this.options.Required && this.options.Required.length > 0)
			required = this.options.Required.split(/,\s*/);
		return required;
	}
});


/**
 * TDataTypeValidator verifies if the input data is of the type specified
 * by <tt>DataType</tt> option.
 * 
 * <p>The following data types are supported:</p>
 * 
 * - <b>Integer</b> A 32-bit signed integer data type.<br />
 * - <b>Float</b> A double-precision floating point number data type.<br />
 * - <b>Date</b> A date data type.<br />
 * - <b>String</b> A string data type.<br />
 * 
 * <p>For <b>Date</b> type, the option <tt>DateFormat</tt>
 * will be used to determine how to parse the date string.</p>
 * 
 * @class Prado.WebUI.TDataTypeValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TDataTypeValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Additional constructor option.
	 * @constructor initialize
	 * @param {object} options - Additional constructor option:
	 * @... {string} DataType - Value data type: "Integer", "Float", "Date" or "String"
	 * @... {string} DateFormat - Date format for data type Date.
	 */

	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if value matches required data type. 
	 */
	evaluateIsValid : function()
	{
		var value = this.getValidationValue();
		if(value.length <= 0)
			return true;
		return this.convert(this.options.DataType, value) != null;
	}
});

/**
 * TCaptchaValidator verifies if the input data is the same as 
 * the token shown in the associated CAPTCHA control.
 * 
 * @class Prado.WebUI.TCaptchaValidator
 * @extends Prado.WebUI.TBaseValidator
 */
Prado.WebUI.TCaptchaValidator = Class.extend(Prado.WebUI.TBaseValidator,
{
	/**
	 * Evaluate validation state
	 * @function {boolean} ?
	 * @return True if value matches captcha text 
	 */
	evaluateIsValid : function()
	{
		var a = this.getValidationValue();
		var h = 0;
		if (this.options.CaseSensitive==false)
			a = a.toUpperCase();
		for(var i = a.length-1; i >= 0; --i)
			h += a.charCodeAt(i);
		return h == this.options.TokenHash;
	},

	crc32 : function(str)
	{
	    function Utf8Encode(string)
		{
	        string = string.replace(/\r\n/g,"\n");
	        var utftext = "";

	        for (var n = 0; n < string.length; n++)
			{
	            var c = string.charCodeAt(n);

	            if (c < 128) {
	                utftext += String.fromCharCode(c);
	            }
	            else if((c > 127) && (c < 2048)) {
	                utftext += String.fromCharCode((c >> 6) | 192);
	                utftext += String.fromCharCode((c & 63) | 128);
	            }
	            else {
	                utftext += String.fromCharCode((c >> 12) | 224);
	                utftext += String.fromCharCode(((c >> 6) & 63) | 128);
	                utftext += String.fromCharCode((c & 63) | 128);
	            }
	        }

	        return utftext;
	    };

	    str = Utf8Encode(str);

	    var table = "00000000 77073096 EE0E612C 990951BA 076DC419 706AF48F E963A535 9E6495A3 0EDB8832 79DCB8A4 E0D5E91E 97D2D988 09B64C2B 7EB17CBD E7B82D07 90BF1D91 1DB71064 6AB020F2 F3B97148 84BE41DE 1ADAD47D 6DDDE4EB F4D4B551 83D385C7 136C9856 646BA8C0 FD62F97A 8A65C9EC 14015C4F 63066CD9 FA0F3D63 8D080DF5 3B6E20C8 4C69105E D56041E4 A2677172 3C03E4D1 4B04D447 D20D85FD A50AB56B 35B5A8FA 42B2986C DBBBC9D6 ACBCF940 32D86CE3 45DF5C75 DCD60DCF ABD13D59 26D930AC 51DE003A C8D75180 BFD06116 21B4F4B5 56B3C423 CFBA9599 B8BDA50F 2802B89E 5F058808 C60CD9B2 B10BE924 2F6F7C87 58684C11 C1611DAB B6662D3D 76DC4190 01DB7106 98D220BC EFD5102A 71B18589 06B6B51F 9FBFE4A5 E8B8D433 7807C9A2 0F00F934 9609A88E E10E9818 7F6A0DBB 086D3D2D 91646C97 E6635C01 6B6B51F4 1C6C6162 856530D8 F262004E 6C0695ED 1B01A57B 8208F4C1 F50FC457 65B0D9C6 12B7E950 8BBEB8EA FCB9887C 62DD1DDF 15DA2D49 8CD37CF3 FBD44C65 4DB26158 3AB551CE A3BC0074 D4BB30E2 4ADFA541 3DD895D7 A4D1C46D D3D6F4FB 4369E96A 346ED9FC AD678846 DA60B8D0 44042D73 33031DE5 AA0A4C5F DD0D7CC9 5005713C 270241AA BE0B1010 C90C2086 5768B525 206F85B3 B966D409 CE61E49F 5EDEF90E 29D9C998 B0D09822 C7D7A8B4 59B33D17 2EB40D81 B7BD5C3B C0BA6CAD EDB88320 9ABFB3B6 03B6E20C 74B1D29A EAD54739 9DD277AF 04DB2615 73DC1683 E3630B12 94643B84 0D6D6A3E 7A6A5AA8 E40ECF0B 9309FF9D 0A00AE27 7D079EB1 F00F9344 8708A3D2 1E01F268 6906C2FE F762575D 806567CB 196C3671 6E6B06E7 FED41B76 89D32BE0 10DA7A5A 67DD4ACC F9B9DF6F 8EBEEFF9 17B7BE43 60B08ED5 D6D6A3E8 A1D1937E 38D8C2C4 4FDFF252 D1BB67F1 A6BC5767 3FB506DD 48B2364B D80D2BDA AF0A1B4C 36034AF6 41047A60 DF60EFC3 A867DF55 316E8EEF 4669BE79 CB61B38C BC66831A 256FD2A0 5268E236 CC0C7795 BB0B4703 220216B9 5505262F C5BA3BBE B2BD0B28 2BB45A92 5CB36A04 C2D7FFA7 B5D0CF31 2CD99E8B 5BDEAE1D 9B64C2B0 EC63F226 756AA39C 026D930A 9C0906A9 EB0E363F 72076785 05005713 95BF4A82 E2B87A14 7BB12BAE 0CB61B38 92D28E9B E5D5BE0D 7CDCEFB7 0BDBDF21 86D3D2D4 F1D4E242 68DDB3F8 1FDA836E 81BE16CD F6B9265B 6FB077E1 18B74777 88085AE6 FF0F6A70 66063BCA 11010B5C 8F659EFF F862AE69 616BFFD3 166CCF45 A00AE278 D70DD2EE 4E048354 3903B3C2 A7672661 D06016F7 4969474D 3E6E77DB AED16A4A D9D65ADC 40DF0B66 37D83BF0 A9BCAE53 DEBB9EC5 47B2CF7F 30B5FFE9 BDBDF21C CABAC28A 53B39330 24B4A3A6 BAD03605 CDD70693 54DE5729 23D967BF B3667A2E C4614AB8 5D681B02 2A6F2B94 B40BBE37 C30C8EA1 5A05DF1B 2D02EF8D";
		var crc = 0;
	    var x = 0;
	    var y = 0;

	    crc = crc ^ (-1);
	    for( var i = 0, iTop = str.length; i < iTop; i++ )
		{
	        y = ( crc ^ str.charCodeAt( i ) ) & 0xFF;
	        x = "0x" + table.substr( y * 9, 8 );
	        crc = ( crc >>> 8 ) ^ x;
	    }
	    return crc ^ (-1);
	}
});
