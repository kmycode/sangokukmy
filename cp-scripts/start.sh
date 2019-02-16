#!/bin/bash

cp /home/sangokukmy/publish_settings/appsettings.Production.json /home/sangokukmy/publish/appsettings.Production.json
pwd +r /home/sangokukmy/publish/appsettings.Production.json
systemctl start sangokukmy
