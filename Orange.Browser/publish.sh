sudo systemctl stop discountweb.service
rm -rf /home/dietpi/projects/orange/OrangePriceCalc/Orange.Browser/bin
wait 5m
sudo systemctl start discountweb.service