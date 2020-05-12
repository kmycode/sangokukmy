#!/bin/bash

cp /home/sangokukmy/publish_settings/appsettings.Production.json /home/sangokukmy/publish/appsettings.Production.json
cp /home/sangokukmy/publish_settings/NLog.config /home/sangokukmy/publish/NLog.config
pwd +r /home/sangokukmy/publish/appsettings.Production.json
pwd +r /home/sangokukmy/publish/NLog.config
mkdir /home/sangokukmy/publish/logs
chmod 0777 /home/sangokukmy/publish/logs
systemctl start sangokukmy
