#!/bin/bash
# Render build script
dotnet restore
dotnet publish -c Release -o out