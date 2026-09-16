# [Choice] .NET version: 7.0, 7.0-bullseye-slim, 7.0-jammy, 6.0, 6.0-bullseye-slim, 6.0-jammy, 6.0-focal
ARG VARIANT="8.0-jammy@sha256:df5158092e643bf6407f0e048b5f8bcc231d72f963454b3dfa1f514faa992006"
FROM mcr.microsoft.com/dotnet/sdk:${VARIANT}

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>