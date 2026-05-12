using System.Net.Http.Json;

namespace EduLearn.ReviewService
{
    public class TestApi
    {
        public static async Task TestAverageRatingEndpoint()
        {
            using var client = new HttpClient();
            
            try
            {
                // Test with a sample course ID (replace with actual course ID from your database)
                var testCourseId = "00000000-0000-0000-0000-000000000000";
                
                var response = await client.GetAsync($"http://localhost:5006/api/reviews/average/{testCourseId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {content}");
                }
                else
                {
                    Console.WriteLine($"API Error: {response.StatusCode}");
                    Console.WriteLine($"Error Content: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
