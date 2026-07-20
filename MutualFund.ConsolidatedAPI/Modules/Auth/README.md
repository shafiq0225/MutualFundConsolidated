---
title: Mutual Fund Auth API
emoji: 🔐
colorFrom: indigo
colorTo: purple
sdk: docker
app_port: 7860
---

# Mutual Fund Auth API

This is the Authentication Microservice for the Mutual Fund App, containerized and configured for deployment on Hugging Face Spaces.

## How to Deploy on Hugging Face Spaces

1. Create a new Space on [Hugging Face](https://huggingface.co/spaces).
2. Select **Docker** as the SDK.
3. Push the contents of this folder (containing the `Dockerfile`, `README.md`, and all `.csproj` subfolders) to the space's repository.
4. Go to **Settings** of the Space and add the following **Variables** / **Secrets**:
   * **Secret**: `ConnectionStrings__DefaultConnection` -> Value of your SQL Server connection string (e.g. from MonsterASP.net).
   * **Secret**: `JwtSettings__SecretKey` -> Your JWT signing key.
