# [Choice] .NET version: 7.0, 7.0-bullseye-slim, 7.0-jammy, 6.0, 6.0-bullseye-slim, 6.0-jammy, 6.0-focal
ARG VARIANT="8.0-jammy@sha256:f02f30ab8833e62138c7ade9c78db6870ef3c985637d6690428e13d27adc9bd2"
FROM mcr.microsoft.com/dotnet/sdk:${VARIANT}

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>