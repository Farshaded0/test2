#!/bin/bash

echo "Installing .NET MAUI workload..."
dotnet workload install maui

echo "Restoring NuGet packages..."
dotnet restore

echo "Setup complete!"
