set MASTER_PORT=5000
set WORLD_WEBAPI_PORT=5001
set WORLD_PORT=4002
set LOGIN_PORT=1337
set HOST=127.0.0.1
set DB_HOST=host.docker.internal

cd ..

docker-compose up db
PAUSE