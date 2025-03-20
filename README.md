# Azure Managed Redis AI LevelUp

A modern Blazor Web application that combines Azure Managed Redis and vector search capabilities to create, manage, and search for recipes with semantic understanding and improved efficiency.

## Overview

This application demonstrates how to leverage Azure Managed Redis for both traditional key-value storage and advanced vector search capabilities with Azure OpenAI embeddings. The project allows users to:

- Create and edit recipes with ingredients, steps, and descriptions
- Search for recipes using semantic vector search
- Retrieve recipes by exact key lookup
- Compare different vector search approaches (Vector Range vs. Nearest Neighbors)
- Experience the performance benefits of Redis caching

## Key Technologies

- **.NET 9**: Modern C# and .NET features
- **Blazor**: Server-side interactive web UI framework
- **Azure Managed Redis**: Enterprise-grade Redis service with vector search capabilities
- **Redis OM**: Object mapping for Redis
- **Azure OpenAI**: Embedding generation for semantic search

## Features

### Recipe Management
- Full CRUD operations for recipes
- Structured data model for recipe information
- Form-based interface for creating and editing recipes

### Vector Search
- Semantic search using vector embeddings
- Multiple search approaches available:
  - **Vector Range**: For broader semantic matching
  - **Nearest Neighbors**: For more precise similarity matching
- Automatic embedding generation using Azure OpenAI

### Performance Optimization
- Redis caching for fast data retrieval
- Visual indicators for cache hits
- Efficient data structure design

## Architecture

The application consists of several key components:

- **Web UI (Blazor)**: Interactive user interface for recipe management and search
- **API Service**: Backend service for data operations and integration with Redis
- **Redis OM**: Object mapping layer for Redis operations
- **Azure OpenAI**: Embedding generation for semantic search
- **Data Loading Tool**: For initializing the recipe database

## Setup Instructions

1. **Prerequisites**:
   - .NET 9 SDK
   - Azure subscription
   - Azure Managed Redis instance
   - Azure OpenAI service

2. **Configuration**:
   - Update connection strings in user secrets or configuration files
   - Set up the necessary Azure services

3. **Running the Application**:
   - Build and run the application using standard .NET commands or Visual Studio
   - Use the LoadData tool to populate initial recipe data if needed

## Development

This project uses modern .NET development practices:

- **Project Structure**: Clean separation of concerns
- **Component-Based UI**: Reusable Blazor components
- **API Client Pattern**: Abstraction for backend communication
- **State Management**: Efficient state handling in Blazor

## License

This project is available under standard open-source licensing terms.