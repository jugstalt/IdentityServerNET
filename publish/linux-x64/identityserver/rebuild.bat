del app\_config\*.* /q

set /P version=verion eg 1.0.0: 

docker build -t identityserver-net-base:%version% .
docker tag identityserver-net-base:%version% identityserver-net-base:latest

cd ./default

docker build -t identityserver-net:%version% .

cd ./../dev

docker build -t identityserver-net-dev:%version% .
