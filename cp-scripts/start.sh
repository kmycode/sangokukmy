﻿#!/bin/bash

cp /home/sangokukmy/publish_settings/appsettings.Production.json /home/sangokukmy/publish/appsettings.Production.json
pwd +r /home/sangokukmy/publish/appsettings.Production.json
mkdir /home/sangokukmy/publish/logs
chmod 0777 /home/sangokukmy/publish/logs
systemctl start sangokukmy
