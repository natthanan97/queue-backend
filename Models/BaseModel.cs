using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace queue_backend.Models
{
    public class BaseModel
    {
        public int ID { get; set; }
        public bool Valid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}