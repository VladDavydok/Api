using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApplication1.Controllers;
using WebApplication1.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Controllers : ControllerBase
    {
        private readonly ILogger<Controllers> _logger;
        public Controllers(ILogger<Controllers> logger)
        {
            _logger = logger;
        }
        
        [HttpGet("get_parks")]
        public async Task<ActionResult<List<Park>>> GetParks(string CityName)
        {
            HttpClient httpClient = new HttpClient();

            var response = await httpClient.GetAsync($"https://maps.googleapis.com/maps/api/place/textsearch/json?query=parks+in+{CityName}&language=uk&key={constants.apikey}");
            string result = await response.Content.ReadAsStringAsync();

           var jsonResult = JsonConvert.DeserializeObject<JObject>(result);
            var parkList = new List<Park>();

            var results = jsonResult["results"] as JArray;
            foreach (var parkData in results)
            {
                 string name = parkData["name"].ToString();
                 double lat = (double)parkData["geometry"]["location"]["lat"];
                 double lng = (double)parkData["geometry"]["location"]["lng"];
                 Park park = new Park(name, lat, lng);
                 parkList.Add(park);
            }
            return Ok(parkList);
        }

        [HttpGet("get_forecast")]
        public async Task<ActionResult<List<Forecast>>> GetForecast(string CityName, string formattedDate)
        {
            HttpClient httpClient = new HttpClient();

            var response = await httpClient.GetAsync($"https://api.weatherapi.com/v1/forecast.json?key={constants.apikeyy}&q={CityName}&dt={formattedDate}");
            string result = await response.Content.ReadAsStringAsync();

            var jsonResult = JsonConvert.DeserializeObject<JObject>(result);
            var forecastList = new List<Forecast>();

            var forecastday = jsonResult["forecast"]["forecastday"] as JArray;
            foreach (var forecastData in forecastday)
            {
                string date = forecastData["date"].ToString();
                double avgtemp_c = (double)forecastData["day"]["avgtemp_c"];
                int daily_chance_of_rain = (int)forecastData["day"]["daily_chance_of_rain"];

                Forecast forecast = new Forecast(date, avgtemp_c, daily_chance_of_rain);
                forecastList.Add(forecast);
            }
            return Ok(forecastList);
        }

        [HttpGet("get_water")]
        public async Task<ActionResult<List<Water>>> GetWater(string latitude, string longitude)
        {
            HttpClient httpClient = new HttpClient();

            var response = await httpClient.GetAsync($"https://my.meteoblue.com/packages/sea-day?apikey={constants.apikeyyy}&lat={latitude}&lon={longitude}&asl=187&format=json");
            string result = await response.Content.ReadAsStringAsync();

            var jsonResult = JsonConvert.DeserializeObject<JObject>(result);
            var dataDay = jsonResult["data_day"];

            var timeArray = dataDay["time"] as JArray;
            var temperatureArray = dataDay["seasurfacetemperature_mean"] as JArray;

            List<DateTime> timeList = timeArray.Select(t => DateTime.Parse(t.ToString())).ToList();
            List<double?> temperatureList = temperatureArray.Select(t => t?.ToObject<double?>()).ToList();

            Dictionary<DateTime, double?> dataDictionary = new Dictionary<DateTime, double?>();
            for (int i = 0; i < timeList.Count; i++)
            {
                dataDictionary.Add(timeList[i], temperatureList[i]);
            }
            return Ok(dataDictionary);
        }

        [HttpPost("post_parks_list")]
        public async Task<ActionResult> PostParksList(long id, string CityName)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId); 

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("CityName", CityName);
            var results = constants.collection.UpdateOne(filter, update);
            var document = constants.collection.Find(filter).FirstOrDefault();
            string cityName = null;
            if (document != null && document.TryGetValue("CityName", out BsonValue cityNameValue))
            {
                cityName = cityNameValue.AsString;
            }
            if (cityName != null)
            {
                var response = await httpClient.GetAsync($"https://{constants.host}/Controllers/get_parks?CityName={cityName}");
                var result = await response.Content.ReadAsStringAsync();
                List<Park> parks = JsonConvert.DeserializeObject<List<Park>>(result);
                List<Park> sortedParks = parks.OrderBy(a => a.Name).ToList();
                int i = 1;
                foreach (Park park in sortedParks)
                {
                    await bot.SendTextMessageAsync(id, $"Номер парку: {i}\nНазва парку: {park.Name}\nДовгота: {park.Lat}\nШирина: {park.Lng}");
                    i++;
                }
                await bot.SendTextMessageAsync(id, $"Якщо хочете додати парк до свого списку, використайте команду /add_park_to_my_list");
            }
            else
            {
                await bot.SendTextMessageAsync(id, "Помилка: назва міста не знайдена.");
            }

            return Ok();
        }

        [HttpPost("post_forecast_list")]
        public async Task<ActionResult> PostForecastList(long id, string сityName, int number)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            double bestTemperature = double.MinValue;
            string bestTemperatureDate = string.Empty;

            for (int i = 0; i <= number; i++)
            {
                var response = await httpClient.GetAsync($"https://{constants.host}/Controllers/get_forecast?CityName={сityName}&formattedDate={DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd")}");
                var result = await response.Content.ReadAsStringAsync();
                List<Forecast> forecasts = JsonConvert.DeserializeObject<List<Forecast>>(result);
                foreach (Forecast forecast in forecasts)
                {
                    await bot.SendTextMessageAsync(id, $"Дата: {forecast.Date},\nТемпература= {forecast.Avgtemp_c}°C,\nШанс дощу= {forecast.Daily_chance_of_rain}.");
                    if(forecast.Avgtemp_c <= 20 && forecast.Daily_chance_of_rain <= 50)
                    {
                        await bot.SendTextMessageAsync(id, "На вулиці прохолодно, візьміть теплий одяг. Шанс дощу невеликий, парасольку можна не брати.");
                    }
                    if (forecast.Avgtemp_c >= 20 && forecast.Daily_chance_of_rain <= 50)
                    {
                        await bot.SendTextMessageAsync(id, "На вулиці тепло, візьміть легкий одяг. Шанс дощу невеликий, парасольку можна не брати.");
                    }
                    if (forecast.Avgtemp_c <= 20 && forecast.Daily_chance_of_rain >= 50)
                    {
                        await bot.SendTextMessageAsync(id, "На вулиці прохолодно, візьміть теплий одяг. Шанс дощу великий, візьміть з собою парасольку або дощовик.");
                    }
                    if (forecast.Avgtemp_c >= 20 && forecast.Daily_chance_of_rain >= 50)
                    {
                        await bot.SendTextMessageAsync(id, "На вулиці тепло, візьміть легкий одяг. Шанс дощу великий, візьміть з собою парасольку або дощовик.");
                    }   
                    if (forecast.Avgtemp_c > bestTemperature)
                    {
                        bestTemperature = forecast.Avgtemp_c;
                        bestTemperatureDate = forecast.Date;
                    }
                }
            }
            await bot.SendTextMessageAsync(id, $"Найкраща температура: {bestTemperature}°C  в {bestTemperatureDate}.");
            return Ok();
        }

        [HttpPost("post_water_list")]
        public async Task<ActionResult> PostWaterList(long id, string lat, string lng)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);
            var response = await httpClient.GetAsync($"https://{constants.host}/Controllers/get_water?latitude={lat}&longitude={lng}");
            var result = await response.Content.ReadAsStringAsync();
            var waterList = JsonConvert.DeserializeObject<Dictionary<string, double>>(result)
            .Select(pair => new Water(pair.Key, pair.Value))
            .ToList();
            foreach (var water in waterList)
            {
                if (water.Seasurfacetemperature_mean == null)
                {
                    await bot.SendTextMessageAsync(id, "Немає даних про температуру води");
                }
                else
                {
                    await bot.SendTextMessageAsync(id, $"Дата: {water.Time} \nТемпература води: {water.Seasurfacetemperature_mean}°C");
                    if (water.Seasurfacetemperature_mean < 20)
                    {
                        await bot.SendTextMessageAsync(id, "Вода холодна, краще не купатися");
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(id, "Вода тепла, можете брати купальник та йти купатися");
                    }
                }   
            }
            return Ok();
        }

        [HttpPut("put_park_to_list")]
        public async Task<ActionResult<Park>> PutParkToList(long id, string CityName, int number)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var response = await httpClient.GetAsync($"https://{constants.host}/Controllers/get_parks?CityName={CityName}");
            var result = await response.Content.ReadAsStringAsync();
            List<Park> parks = JsonConvert.DeserializeObject<List<Park>>(result);
            List<Park> sortedParks = parks.OrderBy(a => a.Name).ToList();
            if (number > parks.Count || number < 1)
            {
                await bot.SendTextMessageAsync(id, "Цього парка немає у списку. Щоб подивитися список використайте /parks");
                return Ok();
            }
            Park park = sortedParks[number - 1];

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();

            var my_parks = document["parks"].AsBsonArray;
            BsonValue bson_park = BsonDocument.Parse(park.ToJson());

            bool hasDuplicate = my_parks.AsQueryable().Any(a => a["Name"].AsString == park.Name);


            if (hasDuplicate)
            {
                await bot.SendTextMessageAsync(id, "Цей парк вже є у вашому списку.");
            }
            else
            {
                my_parks.Add(bson_park);
                var update = Builders<BsonDocument>.Update.Set("parks", my_parks);
                constants.collection.UpdateOne(filter, update);
                await bot.SendTextMessageAsync(id, "Парк успішно додано до вашого списку");
            }
            return Ok();
        }
        [HttpPost("post_my_parks_list")]
        public async Task<ActionResult> PostMyParksList(long id)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();

            var myParks = document["parks"].AsBsonArray;
            if (myParks.Count == 0)
            {
                await bot.SendTextMessageAsync(id, "Ваш список порожній");
            }
            else
            {
                for (int i = 0; i < myParks.Count; i++)
                {
                    await bot.SendTextMessageAsync(id, $"Номер парку у вашому списку: {i + 1}\n\nЗагальна інформація про парк:\nНазва парку: {myParks[i]["Name"]}\nШирота: {myParks[i]["Lat"]}\nДовгота: {myParks[i]["Lng"]}");

                }
            }
            return Ok();
        }

        [HttpDelete("delete_park_from_list")]
        public async Task<ActionResult> DeleteParkFromList(long id, int number)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();

            var myParks = document["parks"].AsBsonArray;
            try
            {
                myParks.RemoveAt(number - 1);
                var update = Builders<BsonDocument>.Update.Set("parks", myParks);
                constants.collection.UpdateOne(filter, update);
                await bot.SendTextMessageAsync(id, "Парк успішно видалено з вашого списку");
            }
            catch
            {
                await bot.SendTextMessageAsync(id, "Помилка");
            }
            return Ok();
        }

        [HttpPut("bot_is_waiting_for_park_city/{id}")]
        public ActionResult<string> BotIsWaitingForParkCity(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_park_city", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_park_city/{id}")]
        public ActionResult<bool> BotIsWaitingForParkCity(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_park_city", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }

        [HttpPut("bot_is_waiting_for_forecast_days/{id}")]
        public ActionResult<string> BotIsWaitingForForecastDays(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_forecast_days", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_forecast_days/{id}")]
        public ActionResult<bool> BotIsWaitingForForecastDays(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_forecast_days", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }

        [HttpPut("bot_is_waiting_for_lat/{id}")]
        public ActionResult<string> BotIsWaitingForLat(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_lat", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_lat/{id}")]
        public ActionResult<bool> BotIsWaitingForLat(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_lat", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }

        [HttpPut("bot_is_waiting_for_lng/{id}")]
        public ActionResult<string> BotIsWaitingForLng(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_lng", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_lng/{id}")]
        public ActionResult<bool> BotIsWaitingForLng(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_lng", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }

        [HttpPut("bot_is_waiting_for_number_of_park_to_add/{id}")]
        public ActionResult<string> BotIsWaitingForNumberOfParkToAdd(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_number_of_park_to_add", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_number_of_park_to_add/{id}")]
        public ActionResult<bool> BotIsWaitingForNumberOfParkToAdd(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_number_of_park_to_add", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPut("bot_is_waiting_for_number_of_park_to_delete/{id}")]
        public ActionResult<string> BotIsWaitingForNumberOfParkToDelete(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_number_of_park_to_delete", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_number_of_park_to_delete/{id}")]
        public ActionResult<bool> BotIsWaitingForNumberOfParkToDelete(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_number_of_park_to_delete", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }        
    }
}
