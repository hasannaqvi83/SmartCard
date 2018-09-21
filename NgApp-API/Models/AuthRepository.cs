using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NgApp_API.Models
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext _ctx;
        public AuthRepository(DataContext ctx)
        {
            this._ctx = ctx;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            var user = await _ctx.Users.FirstOrDefaultAsync(x => x.Username == username);

            if (user == null)
                return null;

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;
            return user;
        }

        //Compares the password hash of the user provided password and the hash stored in 
        //databse. This method compares each bytes with the two hashes and returns false
        //in-case a mismatch of bytes is found.
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i])
                        return false;
                }
            }
            return true;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _ctx.Users.AddAsync(user);
            await _ctx.SaveChangesAsync();

            return user;
        }

        //Creates a random salt and computes a hash based on the provided password. 
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return (await _ctx.Users.AnyAsync(x => x.Username == username));
        }
    }
}