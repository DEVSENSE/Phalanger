<?php

/**
 * TSecurityManager class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSecurityManager.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Security
 */

/**
 * TSecurityManager class
 *
 * TSecurityManager provides private keys, hashing and encryption
 * functionalities that may be used by other PRADO components,
 * such as viewstate persister, cookies.
 *
 * TSecurityManager is mainly used to protect data from being tampered
 * and viewed. It can generate HMAC and encrypt the data.
 * The private key used to generate HMAC is set by {@link setValidationKey ValidationKey}.
 * The key used to encrypt data is specified by {@link setEncryptionKey EncryptionKey}.
 * If the above keys are not explicitly set, random keys will be generated
 * and used.
 *
 * To prefix data with an HMAC, call {@link hashData()}.
 * To validate if data is tampered, call {@link validateData()}, which will
 * return the real data if it is not tampered.
 * The algorithm used to generated HMAC is specified by {@link setValidation Validation}.
 *
 * To encrypt and decrypt data, call {@link encrypt()} and {@link decrypt()}
 * respectively. The encryption algorithm can be set by {@link setEncryption Encryption}.
 *
 * Note, to use encryption, the PHP Mcrypt extension must be loaded.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TSecurityManager.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Security
 * @since 3.0
 */
class TSecurityManager extends TModule
{
	const STATE_VALIDATION_KEY = 'prado:securitymanager:validationkey';
	const STATE_ENCRYPTION_KEY = 'prado:securitymanager:encryptionkey';

	private $_validationKey = null;
	private $_encryptionKey = null;
	private $_validation = TSecurityManagerValidationMode::SHA1;
	private $_encryption = '3DES';

	/**
	 * Initializes the module.
	 * The security module is registered with the application.
	 * @param TXmlElement initial module configuration
	 */
	public function init($config)
	{
		$this->getApplication()->setSecurityManager($this);
	}

	/**
	 * Generates a random key.
	 */
	protected function generateRandomKey()
	{
		return rand().rand().rand().rand();
	}

	/**
	 * @return string the private key used to generate HMAC.
	 * If the key is not explicitly set, a random one is generated and returned.
	 */
	public function getValidationKey()
	{
		if(null === $this->_validationKey) {
			if(null === ($this->_validationKey = $this->getApplication()->getGlobalState(self::STATE_VALIDATION_KEY))) {
				$this->_validationKey = $this->generateRandomKey();
				$this->getApplication()->setGlobalState(self::STATE_VALIDATION_KEY, $this->_validationKey, null);
			}
		}
		return $this->_validationKey;
	}

	/**
	 * @param string the key used to generate HMAC
	 * @throws TInvalidDataValueException if the key is empty
	 */
	public function setValidationKey($value)
	{
		if('' === $value)
			throw new TInvalidDataValueException('securitymanager_validationkey_invalid');

		$this->_validationKey = $value;
	}

	/**
	 * @return string the private key used to encrypt/decrypt data.
	 * If the key is not explicitly set, a random one is generated and returned.
	 */
	public function getEncryptionKey()
	{
		if(null === $this->_encryptionKey) {
			if(null === ($this->_encryptionKey = $this->getApplication()->getGlobalState(self::STATE_ENCRYPTION_KEY))) {
				$this->_encryptionKey = $this->generateRandomKey();
				$this->getApplication()->setGlobalState(self::STATE_ENCRYPTION_KEY, $this->_encryptionKey, null);
			}
		}
		return $this->_encryptionKey;
	}

	/**
	 * @param string the key used to encrypt/decrypt data.
	 * @throws TInvalidDataValueException if the key is empty
	 */
	public function setEncryptionKey($value)
	{
		if('' === $value)
			throw new TInvalidDataValueException('securitymanager_encryptionkey_invalid');

		$this->_encryptionKey = $value;
	}

	/**
	 * @return TSecurityManagerValidationMode hashing algorithm used to generate HMAC. Defaults to TSecurityManagerValidationMode::SHA1.
	 */
	public function getValidation()
	{
		return $this->_validation;
	}

	/**
	 * @param TSecurityManagerValidationMode hashing algorithm used to generate HMAC.
	 */
	public function setValidation($value)
	{
		$this->_validation = TPropertyValue::ensureEnum($value, 'TSecurityManagerValidationMode');
	}

	/**
	 * @return string the algorithm used to encrypt/decrypt data. Defaults to '3DES'.
	 */
	public function getEncryption()
	{
		return $this->_encryption;
	}

	/**
	 * @throws TNotSupportedException Do not call this method presently.
	 */
	public function setEncryption($value)
	{
		throw new TNotSupportedException('Currently only 3DES encryption is supported');
	}

	/**
	 * Encrypts data with {@link getEncryptionKey EncryptionKey}.
	 * @param string data to be encrypted.
	 * @return string the encrypted data
	 * @throws TNotSupportedException if PHP Mcrypt extension is not loaded
	 */
	public function encrypt($data)
	{
		if(!function_exists('mcrypt_encrypt'))
			throw new TNotSupportedException('securitymanager_mcryptextension_required');

		$module = mcrypt_module_open(MCRYPT_3DES, '', MCRYPT_MODE_CBC, '');
		$key = substr(md5($this->getEncryptionKey()), 0, mcrypt_enc_get_key_size($module));
		srand();
		$iv = mcrypt_create_iv(mcrypt_enc_get_iv_size($module), MCRYPT_RAND);
		mcrypt_generic_init($module, $key, $iv);
		$encrypted = $iv.mcrypt_generic($module, $data);
		mcrypt_generic_deinit($module);
		mcrypt_module_close($module);
		return $encrypted;
	}

	/**
	 * Decrypts data with {@link getEncryptionKey EncryptionKey}.
	 * @param string data to be decrypted.
	 * @return string the decrypted data
	 * @throws TNotSupportedException if PHP Mcrypt extension is not loaded
	 */
	public function decrypt($data)
	{
		if(!function_exists('mcrypt_decrypt'))
			throw new TNotSupportedException('securitymanager_mcryptextension_required');
		
		$module = mcrypt_module_open(MCRYPT_3DES, '', MCRYPT_MODE_CBC, '');
		$key = substr(md5($this->getEncryptionKey()), 0, mcrypt_enc_get_key_size($module));
		$ivSize = mcrypt_enc_get_iv_size($module);
		$iv = substr($data, 0, $ivSize);
		mcrypt_generic_init($module, $key, $iv);
		$decrypted = mdecrypt_generic($module, substr($data, $ivSize));
		mcrypt_generic_deinit($module);
		mcrypt_module_close($module);
		return $decrypted;
	}

	/**
	 * Prefixes data with an HMAC.
	 * @param string data to be hashed.
	 * @return string data prefixed with HMAC
	 */
	public function hashData($data)
	{
		$hmac = $this->computeHMAC($data);
		return $hmac.$data;
	}

	/**
	 * Validates if data is tampered.
	 * @param string data to be validated. The data must be previously
	 * generated using {@link hashData()}.
	 * @return string the real data with HMAC stripped off. False if the data
	 * is tampered.
	 */
	public function validateData($data)
	{
		$len = 'SHA1' === $this->_validation ? 40 : 32;
		if(strlen($data) < $len)
			return false;

		$hmac = substr($data, 0, $len);
		$data2 = substr($data, $len);
		return $hmac === $this->computeHMAC($data2) ? $data2 : false;
	}

	/**
	 * Computes the HMAC for the data with {@link getValidationKey ValidationKey}.
	 * @param string data to be generated HMAC
	 * @return string the HMAC for the data
	 */
	protected function computeHMAC($data)
	{
		if('SHA1' === $this->_validation) {
			$pack = 'H40';
			$func = 'sha1';
		} else {
			$pack = 'H32';
			$func = 'md5';
		}
		$key = $this->getValidationKey();
		$key = str_pad($func($key), 64, chr(0));
		return $func((str_repeat(chr(0x5C), 64) ^ substr($key, 0, 64)) . pack($pack, $func((str_repeat(chr(0x36), 64) ^ substr($key, 0, 64)) . $data)));
	}
}

/**
 * TSecurityManagerValidationMode class.
 * TSecurityManagerValidationMode defines the enumerable type for the possible validation modes
 * that can be used by {@link TSecurityManager}.
 *
 * The following enumerable values are defined:
 * - MD5: an MD5 hash is generated from the data and used for validation.
 * - SHA1: an SHA1 hash is generated from the data and used for validation.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TSecurityManager.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Security
 * @since 3.0.4
 */
class TSecurityManagerValidationMode extends TEnumerable
{
	const MD5 = 'MD5';
	const SHA1 = 'SHA1';
}
