

using Microsoft.AspNetCore.Http;

namespace EmailManager.Models
{
    public class LocalInfrastructure
    {
      public  static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public async  Task<string> LocalImageStore(string path, IFormFile file)
        {
            string imgValue = null;

            var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            imgValue = file.FileName.Replace(" ", "_");

            stream.Close();

            return imgValue;
        }


        // Helper method to compute expiry
        public DateTime CalculateExpiryDate(string duration)
        {
            duration = duration.ToUpper();

            return duration switch
            {
                "DAILY" => DateTime.UtcNow.AddMinutes(2),
                "WEEKLY" => DateTime.UtcNow.AddDays(7),
                "MONTHLY" => DateTime.UtcNow.AddMonths(1),
                _ => throw new ArgumentException("Invalid duration")
            };
        }
    }
}
