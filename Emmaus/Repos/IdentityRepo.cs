using Emmaus.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Repos
{
    public interface IIdentityRepo
    {
        Task AddRolesToUserAsync(string email, IEnumerable<string> roles);
        Task AddRoleToUserAsync(string email, string role);
        Task CreateAdminUserIfRequiredAsync(string email, string password);
        Task CreateRoleAsync(string roleName);
        Task CreateRolesIfRequiredAsync(IEnumerable<string> roles);
        Task CreateUserAsync(string email, string password);
        Task CreateUserIfRequiredAsync(string email, string password);
        Task DeleteRoleAsync(string roleName);
        Task DeleteUserAsync(string email);
        IEnumerable<string> GetRoles();
        Task<IEnumerable<UserInfo>> GetUsersAsync();
        Task LogoutAsync();
        Task SignInAsync(string email, string password);
    }

    public class IdentityRepo : IIdentityRepo
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public IdentityRepo(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
        }

        public async Task DeleteUserAsync(string email)
        {
            ApplicationUser user = await GetUser(email);
            IdentityResult deletedResult = await userManager.DeleteAsync(user);
            if (!deletedResult.Succeeded)
                throw new Exception("Could not delete user.");
        }

        public async Task<IEnumerable<UserInfo>> GetUsersAsync()
        {
            var users = userManager.Users.ToList();
            var userInfos = new List<UserInfo>() { };

            foreach (ApplicationUser user in users)
            {
                var roles = (await signInManager.UserManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
                userInfos.Add(new UserInfo() { EmailAddress = user.Email, Roles = roles });
            }

            return userInfos.OrderBy(u => u.EmailAddress);
        }

        public async Task SignInAsync(string email, string password)
        {
            SignInResult result = await signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: true);
            if (!result.Succeeded)
                throw new Exception("Username or password incorrect.");
        }

        public async Task LogoutAsync() =>
            await signInManager.SignOutAsync();

        public async Task CreateAdminUserIfRequiredAsync(string email, string password)
        {
            await CreateUserIfRequiredAsync(email, password);
            await AddRoleToUserAsync(email, Roles.admin.ToString());
        }

        public async Task CreateRolesIfRequiredAsync(IEnumerable<string> roles)
        {
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await CreateRoleAsync(roleName);
            }
        }

        public async Task DeleteRoleAsync(string roleName)
        {
            IdentityRole role = roleManager.Roles.First(r => r.Name == roleName) ?? null;
            if (role == null)
                throw new Exception("Role not found.");

            IdentityResult result = await roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                throw new Exception("Role not deleted.");
        }

        public IEnumerable<string> GetRoles() =>
            roleManager.Roles.Select(r => r.Name).OrderBy(r => r);

        public async Task CreateUserIfRequiredAsync(string email, string password)
        {
            ApplicationUser user = await GetUser(email);
            if (user == null) await CreateUserAsync(email, password);
        }

        public async Task CreateUserAsync(string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            IdentityResult createUserResult = await userManager.CreateAsync(user, password);
            if (!createUserResult.Succeeded)
                throw new Exception("User not created.");
        }

        public async Task AddRoleToUserAsync(string email, string role)
        {
            ApplicationUser user = await GetUser(email);
            IList<string> usersCurrentRoles = await userManager.GetRolesAsync(user);
            if (!usersCurrentRoles.Contains(role))
            {
                IdentityResult addToRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addToRoleResult.Succeeded)
                    throw new Exception("Role not added to user.");
            }
        }

        public async Task AddRolesToUserAsync(string email, IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                await AddRoleToUserAsync(email, role);
            }
        }

        public async Task CreateRoleAsync(string roleName)
        {
            var newRole = new IdentityRole();
            newRole.Name = roleName;
            IdentityResult createUserResult = await roleManager.CreateAsync(newRole);
            if (!createUserResult.Succeeded)
                throw new Exception("Role not created.");
        }

        private async Task<ApplicationUser> GetUser(string email) =>
         await userManager.FindByEmailAsync(email);
    }
}