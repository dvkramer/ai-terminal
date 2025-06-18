#!/bin/bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet
dotnet --version
dotnet new --list
