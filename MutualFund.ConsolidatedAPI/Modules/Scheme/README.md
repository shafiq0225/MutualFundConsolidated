---
title: Mutual Fund Scheme API
emoji: 📊
colorFrom: yellow
colorTo: orange
sdk: docker
app_port: 7860
---

# Mutual Fund Scheme API

This is the Scheme Microservice for the Mutual Fund App, containerized and configured for deployment on Hugging Face Spaces.

## How to Deploy on Hugging Face Spaces

1. Create a new Space on [Hugging Face](https://huggingface.co/spaces).
2. Select **Docker** as the SDK.
3. Push the contents of this folder (containing the `Dockerfile`, `README.md`, and all `.csproj` subfolders) to the space's repository.
4. Go to **Settings** of the Space and add the following **Variables** / **Secrets**:
   * **Secret**: `ConnectionStrings__DefaultConnection` -> Value of your SQL Server connection string (e.g. from MonsterASP.net).
   * **Secret**: `JwtSettings__SecretKey` -> Your JWT signing key.
   * **Variable**: `Kafka__BootstrapServers` -> The Bootstrap Server URL of your secure remote Kafka (e.g. from Aiven or Upstash).
   * **Secret**: `Kafka__SaslUsername` -> Your Kafka username.
   * **Secret**: `Kafka__SaslPassword` -> Your Kafka password.
   * **Variable**: `Kafka__SecurityProtocol` -> `SaslSsl`
   * **Variable**: `Kafka__SaslMechanism` -> `ScramSha256` (or `Plain` if using Upstash).
