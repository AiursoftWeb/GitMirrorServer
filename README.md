# GitMirrorServer

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/aiursoftweb/gitMirrorServer/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/gitMirrorServer/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/gitMirrorServer/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/gitMirrorServer/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/gitMirrorServer/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Fgitmirrorserver)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Fgitmirrorserver&scope=baseline&ecosystem=all)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/gitMirrorServer.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/gitMirrorServer.html)
[![Website](https://img.shields.io/website?url=https%3A%2F%2Fgitmirror.aiursoft.com)](https://gitmirror.aiursoft.com)
[![Docker](https://img.shields.io/docker/pulls/aiursoft/gitmirrorserver.svg)](https://hub.docker.com/r/aiursoft/gitmirrorserver)

GitMirrorServer is project that helps you mirror git repositories from one server to another server automatically. It is built with ASP.NET Core and supports multiple git services including GitHub, GitLab, Gitea, and Azure DevOps.

![screenshot](./screenshot.png)

Default user name is `admin@default.com` and default password is `Admin@123456!`.

## Try

For GitLab sources that require authentication, set **From Token** to a token with
`read_api` and `read_repository` access. The token is used for both project discovery
and HTTPS clone/fetch. Create or edit a mirror to choose source visibility and whether
to include archived repositories. Defaults include Public and Internal, exclude Private
and archived, and create missing GitHub repositories as Public. Internal does not mean
open source: only select repositories you are authorized to publish. Private-to-Public
GitHub mirroring requires explicit confirmation when saving. New GitHub repositories
can instead be created as Private; existing target permissions are never changed.
Excluding a source later does not remove copies already published. The form explains
credentials and permissions for first-time administrators.

Try a running GitMirrorServer [here](https://gitmirror.aiursoft.com).

## Install with Docker

Run the installer with the desired port (omit it to choose an available port):

```bash
curl -fsSL https://raw.githubusercontent.com/aiursoftweb/gitmirrorserver/master/install.sh | sudo bash -s -- 8080
```

Installation starts after three seconds; press Ctrl+C to cancel. Then open `http://localhost:8080`.

## Run manually

Requirements about how to run

1. Install [.NET 10 SDK](http://dot.net/) and [Node.js](https://nodejs.org/).
2. Execute `npm install` at `wwwroot` folder to install the dependencies.
3. Execute `dotnet run` to run the app.
4. Use your browser to view [http://localhost:5000](http://localhost:5000).

## Run in Microsoft Visual Studio

1. Open the `.sln` file in the project path.
2. Press `F5` to run the app.

## Run in Docker

First, install Docker [here](https://docs.docker.com/get-docker/).

Then run the following commands in a Linux shell:

```bash
image=aiursoft/gitMirrorServer
appName=gitMirrorServer
sudo docker pull $image
sudo docker run -d --name $appName --restart unless-stopped -p 5000:5000 -v /var/www/$appName:/data $image
```

That will start a web server at `http://localhost:5000` and you can test the app.

The docker image has the following context:

| Properties  | Value                           |
|-------------|---------------------------------|
| Image       | aiursoft/gitMirrorServer               |
| Ports       | 5000                            |
| Binary path | /app                            |
| Data path   | /data                           |
| Config path | /data/appsettings.json          |

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
