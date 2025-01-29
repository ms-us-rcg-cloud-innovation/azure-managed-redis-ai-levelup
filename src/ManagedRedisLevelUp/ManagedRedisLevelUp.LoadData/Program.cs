﻿using Azure.Identity;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using StackExchange.Redis;
using System.Globalization;
using System.IO.Compression;

namespace ManagedRedisLevelUp.LoadData
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var builder = new ConfigurationBuilder()
          .AddUserSecrets<Program>();

      var config = builder.Build();

      //var zipFilePath = "RAW_recipes.csv.zip";
      //var csvFileName = "RAW_recipes.csv";

      //using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
      //ZipArchiveEntry entry = archive.GetEntry(csvFileName)
      //  ?? throw new FileNotFoundException("The specified file was not found in the archive.", "RAW_recipes.csv");

      //using StreamReader reader = new(entry.Open());
      //using CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
      //csv.Context.RegisterClassMap<RecipeMap>();
      //List<Shared.Recipe> recipes = [];
      //for (int i = 0; i < 100; i++)
      //{
      //  csv.Read();
      //  recipes.Add(csv.GetRecord<Shared.Recipe>());
      //}

      // read from local JSON file and parse into List<Recipe>
      var jsonFilePath = "recipes.json";
      using StreamReader reader = new(jsonFilePath);
      var recipes = System.Text.Json.JsonSerializer.Deserialize<List<Shared.Recipe>>(reader.ReadToEnd()) 
        ?? throw new InvalidOperationException("The recipes list is null.");


#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      var kernel = Kernel.CreateBuilder()
          .AddRedisVectorStore(config.GetConnectionString("REDIS"))
          .AddAzureOpenAITextEmbeddingGeneration(
              deploymentName: config.GetSection("AOAI")["DEPLOYMENT_NAME"],
              endpoint: config.GetSection("AOAI")["ENDPOINT"],
              apiKey: config.GetSection("AOAI")["KEY"]
          ).Build();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      var textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
      var vectorStore = kernel.GetRequiredService<IVectorStore>();
      var collection = vectorStore.GetCollection<string, Shared.Recipe>("recipes");

      await collection.CreateCollectionIfNotExistsAsync();

      var redisConfig = new ConfigurationOptions();

      var _redisHostName = "amrlevelup-errordemo.southcentralus.redis.azure.net:10000";// config["REDIS"];
      var configurationOptions = await ConfigurationOptions.Parse($"{_redisHostName}").ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());
      ConnectionMultiplexer _newConnection = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
      IDatabase Database = _newConnection.GetDatabase();

      var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingsAsync(recipes.Select(r => r.GetEmbeddingString()).ToList());
      for (int i = 0; i < recipes.Count; i++)
      {
        recipes[i].RecipeEmbedding = embeddings[i];
      }

      var upsertTaskList = new List<Task<string>>();

      foreach (var recipe in recipes)
      {
        // Generate the recipe embedding.
        Console.WriteLine($"Generating embedding for recipe: {recipe.Key}");
        upsertTaskList.Add(collection.UpsertAsync(recipe));
      }

      await Task.WhenAll(upsertTaskList);

      var ids = upsertTaskList
        .Where(t => t.Status == TaskStatus.RanToCompletion)
        .Select(t => t.Result)
        .ToList();
      Console.WriteLine($"Total records upserted: {ids.Count}.");
    }
  }

  //public class Recipe
  //{
  //  /// <summary>A unique key for the recipe.</summary>
  //  [VectorStoreRecordKey]
  //  public string Key { get; set; } = Guid.NewGuid().ToString();
  //  public string Name { get; set; }
  //  public int Id { get; set; }
  //  public int Minutes { get; set; }
  //  public int ContributorId { get; set; }
  //  public DateTime Submitted { get; set; }
  //  public string Tags { get; set; }
  //  public string Nutrition { get; set; }
  //  public int NSteps { get; set; }
  //  public string Steps { get; set; }
  //  public string Description { get; set; }
  //  public string Ingredients { get; set; }
  //  public int NIngredients { get; set; }

  //  /// <summary>The embedding generated from the recipe text.</summary>
  //  [VectorStoreRecordVector(Dimensions: 1536)]
  //  public ReadOnlyMemory<float> RecipeEmbedding { get; set; } = new float[1536];
  //}

  //public class RecipeMap : ClassMap<Shared.Recipe>
  //{
  //  public RecipeMap()
  //  {
  //    Map(m => m.Key).Ignore();
  //    Map(m => m.Name).Name("name");
  //    Map(m => m.Id).Name("id");
  //    Map(m => m.Minutes).Name("minutes");
  //    Map(m => m.ContributorId).Name("contributor_id");
  //    Map(m => m.Submitted).Name("submitted");
  //    Map(m => m.Tags).Name("tags");
  //    Map(m => m.Nutrition).Name("nutrition");
  //    Map(m => m.NSteps).Name("n_steps");
  //    Map(m => m.Steps).Name("steps");
  //    Map(m => m.Description).Name("description");
  //    Map(m => m.Ingredients).Name("ingredients");
  //    Map(m => m.NIngredients).Name("n_ingredients");
  //    // Ignore the RecipeEmbedding property
  //    Map(m => m.RecipeEmbedding).Ignore();
  //  }
  //}
}