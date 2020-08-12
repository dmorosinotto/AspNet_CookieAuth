using System.Collections.Generic;
using System.Linq;
using ConfArch.Data.Models;


namespace ConfArch.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private List<User> users = new List<User>
        {
            //new User { Id = 3522, Name = "roland", Password = "K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=",
            //ConferenceRepository.cs    FavoriteColor = "blue", Role = "Admin", GoogleId = "101517359495305583936" },
            new User { Id = 3522, Name = "lele", Password = "+MFI1qpRqof4h/BLDV3UZAeh4w/CM2PeDLMJko77U1U=", //kotp
                FavoriteColor = "red", Role = "Admin", GoogleId = "110760409360717824457"} //daniele.morosinotto@gmail.com
        };

        public User GetByUsernameAndPassword(string username, string password)
        {
            var user = users.SingleOrDefault(u => u.Name == username &&
                u.Password == password.Sha256());
            //System.Console.WriteLine($"password={password} - PASSWORDHASH=\t{password.Sha256()}");
            //return users[0];
            return user;
        }

        public User GetByGoogleId(string googleId)
        {
            var user = users.SingleOrDefault(u => u.GoogleId == googleId);
            //System.Console.WriteLine($"GOOGLEID=\t{googleId}");
            //return users[0];
            return user;
        }
    }
}
