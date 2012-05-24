Prado.WebUI.TDatePicker = Class.create();
Object.extend(Prado.WebUI.TDatePicker,
{
	/**
	 * @return Date the date from drop down list options.
	 */
	getDropDownDate : function(control)
	{
		var now=new Date();
		var year=now.getFullYear();
		var month=now.getMonth();
		var day=1;

		var month_list = this.getMonthListControl(control);
	 	var day_list = this.getDayListControl(control);
	 	var year_list = this.getYearListControl(control);

		var day = day_list ? $F(day_list) : 1;
		var month = month_list ? $F(month_list) : now.getMonth();
		var year = year_list ? $F(year_list) : now.getFullYear();

		return new Date(year,month,day, 0, 0, 0);
	},

	getYearListControl : function(control)
	{
		return $(control.id+"_year");
	},

	getMonthListControl : function(control)
	{
		return $(control.id+"_month");
	},

	getDayListControl : function(control)
	{
		return $(control.id+"_day");
	}
});

Prado.WebUI.TDatePicker.prototype =
{
	MonthNames : [	"January",		"February",		"March",	"April",
		"May",			"June",			"July",		"August",
		"September",	"October",		"November",	"December"
	],
	AbbreviatedMonthNames : ["Jan", "Feb", "Mar", "Apr", "May",
						"Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],

	ShortWeekDayNames : ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" ],

	Format : "yyyy-MM-dd",

	FirstDayOfWeek : 1, // 0 for sunday

	ClassName : "",

	CalendarStyle : "default",

	FromYear : 2000, UpToYear: 2015,

	initialize : function(options)
	{
		this.options = options || [];
		this.control = $(options.ID);
		this.dateSlot = new Array(42);
		this.weekSlot = new Array(6);
		this.minimalDaysInFirstWeek	= 4;
		this.selectedDate = this.newDate();
		this.positionMode = 'Bottom';
		
		Prado.Registry.set(options.ID, this);

		//which element to trigger to show the calendar
		if(this.options.Trigger)
		{
			this.trigger = $(this.options.Trigger) ;
			var triggerEvent = this.options.TriggerEvent || "click";
		}
		else
		{
			this.trigger  = this.control;
			var triggerEvent = this.options.TriggerEvent || "focus";
		}
		
		// Popup position
		if(this.options.PositionMode == 'Top')
		{
			this.positionMode = this.options.PositionMode;
		}

		Object.extend(this,options);

		Event.observe(this.trigger, triggerEvent, this.show.bindEvent(this));
		
		// Listen to change event if needed
		if (typeof(this.options.OnDateChanged) == "function")
		{
			if(this.options.InputMode == "TextBox")
			{
				Event.observe(this.control, "change", this.onDateChanged.bindEvent(this));
			} 
			else
			{
				var day = Prado.WebUI.TDatePicker.getDayListControl(this.control);
				var month = Prado.WebUI.TDatePicker.getMonthListControl(this.control);
				var year = Prado.WebUI.TDatePicker.getYearListControl(this.control);
				Event.observe (day, "change", this.onDateChanged.bindEvent(this));
				Event.observe (month, "change", this.onDateChanged.bindEvent(this));
				Event.observe (year, "change", this.onDateChanged.bindEvent(this));
				
			}
			
			
		}

	},

	create : function()
	{
		if(typeof(this._calDiv) != "undefined")
			return;

		var div;
		var table;
		var tbody;
		var tr;
		var td;

		// Create the top-level div element
		this._calDiv = document.createElement("div");
		this._calDiv.className = "TDatePicker_"+this.CalendarStyle+" "+this.ClassName;
		this._calDiv.style.display = "none";
		this._calDiv.style.position = "absolute"

		// header div
		div = document.createElement("div");
		div.className = "calendarHeader";
		this._calDiv.appendChild(div);

		table = document.createElement("table");
		table.style.cellSpacing = 0;
		div.appendChild(table);

		tbody = document.createElement("tbody");
		table.appendChild(tbody);

		tr = document.createElement("tr");
		tbody.appendChild(tr);

		// Previous Month Button
		td = document.createElement("td");
		var previousMonth = document.createElement("input");
		previousMonth.className = "prevMonthButton button";
		previousMonth.type = "button"
		previousMonth.value = "<<";
		td.appendChild(previousMonth);
		tr.appendChild(td);



		//
		// Create the month drop down
		//
		td = document.createElement("td");
		tr.appendChild(td);
		this._monthSelect = document.createElement("select");
		this._monthSelect.className = "months";
	    for (var i = 0 ; i < this.MonthNames.length ; i++) {
	        var opt = document.createElement("option");
	        opt.innerHTML = this.MonthNames[i];
	        opt.value = i;
	        if (i == this.selectedDate.getMonth()) {
	            opt.selected = true;
	        }
	        this._monthSelect.appendChild(opt);
	    }
		td.appendChild(this._monthSelect);


		//
		// Create the year drop down
		//
		td = document.createElement("td");
		td.className = "labelContainer";
		tr.appendChild(td);
		this._yearSelect = document.createElement("select");
		for(var i=this.FromYear; i <= this.UpToYear; ++i) {
			var opt = document.createElement("option");
			opt.innerHTML = i;
			opt.value = i;
			if (i == this.selectedDate.getFullYear()) {
				opt.selected = false;
			}
			this._yearSelect.appendChild(opt);
		}
		td.appendChild(this._yearSelect);


		td = document.createElement("td");
		var nextMonth = document.createElement("input");
		nextMonth.className = "nextMonthButton button";
		nextMonth.type = "button";
		nextMonth.value = ">>";
		td.appendChild(nextMonth);
		tr.appendChild(td);

		// Calendar body
		div = document.createElement("div");
		div.className = "calendarBody";
		this._calDiv.appendChild(div);
		var calendarBody = div;

		// Create the inside of calendar body

		var text;
		table = document.createElement("table");
		table.align="center";
		table.className = "grid";

	    div.appendChild(table);
		var thead = document.createElement("thead");
		table.appendChild(thead);
		tr = document.createElement("tr");
		thead.appendChild(tr);

		for(i=0; i < 7; ++i) {
			td = document.createElement("th");
			text = document.createTextNode(this.ShortWeekDayNames[(i+this.FirstDayOfWeek)%7]);
			td.appendChild(text);
			td.className = "weekDayHead";
			tr.appendChild(td);
		}

		// Date grid
		tbody = document.createElement("tbody");
		table.appendChild(tbody);

		for(var week=0; week<6; ++week) {
			tr = document.createElement("tr");
			tbody.appendChild(tr);

		for(var day=0; day<7; ++day) {
				td = document.createElement("td");
				td.className = "calendarDate";
				text = document.createTextNode(String.fromCharCode(160));
				td.appendChild(text);

				tr.appendChild(td);
				var tmp = new Object();
				tmp.tag = "DATE";
				tmp.value = -1;
				tmp.data = text;
				this.dateSlot[(week*7)+day] = tmp;

				Event.observe(td, "mouseover", this.hover.bindEvent(this));
				Event.observe(td, "mouseout", this.hover.bindEvent(this));

			}
		}

		// Calendar Footer
		div = document.createElement("div");
		div.className = "calendarFooter";
		this._calDiv.appendChild(div);

		var todayButton = document.createElement("input");
		todayButton.type="button";
		todayButton.className = "todayButton";
		var today = this.newDate();
		var buttonText = today.SimpleFormat(this.Format,this);
		todayButton.value = buttonText;
		div.appendChild(todayButton);

		if(Prado.Browser().ie)
		{
			this.iePopUp = document.createElement('iframe');
			this.iePopUp.src = Prado.WebUI.TDatePicker.spacer;
			this.iePopUp.style.position = "absolute"
			this.iePopUp.scrolling="no"
			this.iePopUp.frameBorder="0"
			this.control.parentNode.appendChild(this.iePopUp);
		}

		this.control.parentNode.appendChild(this._calDiv);

		this.update();
		this.updateHeader();

		this.ieHack(true);

		// IE55+ extension
		previousMonth.hideFocus = true;
		nextMonth.hideFocus = true;
		todayButton.hideFocus = true;
		// end IE55+ extension

		// hook up events
		Event.observe(previousMonth, "click", this.prevMonth.bindEvent(this));
		Event.observe(nextMonth, "click", this.nextMonth.bindEvent(this));
		Event.observe(todayButton, "click", this.selectToday.bindEvent(this));
		//Event.observe(clearButton, "click", this.clearSelection.bindEvent(this));
		Event.observe(this._monthSelect, "change", this.monthSelect.bindEvent(this));
		Event.observe(this._yearSelect, "change", this.yearSelect.bindEvent(this));

		// ie6 extension
		Event.observe(this._calDiv, "mousewheel", this.mouseWheelChange.bindEvent(this));

		Event.observe(calendarBody, "click", this.selectDate.bindEvent(this));

		Prado.Element.focus(this.control);

	},

	ieHack : function(cleanup)
	{
		// IE hack
		if(this.iePopUp)
		{
			this.iePopUp.style.display = "block";
			this.iePopUp.style.left = (this._calDiv.offsetLeft -1)+ "px";
			this.iePopUp.style.top = (this._calDiv.offsetTop -1 ) + "px";
			this.iePopUp.style.width = Math.abs(this._calDiv.offsetWidth -2)+ "px";
			this.iePopUp.style.height = (this._calDiv.offsetHeight + 1)+ "px";
			if(cleanup) this.iePopUp.style.display = "none";
		}
	},

	keyPressed : function(ev)
	{
		if(!this.showing) return;
		if (!ev) ev = document.parentWindow.event;
		var kc = ev.keyCode != null ? ev.keyCode : ev.charCode;

		if(kc == Event.KEY_RETURN || kc == Event.KEY_SPACEBAR || kc == Event.KEY_TAB)
		{
			this.setSelectedDate(this.selectedDate);
			Event.stop(ev);
			this.hide();
		}
		if(kc == Event.KEY_ESC)
		{
			Event.stop(ev); this.hide();
		}

		var getDaysPerMonth = function (nMonth, nYear)
		{
			nMonth = (nMonth + 12) % 12;
	        var days= [31,28,31,30,31,30,31,31,30,31,30,31];
			var res = days[nMonth];
			if (nMonth == 1) //feburary, leap years has 29
                res += nYear % 4 == 0 && !(nYear % 400 == 0) ? 1 : 0;
	        return res;
		}

		if(kc < 37 || kc > 40) return true;

		var current = this.selectedDate;
		var d = current.valueOf();
		if(kc == Event.KEY_LEFT)
		{
			if(ev.ctrlKey || ev.shiftKey) // -1 month
			{
                current.setDate( Math.min(current.getDate(), getDaysPerMonth(current.getMonth() - 1,current.getFullYear())) ); // no need to catch dec -> jan for the year
                d = current.setMonth( current.getMonth() - 1 );
			}
			else
				d -= 86400000; //-1 day
		}
		else if (kc == Event.KEY_RIGHT)
		{
			if(ev.ctrlKey || ev.shiftKey) // +1 month
			{
				current.setDate( Math.min(current.getDate(), getDaysPerMonth(current.getMonth() + 1,current.getFullYear())) ); // no need to catch dec -> jan for the year
				d = current.setMonth( current.getMonth() + 1 );
			}
			else
				d += 86400000; //+1 day
		}
		else if (kc == Event.KEY_UP)
		{
			if(ev.ctrlKey || ev.shiftKey) //-1 year
			{
				current.setDate( Math.min(current.getDate(), getDaysPerMonth(current.getMonth(),current.getFullYear() - 1)) ); // no need to catch dec -> jan for the year
				d = current.setFullYear( current.getFullYear() - 1 );
			}
			else
				d -= 604800000; // -7 days
		}
		else if (kc == Event.KEY_DOWN)
		{
			if(ev.ctrlKey || ev.shiftKey) // +1 year
			{
				current.setDate( Math.min(current.getDate(), getDaysPerMonth(current.getMonth(),current.getFullYear() + 1)) ); // no need to catch dec -> jan for the year
				d = current.setFullYear( current.getFullYear() + 1 );
			}
			else
				d += 7 * 24 * 61 * 60 * 1000; // +7 days
		}
		this.setSelectedDate(d);
		Event.stop(ev);
	},

	selectDate : function(ev)
	{
		var el = Event.element(ev);
		while (el.nodeType != 1)
			el = el.parentNode;

		while (el != null && el.tagName && el.tagName.toLowerCase() != "td")
			el = el.parentNode;

		// if no td found, return
		if (el == null || el.tagName == null || el.tagName.toLowerCase() != "td")
			return;

		var d = this.newDate(this.selectedDate);
		var n = Number(el.firstChild.data);
		if (isNaN(n) || n <= 0 || n == null)
			return;

		d.setDate(n);
		this.setSelectedDate(d);
		this.hide();
	},

	selectToday : function()
	{
		if(this.selectedDate.toISODate() == this.newDate().toISODate())
			this.hide();

		this.setSelectedDate(this.newDate());
	},

	clearSelection : function()
	{
		this.setSelectedDate(this.newDate());
		this.hide();
	},

	monthSelect : function(ev)
	{
		this.setMonth(Form.Element.getValue(Event.element(ev)));
	},

	yearSelect : function(ev)
	{
		this.setYear(Form.Element.getValue(Event.element(ev)));
	},

	// ie6 extension
	mouseWheelChange : function (e)
	{
		if (e == null) e = document.parentWindow.event;
		var n = - e.wheelDelta / 120;
		var d = this.newDate(this.selectedDate);
		var m = d.getMonth() + n;
		this.setMonth(m);

		return false;
	},

	// Respond to change event on the textbox or dropdown list
	// This method raises OnDateChanged event on client side if it has been defined
	onDateChanged : function ()
	{
		if (this.options.OnDateChanged)
		{
		 	var date;
		 	if (this.options.InputMode == "TextBox")
		 	{
		 		date=this.control.value;
		 	} 
		 	else
		 	{
		 		var day = Prado.WebUI.TDatePicker.getDayListControl(this.control).selectedIndex+1;
				var month = Prado.WebUI.TDatePicker.getMonthListControl(this.control).selectedIndex;
				var year = Prado.WebUI.TDatePicker.getYearListControl(this.control).value;
				date=new Date(year, month, day, 0,0,0).SimpleFormat(this.Format, this);
			}
			this.options.OnDateChanged(this, date);
		}
	},
	
	onChange : function()
	{ 
		if(this.options.InputMode == "TextBox")
		{
			this.control.value = this.formatDate();
			Event.fireEvent(this.control, "change");
		}
		else
		{
			var day = Prado.WebUI.TDatePicker.getDayListControl(this.control);
			var month = Prado.WebUI.TDatePicker.getMonthListControl(this.control);
			var year = Prado.WebUI.TDatePicker.getYearListControl(this.control);
			var date = this.selectedDate;
			if(day)
			{
				day.selectedIndex = date.getDate()-1;
			}
			if(month)
			{
				month.selectedIndex = date.getMonth();
			}
			if(year)
			{
				var years = year.options;
				var currentYear = date.getFullYear();
				for(var i = 0; i < years.length; i++)
					years[i].selected = years[i].value.toInteger() == currentYear;
			}
			Event.fireEvent(day || month || year, "change");
		}
	},

	formatDate : function()
	{
		return this.selectedDate ? this.selectedDate.SimpleFormat(this.Format,this) : '';
	},

	newDate : function(date)
	{
		if(!date)
			date = new Date();
		if(typeof(date) == "string" || typeof(date) == "number")
			date = new Date(date);
		return new Date(Math.min(Math.max(date.getFullYear(),this.FromYear),this.UpToYear), date.getMonth(), date.getDate(), 0,0,0);
	},

	setSelectedDate : function(date)
	{
		if (date == null)
			return;
		var old=this.selectedDate;
		this.selectedDate = this.newDate(date);
		var dateChanged=(old - this.selectedDate != 0) || ( this.options.InputMode == "TextBox" && this.control.value != this.formatDate());

		this.updateHeader();
		this.update();
		if (dateChanged && typeof(this.onChange) == "function")
			this.onChange(this, date);
	},

	getElement : function()
	{
		return this._calDiv;
	},

	getSelectedDate : function ()
	{
		return this.selectedDate == null ? null : this.newDate(this.selectedDate);
	},

	setYear : function(year)
	{
		var d = this.newDate(this.selectedDate);
		d.setFullYear(year);
		this.setSelectedDate(d);
	},

	setMonth : function (month)
	{
		var d = this.newDate(this.selectedDate);
		d.setDate(Math.min(d.getDate(), this.getDaysPerMonth(month,d.getFullYear())));
		d.setMonth(month);
		this.setSelectedDate(d);
	},

	nextMonth : function ()
	{
		this.setMonth(this.selectedDate.getMonth()+1);
	},

	prevMonth : function ()
	{
		this.setMonth(this.selectedDate.getMonth()-1);
	},

	getDaysPerMonth : function (month, year)
	{
		month = (Number(month)+12) % 12;
        var days = [31,28,31,30,31,30,31,31,30,31,30,31];
		var res = days[month];
		if (month == 1 && ((!(year % 4) && (year % 100)) || !(year % 400))) //feburary, leap years has 29
            res++;
        return res;
	},

	getDatePickerOffsetHeight : function()
	{
		if(this.options.InputMode == "TextBox")
			return this.control.offsetHeight;

		var control = Prado.WebUI.TDatePicker.getDayListControl(this.control);
		if(control) return control.offsetHeight;

		var control = Prado.WebUI.TDatePicker.getMonthListControl(this.control);
		if(control) return control.offsetHeight;

		var control = Prado.WebUI.TDatePicker.getYearListControl(this.control);
		if(control) return control.offsetHeight;
		return 0;
	},

	show : function()
	{
		this.create();

		if(!this.showing)
		{
			var pos = this.control.positionedOffset();

			pos[1] += this.getDatePickerOffsetHeight();
			this._calDiv.style.top = (pos[1]-1) + "px";
			this._calDiv.style.display = "block";
			this._calDiv.style.left = pos[0] + "px";

			this.documentClickEvent = this.hideOnClick.bindEvent(this);
			this.documentKeyDownEvent = this.keyPressed.bindEvent(this);
			Event.observe(document.body, "click", this.documentClickEvent);
			var date = this.getDateFromInput();
			if(date)
			{
				this.selectedDate = date;
				this.setSelectedDate(date);
			}
			Event.observe(document,"keydown", this.documentKeyDownEvent);
			this.showing = true;
			
			if(this.positionMode=='Top')
			{
				this._calDiv.style.top = ((pos[1]-1) - this.getDatePickerOffsetHeight() - this._calDiv.offsetHeight) + 'px';
				if(Prado.Browser().ie)
					this.iePopup = this._calDiv.style.top;					
			}
			this.ieHack(false);
		}
	},

	getDateFromInput : function()
	{
		if(this.options.InputMode == "TextBox")
			return Date.SimpleParse($F(this.control), this.Format);
		else
			return Prado.WebUI.TDatePicker.getDropDownDate(this.control);
	},

	//hide the calendar when clicked outside any calendar
	hideOnClick : function(ev)
	{
		if(!this.showing) return;
		var el = Event.element(ev);
		var within = false;
		do
		{
			within = within || (el.className && Element.hasClassName(el, "TDatePicker_"+this.CalendarStyle));
			within = within || el == this.trigger;
			within = within || el == this.control;
			if(within) break;
			el = el.parentNode;
		}
		while(el);
		if(!within) this.hide();
	},


	hide : function()
	{
		if(this.showing)
		{
			this._calDiv.style.display = "none";
			if(this.iePopUp)
				this.iePopUp.style.display = "none";
			this.showing = false;
			Event.stopObserving(document.body, "click", this.documentClickEvent);
			Event.stopObserving(document,"keydown", this.documentKeyDownEvent);
		}
	},

	update : function()
	{
		// Calculate the number of days in the month for the selected date
		var date = this.selectedDate;
		var today = (this.newDate()).toISODate();

		var selected = date.toISODate();
		var d1 = new Date(date.getFullYear(), date.getMonth(), 1);
		var d2 = new Date(date.getFullYear(), date.getMonth()+1, 1);
		var monthLength = Math.round((d2 - d1) / (24 * 60 * 60 * 1000));

		// Find out the weekDay index for the first of this month
		var firstIndex = (d1.getDay() - this.FirstDayOfWeek) % 7 ;
	    if (firstIndex < 0)
	    	firstIndex += 7;

		var index = 0;
		while (index < firstIndex) {
			this.dateSlot[index].value = -1;
			this.dateSlot[index].data.data = String.fromCharCode(160);
			this.dateSlot[index].data.parentNode.className = "empty";
			index++;
		}

	    for (var i = 1; i <= monthLength; i++, index++) {
			var slot = this.dateSlot[index];
			var slotNode = slot.data.parentNode;
			slot.value = i;
			slot.data.data = i;
			slotNode.className = "date";
			//slotNode.style.color = "";
			if (d1.toISODate() == today) {
				slotNode.className += " today";
			}
			if (d1.toISODate() == selected) {
			//	slotNode.style.color = "blue";
				slotNode.className += " selected";
			}
			d1 = new Date(d1.getFullYear(), d1.getMonth(), d1.getDate()+1);
		}

		var lastDateIndex = index;

	    while(index < 42) {
			this.dateSlot[index].value = -1;
			this.dateSlot[index].data.data = String.fromCharCode(160);
			this.dateSlot[index].data.parentNode.className = "empty";
			++index;
		}

	},

	hover : function(ev)
	{
		if(Event.element(ev).tagName)
		{
			if(ev.type == "mouseover")
				Event.element(ev).addClassName("hover");
				else
				Event.element(ev).removeClassName("hover");
		}
	},

	updateHeader : function () {

		var options = this._monthSelect.options;
		var m = this.selectedDate.getMonth();
		for(var i=0; i < options.length; ++i) {
			options[i].selected = false;
			if (options[i].value == m) {
				options[i].selected = true;
			}
		}

		options = this._yearSelect.options;
		var year = this.selectedDate.getFullYear();
		for(var i=0; i < options.length; ++i) {
			options[i].selected = false;
			if (options[i].value == year) {
				options[i].selected = true;
			}
		}

	}
};
