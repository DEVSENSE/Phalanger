<?php

Prado::using('System.Exceptions.TErrorHandler');
Prado::using('Application.BlogException');

class BlogErrorHandler extends TErrorHandler
{
	/**
	 * Retrieves the template used for displaying external exceptions.
	 * This method overrides the parent implementation.
	 */
	protected function getErrorTemplate($statusCode,$exception)
	{
		// use our own template for BlogException
		if($exception instanceof BlogException)
		{
			// get the path of the error template file: protected/error.html
			$templateFile=Prado::getPathOfNamespace('Application.error','.html');
			return file_get_contents($templateFile);
		}
		else // otherwise use the template defined by PRADO
			return parent::getErrorTemplate($statusCode,$exception);
	}

	/**
	 * Handles external error caused by end-users.
	 * This method overrides the parent implementation.
	 * It is invoked by PRADO when an external exception is thrown.
	 */
	protected function handleExternalError($statusCode,$exception)
	{
		// log the error (only for BlogException)
		if($exception instanceof BlogException)
			Prado::log($exception->getErrorMessage(),TLogger::ERROR,'BlogApplication');
		// call parent implementation to display the error
		parent::handleExternalError($statusCode,$exception);
	}
}

?>