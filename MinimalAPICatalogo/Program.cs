using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinimalAPICatalogo.Context;
using MinimalAPICatalogo.Models;
using MinimalAPICatalogo.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => 
    options
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddSingleton<ITokenService>(new TokenService());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

//Endpoint para login
app.MapPost("/login", [AllowAnonymous] (UserModel userModel, ITokenService tokenService) =>
{
    if (userModel == null)
    {
        return Results.BadRequest("Login inválido");
    }
    if (userModel.UserName == "Sundjata" && userModel.Password == "user123")
    {
        var tokenString = tokenService.GerarToken(app.Configuration["Jwt:Key"],
            app.Configuration["Jwt:Issuer"],
            app.Configuration["Jwt:Audience"],
            userModel);

        return Results.Ok(new { token = tokenString });
    }
    else
    {
        return Results.BadRequest("Login inválido");
    }

}).Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status200OK)
    .WithName("Login")
    .WithTags("Auteticacao");

//definir os endpoints
//app.MapGet("/", () => "Catalogo de Produtos - 2023");

app.MapPost("/categorias", async(Categoria categoria, AppDbContext db) 
        => {
            db.Categorias.Add(categoria);
            await db.SaveChangesAsync();

            return Results.Created($"/categorias/{categoria.CategoriaId}", categoria);
        });

app.MapGet("/categorias", async(AppDbContext db) => 
    await db.Categorias.ToListAsync()).RequireAuthorization();

app.MapGet("/categorias/{id:int}", async(int id, AppDbContext db)
    =>{
    return await db.Categorias.FindAsync(id)
            is Categoria categoria
            ? Results.Ok(categoria)
            : Results.NotFound();
});

app.MapPut("/categorias/{id:int}", async(int id, Categoria categoria, AppDbContext db) 
    =>{

        if (categoria.CategoriaId != id) return Results.BadRequest();

        var categoriaDB = await db.Categorias.FindAsync(id);

        if (categoriaDB is null) return Results.NotFound();

        categoriaDB.Nome = categoria.Nome;
        categoriaDB.Descricao = categoria.Descricao;

        await db.SaveChangesAsync();

        return Results.Ok(categoria);
});

app.MapDelete("/categoria/{id:int}", async(int id, AppDbContext db) => 
    {
        var categoria = await db.Categorias.FindAsync(id);

        if(categoria is null) return Results.NotFound();

        db.Categorias.Remove(categoria);
        await db.SaveChangesAsync();

        return Results.NoContent();
    });

//----------------- endpoints para produto ------------
app.MapPost("/produtos", async(Produto produto, AppDbContext db)
 =>{
     db.Produtos.Add(produto);
     await db.SaveChangesAsync();

     return Results.Created($"/produtos/{produto.ProdutoId}", produto);
});

app.MapGet("/produtos", async (AppDbContext db) => 
    await db.Produtos.ToListAsync()).RequireAuthorization();

app.MapGet("/produtos/{id:int}", async (int id, AppDbContext db) 
    => { 
        return await db.Produtos.FindAsync(id)
                     is Produto produto
                     ? Results.Ok(produto)
                     : Results.NotFound();
    });

app.MapPut("/produtos/{id:int}", async (int id, Produto produto ,AppDbContext db) => 
{
        if (produto.ProdutoId != id) return Results.BadRequest("Deu ruim!");

        var produtoDB = await db.Produtos.FindAsync(id);

        if(produtoDB is null) return Results.NotFound("Não achou...");

        produtoDB.Nome = produto.Nome;
        produtoDB.Descricao = produto.Descricao;
        produtoDB.Imagem = produto.Imagem;
        produtoDB.Categoria = produto.Categoria;
        produtoDB.Estoque = produto.Estoque;
        produtoDB.DataCompra = produto.DataCompra;
        produtoDB.CategoriaId = produto.CategoriaId;

        await db.SaveChangesAsync();

        return Results.Ok(produtoDB + " Deu certo!");
});

app.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) => 
{
    var produto = await db.Produtos.FindAsync(id);

    if(produto is null) return Results.NotFound();

    db.Produtos.Remove(produto);
    await db.SaveChangesAsync();

    return Results.NoContent();
    
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
