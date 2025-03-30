# CertificateExpirationWatcher

This lightweight program is designed to run in the background. Ideally as a docker container.  
It periodically checks specific website certificates to warn the user of their impending expiration.  

> The reason I developed this is because LetsEncrypt stopped sending expiration warning emails to free users.
> I wanted something very simple and easy to configure to receive notice of upcoming certificate expiration

## Features

- Can watch multiple websites
- Supports custom reminders
- Notification by email

## Configuration

You can configure the program by creating a `config.json` file.  
  
Here's the structure :  

```
{
  "emailSettings": {
    "smtpServer": string,
    "smtpPort": number,
    "smtpUser": string,
    "smtpPassword": string,
    "fromEmail": string,
    "toEmail": string
  },
  "watchers": [
    {
      "url": string,
      "expiration": string (DateTime),
      "notification": [number],
      "notificationDone": [number],
      "latest": string
    }
  ]
}
```

There are two main entries, `emailSettings` and `watchers`  

#### emailSettings

- `smtpServer` : outgoing mail server address
- `smtpPort` : outgoing mail server port
- `smtpUser` : user name for mail server login
- `smtpPassword` : password for mail server login
- `fromEmail` : sender email
- `toEmail` : recipient email (the one that will receive notifications)

#### watchers

Array - can specify multiple servers to watch  

- `url` : Url of the server to watch. Enter a valid address (i.e. should not return 4xx or 5xx error codes)
- `expiration` : current expiration date (updated by program)
- `notification` : Array - Days before expiration when notifications will be sent
- `notificationDone` : Array - Notifications that have already been sent for the current expiration date (updated by program)
- `latest` : Indicates the latest status of current watcher (updated by program)

## Building

This program uses .NET 8  
Build with `dotnet build`  
  
This program is designed to run with [Docker](https://www.docker.com/products/docker-desktop/)  
Docker image file is provided  

#### Build
`docker build -t certificate-expiration-watcher .`

#### Export image
`docker save -o certificate-expiration-watcher.tar certificate-expiration-watcher`

#### Import image
`docker load -i certificate-expiration-watcher.tar`

#### Run
`docker run -d -p 8793:8793 --name cert-watcher certificate-expiration-watcher`  
You can customize the port