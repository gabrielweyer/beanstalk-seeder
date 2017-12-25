using System.Collections.Generic;

namespace BeanstalkSeeder.Models
{
    public class WorkerMessage
    {
        public string JsonPayload { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string ReceiptHandle { get; set; }
    }
}