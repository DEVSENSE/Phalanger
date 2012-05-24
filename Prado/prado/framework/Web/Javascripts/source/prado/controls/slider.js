/**
 * TSlider client class.
 * This clas is mainly based on Scriptaculous Slider control (http://script.aculo.us)
 */

Prado.WebUI.TSlider = Class.extend(Prado.WebUI.PostBackControl,
{
	onInit : function (options)
	{
		var slider = this;
		this.options=options || {};
		this.track = $(options.ID+'_track');
		this.handle =$(options.ID+'_handle');
		this.progress = $(options.ID+'_progress');
		this.axis  = this.options.axis || 'horizontal';
		this.range = this.options.range || $R(0,1);
		this.value = 0;
		this.maximum   = this.options.maximum || this.range.end;
		this.minimum   = this.options.minimum || this.range.start;
		this.hiddenField=$(this.options.ID+'_1');
		
		// Issue 181
		this.element.stopObserving();
		this.track.stopObserving();
		this.handle.stopObserving();
		if (this.progress) this.progress.stopObserving();
		this.hiddenField.stopObserving();
		
		// Will be used to align the handle onto the track, if necessary
		this.alignX = parseInt(this.options.alignX || - this.track.offsetLeft);
		this.alignY = parseInt(this.options.alignY || - this.track.offsetTop);
		
		this.trackLength = this.maximumOffset() - this.minimumOffset();
		this.handleLength = this.isVertical() ? 
			(this.handle.offsetHeight != 0 ? 
				this.handle.offsetHeight : this.handles.style.height.replace(/px$/,"")) : 
				(this.handle.offsetWidth != 0 ? this.handle.offsetWidth : 
					this.handle.style.width.replace(/px$/,""));
	
		this.active   = false;
		this.dragging = false;
		this.disabled = false;

		if(this.options.disabled) this.setDisabled();
	
		// Allowed values array
		this.allowedValues = this.options.values ? this.options.values.sortBy(Prototype.K) : false;
		if(this.allowedValues) {
			this.minimum = this.allowedValues.min();
			this.maximum = this.allowedValues.max();
		}

		this.eventMouseDown = this.startDrag.bindAsEventListener(this);
		this.eventMouseUp   = this.endDrag.bindAsEventListener(this);
		this.eventMouseMove = this.update.bindAsEventListener(this);

		// Initialize handle
		this.setValue(parseFloat(slider.options.sliderValue));
		Element.makePositioned(this.handle); // fix IE
		Event.observe (this.handle, "mousedown", this.eventMouseDown);
		
		Event.observe (this.track, "mousedown", this.eventMouseDown);
		if (this.progress) Event.observe (this.progress, "mousedown", this.eventMouseDown);
		
		// Issue 181
		document.stopObserving("mouseup", this.eventMouseUp);
		document.stopObserving("mousemove", this.eventMouseMove);
		Event.observe (document, "mouseup", this.eventMouseUp);
		Event.observe (document, "mousemove", this.eventMouseMove);
		
		this.initialized=true;
		
		
		if(this.options['AutoPostBack']==true)
			Event.observe(this.hiddenField, "change", Prado.PostBack.bindEvent(this,options));
    
	},
  
	dispose: function() {
		var slider = this;    
		Event.stopObserving(this.track, "mousedown", this.eventMouseDown);
		Event.stopObserving(document, "mouseup", this.eventMouseUp);
		Event.stopObserving(document, "mousemove", this.eventMouseMove);
	
		Event.stopObserving(this.handle, "mousedown", slider.eventMouseDown);
 	},
 	
	setDisabled: function(){
		this.disabled = true;
	},
	setEnabled: function(){
		this.disabled = false;
	},  
	getNearestValue: function(value){
		if(this.allowedValues){
			if(value >= this.allowedValues.max()) return(this.allowedValues.max());
			if(value <= this.allowedValues.min()) return(this.allowedValues.min());
      
			var offset = Math.abs(this.allowedValues[0] - value);
			var newValue = this.allowedValues[0];
			this.allowedValues.each( function(v) {
				var currentOffset = Math.abs(v - value);
				if(currentOffset <= offset){
					newValue = v;
					offset = currentOffset;
				} 
			});
			return newValue;
		}
		if(value > this.range.end) return this.range.end;
		if(value < this.range.start) return this.range.start;
		return value;
	},
	
	setValue: function(sliderValue){
		if(!this.active) {
			this.updateStyles();
		}
		this.value = this.getNearestValue(sliderValue);
		var pixelValue= this.translateToPx(this.value);
		this.handle.style[this.isVertical() ? 'top' : 'left'] =	pixelValue;
		if (this.progress)
			this.progress.style[this.isVertical() ? 'height' : 'width'] = pixelValue;
    
		//this.drawSpans();
		if(!this.dragging || !this.event) this.updateFinished();
	},
  
	setValueBy: function(delta) {
    	this.setValue(this.value + delta);
	},
	
	translateToPx: function(value) {
		return Math.round(
      		((this.trackLength-this.handleLength)/(this.range.end-this.range.start)) * (value - this.range.start)) + "px";
	},
	
	translateToValue: function(offset) {
		return ((offset/(this.trackLength-this.handleLength) * (this.range.end-this.range.start)) + this.range.start);
	},
	
	getRange: function(range) {
		var v = this.values.sortBy(Prototype.K); 
		range = range || 0;
		return $R(v[range],v[range+1]);
	},
	
	minimumOffset: function(){
		return(this.isVertical() ? this.alignY : this.alignX);
  	},
  	
	maximumOffset: function(){
		return(this.isVertical() ? 
			(this.track.offsetHeight != 0 ? this.track.offsetHeight :
				this.track.style.height.replace(/px$/,"")) - this.alignY : 
				(this.track.offsetWidth != 0 ? this.track.offsetWidth : 
				this.track.style.width.replace(/px$/,"")) - this.alignX);
	},
	  
	isVertical:  function(){
		return (this.axis == 'vertical');
	},
	
	updateStyles: function() {
		if (this.active) 
			Element.addClassName(this.handle, 'selected');
		else
			Element.removeClassName(this.handle, 'selected');
	},
	
	startDrag: function(event) {
		if(Event.isLeftClick(event)) {
			if(!this.disabled){
				this.active = true;
				var handle = Event.element(event);
				var pointer  = [Event.pointerX(event), Event.pointerY(event)];
				var track = handle;
				if(track==this.track) {
					var offsets  = this.track.cumulativeOffset(); 
					this.event = event;
					this.setValue(this.translateToValue( 
						(this.isVertical() ? pointer[1]-offsets[1] : pointer[0]-offsets[0])-(this.handleLength/2)
					));
					var offsets  = this.handle.cumulativeOffset();
					this.offsetX = (pointer[0] - offsets[0]);
					this.offsetY = (pointer[1] - offsets[1]);
				} else {
					this.updateStyles();
					var offsets  = this.handle.cumulativeOffset();
					this.offsetX = (pointer[0] - offsets[0]);
					this.offsetY = (pointer[1] - offsets[1]);
				}
			}
			Event.stop(event);
		}
	},
	
	update: function(event) {
		if(this.active) {
			if(!this.dragging) this.dragging = true;
			this.draw(event);
			if(Prototype.Browser.WebKit) window.scrollBy(0,0);
			Event.stop(event);
		}
	},
	
	draw: function(event) {
		var pointer = [Event.pointerX(event), Event.pointerY(event)];
		var offsets = this.track.cumulativeOffset();
		pointer[0] -= this.offsetX + offsets[0];
		pointer[1] -= this.offsetY + offsets[1];
		this.event = event;
		this.setValue(this.translateToValue( this.isVertical() ? pointer[1] : pointer[0] ));
		if(this.initialized && this.options.onSlide)
			this.options.onSlide(this.value, this);
	},
	
	endDrag: function(event) {
		if(this.active && this.dragging) {
			this.finishDrag(event, true);
			Event.stop(event);
		}
		this.active = false;
		this.dragging = false;
	},  
  
	finishDrag: function(event, success) {
		this.active = false;
		this.dragging = false;
		this.updateFinished();
	},
	
	updateFinished: function() {
		this.hiddenField.value=this.value;
		this.updateStyles();
		if(this.initialized && this.options.onChange) 
			this.options.onChange(this.value, this);
		this.event = null;
		if (this.options['AutoPostBack']==true)
		{
			Event.fireEvent(this.hiddenField,"change");
		}
	}

});