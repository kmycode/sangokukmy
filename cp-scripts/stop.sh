#!/bin/bash

systemctl stop sangokukmy
mv /home/sangokukmy/publish/wwwroot/images/character-uploaded-icons /home/sangokukmy/publish_settings
mv /home/sangokukmy/publish/wwwroot/images/character-historical-uploaded-icons /home/sangokukmy/publish_settings
rm -rf /home/sangokukmy/publish
mkdir /home/sangokukmy/publish
