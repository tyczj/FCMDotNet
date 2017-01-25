using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PushNotifications
{
    public class PushClient
    {
        private static Uri FCM_URI = new Uri("https://fcm.googleapis.com/fcm/send");
        private string apiKey = "";
        private ISerializer serializer;

        public PushClient(string apiKey)
        {
            this.apiKey = apiKey;
            serializer = new JsonNetSerializer();
        }

        public async Task<T> sendMessageAsync<T>(PushMessage message) where T : class, IFCMResponse
        {
            var result = await SendMessageAsync(message);
            return result as T;
        }

        public async Task<IFCMResponse> SendMessageAsync(PushMessage message)
        {
            HttpClient httpClient = new HttpClient();
        var serializedMessage = serializer.Serialize(message);

            using (var client = httpClient)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, FCM_URI);
                request.Headers.TryAddWithoutValidation("Authorization", "key=" + apiKey);
                request.Content = new StringContent(serializedMessage, Encoding.UTF8, "application/json");

                var result = await client.SendAsync(request);

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException();
                    }
                    var errorMessage = await result.Content.ReadAsStringAsync();
                    throw new Exception(errorMessage);
                }

                var content = await result.Content.ReadAsStringAsync();

                //if contains a multicast_id field, it's a downstream message
                if (content.Contains("multicast_id"))
                {
                    return serializer.Deserialize<FCMResponse>(content);
                }

                //otherwhise it's a topic message
                return serializer.Deserialize<TopicMessageResponse>(content);
            }

        }
    }
}
