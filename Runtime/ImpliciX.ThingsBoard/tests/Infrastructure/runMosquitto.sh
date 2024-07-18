
conf=$(readlink -f ./mosquitto.conf)
docker run -it --rm --name mosquitto -p 1883:1883 -p 9001:9001 -v "${conf}:/mosquitto/config/mosquitto.conf" eclipse-mosquitto 
