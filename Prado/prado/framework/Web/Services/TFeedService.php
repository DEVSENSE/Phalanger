<?php
/**
 * TFeedService and TFeed class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @author Knut Urdalen <knut.urdalen@gmail.com>
 * @link http://www.pradosoft.com
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Web.Services
 */

/**
 * TFeedService class
 *
 * TFeedService provides to end-users feed content.
 *
 * TFeedService manages a set of feeds. The service parameter, referring
 * to the ID of the feed, specifies which feed content to be provided to end-users.
 *
 * To use TFeedService, configure it in application configuration as follows,
 * <code>
 *  <service id="feed" class="System.Web.Services.TFeedService">
 *    <feed id="ch1" class="Path.To.FeedClass1" .../>
 *    <feed id="ch2" class="Path.To.FeedClass2" .../>
 *    <feed id="ch3" class="Path.To.FeedClass3" .../>
 *  </service>
 * </code>
 * where each &lt;feed&gt; element specifies a feed identified by its "id" value (case-sensitive).
 * The class attribute indicates which PHP class will provide the actual feed
 * content. Note, the class must implement {@link IFeedContentProvider} interface.
 * Other initial properties for the feed class may also be specified in the
 * corresponding &lt;feed&gt; element.
 *
 * To retrieve the feed content identified by "ch2", use the URL
 * <code>/path/to/index.php?feed=ch2</code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @author Knut Urdalen <knut.urdalen@gmail.com>
 * @package System.Web.Services
 * @since 3.1
 */
class TFeedService extends TService
{
	private $_feeds=array();

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * @param TXmlElement configuration for this module, can be null
	 */
	public function init($config)
	{
		foreach($config->getElementsByTagName('feed') as $feed)
		{
			if(($id=$feed->getAttributes()->remove('id'))!==null)
				$this->_feeds[$id]=$feed;
			else
				throw new TConfigurationException('feedservice_id_required');
		}
	}

	/**
	 * @return string the requested feed path
	 */
	protected function determineRequestedFeedPath()
	{
		return $this->getRequest()->getServiceParameter();
	}

	/**
	 * Runs the service.
	 * This method is invoked by application automatically.
	 */
	public function run()
	{
		$id=$this->getRequest()->getServiceParameter();
		if(isset($this->_feeds[$id]))
		{
			$feedConfig=$this->_feeds[$id];
			$properties=$feedConfig->getAttributes();
			if(($class=$properties->remove('class'))!==null)
			{
				$feed=Prado::createComponent($class);
				if($feed instanceof IFeedContentProvider)
				{
					// init feed properties
					foreach($properties as $name=>$value)
						$feed->setSubproperty($name,$value);
					$feed->init($feedConfig);

					$content=$feed->getFeedContent();
				    //$this->getResponse()->setContentType('application/rss+xml');
				    $this->getResponse()->setContentType($feed->getContentType());
				    $this->getResponse()->write($content);
				}
				else
					throw new TConfigurationException('feedservice_feedtype_invalid',$id);
			}
			else
				throw new TConfigurationException('feedservice_class_required',$id);
		}
		else
			throw new THttpException(404,'feedservice_feed_unknown',$id);
	}
}

/**
 * IFeedContentProvider interface.
 *
 * IFeedContentProvider interface must be implemented by a feed class who
 * provides feed content.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @author Knut Urdalen <knut.urdalen@gmail.com>
 * @package System.Web.Services
 * @since 3.1
 */
interface IFeedContentProvider
{
	/**
	 * Initializes the feed content provider.
	 * This method is invoked (before {@link getFeedContent})
	 * when the feed provider is requested by a user.
	 * @param TXmlElement configurations specified within the &lt;feed&gt; element
	 * corresponding to this feed provider when configuring {@link TFeedService}.
	 */
	public function init($config);
	/**
	 * @return string feed content in proper XML format
	 */
	public function getFeedContent();
	/**
	 * Sets the content type of the feed content to be sent.
	 * Some examples are:
	 * RSS 1.0 feed: application/rdf+xml
	 * RSS 2.0 feed: application/rss+xml or application/xml or text/xml
	 * ATOM feed: application/atom+xml
	 * @return string the content type for the feed content.
	 * @since 3.1.1
	 */
	public function getContentType();
}

