# successapi

## Setup Info (ubuntu vps)
sudo apt-get update
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

## Create local binary on dev machine
dotnet publish -c Release -r linux-x64 --self-contained false
scp -r bin/Release/net8.0/linux-x64/publish/* username@your-vps-ip:/home/username/myapi/

## Setup SystemD Service
sudo nano /etc/systemd/system/timestampapi.service

"[Unit]
Description=Timestamp API Service
After=network.target

[Service]
WorkingDirectory=/home/username/myapi
ExecStart=/usr/bin/dotnet /home/username/myapi/API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=timestamp-api
User=username
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000

[Install]
WantedBy=multi-user.target"

sudo systemctl enable timestampapi.service
sudo systemctl start timestampapi.service
sudo systemctl status timestampapi.service

## Setup reverse proxy nginx
sudo apt-get install nginx
sudo nano /etc/nginx/sites-available/timestampapi

server {
    listen 80;
    server_name ip.ip.ip;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

sudo ln -s /etc/nginx/sites-available/timestampapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
