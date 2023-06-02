using Microsoft.OpenApi.Models;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Driver;
using WebApplication1;
using WebApplication1.Models;

class Program
{
    static void Main(string[] args)
    {
        constants.mongoClient = new MongoClient("mongodb+srv://Vladdavydok:09123456d@cluster0.kzkmm9o.mongodb.net/");
        constants.database = constants.mongoClient.GetDatabase("park");
        constants.collection = constants.database.GetCollection<BsonDocument>("collection1");

        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ParksForecastAPI",
                Version = "v1"
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            c.RoutePrefix = string.Empty;
        });

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}