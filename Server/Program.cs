using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";



builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("https://localhost:7117")
                                                .AllowAnyHeader()
                                                .AllowAnyMethod()
                                                .AllowAnyOrigin();
                      });
});
var app = builder.Build();
// Adding a recipe.
app.MapPost("recipes/add-recipe", async (Recipe recipe) =>
{
    List<Recipe> recipes = await ReadFile();
    if (recipes.Any())
    {
        recipes.Add(recipe);
        UpdateFile(recipes);
        return Results.Created("Successfully added a recipe", recipe);
    }
    return Results.BadRequest();
});

// Editing a recipe.
app.MapPut("recipes/edit-recipe/{id}", async (Guid id, string attributeName, string editedParameter) =>
{
    // Input format -value \n -value.
    List<Recipe> recipes = await ReadFile();

    List<string> newValue = editedParameter.Split("-").ToList();
    newValue.Remove(newValue[0]);
    for (int i = 0; i < newValue.Count(); i++)
    {
        newValue[i] = newValue[i].Trim();
    }

    if (attributeName == "Title")
    {
        recipes.Find(r => r.Id == id).Title = editedParameter;
    }
    else if (attributeName == "Instructions")
    {
        recipes.Find(r => r.Id == id).Instructions = newValue;
    }
    else if (attributeName == "Ingredients")
    {
        recipes.Find(r => r.Id == id).Ingredients = newValue;
    }
    else if (attributeName == "Categories")
    {
        recipes.Find(r => r.Id == id).Categories = newValue;
    }
    else
    {
        return Results.BadRequest();
    }
    UpdateFile(recipes);
    return Results.Ok(recipes.Find(r => r.Id == id));
});

// Listing a recipe.
app.MapGet("recipes/list-recipe/{title}", async (string title) =>
{
    List<Recipe> recipes = await ReadFile();
    List<Recipe> foundRecipes = recipes.FindAll(r => r.Title == title);
    if (!foundRecipes.Any())
        return Results.NotFound();
    else
        return Results.Ok(foundRecipes);
});

// Deleting a recipe
app.MapDelete("recipes/delete-recipe/{id}", async (Guid id) =>
{
    List<Recipe> recipes = await ReadFile();
    bool isRemoved=recipes.Remove(recipes.Find(r => r.Id == id));
    if(!isRemoved)
    {
       return Results.BadRequest();
    }
    else 
    { 
       UpdateFile(recipes);
       return Results.Ok();
    }

});

// Renaming a category.
app.MapPut("recipes/rename-category", async (string oldName, string newName) =>
{
    List<Recipe> recipes = await ReadFile();
    List<Recipe> beforeRename = recipes.FindAll(r => r.Categories.Contains(oldName));
    if (beforeRename.Any())
    {
        foreach (Recipe r in beforeRename)
        {
            int index = r.Categories.FindIndex(cat => cat == oldName);

            if (index != -1)
                r.Categories[index] = newName;
        }
        UpdateFile(recipes);
        return Results.Ok("Successfully updated");
    }
    return Results.BadRequest("This category does not exist.");
});

// Removing a category.
app.MapDelete("recipes/remove-category/{category}", async (string category) =>
{
    List<Recipe> recipes = await ReadFile();
    bool found = false;

    foreach (Recipe r in recipes.ToList())
    {
        if (r.Categories[0] == category && r.Categories.Count == 1)
        {
            Console.WriteLine("hnaaa");
            found = true;
            recipes.Remove(r);
        }
        else
        {
            if (r.Categories.Contains(category))
            {
                found = true;
                r.Categories.Remove(category);
            }
        }
    }
    if (found)
    {
        UpdateFile(recipes);
        return Results.Ok("Successfully deleted");
    }
    return Results.BadRequest("This category does not exist.");
});

// Getting the json file content to display it.
app.MapGet("recipes", async () =>
{
    List<Recipe> recipes = await ReadFile();
    return Results.Ok(recipes);
});

// Getting the json file content of the categories.
app.MapGet("categories", async () =>
{
    List<Recipe> recipes = await ReadFile();
    List<string> categories = new();
    for (int i = 0; i < recipes.Count; i++)
    {
        for (int y = 0; y < recipes[i].Categories.Count; y++)
        {
            if (!categories.Contains(recipes[i].Categories[y]))
                categories.Add(recipes[i].Categories[y]);
        }
    }
    return Results.Ok(categories);
});
app.UseCors(MyAllowSpecificOrigins);
app.Run();

// Reading the json file content.
static async Task<List<Recipe>> ReadFile()
{
    string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\" + "Text.json");
    string sFilePath = Path.GetFullPath(sFile);
    string jsonString = await File.ReadAllTextAsync(sFilePath);
    List<Recipe>? menu = System.Text.Json.JsonSerializer.Deserialize<List<Recipe>>(jsonString);
    return menu;
}

// Updating the json file content.
static async void UpdateFile(List<Recipe> newRecipes)
{
    string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\" + "Text.json");
    string sFilePath = Path.GetFullPath(sFile);
    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(sFilePath, System.Text.Json.JsonSerializer.Serialize(newRecipes));
}

