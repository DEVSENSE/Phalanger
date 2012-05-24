Prado.WebUI.TTabPanel = Class.create();
Prado.WebUI.TTabPanel.prototype =
{
	initialize : function(options)
	{
		this.element = $(options.ID);
		this.onInit(options);
		Prado.Registry.set(options.ID, this);
	},

	onInit : function(options)
	{
		this.views = options.Views;
		this.viewsvis = options.ViewsVis;
		this.hiddenField = $(options.ID+'_1');
		this.activeCssClass = options.ActiveCssClass;
		this.normalCssClass = options.NormalCssClass;
		var length = options.Views.length;
		for(var i = 0; i<length; i++)
		{
			var item = options.Views[i];
			var element = $(item+'_0');
			if (options.ViewsVis[i])
			{
				Event.observe(element, "click", this.elementClicked.bindEvent(this,item));
			}
			
			if(element)
			{
				if(this.hiddenField.value == i)
				{
					element.className=this.activeCssClass;
					$(options.Views[i]).show();
				} else {
					element.className=this.normalCssClass;
					$(options.Views[i]).hide();
				}
			}
		}
	},

	elementClicked : function(event,viewID)
	{
		var length = this.views.length;
		for(var i = 0; i<length; i++)
		{
			var item = this.views[i];
			if ($(item))
			{
				if(item == viewID)
				{
					$(item+'_0').className=this.activeCssClass;
					$(item).show();
					this.hiddenField.value=i;
				}
				else
				{
					$(item+'_0').className=this.normalCssClass;
					$(item).hide();
				}
			}
		}
	}
};
