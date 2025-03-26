REM Run the Docker container, mapping port 8793 on the host to port 8793 in the container
docker run -d -p 8793:8793 --name cert-watcher certificate-expiration-watcher
