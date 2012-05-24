/**
 * DropContainer control
 */
 
Prado.WebUI.DropContainer = Class.extend(Prado.WebUI.CallbackControl);

Object.extend(Prado.WebUI.DropContainer.prototype,
{
	initialize: function(options)
	{
		this.options = options;
		Object.extend (this.options, 
		{
			onDrop: this.onDrop.bind(this)
		});
		
		Droppables.add (options.ID, this.options);
		Prado.Registry.set(options.ID, this);
	},
	
	onDrop: function(dragElement, dropElement)
	{
		Prado.Callback(this.options.EventTarget, dragElement.id, null, this.options);
	}
});
