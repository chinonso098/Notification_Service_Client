using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationServiceClient
{
    public class Notification
    {
        public string ExternalReferenceID { get; set; }
        public int MessageID { get; set; }
        public string MessageTitle { get; set; }
        public string Receiver { get; set; }
        public string MessageBody { get; set; }
        public string DeliveryType { get; set; }
        public bool ResponseNeeded { get; set; }
        public string ErrorMessage { get; set; }
        public Nullable<System.DateTime> EndsAt { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
