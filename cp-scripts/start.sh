#!/bin/bash

cp /home/sangokukmy/publish_settings/appsettings.Production.json /home/sangokukmy/publish/appsettings.Production.json
pwd +r /home/sangokukmy/publish/appsettings.Production.json
mkdir -p /home/sangokukmy/publish/wwwroot/images/character-default-icons
cp /home/sangokukmy/publish_settings/default-images/*.gif /home/sangokukmy/publish/wwwroot/images/character-default-icons
mv /home/sangokukmy/publish_settings/character-uploaded-icons /home/sangokukmy/publish/wwwroot/images
mv /home/sangokukmy/publish_settings/character-historical-uploaded-icons /home/sangokukmy/publish/wwwroot/images
mkdir /home/sangokukmy/publish/logs
chmod 0777 /home/sangokukmy/publish/logs
chmod 0777 /home/sangokukmy/publish/wwwroot/images/character-uploaded-icons
systemctl start sangokukmy
