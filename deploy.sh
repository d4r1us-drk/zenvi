#!/bin/bash

# Set environment variables
export DB_NAME=zenvidb
export DB_USER=zenviuser
export DB_PASSWORD=zenvipassword
export DB_ROOT_PASSWORD=rootpassword
export CONNECTION_STRING="Server=db;Database=$DB_NAME;User=$DB_USER;Password=$DB_PASSWORD;"

# Deploy the containers using Docker Compose
docker-compose up --build
