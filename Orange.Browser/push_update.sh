dotnet publish -c Release 
ssh dietpi@192.168.1.160 "rm -rf ~/discountwww/*"
rsync -avz --progress ~/RiderProjects/OrangePriceCalc/Orange.Browser/bin/Release/net9.0-browser/publish/wwwroot dietpi@192.168.1.160:~/discountwww/
ssh dietpi@192.168.1.160 "cd ~/discountwww && mv wwwroot/* ./ && rm -rf wwwroot"
sudo shutdown now