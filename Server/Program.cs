using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins(config["url"])
                                                .AllowAnyHeader()
                                                .AllowAnyMethod()
                                                .AllowAnyOrigin();
                      });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

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
app.MapPut("recipes/edit-recipe/{id}", async (Guid id, Recipe editedRecipe) =>
{
    // Input format -value \n -value.
    List<Recipe> recipes = await ReadFile();
    int index = recipes.FindIndex(r => r.Id == id);
    if(index!=-1)
    {
       recipes[index]=editedRecipe;
       recipes[index].Categories.Sort((x, y) => string.Compare(x, y));;
       UpdateFile(recipes);
       return Results.Ok(recipes.Find(r => r.Id == id));
    }
        return Results.BadRequest();
   
});

// Listing a recipe.
app.MapGet("recipes/list-recipe/{id}", async (Guid id) =>
{
    List<Recipe> recipes = await ReadFile();
    Recipe foundRecipe = recipes.Find(r => r.Id == id);
    if (foundRecipe==null)
        return Results.NotFound();
    else
        return Results.Ok(foundRecipe);
});

// Deleting a recipe.
app.MapDelete("recipes/delete-recipe/{id}", async (Guid id) =>
{
    List<Recipe> recipes = await ReadFile();
    bool isRemoved=recipes.Remove(recipes.Find(r => r.Id == id));
    if(!isRemoved)
    {
       return Results.BadRequest("This recipe does not exist.");
    }
    else 
    { 
       UpdateFile(recipes);
       return Results.Ok("Successfuly deleted");
    }

});

// Adding a category.
app.MapPost("recipes/add-category", async (Categories category) =>
{
    List<Categories> categories =await ReadCategories();
    if (categories.Any())
    {
        if(categories.FindIndex(c=>c.Name==category.Name) ==-1)
        { 
            categories.Add(category);
            categories.Sort((x, y) => string.Compare(x.Name, y.Name));
            UpdateCategories(categories);
            return Results.Created("Successfully added a category",category);
        }
        else
        {
            return Results.BadRequest("This category already exists"); 
        }
    }
    else
    {
        return Results.BadRequest();
    }
});

// Renaming a category.
app.MapPut("categories/rename-category", async (string oldName, string newName) =>
{
    if(oldName==newName)
    {
        return Results.BadRequest("you have entered the same name"); 
    }

    // Renaming category in the categories file.
    List<Categories> categories =await ReadCategories();
    int index = categories.FindIndex(c => c.Name == oldName);
    if (index != -1)
    {
        if(categories.FindIndex(c=>c.Name==newName) ==-1)
        {
            categories[index].Name = newName;
            categories.Sort((x, y) => string.Compare(x.Name, y.Name));
            UpdateCategories(categories);
            
            // Renaming category in the recipes file.
            List<Recipe> recipes = await ReadFile();
            List<Recipe> beforeRename = recipes.FindAll(r => r.Categories.Contains(oldName));
            if (beforeRename.Count!=0)
            {
                foreach (Recipe r in beforeRename)
                {
                    int i = r.Categories.FindIndex(cat => cat == oldName);
                    if (i != -1)
                    {
                        r.Categories[i] = newName;
                        r.Categories.Sort((x, y) => string.Compare(x, y));
                    }
                }
                UpdateFile(recipes);
            }
            return Results.Ok("Successfully updated");
        }
        else
        {
            return Results.BadRequest("new category name already exists"); 
        }
    }
    else
    {
        return Results.BadRequest("old category does not exist.");
    }
});

// Removing a category.
app.MapDelete("recipes/remove-category/{category}", async (string category) =>
{
    // Removing from the categories file.
    List<Categories> categories =await ReadCategories();
    bool isRemoved=categories.Remove(categories.Find(c => c.Name == category));
    if(!isRemoved)
    {
       return Results.BadRequest("This category does not exist.");
    }
    else 
    { 
       UpdateCategories(categories);
       // Removing from the recipes file.
       List<Recipe> recipes = await ReadFile();
       bool foundRecipe = false;
       foreach (Recipe r in recipes.ToList())
       {
            if (r.Categories[0] == category && r.Categories.Count == 1)
            {
                foundRecipe = true;
                recipes.Remove(r);
            }
            else
            {
                if (r.Categories.Contains(category))
                {
                    foundRecipe = true;
                    r.Categories.Remove(category);
                }
            }
       }
       if (foundRecipe)
       {
           UpdateFile(recipes);
       }
       return Results.Ok("Successfuly deleted.");
    }
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
    List<Categories> recipes =await ReadCategories();
    return Results.Ok(recipes);
});
app.UseCors(MyAllowSpecificOrigins);
app.Run();

// Reading the recipes json file content.
static async Task<List<Recipe>> ReadFile()
{
    string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string sFile = System.IO.Path.Combine(Environment.CurrentDirectory,"Text.json");
    string sFilePath = Path.GetFullPath(sFile);
    string jsonString = await File.ReadAllTextAsync(sFilePath);
    List<Recipe>? menu = System.Text.Json.JsonSerializer.Deserialize<List<Recipe>>(jsonString);
    return menu;
}

// Reading the categories json file content.
static async Task<List<Categories>> ReadCategories()
{
    string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string sFile = System.IO.Path.Combine(Environment.CurrentDirectory,"Categories.json");
    string sFilePath = Path.GetFullPath(sFile);
    string jsonString = await File.ReadAllTextAsync(sFilePath);
    List<Categories>? menu = System.Text.Json.JsonSerializer.Deserialize<List<Categories>>(jsonString);
    return menu;
}

// Updating the recipes json file content.
static async void UpdateFile(List<Recipe> newRecipes)
{
    string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string sFile = System.IO.Path.Combine(Environment.CurrentDirectory,"Text.json");
    string sFilePath = Path.GetFullPath(sFile);
    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(sFilePath, System.Text.Json.JsonSerializer.Serialize(newRecipes));
}

// Updating the categories json file content.
static async void UpdateCategories(List<Categories> newRecipes)
{
    string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string sFile = System.IO.Path.Combine(Environment.CurrentDirectory, "Categories.json");
    string sFilePath = Path.GetFullPath(sFile);
    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(sFilePath, System.Text.Json.JsonSerializer.Serialize(newRecipes));
}

