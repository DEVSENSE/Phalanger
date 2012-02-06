#!/bin/bash

#Installing preconditions
apt-get install xmlstarlet
apt-get install apache2 libapache2-mod-mono

echo "Enter Phalanger installation directory: (default=/usr/lib/phalanger)"
read phalanger_folder

if  [ "$phalanger_folder" == "" ]; then
	phalanger_folder="/usr/lib/phalanger"
fi

cp -r phalanger $phalanger_folder

chmod 777 $phalanger_folder/dynamic

echo "Enter Mono etc directory: (default=/etc/mono)"
read mono_etc_folder

if  [ "$mono_etc_folder" == "" ]; then
	mono_etc_folder="/etc/mono/"
fi

version="3.0.0.0"
machine_config="$mono_etc_folder/4.0/machine.config"
web_config="$mono_etc_folder/4.0/web.config"
public_key="0a8e8c4c76728c71"
public_key2="4af37afe3cde05fb"
pars="-P -L"

# Adding definition of phpNet section
xmlstarlet ed $pars -s "/configuration/configSections" -t elem -n "section" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/configSections/section[last()]" -t attr -n "name" -v "phpNet" $machine_config
xmlstarlet ed $pars -s "/configuration/configSections/section[last()]" -t attr -n "type" -v "PHP.Core.ConfigurationSectionHandler, PhpNetCore, Version=$version, Culture=neutral, PublicKeyToken=$public_key" $machine_config

#Registering phpNet as compiler
xmlstarlet ed $pars -s "/configuration" -t elem -n "phpNet" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet" -t elem -n "compiler" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet" -t elem -n "paths" -v "" $machine_config

#Registering Phalangers Dynamic folder
xmlstarlet ed $pars -s "/configuration/phpNet/paths" -t elem -n "set" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/paths/set" -t attr -n "name" -v "DynamicWrappers" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/paths/set" -t attr -n "value" -v "$phalanger_folder/dynamic" $machine_config

#Setting reference to Phalangers Libraries folder
xmlstarlet ed $pars -s "/configuration/phpNet/paths" -t elem -n "set" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/paths/set[last()]" -t attr -n "name" -v "Libraries" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/paths/set[last()]" -t attr -n "value" -v "$phalanger_folder/bin" $machine_config

#Registering PhpNetClassLibrary
xmlstarlet ed $pars -s "/configuration/phpNet" -t elem -n "classLibrary" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/classLibrary" -t elem -n "add" -v "" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/classLibrary/add" -t attr -n "assembly" -v "PhpNetClassLibrary, Version=$version, Culture=neutral, PublicKeyToken=$public_key2" $machine_config
xmlstarlet ed $pars -s "/configuration/phpNet/classLibrary/add" -t attr -n "section" -v "bcl" $machine_config

#Registering Phalanger as HttpHandler
xmlstarlet ed $pars -s "/configuration/system.web/httpHandlers" -t elem -n "add" -v "" $web_config
xmlstarlet ed $pars -s "/configuration/system.web/httpHandlers/add[last()]" -t attr -n "path" -v "*.php" $web_config
xmlstarlet ed $pars -s "/configuration/system.web/httpHandlers/add[last()]" -t attr -n "verb" -v "*" $web_config
xmlstarlet ed $pars -s "/configuration/system.web/httpHandlers/add[last()]" -t attr -n "type" -v "PHP.Core.RequestHandler, PhpNetCore, Version=$version, Culture=neutral, PublicKeyToken=$public_key" $web_config

#Installing necessary assemblies in GAC
#gacutil -i $phalanger_folder/bin/PhpNetCore.IL.dll
gacutil -i $phalanger_folder/bin/PhpNetCore.dll
gacutil -i $phalanger_folder/bin/PhpNetClassLibrary.dll
gacutil -i $phalanger_folder/bin/PhpNetXmlDom.dll

#Enabling mod_mono (if it already isnt)
a2enmod mod_mono