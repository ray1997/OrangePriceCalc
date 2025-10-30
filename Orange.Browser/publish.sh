sudo systemctl stop discountweb.service
rm -rf /home/dietpi/projects/orange/OrangePriceCalc/Orange.Browser/bin
dotnet publish -c Release
sudo systemctl start discountweb.service