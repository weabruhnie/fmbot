using System;
using System.Collections.Generic;
using System.Text;

namespace FMBot.Persistence.Domain.Models
{
    public class GuildUser
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int GuildId { get; set; }
        public Guild Guild { get; set; }
    }
}
