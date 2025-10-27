sudo systemctl stop discountapi.service
rm -rf /home/dietpi/discountapi
mkdir /home/dietpi/discountapi
dotnet publish -c Release -o /home/dietpi/discountapi --self-contained true
sudo systemctl start discountapi.service