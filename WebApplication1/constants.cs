using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApplication1
{
    public class constants
    {
        public static string botId = "6119870516:AAEsfjrigfjPHMaVRbzM93oNjRGx51RMWo4";
        public static string host = "parksforecastapi20230601201108.azurewebsites.net";
        public static MongoClient mongoClient;
        public static IMongoDatabase database;
        public static IMongoCollection<BsonDocument> collection;
        public static string apikey = "AIzaSyCTpNqKxqiOJ-Lm64poVG7At9Dx3Cau6g8";
        public static string apikeyy = "ea42fe4f8cbe4df6b5e175328233005";
        public static string apikeyyy = "sgOAPtnM2VpoRFqy";
    }
}
