#!/bin/bash

cp /home/sangokukmy/publish_settings/appsettings.Production.json /home/sangokukmy/publish/appsettings.Production.json
pwd +r /home/sangokukmy/publish/appsettings.Production.json
mkdir -p /home/sangokukmy/publish/wwwroot/images/character-default-icons
cp /home/sangokukmy/publish_settings/default-images/*.gif /home/sangokukmy/publish/wwwroot/images/character-default-icons
systemctl start sangokukmy
