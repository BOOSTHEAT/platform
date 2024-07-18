
docker_context=$(dirname $0)

docker build -t boostheat/dotnet_sdk:latest ${docker_context}

