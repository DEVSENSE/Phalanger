Prado.WebUI.TRatingList = Base.extend(
{
	selectedIndex : -1,
	rating: -1,
	readOnly : false,

	constructor : function(options)
	{
		var cap = $(options.CaptionID);
		this.options = Object.extend(
		{
			caption : cap ? cap.innerHTML : ''
		}, options || {});

		Prado.WebUI.TRatingList.register(this);
		this._init();
		Prado.Registry.set(options.ListID, this);
		this.selectedIndex = options.SelectedIndex;
		this.rating = options.Rating;
		this.readOnly = options.ReadOnly
		if(options.Rating <= 0 && options.SelectedIndex >= 0)
			this.rating = options.SelectedIndex+1;
		this.setReadOnly(this.readOnly);
	},

	_init: function(options)
	{
		Element.addClassName($(this.options.ListID),this.options.Style);
		this.radios = new Array();
		this._mouseOvers = new Array();
		this._mouseOuts = new Array();
		this._clicks = new Array();
		var index=0;
		for(var i = 0; i<this.options.ItemCount; i++)
		{
			var radio = $(this.options.ListID+'_c'+i);
			var td = radio.parentNode.parentNode;
			if(radio && td.tagName.toLowerCase()=='td')
			{
				this.radios.push(radio);
				this._mouseOvers.push(this.hover.bindEvent(this,index));
				this._mouseOuts.push(this.recover.bindEvent(this,index));
				this._clicks.push(this.click.bindEvent(this,index));
				index++;
				Element.addClassName(td,"rating");
			}
		}
	},

	hover : function(ev,index)
	{
		if(this.readOnly==true) return;
		for(var i = 0; i<this.radios.length; i++)
		{
			var node = this.radios[i].parentNode.parentNode;
			var action = i <= index ? 'addClassName' : 'removeClassName'
			Element[action](node,"rating_hover");
			Element.removeClassName(node,"rating_selected");
			Element.removeClassName(node,"rating_half");
		}
		this.showCaption(this.getIndexCaption(index));
	},

	recover : function(ev,index)
	{
		if(this.readOnly==true) return;
		this.showRating(this.rating);
		this.showCaption(this.options.caption);
	},

	click : function(ev, index)
	{
		if(this.readOnly==true) return;
		for(var i = 0; i<this.radios.length; i++)
			this.radios[i].checked = (i == index);
		this.selectedIndex = index;
		this.setRating(index+1);

		if(this.options['AutoPostBack']==true){
			this.dispatchRequest(ev);
		}
	},

	dispatchRequest : function(ev)
	{
		var requestOptions = Object.extend(
		{
			ID : this.options.ListID+"_c"+this.selectedIndex,
			EventTarget : this.options.ListName+"$c"+this.selectedIndex
		},this.options);
		Prado.PostBack(ev, requestOptions);
	},

	setRating : function(value)
	{
		this.rating = value;
		var base = Math.floor(value-1);
		var remainder = value - base-1;
		var halfMax = this.options.HalfRating["1"];
		var index = remainder > halfMax ? base+1 : base;
		for(var i = 0; i<this.radios.length; i++)
			this.radios[i].checked = (i == index);

		var caption = this.getIndexCaption(index);
		this.setCaption(caption);
		this.showCaption(caption);

		this.showRating(this.rating);
	},

	showRating: function(value)
	{
		var base = Math.floor(value-1);
		var remainder = value - base-1;
		var halfMin = this.options.HalfRating["0"];
		var halfMax = this.options.HalfRating["1"];
		var index = remainder > halfMax ? base+1 : base;
		var hasHalf = remainder >= halfMin && remainder <= halfMax;
		for(var i = 0; i<this.radios.length; i++)
		{
			var node = this.radios[i].parentNode.parentNode;
			var action = i > index ? 'removeClassName' : 'addClassName';
			Element[action](node, "rating_selected");
			if(i==index+1 && hasHalf)
				Element.addClassName(node, "rating_half");
			else
				Element.removeClassName(node, "rating_half");
			Element.removeClassName(node,"rating_hover");
		}
	},
	
	getIndexCaption : function(index)
	{
		return index > -1 ? this.radios[index].value : this.options.caption;
	},

	showCaption : function(value)
	{
		var caption = $(this.options.CaptionID);
		if(caption) caption.innerHTML = value;
		$(this.options.ListID).title = value;
	},

	setCaption : function(value)
	{
		this.options.caption = value;
		this.showCaption(value);
	},

	setReadOnly : function(value)
	{
		this.readOnly = value;
		for(var i = 0; i<this.radios.length; i++)
		{
			
			var action = value ? 'addClassName' : 'removeClassName';
			Element[action](this.radios[i].parentNode.parentNode, "rating_disabled");
			
			var action = value ? 'stopObserving' : 'observe';
			var td = this.radios[i].parentNode.parentNode;
			Event[action](td, "mouseover", this._mouseOvers[i]);
			Event[action](td, "mouseout", this._mouseOuts[i]);
			Event[action](td, "click", this._clicks[i]);
		}

		this.showRating(this.rating);
	}
},
{
ratings : {},
register : function(rating)
{
	Prado.WebUI.TRatingList.ratings[rating.options.ListID] = rating;
},

setReadOnly : function(id,value)
{
	Prado.WebUI.TRatingList.ratings[id].setReadOnly(value);
},

setRating : function(id,value)
{
	Prado.WebUI.TRatingList.ratings[id].setRating(value);
},

setCaption : function(id,value)
{
	Prado.WebUI.TRatingList.ratings[id].setCaption(value);
}
});

Prado.WebUI.TActiveRatingList = Prado.WebUI.TRatingList.extend( 
{	
	dispatchRequest : function(ev)
	{
		var requestOptions = Object.extend(
		{
			ID : this.options.ListID+"_c"+this.selectedIndex,
			EventTarget : this.options.ListName+"$c"+this.selectedIndex
		},this.options);
		var request = new Prado.CallbackRequest(requestOptions.EventTarget, requestOptions);
		if(request.dispatch()==false)
			Event.stop(ev);
	}
	
});
